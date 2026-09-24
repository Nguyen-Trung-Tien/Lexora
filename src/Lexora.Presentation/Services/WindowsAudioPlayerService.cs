using Lexora.Application.Interfaces;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Lexora.Presentation.Services;

public class WindowsAudioPlayerService : IAudioPlayerService, IDisposable
{
    private readonly IAudioStorageService _audioStorage;
    private readonly IUserPreferencesService _preferencesService;
    private readonly MediaPlayer _mediaPlayer;
    private readonly SpeechSynthesizer _synthesizer;

    public double PlaybackRate { get; set; } = 1.0;

    public WindowsAudioPlayerService(IAudioStorageService audioStorage, IUserPreferencesService preferencesService)
    {
        _audioStorage = audioStorage;
        _preferencesService = preferencesService;
        _mediaPlayer = new MediaPlayer();
        _synthesizer = new SpeechSynthesizer();

        PlaybackRate = _preferencesService.GetPreferences().SpeechRate;
        _preferencesService.PreferencesChanged += (_, prefs) =>
        {
            PlaybackRate = prefs.SpeechRate;
        };
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

        try
        {
            // Stop any currently playing audio before beginning new playback
            _mediaPlayer.Pause();

            // Set media player playback speed
            _mediaPlayer.PlaybackSession.PlaybackRate = effectiveRate;

            // 1. Check if cached audio file exists
            var cachedPath = _audioStorage.GetCachedAudioPath(safeTerm, accent);
            if (cachedPath != null && File.Exists(cachedPath))
            {
                var fileUri = new Uri(cachedPath);
                _mediaPlayer.Source = MediaSource.CreateFromUri(fileUri);
                _mediaPlayer.Play();
                return;
            }

            // 2. Speech Synthesis fallback
            var voices = SpeechSynthesizer.AllVoices;
            VoiceInformation? targetVoice = null;

            if (accent.Equals("uk", StringComparison.OrdinalIgnoreCase))
            {
                targetVoice = voices.FirstOrDefault(v => v.Language.Equals("en-GB", StringComparison.OrdinalIgnoreCase))
                           ?? voices.FirstOrDefault(v => v.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                targetVoice = voices.FirstOrDefault(v => v.Language.Equals("en-US", StringComparison.OrdinalIgnoreCase))
                           ?? voices.FirstOrDefault(v => v.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
            }

            if (targetVoice != null)
            {
                _synthesizer.Voice = targetVoice;
            }

            _synthesizer.Options.SpeakingRate = effectiveRate;

            var stream = await _synthesizer.SynthesizeTextToStreamAsync(safeTerm);
            _mediaPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
            _mediaPlayer.Play();
        }
        catch
        {
            // Fail silently or log if sound device not available
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
        _mediaPlayer.Dispose();
        _synthesizer.Dispose();
        GC.SuppressFinalize(this);
    }
}
