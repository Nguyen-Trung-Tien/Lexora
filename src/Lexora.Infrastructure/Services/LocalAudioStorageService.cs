using System.Text.RegularExpressions;
using Lexora.Application.Interfaces;

namespace Lexora.Infrastructure.Services;

public class LocalAudioStorageService : IAudioStorageService
{
    private readonly string _cacheDirectory;
    private static readonly Regex SafeCharRegex = new(@"[^a-zA-Z0-9_\-]", RegexOptions.Compiled);

    public LocalAudioStorageService(string? baseDir = null)
    {
        _cacheDirectory = Path.GetFullPath(baseDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Lexora",
            "audio"));

        Directory.CreateDirectory(_cacheDirectory);
    }

    public string GetAudioCacheDirectory() => _cacheDirectory;

    public async Task<string?> CacheAudioAsync(string term, string accent, Stream audioStream, CancellationToken cancellationToken = default)
    {
        var filePath = GetSafeFilePath(term, accent);
        if (filePath == null) return null;

        const long maxAudioBytes = 10 * 1024 * 1024; // 10MB limit
        if (audioStream.CanSeek && audioStream.Length > maxAudioBytes)
        {
            throw new ArgumentException("Audio stream exceeds maximum allowable size (10MB).", nameof(audioStream));
        }

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await audioStream.CopyToAsync(fileStream, cancellationToken);

        if (fileStream.Length > maxAudioBytes)
        {
            fileStream.Dispose();
            try { File.Delete(filePath); } catch { }
            throw new InvalidOperationException("Cached audio exceeds maximum allowable size (10MB).");
        }

        return filePath;
    }

    public string? GetCachedAudioPath(string term, string accent)
    {
        var filePath = GetSafeFilePath(term, accent);
        if (filePath == null) return null;
        return File.Exists(filePath) ? filePath : null;
    }

    private string? GetSafeFilePath(string term, string accent)
    {
        if (string.IsNullOrWhiteSpace(term)) return null;

        var safeTerm = SafeCharRegex.Replace(term.Trim().ToLowerInvariant(), "_").Trim('_');
        var safeAccent = SafeCharRegex.Replace((accent ?? "uk").Trim().ToLowerInvariant(), "").Trim();

        if (string.IsNullOrWhiteSpace(safeTerm)) return null;
        if (string.IsNullOrWhiteSpace(safeAccent)) safeAccent = "uk";

        // Limit file name length
        if (safeTerm.Length > 100) safeTerm = safeTerm.Substring(0, 100);

        var fileName = $"{safeTerm}_{safeAccent}.mp3";
        var fullPath = Path.GetFullPath(Path.Combine(_cacheDirectory, fileName));

        // Ensure path remains strictly within cache directory with trailing separator
        var safeBaseDir = _cacheDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(safeBaseDir, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }
}
