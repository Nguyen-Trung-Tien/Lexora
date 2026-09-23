using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class SmartImportWorkflowTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly ExportImportService _service;

    public SmartImportWorkflowTests()
    {
        _service = new ExportImportService(_mockVocabRepo.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task AnalyzeSmartImportAsync_CsvFormat_ShouldDetectColumnsAndNormalize()
    {
        // Arrange
        var csvContent = "Word,Part of Speech,Meaning,Pronunciation,Example\n" +
                         "ephemeral,adjective,lasting for a very short time,/ɪˈfem.ər.əl/,Fashions are ephemeral.\n" +
                         "break down,phrasal verb,stop functioning,,The car broke down.\n" +
                         "by and large,phrase,on the whole; everything considered,,By and large, it was a good trip.";

        _mockVocabRepo.Setup(r => r.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.AnalyzeSmartImportAsync(csvContent, "test.csv");

        // Assert
        result.Should().NotBeNull();
        result.DetectedFormat.Should().Be(DataFormat.Csv);
        result.TotalParsed.Should().Be(3);
        result.ValidCount.Should().Be(3);
        result.DuplicateCount.Should().Be(0);

        var phraseItem = result.PreviewItems.FirstOrDefault(x => x.Term == "by and large");
        phraseItem.Should().NotBeNull();
        phraseItem!.PartOfSpeech.Should().Be(PartOfSpeech.Phrase);
        phraseItem.DefinitionVi.Should().Be("on the whole; everything considered");
    }

    [Fact]
    public async Task AnalyzeSmartImportAsync_WhenTermAlreadyExists_ShouldMarkAsDuplicate()
    {
        // Arrange
        var csvContent = "Term,Meaning\n" +
                         "ubiquitous,present everywhere\n" +
                         "serendipity,fortunate accident";

        var existingWord = new Word { Term = "ubiquitous", NormalizedTerm = "ubiquitous" };
        _mockVocabRepo.Setup(r => r.SearchAsync(
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
            .ReturnsAsync(new List<Word> { existingWord });

        _mockVocabRepo.Setup(r => r.GetByTermAsync("ubiquitous", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWord);
        _mockVocabRepo.Setup(r => r.GetByTermAsync("serendipity", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.AnalyzeSmartImportAsync(csvContent);

        // Assert
        result.TotalParsed.Should().Be(2);
        result.DuplicateCount.Should().Be(1);
        result.ValidCount.Should().Be(1); // 1 new valid word, 1 duplicate

        var dupItem = result.PreviewItems.First(x => x.Term == "ubiquitous");
        dupItem.IsDuplicate.Should().BeTrue();

        var newItem = result.PreviewItems.First(x => x.Term == "serendipity");
        newItem.IsDuplicate.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeSmartImportAsync_MissingMeaning_ShouldFlagAsInvalid()
    {
        // Arrange
        var txtContent = "word_without_meaning\t\t\n" +
                         "valid_word\tmeaning of valid word\n";

        _mockVocabRepo.Setup(r => r.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.AnalyzeSmartImportAsync(txtContent, "vocab.tsv");

        // Assert
        result.TotalParsed.Should().Be(2);
        result.InvalidCount.Should().Be(1);

        var invalidItem = result.PreviewItems.First(x => x.Term == "word_without_meaning");
        invalidItem.IsValid.Should().BeFalse();
        invalidItem.ValidationMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CommitSmartImportAsync_ValidItems_ShouldPersistViaUnitOfWork()
    {
        // Arrange
        var item = new SmartImportItemDto(
            RowIndex: 1,
            Term: "resilient",
            Level: CefrLevel.None,
            PartOfSpeech: PartOfSpeech.Adjective,
            DefinitionVi: "able to withstand or recover quickly from difficult conditions",
            DefinitionEn: null,
            PhoneticUk: null,
            PhoneticUs: null,
            Examples: null,
            Topics: null,
            ExamTags: null,
            Notes: null,
            IsDuplicate: false,
            IsValid: true,
            ValidationMessage: null,
            IsSelected: true
        );

        var request = new SmartImportCommitRequestDto(
            Items: new List<SmartImportItemDto> { item },
            ConflictResolution: ImportConflictResolution.Skip
        );

        _mockVocabRepo.Setup(r => r.GetByTermAsync("resilient", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.CommitSmartImportAsync(request);

        // Assert
        result.TotalImported.Should().Be(1);
        result.TotalErrors.Should().Be(0);
        _mockVocabRepo.Verify(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitSmartImportAsync_DuplicateWithSkip_ShouldNotOverwrite()
    {
        // Arrange
        var existing = new Word { Term = "existing" };
        _mockVocabRepo.Setup(r => r.GetByTermAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var item = new SmartImportItemDto(
            RowIndex: 1,
            Term: "existing",
            Level: CefrLevel.None,
            PartOfSpeech: PartOfSpeech.Noun,
            DefinitionVi: "new meaning",
            DefinitionEn: null,
            PhoneticUk: null,
            PhoneticUs: null,
            Examples: null,
            Topics: null,
            ExamTags: null,
            Notes: null,
            IsDuplicate: true,
            IsValid: true,
            ValidationMessage: null,
            IsSelected: true
        );

        var request = new SmartImportCommitRequestDto(
            Items: new List<SmartImportItemDto> { item },
            ConflictResolution: ImportConflictResolution.Skip
        );

        // Act
        var result = await _service.CommitSmartImportAsync(request);

        // Assert
        result.TotalSkipped.Should().Be(1);
        result.TotalImported.Should().Be(0);
        _mockVocabRepo.Verify(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
