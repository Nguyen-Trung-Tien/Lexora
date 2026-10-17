using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record LearningStatusDistributionDto(
    LearningStatus Status,
    int Count,
    double Percentage,
    string ColorHex,
    string DisplayName
);

public record ExamProgressDto(
    ExamType Exam,
    int TotalWords,
    int MasteredWords,
    int LearningWords,
    double ProgressPercentage,
    string? CustomExamName = null,
    string? CustomTargetDescription = null
)
{
    public string ExamName => CustomExamName ?? Exam switch
    {
        ExamType.Toeic => "TOEIC Preparation",
        ExamType.Vstep => "VSTEP B1-C1",
        ExamType.Ielts => "IELTS Academic",
        _ => "General Vocabulary"
    };

    public string TargetDescription => CustomTargetDescription ?? Exam switch
    {
        ExamType.Toeic => "Target score 600 - 850+",
        ExamType.Vstep => "Target level B1 - C1",
        ExamType.Ielts => "Target band 6.5 - 8.0+",
        _ => "Daily English Mastery"
    };
}

public record CefrLevelDistributionDto(
    CefrLevel Level,
    int Count,
    double Percentage
);

public record PosDistributionDto(
    PartOfSpeech PartOfSpeech,
    int Count,
    double Percentage
);

public record TopicProgressDto(
    string TopicName,
    string? IconGlyph,
    string? ColorHex,
    int TotalWords,
    int MasteredWords,
    double ProgressPercentage
);

public record StudyHistoryDayDto(
    DateOnly Date,
    string DayOfWeek,
    int ReviewsCount,
    int CorrectCount,
    int HeatmapLevel
)
{
    public string ColorHex => HeatmapLevel switch
    {
        4 => "#216E39", // Deep green
        3 => "#30A14E", // Medium green
        2 => "#40C463", // Light green
        1 => "#9BE9A8", // Pale green
        _ => "#E1DFDD"  // Neutral empty
    };

    public string TextColorHex => HeatmapLevel >= 3 ? "#FFFFFF" : "#202020";

    public string TooltipText => $"{Date:dd/MM/yyyy}: {ReviewsCount} reviews ({CorrectCount} correct)";
}

public record RetentionCurvePointDto(
    int IntervalDays,
    double RetentionRate,
    int WordCount
)
{
    public string Label => IntervalDays switch
    {
        <= 1 => "Day 1",
        <= 3 => "Day 3",
        <= 7 => "Day 7",
        <= 14 => "Day 14",
        <= 30 => "Day 30",
        _ => "60d+"
    };

    public double TheoreticalRate => IntervalDays switch
    {
        <= 1 => 33.7,
        <= 3 => 27.8,
        <= 7 => 25.4,
        <= 14 => 21.1,
        <= 30 => 18.5,
        _ => 15.0
    };

    public double RetentionAdvantage => Math.Max(0, RetentionRate - TheoreticalRate);
    public string RetentionAdvantageDisplay => $"+{RetentionAdvantage:F1}%";
    public string ActualRateDisplay => $"{RetentionRate:F1}%";
    public string TheoreticalRateDisplay => $"{TheoreticalRate:F1}%";
    public string WordCountDisplay => $"{WordCount} words";
}

public record DetailedAnalyticsDto(
    DashboardSummaryDto Summary,
    List<LearningStatusDistributionDto> StatusBreakdown,
    List<ExamProgressDto> ExamProgress,
    List<CefrLevelDistributionDto> CefrDistribution,
    List<PosDistributionDto> PosDistribution,
    List<TopicProgressDto> TopicProgress,
    List<StudyHistoryDayDto> Last30DaysHeatmap,
    List<RetentionCurvePointDto> RetentionCurve,
    List<StudySessionDto> RecentSessions
);
