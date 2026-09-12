using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Interfaces;

public interface IReviewRepository : IRepository<ReviewCard>
{
    Task<ReviewCard?> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewCard>> GetDueCardsAsync(DateTime asOfUtc, int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewCard>> GetNewCardsAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewCard>> GetCardsByStatusAsync(LearningStatus status, CancellationToken cancellationToken = default);
    Task<Dictionary<LearningStatus, int>> GetCardCountsByStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudyLog>> GetRecentLogsAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<UserStreak?> GetUserStreakAsync(CancellationToken cancellationToken = default);
    Task UpdateStreakAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewCard>> GetDifficultCardsAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<StudySession> CreateStudySessionAsync(StudySession session, CancellationToken cancellationToken = default);
    Task<StudySession?> GetStudySessionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudySession>> GetRecentSessionsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task UpdateStudySessionAsync(StudySession session, CancellationToken cancellationToken = default);
    void AddStudyLog(StudyLog log);
}
