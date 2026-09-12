using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Application.SpacedRepetition;

public class Sm2SpacedRepetitionEngine : ISpacedRepetitionEngine
{
    private const double MinEaseFactor = 1.3;
    private const double DefaultEaseFactor = 2.5;

    public SpacedRepetitionResult CalculateNextReview(ReviewCard card, ReviewRating rating, DateTime? reviewTimeUtc = null)
    {
        var now = reviewTimeUtc ?? DateTime.UtcNow;
        double ef = card.EaseFactor <= 0 ? DefaultEaseFactor : card.EaseFactor;
        int reps = card.Repetitions;
        int lapses = card.Lapses;
        int interval;
        LearningStatus status;

        switch (rating)
        {
            case ReviewRating.Again:
                reps = 0;
                lapses++;
                interval = 1;
                ef = Math.Max(MinEaseFactor, ef - 0.20);
                status = LearningStatus.NotRemembered;
                break;

            case ReviewRating.Hard:
                if (reps == 0)
                    interval = 1;
                else if (reps == 1)
                    interval = 2;
                else
                    interval = Math.Max(2, (int)Math.Round(card.IntervalDays * 1.2));

                reps++;
                ef = Math.Max(MinEaseFactor, ef - 0.15);
                status = LearningStatus.Learning;
                break;

            case ReviewRating.Good:
                if (reps == 0)
                    interval = 1;
                else if (reps == 1)
                    interval = 4;
                else
                    interval = Math.Max(1, (int)Math.Round(card.IntervalDays * ef));

                reps++;
                // SM-2 EF formula adjustment for q=4: ef = ef + (0.1 - (5-4)*(0.08 + (5-4)*0.02)) = ef + 0.0 = no change
                status = reps >= 5 ? LearningStatus.Mastered : LearningStatus.Remembered;
                break;

            case ReviewRating.Easy:
                if (reps == 0)
                    interval = 3;
                else if (reps == 1)
                    interval = 7;
                else
                    interval = Math.Max(1, (int)Math.Round(card.IntervalDays * ef * 1.3));

                reps++;
                ef = Math.Min(3.0, ef + 0.15);
                status = reps >= 4 ? LearningStatus.Mastered : LearningStatus.Remembered;
                break;

            default:
                interval = 1;
                status = LearningStatus.Learning;
                break;
        }

        ef = Math.Round(ef, 2);
        var nextReviewAt = now.Date.AddDays(interval);

        return new SpacedRepetitionResult(
            NewEaseFactor: ef,
            NewIntervalDays: interval,
            NewRepetitions: reps,
            NewLapses: lapses,
            NewStatus: status,
            NextReviewAt: nextReviewAt
        );
    }

    public IntervalPreviewDto GetIntervalPreview(ReviewCard card, DateTime? reviewTimeUtc = null)
    {
        var again = CalculateNextReview(card, ReviewRating.Again, reviewTimeUtc);
        var hard = CalculateNextReview(card, ReviewRating.Hard, reviewTimeUtc);
        var good = CalculateNextReview(card, ReviewRating.Good, reviewTimeUtc);
        var easy = CalculateNextReview(card, ReviewRating.Easy, reviewTimeUtc);

        return new IntervalPreviewDto(
            AgainText: FormatInterval(again.NewIntervalDays),
            HardText: FormatInterval(hard.NewIntervalDays),
            GoodText: FormatInterval(good.NewIntervalDays),
            EasyText: FormatInterval(easy.NewIntervalDays)
        );
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
