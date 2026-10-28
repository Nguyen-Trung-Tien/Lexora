namespace Lexora.Application.DTOs;

/// <summary>
/// Encapsulates metadata about an application release/update payload.
/// </summary>
public record UpdateInfo(
    string CurrentVersion,
    string NewVersion,
    string ReleaseTitle,
    string ReleaseNotes,
    string DownloadUrl,
    long FileSizeBytes,
    string? Sha256Checksum,
    DateTimeOffset PublishedAt,
    bool IsMandatory = false,
    string? AssetApiUrl = null)
{
    /// <summary>
    /// Checks whether the newly discovered version is strictly greater than the current version.
    /// Safely handles semantic version prefixes ('v') and build suffixes.
    /// </summary>
    public bool HasUpdate
    {
        get
        {
            var cur = NormalizeVersionString(CurrentVersion);
            var next = NormalizeVersionString(NewVersion);

            if (Version.TryParse(cur, out var parsedCur) && Version.TryParse(next, out var parsedNext))
            {
                return parsedNext > parsedCur;
            }

            return false;
        }
    }

    private static string NormalizeVersionString(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return "0.0.0.0";
        var cleaned = version.Trim().TrimStart('v', 'V');
        var dashIdx = cleaned.IndexOf('-');
        if (dashIdx >= 0)
        {
            cleaned = cleaned[..dashIdx];
        }
        var plusIdx = cleaned.IndexOf('+');
        if (plusIdx >= 0)
        {
            cleaned = cleaned[..plusIdx];
        }

        var parts = cleaned.Split('.');
        if (parts.Length == 1) return $"{parts[0]}.0.0.0";
        if (parts.Length == 2) return $"{parts[0]}.{parts[1]}.0.0";
        if (parts.Length == 3) return $"{parts[0]}.{parts[1]}.{parts[2]}.0";
        return cleaned;
    }
}

/// <summary>
/// Progress reporting model for downloading an update asset.
/// </summary>
public record UpdateDownloadProgress(
    long BytesReceived,
    long TotalBytesToReceive,
    double ProgressPercentage);
