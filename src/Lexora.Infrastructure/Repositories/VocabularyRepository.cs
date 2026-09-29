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
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return Array.Empty<Word>();

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

    public async Task<IReadOnlyList<Word>> SearchAsync(
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

        query = ApplyFilters(query, searchTerm, level, partOfSpeech, topicId, examType, status);

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

        return await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<int> CountFilteredAsync(
        string? searchTerm = null,
        CefrLevel? level = null,
        PartOfSpeech? partOfSpeech = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        query = ApplyFilters(query, searchTerm, level, partOfSpeech, topicId, examType, status);
        return await query.CountAsync(cancellationToken);
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
        LearningStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            var pattern = $"%{term}%";
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
                    m.Examples.Any(ex =>
                        EF.Functions.Like(ex.SentenceEn, pattern) ||
                        (ex.SentenceVi != null && EF.Functions.Like(ex.SentenceVi, pattern)))) ||
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
}
