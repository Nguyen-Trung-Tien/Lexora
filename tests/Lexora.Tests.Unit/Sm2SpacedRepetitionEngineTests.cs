using FluentAssertions;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Xunit;

namespace Lexora.Tests.Unit;

public class Sm2SpacedRepetitionEngineTests
{
    private readonly Sm2SpacedRepetitionEngine _engine = new();

    [Fact]
    public void CalculateNextReview_WhenRatingIsAgain_ShouldResetRepetitionsAndSetIntervalToOne()
    {
        // Arrange
        var card = new ReviewCard
        {
            EaseFactor = 2.5,
            IntervalDays = 10,
            Repetitions = 3,
            Lapses = 0,
            Status = LearningStatus.Remembered
        };
        var now = new DateTime(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _engine.CalculateNextReview(card, ReviewRating.Again, now);

        // Assert
        result.NewRepetitions.Should().Be(0);
        result.NewIntervalDays.Should().Be(1);
        result.NewLapses.Should().Be(1);
        result.NewStatus.Should().Be(LearningStatus.NotRemembered);
        result.NewEaseFactor.Should().Be(2.3); // 2.5 - 0.20
        result.NextReviewAt.Should().Be(now.Date.AddDays(1));
    }

    [Fact]
    public void CalculateNextReview_WhenRatingIsAgain_ShouldNeverDropEaseFactorBelowMin()
    {
        // Arrange
        var card = new ReviewCard
        {
            EaseFactor = 1.35,
            IntervalDays = 1,
            Repetitions = 0,
            Lapses = 5
        };

        // Act
        var result = _engine.CalculateNextReview(card, ReviewRating.Again);

        // Assert
        result.NewEaseFactor.Should().Be(1.3); // Clamped at 1.3
    }

    [Fact]
    public void CalculateNextReview_WhenFirstGoodReview_ShouldSetIntervalToOne()
    {
        // Arrange
        var card = new ReviewCard
        {
            EaseFactor = 2.5,
            IntervalDays = 0,
            Repetitions = 0,
            Status = LearningStatus.New
        };

        // Act
        var result = _engine.CalculateNextReview(card, ReviewRating.Good);

        // Assert
        result.NewRepetitions.Should().Be(1);
        result.NewIntervalDays.Should().Be(1);
        result.NewStatus.Should().Be(LearningStatus.Remembered);
    }

    [Fact]
    public void CalculateNextReview_WhenSecondGoodReview_ShouldSetIntervalToFour()
    {
        // Arrange
        var card = new ReviewCard
        {
            EaseFactor = 2.5,
            IntervalDays = 1,
            Repetitions = 1,
            Status = LearningStatus.Learning
        };

        // Act
        var result = _engine.CalculateNextReview(card, ReviewRating.Good);

        // Assert
        result.NewRepetitions.Should().Be(2);
        result.NewIntervalDays.Should().Be(4);
        result.NewStatus.Should().Be(LearningStatus.Remembered);
    }

    [Fact]
    public void CalculateNextReview_WhenEasyReview_ShouldAccelerateIntervalAndIncreaseEaseFactor()
    {
        // Arrange
        var card = new ReviewCard
        {
            EaseFactor = 2.5,
            IntervalDays = 4,
            Repetitions = 2,
            Status = LearningStatus.Remembered
        };

        // Act
        var result = _engine.CalculateNextReview(card, ReviewRating.Easy);

        // Assert
        result.NewEaseFactor.Should().Be(2.65); // 2.5 + 0.15
        result.NewRepetitions.Should().Be(3);
        result.NewIntervalDays.Should().BeGreaterThan(4);
    }

    [Fact]
    public void CalculateNextReview_WhenHighRepetitions_ShouldPromoteToMastered()
    {
        // Arrange
        var card = new ReviewCard
        {
            EaseFactor = 2.5,
            IntervalDays = 15,
            Repetitions = 4,
            Status = LearningStatus.Remembered
        };

        // Act
        var result = _engine.CalculateNextReview(card, ReviewRating.Good);

        // Assert
        result.NewRepetitions.Should().Be(5);
        result.NewStatus.Should().Be(LearningStatus.Mastered);
    }
}
