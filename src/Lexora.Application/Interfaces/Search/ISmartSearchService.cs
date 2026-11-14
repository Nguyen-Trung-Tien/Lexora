using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface ISmartSearchService
{
    SmartSearchMatchResult<WordDto>? ScoreWord(string query, WordDto word);
    SmartSearchMatchResult<GrammarRuleDto>? ScoreGrammar(string query, GrammarRuleDto rule);

    IReadOnlyList<SmartSearchMatchResult<WordDto>> RankWords(
        string query,
        IEnumerable<WordDto> candidates,
        int take = 50);

    IReadOnlyList<SmartSearchMatchResult<GrammarRuleDto>> RankGrammar(
        string query,
        IEnumerable<GrammarRuleDto> candidates,
        int take = 50);

    IReadOnlyList<string> FindClosestWordSuggestions(
        string query,
        IEnumerable<string> candidates,
        int maxSuggestions = 5,
        double minSimilarity = 0.55);

    IReadOnlyList<string> FindClosestWordSuggestions(
        string query,
        IEnumerable<WordDto> candidates,
        int maxSuggestions = 5,
        double minSimilarity = 0.55);

    IReadOnlyList<string> FindClosestGrammarSuggestions(
        string query,
        IEnumerable<string> candidates,
        int maxSuggestions = 5,
        double minSimilarity = 0.55);

    IReadOnlyList<string> FindClosestGrammarRuleSuggestions(
        string query,
        IEnumerable<GrammarRuleDto> candidates,
        int maxSuggestions = 5,
        double minSimilarity = 0.55);

    string NormalizeSearchText(string text);
    IReadOnlyList<string> Tokenize(string text);
}
