using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;

namespace Lexora.Application.Services;

public class SmartSearchService : ISmartSearchService
{
    private readonly IFuzzySearchService _fuzzyService;

    public SmartSearchService(IFuzzySearchService? fuzzyService = null)
    {
        _fuzzyService = fuzzyService ?? new FuzzySearchService();
    }

    public SmartSearchMatchResult<WordDto>? ScoreWord(string query, WordDto word)
    {
        if (string.IsNullOrWhiteSpace(query) || word == null) return null;

        var cleanQuery = query.Trim();
        var normQuery = NormalizeSearchText(cleanQuery);
        if (string.IsNullOrEmpty(normQuery)) return null;

        var term = word.Term ?? string.Empty;
        var normTerm = NormalizeSearchText(term);

        // 1. Exact match on Term
        if (string.Equals(term, cleanQuery, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normTerm, normQuery, StringComparison.OrdinalIgnoreCase))
        {
            return new SmartSearchMatchResult<WordDto>(
                word,
                100.0,
                "Exact",
                "Term",
                term);
        }

        // 2. Prefix match on Term
        if (normTerm.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
        {
            int diff = Math.Max(0, normTerm.Length - normQuery.Length);
            double score = Math.Max(85.0, 95.0 - (diff * 0.6));
            return new SmartSearchMatchResult<WordDto>(
                word,
                score,
                "Prefix",
                "Term",
                term);
        }

        // 3. Word-boundary / phrase match in Term
        var queryTokens = Tokenize(cleanQuery);
        var normTokens = queryTokens.Select(NormalizeSearchText).Where(t => !string.IsNullOrEmpty(t)).ToList();

        if (IsWordBoundaryMatch(normTerm, normQuery))
        {
            return new SmartSearchMatchResult<WordDto>(
                word,
                82.0,
                "Phrase",
                "Term",
                term);
        }

        if (normTerm.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
        {
            return new SmartSearchMatchResult<WordDto>(
                word,
                75.0,
                "Partial",
                "Term",
                term);
        }

        // 4. Matches in Meanings / Definitions
        double bestDefinitionScore = 0.0;
        string? matchedDefField = null;
        string? matchedDefSnippet = null;

        if (word.Meanings != null)
        {
            foreach (var m in word.Meanings)
            {
                var defVi = m.DefinitionVi ?? string.Empty;
                var normDefVi = NormalizeSearchText(defVi);
                var defEn = m.DefinitionEn ?? string.Empty;
                var normDefEn = NormalizeSearchText(defEn);
                var expl = m.Explanation ?? string.Empty;
                var normExpl = NormalizeSearchText(expl);

                // Exact phrase in definition
                if (normDefVi.Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                    normDefEn.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
                {
                    double score = 68.0;
                    if (IsWordBoundaryMatch(normDefVi, normQuery) || IsWordBoundaryMatch(normDefEn, normQuery))
                        score = 72.0;

                    if (score > bestDefinitionScore)
                    {
                        bestDefinitionScore = score;
                        matchedDefField = "Definition";
                        matchedDefSnippet = !string.IsNullOrWhiteSpace(defVi) ? defVi : defEn;
                    }
                }
                else if (!string.IsNullOrEmpty(normExpl) && normExpl.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
                {
                    if (58.0 > bestDefinitionScore)
                    {
                        bestDefinitionScore = 58.0;
                        matchedDefField = "Explanation";
                        matchedDefSnippet = expl;
                    }
                }
            }
        }

        // 5. Multi-token match across all fields (Term + Meanings + Topics + ExamTags)
        if (normTokens.Count > 1)
        {
            var combinedTarget = BuildCombinedWordSearchTarget(word);
            int matchedTokensCount = normTokens.Count(t => combinedTarget.Contains(t, StringComparison.OrdinalIgnoreCase));
            if (matchedTokensCount == normTokens.Count)
            {
                double multiScore = 65.0 + (normTokens.Count * 2.0);
                if (multiScore > bestDefinitionScore)
                {
                    return new SmartSearchMatchResult<WordDto>(
                        word,
                        Math.Min(80.0, multiScore),
                        "Keyword",
                        "MultiField",
                        word.PrimaryDefinition);
                }
            }
            else if (matchedTokensCount > 0 && matchedTokensCount >= (normTokens.Count / 2.0))
            {
                double partialTokenScore = 40.0 + (matchedTokensCount * 6.0);
                if (partialTokenScore > bestDefinitionScore)
                {
                    bestDefinitionScore = partialTokenScore;
                    matchedDefField = "MultiFieldPartial";
                    matchedDefSnippet = word.PrimaryDefinition;
                }
            }
        }

        if (bestDefinitionScore > 0)
        {
            return new SmartSearchMatchResult<WordDto>(
                word,
                bestDefinitionScore,
                "Keyword",
                matchedDefField,
                matchedDefSnippet);
        }

        // 6. Topic or Exam Tag match
        if (word.Topics != null && word.Topics.Any(t => NormalizeSearchText(t).Contains(normQuery, StringComparison.OrdinalIgnoreCase)))
        {
            var matchedTopic = word.Topics.First(t => NormalizeSearchText(t).Contains(normQuery, StringComparison.OrdinalIgnoreCase));
            return new SmartSearchMatchResult<WordDto>(word, 52.0, "Keyword", "Topic", matchedTopic);
        }

        if (word.ExamTags != null && word.ExamTags.Any(t => NormalizeSearchText(t).Contains(normQuery, StringComparison.OrdinalIgnoreCase)))
        {
            var matchedTag = word.ExamTags.First(t => NormalizeSearchText(t).Contains(normQuery, StringComparison.OrdinalIgnoreCase));
            return new SmartSearchMatchResult<WordDto>(word, 50.0, "Keyword", "ExamTag", matchedTag);
        }

        // 7. Examples or Notes match
        if (word.Meanings != null)
        {
            foreach (var m in word.Meanings)
            {
                if (m.Examples == null) continue;
                foreach (var ex in m.Examples)
                {
                    var normExEn = NormalizeSearchText(ex.SentenceEn ?? string.Empty);
                    var normExVi = NormalizeSearchText(ex.SentenceVi ?? string.Empty);
                    if (normExEn.Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                        normExVi.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
                    {
                        return new SmartSearchMatchResult<WordDto>(
                            word,
                            45.0,
                            "Partial",
                            "Example",
                            ex.SentenceEn);
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(word.Notes))
        {
            var normNotes = NormalizeSearchText(word.Notes);
            if (normNotes.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
            {
                return new SmartSearchMatchResult<WordDto>(
                    word,
                    40.0,
                    "Partial",
                    "Notes",
                    word.Notes);
            }
        }

        // 8. Fuzzy match on Term
        if (cleanQuery.Length >= 3 && term.Length >= 3)
        {
            double sim = _fuzzyService.CalculateSimilarity(normQuery, normTerm);
            int dist = _fuzzyService.CalculateDistance(normQuery, normTerm);

            if (sim >= 0.65 || (cleanQuery.Length <= 4 && dist <= 1))
            {
                double score = Math.Max(25.0, sim * 58.0);
                return new SmartSearchMatchResult<WordDto>(
                    word,
                    score,
                    "Fuzzy",
                    "Term",
                    term);
            }
        }

        return null;
    }

    public SmartSearchMatchResult<GrammarRuleDto>? ScoreGrammar(string query, GrammarRuleDto rule)
    {
        if (string.IsNullOrWhiteSpace(query) || rule == null) return null;

        var cleanQuery = query.Trim();
        var normQuery = NormalizeSearchText(cleanQuery);
        if (string.IsNullOrEmpty(normQuery)) return null;

        var title = rule.Title ?? string.Empty;
        var normTitle = NormalizeSearchText(title);

        // 1. Exact match on Title
        if (string.Equals(title, cleanQuery, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normTitle, normQuery, StringComparison.OrdinalIgnoreCase))
        {
            return new SmartSearchMatchResult<GrammarRuleDto>(
                rule,
                100.0,
                "Exact",
                "Title",
                title);
        }

        // 2. Prefix match on Title
        if (normTitle.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
        {
            int diff = Math.Max(0, normTitle.Length - normQuery.Length);
            double score = Math.Max(85.0, 95.0 - (diff * 0.5));
            return new SmartSearchMatchResult<GrammarRuleDto>(
                rule,
                score,
                "Prefix",
                "Title",
                title);
        }

        // 3. Word-boundary or phrase in Title
        var queryTokens = Tokenize(cleanQuery);
        var normTokens = queryTokens.Select(NormalizeSearchText).Where(t => !string.IsNullOrEmpty(t)).ToList();

        if (IsWordBoundaryMatch(normTitle, normQuery))
        {
            return new SmartSearchMatchResult<GrammarRuleDto>(
                rule,
                82.0,
                "Phrase",
                "Title",
                title);
        }

        if (normTitle.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
        {
            return new SmartSearchMatchResult<GrammarRuleDto>(
                rule,
                75.0,
                "Partial",
                "Title",
                title);
        }

        // 4. Structure match (e.g. searching "If + S + V" or "S + had + V3")
        var structure = rule.Structure ?? string.Empty;
        var normStructure = NormalizeSearchText(structure);
        if (!string.IsNullOrEmpty(normStructure) && normStructure.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
        {
            return new SmartSearchMatchResult<GrammarRuleDto>(
                rule,
                70.0,
                "Structure",
                "Structure",
                structure);
        }

        // 5. Explanation match
        var explVi = rule.ExplanationVi ?? string.Empty;
        var normExplVi = NormalizeSearchText(explVi);
        var explEn = rule.ExplanationEn ?? string.Empty;
        var normExplEn = NormalizeSearchText(explEn);

        if (normExplVi.Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
            normExplEn.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
        {
            double score = 65.0;
            if (IsWordBoundaryMatch(normExplVi, normQuery) || IsWordBoundaryMatch(normExplEn, normQuery))
                score = 68.0;

            return new SmartSearchMatchResult<GrammarRuleDto>(
                rule,
                score,
                "Explanation",
                "Explanation",
                !string.IsNullOrWhiteSpace(explVi) ? explVi : explEn);
        }

        // 6. Common Mistakes match
        if (!string.IsNullOrWhiteSpace(rule.CommonMistakes))
        {
            var normMistakes = NormalizeSearchText(rule.CommonMistakes);
            if (normMistakes.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
            {
                return new SmartSearchMatchResult<GrammarRuleDto>(
                    rule,
                    55.0,
                    "Mistakes",
                    "CommonMistakes",
                    rule.CommonMistakes);
            }
        }

        // 7. Multi-token match across all fields
        if (normTokens.Count > 1)
        {
            var combinedTarget = BuildCombinedGrammarSearchTarget(rule);
            int matchedTokensCount = normTokens.Count(t => combinedTarget.Contains(t, StringComparison.OrdinalIgnoreCase));
            if (matchedTokensCount == normTokens.Count)
            {
                return new SmartSearchMatchResult<GrammarRuleDto>(
                    rule,
                    66.0,
                    "Keyword",
                    "MultiField",
                    rule.Title);
            }
            else if (matchedTokensCount > 0 && matchedTokensCount >= (normTokens.Count / 2.0))
            {
                return new SmartSearchMatchResult<GrammarRuleDto>(
                    rule,
                    45.0,
                    "Keyword",
                    "MultiFieldPartial",
                    rule.Title);
            }
        }

        // 8. Topic match
        if (!string.IsNullOrWhiteSpace(rule.Topic))
        {
            var normTopic = NormalizeSearchText(rule.Topic);
            if (normTopic.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
            {
                return new SmartSearchMatchResult<GrammarRuleDto>(
                    rule,
                    50.0,
                    "Keyword",
                    "Topic",
                    rule.Topic);
            }
        }

        // 9. Examples match
        if (rule.Examples != null)
        {
            foreach (var ex in rule.Examples)
            {
                var normEn = NormalizeSearchText(ex.SentenceEn ?? string.Empty);
                var normVi = NormalizeSearchText(ex.SentenceVi ?? string.Empty);
                if (normEn.Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                    normVi.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return new SmartSearchMatchResult<GrammarRuleDto>(
                        rule,
                        46.0,
                        "Partial",
                        "Example",
                        ex.SentenceEn);
                }
            }
        }

        // 10. Fuzzy match on Title
        if (cleanQuery.Length >= 4 && title.Length >= 4)
        {
            double sim = _fuzzyService.CalculateSimilarity(normQuery, normTitle);
            int dist = _fuzzyService.CalculateDistance(normQuery, normTitle);

            if (sim >= 0.65 || dist <= 2)
            {
                double score = Math.Max(25.0, sim * 56.0);
                return new SmartSearchMatchResult<GrammarRuleDto>(
                    rule,
                    score,
                    "Fuzzy",
                    "Title",
                    title);
            }
        }

        return null;
    }

    public IReadOnlyList<SmartSearchMatchResult<WordDto>> RankWords(
        string query,
        IEnumerable<WordDto> candidates,
        int take = 50)
    {
        if (string.IsNullOrWhiteSpace(query) || candidates == null)
            return Array.Empty<SmartSearchMatchResult<WordDto>>();

        var results = new List<SmartSearchMatchResult<WordDto>>();
        foreach (var w in candidates)
        {
            var match = ScoreWord(query, w);
            if (match != null)
            {
                results.Add(match);
            }
        }

        return results
            .OrderByDescending(r => r.RelevanceScore)
            .ThenBy(r => r.Item.Term.Length)
            .Take(take)
            .ToList();
    }

    public IReadOnlyList<SmartSearchMatchResult<GrammarRuleDto>> RankGrammar(
        string query,
        IEnumerable<GrammarRuleDto> candidates,
        int take = 50)
    {
        if (string.IsNullOrWhiteSpace(query) || candidates == null)
            return Array.Empty<SmartSearchMatchResult<GrammarRuleDto>>();

        var results = new List<SmartSearchMatchResult<GrammarRuleDto>>();
        foreach (var gr in candidates)
        {
            var match = ScoreGrammar(query, gr);
            if (match != null)
            {
                results.Add(match);
            }
        }

        return results
            .OrderByDescending(r => r.RelevanceScore)
            .ThenBy(r => r.Item.Title.Length)
            .Take(take)
            .ToList();
    }

    public IReadOnlyList<string> FindClosestWordSuggestions(
        string query,
        IEnumerable<string> candidates,
        int maxSuggestions = 5,
        double minSimilarity = 0.55)
    {
        return FindClosestSuggestionsInternal(query, candidates, maxSuggestions, minSimilarity);
    }

    public IReadOnlyList<string> FindClosestWordSuggestions(
        string query,
        IEnumerable<WordDto> candidates,
        int maxSuggestions = 5,
        double minSimilarity = 0.55)
    {
        if (string.IsNullOrWhiteSpace(query) || candidates == null)
            return Array.Empty<string>();

        var cleanQuery = query.Trim();
        var normQuery = NormalizeSearchText(cleanQuery);
        if (normQuery.Length < 2) return Array.Empty<string>();

        var scored = new List<(string Term, double Similarity, int Distance)>();

        foreach (var word in candidates)
        {
            if (string.IsNullOrWhiteSpace(word.Term)) continue;
            var term = word.Term.Trim();
            var normTerm = NormalizeSearchText(term);

            // Exact match shouldn't be suggested as a typo correction
            if (string.Equals(normTerm, normQuery, StringComparison.OrdinalIgnoreCase))
                continue;

            // Score against Term
            var termMatches = FindClosestSuggestionsInternal(cleanQuery, new[] { term }, 1, minSimilarity);
            double bestSim = 0.0;
            int bestDist = int.MaxValue;

            if (termMatches.Count > 0)
            {
                bestDist = _fuzzyService.CalculateDistance(normQuery, normTerm);
                bestSim = _fuzzyService.CalculateSimilarity(normQuery, normTerm);
                if (normTerm.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
                    bestSim = Math.Max(bestSim, 0.90);
                if (string.Equals(CollapseConsecutiveDuplicates(normQuery), CollapseConsecutiveDuplicates(normTerm), StringComparison.OrdinalIgnoreCase))
                    bestSim = Math.Max(bestSim, 0.92);
            }

            // Also check Vietnamese definitions if query might be Vietnamese
            if (word.Meanings != null)
            {
                foreach (var m in word.Meanings)
                {
                    if (!string.IsNullOrWhiteSpace(m.DefinitionVi))
                    {
                        var normDefVi = NormalizeSearchText(m.DefinitionVi);
                        if (normDefVi.Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                            normDefVi.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
                        {
                            double defScore = 0.85;
                            if (defScore > bestSim)
                            {
                                bestSim = defScore;
                                bestDist = 1;
                            }
                        }
                    }
                }
            }

            if (bestSim >= minSimilarity || (cleanQuery.Length <= 4 && bestDist <= 1))
            {
                scored.Add((term, bestSim, bestDist));
            }
        }

        return scored
            .OrderByDescending(s => s.Similarity)
            .ThenBy(s => s.Distance)
            .ThenBy(s => Math.Abs(s.Term.Length - cleanQuery.Length))
            .Select(s => s.Term)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxSuggestions)
            .ToList();
    }

    public IReadOnlyList<string> FindClosestGrammarSuggestions(
        string query,
        IEnumerable<string> candidates,
        int maxSuggestions = 5,
        double minSimilarity = 0.55)
    {
        return FindClosestSuggestionsInternal(query, candidates, maxSuggestions, minSimilarity);
    }

    public IReadOnlyList<string> FindClosestGrammarRuleSuggestions(
        string query,
        IEnumerable<GrammarRuleDto> candidates,
        int maxSuggestions = 5,
        double minSimilarity = 0.55)
    {
        if (string.IsNullOrWhiteSpace(query) || candidates == null)
            return Array.Empty<string>();

        var cleanQuery = query.Trim();
        var normQuery = NormalizeSearchText(cleanQuery);
        if (normQuery.Length < 2) return Array.Empty<string>();

        var qCollapsed = CollapseConsecutiveDuplicates(normQuery);
        var qTokens = Tokenize(normQuery);

        var ruleScores = new List<(string Title, double Score, int Distance)>();

        foreach (var rule in candidates)
        {
            if (string.IsNullOrWhiteSpace(rule.Title)) continue;
            var title = rule.Title.Trim();
            var normTitle = NormalizeSearchText(title);

            // Exact match does not need suggestion
            if (string.Equals(normTitle, normQuery, StringComparison.OrdinalIgnoreCase))
                continue;

            double bestScore = 0.0;
            int bestDist = int.MaxValue;

            // 1. Direct match on Title (using phrase-aware search)
            var titleMatches = FindClosestSuggestionsInternal(cleanQuery, new[] { title }, 1, minSimilarity);
            if (titleMatches.Count > 0)
            {
                bestDist = _fuzzyService.CalculateDistance(normQuery, normTitle);
                bestScore = _fuzzyService.CalculateSimilarity(normQuery, normTitle);

                // Token level matching against title tokens
                var titleTokens = Tokenize(normTitle);
                foreach (var tt in titleTokens)
                {
                    var normTt = NormalizeSearchText(tt);
                    double ttSim = _fuzzyService.CalculateSimilarity(normQuery, normTt);
                    int ttDist = _fuzzyService.CalculateDistance(normQuery, normTt);

                    if (normTt.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
                        ttSim = Math.Max(ttSim, 0.90);
                    if (string.Equals(qCollapsed, CollapseConsecutiveDuplicates(normTt), StringComparison.OrdinalIgnoreCase))
                        ttSim = Math.Max(ttSim, 0.92);

                    if (ttSim > bestScore)
                    {
                        bestScore = ttSim * 0.95;
                        bestDist = ttDist;
                    }
                }
            }

            // 2. Check Structure (e.g. If + S + V, have/has + V3)
            if (!string.IsNullOrWhiteSpace(rule.Structure))
            {
                var normStruct = NormalizeSearchText(rule.Structure);
                if (normStruct.Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                    normStruct.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
                {
                    double structScore = 0.82;
                    if (structScore > bestScore)
                    {
                        bestScore = structScore;
                        bestDist = 1;
                    }
                }
            }

            // 3. Check Topic (e.g. Conditionals, Tenses, Passive Voice)
            if (!string.IsNullOrWhiteSpace(rule.Topic))
            {
                var normTopic = NormalizeSearchText(rule.Topic);
                if (normTopic.Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                    normTopic.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
                {
                    double topicScore = 0.80;
                    if (topicScore > bestScore)
                    {
                        bestScore = topicScore;
                        bestDist = 1;
                    }
                }
                else
                {
                    double topSim = _fuzzyService.CalculateSimilarity(normQuery, normTopic);
                    if (topSim >= 0.70 && topSim * 0.88 > bestScore)
                    {
                        bestScore = topSim * 0.88;
                        bestDist = _fuzzyService.CalculateDistance(normQuery, normTopic);
                    }
                }
            }

            // 4. Check ExplanationVi (Vietnamese name / keywords e.g. "câu điều kiện", "hiện tại hoàn thành")
            if (!string.IsNullOrWhiteSpace(rule.ExplanationVi))
            {
                var normExplVi = NormalizeSearchText(rule.ExplanationVi);
                if (normExplVi.Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                    normExplVi.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
                {
                    double explScore = 0.84;
                    if (explScore > bestScore)
                    {
                        bestScore = explScore;
                        bestDist = 1;
                    }
                }
            }

            if (bestScore >= minSimilarity || (cleanQuery.Length <= 4 && bestDist <= 1))
            {
                ruleScores.Add((title, bestScore, bestDist));
            }
        }

        return ruleScores
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Distance)
            .ThenBy(r => Math.Abs(r.Title.Length - cleanQuery.Length))
            .Select(r => r.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxSuggestions)
            .ToList();
    }

    private IReadOnlyList<string> FindClosestSuggestionsInternal(
        string query,
        IEnumerable<string> candidates,
        int maxSuggestions,
        double minSimilarity)
    {
        if (string.IsNullOrWhiteSpace(query) || candidates == null)
            return Array.Empty<string>();

        var cleanQuery = query.Trim();
        var normQuery = NormalizeSearchText(cleanQuery);
        if (normQuery.Length < 2) return Array.Empty<string>();

        var qCollapsed = CollapseConsecutiveDuplicates(normQuery);
        var qTokens = Tokenize(normQuery);

        var matches = new List<(string Candidate, double Similarity, int Distance)>();

        foreach (var c in candidates)
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            var normC = NormalizeSearchText(c.Trim());
            if (string.Equals(normC, normQuery, StringComparison.OrdinalIgnoreCase))
                continue; // exact matches are not suggestions for missing results

            double sim = _fuzzyService.CalculateSimilarity(normQuery, normC);
            int dist = _fuzzyService.CalculateDistance(normQuery, normC);

            // Double-letter typo match (e.g., accomodate -> accommodate, embaras -> embarrass)
            var cCollapsed = CollapseConsecutiveDuplicates(normC);
            if (string.Equals(qCollapsed, cCollapsed, StringComparison.OrdinalIgnoreCase))
            {
                sim = Math.Max(sim, 0.92);
                dist = Math.Min(dist, 1);
            }

            // Boost prefix or partial overlap
            if (normC.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
            {
                double prefixScore = 0.90 - Math.Min(0.15, (normC.Length - normQuery.Length) * 0.015);
                sim = Math.Max(sim, prefixScore);
            }
            else if (normC.Contains(normQuery, StringComparison.OrdinalIgnoreCase))
            {
                sim = Math.Max(sim, 0.78);
            }

            // Multi-token candidate phrase matching (especially for Grammar titles and Phrasal verbs)
            var cTokens = Tokenize(normC);
            if (cTokens.Count > 1)
            {
                if (qTokens.Count == 1)
                {
                    double bestTokenSim = 0.0;
                    int minTokenDist = int.MaxValue;
                    foreach (var ct in cTokens)
                    {
                        var normCt = NormalizeSearchText(ct);
                        if (string.Equals(normCt, normQuery, StringComparison.OrdinalIgnoreCase))
                        {
                            bestTokenSim = 1.0;
                            minTokenDist = 0;
                            break;
                        }

                        double tSim = _fuzzyService.CalculateSimilarity(normQuery, normCt);
                        int tDist = _fuzzyService.CalculateDistance(normQuery, normCt);

                        if (normCt.StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
                            tSim = Math.Max(tSim, 0.88 - Math.Min(0.12, (normCt.Length - normQuery.Length) * 0.02));

                        if (string.Equals(qCollapsed, CollapseConsecutiveDuplicates(normCt), StringComparison.OrdinalIgnoreCase))
                        {
                            tSim = Math.Max(tSim, 0.90);
                            tDist = Math.Min(tDist, 1);
                        }

                        if (tSim > bestTokenSim)
                        {
                            bestTokenSim = tSim;
                            minTokenDist = tDist;
                        }
                    }

                    if (bestTokenSim >= minSimilarity || (cleanQuery.Length <= 4 && minTokenDist <= 1))
                    {
                        double phraseSim = bestTokenSim * 0.92;
                        if (phraseSim > sim)
                        {
                            sim = phraseSim;
                            dist = minTokenDist;
                        }
                    }
                }
                else if (qTokens.Count > 1)
                {
                    double totalTokenSim = 0.0;
                    int totalTokenDist = 0;
                    int matchedCount = 0;

                    foreach (var qt in qTokens)
                    {
                        var normQt = NormalizeSearchText(qt);
                        var qtCollapsed = CollapseConsecutiveDuplicates(normQt);
                        double bestT = 0.0;
                        int minD = int.MaxValue;

                        foreach (var ct in cTokens)
                        {
                            var normCt = NormalizeSearchText(ct);
                            if (string.Equals(normCt, normQt, StringComparison.OrdinalIgnoreCase))
                            {
                                bestT = 1.0;
                                minD = 0;
                                break;
                            }
                            double tSim = _fuzzyService.CalculateSimilarity(normQt, normCt);
                            int tDist = _fuzzyService.CalculateDistance(normQt, normCt);
                            if (normCt.StartsWith(normQt, StringComparison.OrdinalIgnoreCase))
                                tSim = Math.Max(tSim, 0.88);
                            if (string.Equals(qtCollapsed, CollapseConsecutiveDuplicates(normCt), StringComparison.OrdinalIgnoreCase))
                            {
                                tSim = Math.Max(tSim, 0.90);
                                tDist = Math.Min(tDist, 1);
                            }
                            if (tSim > bestT)
                            {
                                bestT = tSim;
                                minD = tDist;
                            }
                        }

                        if (bestT >= 0.50)
                        {
                            matchedCount++;
                            totalTokenSim += bestT;
                            totalTokenDist += minD;
                        }
                    }

                    if (matchedCount == qTokens.Count)
                    {
                        double avgSim = totalTokenSim / qTokens.Count;
                        if (avgSim > sim)
                        {
                            sim = avgSim;
                            dist = totalTokenDist;
                        }
                    }
                }
            }

            if (sim >= minSimilarity || (cleanQuery.Length <= 4 && dist <= 1))
            {
                matches.Add((c, sim, dist));
            }
        }

        return matches
            .OrderByDescending(m => m.Similarity)
            .ThenBy(m => m.Distance)
            .ThenBy(m => Math.Abs(m.Candidate.Length - cleanQuery.Length))
            .ThenBy(m => m.Candidate.Length)
            .Select(m => m.Candidate)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxSuggestions)
            .ToList();
    }

    private static string CollapseConsecutiveDuplicates(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 2) return text;
        var sb = new StringBuilder(text.Length);
        char prev = '\0';
        foreach (char c in text)
        {
            if (c != prev)
            {
                sb.Append(c);
                prev = c;
            }
        }
        return sb.ToString();
    }

    public string NormalizeSearchText(string text)
    {
        return Lexora.Domain.Common.SearchTextHelper.Normalize(text);
    }

    public IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();

        // Split by whitespace, punctuation except hyphens inside words
        var parts = Regex.Split(text.Trim(), @"[^\p{L}\p{N}\-]+");
        return parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
    }

    private static bool IsWordBoundaryMatch(string target, string query)
    {
        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(query)) return false;
        var pattern = $@"\b{Regex.Escape(query)}\b";
        return Regex.IsMatch(target, pattern, RegexOptions.IgnoreCase);
    }

    private string BuildCombinedWordSearchTarget(WordDto word)
    {
        var sb = new StringBuilder();
        sb.Append(NormalizeSearchText(word.Term)).Append(' ');
        if (word.Meanings != null)
        {
            foreach (var m in word.Meanings)
            {
                sb.Append(NormalizeSearchText(m.DefinitionVi)).Append(' ');
                if (!string.IsNullOrEmpty(m.DefinitionEn))
                    sb.Append(NormalizeSearchText(m.DefinitionEn)).Append(' ');
                if (!string.IsNullOrEmpty(m.Explanation))
                    sb.Append(NormalizeSearchText(m.Explanation)).Append(' ');
            }
        }
        if (word.Topics != null)
        {
            foreach (var t in word.Topics) sb.Append(NormalizeSearchText(t)).Append(' ');
        }
        if (word.ExamTags != null)
        {
            foreach (var et in word.ExamTags) sb.Append(NormalizeSearchText(et)).Append(' ');
        }
        return sb.ToString();
    }

    private string BuildCombinedGrammarSearchTarget(GrammarRuleDto rule)
    {
        var sb = new StringBuilder();
        sb.Append(NormalizeSearchText(rule.Title)).Append(' ');
        sb.Append(NormalizeSearchText(rule.Structure)).Append(' ');
        sb.Append(NormalizeSearchText(rule.ExplanationVi)).Append(' ');
        if (!string.IsNullOrEmpty(rule.ExplanationEn))
            sb.Append(NormalizeSearchText(rule.ExplanationEn)).Append(' ');
        if (!string.IsNullOrEmpty(rule.Topic))
            sb.Append(NormalizeSearchText(rule.Topic)).Append(' ');
        if (!string.IsNullOrEmpty(rule.CommonMistakes))
            sb.Append(NormalizeSearchText(rule.CommonMistakes)).Append(' ');
        return sb.ToString();
    }
}
