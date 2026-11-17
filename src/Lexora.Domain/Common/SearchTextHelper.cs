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

    /// <summary>
    /// Computes a smart relevance score (0 = no match, up to 100 = exact primary match)
    /// supporting diacritic-insensitive Vietnamese/Latin text, prefix priority,
    /// multi-token matching, weighted secondary fields, and typo-tolerant fuzzy matching.
    /// </summary>
    public static double ScoreMatch(
        string? query,
        string? primaryText,
        params (string? Text, double Weight)[] secondaryFields)
    {
        var normQuery = Normalize(query);
        if (string.IsNullOrEmpty(normQuery)) return 0.0;

        var normPrimary = Normalize(primaryText);
        if (!string.IsNullOrEmpty(normPrimary))
        {
            if (string.Equals(normPrimary, normQuery, StringComparison.Ordinal))
                return 100.0;

            if (normPrimary.StartsWith(normQuery, StringComparison.Ordinal))
                return Math.Max(85.0, 95.0 - Math.Max(0, normPrimary.Length - normQuery.Length) * 0.4);

            if (normPrimary.Contains($" {normQuery} ", StringComparison.Ordinal) ||
                normPrimary.EndsWith($" {normQuery}", StringComparison.Ordinal))
                return 82.0;

            if (normPrimary.Contains(normQuery, StringComparison.Ordinal))
                return 74.0;
        }

        // Secondary field exact/prefix/substring matches
        double bestSecondary = 0.0;
        foreach (var (text, weight) in secondaryFields)
        {
            var normSec = Normalize(text);
            if (string.IsNullOrEmpty(normSec)) continue;

            if (string.Equals(normSec, normQuery, StringComparison.Ordinal))
                bestSecondary = Math.Max(bestSecondary, 70.0 * weight);
            else if (normSec.StartsWith(normQuery, StringComparison.Ordinal))
                bestSecondary = Math.Max(bestSecondary, 64.0 * weight);
            else if (normSec.Contains(normQuery, StringComparison.Ordinal))
                bestSecondary = Math.Max(bestSecondary, 56.0 * weight);
        }

        if (bestSecondary > 0.0)
            return bestSecondary;

        // Multi-token AND match across primary + secondary combined text
        var tokens = normQuery.Split([' ', '\t', ',', '.', ';', '/', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > 1)
        {
            var combined = new StringBuilder(normPrimary);
            foreach (var (text, _) in secondaryFields)
            {
                var normSec = Normalize(text);
                if (!string.IsNullOrEmpty(normSec))
                {
                    combined.Append(' ').Append(normSec);
                }
            }

            var combinedStr = combined.ToString();
            int matchedTokens = tokens.Count(t => combinedStr.Contains(t, StringComparison.Ordinal));
            if (matchedTokens == tokens.Length)
                return 52.0;
        }

        // Typo-tolerant fuzzy match on primary words (only when query length >= 3)
        if (normQuery.Length >= 3 && !string.IsNullOrEmpty(normPrimary))
        {
            int maxDist = normQuery.Length <= 5 ? 1 : 2;
            var primaryWords = normPrimary.Split([' ', '\t', ',', '.', '/', '-'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in primaryWords)
            {
                if (Math.Abs(word.Length - normQuery.Length) > maxDist) continue;
                int dist = ComputeLevenshteinDistance(normQuery, word);
                if (dist <= maxDist)
                {
                    return 48.0 - (dist * 6.0);
                }
            }
        }

        return 0.0;
    }

    private static int ComputeLevenshteinDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) prev[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }

        return prev[b.Length];
    }
}
