using System.Text.RegularExpressions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services;

public class GrammarSuggestionService : IGrammarSuggestionService
{
    private readonly IGrammarService _grammarService;
    private readonly IVocabularyService? _vocabService;
    private readonly ISmartSearchService _searchService;

    // Standard confusing grammar pairs (canonical normalized keys)
    private static readonly List<(string Key1, string Key2, string ReasonVi, string ReasonEn)> ConfusedPairs = new()
    {
        ("present perfect", "past simple", "Phân biệt thì Hiện tại hoàn thành (kết quả ở hiện tại) và Quá khứ đơn (mốc thời gian xác định)", "Differentiate Present Perfect vs Past Simple"),
        ("past simple", "past continuous", "Phân biệt hành động đang xảy ra (Quá khứ tiếp diễn) thì hành động khác xen vào (Quá khứ đơn)", "Interrupted actions vs ongoing past action"),
        ("although", "despite", "Phân biệt Although (+ Mệnh đề) vs Despite / In spite of (+ Cụm danh từ / V-ing)", "Although (+ Clause) vs Despite / In spite of (+ Noun/V-ing)"),
        ("even though", "in spite of", "Phân biệt Even though (+ Mệnh đề) vs In spite of (+ Danh từ / V-ing)", "Even though (+ Clause) vs In spite of (+ Noun/V-ing)"),
        ("because", "because of", "Phân biệt Because (+ Mệnh đề) vs Because of / Due to (+ Cụm danh từ / V-ing)", "Because (+ Clause) vs Because of / Due to (+ Noun/V-ing)"),
        ("used to", "be used to", "Phân biệt Used to + V (thói quen quá khứ) vs Be / Get used to + V-ing (quen với việc gì ở hiện tại)", "Used to + V vs Be / Get used to + V-ing"),
        ("used to", "get used to", "Phân biệt Used to + V (thói quen quá khứ) vs Get used to + V-ing (dần quen với)", "Used to + V vs Get used to + V-ing"),
        ("will", "be going to", "Phân biệt Will (quyết định tức thời / dự đoán chủ quan) vs Be going to (kế hoạch sẵn / dấu hiệu rõ)", "Instant decision / prediction vs planned action / present evidence"),
        ("so that", "such that", "Phân biệt So + Adj/Adv + that vs Such + (a/an) + Adj + Noun + that", "So ... that vs Such ... that structure"),
        ("gerund", "infinitive", "Phân biệt động từ đi kèm V-ing (Gerund) vs To-V (Infinitive)", "Verbs followed by Gerund vs Infinitive"),
        ("wish", "hope", "Phân biệt Wish (ước điều không có thật) vs Hope (hy vọng điều có thể xảy ra)", "Wish (unreal/hypothetical) vs Hope (real/possible)"),
        ("first conditional", "second conditional", "Phân biệt Điều kiện loại 1 (có thể xảy ra) vs Loại 2 (trái thực tế hiện tại)", "First Conditional (possible) vs Second Conditional (unreal present)"),
        ("second conditional", "third conditional", "Phân biệt Điều kiện loại 2 (hiện tại) vs Loại 3 (trái thực tế quá khứ)", "Second Conditional (present unreal) vs Third Conditional (past unreal)"),
        ("each", "every", "Phân biệt Each (từng cá thể riêng rẽ) vs Every (toàn bộ thành viên như một nhóm)", "Each (individual members) vs Every (group as whole)"),
        ("another", "other", "Phân biệt Another (+ Danh từ số ít) vs Other (+ Danh từ số nhiều / không đếm được)", "Another (+ singular) vs Other (+ plural/uncountable)")
    };

    // Keyword clusters for structural similarity
    private static readonly Dictionary<string, string[]> StructuralClusters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Conditionals"] = new[] { "if", "unless", "provided", "as long as", "suppose", "condition", "dieu kien" },
        ["Concession"] = new[] { "although", "though", "even though", "despite", "in spite of", "nevertheless", "nhuong bo" },
        ["Reason"] = new[] { "because", "since", "as", "due to", "owing to", "because of", "nguyen nhan" },
        ["Result"] = new[] { "so", "therefore", "thus", "consequently", "as a result", "ket qua" },
        ["Passive"] = new[] { "passive", "be + v3", "get + v3", "bi dong" },
        ["Inversion"] = new[] { "inversion", "not only", "hardly", "scarcely", "seldom", "never", "dao ngu" },
        ["Relative Clauses"] = new[] { "who", "whom", "which", "that", "whose", "where", "when", "menh de quan he" },
        ["Reported Speech"] = new[] { "reported", "indirect", "tell", "say", "ask", "gian tiep" },
        ["Modals"] = new[] { "modal", "can", "could", "must", "should", "may", "might", "ought", "dong tu khuyet thieu" },
        ["Comparisons"] = new[] { "comparative", "superlative", "so sanh", "as ... as", "more", "most" }
    };

    public GrammarSuggestionService(
        IGrammarService? grammarService = null,
        ISmartSearchService? searchService = null,
        IVocabularyService? vocabService = null)
    {
        _grammarService = grammarService!;
        _searchService = searchService ?? new SmartSearchService(new FuzzySearchService());
        _vocabService = vocabService;
    }

    public async Task<GrammarRelatedSuggestionsDto> GetSuggestionsAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var targetRule = await _grammarService.GetRuleByIdAsync(ruleId, cancellationToken);
        if (targetRule == null)
        {
            return new GrammarRelatedSuggestionsDto(
                ruleId,
                string.Empty,
                Array.Empty<GrammarSuggestionItemDto>(),
                Array.Empty<GrammarSuggestionItemDto>(),
                Array.Empty<GrammarSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>());
        }

        var allRules = await _grammarService.GetAllRulesAsync(cancellationToken);
        IReadOnlyList<WordDto>? words = null;
        if (_vocabService != null)
        {
            try
            {
                words = await _vocabService.SearchWordsAsync(pageSize: 150, cancellationToken: cancellationToken);
            }
            catch
            {
                words = null;
            }
        }

        return GetSuggestions(targetRule, allRules, words);
    }

    public GrammarRelatedSuggestionsDto GetSuggestions(
        GrammarRuleDto targetRule,
        IEnumerable<GrammarRuleDto> grammarPool,
        IEnumerable<WordDto>? vocabularyPool = null)
    {
        if (targetRule == null || grammarPool == null)
        {
            return new GrammarRelatedSuggestionsDto(
                targetRule?.Id ?? Guid.Empty,
                targetRule?.Title ?? string.Empty,
                Array.Empty<GrammarSuggestionItemDto>(),
                Array.Empty<GrammarSuggestionItemDto>(),
                Array.Empty<GrammarSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>());
        }

        var poolList = grammarPool.Where(r => r.Id != targetRule.Id).ToList();

        var similar = GetSimilarStructures(targetRule, poolList);
        var confused = GetConfusedGrammar(targetRule, poolList);
        var sameTopic = GetSameTopicRules(targetRule, poolList);
        var relatedVocab = vocabularyPool != null
            ? GetRelatedVocabulary(targetRule, vocabularyPool)
            : Array.Empty<WordSuggestionItemDto>();

        return new GrammarRelatedSuggestionsDto(
            targetRule.Id,
            targetRule.Title,
            similar,
            confused,
            sameTopic,
            relatedVocab);
    }

    public IReadOnlyList<GrammarSuggestionItemDto> GetSimilarStructures(GrammarRuleDto targetRule, IEnumerable<GrammarRuleDto> pool)
    {
        var results = new List<GrammarSuggestionItemDto>();
        var normTargetTitle = _searchService.NormalizeSearchText(targetRule.Title);
        var normTargetStruct = _searchService.NormalizeSearchText(targetRule.Structure);
        var combinedTarget = normTargetTitle + " " + normTargetStruct + " " + _searchService.NormalizeSearchText(targetRule.Topic ?? string.Empty);

        // Find active clusters for target
        var targetClusters = new List<string>();
        foreach (var (clusterName, keywords) in StructuralClusters)
        {
            if (keywords.Any(k => combinedTarget.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                targetClusters.Add(clusterName);
            }
        }

        foreach (var candidate in pool)
        {
            if (candidate.Id == targetRule.Id) continue;
            var normCandTitle = _searchService.NormalizeSearchText(candidate.Title);
            var normCandStruct = _searchService.NormalizeSearchText(candidate.Structure);
            var combinedCand = normCandTitle + " " + normCandStruct + " " + _searchService.NormalizeSearchText(candidate.Topic ?? string.Empty);

            // 1. Cluster match
            var candClusters = targetClusters.Where(c => StructuralClusters[c].Any(k => combinedCand.Contains(k, StringComparison.OrdinalIgnoreCase))).ToList();
            if (candClusters.Count > 0)
            {
                double score = 0.72 + (candClusters.Count * 0.08);
                results.Add(new GrammarSuggestionItemDto(
                    candidate,
                    GrammarSuggestionCategory.SimilarStructure,
                    Math.Min(0.92, score),
                    $"Cùng nhóm cấu trúc: {string.Join(", ", candClusters)}"));
                continue;
            }

            // 2. Direct structural pattern overlap
            var targetTokens = _searchService.Tokenize(normTargetStruct).Where(t => t.Length >= 2).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candTokens = _searchService.Tokenize(normCandStruct).Where(t => t.Length >= 2).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var commonTokens = targetTokens.Intersect(candTokens, StringComparer.OrdinalIgnoreCase).ToList();

            if (commonTokens.Count >= 3)
            {
                results.Add(new GrammarSuggestionItemDto(
                    candidate,
                    GrammarSuggestionCategory.SimilarStructure,
                    Math.Min(0.85, 0.60 + (commonTokens.Count * 0.06)),
                    $"Tương đồng mẫu cấu trúc ({string.Join(", ", commonTokens.Take(3))})"));
            }
        }

        return results.OrderByDescending(r => r.RelevanceScore).Take(8).ToList();
    }

    public IReadOnlyList<GrammarSuggestionItemDto> GetConfusedGrammar(GrammarRuleDto targetRule, IEnumerable<GrammarRuleDto> pool)
    {
        var results = new List<GrammarSuggestionItemDto>();
        var normTargetTitle = _searchService.NormalizeSearchText(targetRule.Title);
        var normMistakes = _searchService.NormalizeSearchText(targetRule.CommonMistakes ?? string.Empty);
        var normExplanation = _searchService.NormalizeSearchText(targetRule.ExplanationVi ?? string.Empty);

        foreach (var candidate in pool)
        {
            if (candidate.Id == targetRule.Id) continue;
            var normCandTitle = _searchService.NormalizeSearchText(candidate.Title);
            var normCandMistakes = _searchService.NormalizeSearchText(candidate.CommonMistakes ?? string.Empty);

            // 1. Check known confusion pairs
            foreach (var pair in ConfusedPairs)
            {
                var normKey1 = _searchService.NormalizeSearchText(pair.Key1);
                var normKey2 = _searchService.NormalizeSearchText(pair.Key2);

                bool matchA = (normTargetTitle.Contains(normKey1, StringComparison.OrdinalIgnoreCase) || normTargetTitle.Contains(normKey2, StringComparison.OrdinalIgnoreCase)) &&
                              (normCandTitle.Contains(normKey1, StringComparison.OrdinalIgnoreCase) || normCandTitle.Contains(normKey2, StringComparison.OrdinalIgnoreCase));

                if (matchA)
                {
                    results.Add(new GrammarSuggestionItemDto(
                        candidate,
                        GrammarSuggestionCategory.ConfusedGrammar,
                        0.95,
                        pair.ReasonVi));
                    break;
                }
            }

            if (results.Any(r => r.Rule.Id == candidate.Id)) continue;

            // 2. Cross-reference in Common Mistakes or Explanation
            if (!string.IsNullOrEmpty(normMistakes) && normMistakes.Contains(normCandTitle, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new GrammarSuggestionItemDto(
                    candidate,
                    GrammarSuggestionCategory.ConfusedGrammar,
                    0.88,
                    $"Được nhắc tới trong phần Lỗi thường gặp của '{targetRule.Title}'"));
                continue;
            }

            if (!string.IsNullOrEmpty(normCandMistakes) && normCandMistakes.Contains(normTargetTitle, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new GrammarSuggestionItemDto(
                    candidate,
                    GrammarSuggestionCategory.ConfusedGrammar,
                    0.88,
                    $"Có cảnh báo nhầm lẫn với '{targetRule.Title}'"));
                continue;
            }

            // 3. Keyword cues like "nhầm với", "tránh nhầm", "vs"
            if (normMistakes.Contains("nham voi") || normMistakes.Contains("so sanh voi") || normMistakes.Contains("distinguish"))
            {
                var candKeywords = _searchService.Tokenize(normCandTitle).Where(t => t.Length >= 4);
                if (candKeywords.Any(k => normMistakes.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    results.Add(new GrammarSuggestionItemDto(
                        candidate,
                        GrammarSuggestionCategory.ConfusedGrammar,
                        0.80,
                        $"Điểm ngữ pháp dễ nhầm lẫn được ghi nhận"));
                }
            }
        }

        return results.OrderByDescending(r => r.RelevanceScore).Take(6).ToList();
    }

    public IReadOnlyList<GrammarSuggestionItemDto> GetSameTopicRules(GrammarRuleDto targetRule, IEnumerable<GrammarRuleDto> pool)
    {
        var results = new List<GrammarSuggestionItemDto>();
        var targetTopic = targetRule.Topic?.Trim();

        if (string.IsNullOrWhiteSpace(targetTopic))
            return Array.Empty<GrammarSuggestionItemDto>();

        var normTargetTopic = _searchService.NormalizeSearchText(targetTopic);

        foreach (var candidate in pool)
        {
            if (candidate.Id == targetRule.Id) continue;
            var candTopic = candidate.Topic?.Trim();
            if (string.IsNullOrWhiteSpace(candTopic)) continue;

            var normCandTopic = _searchService.NormalizeSearchText(candTopic);
            if (string.Equals(normCandTopic, normTargetTopic, StringComparison.OrdinalIgnoreCase))
            {
                double score = 0.65;
                if (candidate.Level == targetRule.Level && candidate.Level != CefrLevel.None)
                    score += 0.08;
                if (candidate.ExamTarget == targetRule.ExamTarget && candidate.ExamTarget != ExamType.General)
                    score += 0.05;

                results.Add(new GrammarSuggestionItemDto(
                    candidate,
                    GrammarSuggestionCategory.SameTopic,
                    Math.Min(0.82, score),
                    $"Cùng chủ đề ngữ pháp: {targetTopic}"));
            }
        }

        return results.OrderByDescending(r => r.RelevanceScore).Take(8).ToList();
    }

    public IReadOnlyList<WordSuggestionItemDto> GetRelatedVocabulary(GrammarRuleDto targetRule, IEnumerable<WordDto> vocabularyPool)
    {
        var results = new List<WordSuggestionItemDto>();
        var normTitle = _searchService.NormalizeSearchText(targetRule.Title);
        var normStruct = _searchService.NormalizeSearchText(targetRule.Structure);
        var normTopic = _searchService.NormalizeSearchText(targetRule.Topic ?? string.Empty);

        // Extract signal words from structure, examples and title
        var signalWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Check connectors
        var structTokens = _searchService.Tokenize(normStruct);
        foreach (var tok in structTokens)
        {
            if (tok.Length >= 3 && !tok.Equals("and", StringComparison.OrdinalIgnoreCase) && !tok.Equals("the", StringComparison.OrdinalIgnoreCase))
            {
                signalWords.Add(tok);
            }
        }

        if (targetRule.Examples != null)
        {
            foreach (var ex in targetRule.Examples.Take(3))
            {
                var exTokens = _searchService.Tokenize(_searchService.NormalizeSearchText(ex.SentenceEn));
                foreach (var tok in exTokens)
                {
                    if (tok.Length >= 4 && signalWords.Count < 20)
                    {
                        signalWords.Add(tok);
                    }
                }
            }
        }

        foreach (var word in vocabularyPool)
        {
            var normTerm = _searchService.NormalizeSearchText(word.Term);

            // 1. Direct match with signal words in grammar
            if (signalWords.Contains(normTerm))
            {
                results.Add(new WordSuggestionItemDto(
                    word,
                    SuggestionCategory.Collocation,
                    0.85,
                    $"Từ khóa ngữ pháp xuất hiện trong cấu trúc/ví dụ của '{targetRule.Title}'"));
                continue;
            }

            // 2. Match with Topic
            if (!string.IsNullOrEmpty(normTopic) && word.Topics != null)
            {
                if (word.Topics.Any(t => _searchService.NormalizeSearchText(t).Contains(normTopic, StringComparison.OrdinalIgnoreCase) ||
                                         normTopic.Contains(_searchService.NormalizeSearchText(t), StringComparison.OrdinalIgnoreCase)))
                {
                    results.Add(new WordSuggestionItemDto(
                        word,
                        SuggestionCategory.SameTopic,
                        0.65,
                        $"Từ vựng cùng chủ đề '{targetRule.Topic}'"));
                }
            }
        }

        return results.OrderByDescending(r => r.RelevanceScore).Take(10).ToList();
    }
}
