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
}
