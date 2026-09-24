namespace Lexora.Application.Interfaces;

public interface IAudioPlayerService
{
    double PlaybackRate { get; set; }
    Task PlayPronunciationAsync(string term, string accent = "uk", double? rate = null);
    void Stop();
}
