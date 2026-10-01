using System.IO;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Dispatching;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Lexora.Presentation.Services;

/// <summary>
/// Windows-native TTS and cached-audio pronunciation service.
///
/// THREADING CONTRACT:
///   SpeechSynthesizer is a WinRT STA COM object � must be constructed and used
///   exclusively on the UI thread. All synthesis work is dispatched to the UI
///   thread via _dispatcherQueue. Only cache I/O uses ConfigureAwait(false).
/// </summary>
public class WindowsAudioPlayerService : IAudioPlayerService, IDisposable
{
    private readonly IAudioStorageService _audioStorage;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IPronunciationVoiceResolver _voiceResolver;
    private readonly ILogger<WindowsAudioPlayerService> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly MediaPlayer _mediaPlayer;
    private readonly SemaphoreSlim _audioSemaphore = new(1, 1);
    private readonly EventHandler<UserPreferences> _preferencesChangedHandler;
    private bool _disposed;

    public double PlaybackRate { get; set; } = 1.0;

    public event EventHandler<VoiceResolutionResult>? FallbackVoiceUsed;

    public WindowsAudioPlayerService(
        IAudioStorageService audioStorage,
        IUserPreferencesService preferencesService,
        IPronunciationVoiceResolver voiceResolver,
        DispatcherQueue dispatcherQueue,
        ILogger<WindowsAudioPlayerService>? logger = null)
    {
        _audioStorage = audioStorage;
        _preferencesService = preferencesService;
        _voiceResolver = voiceResolver;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger ?? NullLogger<WindowsAudioPlayerService>.Instance;
        _mediaPlayer = new MediaPlayer();

        PlaybackRate = _preferencesService.GetPreferences().SpeechRate;
        _preferencesChangedHandler = (_, prefs) => PlaybackRate = prefs.SpeechRate;
        _preferencesService.PreferencesChanged += _preferencesChangedHandler;
    }

    public VoiceResolutionResult ResolveVoice(string accentOrLocale)
        => _voiceResolver.ResolveVoice(accentOrLocale);

    public IReadOnlyList<VoiceDescriptor> GetAvailableVoices()
        => _voiceResolver.GetAvailableVoices();

    public async Task PlayPronunciationAsync(string term, string accent = "uk", double? rate = null)
    {
        if (string.IsNullOrWhiteSpace(term)) return;

        var safeTerm = term.Trim();
        if (safeTerm.Length > 150)
            safeTerm = safeTerm[..150];

        double effectiveRate = Math.Clamp(rate ?? PlaybackRate, 0.5, 2.0);

        var resolution = _voiceResolver.ResolveVoice(accent);
        if (resolution.IsFallback)
        {
            try { FallbackVoiceUsed?.Invoke(this, resolution); }
            catch (Exception ex) { _logger.LogDebug(ex, "FallbackVoiceUsed event handler threw."); }
        }

        // No ConfigureAwait(false) � preserve context for UI dispatch
        await _audioSemaphore.WaitAsync();
        try
        {
            if (_disposed) return;

            StopInternal();
            _mediaPlayer.PlaybackSession.PlaybackRate = effectiveRate;

            // 1. Try cached human audio
            var cachedPath = _audioStorage.GetCachedAudioPath(safeTerm, resolution.RequestedLocale);
            if (cachedPath != null && File.Exists(cachedPath))
            {
                var bytes = await File.ReadAllBytesAsync(cachedPath).ConfigureAwait(false);
                var stream = new MemoryStream(bytes).AsRandomAccessStream();
                _mediaPlayer.Source = MediaSource.CreateFromStream(stream, "audio/wav");
                _mediaPlayer.Play();
                return;
            }

            // 2. Synthesize via TTS on UI thread (SpeechSynthesizer is WinRT STA)
            await SynthesizeOnUIThreadAsync(safeTerm, resolution, effectiveRate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PlayPronunciationAsync failed for term='{Term}' accent='{Accent}'.", safeTerm, accent);
        }
        finally
        {
            _audioSemaphore.Release();
        }
    }

    private async Task SynthesizeOnUIThreadAsync(string term, VoiceResolutionResult resolution, double rate)
    {
        var tcs = new TaskCompletionSource<SpeechSynthesisStream>(TaskCreationOptions.RunContinuationsAsynchronously);

        bool enqueued = _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                using var synth = new SpeechSynthesizer();
                if (resolution.Voice != null)
                {
                    var match = SpeechSynthesizer.AllVoices
                        .FirstOrDefault(v => string.Equals(v.Id, resolution.Voice.Id, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        synth.Voice = match;
                }
                synth.Options.SpeakingRate = rate;
                var stream = await synth.SynthesizeTextToStreamAsync(term);
                tcs.TrySetResult(stream);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        if (!enqueued)
        {
            _logger.LogWarning("DispatcherQueue.TryEnqueue failed - UI thread may be shut down.");
            tcs.TrySetCanceled();
            return;
        }

        SpeechSynthesisStream stream;
        try
        {
            stream = await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SpeechSynthesizer failed for '{Term}'. Voice='{Voice}'.",
                term, resolution.Voice?.DisplayName ?? "default");
            return;
        }

        if (resolution.IsExactMatch)
        {
            try
            {
                // Clone stream for reading so disposing readStream does not dispose the original stream for MediaPlayer
                using var cloneStream = stream.CloneStream();
                using var memStream = new MemoryStream();
                using (var readStream = cloneStream.AsStreamForRead())
                {
                    await readStream.CopyToAsync(memStream).ConfigureAwait(false);
                }
                memStream.Position = 0;
                await _audioStorage.CacheAudioAsync(term, resolution.RequestedLocale, memStream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to cache audio for '{Term}'.", term);
            }
        }

        stream.Seek(0);
        _mediaPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
        _mediaPlayer.Play();
    }

    public void Stop()
    {
        try { StopInternal(); }
        catch (Exception ex) { _logger.LogDebug(ex, "Stop() failed."); }
    }

    private void StopInternal()
    {
        _mediaPlayer.Pause();
        _mediaPlayer.Source = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _preferencesService.PreferencesChanged -= _preferencesChangedHandler;
        try { StopInternal(); } catch { }
        _mediaPlayer.Dispose();
        _audioSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
