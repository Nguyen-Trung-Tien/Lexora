using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class GrammarService : IGrammarService
{
    private readonly IGrammarRepository _grammarRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISmartSearchService? _smartSearch;
    private readonly ISmartSearchCacheService? _cacheService;
    private readonly IVocabularyService? _vocabService;

    public GrammarService(
        IGrammarRepository grammarRepo,
        IUnitOfWork unitOfWork,
        ISmartSearchService? smartSearch = null,
        ISmartSearchCacheService? cacheService = null,
        IVocabularyService? vocabService = null)
    {
        _grammarRepo = grammarRepo;
        _unitOfWork = unitOfWork;
        _smartSearch = smartSearch;
        _cacheService = cacheService;
        _vocabService = vocabService;
    }

    public async Task<IReadOnlyList<GrammarRuleDto>> GetAllRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _grammarRepo.GetAllAsync(cancellationToken);
        return rules.Select(MapToDto).ToList();
    }

    public async Task<GrammarRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _grammarRepo.GetWithExamplesAsync(id, cancellationToken);
        return rule == null ? null : MapToDto(rule);
    }

    public async Task<IReadOnlyList<GrammarRuleDto>> GetRulesByLevelAsync(CefrLevel level, CancellationToken cancellationToken = default)
    {
        var rules = await _grammarRepo.GetByLevelAsync(level, cancellationToken);
        return rules.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<GrammarRuleDto>> SearchRulesAsync(string query, CancellationToken cancellationToken = default)
    {
        var rules = await _grammarRepo.SearchAsync(query, cancellationToken);
        return rules.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<GrammarRuleDto>> GetFilteredRulesAsync(
        CefrLevel? level = null,
        ExamType? examType = null,
        string? topic = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var rules = await _grammarRepo.GetFilteredAsync(level, examType, topic, search, cancellationToken);
        return rules.Select(MapToDto).ToList();
    }

    public async Task<GrammarPagedResultDto> GetPagedRulesAsync(
        int page,
        int pageSize,
        CefrLevel? level = null,
        ExamType? examType = null,
        string? topic = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;

        int totalCount;
        try
        {
            totalCount = await _grammarRepo.CountFilteredAsync(level, examType, topic, search, cancellationToken);
        }
        catch
        {
            var fallback = await _grammarRepo.GetFilteredAsync(level, examType, topic, search, cancellationToken);
            totalCount = fallback.Count;
            var pagedFallback = fallback.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();
            return new GrammarPagedResultDto(pagedFallback, totalCount, page, pageSize);
        }

        int skip = (page - 1) * pageSize;

        if (!string.IsNullOrWhiteSpace(search) && _smartSearch != null)
        {
            var allFiltered = await _grammarRepo.GetFilteredAsync(level, examType, topic, search, cancellationToken);
            totalCount = allFiltered.Count;
            var dtos = allFiltered.Select(MapToDto).ToList();
            var ranked = _smartSearch.RankGrammar(search, dtos, dtos.Count);
            var pagedRanked = ranked.Skip(skip).Take(pageSize).Select(r => r.Item).ToList();
            return new GrammarPagedResultDto(pagedRanked, totalCount, page, pageSize);
        }

        IReadOnlyList<GrammarRule> rules;
        try
        {
            rules = await _grammarRepo.GetFilteredPagedAsync(skip, pageSize, level, examType, topic, search, cancellationToken);
        }
        catch
        {
            var fallback = await _grammarRepo.GetFilteredAsync(level, examType, topic, search, cancellationToken);
            rules = fallback.Skip(skip).Take(pageSize).ToList();
        }

        return new GrammarPagedResultDto(rules.Select(MapToDto).ToList(), totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<string>> GetAllTopicsAsync(CancellationToken cancellationToken = default)
    {
        return await _grammarRepo.GetAllTopicsAsync(cancellationToken);
    }

    public async Task<GrammarRuleDto> CreateRuleAsync(GrammarRuleCreateOrUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Grammar rule title is required.", nameof(dto));

        var rule = new GrammarRule
        {
            Title = dto.Title.Trim(),
            Structure = dto.Structure.Trim(),
            ExplanationVi = dto.ExplanationVi.Trim(),
            ExplanationEn = dto.ExplanationEn?.Trim(),
            CommonMistakes = dto.CommonMistakes?.Trim(),
            Level = dto.Level,
            ExamTarget = dto.ExamTarget,
            Topic = dto.Topic?.Trim()
        };

        if (dto.Examples != null)
        {
            foreach (var ex in dto.Examples)
            {
                rule.Examples.Add(new GrammarExample
                {
                    SentenceEn = ex.SentenceEn.Trim(),
                    SentenceVi = ex.SentenceVi.Trim(),
                    Note = ex.Note?.Trim()
                });
            }
        }

        await _grammarRepo.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _cacheService?.InvalidateGrammar();

        return MapToDto(rule);
    }

    public async Task<GrammarRuleDto> UpdateRuleAsync(GrammarRuleCreateOrUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Id == null)
            throw new ArgumentException("Rule Id must be provided for update.", nameof(dto));

        var rule = await _grammarRepo.GetWithExamplesAsync(dto.Id.Value, cancellationToken);
        if (rule == null)
            throw new KeyNotFoundException($"Grammar rule with ID {dto.Id} not found.");

        rule.Title = dto.Title.Trim();
        rule.Structure = dto.Structure.Trim();
        rule.ExplanationVi = dto.ExplanationVi.Trim();
        rule.ExplanationEn = dto.ExplanationEn?.Trim();
        rule.CommonMistakes = dto.CommonMistakes?.Trim();
        rule.Level = dto.Level;
        rule.ExamTarget = dto.ExamTarget;
        rule.Topic = dto.Topic?.Trim();
        rule.UpdatedAt = DateTime.UtcNow;

        // Clean up previous examples
        if (rule.Examples.Count > 0)
        {
            _grammarRepo.RemoveExamples(rule.Examples.ToList());
            rule.Examples.Clear();
        }

        if (dto.Examples != null)
        {
            var newExamples = dto.Examples.Select(ex => new GrammarExample
            {
                GrammarRuleId = rule.Id,
                SentenceEn = ex.SentenceEn.Trim(),
                SentenceVi = ex.SentenceVi.Trim(),
                Note = ex.Note?.Trim()
            }).ToList();

            foreach (var ex in newExamples)
            {
                rule.Examples.Add(ex);
            }

            _grammarRepo.AddExamples(newExamples);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _cacheService?.InvalidateGrammar();

        var refreshed = await _grammarRepo.GetWithExamplesAsync(rule.Id, cancellationToken);
        return MapToDto(refreshed ?? rule);
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) return;

        var rule = await _grammarRepo.GetWithExamplesAsync(id, cancellationToken)
                   ?? await _grammarRepo.GetByIdAsync(id, cancellationToken);
        if (rule != null)
        {
            await _grammarRepo.DeleteAsync(rule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _cacheService?.InvalidateGrammar();
            LearningStorageService.NotifyGlobalStorageChanged();
        }
    }

    public async Task<int> DeleteRulesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var idList = ids.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            int deletedCount = 0;
            foreach (var chunk in idList.Chunk(250))
            {
                cancellationToken.ThrowIfCancellationRequested();
                int chunkCount = 0;
                foreach (var id in chunk)
                {
                    var rule = await _grammarRepo.GetWithExamplesAsync(id, cancellationToken)
                               ?? await _grammarRepo.GetByIdAsync(id, cancellationToken);
                    if (rule != null)
                    {
                        await _grammarRepo.DeleteAsync(rule, cancellationToken);
                        deletedCount++;
                        chunkCount++;
                    }
                }

                if (chunkCount > 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _unitOfWork.ClearTracker();
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _cacheService?.InvalidateGrammar();
            LearningStorageService.NotifyGlobalStorageChanged();
            return deletedCount;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<GrammarQuizQuestionDto>> GenerateGrammarQuizAsync(
        int count = 10,
        CefrLevel? level = null,
        ExamType? examType = null,
        CancellationToken cancellationToken = default)
    {
        var rules = await _grammarRepo.GetFilteredAsync(level, examType, cancellationToken: cancellationToken);
        if (rules.Count == 0)
        {
            rules = await _grammarRepo.GetAllAsync(cancellationToken);
        }

        if (rules.Count == 0)
        {
            return Array.Empty<GrammarQuizQuestionDto>();
        }

        var selectedRules = rules.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
        var allStructures = rules.Select(r => r.Structure).Distinct().ToList();

        var questions = new List<GrammarQuizQuestionDto>();
        for (int i = 0; i < selectedRules.Count; i++)
        {
            var rule = selectedRules[i];
            var example = rule.Examples.FirstOrDefault();

            string prompt;
            string? subPrompt;
            string correctAnswer;
            var options = new List<string>();
            string explanation;

            // Generate either structure-identification or sentence-completion question
            if (example != null && !string.IsNullOrWhiteSpace(example.SentenceEn) && Random.Shared.Next(2) == 0)
            {
                prompt = $"Which grammar structure is applied in the following sentence?\n\"{example.SentenceEn}\"";
                subPrompt = $"Meaning: {example.SentenceVi}";
                correctAnswer = rule.Structure;
                explanation = $"Rule: {rule.Title} ({rule.Structure}) — {rule.ExplanationVi}";

                options.Add(correctAnswer);
                var distractors = allStructures
                    .Where(s => !s.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(distractors);
            }
            else
            {
                prompt = $"What is the correct structure formula for \"{rule.Title}\"?";
                subPrompt = $"Target level: {rule.Level} • Exam: {rule.ExamTarget}";
                correctAnswer = rule.Structure;
                explanation = $"{rule.Title}: {rule.Structure}\nExplanation: {rule.ExplanationVi}";

                options.Add(correctAnswer);
                var distractors = allStructures
                    .Where(s => !s.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(distractors);
            }

            options = options.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            while (options.Count < 4)
            {
                options.Add($"Alternative Formula {options.Count + 1}");
            }

            var shuffledOptions = options.OrderBy(_ => Random.Shared.Next()).ToList();

            questions.Add(new GrammarQuizQuestionDto(
                rule.Id,
                i + 1,
                prompt,
                subPrompt,
                correctAnswer,
                shuffledOptions,
                explanation
            ));
        }

        return questions;
    }

    public GrammarPracticeResultDto EvaluateQuiz(List<GrammarQuizQuestionDto> questions, List<GrammarQuizSubmissionDto> answers)
    {
        int correct = 0;
        int incorrect = 0;
        var evaluatedQuestions = new List<GrammarQuizQuestionDto>();

        foreach (var q in questions)
        {
            var ans = answers.FirstOrDefault(a => a.RuleId == q.RuleId && a.QuestionNumber == q.QuestionNumber);
            string userAns = ans?.SelectedAnswer?.Trim() ?? string.Empty;
            bool isCorrect = string.Equals(userAns, q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isCorrect) correct++;
            else incorrect++;

            evaluatedQuestions.Add(q with
            {
                UserAnswer = userAns,
                IsCorrect = isCorrect
            });
        }

        int total = evaluatedQuestions.Count;
        double scorePercent = total > 0 ? Math.Round((double)correct / total * 100.0, 1) : 0.0;

        return new GrammarPracticeResultDto(
            total,
            correct,
            incorrect,
            scorePercent,
            evaluatedQuestions
        );
    }

    public async Task<GrammarRelatedSuggestionsDto> GetGrammarSuggestionsAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(ruleId, cancellationToken);
        if (rule == null)
        {
            return new GrammarRelatedSuggestionsDto(
                ruleId,
                string.Empty,
                Array.Empty<GrammarSuggestionItemDto>(),
                Array.Empty<GrammarSuggestionItemDto>(),
                Array.Empty<GrammarSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>());
        }

        var allRules = await GetAllRulesAsync(cancellationToken);
        IReadOnlyList<WordDto>? words = null;
        if (_vocabService != null)
        {
            try { words = await _vocabService.SearchWordsAsync(pageSize: 100, cancellationToken: cancellationToken); }
            catch { words = null; }
        }

        var searchSvc = _smartSearch ?? new SmartSearchService(new FuzzySearchService());
        var suggestionSvc = new GrammarSuggestionService(this, searchSvc, _vocabService);
        return suggestionSvc.GetSuggestions(rule, allRules, words);
    }

    public async Task<SmartSearchResultPageDto<GrammarRuleDto>> SmartSearchRulesAsync(
        string? search = null,
        CefrLevel? level = null,
        ExamType? examType = null,
        string? topic = null,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool isAscending = true,
        CancellationToken cancellationToken = default)
    {
        var paged = await GetPagedRulesAsync(page, pageSize, level, examType, topic, search, cancellationToken);
        var matchResults = new List<SmartSearchMatchResult<GrammarRuleDto>>();

        if (!string.IsNullOrWhiteSpace(search) && _smartSearch != null)
        {
            foreach (var r in paged.Rules)
            {
                var scored = _smartSearch.ScoreGrammar(search, r);
                matchResults.Add(scored ?? new SmartSearchMatchResult<GrammarRuleDto>(r, 50.0, "Matched"));
            }
        }
        else
        {
            matchResults = paged.Rules.Select(r => new SmartSearchMatchResult<GrammarRuleDto>(r, 100.0, "Filter")).ToList();
        }

        IReadOnlyList<string> closestSuggestions = Array.Empty<string>();
        if (paged.TotalCount == 0 && !string.IsNullOrWhiteSpace(search) && _smartSearch != null)
        {
            var allRules = await GetAllRulesAsync(cancellationToken);
            closestSuggestions = _smartSearch.FindClosestGrammarRuleSuggestions(search, allRules, maxSuggestions: 5);
        }

        return new SmartSearchResultPageDto<GrammarRuleDto>(
            matchResults,
            paged.TotalCount,
            page,
            pageSize,
            closestSuggestions);
    }

    private static GrammarRuleDto MapToDto(GrammarRule r)
    {
        var examples = r.Examples.Select(e => new GrammarExampleDto(e.Id, e.SentenceEn, e.SentenceVi, e.Note)).ToList();
        return new GrammarRuleDto(
            r.Id,
            r.Title,
            r.Structure,
            r.ExplanationVi,
            r.ExplanationEn,
            r.CommonMistakes,
            r.Level,
            r.ExamTarget,
            r.Topic,
            examples
        );
    }
}
