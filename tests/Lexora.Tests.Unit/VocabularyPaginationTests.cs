using FluentAssertions;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class VocabularyPaginationTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly VocabularyService _vocabService;

    public VocabularyPaginationTests()
    {
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockReviewRepo = new Mock<IReviewRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _vocabService = new VocabularyService(_mockVocabRepo.Object, _mockReviewRepo.Object, _mockUnitOfWork.Object);
    }

    [Theory]
    [InlineData(1, 50, 0, 50)]
    [InlineData(2, 50, 50, 50)]
    [InlineData(3, 20, 40, 20)]
    [InlineData(5, 100, 400, 100)]
    public async Task SearchWordsAsync_WithPagingParameters_ShouldPassCorrectSkipAndTake(
        int page, int pageSize, int expectedSkip, int expectedTake)
    {
        // Arrange
        _mockVocabRepo.Setup(r => r.SearchAsync(
            It.IsAny<string?>(),
            It.IsAny<CefrLevel?>(),
            It.IsAny<PartOfSpeech?>(),
            It.IsAny<Guid?>(),
            It.IsAny<ExamType?>(),
            It.IsAny<LearningStatus?>(),
            expectedSkip,
            expectedTake,
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word>());

        // Act
        var result = await _vocabService.SearchWordsAsync(page: page, pageSize: pageSize);

        // Assert
        result.Should().NotBeNull();
        _mockVocabRepo.Verify(r => r.SearchAsync(
            It.IsAny<string?>(),
            It.IsAny<CefrLevel?>(),
            It.IsAny<PartOfSpeech?>(),
            It.IsAny<Guid?>(),
            It.IsAny<ExamType?>(),
            It.IsAny<LearningStatus?>(),
            expectedSkip,
            expectedTake,
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CountWordsAsync_ShouldCallRepositoryCountFilteredAsync()
    {
        // Arrange
        _mockVocabRepo.Setup(r => r.CountFilteredAsync(
            "test",
            CefrLevel.B2,
            PartOfSpeech.Verb,
            null,
            ExamType.Ielts,
            LearningStatus.Learning,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var count = await _vocabService.CountWordsAsync(
            search: "test",
            level: CefrLevel.B2,
            pos: PartOfSpeech.Verb,
            examType: ExamType.Ielts,
            status: LearningStatus.Learning);

        // Assert
        count.Should().Be(42);
        _mockVocabRepo.Verify(r => r.CountFilteredAsync(
            "test",
            CefrLevel.B2,
            PartOfSpeech.Verb,
            null,
            ExamType.Ielts,
            LearningStatus.Learning,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
