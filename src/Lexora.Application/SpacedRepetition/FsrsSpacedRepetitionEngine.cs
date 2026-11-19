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

    // FSRS v5 19-parameter calibrated weight vector
    private static readonly double[] W =
    {
        0.40255, 1.18385, 3.17300, 15.69105, // W[0..3]: Initial stabilities for Again, Hard, Good, Easy
        7.19490,                             // W[4]: Initial difficulty base
        0.53450,                             // W[5]: Initial difficulty grade adjustment
        1.46040,                             // W[6]: Difficulty delta factor
        0.00460,                             // W[7]: Difficulty mean reversion factor
        1.54575,                             // W[8]: Recall stability growth exponent
        0.11920,                             // W[9]: Recall stability power decay
        1.01925,                             // W[10]: Recall retrievability sensitivity
        1.93950,                             // W[11]: Lapse stability base
        0.11000,                             // W[12]: Lapse difficulty power
        0.29605,                             // W[13]: Lapse stability power
        2.26980,                             // W[14]: Lapse retrievability sensitivity
        0.23150,                             // W[15]: Hard penalty factor
        2.98980,                             // W[16]: Easy bonus factor
        0.51655,                             // W[17]: Same-day short-term stability scale
        0.66210                              // W[18]: Same-day short-term grade offset
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
            // First learning step (FSRS-5 initial stability & difficulty)
            newS = W[grade - 1];
            newD = Math.Clamp(W[4] - Math.Exp(W[5] * (grade - 1)) + 1.0, MinDifficulty, MaxDifficulty);
            reps = 1;
            if (grade == 1) lapses++;
        }
        else
        {
            // Review step: Calculate elapsed days since last review
            var lastReview = card.LastReviewedAt ?? (card.CreatedAt != default ? card.CreatedAt : now.AddDays(-Math.Max(1, card.IntervalDays)));
            double elapsedDays = Math.Max(0.0, (now - lastReview).TotalDays);
            double retrievability = CalculateRetrievability(currentS, elapsedDays);

            // FSRS-5 Difficulty update with mean reversion toward D0(4)
            double d0Easy = W[4] - Math.Exp(W[5] * 3) + 1.0;
            double deltaD = -W[6] * (grade - 3);
            double nextD = currentD + deltaD * ((10.0 - currentD) / 9.0);
            newD = Math.Clamp(W[7] * d0Easy + (1.0 - W[7]) * nextD, MinDifficulty, MaxDifficulty);

            if (card.LastReviewedAt.HasValue && elapsedDays < 1.0)
            {
                // FSRS-5 Same-day short-term stability formula
                newS = Math.Clamp(
                    currentS * Math.Exp(W[17] * (grade - 3 + W[18])),
                    MinStability,
                    MaxStability
                );
                if (grade == 1)
                {
                    lapses++;
                    reps = 0;
                }
                else
                {
                    reps++;
                }
            }
            else if (grade == 1)
            {
                // Lapse
                lapses++;
                reps = 0;
                newS = Math.Clamp(
                    W[11] * Math.Pow(newD, -W[12]) * (Math.Pow(currentS + 1.0, W[13]) - 1.0) * Math.Exp(W[14] * (1.0 - retrievability)),
                    MinStability,
                    Math.Max(MinStability, currentS * 0.6)
                );
            }
            else
            {
                // Long-term recall success
                reps++;
                double hardPenalty = grade == 2 ? W[15] : 1.0;
                double easyBonus = grade == 4 ? W[16] : 1.0;
                double minMultiplier = grade == 2 ? 1.05 : (grade == 3 ? 1.15 : 1.35);

                double stabilityGrowth = Math.Exp(W[8])
                    * (11.0 - newD)
                    * Math.Pow(currentS, -W[9])
                    * (Math.Exp(W[10] * (1.0 - retrievability)) - 1.0)
                    * hardPenalty
                    * easyBonus;

                newS = Math.Clamp(
                    currentS * Math.Max(minMultiplier, 1.0 + stabilityGrowth),
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
        if (card.Stability > 0.01)
        {
            return Math.Clamp(Math.Round(card.Stability, 2), MinStability, MaxStability);
        }

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
        if (card.Stability > 0.01 && card.Difficulty >= MinDifficulty && card.Difficulty <= MaxDifficulty)
        {
            return Math.Clamp(Math.Round(card.Difficulty, 2), MinDifficulty, MaxDifficulty);
        }

        if (card.EaseFactor <= 0) return 5.0;
        // Invert EaseFactor (1.3 to 2.5+) to Difficulty (1.0 to 10.0)
        // High EF (2.8) -> Low Difficulty (2.0)
        // Low EF (1.3) -> High Difficulty (9.0)
        double d = 10.0 - ((card.EaseFactor - 1.3) / (2.8 - 1.3) * 8.0);
        return Math.Clamp(Math.Round(d, 2), MinDifficulty, MaxDifficulty);
    }

    public IntervalPreviewDto GetIntervalPreview(ReviewCard card, DateTime? reviewTimeUtc = null)
        => GetIntervalPreview(card, reviewTimeUtc, card.DesiredRetention ?? DefaultDesiredRetention);

    public IntervalPreviewDto GetIntervalPreview(ReviewCard card, DateTime? reviewTimeUtc, double desiredRetention)
    {
        double targetR = Math.Clamp(desiredRetention, 0.70, 0.98);
        var again = CalculateFsrsNextReview(card, ReviewRating.Again, reviewTimeUtc, targetR);
        var hard = CalculateFsrsNextReview(card, ReviewRating.Hard, reviewTimeUtc, targetR);
        var good = CalculateFsrsNextReview(card, ReviewRating.Good, reviewTimeUtc, targetR);
        var easy = CalculateFsrsNextReview(card, ReviewRating.Easy, reviewTimeUtc, targetR);

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

    public bool IsLeechCard(ReviewCard card)
    {
        if (card.Lapses >= 6) return true;
        return card.Lapses >= 4 && card.TotalReviews > 0 && card.Confidence < 40.0;
    }

    public double CalculatePriorityScore(ReviewCard card, DateTime? asOfUtc = null, ExamTarget? activeExamTarget = null)
    {
        var now = asOfUtc ?? DateTime.UtcNow;
        double retentionPct = CalculateRetentionProbability(card, now);
        double targetPct = (card.DesiredRetention ?? DefaultDesiredRetention) * 100.0;

        // Cards near or just below target retention get highest priority; leeches get a modest boost
        double urgency = Math.Max(0.0, targetPct - retentionPct);
        double overdueDays = Math.Max(0.0, (now - card.NextReviewAt).TotalDays);
        double leechBoost = IsLeechCard(card) ? 12.0 : 0.0;

        double examBoost = 0.0;
        if (activeExamTarget.HasValue && card.Word?.WordExamTags != null && card.Word.WordExamTags.Count > 0)
        {
            var mappedExam = activeExamTarget.Value switch
            {
                ExamTarget.Toeic => ExamType.Toeic,
                ExamTarget.Ielts => ExamType.Ielts,
                ExamTarget.Vstep => ExamType.Vstep,
                _ => (ExamType?)null
            };

            if (mappedExam.HasValue && card.Word.WordExamTags.Any(t => t.ExamTag?.Type == mappedExam.Value))
            {
                examBoost = 15.0;
            }
        }

        return Math.Round(urgency * 1.5 + Math.Min(30.0, overdueDays * 3.0) + leechBoost + examBoost, 2);
    }

    public int ApplyWorkloadFuzz(int intervalDays, IReadOnlyDictionary<DateOnly, int>? dueForecast, DateTime? reviewTimeUtc = null)
    {
        if (intervalDays < 4 || dueForecast == null || dueForecast.Count == 0)
        {
            return intervalDays;
        }

        int fuzzRadius = Math.Clamp((int)Math.Round(intervalDays * 0.05), 1, 3);
        int minInterval = Math.Max(4, intervalDays - fuzzRadius);
        int maxInterval = intervalDays + fuzzRadius;

        var now = reviewTimeUtc ?? DateTime.UtcNow;
        int bestInterval = intervalDays;
        int lowestLoad = dueForecast.GetValueOrDefault(DateOnly.FromDateTime(now.Date.AddDays(intervalDays)), 0);

        for (int candidate = minInterval; candidate <= maxInterval; candidate++)
        {
            var date = DateOnly.FromDateTime(now.Date.AddDays(candidate));
            int load = dueForecast.GetValueOrDefault(date, 0);
            if (load < lowestLoad)
            {
                lowestLoad = load;
                bestInterval = candidate;
            }
        }

        return bestInterval;
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
