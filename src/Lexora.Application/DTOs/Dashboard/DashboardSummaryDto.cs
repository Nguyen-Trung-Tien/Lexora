namespace Lexora.Application.DTOs;

public record DashboardSummaryDto(
    int TotalWords,
    int TotalRemembered,
    int TotalNotRemembered,
    int TotalLearning,
    int TotalMastered,
    int DueReviewsToday,
    int CurrentStreakDays,
    int LongestStreakDays,
    double AccuracyRate,
    int StudiedTodayCount
);
