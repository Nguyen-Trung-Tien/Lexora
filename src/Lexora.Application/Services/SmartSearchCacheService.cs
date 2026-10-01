using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;

namespace Lexora.Application.Services;

public class SmartSearchCacheService : ISmartSearchCacheService
{
    private readonly SemaphoreSlim _vocabLock = new(1, 1);
    private readonly SemaphoreSlim _grammarLock = new(1, 1);

    private IReadOnlyList<WordDto>? _cachedWords;
    private DateTime _vocabCacheTime = DateTime.MinValue;

    private IReadOnlyList<GrammarRuleDto>? _cachedRules;
    private DateTime _grammarCacheTime = DateTime.MinValue;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<WordDto>> GetCachedWordsAsync(
        Func<Task<IReadOnlyList<WordDto>>> factory,
        bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedWords != null && (DateTime.UtcNow - _vocabCacheTime) < CacheTtl)
        {
            return _cachedWords;
        }

        await _vocabLock.WaitAsync();
        try
        {
            if (!forceRefresh && _cachedWords != null && (DateTime.UtcNow - _vocabCacheTime) < CacheTtl)
            {
                return _cachedWords;
            }

            var words = await factory();
            _cachedWords = words;
            _vocabCacheTime = DateTime.UtcNow;
            return _cachedWords;
        }
        finally
        {
            _vocabLock.Release();
        }
    }

    public async Task<IReadOnlyList<GrammarRuleDto>> GetCachedGrammarRulesAsync(
        Func<Task<IReadOnlyList<GrammarRuleDto>>> factory,
        bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedRules != null && (DateTime.UtcNow - _grammarCacheTime) < CacheTtl)
        {
            return _cachedRules;
        }

        await _grammarLock.WaitAsync();
        try
        {
            if (!forceRefresh && _cachedRules != null && (DateTime.UtcNow - _grammarCacheTime) < CacheTtl)
            {
                return _cachedRules;
            }

            var rules = await factory();
            _cachedRules = rules;
            _grammarCacheTime = DateTime.UtcNow;
            return _cachedRules;
        }
        finally
        {
            _grammarLock.Release();
        }
    }

    public void InvalidateVocabulary()
    {
        _cachedWords = null;
        _vocabCacheTime = DateTime.MinValue;
    }

    public void InvalidateGrammar()
    {
        _cachedRules = null;
        _grammarCacheTime = DateTime.MinValue;
    }
}
