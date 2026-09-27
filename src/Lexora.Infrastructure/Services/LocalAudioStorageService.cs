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


    public string? GetCachedAudioPath(string term, string accent)
    {
        var normalizedAccent = NormalizeAccent(accent);
        var wavPath = GetSafeFilePath(term, normalizedAccent, ".wav");
        if (wavPath != null && File.Exists(wavPath)) return wavPath;

        var mp3Path = GetSafeFilePath(term, normalizedAccent, ".mp3");
        if (mp3Path != null && File.Exists(mp3Path)) return mp3Path;

        return null;
    }

    public async Task<string?> CacheAudioAsync(string term, string accent, Stream audioStream, CancellationToken cancellationToken = default)
    {
        var normalizedAccent = NormalizeAccent(accent);
        // Default to .wav for synthesizer streams or fallback
        var filePath = GetSafeFilePath(term, normalizedAccent, ".wav");
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

    public static string NormalizeAccent(string? accent)
    {
        if (string.IsNullOrWhiteSpace(accent)) return "uk";
        var clean = SafeCharRegex.Replace(accent.Trim().ToLowerInvariant(), "").Trim();
        if (clean == "en-gb" || clean == "engb" || clean == "gb" || clean == "british" || clean == "uk") return "uk";
        if (clean == "en-us" || clean == "enus" || clean == "usa" || clean == "american" || clean == "us") return "us";
        return clean.Length > 0 ? clean : "uk";
    }

    private string? GetSafeFilePath(string term, string accent, string extension = ".wav")
    {
        if (string.IsNullOrWhiteSpace(term)) return null;

        var safeTerm = SafeCharRegex.Replace(term.Trim().ToLowerInvariant(), "_").Trim('_');
        var safeAccent = NormalizeAccent(accent);

        if (string.IsNullOrWhiteSpace(safeTerm)) return null;

        // Limit file name length
        if (safeTerm.Length > 100) safeTerm = safeTerm.Substring(0, 100);

        var ext = extension.StartsWith('.') ? extension : "." + extension;
        var fileName = $"{safeTerm}_{safeAccent}{ext}";
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
