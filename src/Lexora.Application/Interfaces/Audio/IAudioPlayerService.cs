using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IAudioPlayerService
{
    double PlaybackRate { get; set; }
    Task PlayPronunciationAsync(string term, string accent = "uk", double? rate = null);
    void Stop();
    VoiceResolutionResult ResolveVoice(string accentOrLocale);
    IReadOnlyList<VoiceDescriptor> GetAvailableVoices();
    event EventHandler<VoiceResolutionResult>? FallbackVoiceUsed;
}
