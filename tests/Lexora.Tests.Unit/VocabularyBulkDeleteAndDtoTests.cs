using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class VocabularyBulkDeleteAndDtoTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly VocabularyService _vocabService;

    public VocabularyBulkDeleteAndDtoTests()
    {
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockReviewRepo = new Mock<IReviewRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _vocabService = new VocabularyService(_mockVocabRepo.Object, _mockReviewRepo.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task DeleteWordsAsync_WithValidIds_ShouldDeleteAllAndCommitOnce()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var word1 = new Word { Term = "run", Level = CefrLevel.A1 };
        var word2 = new Word { Term = "fast", Level = CefrLevel.A1 };

        _mockVocabRepo.Setup(r => r.GetByIdAsync(id1, It.IsAny<CancellationToken>())).ReturnsAsync(word1);
        _mockVocabRepo.Setup(r => r.GetByIdAsync(id2, It.IsAny<CancellationToken>())).ReturnsAsync(word2);

        // Act
        var deletedCount = await _vocabService.DeleteWordsAsync([id1, id2]);

        // Assert
        deletedCount.Should().Be(2);
        _mockVocabRepo.Verify(r => r.DeleteAsync(word1, It.IsAny<CancellationToken>()), Times.Once);
        _mockVocabRepo.Verify(r => r.DeleteAsync(word2, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWordsAsync_WithDuplicateIds_ShouldDeduplicateAndCommitOnce()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var word1 = new Word { Term = "test", Level = CefrLevel.B1 };

        _mockVocabRepo.Setup(r => r.GetByIdAsync(id1, It.IsAny<CancellationToken>())).ReturnsAsync(word1);

        // Act
        var deletedCount = await _vocabService.DeleteWordsAsync([id1, id1, id1]);

        // Assert
        deletedCount.Should().Be(1);
        _mockVocabRepo.Verify(r => r.DeleteAsync(word1, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWordsAsync_WithEmptyList_ShouldReturnZeroWithoutCommit()
    {
        // Act
        var deletedCount = await _vocabService.DeleteWordsAsync([]);

        // Assert
        deletedCount.Should().Be(0);
        _mockVocabRepo.Verify(r => r.DeleteAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void WordDto_WithMultiplePartsOfSpeech_ShouldExposeAllPosAndFormatDisplay()
    {
        // Arrange
        var meanings = new List<WordMeaningDto>
        {
            new(Guid.NewGuid(), PartOfSpeech.Noun, "hành động chạy", "a physical action of running", null, 0, []),
            new(Guid.NewGuid(), PartOfSpeech.Verb, "chạy nhanh", "to move fast using feet", null, 1, [])
        };

        var dto = new WordDto(
            Id: Guid.NewGuid(),
            Term: "run",
            PhoneticUk: "/rʌn/",
            PhoneticUs: "/rʌn/",
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.A1,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: null,
            Meanings: meanings,
            Topics: [],
            ExamTags: [],
            LearningStatus: LearningStatus.New,
            NextReviewAt: null,
            Confidence: 1.0
        );

        // Assert
        dto.AllPartsOfSpeech.Should().Contain([PartOfSpeech.Noun, PartOfSpeech.Verb]);
        dto.HasMultiplePartsOfSpeech.Should().BeTrue();
        dto.IsIdiomOrPhrase.Should().BeFalse();
        dto.PartsOfSpeechDisplay.Should().Be("n. • v.");
    }

    [Fact]
    public void WordDto_WithIdiomOrPhrase_ShouldDetectIdiomFlag()
    {
        // Arrange: An idiom
        var meanings = new List<WordMeaningDto>
        {
            new(Guid.NewGuid(), PartOfSpeech.Idiom, "qua đời, hỏng hóc", "to die or stop functioning", null, 0, [])
        };

        var dto = new WordDto(
            Id: Guid.NewGuid(),
            Term: "kick the bucket",
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.C1,
            PrimaryPartOfSpeech: PartOfSpeech.Idiom,
            Notes: null,
            Meanings: meanings,
            Topics: [],
            ExamTags: [],
            LearningStatus: LearningStatus.New,
            NextReviewAt: null,
            Confidence: 1.0
        );

        // Assert
        dto.IsIdiomOrPhrase.Should().BeTrue();
        dto.IsIdiom.Should().BeTrue();
        dto.IsPhrasalVerb.Should().BeFalse();
        dto.HasMultiplePartsOfSpeech.Should().BeFalse();
        dto.PartsOfSpeechDisplay.Should().Be("idiom");
    }

    [Fact]
    public void WordDto_WithPhrasalVerb_ShouldDetectIdiomOrPhrase()
    {
        // Arrange
        var meanings = new List<WordMeaningDto>
        {
            new(Guid.NewGuid(), PartOfSpeech.PhrasalVerb, "từ bỏ", "to quit or surrender", null, 0, [])
        };

        var dto = new WordDto(
            Id: Guid.NewGuid(),
            Term: "give up",
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B1,
            PrimaryPartOfSpeech: PartOfSpeech.PhrasalVerb,
            Notes: null,
            Meanings: meanings,
            Topics: [],
            ExamTags: [],
            LearningStatus: LearningStatus.New,
            NextReviewAt: null,
            Confidence: 1.0
        );

        // Assert
        dto.IsIdiomOrPhrase.Should().BeTrue();
        dto.IsIdiom.Should().BeFalse();
        dto.IsPhrasalVerb.Should().BeTrue();
        dto.PartsOfSpeechDisplay.Should().Be("phr. v.");
    }
}
