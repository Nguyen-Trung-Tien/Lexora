namespace Lexora.Application.Interfaces;

public record FuzzyMatchResult<T>(
    T Item,
    int Distance,
    double SimilarityScore,
    bool IsExactOrPrefixMatch
);

public interface IFuzzySearchService
{
    int CalculateDistance(string source, string target);
    double CalculateSimilarity(string source, string target);
    IReadOnlyList<FuzzyMatchResult<T>> Search<T>(string query, IEnumerable<T> candidates, Func<T, string> textSelector, int maxResults = 20, int maxDistance = 2);
    string? SuggestCorrection(string query, IEnumerable<string> vocabularyTerms, int maxDistance = 2);
}
