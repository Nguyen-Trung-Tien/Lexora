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
)
{
    /// <summary>Friendly status label shown in SM-2 Insights panel.</summary>
    public string StatusLabel => Status switch
    {
        LearningStatus.New           => "New",
        LearningStatus.Learning      => "Learning",
        LearningStatus.Remembered    => "Remembered",
        LearningStatus.Mastered      => "Mastered",
        LearningStatus.NotRemembered => "Forgotten",
        _                            => Status.ToString()
    };

    /// <summary>Formatted confidence string, e.g. "50%".</summary>
    public string ConfidenceText => $"{Confidence:F0}%";

    /// <summary>Formatted ease factor, e.g. "2.50".</summary>
    public string EaseFactorText => EaseFactor.ToString("F2");

    /// <summary>Compact interval label, e.g. "< 1 d" or "6 d".</summary>
    public string IntervalLabel => IntervalDays <= 0 ? "< 1 d" : $"{IntervalDays} d";
}


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
    Difficult = 2,
    ByTopic = 3,
    CustomSelection = 4
}

public record StudySessionDto(
    Guid Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalCards,
    int CorrectCards,
    int IncorrectCards,
    double AccuracyRate,
    string? SessionType,
    string? CustomDurationText = null
)
{
    public string StartedAtText => StartedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public string DurationText => CustomDurationText ?? (CompletedAt.HasValue
        ? $"{(CompletedAt.Value - StartedAt).Minutes:D2}:{(CompletedAt.Value - StartedAt).Seconds:D2}"
        : (System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ? "Đang diễn ra" : "In progress"));
}

public record StudyLogDto(
    Guid Id,
    Guid ReviewCardId,
    string Term,
    ReviewRating Rating,
    int ResponseTimeMs,
    LearningStatus ResultingStatus,
    int ResultingIntervalDays,
    DateTime CreatedAt
)
{
    /// <summary>Human-readable rating label shown on the badge.</summary>
    public string RatingLabel => Rating switch
    {
        ReviewRating.Again => "Again",
        ReviewRating.Hard  => "Hard",
        ReviewRating.Good  => "Good",
        ReviewRating.Easy  => "Easy",
        _                  => Rating.ToString()
    };

    /// <summary>Segoe Fluent icon glyph per rating.</summary>
    public string RatingIcon => Rating switch
    {
        ReviewRating.Again => "\uE711",  // Cancel / repeat
        ReviewRating.Hard  => "\uE74E",  // Pause
        ReviewRating.Good  => "\uE73E",  // Accept / check
        ReviewRating.Easy  => "\uE735",  // FavoriteStar
        _                  => "\uE7C1"
    };

    /// <summary>Human-readable status label (no raw enum name).</summary>
    public string StatusLabel => ResultingStatus switch
    {
        LearningStatus.New          => "New",
        LearningStatus.Learning     => "Learning",
        LearningStatus.Remembered   => "Remembered",
        LearningStatus.Mastered     => "Mastered",
        LearningStatus.NotRemembered => "Forgotten",
        _                           => ResultingStatus.ToString()
    };

    /// <summary>Concise next-interval label, e.g. "1 d" or "15 d".</summary>
    public string IntervalLabel => ResultingIntervalDays <= 0
        ? "< 1 d"
        : $"{ResultingIntervalDays} d";

    /// <summary>Relative human-readable timestamp, e.g. "2 min ago", "Yesterday".</summary>
    public string RelativeTime
    {
        get
        {
            var diff = DateTime.UtcNow - CreatedAt.ToUniversalTime();
            if (diff.TotalMinutes < 1)   return "Just now";
            if (diff.TotalMinutes < 60)  return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24)    return $"{(int)diff.TotalHours} h ago";
            if (diff.TotalDays < 2)      return "Yesterday";
            if (diff.TotalDays < 7)      return $"{(int)diff.TotalDays} days ago";
            return CreatedAt.ToLocalTime().ToString("dd/MM HH:mm");
        }
    }
};
