using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class ReviewSessionTests
{
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly ISpacedRepetitionEngine _srEngine;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ReviewService _reviewService;

    public ReviewSessionTests()
    {
        _mockReviewRepo = new Mock<IReviewRepository>();
        _srEngine = new Sm2SpacedRepetitionEngine();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _reviewService = new ReviewService(_mockReviewRepo.Object, _srEngine, _mockUnitOfWork.Object);
    }

    [Fact]
    public void GetIntervalPreview_ForNewCard_ShouldPredictAccurateIntervals()
    {
        // Arrange
        var newCard = new ReviewCard
        {
            Repetitions = 0,
            EaseFactor = 2.5,
            IntervalDays = 0,
            Status = LearningStatus.New
        };

        // Act
        var preview = _srEngine.GetIntervalPreview(newCard);

        // Assert
        preview.AgainText.Should().Be("< 1d");
        preview.HardText.Should().Be("< 1d"); // interval = 1
        preview.GoodText.Should().Be("< 1d"); // interval = 1
        preview.EasyText.Should().Be("3d");   // interval = 3
    }

    [Fact]
    public void GetIntervalPreview_ForEstablishedCard_ShouldPredictMultiDayIntervals()
    {
        // Arrange
        var card = new ReviewCard
        {
            Repetitions = 2,
            EaseFactor = 2.5,
            IntervalDays = 6,
            Status = LearningStatus.Remembered
        };

        // Act
        var preview = _srEngine.GetIntervalPreview(card);

        // Assert
        preview.AgainText.Should().Be("< 1d");
        preview.HardText.Should().Be("7d");  // 6 * 1.2 = 7.2 -> 7d
        preview.GoodText.Should().Be("15d"); // 6 * 2.5 = 15d
        preview.EasyText.Should().Be("20d"); // 6 * 2.5 * 1.3 = 19.5 -> 20d
    }

    [Fact]
    public async Task StartAndCompleteSession_ShouldTrackAccuracyAndCountsCorrectly()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new StudySession
        {
            Id = sessionId,
            StartedAt = DateTime.UtcNow,
            SessionType = ReviewSessionMode.TodayDue.ToString()
        };

        _mockReviewRepo.Setup(r => r.CreateStudySessionAsync(It.IsAny<StudySession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _mockReviewRepo.Setup(r => r.GetStudySessionByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act 1: Start
        var startedDto = await _reviewService.StartSessionAsync(ReviewSessionMode.TodayDue, 10);
        startedDto.Should().NotBeNull();
        startedDto.Id.Should().Be(sessionId);

        // Act 2: Complete
        var completedDto = await _reviewService.CompleteSessionAsync(sessionId, totalCards: 10, correctCount: 8, incorrectCount: 2);

        // Assert
        completedDto.TotalCards.Should().Be(10);
        completedDto.CorrectCards.Should().Be(8);
        completedDto.IncorrectCards.Should().Be(2);
        completedDto.AccuracyRate.Should().Be(80.0);
        completedDto.CompletedAt.Should().NotBeNull();

        _mockReviewRepo.Verify(r => r.UpdateStudySessionAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SubmitReview_WithSessionId_ShouldAttachStudyLogToSession()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var card = new ReviewCard
        {
            Id = cardId,
            EaseFactor = 2.5,
            IntervalDays = 1,
            Repetitions = 1,
            Status = LearningStatus.Learning
        };

        _mockReviewRepo.Setup(r => r.GetByIdAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);

        // Act
        var result = await _reviewService.SubmitReviewAsync(cardId, ReviewRating.Good, responseTimeMs: 1450, sessionId: sessionId);

        // Assert
        result.Should().NotBeNull();
        result.NewStatus.Should().Be(LearningStatus.Remembered);
        card.TotalReviews.Should().Be(1);
        card.CorrectReviews.Should().Be(1);
        card.Confidence.Should().Be(100.0);

        _mockReviewRepo.Verify(r => r.AddStudyLog(It.Is<StudyLog>(l => l.StudySessionId == sessionId && l.Rating == ReviewRating.Good && l.ResponseTimeMs == 1450)), Times.Once);
        _mockReviewRepo.Verify(r => r.UpdateStreakAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockReviewRepo.Verify(r => r.UpdateAsync(card, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkStatusQuick_ShouldUpdateStatusAndRecordLog()
    {
        // Arrange
        var wordId = Guid.NewGuid();
        var card = new ReviewCard
        {
            WordId = wordId,
            Status = LearningStatus.New,
            IntervalDays = 0,
            Repetitions = 0
        };

        _mockReviewRepo.Setup(r => r.GetByWordIdAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);

        // Act
        await _reviewService.MarkStatusQuickAsync(wordId, remembered: true);

        // Assert
        card.Status.Should().Be(LearningStatus.Remembered);
        card.TotalReviews.Should().Be(1);
        card.CorrectReviews.Should().Be(1);
        card.Confidence.Should().Be(100.0);

        _mockReviewRepo.Verify(r => r.UpdateStreakAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UndoReviewAsync_ShouldRevertCardStateAndPersistChanges()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var card = new ReviewCard
        {
            Id = cardId,
            Status = LearningStatus.Remembered,
            IntervalDays = 4,
            EaseFactor = 2.5,
            Repetitions = 2,
            Lapses = 0,
            TotalReviews = 2,
            CorrectReviews = 2
        };

        _mockReviewRepo.Setup(r => r.GetByIdAsync(cardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);

        // Act - Revert to previous state (Status: Learning, Interval: 1, EF: 2.5, Reps: 1, Lapses: 0)
        var prevNextReview = DateTime.UtcNow.AddDays(1);
        await _reviewService.UndoReviewAsync(cardId, LearningStatus.Learning, 1, 2.5, 1, 0, prevNextReview);

        // Assert
        card.Status.Should().Be(LearningStatus.Learning);
        card.IntervalDays.Should().Be(1);
        card.Repetitions.Should().Be(1);
        card.Lapses.Should().Be(0);
        card.TotalReviews.Should().Be(1);
        card.NextReviewAt.Should().Be(prevNextReview);

        _mockReviewRepo.Verify(r => r.UpdateAsync(card, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
