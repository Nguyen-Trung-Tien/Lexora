using System.Text.RegularExpressions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services;

public class VocabularySuggestionService : IVocabularySuggestionService
{
    private readonly IVocabularyService _vocabService;
    private readonly ISmartSearchService _searchService;

    // Common English suffixes and prefixes for morphological root extraction
    private static readonly string[] Suffixes =
    {
        "tion", "sion", "ment", "ness", "ity", "ance", "ence", "able", "ible",
        "ful", "less", "ly", "ive", "al", "ous", "ize", "ise", "ify", "ing",
        "ed", "er", "or", "est", "ish", "ic", "ship", "hood"
    };

    private static readonly string[] Prefixes =
    {
        "un", "dis", "in", "im", "non", "re", "mis", "pre", "over", "under", "sub", "inter"
    };

    private static readonly HashSet<string> StopWordsVi = new(StringComparer.OrdinalIgnoreCase)
    {
        "la", "mot", "nhung", "cac", "cua", "cho", "va", "hoac", "trong", "tren", "duoi", "khi",
        "de", "nguoi", "su", "viec", "cai", "con", "co", "duoc", "bi", "rat", "qua"
    };

    private static readonly HashSet<string> StopWordsEn = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "if", "in", "on", "at", "to", "for", "with",
        "by", "about", "as", "of", "from", "is", "are", "was", "were", "be", "been", "being"
    };

    public VocabularySuggestionService(
        IVocabularyService? vocabService = null,
        ISmartSearchService? searchService = null)
    {
        _vocabService = vocabService!;
        _searchService = searchService ?? new SmartSearchService(new FuzzySearchService());
    }

    public async Task<WordRelatedSuggestionsDto> GetSuggestionsAsync(Guid wordId, CancellationToken cancellationToken = default)
    {
        var targetWord = await _vocabService.GetWordByIdAsync(wordId, cancellationToken);
        if (targetWord == null)
        {
            return new WordRelatedSuggestionsDto(
                wordId,
                string.Empty,
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>());
        }

        // Fetch candidates: same topics, same POS, or prefix/fuzzy matches
        var pool = await _vocabService.SearchWordsAsync(
            pageSize: 200,
            cancellationToken: cancellationToken);

        return GetSuggestions(targetWord, pool);
    }

    public WordRelatedSuggestionsDto GetSuggestions(WordDto targetWord, IEnumerable<WordDto> vocabularyPool)
    {
        if (targetWord == null || vocabularyPool == null)
        {
            return new WordRelatedSuggestionsDto(
                targetWord?.Id ?? Guid.Empty,
                targetWord?.Term ?? string.Empty,
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>());
        }

        var poolList = vocabularyPool.Where(w => w.Id != targetWord.Id).ToList();

        var synAnt = GetSynonymsAndAntonyms(targetWord, poolList);
        var synonyms = synAnt.Where(s => s.Category == SuggestionCategory.Synonym).ToList();
        var antonyms = synAnt.Where(s => s.Category == SuggestionCategory.Antonym).ToList();

        var wordFamily = GetWordFamily(targetWord, poolList);
        var collocations = GetCollocationsAndPhrases(targetWord, poolList);
        var sameTopic = GetSameTopicWords(targetWord, poolList);

        return new WordRelatedSuggestionsDto(
            targetWord.Id,
            targetWord.Term,
            synonyms,
            antonyms,
            wordFamily,
            collocations,
            sameTopic);
    }

    public IReadOnlyList<WordSuggestionItemDto> GetSynonymsAndAntonyms(WordDto targetWord, IEnumerable<WordDto> pool)
    {
        var results = new List<WordSuggestionItemDto>();
        var targetTerm = targetWord.Term.Trim().ToLowerInvariant();

        // Extract key definition words
        var targetViTokens = ExtractDefinitionKeywords(targetWord, true);
        var targetEnTokens = ExtractDefinitionKeywords(targetWord, false);

        // Check explicit notes annotations (e.g. syn: / opp: / ant:)
        var (explicitSynonyms, explicitAntonyms) = ExtractExplicitRelationsFromNotes(targetWord);

        foreach (var candidate in pool)
        {
            if (candidate.Id == targetWord.Id) continue;
            var candidateTerm = candidate.Term.Trim().ToLowerInvariant();

            // 1. Check explicit matches
            if (explicitSynonyms.Contains(candidateTerm))
            {
                results.Add(new WordSuggestionItemDto(
                    candidate,
                    SuggestionCategory.Synonym,
                    0.95,
                    $"Ghi chú đồng nghĩa trực tiếp với '{targetWord.Term}'"));
                continue;
            }

            if (explicitAntonyms.Contains(candidateTerm))
            {
                results.Add(new WordSuggestionItemDto(
                    candidate,
                    SuggestionCategory.Antonym,
                    0.95,
                    $"Ghi chú trái nghĩa trực tiếp với '{targetWord.Term}'"));
                continue;
            }

            // 2. Definition keyword overlap (Vietnamese)
            var candViTokens = ExtractDefinitionKeywords(candidate, true);
            var commonVi = targetViTokens.Intersect(candViTokens, StringComparer.OrdinalIgnoreCase).ToList();

            // 3. Definition keyword overlap (English)
            var candEnTokens = ExtractDefinitionKeywords(candidate, false);
            var commonEn = targetEnTokens.Intersect(candEnTokens, StringComparer.OrdinalIgnoreCase).ToList();

            if (commonVi.Count >= 2 || (commonVi.Count == 1 && commonVi[0].Length >= 4))
            {
                // Check if candidate definition indicates antonym (trái nghĩa / không / ngược lại)
                bool isAntonym = candidate.PrimaryDefinition.Contains("ngược lại", StringComparison.OrdinalIgnoreCase) ||
                                 candidate.PrimaryDefinition.Contains("trái với", StringComparison.OrdinalIgnoreCase) ||
                                 targetWord.PrimaryDefinition.Contains("trái với", StringComparison.OrdinalIgnoreCase);

                var category = isAntonym ? SuggestionCategory.Antonym : SuggestionCategory.Synonym;
                double score = Math.Min(0.88, 0.60 + (commonVi.Count * 0.10));
                string reason = isAntonym
                    ? $"Từ trái nghĩa (Nghĩa tương phản qua từ khóa '{commonVi[0]}')"
                    : $"Gần nghĩa: chung khái niệm '{string.Join(", ", commonVi.Take(2))}'";

                results.Add(new WordSuggestionItemDto(candidate, category, score, reason));
                continue;
            }

            if (commonEn.Count >= 2)
            {
                results.Add(new WordSuggestionItemDto(
                    candidate,
                    SuggestionCategory.Synonym,
                    Math.Min(0.85, 0.55 + (commonEn.Count * 0.12)),
                    $"Near-synonym: shares English context '{string.Join(", ", commonEn.Take(2))}'"));
            }
        }

        return results.OrderByDescending(r => r.RelevanceScore).Take(10).ToList();
    }

    public IReadOnlyList<WordSuggestionItemDto> GetWordFamily(WordDto targetWord, IEnumerable<WordDto> pool)
    {
        var results = new List<WordSuggestionItemDto>();
        var targetTerm = targetWord.Term.Trim().ToLowerInvariant();
        var targetStem = ExtractStem(targetTerm);

        if (targetStem.Length < 3) return Array.Empty<WordSuggestionItemDto>();

        foreach (var candidate in pool)
        {
            if (candidate.Id == targetWord.Id) continue;
            var candTerm = candidate.Term.Trim().ToLowerInvariant();

            // Skip exact duplicate terms
            if (candTerm == targetTerm) continue;

            var candStem = ExtractStem(candTerm);

            bool isWordFamily = false;
            double score = 0.0;
            string reason = string.Empty;

            // 1. Same stem extracted via morphological rules
            if (candStem.Length >= 3 && candStem == targetStem)
            {
                isWordFamily = true;
                score = 0.85;
                reason = $"Cùng gốc từ '{targetStem}' ({candidate.PrimaryPartOfSpeech})";
            }
            // 2. Candidate is root + valid suffix/prefix of target (or vice versa)
            else if (IsValidAffixDerivative(candTerm, targetStem) || IsValidAffixDerivative(targetTerm, candStem))
            {
                isWordFamily = true;
                score = 0.80;
                reason = $"Cùng họ từ '{targetStem}' ({candidate.PrimaryPartOfSpeech})";
            }

            if (isWordFamily && score >= 0.70)
            {
                results.Add(new WordSuggestionItemDto(
                    candidate,
                    SuggestionCategory.WordFamily,
                    score,
                    reason));
            }
        }

        return results.OrderByDescending(r => r.RelevanceScore).Take(8).ToList();
    }

    public IReadOnlyList<WordSuggestionItemDto> GetCollocationsAndPhrases(WordDto targetWord, IEnumerable<WordDto> pool)
    {
        var results = new List<WordSuggestionItemDto>();
        var targetTerm = targetWord.Term.Trim().ToLowerInvariant();
        var wordPattern = $@"\b{Regex.Escape(targetTerm)}\b";

        foreach (var candidate in pool)
        {
            if (candidate.Id == targetWord.Id) continue;
            var candTerm = candidate.Term.Trim().ToLowerInvariant();

            // 1. Candidate is a phrase containing this word (e.g. "take off", "take care of")
            if (candidate.IsPhrase || candidate.IsIdiom || candidate.IsPhrasalVerb || candTerm.Contains(' '))
            {
                if (Regex.IsMatch(candTerm, wordPattern, RegexOptions.IgnoreCase))
                {
                    results.Add(new WordSuggestionItemDto(
                        candidate,
                        SuggestionCategory.Collocation,
                        0.90,
                        $"Cụm từ / Collocation chứa '{targetWord.Term}'"));
                    continue;
                }
            }

            // 2. Examples contain collocation
            if (candidate.Meanings != null)
            {
                bool exampleMatched = false;
                foreach (var m in candidate.Meanings)
                {
                    if (m.Examples == null) continue;
                    foreach (var ex in m.Examples)
                    {
                        if (Regex.IsMatch(ex.SentenceEn ?? string.Empty, wordPattern, RegexOptions.IgnoreCase))
                        {
                            results.Add(new WordSuggestionItemDto(
                                candidate,
                                SuggestionCategory.Collocation,
                                0.70,
                                $"Xuất hiện chung trong ngữ cảnh ví dụ"));
                            exampleMatched = true;
                            break;
                        }
                    }
                    if (exampleMatched) break;
                }
            }
        }

        return results.OrderByDescending(r => r.RelevanceScore).Take(8).ToList();
    }

    public IReadOnlyList<WordSuggestionItemDto> GetSameTopicWords(WordDto targetWord, IEnumerable<WordDto> pool)
    {
        var results = new List<WordSuggestionItemDto>();
        var targetTopics = targetWord.Topics?.Where(t => !string.IsNullOrWhiteSpace(t)).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();
        var targetTags = targetWord.ExamTags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();

        if (targetTopics.Count == 0 && targetTags.Count == 0)
            return Array.Empty<WordSuggestionItemDto>();

        foreach (var candidate in pool)
        {
            if (candidate.Id == targetWord.Id) continue;

            var candTopics = candidate.Topics ?? new List<string>();
            var sharedTopics = targetTopics.Intersect(candTopics, StringComparer.OrdinalIgnoreCase).ToList();

            var candTags = candidate.ExamTags ?? new List<string>();
            var sharedTags = targetTags.Intersect(candTags, StringComparer.OrdinalIgnoreCase).ToList();

            if (sharedTopics.Count > 0 || sharedTags.Count > 0)
            {
                double score = 0.50 + (sharedTopics.Count * 0.12) + (sharedTags.Count * 0.08);
                string reason = sharedTopics.Count > 0
                    ? $"Cùng chủ đề: {sharedTopics[0]}"
                    : $"Cùng mục tiêu thi: {sharedTags[0]}";

                // Boost if CEFR level is identical or adjacent
                if (candidate.Level == targetWord.Level && candidate.Level != CefrLevel.None)
                {
                    score += 0.05;
                }

                results.Add(new WordSuggestionItemDto(
                    candidate,
                    SuggestionCategory.SameTopic,
                    Math.Min(0.78, score),
                    reason));
            }
        }

        return results.OrderByDescending(r => r.RelevanceScore).Take(10).ToList();
    }

    private static string ExtractStem(string term)
    {
        var stem = term.Trim().ToLowerInvariant();

        // Strip prefix if any
        foreach (var prefix in Prefixes)
        {
            if (stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && stem.Length - prefix.Length >= 3)
            {
                stem = stem[prefix.Length..];
                break;
            }
        }

        // Strip suffix if any
        foreach (var suffix in Suffixes)
        {
            if (stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && stem.Length - suffix.Length >= 3)
            {
                // In English orthography, roots ending in 'e' drop 'e' before vowel suffixes like 'er'/'ed' (baker, carer).
                // Words ending in 'eer' or 'eed' (career, pioneer, succeed, seed) are never root + 'er'/'ed'.
                if ((suffix == "er" && stem.EndsWith("eer", StringComparison.OrdinalIgnoreCase)) ||
                    (suffix == "ed" && stem.EndsWith("eed", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                stem = stem[..^suffix.Length];
                break;
            }
        }

        return stem;
    }

    private static bool IsValidAffixDerivative(string word, string stem)
    {
        if (string.IsNullOrWhiteSpace(stem) || stem.Length < 3 || word.Length <= stem.Length)
            return false;

        // Check prefix derivative: e.g. un-happy vs happy
        foreach (var prefix in Prefixes)
        {
            if (word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                word[prefix.Length..].Equals(stem, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check suffix derivative: e.g. care-ful vs care
        if (word.StartsWith(stem, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = word[stem.Length..];
            // Disallow false roots (e.g. career with stem care, or seed with stem se)
            if ((remainder == "er" && word.EndsWith("eer", StringComparison.OrdinalIgnoreCase)) ||
                (remainder == "ed" && word.EndsWith("eed", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (Suffixes.Contains(remainder, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int GetCommonPrefixLength(string a, string b)
    {
        int len = Math.Min(a.Length, b.Length);
        for (int i = 0; i < len; i++)
        {
            if (char.ToLowerInvariant(a[i]) != char.ToLowerInvariant(b[i]))
                return i;
        }
        return len;
    }

    private HashSet<string> ExtractDefinitionKeywords(WordDto word, bool isVietnamese)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (word.Meanings == null) return keywords;

        var stopWords = isVietnamese ? StopWordsVi : StopWordsEn;

        foreach (var m in word.Meanings)
        {
            var text = isVietnamese ? m.DefinitionVi : m.DefinitionEn;
            if (string.IsNullOrWhiteSpace(text)) continue;

            var norm = _searchService.NormalizeSearchText(text);
            var tokens = _searchService.Tokenize(norm);

            foreach (var tok in tokens)
            {
                if (tok.Length >= 3 && !stopWords.Contains(tok))
                {
                    keywords.Add(tok);
                }
            }
        }

        return keywords;
    }

    private (HashSet<string> Synonyms, HashSet<string> Antonyms) ExtractExplicitRelationsFromNotes(WordDto word)
    {
        var syns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var text = (word.Notes ?? string.Empty) + " " + string.Join(" ", word.Meanings?.Select(m => m.Explanation ?? string.Empty) ?? Array.Empty<string>());
        if (string.IsNullOrWhiteSpace(text)) return (syns, ants);

        // Pattern: syn: word1, word2 or đồng nghĩa: word1
        var synMatch = Regex.Match(text, @"(?:syn(?:onym)?|đồng nghĩa)\s*:\s*([^.;\n]+)", RegexOptions.IgnoreCase);
        if (synMatch.Success)
        {
            var parts = synMatch.Groups[1].Value.Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var clean = p.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(clean)) syns.Add(clean);
            }
        }

        // Pattern: ant: / opp: / trái nghĩa:
        var antMatch = Regex.Match(text, @"(?:ant(?:onym)?|opp(?:osite)?|trái nghĩa)\s*:\s*([^.;\n]+)", RegexOptions.IgnoreCase);
        if (antMatch.Success)
        {
            var parts = antMatch.Groups[1].Value.Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var clean = p.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(clean)) ants.Add(clean);
            }
        }

        return (syns, ants);
    }
}
