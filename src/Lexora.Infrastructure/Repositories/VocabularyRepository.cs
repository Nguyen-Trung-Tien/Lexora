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
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Word?> GetByTermAsync(string term, CancellationToken cancellationToken = default)
    {
        var normalized = term.Trim().ToLowerInvariant();
        return await DbSet
            .Include(w => w.Meanings)
                .ThenInclude(m => m.Examples)
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
            query = query.Where(w =>
                w.NormalizedTerm.Contains(term) ||
                w.Meanings.Any(m => m.DefinitionVi.ToLower().Contains(term) || (m.DefinitionEn != null && m.DefinitionEn.ToLower().Contains(term))));
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
            query = query.Where(w => w.WordExamTags.Any(wet => wet.ExamTag.Type == examType.Value));
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

    public async Task<IReadOnlyList<ExamTag>> GetAllExamTagsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.ExamTags.AsNoTracking().OrderBy(et => et.Name).ToListAsync(cancellationToken);
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
