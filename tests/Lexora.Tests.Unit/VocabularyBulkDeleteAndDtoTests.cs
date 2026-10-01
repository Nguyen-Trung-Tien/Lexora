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

    [Fact]
    public void WordDto_WithMultiplePartsOfSpeech_ShouldExposeAllPartsOfSpeechAndCombinedDisplay()
    {
        // Arrange
        var meanings = new List<WordMeaningDto>
        {
            new(Guid.NewGuid(), PartOfSpeech.Verb, "chạy, vận hành", "to move fast on foot", null, 0, []),
            new(Guid.NewGuid(), PartOfSpeech.Noun, "chuyến đi, cuộc chạy", "an act of running", null, 1, []),
            new(Guid.NewGuid(), PartOfSpeech.Verb, "quản lý, điều hành", "to manage or operate", null, 2, [])
        };

        var dto = new WordDto(
            Id: Guid.NewGuid(),
            Term: "run",
            PhoneticUk: "/rʌn/",
            PhoneticUs: "/rʌn/",
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.A1,
            PrimaryPartOfSpeech: PartOfSpeech.Verb,
            Notes: null,
            Meanings: meanings,
            Topics: [],
            ExamTags: [],
            LearningStatus: LearningStatus.Learning,
            NextReviewAt: null,
            Confidence: 1.0
        );

        // Assert
        dto.HasMultiplePartsOfSpeech.Should().BeTrue();
        dto.AllPartsOfSpeech.Should().ContainInOrder(PartOfSpeech.Verb, PartOfSpeech.Noun);
        dto.AllPartsOfSpeech.Should().HaveCount(2);
        dto.PartsOfSpeechDisplay.Should().Be("v. • n.");
    }

    [Fact]
    public void WordDto_GetAbbreviation_ShouldReturnCorrectAbbreviationForAllPartsOfSpeech()
    {
        WordDto.GetAbbreviation(PartOfSpeech.Noun).Should().Be("n.");
        WordDto.GetAbbreviation(PartOfSpeech.Verb).Should().Be("v.");
        WordDto.GetAbbreviation(PartOfSpeech.Adjective).Should().Be("adj.");
        WordDto.GetAbbreviation(PartOfSpeech.Adverb).Should().Be("adv.");
        WordDto.GetAbbreviation(PartOfSpeech.PhrasalVerb).Should().Be("phr. v.");
        WordDto.GetAbbreviation(PartOfSpeech.NounPhrase).Should().Be("n. phr.");
        WordDto.GetAbbreviation(PartOfSpeech.Idiom).Should().Be("idiom");
        WordDto.GetAbbreviation(PartOfSpeech.Phrase).Should().Be("phr.");
        WordDto.GetAbbreviation(PartOfSpeech.Preposition).Should().Be("prep.");
        WordDto.GetAbbreviation(PartOfSpeech.Conjunction).Should().Be("conj.");
        WordDto.GetAbbreviation(PartOfSpeech.Interjection).Should().Be("interj.");
    }

    [Fact]
    public async Task CreateWordAsync_WhenTermAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var existing = new Word { Id = Guid.NewGuid(), Term = "diligent", NormalizedTerm = "diligent" };
        _mockVocabRepo.Setup(r => r.GetByTermAsync("diligent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var dto = new WordCreateOrUpdateDto(null, "diligent", null, null, null, null, CefrLevel.B2, PartOfSpeech.Adjective, null, [], [], []);

        // Act & Assert
        var act = async () => await _vocabService.CreateWordAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*diligent*already exists*");
    }

    [Fact]
    public async Task UpdateWordAsync_WhenTermAlreadyExistsOnDifferentWord_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var currentId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var currentWord = new Word { Id = currentId, Term = "diligent", NormalizedTerm = "diligent" };
        var duplicateWord = new Word { Id = otherId, Term = "perspicacious", NormalizedTerm = "perspicacious" };

        _mockVocabRepo.Setup(r => r.GetWithDetailsAsync(currentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentWord);
        _mockVocabRepo.Setup(r => r.GetByTermAsync("perspicacious", It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicateWord);

        var dto = new WordCreateOrUpdateDto(currentId, "perspicacious", null, null, null, null, CefrLevel.C2, PartOfSpeech.Adjective, null, [], [], []);

        // Act & Assert
        var act = async () => await _vocabService.UpdateWordAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*perspicacious*already exists*");
    }
}
