using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public enum SmartReviewPresetMode
{
    StandardDue = 0,
    SpeedBlitz = 1,
    LeechRescue = 2,
    HighYieldExam = 3,
    ContextImmersion = 4,
    UrgentOverdue = 5,
    HighForgettingRisk = 6,
    ExamHighYield = 7,
    FreshLearned = 8,
    LongTermConsolidation = 9
}

public record MemoryStabilityMetricsDto(
    Guid WordId,
    string Term,
    double StabilityDays,
    double RetrievabilityPercent,
    double EaseFactor,
    int Repetitions,
    int Lapses,
    string IntervalIfAgain,
    string IntervalIfHard,
    string IntervalIfGood,
    string IntervalIfEasy,
    string MemoryHealthLevel // Fragile, Developing, Stable, Permanent
)
{
    public string HealthLevel => MemoryHealthLevel;
    public double HalfLifeDays => StabilityDays;
    public string OptimalRecallWindow => $"Ease: {EaseFactor:F2}";
}

public record SmartReviewPresetInfo(
    SmartReviewPresetMode Mode,
    string TitleVi,
    string TitleEn,
    string DescriptionVi,
    string DescriptionEn,
    string IconGlyph,
    int AvailableCardsCount
);

public record SmartReviewPresetDisplayItem(
    SmartReviewPresetMode Mode,
    string Title,
    string Description,
    string IconGlyph,
    int AvailableCardsCount,
    string BadgeText
);

public record SmartReviewSessionSummaryDto(
    Guid SessionId,
    SmartReviewPresetMode Mode,
    int TotalCards,
    int CorrectCards,
    int IncorrectCards,
    double AccuracyPercentage,
    int AverageResponseTimeMs,
    int WordsPromotedToMastered,
    int WordsRecovered,
    IReadOnlyList<WordDto> LapsedWords,
    DateTime CompletedAt
);
