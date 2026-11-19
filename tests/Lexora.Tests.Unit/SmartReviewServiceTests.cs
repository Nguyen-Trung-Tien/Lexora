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

public class SmartReviewServiceTests
{
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly ISpacedRepetitionEngine _srEngine;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ReviewService _reviewService;

    public SmartReviewServiceTests()
    {
        _mockReviewRepo = new Mock<IReviewRepository>();
        _srEngine = new Sm2SpacedRepetitionEngine();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _reviewService = new ReviewService(_mockReviewRepo.Object, _srEngine, _mockUnitOfWork.Object);
    }

    [Fact]
    public void GetMemoryStabilityMetrics_WhenCardProvided_CalculatesStabilityAndRetrievability()
    {
        // Arrange
        var cardDto = new ReviewCardDto(
            Id: Guid.NewGuid(),
            WordId: Guid.NewGuid(),
            Term: "ubiquitous",
            PhoneticUk: "/juːˈbɪk.wɪ.təs/",
            PhoneticUs: "/juːˈbɪk.wə.təs/",
            AudioUkPath: null,
            AudioUsPath: null,
            PrimaryPartOfSpeech: PartOfSpeech.Adjective,
            Level: CefrLevel.C1,
            PrimaryDefinitionVi: "có mặt ở khắp nơi",
            PrimaryDefinitionEn: "present everywhere",
            Examples: new List<ExampleSentenceDto>(),
            Status: LearningStatus.Remembered,
            EaseFactor: 2.5,
            IntervalDays: 10,
            Repetitions: 3,
            Lapses: 0,
            LastReviewedAt: DateTime.UtcNow.AddDays(-2),
            NextReviewAt: DateTime.UtcNow.AddDays(8),
            TotalReviews: 5,
            CorrectReviews: 4,
            Confidence: 75.0
        );

        // Act
        var metrics = _reviewService.GetMemoryStabilityMetrics(cardDto);

        // Assert
        metrics.Should().NotBeNull();
        metrics.Term.Should().Be("ubiquitous");
        metrics.StabilityDays.Should().BeGreaterThan(0);
        metrics.RetrievabilityPercent.Should().BeInRange(0.0, 100.0);
        metrics.EaseFactor.Should().Be(2.5);
        metrics.HealthLevel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetSmartPresetsAsync_ReturnsConfiguredPresetOptions()
    {
        // Arrange
        _mockReviewRepo.Setup(r => r.CountDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        // Act
        var presets = await _reviewService.GetSmartPresetsAsync();

        // Assert
        presets.Should().NotBeEmpty();
        presets.Should().Contain(p => p.Mode == SmartReviewPresetMode.StandardDue);
        presets.Should().Contain(p => p.Mode == SmartReviewPresetMode.SpeedBlitz);
        presets.Should().Contain(p => p.Mode == SmartReviewPresetMode.LeechRescue);
    }

    [Fact]
    public async Task GetPresetQueueAsync_WhenLeechRescue_QueriesDifficultCards()
    {
        // Arrange
        var difficultCards = new List<ReviewCard>
        {
            new() { Id = Guid.NewGuid(), Word = new Word { Id = Guid.NewGuid(), Term = "meticulous" }, Lapses = 3 }
        };

        _mockReviewRepo.Setup(r => r.GetDifficultCardsAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(difficultCards);

        // Act
        var queue = await _reviewService.GetPresetQueueAsync(SmartReviewPresetMode.LeechRescue, limit: 30);

        // Assert
        queue.Should().HaveCount(1);
        queue[0].Term.Should().Be("meticulous");
    }

    [Fact]
    public void GetMemoryStabilityMetrics_WhenFsrsActive_UsesCardStabilityAndTargetRetention()
    {
        var mockPrefs = new Mock<Lexora.Application.Interfaces.IUserPreferencesService>();
        mockPrefs.Setup(p => p.GetPreferences()).Returns(new UserPreferences
        {
            SpacedRepetitionAlgorithm = "FSRS",
            TargetRetention = 0.90
        });
        var fsrs = new FsrsSpacedRepetitionEngine();
        var svc = new ReviewService(_mockReviewRepo.Object, _srEngine, _mockUnitOfWork.Object, null, fsrs, mockPrefs.Object);

        var cardDto = new ReviewCardDto(
            Id: Guid.NewGuid(),
            WordId: Guid.NewGuid(),
            Term: "ephemeral",
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            PrimaryPartOfSpeech: PartOfSpeech.Adjective,
            Level: CefrLevel.C1,
            PrimaryDefinitionVi: "ngắn ngủi",
            PrimaryDefinitionEn: "short-lived",
            Examples: new List<ExampleSentenceDto>(),
            Status: LearningStatus.Remembered,
            EaseFactor: 2.3,
            IntervalDays: 10,
            Repetitions: 3,
            Lapses: 0,
            LastReviewedAt: DateTime.UtcNow.AddDays(-5),
            NextReviewAt: DateTime.UtcNow.AddDays(5),
            TotalReviews: 4,
            CorrectReviews: 4,
            Confidence: 100.0,
            Stability: 18.5,
            Difficulty: 4.2
        );

        var metrics = svc.GetMemoryStabilityMetrics(cardDto);
        metrics.StabilityDays.Should().Be(18.5);
        metrics.RetrievabilityPercent.Should().BeGreaterThan(80.0);
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenEasyWithHintOrSlowResponse_CapsToGoodScheduling()
    {
        var fsrs = new FsrsSpacedRepetitionEngine();
        var mockPrefs = new Mock<Lexora.Application.Interfaces.IUserPreferencesService>();
        mockPrefs.Setup(p => p.GetPreferences()).Returns(new UserPreferences
        {
            SpacedRepetitionAlgorithm = "FSRS",
            TargetRetention = 0.90
        });
        var svc = new ReviewService(_mockReviewRepo.Object, _srEngine, _mockUnitOfWork.Object, null, fsrs, mockPrefs.Object);

        var cardFast = new ReviewCard
        {
            Id = Guid.NewGuid(),
            WordId = Guid.NewGuid(),
            Status = LearningStatus.Remembered,
            Repetitions = 2,
            Stability = 6.0,
            Difficulty = 5.0,
            IntervalDays = 6,
            LastReviewedAt = DateTime.UtcNow.AddDays(-6)
        };
        var cardHint = new ReviewCard
        {
            Id = Guid.NewGuid(),
            WordId = Guid.NewGuid(),
            Status = LearningStatus.Remembered,
            Repetitions = 2,
            Stability = 6.0,
            Difficulty = 5.0,
            IntervalDays = 6,
            LastReviewedAt = DateTime.UtcNow.AddDays(-6)
        };

        _mockReviewRepo.Setup(r => r.GetByIdAsync(cardFast.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cardFast);
        _mockReviewRepo.Setup(r => r.GetByIdAsync(cardHint.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cardHint);
        _mockReviewRepo.Setup(r => r.GetDueForecastCountsAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, int>());

        var fastResult = await svc.SubmitReviewAsync(cardFast.Id, ReviewRating.Easy, responseTimeMs: 1200, usedHint: false);
        var hintResult = await svc.SubmitReviewAsync(cardHint.Id, ReviewRating.Easy, responseTimeMs: 1200, usedHint: true);

        hintResult.Stability.Should().BeLessThan(fastResult.Stability);
    }

    [Fact]
    public void SuggestRatingByResponseTime_ReturnsExpectedSuggestions()
    {
        var card = new ReviewCardDto(
            Guid.NewGuid(), Guid.NewGuid(), "test", null, null, null, null,
            PartOfSpeech.Noun, CefrLevel.B1, "thử", null, new List<ExampleSentenceDto>(),
            LearningStatus.Learning, 2.5, 1, 1, 0, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1, 1, 100.0);

        _reviewService.SuggestRatingByResponseTime(card, 1200, usedHint: false, isTypingCorrect: true).Should().Be(ReviewRating.Easy);
        _reviewService.SuggestRatingByResponseTime(card, 1200, usedHint: true, isTypingCorrect: true).Should().Be(ReviewRating.Hard);
        _reviewService.SuggestRatingByResponseTime(card, 8000, usedHint: false, isTypingCorrect: true).Should().Be(ReviewRating.Hard);
        _reviewService.SuggestRatingByResponseTime(card, 2000, usedHint: false, isTypingCorrect: false).Should().Be(ReviewRating.Again);
        _reviewService.SuggestRatingByResponseTime(card, 3000, usedHint: false, isTypingCorrect: null).Should().Be(ReviewRating.Good);
    }

    [Fact]
    public async Task GetReviewAnalyticsSummaryAsync_ComputesTrueRetentionAndAdvice()
    {
        var logs = Enumerable.Range(0, 20).Select(i => new StudyLog
        {
            Id = Guid.NewGuid(),
            ReviewCardId = Guid.NewGuid(),
            Rating = i < 18 ? ReviewRating.Good : ReviewRating.Again,
            ResponseTimeMs = 2400
        }).ToList();

        _mockReviewRepo.Setup(r => r.GetLogsSinceAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(logs);
        _mockReviewRepo.Setup(r => r.GetDifficultCardsAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReviewCard>());

        var summary = await _reviewService.GetReviewAnalyticsSummaryAsync(30);

        summary.TotalReviewsInPeriod.Should().Be(20);
        summary.TrueRetentionPercent.Should().Be(90.0);
        summary.AverageResponseTimeMs.Should().Be(2400);
        summary.CalibrationAdviceKey.Should().Be("Review_Calibration_Optimal");
    }

    [Fact]
    public void FsrsEngine_ApplyWorkloadFuzz_SelectsLowestLoadDayInWindow()
    {
        var fsrs = new FsrsSpacedRepetitionEngine();
        var now = new DateTime(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);
        var forecast = new Dictionary<DateOnly, int>
        {
            [DateOnly.FromDateTime(now.Date.AddDays(19))] = 5,
            [DateOnly.FromDateTime(now.Date.AddDays(20))] = 25,
            [DateOnly.FromDateTime(now.Date.AddDays(21))] = 2
        };

        int balanced = fsrs.ApplyWorkloadFuzz(20, forecast, now);
        balanced.Should().Be(21);
    }
}
