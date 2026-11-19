namespace Lexora.Application.DTOs;

public enum StudyAdviceActionType
{
    UrgentReview,
    WeakWordsQuiz,
    DailyMaintenance,
    MilestonePractice
}

public record StudyAdviceSummaryDto(
    int UrgentReviewCount,
    int WeakWordsCount,
    int OverdueCardsCount,
    double OverallRetentionScore,
    StudyAdviceActionType RecommendedAction,
    string AdviceTitleVi,
    string AdviceTitleEn,
    string AdviceDescriptionVi,
    string AdviceDescriptionEn,
    string ActionButtonTextVi,
    string ActionButtonTextEn,
    string? TargetExamContext,
    double ExamReadinessPercent = 0.0,
    int LeechCardsCount = 0
);

public record TimeBudgetPlanDto(
    int AvailableMinutes,
    int UrgentReviewCount,
    int LeechDrillCount,
    int NewWordsCount,
    int EstimatedTotalCards,
    double ExamReadinessPercent,
    string SummaryVi,
    string SummaryEn
);
