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

        int n = s.Length;
        int m = t.Length;

        // Use stackalloc for small strings to avoid heap allocation
        Span<int> prevRow = n < 128 ? stackalloc int[n + 1] : new int[n + 1];
        Span<int> currRow = n < 128 ? stackalloc int[n + 1] : new int[n + 1];

        for (int i = 0; i <= n; i++)
        {
            prevRow[i] = i;
        }

        for (int j = 1; j <= m; j++)
        {
            currRow[0] = j;
            char tChar = t[j - 1];

            for (int i = 1; i <= n; i++)
            {
                int cost = s[i - 1] == tChar ? 0 : 1;
                currRow[i] = Math.Min(
                    Math.Min(currRow[i - 1] + 1, prevRow[i] + 1),
                    prevRow[i - 1] + cost);
            }

            currRow.CopyTo(prevRow);
        }

        return prevRow[n];
    }

    public double CalculateSimilarity(string source, string target)
    {
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        int distance = CalculateDistance(source, target);
        int maxLen = Math.Max(source.Length, target.Length);
        if (maxLen == 0) return 1.0;

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
            if (dist <= maxDistance && dist < minDistance)
            {
                double sim = CalculateSimilarity(cleanQuery, cleanTerm);
                if (sim >= 0.60 && sim > maxSimilarity)
                {
                    minDistance = dist;
                    maxSimilarity = sim;
                    bestCandidate = term;
                }
            }
        }

        return bestCandidate;
    }
}
