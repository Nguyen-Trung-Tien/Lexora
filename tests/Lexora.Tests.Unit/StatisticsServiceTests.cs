using FluentAssertions;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class StatisticsServiceTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly StatisticsService _statsService;

    public StatisticsServiceTests()
    {
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockReviewRepo = new Mock<IReviewRepository>();

        _statsService = new StatisticsService(_mockVocabRepo.Object, _mockReviewRepo.Object);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCalculateMetricsCorrectly()
    {
        // Arrange
        _mockVocabRepo.Setup(v => v.CountAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var counts = new Dictionary<LearningStatus, int>
        {
            [LearningStatus.Mastered] = 20,
            [LearningStatus.Remembered] = 30,
            [LearningStatus.Learning] = 40,
            [LearningStatus.NotRemembered] = 10
        };
        _mockReviewRepo.Setup(r => r.GetCardCountsByStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(counts);
        _mockReviewRepo.Setup(r => r.CountDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _mockReviewRepo.Setup(r => r.GetUserStreakAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreak { CurrentStreak = 5, LongestStreak = 12, LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow) });

        var logs = new List<StudyLog>
        {
            new() { CreatedAt = DateTime.UtcNow, Rating = ReviewRating.Good },
            new() { CreatedAt = DateTime.UtcNow, Rating = ReviewRating.Easy },
            new() { CreatedAt = DateTime.UtcNow, Rating = ReviewRating.Again },
            new() { CreatedAt = DateTime.UtcNow, Rating = ReviewRating.Good }
        };
        _mockReviewRepo.Setup(r => r.GetRecentLogsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(logs);

        // Act
        var summary = await _statsService.GetDashboardSummaryAsync();

        // Assert
        summary.TotalWords.Should().Be(100);
        summary.TotalMastered.Should().Be(20);
        summary.TotalRemembered.Should().Be(30);
        summary.TotalLearning.Should().Be(40);
        summary.TotalNotRemembered.Should().Be(10);
        summary.DueReviewsToday.Should().Be(2);
        summary.CurrentStreakDays.Should().Be(5);
        summary.LongestStreakDays.Should().Be(12);
        summary.AccuracyRate.Should().Be(75.0); // 3 of 4 Good/Easy
        summary.StudiedTodayCount.Should().Be(4);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_WhenLastActiveDateIsOld_ShouldResetCurrentStreakToZero()
    {
        // Arrange
        _mockVocabRepo.Setup(v => v.CountAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        _mockReviewRepo.Setup(r => r.GetCardCountsByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<LearningStatus, int>());
        _mockReviewRepo.Setup(r => r.CountDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockReviewRepo.Setup(r => r.GetUserStreakAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreak
            {
                CurrentStreak = 10,
                LongestStreak = 15,
                LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3) // Inactive for 3 days
            });
        _mockReviewRepo.Setup(r => r.GetRecentLogsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudyLog>());

        // Act
        var summary = await _statsService.GetDashboardSummaryAsync();

        // Assert
        summary.CurrentStreakDays.Should().Be(0);
        summary.LongestStreakDays.Should().Be(15);
    }

    [Fact]
    public async Task GetDetailedAnalyticsAsync_ShouldGenerateFullBreakdownAndHeatmap()
    {
        // Arrange
        var sampleWords = new List<Word>
        {
            new() { Id = Guid.NewGuid(), Term = "word1", Level = CefrLevel.B1, PrimaryPartOfSpeech = PartOfSpeech.Noun },
            new() { Id = Guid.NewGuid(), Term = "word2", Level = CefrLevel.B2, PrimaryPartOfSpeech = PartOfSpeech.Verb },
            new() { Id = Guid.NewGuid(), Term = "word3", Level = CefrLevel.C1, PrimaryPartOfSpeech = PartOfSpeech.Adjective }
        };

        _mockVocabRepo.Setup(v => v.CountAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sampleWords.Count);
        _mockVocabRepo.Setup(v => v.SearchAsync(
            It.IsAny<string?>(),
            It.IsAny<CefrLevel?>(),
            It.IsAny<PartOfSpeech?>(),
            It.IsAny<Guid?>(),
            It.IsAny<ExamType?>(),
            It.IsAny<LearningStatus?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleWords);

        _mockVocabRepo.Setup(v => v.GetAllTopicsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Topic>());
        _mockReviewRepo.Setup(r => r.GetCardCountsByStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<LearningStatus, int>());
        _mockReviewRepo.Setup(r => r.GetDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReviewCard>());
        _mockReviewRepo.Setup(r => r.GetUserStreakAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new UserStreak { CurrentStreak = 3 });
        _mockReviewRepo.Setup(r => r.GetRecentLogsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<StudyLog>());
        _mockReviewRepo.Setup(r => r.GetRecentSessionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<StudySession>());

        // Act
        var analytics = await _statsService.GetDetailedAnalyticsAsync();

        // Assert
        analytics.Should().NotBeNull();
        analytics.StatusBreakdown.Should().HaveCount(6); // Mastered, Remembered, Learning, Review, NotRemembered, New
        analytics.ExamProgress.Should().HaveCount(3);    // TOEIC, VSTEP, IELTS
        analytics.CefrDistribution.Should().HaveCount(6); // A1 to C2
        analytics.Last30DaysHeatmap.Should().HaveCount(30); // 30 days
        analytics.RetentionCurve.Should().HaveCount(6);

        // Verify heatmap dates are in ascending order
        for (int i = 1; i < analytics.Last30DaysHeatmap.Count; i++)
        {
            analytics.Last30DaysHeatmap[i].Date.Should().Be(analytics.Last30DaysHeatmap[i - 1].Date.AddDays(1));
        }
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_WithHighVolumeToday_ShouldCountAllReviewsAccurately()
    {
        // Arrange: 250 reviews done today (exceeds default limit of 200)
        _mockVocabRepo.Setup(v => v.CountAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(500);
        _mockReviewRepo.Setup(r => r.GetCardCountsByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<LearningStatus, int>());
        _mockReviewRepo.Setup(r => r.CountDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _mockReviewRepo.Setup(r => r.GetUserStreakAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreak { CurrentStreak = 10, LongestStreak = 15, LastActiveDate = DateOnly.FromDateTime(DateTime.Today) });

        var todayLogs = Enumerable.Range(0, 250)
            .Select(_ => new StudyLog { CreatedAt = DateTime.UtcNow, Rating = ReviewRating.Good })
            .ToList();

        _mockReviewRepo.Setup(r => r.GetRecentLogsAsync(200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todayLogs.Take(200).ToList());
        _mockReviewRepo.Setup(r => r.GetLogsSinceAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(todayLogs);

        // Act
        var summary = await _statsService.GetDashboardSummaryAsync();

        // Assert: all 250 reviews counted without truncation
        summary.StudiedTodayCount.Should().Be(250);
        summary.AccuracyRate.Should().Be(100.0);
    }
}
