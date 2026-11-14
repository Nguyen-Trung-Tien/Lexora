using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public enum CurriculumTaskType
{
    ReviewDueCards = 0,
    LearnNewWords = 1,
    PracticeWeakWords = 2,
    StudyGrammar = 3,
    TakeAdaptiveQuiz = 4
}

public record CurriculumTaskDto(
    CurriculumTaskType Type,
    string TitleVi,
    string TitleEn,
    string DescriptionVi,
    string DescriptionEn,
    int TargetCount,
    int CompletedCount,
    bool IsCompleted,
    string NavigationTag
)
{
    public double ProgressPercent => TargetCount <= 0 ? 100.0 : Math.Clamp(Math.Round((double)CompletedCount / TargetCount * 100.0, 1), 0.0, 100.0);
    public string StatusGlyph => IsCompleted ? "\uE73E" : "\uE916";
}

public record StudyGoalDto(
    ExamType TargetExam,
    string TargetScore,
    DateOnly TargetDate,
    int DailyMinutesGoal,
    int DailyNewWordsGoal,
    int DailyReviewTarget,
    bool IsConfigured
);

public record DailyCurriculumDto(
    DateOnly PlanDate,
    int TotalEstimatedMinutes,
    int TotalTasksCount,
    int CompletedTasksCount,
    double OverallProgressPercent,
    int NewWordsTarget,
    int DueReviewsTarget,
    int WeakWordsTarget,
    Guid? RecommendedGrammarRuleId,
    string? RecommendedGrammarTitle,
    IReadOnlyList<CurriculumTaskDto> Tasks,
    string MotivationalTipVi,
    string MotivationalTipEn,
    int DaysRemaining,
    string PacingStatus
);
