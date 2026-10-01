using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IGrammarSuggestionService
{
    Task<GrammarRelatedSuggestionsDto> GetSuggestionsAsync(Guid ruleId, CancellationToken cancellationToken = default);
    GrammarRelatedSuggestionsDto GetSuggestions(GrammarRuleDto targetRule, IEnumerable<GrammarRuleDto> grammarPool, IEnumerable<WordDto>? vocabularyPool = null);
    IReadOnlyList<GrammarSuggestionItemDto> GetSimilarStructures(GrammarRuleDto targetRule, IEnumerable<GrammarRuleDto> pool);
    IReadOnlyList<GrammarSuggestionItemDto> GetConfusedGrammar(GrammarRuleDto targetRule, IEnumerable<GrammarRuleDto> pool);
    IReadOnlyList<GrammarSuggestionItemDto> GetSameTopicRules(GrammarRuleDto targetRule, IEnumerable<GrammarRuleDto> pool);
    IReadOnlyList<WordSuggestionItemDto> GetRelatedVocabulary(GrammarRuleDto targetRule, IEnumerable<WordDto> vocabularyPool);
}
