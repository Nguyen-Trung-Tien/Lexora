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

public class StudyAdvisorServiceTests
{
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<ISpacedRepetitionEngine> _mockEngine;
    private readonly StudyAdvisorService _advisorService;

    public StudyAdvisorServiceTests()
    {
        _mockReviewRepo = new Mock<IReviewRepository>();
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockEngine = new Mock<ISpacedRepetitionEngine>();

        _advisorService = new StudyAdvisorService(
            _mockReviewRepo.Object,
            _mockVocabRepo.Object,
            _mockEngine.Object);
    }

    [Fact]
    public async Task GetDailyAdviceAsync_WhenDueCardsExist_ShouldRecommendUrgentReview()
    {
        // Arrange
        _mockReviewRepo.Setup(r => r.CountDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        _mockReviewRepo.Setup(r => r.GetDifficultCardsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReviewCard>());
        _mockReviewRepo.Setup(r => r.GetCardCountsByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<LearningStatus, int>
            {
                [LearningStatus.Remembered] = 20,
                [LearningStatus.Mastered] = 10
            });
        _mockVocabRepo.Setup(v => v.CountAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(30);

        // Act
        var advice = await _advisorService.GetDailyAdviceAsync();

        // Assert
        advice.Should().NotBeNull();
        advice.RecommendedAction.Should().Be(StudyAdviceActionType.UrgentReview);
        advice.UrgentReviewCount.Should().Be(7);
        advice.AdviceTitleVi.Should().Contain("SM-2");
    }

    [Fact]
    public async Task GetDailyAdviceAsync_WhenNoDueCardsButWeakCardsExist_ShouldRecommendWeakWordsQuiz()
    {
        // Arrange
        _mockReviewRepo.Setup(r => r.CountDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockReviewRepo.Setup(r => r.GetDifficultCardsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReviewCard> { new(), new(), new() });
        _mockReviewRepo.Setup(r => r.GetCardCountsByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<LearningStatus, int>
            {
                [LearningStatus.Remembered] = 15,
                [LearningStatus.NotRemembered] = 3
            });
        _mockVocabRepo.Setup(v => v.CountAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        // Act
        var advice = await _advisorService.GetDailyAdviceAsync();

        // Assert
        advice.Should().NotBeNull();
        advice.RecommendedAction.Should().Be(StudyAdviceActionType.WeakWordsQuiz);
        advice.WeakWordsCount.Should().Be(3);
    }

    [Fact]
    public async Task GetDailyAdviceAsync_WhenAllHealthy_ShouldRecommendDailyMaintenance()
    {
        // Arrange
        _mockReviewRepo.Setup(r => r.CountDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockReviewRepo.Setup(r => r.GetDifficultCardsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReviewCard>());
        _mockReviewRepo.Setup(r => r.GetCardCountsByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<LearningStatus, int>
            {
                [LearningStatus.Mastered] = 30
            });
        _mockVocabRepo.Setup(v => v.CountAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(30);

        // Act
        var advice = await _advisorService.GetDailyAdviceAsync();

        // Assert
        advice.Should().NotBeNull();
        advice.RecommendedAction.Should().Be(StudyAdviceActionType.DailyMaintenance);
        advice.OverallRetentionScore.Should().Be(100.0);
    }

    [Fact]
    public async Task Get7DayForecastAsync_ShouldGenerate7DaysProjection()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var mockDict = new Dictionary<DateOnly, int>
        {
            [today] = 5,
            [today.AddDays(1)] = 8,
            [today.AddDays(2)] = 3,
            [today.AddDays(3)] = 12,
            [today.AddDays(4)] = 0,
            [today.AddDays(5)] = 2,
            [today.AddDays(6)] = 6
        };

        _mockReviewRepo.Setup(r => r.GetDueForecastCountsAsync(It.IsAny<DateTime>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDict);

        // Act
        var forecast = await _advisorService.Get7DayForecastAsync();

        // Assert
        forecast.Should().NotBeNull();
        forecast.Days.Should().HaveCount(7);
        forecast.TotalDueNext7Days.Should().Be(36);
        forecast.PeakDayDueCount.Should().Be(12);
        forecast.Days[0].IsToday.Should().BeTrue();
        forecast.Days[0].DueCount.Should().Be(5);
    }
}
