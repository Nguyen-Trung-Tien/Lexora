namespace Lexora.Application.Services;

public enum DataFormat
{
    Unknown = 0,
    Json = 1,
    Csv = 2,
    Txt = 3
}

public static class SmartContentDetector
{
    private static readonly HashSet<string> UnsupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xlsx", ".xls", ".doc", ".docx", ".pdf", ".zip", ".rar", ".7z", ".tar", ".gz",
        ".exe", ".dll", ".bin", ".dat", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico",
        ".mp3", ".wav", ".m4a", ".mp4", ".mkv", ".avi", ".sqlite", ".db"
    };

    /// <summary>
    /// Detects the data format from the file extension and/or raw content string.
    /// </summary>
    public static DataFormat DetectFormat(string? content, string? fileNameOrExtension = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return DataFormat.Unknown;
        }

        // Binary check: if null characters are present, it's not valid text
        if (content.Contains('\0'))
        {
            return DataFormat.Unknown;
        }

        // 1. Check file extension hint if available
        if (!string.IsNullOrWhiteSpace(fileNameOrExtension))
        {
            var ext = fileNameOrExtension.Trim().ToLowerInvariant();
            var dotExt = Path.GetExtension(ext);
            if (!string.IsNullOrEmpty(dotExt) && UnsupportedExtensions.Contains(dotExt))
            {
                return DataFormat.Unknown;
            }

            if (ext.EndsWith(".json")) return DataFormat.Json;
            if (ext.EndsWith(".csv")) return DataFormat.Csv;
            if (ext.EndsWith(".tsv") || ext.EndsWith(".txt") || ext.EndsWith(".tab") || ext.EndsWith(".md")) return DataFormat.Txt;
        }

        var trimmed = content.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (trimmed.Length == 0)
        {
            return DataFormat.Unknown;
        }

        // 2. Check for markdown code fences or direct JSON structure
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            return DataFormat.Json;
        }

        if (trimmed.StartsWith("```csv", StringComparison.OrdinalIgnoreCase))
        {
            return DataFormat.Csv;
        }

        if (trimmed.StartsWith('[') || (trimmed.StartsWith('{') && (trimmed.Contains("\"term\"") || trimmed.Contains("\"title\"") || trimmed.Contains("\"meanings\""))))
        {
            return DataFormat.Json;
        }

        // 3. Inspect first few non-empty lines
        var lines = trimmed.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            return DataFormat.Unknown;
        }

        var firstLine = lines[0];

        // If lines contain tabs, it's almost certainly TSV / Anki / Quizlet
        if (lines.Take(5).Any(l => l.Contains('\t')))
        {
            return DataFormat.Txt;
        }

        // Check for CSV header or comma/semicolon patterns
        char? sep = firstLine.Contains(',') ? ',' : (firstLine.Contains(';') ? ';' : null);
        if (sep.HasValue && !firstLine.StartsWith('#'))
        {
            var charSep = sep.Value;
            if (firstLine.Contains($"term{charSep}", StringComparison.OrdinalIgnoreCase) ||
                firstLine.Contains($"title{charSep}", StringComparison.OrdinalIgnoreCase) ||
                firstLine.Contains($"structure{charSep}", StringComparison.OrdinalIgnoreCase) ||
                firstLine.Contains("meaningsvi", StringComparison.OrdinalIgnoreCase) ||
                firstLine.Contains($"level{charSep}", StringComparison.OrdinalIgnoreCase) ||
                firstLine.Contains($"{charSep}definition", StringComparison.OrdinalIgnoreCase))
            {
                return DataFormat.Csv;
            }

            // If multiple lines consistently have 2 or more separators
            var validDataLines = lines.Take(5).Where(l => !l.StartsWith('#')).ToList();
            if (validDataLines.Count > 0 && validDataLines.All(l => l.Count(c => c == charSep) >= 2))
            {
                return DataFormat.Csv;
            }
        }

        // Default to TXT / Delimited list
        return DataFormat.Txt;
    }
}
