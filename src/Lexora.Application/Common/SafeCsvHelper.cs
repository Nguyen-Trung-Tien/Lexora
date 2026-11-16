namespace Lexora.Application.Common;

/// <summary>
/// Provides shared CSV and TSV escaping helpers with CWE-1236 formula injection neutralization.
/// </summary>
public static class SafeCsvHelper
{
    private static readonly char[] FormulaTriggers = ['=', '+', '-', '@', '\t', '\r'];

    /// <summary>
    /// Neutralizes potential spreadsheet formula triggers (CWE-1236) by prefixing with a single quote.
    /// </summary>
    public static string NeutralizeFormula(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (FormulaTriggers.Contains(value[0]))
        {
            return "'" + value;
        }

        var trimmedStart = value.TrimStart();
        if (trimmedStart.Length > 0 && FormulaTriggers.Contains(trimmedStart[0]))
        {
            return "'" + value;
        }

        return value;
    }

    /// <summary>
    /// Escapes a field for CSV output, neutralizing formula triggers and quoting when necessary or when <paramref name="alwaysQuote"/> is true.
    /// </summary>
    public static string EscapeCsvField(string? value, bool alwaysQuote = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            return alwaysQuote ? "\"\"" : string.Empty;
        }

        var safeField = NeutralizeFormula(value);
        bool needsQuotes = alwaysQuote
            || !ReferenceEquals(safeField, value)
            || safeField.Contains(',')
            || safeField.Contains('"')
            || safeField.Contains('\n')
            || safeField.Contains('\r');

        return needsQuotes
            ? $"\"{safeField.Replace("\"", "\"\"")}\""
            : safeField;
    }

    /// <summary>
    /// Sanitizes a field for TSV output by replacing tabs/newlines and neutralizing formula triggers.
    /// </summary>
    public static string SanitizeTsvField(string? value)
    {
        var cleaned = (value ?? string.Empty)
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        return NeutralizeFormula(cleaned);
    }
}
