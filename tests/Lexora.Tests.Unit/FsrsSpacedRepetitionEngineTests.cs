using FluentAssertions;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Xunit;

namespace Lexora.Tests.Unit;

public class FsrsSpacedRepetitionEngineTests
{
    private readonly FsrsSpacedRepetitionEngine _engine = new();

    [Fact]
    public void CalculateNextReview_WhenRatingIsAgain_ShouldResetRepetitionsAndIncreaseLapses()
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
        var now = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _engine.CalculateNextReview(card, ReviewRating.Again, now);

        // Assert
        result.NewRepetitions.Should().Be(0);
        result.NewIntervalDays.Should().Be(1);
        result.NewLapses.Should().Be(1);
        result.NewStatus.Should().Be(LearningStatus.NotRemembered);
        result.NewEaseFactor.Should().BeGreaterThanOrEqualTo(1.3);
        result.NextReviewAt.Should().Be(now.Date.AddDays(1));
    }

    [Fact]
    public void CalculateFsrsNextReview_InitialReviews_ShouldAssignDistinctStabilities()
    {
        // Arrange
        var newCard = new ReviewCard
        {
            Repetitions = 0,
            Lapses = 0,
            IntervalDays = 0,
            Status = LearningStatus.New
        };

        // Act
        var again = _engine.CalculateFsrsNextReview(newCard, ReviewRating.Again);
        var hard = _engine.CalculateFsrsNextReview(newCard, ReviewRating.Hard);
        var good = _engine.CalculateFsrsNextReview(newCard, ReviewRating.Good);
        var easy = _engine.CalculateFsrsNextReview(newCard, ReviewRating.Easy);

        // Assert
        again.Stability.Should().BeLessThan(hard.Stability);
        hard.Stability.Should().BeLessThan(good.Stability);
        good.Stability.Should().BeLessThan(easy.Stability);

        again.Difficulty.Should().BeGreaterThan(hard.Difficulty);
        hard.Difficulty.Should().BeGreaterThan(good.Difficulty);
        good.Difficulty.Should().BeGreaterThan(easy.Difficulty);
    }

    [Fact]
    public void CalculateFsrsNextReview_Difficulty_ShouldAlwaysBeClampedBetween1And10()
    {
        // Arrange
        var hardCard = new ReviewCard
        {
            EaseFactor = 1.3,
            IntervalDays = 1,
            Repetitions = 5,
            Lapses = 10
        };

        // Act
        var result = _engine.CalculateFsrsNextReview(hardCard, ReviewRating.Again);

        // Assert
        result.Difficulty.Should().BeInRange(1.0, 10.0);
    }

    [Fact]
    public void CalculateFsrsNextReview_HigherDesiredRetention_ShouldProduceShorterInterval()
    {
        // Arrange
        var card = new ReviewCard
        {
            EaseFactor = 2.5,
            IntervalDays = 5,
            Repetitions = 2,
            Lapses = 0,
            Status = LearningStatus.Learning
        };

        // Act
        var highRetention = _engine.CalculateFsrsNextReview(card, ReviewRating.Good, desiredRetention: 0.95);
        var lowRetention = _engine.CalculateFsrsNextReview(card, ReviewRating.Good, desiredRetention: 0.80);

        // Assert
        highRetention.IntervalDays.Should().BeLessThanOrEqualTo(lowRetention.IntervalDays);
    }

    [Fact]
    public void CalculateNextReview_EaseFactor_ShouldNeverDropBelow1_3Floor()
    {
        // Arrange
        var card = new ReviewCard
        {
            EaseFactor = 1.3,
            IntervalDays = 1,
            Repetitions = 0,
            Lapses = 20
        };

        // Act
        var result = _engine.CalculateNextReview(card, ReviewRating.Again);

        // Assert
        result.NewEaseFactor.Should().BeGreaterThanOrEqualTo(1.3);
    }

    [Fact]
    public void CalculateFsrsNextReview_WhenConsecutiveGoodReviews_ShouldProgressStatusToRemembered()
    {
        // Arrange
        var card = new ReviewCard
        {
            IntervalDays = 14,
            Repetitions = 3,
            Lapses = 0,
            Status = LearningStatus.Learning
        };

        // Act
        var result = _engine.CalculateFsrsNextReview(card, ReviewRating.Good);

        // Assert
        result.Status.Should().Be(LearningStatus.Remembered);
        result.Repetitions.Should().Be(4);
        result.IntervalDays.Should().BeGreaterThan(14);
    }

    [Fact]
    public void CalculateFsrsNextReview_WhenEasyReview_ShouldProgressStatusToMasteredIfRepetitionsHigh()
    {
        // Arrange
        var card = new ReviewCard
        {
            IntervalDays = 60,
            Repetitions = 5,
            Lapses = 0,
            Status = LearningStatus.Remembered
        };

        // Act
        var result = _engine.CalculateFsrsNextReview(card, ReviewRating.Easy);

        // Assert
        result.Status.Should().Be(LearningStatus.Mastered);
        result.Repetitions.Should().Be(6);
    }
}
