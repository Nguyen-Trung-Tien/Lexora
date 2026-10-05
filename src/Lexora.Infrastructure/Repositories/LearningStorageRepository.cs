using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

public class LearningStorageRepository : Repository<LearningStorageItem>, ILearningStorageRepository
{
    public LearningStorageRepository(LexoraDbContext context) : base(context)
    {
    }

    public async Task<LearningStorageItem?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Word)
                .ThenInclude(w => w!.Meanings)
                    .ThenInclude(m => m.Examples)
            .Include(x => x.Word)
                .ThenInclude(w => w!.WordTopics)
                    .ThenInclude(wt => wt.Topic)
            .Include(x => x.Word)
                .ThenInclude(w => w!.WordExamTags)
                    .ThenInclude(wet => wet.ExamTag)
            .Include(x => x.Word)
                .ThenInclude(w => w!.ReviewCard)
            .Include(x => x.GrammarRule)
                .ThenInclude(g => g!.Examples)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<LearningStorageItem?> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Word)
                .ThenInclude(w => w!.Meanings)
                    .ThenInclude(m => m.Examples)
            .Include(x => x.Word)
                .ThenInclude(w => w!.ReviewCard)
            .FirstOrDefaultAsync(x => x.WordId == wordId, cancellationToken);
    }

    public async Task<LearningStorageItem?> GetByGrammarRuleIdAsync(Guid grammarRuleId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.GrammarRule)
                .ThenInclude(g => g!.Examples)
            .FirstOrDefaultAsync(x => x.GrammarRuleId == grammarRuleId, cancellationToken);
    }

    public async Task<IReadOnlyList<LearningStorageItem>> GetFilteredPagedAsync(
        StorageItemType? itemType,
        StorageLearningStatus? status,
        string? search,
        bool? dueTodayOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Word)
                .ThenInclude(w => w!.Meanings)
                    .ThenInclude(m => m.Examples)
            .Include(x => x.Word)
                .ThenInclude(w => w!.WordTopics)
                    .ThenInclude(wt => wt.Topic)
            .Include(x => x.Word)
                .ThenInclude(w => w!.ReviewCard)
            .Include(x => x.GrammarRule)
                .ThenInclude(g => g!.Examples)
            .AsQueryable();

        query = ApplyFilters(query, itemType, status, search, dueTodayOnly);

        // Sorting: Weak items prioritized first, then Learning, New, and Mastered
        // Secondary sort: NextReviewAt ascending (due earliest first)
        query = query
            .OrderBy(x => x.Status == StorageLearningStatus.Weak ? 0 :
                          x.Status == StorageLearningStatus.Learning ? 1 :
                          x.Status == StorageLearningStatus.New ? 2 : 3)
            .ThenBy(x => x.NextReviewAt)
            .ThenByDescending(x => x.UpdatedAt);

        return await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<int> CountFilteredAsync(
        StorageItemType? itemType,
        StorageLearningStatus? status,
        string? search,
        bool? dueTodayOnly,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        query = ApplyFilters(query, itemType, status, search, dueTodayOnly);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<(int Total, int VocabCount, int GrammarCount, int NewCount, int LearningCount, int WeakCount, int MasteredCount, int DueTodayCount, double AvgRetention)> GetStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var items = await DbSet.AsNoTracking().Select(x => new
        {
            x.ItemType,
            x.Status,
            x.NextReviewAt,
            x.RetentionProbability
        }).ToListAsync(cancellationToken);

        int total = items.Count;
        int vocabCount = items.Count(x => x.ItemType == StorageItemType.Vocabulary);
        int grammarCount = items.Count(x => x.ItemType == StorageItemType.Grammar);
        int newCount = items.Count(x => x.Status == StorageLearningStatus.New);
        int learningCount = items.Count(x => x.Status == StorageLearningStatus.Learning);
        int weakCount = items.Count(x => x.Status == StorageLearningStatus.Weak);
        int masteredCount = items.Count(x => x.Status == StorageLearningStatus.Mastered);
        int dueTodayCount = items.Count(x => x.NextReviewAt <= now && x.Status != StorageLearningStatus.Mastered);
        double avgRetention = total > 0 ? Math.Round(items.Average(x => x.RetentionProbability), 1) : 0.0;

        return (total, vocabCount, grammarCount, newCount, learningCount, weakCount, masteredCount, dueTodayCount, avgRetention);
    }

    public async Task<IReadOnlyList<LearningStorageItem>> GetItemsForPracticeAsync(
        StorageItemType itemType,
        int limit,
        bool prioritizeWeak,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Word)
                .ThenInclude(w => w!.Meanings)
                    .ThenInclude(m => m.Examples)
            .Include(x => x.GrammarRule)
                .ThenInclude(g => g!.Examples)
            .Where(x => x.ItemType == itemType);

        if (prioritizeWeak)
        {
            query = query
                .OrderBy(x => x.Status == StorageLearningStatus.Weak ? 0 :
                              x.Status == StorageLearningStatus.Learning ? 1 :
                              x.Status == StorageLearningStatus.New ? 2 : 3)
                .ThenBy(x => x.NextReviewAt);
        }
        else
        {
            query = query.OrderBy(_ => EF.Functions.Random());
        }

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LearningStorageItem>> GetItemsForTestAsync(
        StorageItemType? itemType,
        int limit,
        bool prioritizeWeak,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Word)
                .ThenInclude(w => w!.Meanings)
                    .ThenInclude(m => m.Examples)
            .Include(x => x.GrammarRule)
                .ThenInclude(g => g!.Examples)
            .AsQueryable();

        if (itemType.HasValue)
        {
            query = query.Where(x => x.ItemType == itemType.Value);
        }

        // Tests prioritize items that are NOT mastered, with Weak items at highest priority
        if (prioritizeWeak)
        {
            query = query
                .OrderBy(x => x.Status == StorageLearningStatus.Weak ? 0 :
                              x.Status == StorageLearningStatus.Learning ? 1 :
                              x.Status == StorageLearningStatus.New ? 2 : 3)
                .ThenBy(x => x.RetentionProbability);
        }
        else
        {
            query = query.OrderBy(_ => EF.Functions.Random());
        }

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteRangeAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return 0;

        const int batchSize = 150;
        int totalDeleted = 0;

        for (int i = 0; i < idList.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = idList.Skip(i).Take(batchSize).ToList();
            var items = await DbSet.Where(x => batch.Contains(x.Id)).ToListAsync(cancellationToken);
            if (items.Count > 0)
            {
                DbSet.RemoveRange(items);
                totalDeleted += items.Count;
            }
        }

        return totalDeleted;
    }

    private static IQueryable<LearningStorageItem> ApplyFilters(
        IQueryable<LearningStorageItem> query,
        StorageItemType? itemType,
        StorageLearningStatus? status,
        string? search,
        bool? dueTodayOnly)
    {
        if (itemType.HasValue)
        {
            query = query.Where(x => x.ItemType == itemType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (dueTodayOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.NextReviewAt <= now && x.Status != StorageLearningStatus.Mastered);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Word != null && (x.Word.NormalizedTerm.Contains(term) || x.Word.Meanings.Any(m => m.DefinitionVi.ToLower().Contains(term)))) ||
                (x.GrammarRule != null && (x.GrammarRule.Title.ToLower().Contains(term) || x.GrammarRule.Structure.ToLower().Contains(term) || x.GrammarRule.ExplanationVi.ToLower().Contains(term))) ||
                (x.Note != null && x.Note.ToLower().Contains(term)));
        }

        return query;
    }

    public async Task<IReadOnlySet<Guid>> GetStoredWordIdsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await DbSet
            .AsNoTracking()
            .Where(x => x.ItemType == StorageItemType.Vocabulary && x.WordId.HasValue)
            .Select(x => x.WordId!.Value)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public async Task<IReadOnlySet<Guid>> GetStoredGrammarRuleIdsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await DbSet
            .AsNoTracking()
            .Where(x => x.ItemType == StorageItemType.Grammar && x.GrammarRuleId.HasValue)
            .Select(x => x.GrammarRuleId!.Value)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }
}
