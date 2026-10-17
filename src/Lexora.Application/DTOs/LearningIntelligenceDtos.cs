using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record TimeOfDayEfficiencyDto(
    string SlotKey,
    string SlotNameVi,
    string SlotNameEn,
    string HoursRange,
    int ReviewsCount,
    double AccuracyRate,
    int AverageResponseTimeMs,
    bool IsPeakPerformanceSlot
);

public record WeeklyMasteryVelocityDto(
    DateOnly WeekStartDate,
    string WeekLabel,
    int WordsLearned,
    int WordsMastered,
    int NetGrowth
);

public record ExamReadinessIndexDto(
    ExamType Exam,
    string ExamTitle,
    double ReadinessScore, // 0 - 100%
    string TargetScoreRange,
    int MasteredRequiredWords,
    int TotalRequiredWords,
    string ReadinessAssessmentVi,
    string ReadinessAssessmentEn
);

public record MemoryHealthSegmentDto(
    string CategoryKey,
    string TitleVi,
    string TitleEn,
    int WordsCount,
    double Percentage,
    string DescriptionVi,
    string DescriptionEn,
    string ColorHex
)
{
    public string StatusName => TitleVi;
    public int WordCount => WordsCount;
    public string Recommendation => DescriptionVi;
};

public record LearningIntelligenceReportDto(
    double GlobalRetentionIndex,
    double AverageRecallSpeedSec,
    int CurrentMemoryHalfLifeDays,
    IReadOnlyList<TimeOfDayEfficiencyDto> CognitiveTimeEfficiency,
    IReadOnlyList<WeeklyMasteryVelocityDto> WeeklyVelocity,
    IReadOnlyList<ExamReadinessIndexDto> ExamReadiness,
    IReadOnlyList<MemoryHealthSegmentDto> MemoryHealthBreakdown,
    IReadOnlyList<RetentionCurvePointDto> EmpiricalVsTheoreticalRetention,
    string HighImpactRecommendationVi,
    string HighImpactRecommendationEn
);
