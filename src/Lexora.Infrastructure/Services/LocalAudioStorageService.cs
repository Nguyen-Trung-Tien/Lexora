namespace Lexora.Infrastructure.Services;

public interface IAudioStorageService
{
    string GetAudioCacheDirectory();
    Task<string?> CacheAudioAsync(string term, string accent, Stream audioStream, CancellationToken cancellationToken = default);
    string? GetCachedAudioPath(string term, string accent);
}

public class LocalAudioStorageService : IAudioStorageService
{
    private readonly string _cacheDirectory;

    public LocalAudioStorageService(string? baseDir = null)
    {
        _cacheDirectory = baseDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Lexora",
            "audio");

        Directory.CreateDirectory(_cacheDirectory);
    }

    public string GetAudioCacheDirectory() => _cacheDirectory;

    public async Task<string?> CacheAudioAsync(string term, string accent, Stream audioStream, CancellationToken cancellationToken = default)
    {
        var sanitized = string.Concat(term.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(_cacheDirectory, $"{sanitized}_{accent.ToLowerInvariant()}.mp3");

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await audioStream.CopyToAsync(fileStream, cancellationToken);

        return filePath;
    }

    public string? GetCachedAudioPath(string term, string accent)
    {
        var sanitized = string.Concat(term.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(_cacheDirectory, $"{sanitized}_{accent.ToLowerInvariant()}.mp3");
        return File.Exists(filePath) ? filePath : null;
    }
}
