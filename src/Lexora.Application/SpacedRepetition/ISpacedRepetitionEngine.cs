using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Application.SpacedRepetition;

public record SpacedRepetitionResult(
    double NewEaseFactor,
    int NewIntervalDays,
    int NewRepetitions,
    int NewLapses,
    LearningStatus NewStatus,
    DateTime NextReviewAt
);

public record IntervalPreviewDto(
    string AgainText,
    string HardText,
    string GoodText,
    string EasyText
);

public interface ISpacedRepetitionEngine
{
    SpacedRepetitionResult CalculateNextReview(ReviewCard card, ReviewRating rating, DateTime? reviewTimeUtc = null);
    IntervalPreviewDto GetIntervalPreview(ReviewCard card, DateTime? reviewTimeUtc = null);
}
