using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Interfaces;

public interface ILearningStorageRepository : IRepository<LearningStorageItem>
{
    Task<LearningStorageItem?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LearningStorageItem?> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default);
    Task<LearningStorageItem?> GetByGrammarRuleIdAsync(Guid grammarRuleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LearningStorageItem>> GetFilteredPagedAsync(
        StorageItemType? itemType,
        StorageLearningStatus? status,
        string? search,
        bool? dueTodayOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<int> CountFilteredAsync(
        StorageItemType? itemType,
        StorageLearningStatus? status,
        string? search,
        bool? dueTodayOnly,
        CancellationToken cancellationToken = default);
    Task<(int Total, int VocabCount, int GrammarCount, int NewCount, int LearningCount, int WeakCount, int MasteredCount, int DueTodayCount, double AvgRetention)> GetStatsAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LearningStorageItem>> GetItemsForPracticeAsync(
        StorageItemType itemType,
        int limit,
        bool prioritizeWeak,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LearningStorageItem>> GetItemsForTestAsync(
        StorageItemType? itemType,
        int limit,
        bool prioritizeWeak,
        CancellationToken cancellationToken = default);
    Task<int> DeleteRangeAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetStoredWordIdsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetStoredGrammarRuleIdsAsync(CancellationToken cancellationToken = default);
}
