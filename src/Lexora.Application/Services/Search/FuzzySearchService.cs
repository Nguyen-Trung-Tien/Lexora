using Lexora.Application.Interfaces;

namespace Lexora.Application.Services;

public class FuzzySearchService : IFuzzySearchService
{
    public int CalculateDistance(string source, string target)
    {
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            return 0;

        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var s = source.Trim().ToLowerInvariant();
        var t = target.Trim().ToLowerInvariant();

        if (s == t) return 0;

        int n = s.Length;
        int m = t.Length;

        // Optimization: Ensure m is the shorter string to minimize row allocation and maximize stackalloc usage
        if (m > n)
        {
            (s, t) = (t, s);
            (n, m) = (m, n);
        }

        // Bound maximum comparison lengths to prevent quadratic slowdown on anomalous large inputs
        if (m > 256)
        {
            m = 256;
            t = t[..256];
        }
        if (n > 512)
        {
            n = 512;
            s = s[..512];
        }

        // Use stackalloc for small strings to avoid heap allocation (3 rows for Damerau-Levenshtein transpositions)
        int rowLen = m + 1;
        Span<int> row0 = rowLen <= 128 ? stackalloc int[rowLen] : new int[rowLen];
        Span<int> row1 = rowLen <= 128 ? stackalloc int[rowLen] : new int[rowLen];
        Span<int> row2 = rowLen <= 128 ? stackalloc int[rowLen] : new int[rowLen];

        for (int j = 0; j <= m; j++)
        {
            row1[j] = j;
        }

        for (int i = 1; i <= n; i++)
        {
            row2[0] = i;
            char sChar = s[i - 1];

            for (int j = 1; j <= m; j++)
            {
                char tChar = t[j - 1];
                int cost = sChar == tChar ? 0 : 1;

                int insertion = row2[j - 1] + 1;
                int deletion = row1[j] + 1;
                int substitution = row1[j - 1] + cost;

                int min = Math.Min(Math.Min(insertion, deletion), substitution);

                // Damerau-Levenshtein adjacent transposition
                if (i > 1 && j > 1 && sChar == t[j - 2] && s[i - 2] == tChar)
                {
                    int transposition = row0[j - 2] + 1;
                    if (transposition < min)
                    {
                        min = transposition;
                    }
                }

                row2[j] = min;
            }

            row1.CopyTo(row0);
            row2.CopyTo(row1);
        }

        return row1[m];
    }

    public double CalculateSimilarity(string source, string target)
    {
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        var s = source.Trim();
        var t = target.Trim();

        if (string.Equals(s, t, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        int maxLen = Math.Max(s.Length, t.Length);
        if (maxLen == 0) return 1.0;

        int distance = CalculateDistance(s, t);
        return Math.Max(0.0, 1.0 - (double)distance / maxLen);
    }

    public IReadOnlyList<FuzzyMatchResult<T>> Search<T>(
        string query,
        IEnumerable<T> candidates,
        Func<T, string> textSelector,
        int maxResults = 20,
        int maxDistance = 2)
    {
        if (string.IsNullOrWhiteSpace(query) || candidates == null)
            return Array.Empty<FuzzyMatchResult<T>>();

        var cleanQuery = query.Trim().ToLowerInvariant();
        var results = new List<FuzzyMatchResult<T>>();

        foreach (var item in candidates)
        {
            var text = textSelector(item);
            if (string.IsNullOrWhiteSpace(text)) continue;

            var cleanText = text.Trim().ToLowerInvariant();

            // 1. Exact match
            if (cleanText == cleanQuery)
            {
                results.Add(new FuzzyMatchResult<T>(item, 0, 1.0, true));
                continue;
            }

            // 2. Starts with query (prefix match)
            if (cleanText.StartsWith(cleanQuery, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new FuzzyMatchResult<T>(item, 0, 0.95, true));
                continue;
            }

            // 3. Substring match
            if (cleanText.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new FuzzyMatchResult<T>(item, 1, 0.85, true));
                continue;
            }

            // 4. Fuzzy distance
            int dist = CalculateDistance(cleanQuery, cleanText);
            if (dist <= maxDistance)
            {
                double score = CalculateSimilarity(cleanQuery, cleanText);
                results.Add(new FuzzyMatchResult<T>(item, dist, score, false));
            }
        }

        return results
            .OrderByDescending(r => r.IsExactOrPrefixMatch)
            .ThenByDescending(r => r.SimilarityScore)
            .ThenBy(r => r.Distance)
            .Take(maxResults)
            .ToList();
    }

    public string? SuggestCorrection(string query, IEnumerable<string> vocabularyTerms, int maxDistance = 2)
    {
        if (string.IsNullOrWhiteSpace(query) || vocabularyTerms == null)
            return null;

        var cleanQuery = query.Trim().ToLowerInvariant();
        if (cleanQuery.Length < 3) return null;

        string? bestCandidate = null;
        int minDistance = int.MaxValue;
        double maxSimilarity = 0.0;

        foreach (var term in vocabularyTerms)
        {
            if (string.IsNullOrWhiteSpace(term)) continue;
            var cleanTerm = term.Trim().ToLowerInvariant();

            // If exact match already exists, no typo suggestion needed
            if (cleanTerm == cleanQuery)
                return null;

            int dist = CalculateDistance(cleanQuery, cleanTerm);
            if (dist <= maxDistance)
            {
                double sim = CalculateSimilarity(cleanQuery, cleanTerm);
                if (sim >= 0.60)
                {
                    bool isBetter = sim > maxSimilarity ||
                                    (Math.Abs(sim - maxSimilarity) < 0.001 && dist < minDistance) ||
                                    (Math.Abs(sim - maxSimilarity) < 0.001 && dist == minDistance &&
                                     bestCandidate != null && Math.Abs(cleanTerm.Length - cleanQuery.Length) < Math.Abs(bestCandidate.Length - cleanQuery.Length));

                    if (isBetter)
                    {
                        minDistance = dist;
                        maxSimilarity = sim;
                        bestCandidate = term;
                    }
                }
            }
        }

        return bestCandidate;
    }
}
