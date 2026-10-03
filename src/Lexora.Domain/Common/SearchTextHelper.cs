using System.Globalization;
using System.Text;

namespace Lexora.Domain.Common;

/// <summary>
/// Provides high-performance, diacritic-insensitive normalization for Vietnamese and Latin text search.
/// </summary>
public static class SearchTextHelper
{
    /// <summary>
    /// Normalizes text by trimming, converting to lowercase, decomposing Unicode marks,
    /// removing non-spacing diacritical marks (accents), and mapping Vietnamese 'đ'/'Đ' to 'd'.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (char c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                if (c == 'đ' || c == 'Đ')
                {
                    sb.Append('d');
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
