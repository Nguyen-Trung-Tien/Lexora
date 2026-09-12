using Lexora.Infrastructure.Services;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Lexora.Presentation.Services;

public interface IAudioPlayerService
{
    Task PlayPronunciationAsync(string term, string accent = "uk");
}

public class WindowsAudioPlayerService : IAudioPlayerService, IDisposable
{
    private readonly IAudioStorageService _audioStorage;
    private readonly MediaPlayer _mediaPlayer;
    private readonly SpeechSynthesizer _synthesizer;

    public WindowsAudioPlayerService(IAudioStorageService audioStorage)
    {
        _audioStorage = audioStorage;
        _mediaPlayer = new MediaPlayer();
        _synthesizer = new SpeechSynthesizer();
    }

    public async Task PlayPronunciationAsync(string term, string accent = "uk")
    {
        if (string.IsNullOrWhiteSpace(term)) return;

        var safeTerm = term.Trim();
        if (safeTerm.Length > 150)
        {
            safeTerm = safeTerm.Substring(0, 150);
        }

        try
        {
            // Stop any currently playing audio before beginning new playback
            _mediaPlayer.Pause();

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

            var stream = await _synthesizer.SynthesizeTextToStreamAsync(safeTerm);
            _mediaPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
            _mediaPlayer.Play();
        }
        catch
        {
            // Fail silently or log if sound device not available
        }
    }

    public void Dispose()
    {
        _mediaPlayer.Dispose();
        _synthesizer.Dispose();
        GC.SuppressFinalize(this);
    }
}
