using System.Text;

namespace Lexora.Application.Services.Search;

/// <summary>
/// Lightweight English phonetic encoder (Metaphone-inspired) for offline pronunciation-based word lookup.
/// Helps learners find words when they only remember how a word sounds (e.g., "nolidge" -> "knowledge").
/// </summary>
public static class PhoneticEncoder
{
    public static string Encode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var clean = new StringBuilder(input.Length);
        foreach (char ch in input.Trim().ToUpperInvariant())
        {
            if (ch is >= 'A' and <= 'Z')
                clean.Append(ch);
        }

        if (clean.Length == 0)
            return string.Empty;

        string s = clean.ToString();

        // Common silent prefix / digraph normalizations
        if (s.StartsWith("KN", StringComparison.Ordinal) ||
            s.StartsWith("GN", StringComparison.Ordinal) ||
            s.StartsWith("PN", StringComparison.Ordinal) ||
            s.StartsWith("WR", StringComparison.Ordinal))
        {
            s = s[1..];
        }
        else if (s.StartsWith("PS", StringComparison.Ordinal))
        {
            s = "S" + s[2..];
        }
        else if (s.StartsWith("X", StringComparison.Ordinal))
        {
            s = "S" + s[1..];
        }

        s = s.Replace("PH", "F", StringComparison.Ordinal)
             .Replace("GH", "", StringComparison.Ordinal)
             .Replace("TION", "XN", StringComparison.Ordinal)
             .Replace("SION", "XN", StringComparison.Ordinal)
             .Replace("CK", "K", StringComparison.Ordinal)
             .Replace("QU", "KW", StringComparison.Ordinal)
             .Replace("DG", "J", StringComparison.Ordinal)
             .Replace("TCH", "X", StringComparison.Ordinal)
             .Replace("CH", "X", StringComparison.Ordinal)
             .Replace("SH", "X", StringComparison.Ordinal)
             .Replace("TH", "0", StringComparison.Ordinal);

        var code = new StringBuilder(s.Length);
        char prev = '\0';

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == prev) continue;
            prev = c;

            // Keep leading vowel, drop interior vowels
            if (c is 'A' or 'E' or 'I' or 'O' or 'U' or 'Y')
            {
                if (i == 0) code.Append('A');
                continue;
            }

            if (c == 'H' || c == 'W') continue;

            char mapped = c switch
            {
                'C' => (i + 1 < s.Length && s[i + 1] is 'E' or 'I' or 'Y') ? 'S' : 'K',
                'G' => (i + 1 < s.Length && s[i + 1] is 'E' or 'I' or 'Y') ? 'J' : 'K',
                'Q' => 'K',
                'V' => 'F',
                'Z' => 'S',
                _ => c
            };

            if (code.Length == 0 || code[^1] != mapped)
            {
                code.Append(mapped);
            }
        }

        return code.ToString();
    }

    public static double CalculateSimilarity(string? query, string? target)
    {
        var c1 = Encode(query);
        var c2 = Encode(target);
        if (string.IsNullOrEmpty(c1) || string.IsNullOrEmpty(c2))
            return 0.0;

        if (string.Equals(c1, c2, StringComparison.Ordinal))
            return 1.0;

        int maxLen = Math.Max(c1.Length, c2.Length);
        int dist = LevenshteinDistance(c1, c2);
        return Math.Max(0.0, 1.0 - (double)dist / maxLen);
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int n = a.Length;
        int m = b.Length;
        Span<int> prev = m + 1 <= 64 ? stackalloc int[m + 1] : new int[m + 1];
        Span<int> curr = m + 1 <= 64 ? stackalloc int[m + 1] : new int[m + 1];

        for (int j = 0; j <= m; j++) prev[j] = j;

        for (int i = 1; i <= n; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= m; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            curr.CopyTo(prev);
        }

        return prev[m];
    }
}
