using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class LeechWordsAndForecastTests
{
    [Theory]
    [InlineData(1.3, 1, 0, true)]    // Low EF <= 1.4 with reviews
    [InlineData(1.4, 3, 0, true)]    // EF = 1.4 with reviews
    [InlineData(1.4, 0, 0, false)]   // Low EF but 0 reviews (unreviewed)
    [InlineData(2.5, 5, 3, true)]    // High lapses >= 3
    [InlineData(2.5, 5, 4, true)]    // High lapses >= 3
    [InlineData(2.5, 5, 2, false)]   // Normal word
    [InlineData(2.0, 10, 1, false)]  // Moderate word
    public void WordDto_IsLeech_EvaluatesAccurately(double easeFactor, int totalReviews, int lapses, bool expectedIsLeech)
    {
        // Arrange
        var dto = new WordDto(
            Id: Guid.NewGuid(),
            Term: "benchmark",
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B2,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: null,
            Meanings: new List<WordMeaningDto>(),
            Topics: new List<string>(),
            ExamTags: new List<string>(),
            LearningStatus: LearningStatus.Learning,
            NextReviewAt: DateTime.UtcNow,
            Confidence: 50.0,
            EaseFactor: easeFactor,
            TotalReviews: totalReviews,
            Lapses: lapses
        );

        // Act & Assert
        dto.IsLeech.Should().Be(expectedIsLeech);
    }

    [Fact]
    public async Task VocabularyService_GetLeechWordsAsync_CallsRepositoryAndMapsToDto()
    {
        // Arrange
        var mockRepo = new Mock<IVocabularyRepository>();
        var mockReviewRepo = new Mock<IReviewRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockSrEngine = new Mock<ISpacedRepetitionEngine>();

        var wordId = Guid.NewGuid();
        var word = new Word
        {
            Id = wordId,
            Term = "intricate",
            Level = CefrLevel.C1,
            PrimaryPartOfSpeech = PartOfSpeech.Adjective,
            ReviewCard = new ReviewCard
            {
                WordId = wordId,
                EaseFactor = 1.35,
                Lapses = 4,
                TotalReviews = 6
            }
        };

        mockRepo.Setup(r => r.GetLeechWordsAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { word });

        var service = new VocabularyService(
            mockRepo.Object,
            mockReviewRepo.Object,
            mockUow.Object,
            mockSrEngine.Object);

        // Act
        var result = await service.GetLeechWordsAsync(50);

        // Assert
        result.Should().HaveCount(1);
        result[0].Term.Should().Be("intricate");
        mockRepo.Verify(r => r.GetLeechWordsAsync(50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 4.0)]    // Zero due count gets minimal indicator
    [InlineData(5, 17.5)]   // 5 * 3.5 = 17.5
    [InlineData(10, 35.0)]  // 10 * 3.5 = 35.0
    [InlineData(40, 84.0)]  // Clamped at 84.0 max
    public void DailyForecastItemDto_BarHeight_CalculatesProportionalHeight(int dueCount, double expectedHeight)
    {
        // Arrange
        var item = new DailyForecastItemDto(
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            DayLabel: "Mon",
            DueCount: dueCount,
            IsToday: false,
            Intensity: "Normal"
        );

        // Act & Assert
        item.BarHeight.Should().Be(expectedHeight);
    }
}
