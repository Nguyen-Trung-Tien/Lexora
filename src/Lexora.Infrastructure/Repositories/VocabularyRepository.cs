using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

public class VocabularyRepository : Repository<Word>, IVocabularyRepository
{
    public VocabularyRepository(LexoraDbContext context) : base(context)
    {
    }

    public async Task<Word?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Meanings)
                .ThenInclude(m => m.Examples)
            .Include(w => w.WordTopics)
                .ThenInclude(wt => wt.Topic)
            .Include(w => w.WordExamTags)
                .ThenInclude(wet => wet.ExamTag)
            .Include(w => w.ReviewCard)
                .ThenInclude(rc => rc!.StudyLogs)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Word>> GetByIdsWithDetailsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return Array.Empty<Word>();

        if (idList.Count <= 500)
        {
            return await DbSet
                .Include(w => w.Meanings)
                    .ThenInclude(m => m.Examples)
                .Include(w => w.WordTopics)
                    .ThenInclude(wt => wt.Topic)
                .Include(w => w.WordExamTags)
                    .ThenInclude(wet => wet.ExamTag)
                .Include(w => w.ReviewCard)
                    .ThenInclude(rc => rc!.StudyLogs)
                .Where(w => idList.Contains(w.Id))
                .ToListAsync(cancellationToken);
        }

        var results = new List<Word>(idList.Count);
        foreach (var chunk in idList.Chunk(500))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = await DbSet
                .Include(w => w.Meanings)
                    .ThenInclude(m => m.Examples)
                .Include(w => w.WordTopics)
                    .ThenInclude(wt => wt.Topic)
                .Include(w => w.WordExamTags)
                    .ThenInclude(wet => wet.ExamTag)
                .Include(w => w.ReviewCard)
                    .ThenInclude(rc => rc!.StudyLogs)
                .Where(w => chunk.Contains(w.Id))
                .ToListAsync(cancellationToken);
            results.AddRange(batch);
        }
        return results;
    }

    public async Task<Word?> GetByTermAsync(string term, CancellationToken cancellationToken = default)
    {
        var normalized = term.Trim().ToLowerInvariant();
        return await DbSet
            .Include(w => w.Meanings)
                .ThenInclude(m => m.Examples)
            .Include(w => w.WordTopics)
                .ThenInclude(wt => wt.Topic)
            .Include(w => w.WordExamTags)
                .ThenInclude(wet => wet.ExamTag)
            .Include(w => w.ReviewCard)
            .FirstOrDefaultAsync(w => w.NormalizedTerm == normalized, cancellationToken);
    }

    public Task<IReadOnlyList<Word>> SearchAsync(
        string? searchTerm = null,
        CefrLevel? level = null,
        PartOfSpeech? partOfSpeech = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        int skip = 0,
        int take = 50,
        string? sortBy = null,
        bool isAscending = true,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(searchTerm, level, partOfSpeech, topicId, examType, status, skip, take, sortBy, isAscending, null, null, cancellationToken);
    }

    public async Task<IReadOnlyList<Word>> SearchAsync(
        string? searchTerm,
        CefrLevel? level,
        PartOfSpeech? partOfSpeech,
        Guid? topicId,
        ExamType? examType,
        LearningStatus? status,
        int skip,
        int take,
        string? sortBy,
        bool isAscending,
        bool? isDueToday,
        bool? isWeak,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(w => w.Meanings)
                .ThenInclude(m => m.Examples)
            .Include(w => w.WordTopics)
                .ThenInclude(wt => wt.Topic)
            .Include(w => w.WordExamTags)
                .ThenInclude(wet => wet.ExamTag)
            .Include(w => w.ReviewCard)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, searchTerm, level, partOfSpeech, topicId, examType, status, isDueToday, isWeak);

        query = sortBy?.ToLowerInvariant() switch
        {
            "term" => isAscending ? query.OrderBy(w => w.Term) : query.OrderByDescending(w => w.Term),
            "level" => isAscending ? query.OrderBy(w => w.Level) : query.OrderByDescending(w => w.Level),
            "createdat" => isAscending ? query.OrderBy(w => w.CreatedAt) : query.OrderByDescending(w => w.CreatedAt),
            "nextreview" => isAscending
                ? query.OrderBy(w => w.ReviewCard != null ? w.ReviewCard.NextReviewAt : DateTime.MaxValue)
                : query.OrderByDescending(w => w.ReviewCard != null ? w.ReviewCard.NextReviewAt : DateTime.MinValue),
            "pos" => isAscending
                ? query.OrderBy(w => w.PrimaryPartOfSpeech).ThenBy(w => w.Term)
                : query.OrderByDescending(w => w.PrimaryPartOfSpeech).ThenBy(w => w.Term),
            "multipos" => isAscending
                ? query.OrderBy(w => w.Meanings.Count).ThenBy(w => w.Term)
                : query.OrderByDescending(w => w.Meanings.Count).ThenBy(w => w.Term),
            _ => query.OrderBy(w => w.Term)
        };

        var results = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        if (results.Count > 0 || string.IsNullOrWhiteSpace(searchTerm))
        {
            return results;
        }

        // Diacritic-insensitive fallback for Vietnamese unaccented search
        return await SearchDiacriticFallbackAsync(
            searchTerm, level, partOfSpeech, topicId, examType, status,
            skip, take, sortBy, isAscending, isDueToday, isWeak, cancellationToken);
    }

    public Task<int> CountFilteredAsync(
        string? searchTerm = null,
        CefrLevel? level = null,
        PartOfSpeech? partOfSpeech = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return CountFilteredAsync(searchTerm, level, partOfSpeech, topicId, examType, status, null, null, cancellationToken);
    }

    public async Task<int> CountFilteredAsync(
        string? searchTerm,
        CefrLevel? level,
        PartOfSpeech? partOfSpeech,
        Guid? topicId,
        ExamType? examType,
        LearningStatus? status,
        bool? isDueToday,
        bool? isWeak,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        query = ApplyFilters(query, searchTerm, level, partOfSpeech, topicId, examType, status, isDueToday, isWeak);
        var count = await query.CountAsync(cancellationToken);
        if (count > 0 || string.IsNullOrWhiteSpace(searchTerm))
        {
            return count;
        }

        // Diacritic-insensitive fallback count for Vietnamese unaccented search
        return await CountDiacriticFallbackAsync(
            searchTerm, level, partOfSpeech, topicId, examType, status,
            isDueToday, isWeak, cancellationToken);
    }

    private async Task<IReadOnlyList<Word>> SearchDiacriticFallbackAsync(
        string searchTerm,
        CefrLevel? level,
        PartOfSpeech? partOfSpeech,
        Guid? topicId,
        ExamType? examType,
        LearningStatus? status,
        int skip,
        int take,
        string? sortBy,
        bool isAscending,
        bool? isDueToday,
        bool? isWeak,
        CancellationToken cancellationToken)
    {
        var normQuery = Lexora.Domain.Common.SearchTextHelper.Normalize(searchTerm);
        if (string.IsNullOrEmpty(normQuery)) return Array.Empty<Word>();

        var fallbackQuery = DbSet.AsNoTracking().AsQueryable();
        fallbackQuery = ApplyFilters(fallbackQuery, null, level, partOfSpeech, topicId, examType, status, isDueToday, isWeak);

        var projected = await fallbackQuery
            .Select(w => new
            {
                w.Id,
                w.Term,
                w.NormalizedTerm,
                w.PhoneticUk,
                w.PhoneticUs,
                w.Notes,
                w.Level,
                w.PrimaryPartOfSpeech,
                w.CreatedAt,
                NextReviewAt = w.ReviewCard != null ? w.ReviewCard.NextReviewAt : (DateTime?)null,
                MeaningCount = w.Meanings.Count,
                Definitions = w.Meanings.Select(m => (m.DefinitionVi ?? "") + " " + (m.DefinitionEn ?? "") + " " + (m.Explanation ?? "")),
                Examples = normQuery.Length >= 5 ? w.Meanings.SelectMany(m => m.Examples.Select(e => (e.SentenceEn ?? "") + " " + (e.SentenceVi ?? ""))) : Enumerable.Empty<string>(),
                TopicNames = w.WordTopics.Select(wt => wt.Topic.Name),
                ExamTagNames = w.WordExamTags.Select(wet => wet.ExamTag.Name)
            })
            .ToListAsync(cancellationToken);

        var matched = projected.Where(p => IsProjectedMatch(p.Term, p.NormalizedTerm, p.PhoneticUk, p.PhoneticUs, p.Notes, p.Definitions, p.Examples, p.TopicNames, p.ExamTagNames, normQuery));

        matched = sortBy?.ToLowerInvariant() switch
        {
            "term" => isAscending ? matched.OrderBy(w => w.Term) : matched.OrderByDescending(w => w.Term),
            "level" => isAscending ? matched.OrderBy(w => w.Level) : matched.OrderByDescending(w => w.Level),
            "createdat" => isAscending ? matched.OrderBy(w => w.CreatedAt) : matched.OrderByDescending(w => w.CreatedAt),
            "nextreview" => isAscending
                ? matched.OrderBy(w => w.NextReviewAt ?? DateTime.MaxValue)
                : matched.OrderByDescending(w => w.NextReviewAt ?? DateTime.MinValue),
            "pos" => isAscending
                ? matched.OrderBy(w => w.PrimaryPartOfSpeech).ThenBy(w => w.Term)
                : matched.OrderByDescending(w => w.PrimaryPartOfSpeech).ThenBy(w => w.Term),
            "multipos" => isAscending
                ? matched.OrderBy(w => w.MeaningCount).ThenBy(w => w.Term)
                : matched.OrderByDescending(w => w.MeaningCount).ThenBy(w => w.Term),
            _ => matched.OrderBy(w => w.Term)
        };

        var matchedIds = matched.Skip(skip).Take(take).Select(p => p.Id).ToList();
        if (matchedIds.Count == 0) return Array.Empty<Word>();

        var fullWords = await GetByIdsWithDetailsAsync(matchedIds, cancellationToken);
        var wordDict = fullWords.ToDictionary(w => w.Id);
        var orderedResults = new List<Word>(matchedIds.Count);
        foreach (var id in matchedIds)
        {
            if (wordDict.TryGetValue(id, out var w))
            {
                orderedResults.Add(w);
            }
        }
        return orderedResults;
    }

    private async Task<int> CountDiacriticFallbackAsync(
        string searchTerm,
        CefrLevel? level,
        PartOfSpeech? partOfSpeech,
        Guid? topicId,
        ExamType? examType,
        LearningStatus? status,
        bool? isDueToday,
        bool? isWeak,
        CancellationToken cancellationToken)
    {
        var normQuery = Lexora.Domain.Common.SearchTextHelper.Normalize(searchTerm);
        if (string.IsNullOrEmpty(normQuery)) return 0;

        var fallbackQuery = DbSet.AsNoTracking().AsQueryable();
        fallbackQuery = ApplyFilters(fallbackQuery, null, level, partOfSpeech, topicId, examType, status, isDueToday, isWeak);

        var projected = await fallbackQuery
            .Select(w => new
            {
                w.Term,
                w.NormalizedTerm,
                w.PhoneticUk,
                w.PhoneticUs,
                w.Notes,
                Definitions = w.Meanings.Select(m => (m.DefinitionVi ?? "") + " " + (m.DefinitionEn ?? "") + " " + (m.Explanation ?? "")),
                Examples = normQuery.Length >= 5 ? w.Meanings.SelectMany(m => m.Examples.Select(e => (e.SentenceEn ?? "") + " " + (e.SentenceVi ?? ""))) : Enumerable.Empty<string>(),
                TopicNames = w.WordTopics.Select(wt => wt.Topic.Name),
                ExamTagNames = w.WordExamTags.Select(wet => wet.ExamTag.Name)
            })
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var p in projected)
        {
            if (IsProjectedMatch(p.Term, p.NormalizedTerm, p.PhoneticUk, p.PhoneticUs, p.Notes, p.Definitions, p.Examples, p.TopicNames, p.ExamTagNames, normQuery))
            {
                count++;
            }
        }
        return count;
    }

    private static bool IsProjectedMatch(
        string term,
        string normalizedTerm,
        string? phoneticUk,
        string? phoneticUs,
        string? notes,
        IEnumerable<string> definitions,
        IEnumerable<string> examples,
        IEnumerable<string> topicNames,
        IEnumerable<string> examTagNames,
        string normQuery)
    {
        if (Lexora.Domain.Common.SearchTextHelper.Normalize(term).Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
            Lexora.Domain.Common.SearchTextHelper.Normalize(normalizedTerm).Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(phoneticUk) && Lexora.Domain.Common.SearchTextHelper.Normalize(phoneticUk).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(phoneticUs) && Lexora.Domain.Common.SearchTextHelper.Normalize(phoneticUs).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(notes) && Lexora.Domain.Common.SearchTextHelper.Normalize(notes).Contains(normQuery, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        foreach (var def in definitions)
        {
            if (!string.IsNullOrEmpty(def) && Lexora.Domain.Common.SearchTextHelper.Normalize(def).Contains(normQuery, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (normQuery.Length >= 5)
        {
            foreach (var ex in examples)
            {
                if (!string.IsNullOrEmpty(ex) && Lexora.Domain.Common.SearchTextHelper.Normalize(ex).Contains(normQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        foreach (var topic in topicNames)
        {
            if (!string.IsNullOrEmpty(topic) && Lexora.Domain.Common.SearchTextHelper.Normalize(topic).Contains(normQuery, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var tag in examTagNames)
        {
            if (!string.IsNullOrEmpty(tag) && Lexora.Domain.Common.SearchTextHelper.Normalize(tag).Contains(normQuery, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWordMatch(Word w, string normQuery)
    {
        if (Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
            Lexora.Domain.Common.SearchTextHelper.Normalize(w.NormalizedTerm).Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(w.PhoneticUk) && Lexora.Domain.Common.SearchTextHelper.Normalize(w.PhoneticUk).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(w.PhoneticUs) && Lexora.Domain.Common.SearchTextHelper.Normalize(w.PhoneticUs).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(w.Notes) && Lexora.Domain.Common.SearchTextHelper.Normalize(w.Notes).Contains(normQuery, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (w.Meanings != null)
        {
            foreach (var m in w.Meanings)
            {
                if ((!string.IsNullOrEmpty(m.DefinitionVi) && Lexora.Domain.Common.SearchTextHelper.Normalize(m.DefinitionVi).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(m.DefinitionEn) && Lexora.Domain.Common.SearchTextHelper.Normalize(m.DefinitionEn).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(m.Explanation) && Lexora.Domain.Common.SearchTextHelper.Normalize(m.Explanation).Contains(normQuery, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                if (normQuery.Length >= 5 && m.Examples != null)
                {
                    foreach (var ex in m.Examples)
                    {
                        if ((!string.IsNullOrEmpty(ex.SentenceEn) && Lexora.Domain.Common.SearchTextHelper.Normalize(ex.SentenceEn).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(ex.SentenceVi) && Lexora.Domain.Common.SearchTextHelper.Normalize(ex.SentenceVi).Contains(normQuery, StringComparison.OrdinalIgnoreCase)))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        if (w.WordTopics != null && w.WordTopics.Any(wt => wt.Topic != null && !string.IsNullOrEmpty(wt.Topic.Name) && Lexora.Domain.Common.SearchTextHelper.Normalize(wt.Topic.Name).Contains(normQuery, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (w.WordExamTags != null && w.WordExamTags.Any(wet => wet.ExamTag != null && !string.IsNullOrEmpty(wet.ExamTag.Name) && Lexora.Domain.Common.SearchTextHelper.Normalize(wet.ExamTag.Name).Contains(normQuery, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    public async Task<IReadOnlyList<Word>> GetRandomWordsAsync(int count, Guid? excludeWordId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(w => w.Meanings)
            .AsNoTracking()
            .AsQueryable();

        if (excludeWordId.HasValue)
        {
            query = query.Where(w => w.Id != excludeWordId.Value);
        }

        // Random order via Guid
        return await query
            .OrderBy(_ => EF.Functions.Random())
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Word>> GetWordsForAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.WordTopics)
            .Include(w => w.WordExamTags)
                .ThenInclude(wet => wet.ExamTag)
            .Include(w => w.ReviewCard)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Word> ApplyFilters(
        IQueryable<Word> query,
        string? searchTerm,
        CefrLevel? level,
        PartOfSpeech? partOfSpeech,
        Guid? topicId,
        ExamType? examType,
        LearningStatus? status,
        bool? isDueToday = null,
        bool? isWeak = null)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            var pattern = $"%{term}%";
            bool matchExamples = term.Length >= 5;
            query = query.Where(w =>
                EF.Functions.Like(w.NormalizedTerm, pattern) ||
                EF.Functions.Like(w.Term, pattern) ||
                (w.PhoneticUk != null && EF.Functions.Like(w.PhoneticUk, pattern)) ||
                (w.PhoneticUs != null && EF.Functions.Like(w.PhoneticUs, pattern)) ||
                (w.Notes != null && EF.Functions.Like(w.Notes, pattern)) ||
                w.Meanings.Any(m =>
                    EF.Functions.Like(m.DefinitionVi, pattern) ||
                    (m.DefinitionEn != null && EF.Functions.Like(m.DefinitionEn, pattern)) ||
                    (m.Explanation != null && EF.Functions.Like(m.Explanation, pattern)) ||
                    (matchExamples && m.Examples.Any(ex =>
                        EF.Functions.Like(ex.SentenceEn, pattern) ||
                        (ex.SentenceVi != null && EF.Functions.Like(ex.SentenceVi, pattern))))) ||
                w.WordTopics.Any(wt => wt.Topic != null && EF.Functions.Like(wt.Topic.Name, pattern)) ||
                w.WordExamTags.Any(wet => wet.ExamTag != null && EF.Functions.Like(wet.ExamTag.Name, pattern)));
        }

        if (level.HasValue && level.Value != CefrLevel.None)
        {
            query = query.Where(w => w.Level == level.Value);
        }

        if (partOfSpeech.HasValue)
        {
            query = query.Where(w => w.PrimaryPartOfSpeech == partOfSpeech.Value || w.Meanings.Any(m => m.PartOfSpeech == partOfSpeech.Value));
        }

        if (topicId.HasValue)
        {
            query = query.Where(w => w.WordTopics.Any(wt => wt.TopicId == topicId.Value));
        }

        if (examType.HasValue && examType.Value != ExamType.General)
        {
            query = query.Where(w => w.WordExamTags.Any(wet => wet.ExamTag != null && wet.ExamTag.Type == examType.Value));
        }

        if (status.HasValue)
        {
            query = query.Where(w => w.ReviewCard != null && w.ReviewCard.Status == status.Value);
        }

        if (isDueToday == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(w => w.ReviewCard != null && w.ReviewCard.NextReviewAt <= now);
        }

        if (isWeak == true)
        {
            query = query.Where(w => w.ReviewCard != null && (w.ReviewCard.Lapses > 0 || w.ReviewCard.Status == LearningStatus.NotRemembered));
        }

        return query;
    }

    public async Task<IReadOnlyList<Topic>> GetAllTopicsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Topics.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public async Task<Topic?> GetTopicByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Topics.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Topic?> GetTopicByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim().ToLower();
        return await Context.Topics.FirstOrDefaultAsync(t => t.Name.ToLower() == trimmed, cancellationToken);
    }

    public async Task AddTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        await Context.Topics.AddAsync(topic, cancellationToken);
    }

    public void DeleteTopic(Topic topic)
    {
        var associations = Context.WordTopics.Where(wt => wt.TopicId == topic.Id).ToList();
        if (associations.Count > 0)
        {
            Context.WordTopics.RemoveRange(associations);
        }
        Context.Topics.Remove(topic);
    }

    public async Task<IReadOnlyList<ExamTag>> GetAllExamTagsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.ExamTags.AsNoTracking().OrderBy(et => et.Name).ToListAsync(cancellationToken);
    }

    public async Task AddExamTagAsync(ExamTag examTag, CancellationToken cancellationToken = default)
    {
        await Context.ExamTags.AddAsync(examTag, cancellationToken);
    }

    public void RemoveWordChildren(Word word)
    {
        var examples = word.Meanings.SelectMany(m => m.Examples).ToList();
        if (examples.Count > 0)
        {
            Context.ExampleSentences.RemoveRange(examples);
        }
        var meanings = word.Meanings.ToList();
        if (meanings.Count > 0)
        {
            Context.WordMeanings.RemoveRange(meanings);
            word.Meanings.Clear();
        }
        var topics = word.WordTopics.ToList();
        if (topics.Count > 0)
        {
            Context.WordTopics.RemoveRange(topics);
            word.WordTopics.Clear();
        }
        var tags = word.WordExamTags.ToList();
        if (tags.Count > 0)
        {
            Context.WordExamTags.RemoveRange(tags);
            word.WordExamTags.Clear();
        }
    }

    public void AddMeanings(IEnumerable<WordMeaning> meanings)
    {
        Context.WordMeanings.AddRange(meanings);
    }

    public void AddWordTopics(IEnumerable<WordTopic> wordTopics)
    {
        Context.WordTopics.AddRange(wordTopics);
    }

    public void AddWordExamTags(IEnumerable<WordExamTag> wordExamTags)
    {
        Context.WordExamTags.AddRange(wordExamTags);
    }

    public async Task<HashSet<string>> GetAllNormalizedTermsAsync(CancellationToken cancellationToken = default)
    {
        var terms = await DbSet
            .AsNoTracking()
            .Select(w => w.NormalizedTerm)
            .ToListAsync(cancellationToken);
        return new HashSet<string>(terms, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, Word>> GetWordsByTermsWithDetailsAsync(
        IEnumerable<string> normalizedTerms,
        CancellationToken cancellationToken = default)
    {
        var termList = normalizedTerms.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        if (termList.Count == 0) return new Dictionary<string, Word>(StringComparer.OrdinalIgnoreCase);

        var words = await DbSet
            .Include(w => w.Meanings)
                .ThenInclude(m => m.Examples)
            .Include(w => w.WordTopics)
                .ThenInclude(wt => wt.Topic)
            .Include(w => w.WordExamTags)
                .ThenInclude(wet => wet.ExamTag)
            .Include(w => w.ReviewCard)
            .Where(w => termList.Contains(w.NormalizedTerm))
            .ToListAsync(cancellationToken);

        var dict = new Dictionary<string, Word>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            dict[w.NormalizedTerm] = w;
        }
        return dict;
    }

    public override async Task AddRangeAsync(IEnumerable<Word> words, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(words, cancellationToken);
    }

    public async Task<HashSet<Guid>> GetExistingWordTopicAssociationsAsync(
        Guid topicId,
        IEnumerable<Guid> wordIds,
        CancellationToken cancellationToken = default)
    {
        var ids = wordIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return new HashSet<Guid>();

        var existing = await Context.WordTopics
            .AsNoTracking()
            .Where(wt => wt.TopicId == topicId && ids.Contains(wt.WordId))
            .Select(wt => wt.WordId)
            .ToListAsync(cancellationToken);

        return new HashSet<Guid>(existing);
    }

    public async Task RemoveWordTopicAssociationsAsync(
        Guid topicId,
        IEnumerable<Guid> wordIds,
        CancellationToken cancellationToken = default)
    {
        var ids = wordIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return;

        var toRemove = await Context.WordTopics
            .Where(wt => wt.TopicId == topicId && ids.Contains(wt.WordId))
            .ToListAsync(cancellationToken);

        if (toRemove.Count > 0)
        {
            Context.WordTopics.RemoveRange(toRemove);
        }
    }
}
