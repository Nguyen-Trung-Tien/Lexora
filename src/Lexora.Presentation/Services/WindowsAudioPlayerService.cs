using System.IO;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Lexora.Presentation.Services;

public class WindowsAudioPlayerService : IAudioPlayerService, IDisposable
{
    private readonly IAudioStorageService _audioStorage;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IPronunciationVoiceResolver _voiceResolver;
    private readonly MediaPlayer _mediaPlayer;
    private readonly SpeechSynthesizer _synthesizer;
    private readonly SemaphoreSlim _audioSemaphore = new(1, 1);
    private readonly EventHandler<UserPreferences> _preferencesChangedHandler;

    public double PlaybackRate { get; set; } = 1.0;

    public event EventHandler<VoiceResolutionResult>? FallbackVoiceUsed;

    public WindowsAudioPlayerService(
        IAudioStorageService audioStorage,
        IUserPreferencesService preferencesService,
        IPronunciationVoiceResolver voiceResolver)
    {
        _audioStorage = audioStorage;
        _preferencesService = preferencesService;
        _voiceResolver = voiceResolver;
        _mediaPlayer = new MediaPlayer();
        _synthesizer = new SpeechSynthesizer();

        PlaybackRate = _preferencesService.GetPreferences().SpeechRate;
        _preferencesChangedHandler = (_, prefs) =>
        {
            PlaybackRate = prefs.SpeechRate;
        };
        _preferencesService.PreferencesChanged += _preferencesChangedHandler;
    }

    public VoiceResolutionResult ResolveVoice(string accentOrLocale)
    {
        return _voiceResolver.ResolveVoice(accentOrLocale);
    }

    public IReadOnlyList<VoiceDescriptor> GetAvailableVoices()
    {
        return _voiceResolver.GetAvailableVoices();
    }

    public async Task PlayPronunciationAsync(string term, string accent = "uk", double? rate = null)
    {
        if (string.IsNullOrWhiteSpace(term)) return;

        var safeTerm = term.Trim();
        if (safeTerm.Length > 150)
        {
            safeTerm = safeTerm.Substring(0, 150);
        }

        double effectiveRate = Math.Clamp(rate ?? PlaybackRate, 0.5, 2.0);

        // Resolve voice logic before locking playback
        var resolution = _voiceResolver.ResolveVoice(accent);
        if (resolution.IsFallback)
        {
            try
            {
                FallbackVoiceUsed?.Invoke(this, resolution);
            }
            catch
            {
                // Never allow event subscriber exceptions to break playback
            }
        }

        await _audioSemaphore.WaitAsync();
        try
        {
            // 1. Stop any currently playing audio before beginning new playback
            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;

            // Set media player playback speed
            _mediaPlayer.PlaybackSession.PlaybackRate = effectiveRate;

            // 2. Check if cached audio file exists for the specific accent/locale
            var cachedPath = _audioStorage.GetCachedAudioPath(safeTerm, resolution.RequestedLocale);
            if (cachedPath != null && File.Exists(cachedPath))
            {
                var fileUri = new Uri(cachedPath);
                _mediaPlayer.Source = MediaSource.CreateFromUri(fileUri);
                _mediaPlayer.Play();
                return;
            }

            // 3. Speech Synthesis
            if (resolution.Voice != null)
            {
                var matchingVoice = SpeechSynthesizer.AllVoices
                    .FirstOrDefault(v => string.Equals(v.Id, resolution.Voice.Id, StringComparison.OrdinalIgnoreCase));

                if (matchingVoice != null)
                {
                    _synthesizer.Voice = matchingVoice;
                }
            }

            _synthesizer.Options.SpeakingRate = effectiveRate;

            var stream = await _synthesizer.SynthesizeTextToStreamAsync(safeTerm);

            // 4. Cache synthesized audio ONLY if it was an exact match (do not pollute UK cache with US fallback speech)
            if (resolution.IsExactMatch)
            {
                try
                {
                    using var memStream = new MemoryStream();
                    using (var readStream = stream.AsStreamForRead())
                    {
                        await readStream.CopyToAsync(memStream);
                    }
                    memStream.Position = 0;
                    await _audioStorage.CacheAudioAsync(safeTerm, resolution.RequestedLocale, memStream);
                    stream.Seek(0);
                }
                catch
                {
                    // Fail gracefully on cache write failure
                }
            }

            _mediaPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
            _mediaPlayer.Play();
        }
        catch
        {
            // Fail silently or handle missing audio hardware gracefully
        }
        finally
        {
            _audioSemaphore.Release();
        }
    }

    public void Stop()
    {
        try
        {
            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _preferencesService.PreferencesChanged -= _preferencesChangedHandler;
        Stop();
        _mediaPlayer.Dispose();
        _synthesizer.Dispose();
        _audioSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
