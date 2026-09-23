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
    public void CalculateNextReview_WhenSecondGoodReview_ShouldSetIntervalToSix()
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
        result.NewIntervalDays.Should().Be(6);
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

    [Fact]
    public void CalculateRetentionProbability_WhenCardIsNew_ShouldReturnFiftyPercentBaseline()
    {
        var card = new ReviewCard { Status = LearningStatus.New, Repetitions = 0 };
        var probability = _engine.CalculateRetentionProbability(card);
        probability.Should().Be(50.0);
    }

    [Fact]
    public void CalculateRetentionProbability_AtReviewMoment_ShouldBeAroundOneHundredPercent()
    {
        var now = DateTime.UtcNow;
        var card = new ReviewCard
        {
            Status = LearningStatus.Remembered,
            Repetitions = 2,
            IntervalDays = 5,
            EaseFactor = 2.5,
            LastReviewedAt = now
        };
        var probability = _engine.CalculateRetentionProbability(card, now);
        probability.Should().Be(100.0);
    }

    [Fact]
    public void CalculateRetentionProbability_AtIntervalDueDate_ShouldBeAroundNinetyPercent()
    {
        var reviewedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var card = new ReviewCard
        {
            Status = LearningStatus.Remembered,
            Repetitions = 2,
            IntervalDays = 10,
            EaseFactor = 2.5,
            LastReviewedAt = reviewedAt
        };
        var dueDate = reviewedAt.AddDays(10);
        var probability = _engine.CalculateRetentionProbability(card, dueDate);
        probability.Should().Be(90.0);
    }

    [Fact]
    public void CalculateRetentionProbability_WhenOverdue_ShouldDeclineBelowNinetyPercent()
    {
        var reviewedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var card = new ReviewCard
        {
            Status = LearningStatus.Remembered,
            Repetitions = 2,
            IntervalDays = 10,
            EaseFactor = 2.5,
            LastReviewedAt = reviewedAt
        };
        var overdueDate = reviewedAt.AddDays(20);
        var probability = _engine.CalculateRetentionProbability(card, overdueDate);
        probability.Should().BeLessThan(90.0);
        probability.Should().BeGreaterThan(50.0);
    }
}
