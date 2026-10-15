using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Application.SpacedRepetition;

public record FsrsCardState(
    double Stability,
    double Difficulty,
    double Retrievability,
    int IntervalDays,
    int Repetitions,
    int Lapses,
    LearningStatus Status,
    DateTime NextReviewAt
);

public interface IFsrsSpacedRepetitionEngine : ISpacedRepetitionEngine
{
    FsrsCardState CalculateFsrsNextReview(ReviewCard card, ReviewRating rating, DateTime? reviewTimeUtc = null, double desiredRetention = 0.90);
    double CalculateStability(ReviewCard card);
    double CalculateDifficulty(ReviewCard card);
}
