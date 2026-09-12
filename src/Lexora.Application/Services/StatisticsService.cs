using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IReviewRepository _reviewRepo;

    public StatisticsService(IVocabularyRepository vocabRepo, IReviewRepository reviewRepo)
    {
        _vocabRepo = vocabRepo;
        _reviewRepo = reviewRepo;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        int totalWords = await _vocabRepo.CountAsync(cancellationToken: cancellationToken);
        var statusCounts = await _reviewRepo.GetCardCountsByStatusAsync(cancellationToken);
        var dueCards = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, 1000, cancellationToken);
        var streak = await _reviewRepo.GetUserStreakAsync(cancellationToken);
        var recentLogs = await _reviewRepo.GetRecentLogsAsync(100, cancellationToken);

        int remembered = statusCounts.GetValueOrDefault(LearningStatus.Remembered, 0);
        int notRemembered = statusCounts.GetValueOrDefault(LearningStatus.NotRemembered, 0);
        int learning = statusCounts.GetValueOrDefault(LearningStatus.Learning, 0);
        int mastered = statusCounts.GetValueOrDefault(LearningStatus.Mastered, 0);

        int totalReviews = recentLogs.Count;
        int correctReviews = recentLogs.Count(l => l.Rating >= ReviewRating.Good);
        double accuracy = totalReviews == 0 ? 0.0 : Math.Round((double)correctReviews / totalReviews * 100, 1);

        var today = DateTime.UtcNow.Date;
        int studiedToday = recentLogs.Count(l => l.CreatedAt.Date == today);

        return new DashboardSummaryDto(
            TotalWords: totalWords,
            TotalRemembered: remembered,
            TotalNotRemembered: notRemembered,
            TotalLearning: learning,
            TotalMastered: mastered,
            DueReviewsToday: dueCards.Count,
            CurrentStreakDays: streak?.CurrentStreak ?? 0,
            LongestStreakDays: streak?.LongestStreak ?? 0,
            AccuracyRate: accuracy,
            StudiedTodayCount: studiedToday
        );
    }
}
