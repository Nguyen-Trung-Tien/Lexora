using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Application.SpacedRepetition;

/// <summary>
/// Modern Free Spaced Repetition Scheduler (FSRS v5) Engine.
/// Models human memory based on three components:
/// - Stability (S): Duration in days for recall probability to drop to 90%.
/// - Difficulty (D): Inherent complexity of card (1.0 to 10.0).
/// - Retrievability (R): Current probability of successfully recalling the item.
/// </summary>
public class FsrsSpacedRepetitionEngine : IFsrsSpacedRepetitionEngine
{
    private const double DefaultDesiredRetention = 0.90;
    private const double MinStability = 0.1;
    private const double MaxStability = 3650.0;
    private const double MinDifficulty = 1.0;
    private const double MaxDifficulty = 10.0;

    // FSRS default calibrated parameters
    private static readonly double[] W =
    {
        0.4072, 1.1827, 3.1262, 15.4722,  // Initial stabilities for Again, Hard, Good, Easy
        7.2102,                            // Initial difficulty base
        0.5316,                            // Difficulty grade adjustment factor
        1.0651,                            // Mean reversion factor
        0.0234,                            // Stability recall decay
        1.6160,                            // Stability recall power
        0.1544,                            // Stability lapse base
        1.0824,                            // Stability lapse difficulty power
        1.9813,                            // Stability lapse stability power
        0.0953                             // Stability lapse retrievability factor
    };

    public SpacedRepetitionResult CalculateNextReview(ReviewCard card, ReviewRating rating, DateTime? reviewTimeUtc = null)
    {
        var fsrsState = CalculateFsrsNextReview(card, rating, reviewTimeUtc, DefaultDesiredRetention);

        // Map FSRS stability to SM-2 EaseFactor equivalent for backward compatibility
        // EaseFactor in SM-2 is 1.3 to 2.5+. A difficulty of 5.0 maps to ~2.5 EF.
        double mappedEaseFactor = Math.Clamp(1.3 + (10.0 - fsrsState.Difficulty) * 0.15, 1.3, 3.0);
        mappedEaseFactor = Math.Round(mappedEaseFactor, 2);

        return new SpacedRepetitionResult(
            NewEaseFactor: mappedEaseFactor,
            NewIntervalDays: fsrsState.IntervalDays,
            NewRepetitions: fsrsState.Repetitions,
            NewLapses: fsrsState.Lapses,
            NewStatus: fsrsState.Status,
            NextReviewAt: fsrsState.NextReviewAt
        );
    }

    public FsrsCardState CalculateFsrsNextReview(ReviewCard card, ReviewRating rating, DateTime? reviewTimeUtc = null, double desiredRetention = DefaultDesiredRetention)
    {
        var now = reviewTimeUtc ?? DateTime.UtcNow;
        int grade = (int)rating; // ReviewRating enum: Again = 1, Hard = 2, Good = 3, Easy = 4
        grade = Math.Clamp(grade, 1, 4);

        int reps = card.Repetitions;
        int lapses = card.Lapses;
        double currentD = CalculateDifficulty(card);
        double currentS = CalculateStability(card);

        double newD;
        double newS;

        if (reps == 0 || card.Status == LearningStatus.New)
        {
            // First learning step
            newS = W[grade - 1];
            newD = Math.Clamp(W[4] - Math.Exp(W[5] * (grade - 1)) + 1, MinDifficulty, MaxDifficulty);
            reps = 1;
            if (grade == 1) lapses++;
        }
        else
        {
            // Review step: Calculate elapsed days since last review
            var lastReview = card.LastReviewedAt ?? (card.CreatedAt != default ? card.CreatedAt : now.AddDays(-Math.Max(1, card.IntervalDays)));
            double elapsedDays = Math.Max(0.0, (now - lastReview).TotalDays);
            double retrievability = CalculateRetrievability(currentS, elapsedDays);

            // Update difficulty: D' = D + deltaD
            double deltaD = -W[5] * (grade - 3);
            newD = Math.Clamp(W[6] * W[4] + (1 - W[6]) * (currentD + deltaD), MinDifficulty, MaxDifficulty);

            if (grade == 1)
            {
                // Lapse
                lapses++;
                reps = 0;
                newS = Math.Clamp(
                    W[9] * Math.Pow(currentD, -W[10]) * Math.Pow(currentS, W[11]) * Math.Exp((1 - retrievability) * W[12]),
                    MinStability,
                    Math.Max(MinStability, currentS * 0.5)
                );
            }
            else
            {
                // Recall success
                reps++;
                double hardPenalty = grade == 2 ? 0.8 : 1.0;
                double easyBonus = grade == 4 ? 1.3 : 1.0;
                double minMultiplier = grade == 2 ? 1.05 : (grade == 3 ? 1.2 : 1.4);

                double stabilityGrowth = Math.Exp(W[7]) * (11 - newD) * Math.Pow(currentS, -W[8]) * (Math.Exp((1 - retrievability) * 0.1) - 1) * hardPenalty * easyBonus;

                newS = Math.Clamp(
                    currentS * Math.Max(minMultiplier, 1 + stabilityGrowth),
                    MinStability,
                    MaxStability
                );
            }
        }

        newS = Math.Clamp(Math.Round(newS, 2), MinStability, MaxStability);
        newD = Math.Clamp(Math.Round(newD, 2), MinDifficulty, MaxDifficulty);

        // Optimal interval based on desired retention (Again resets interval to 1 day):
        int intervalDays = grade == 1 ? 1 : Math.Max(1, (int)Math.Round(CalculateInterval(newS, desiredRetention)));

        LearningStatus status = grade == 1
            ? LearningStatus.NotRemembered
            : (reps >= 5 || newS >= 21.0 ? LearningStatus.Mastered : (reps >= 2 ? LearningStatus.Remembered : LearningStatus.Learning));

        var nextReviewAt = now.Date.AddDays(intervalDays);
        double finalR = CalculateRetrievability(newS, 0);

        return new FsrsCardState(
            Stability: newS,
            Difficulty: newD,
            Retrievability: finalR,
            IntervalDays: intervalDays,
            Repetitions: reps,
            Lapses: lapses,
            Status: status,
            NextReviewAt: nextReviewAt
        );
    }

    public double CalculateStability(ReviewCard card)
    {
        if (card.Status == LearningStatus.New || card.Repetitions == 0)
        {
            return W[2]; // Default initial Good stability
        }

        // Approximate stability from existing interval days and ease factor
        double s = Math.Max(1.0, (double)card.IntervalDays);
        if (card.EaseFactor > 2.5)
        {
            s *= (1.0 + (card.EaseFactor - 2.5) * 0.2);
        }
        return Math.Clamp(Math.Round(s, 2), MinStability, MaxStability);
    }

    public double CalculateDifficulty(ReviewCard card)
    {
        if (card.EaseFactor <= 0) return 5.0;
        // Invert EaseFactor (1.3 to 2.5+) to Difficulty (1.0 to 10.0)
        // High EF (2.8) -> Low Difficulty (2.0)
        // Low EF (1.3) -> High Difficulty (9.0)
        double d = 10.0 - ((card.EaseFactor - 1.3) / (2.8 - 1.3) * 8.0);
        return Math.Clamp(Math.Round(d, 2), MinDifficulty, MaxDifficulty);
    }

    public IntervalPreviewDto GetIntervalPreview(ReviewCard card, DateTime? reviewTimeUtc = null)
    {
        var again = CalculateFsrsNextReview(card, ReviewRating.Again, reviewTimeUtc);
        var hard = CalculateFsrsNextReview(card, ReviewRating.Hard, reviewTimeUtc);
        var good = CalculateFsrsNextReview(card, ReviewRating.Good, reviewTimeUtc);
        var easy = CalculateFsrsNextReview(card, ReviewRating.Easy, reviewTimeUtc);

        return new IntervalPreviewDto(
            AgainText: FormatInterval(again.IntervalDays),
            HardText: FormatInterval(hard.IntervalDays),
            GoodText: FormatInterval(good.IntervalDays),
            EasyText: FormatInterval(easy.IntervalDays)
        );
    }

    public double CalculateRetentionProbability(ReviewCard card, DateTime? asOfUtc = null)
    {
        if (card.Status == LearningStatus.New || card.Repetitions == 0)
        {
            return 50.0;
        }

        var now = asOfUtc ?? DateTime.UtcNow;
        var lastReview = card.LastReviewedAt ?? card.CreatedAt;
        double elapsedDays = Math.Max(0.0, (now - lastReview).TotalDays);
        double s = CalculateStability(card);

        double r = CalculateRetrievability(s, elapsedDays);
        return Math.Clamp(Math.Round(r * 100.0, 1), 5.0, 100.0);
    }

    private static double CalculateRetrievability(double stability, double elapsedDays)
    {
        if (stability <= 0) return 0.50;
        // Standard FSRS forgetting curve formula: R(t, S) = (1 + (19/81) * (t / S))^(-0.5)
        const double factor = 19.0 / 81.0;
        const double decay = -0.5;
        double r = Math.Pow(1.0 + factor * (elapsedDays / stability), decay);
        return Math.Clamp(r, 0.05, 1.0);
    }

    private static double CalculateInterval(double stability, double desiredRetention)
    {
        const double factor = 19.0 / 81.0;
        const double decay = -0.5;
        // Solving R(I, S) = desiredRetention for I:
        // (1 + factor * (I / S))^decay = r
        // 1 + factor * (I / S) = r^(1 / decay)
        // I = (S / factor) * (r^(1 / decay) - 1)
        double r = Math.Clamp(desiredRetention, 0.70, 0.98);
        double interval = (stability / factor) * (Math.Pow(r, 1.0 / decay) - 1.0);
        return Math.Max(1.0, interval);
    }

    private static string FormatInterval(int days)
    {
        return days switch
        {
            <= 1 => "< 1d",
            _ => $"{days}d"
        };
    }
}
