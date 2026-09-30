using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces;

public interface ILearningStorageService
{
    Task<StoragePagedResultDto> GetPagedItemsAsync(
        StorageItemType? itemType = null,
        StorageLearningStatus? status = null,
        string? search = null,
        bool? dueTodayOnly = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<StorageStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<LearningStorageItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LearningStorageItemDto?> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default);

    Task<LearningStorageItemDto?> GetByGrammarRuleIdAsync(Guid grammarRuleId, CancellationToken cancellationToken = default);

    Task<LearningStorageItemDto> MoveWordToStorageAsync(
        Guid wordId,
        StorageAdditionReason reason = StorageAdditionReason.Manual,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task<LearningStorageItemDto> MoveGrammarToStorageAsync(
        Guid grammarRuleId,
        StorageAdditionReason reason = StorageAdditionReason.Manual,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task<int> MoveWordsToStorageAsync(
        IEnumerable<Guid> wordIds,
        StorageAdditionReason reason = StorageAdditionReason.Manual,
        CancellationToken cancellationToken = default);

    Task<int> MoveGrammarsToStorageAsync(
        IEnumerable<Guid> grammarRuleIds,
        StorageAdditionReason reason = StorageAdditionReason.Manual,
        CancellationToken cancellationToken = default);

    Task RemoveFromStorageAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> RemoveRangeFromStorageAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task MarkAsMasteredAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> MarkRangeAsMasteredAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(Guid id, StorageLearningStatus newStatus, CancellationToken cancellationToken = default);

    Task UpdateNoteAsync(Guid id, string? note, CancellationToken cancellationToken = default);

    Task AutoCheckAndAddToStorageAsync(
        Guid wordId,
        int consecutiveErrors,
        double retentionRate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoragePracticeQuestionDto>> GeneratePracticeAsync(
        StorageItemType itemType,
        int limit = 15,
        bool prioritizeWeak = true,
        CancellationToken cancellationToken = default);

    Task<StoragePracticeResultDto> EvaluatePracticeAsync(
        IReadOnlyList<StoragePracticeQuestionDto> questions,
        IReadOnlyList<StoragePracticeSubmissionDto> answers,
        CancellationToken cancellationToken = default);

    Task<TestSessionDto> GenerateStorageTestAsync(
        int questionCount = 10,
        StorageItemType? itemType = null,
        bool prioritizeWeak = true,
        CancellationToken cancellationToken = default);
}
