using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record ReviewCardDto(
    Guid Id,
    Guid WordId,
    string Term,
    string? PhoneticUk,
    string? PhoneticUs,
    string? AudioUkPath,
    string? AudioUsPath,
    PartOfSpeech PrimaryPartOfSpeech,
    CefrLevel Level,
    string PrimaryDefinitionVi,
    string? PrimaryDefinitionEn,
    List<ExampleSentenceDto> Examples,
    LearningStatus Status,
    double EaseFactor,
    int IntervalDays,
    int Repetitions,
    int Lapses,
    DateTime? LastReviewedAt,
    DateTime NextReviewAt,
    int TotalReviews,
    int CorrectReviews,
    double Confidence
);

public record ReviewEvaluationResultDto(
    Guid ReviewCardId,
    ReviewRating Rating,
    LearningStatus PreviousStatus,
    LearningStatus NewStatus,
    int NewIntervalDays,
    DateTime NextReviewAt
);

public enum ReviewSessionMode
{
    TodayDue = 0,
    LearnNew = 1,
    Difficult = 2
}

public record StudySessionDto(
    Guid Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalCards,
    int CorrectCards,
    int IncorrectCards,
    double AccuracyRate,
    string? SessionType
);

public record StudyLogDto(
    Guid Id,
    Guid ReviewCardId,
    string Term,
    ReviewRating Rating,
    int ResponseTimeMs,
    LearningStatus ResultingStatus,
    int ResultingIntervalDays,
    DateTime CreatedAt
);
