using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface ISmartSearchCacheService
{
    Task<IReadOnlyList<WordDto>> GetCachedWordsAsync(Func<Task<IReadOnlyList<WordDto>>> factory, bool forceRefresh = false);
    Task<IReadOnlyList<GrammarRuleDto>> GetCachedGrammarRulesAsync(Func<Task<IReadOnlyList<GrammarRuleDto>>> factory, bool forceRefresh = false);
    void InvalidateVocabulary();
    void InvalidateGrammar();
}
