using System.Security;

namespace Lexora.Application.Common;

/// <summary>
/// Provides unified path validation and normalization to prevent path traversal vulnerabilities.
/// </summary>
public static class SafePathHelper
{
    /// <summary>
    /// Validates that targetPath resolves strictly within the allowed base directory.
    /// Throws SecurityException if path traversal is detected.
    /// </summary>
    /// <param name="targetPath">The proposed relative or absolute file/directory path.</param>
    /// <param name="allowedBaseDirectory">The allowed base directory root.</param>
    /// <returns>The normalized, canonical full path.</returns>
    public static string ValidatePathWithinDirectory(string targetPath, string allowedBaseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(allowedBaseDirectory);

        var fullTargetPath = Path.GetFullPath(targetPath);
        var canonicalBaseDir = Path.GetFullPath(allowedBaseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullTargetPath.StartsWith(canonicalBaseDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException($"Potential path traversal attempt detected. Target path '{targetPath}' is outside the allowed directory '{allowedBaseDirectory}'.");
        }

        return fullTargetPath;
    }

    /// <summary>
    /// Sanitizes a file name by removing invalid file name characters and path navigation sequences.
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "unnamed_file";

        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray()).Trim();
        cleaned = cleaned.Replace("..", string.Empty);

        return string.IsNullOrWhiteSpace(cleaned) ? "unnamed_file" : cleaned;
    }
}
