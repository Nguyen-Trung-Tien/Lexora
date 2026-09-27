using System.IO;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Lexora.Presentation.Services;

/// <summary>
/// Windows-native TTS and cached-audio pronunciation service.
/// A fresh <see cref="SpeechSynthesizer"/> is created per synthesis call to avoid
/// COM shared-state race conditions (COMException 0x8001010E on non-UI threads).
/// </summary>
public class WindowsAudioPlayerService : IAudioPlayerService, IDisposable
{
    private readonly IAudioStorageService _audioStorage;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IPronunciationVoiceResolver _voiceResolver;
    private readonly ILogger<WindowsAudioPlayerService> _logger;
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
        ILogger<WindowsAudioPlayerService>? logger = null)
    {
        _audioStorage = audioStorage;
        _preferencesService = preferencesService;
        _voiceResolver = voiceResolver;
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

        // Resolve voice before acquiring the semaphore (pure CPU work)
        var resolution = _voiceResolver.ResolveVoice(accent);
        if (resolution.IsFallback)
        {
            try { FallbackVoiceUsed?.Invoke(this, resolution); }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "FallbackVoiceUsed event handler threw.");
            }
        }

        await _audioSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;

            // Stop any currently playing audio
            StopInternal();
            _mediaPlayer.PlaybackSession.PlaybackRate = effectiveRate;

            // 1. Try cached human audio
            var cachedPath = _audioStorage.GetCachedAudioPath(safeTerm, resolution.RequestedLocale);
            if (cachedPath != null && File.Exists(cachedPath))
            {
                _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(cachedPath));
                _mediaPlayer.Play();
                return;
            }

            // 2. Synthesize via Windows TTS.
            //    A fresh SpeechSynthesizer per call prevents shared-state races.
            await SynthesizeAndPlayAsync(safeTerm, resolution, effectiveRate).ConfigureAwait(false);
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

    private async Task SynthesizeAndPlayAsync(string term, VoiceResolutionResult resolution, double rate)
    {
        // Fresh synthesizer per call — avoids COMException on non-UI threads
        using var synth = new SpeechSynthesizer();

        if (resolution.Voice != null)
        {
            var matchingVoice = SpeechSynthesizer.AllVoices
                .FirstOrDefault(v => string.Equals(v.Id, resolution.Voice.Id, StringComparison.OrdinalIgnoreCase));
            if (matchingVoice != null)
                synth.Voice = matchingVoice;
        }

        synth.Options.SpeakingRate = rate;

        SpeechSynthesisStream stream;
        try
        {
            stream = await synth.SynthesizeTextToStreamAsync(term);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "SpeechSynthesizer failed for '{Term}'. Voice='{Voice}'. Locale='{Locale}'.",
                term, resolution.Voice?.DisplayName ?? "default", resolution.RequestedLocale);
            return;
        }

        // Cache synthesized audio only for exact-match voices
        if (resolution.IsExactMatch)
        {
            try
            {
                using var memStream = new MemoryStream();
                using (var readStream = stream.AsStreamForRead())
                {
                    await readStream.CopyToAsync(memStream).ConfigureAwait(false);
                }
                memStream.Position = 0;
                await _audioStorage.CacheAudioAsync(term, resolution.RequestedLocale, memStream).ConfigureAwait(false);
                stream.Seek(0);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to cache synthesized audio for '{Term}'.", term);
            }
        }

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
