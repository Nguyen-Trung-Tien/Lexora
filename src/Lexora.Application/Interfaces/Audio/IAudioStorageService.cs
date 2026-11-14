namespace Lexora.Application.Interfaces;

public interface IAudioStorageService
{
    string GetAudioCacheDirectory();
    Task<string?> CacheAudioAsync(string term, string accent, Stream audioStream, CancellationToken cancellationToken = default);
    string? GetCachedAudioPath(string term, string accent);
}
