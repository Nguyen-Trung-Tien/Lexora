using Lexora.Application.DTOs;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewCardDto>> GetDueCardsAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewCardDto>> GetNewCardsAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewCardDto>> GetDifficultCardsAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewCardDto>> GetCardsByTopicIdAsync(Guid topicId, int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewCardDto>> GetCardsByWordIdsAsync(IReadOnlyList<Guid> wordIds, CancellationToken cancellationToken = default);
    Task<int> GetDueCardsCountAsync(CancellationToken cancellationToken = default);

    Task<StudySessionDto> StartSessionAsync(ReviewSessionMode mode, int limit = 30, CancellationToken cancellationToken = default);
    Task<StudySessionDto> CompleteSessionAsync(Guid sessionId, int totalCards, int correctCount, int incorrectCount, CancellationToken cancellationToken = default);

    Task<ReviewEvaluationResultDto> SubmitReviewAsync(Guid reviewCardId, ReviewRating rating, int responseTimeMs = 0, Guid? sessionId = null, CancellationToken cancellationToken = default);
    Task MarkStatusQuickAsync(Guid wordId, bool remembered, CancellationToken cancellationToken = default);

    IntervalPreviewDto GetCardIntervalPreview(ReviewCardDto card);
    Task<IReadOnlyList<StudySessionDto>> GetRecentSessionsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudyLogDto>> GetRecentLogsAsync(int limit = 20, CancellationToken cancellationToken = default);
}
