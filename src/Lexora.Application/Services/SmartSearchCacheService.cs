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

    private int _vocabVersion = 0;
    private int _grammarVersion = 0;

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

            int currentVersion = Volatile.Read(ref _vocabVersion);
            var words = await factory();

            // Only cache if no invalidation occurred during factory execution
            if (currentVersion == Volatile.Read(ref _vocabVersion))
            {
                _cachedWords = words;
                _vocabCacheTime = DateTime.UtcNow;
            }

            return words;
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

            int currentVersion = Volatile.Read(ref _grammarVersion);
            var rules = await factory();

            // Only cache if no invalidation occurred during factory execution
            if (currentVersion == Volatile.Read(ref _grammarVersion))
            {
                _cachedRules = rules;
                _grammarCacheTime = DateTime.UtcNow;
            }

            return rules;
        }
        finally
        {
            _grammarLock.Release();
        }
    }

    public void InvalidateVocabulary()
    {
        Interlocked.Increment(ref _vocabVersion);
        _cachedWords = null;
        _vocabCacheTime = DateTime.MinValue;
    }

    public void InvalidateGrammar()
    {
        Interlocked.Increment(ref _grammarVersion);
        _cachedRules = null;
        _grammarCacheTime = DateTime.MinValue;
    }
}
