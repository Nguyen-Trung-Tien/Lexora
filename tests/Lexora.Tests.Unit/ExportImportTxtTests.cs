using FluentAssertions;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class ExportImportTxtTests
{
    private readonly Mock<IVocabularyRepository> _vocabRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ExportImportService _service;

    public ExportImportTxtTests()
    {
        _vocabRepoMock = new Mock<IVocabularyRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new ExportImportService(_vocabRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ExportToTxtAsync_ShouldProduceTabSeparatedContentWithHeader()
    {
        // Arrange
        var sampleWord = new Word
        {
            Term = "resilient",
            NormalizedTerm = "resilient",
            PhoneticUk = "/rɪˈzɪl.jənt/",
            PhoneticUs = "/rɪˈzɪl.jənt/",
            Level = CefrLevel.C1,
            PrimaryPartOfSpeech = PartOfSpeech.Adjective,
            Meanings = new List<WordMeaning>
            {
                new WordMeaning
                {
                    PartOfSpeech = PartOfSpeech.Adjective,
                    DefinitionVi = "kiên cường, phục hồi nhanh",
                    OrderIndex = 0,
                    Examples = new List<ExampleSentence>
                    {
                        new ExampleSentence
                        {
                            SentenceEn = "He is resilient.",
                            SentenceVi = "Anh ấy rất kiên cường."
                        }
                    }
                }
            }
        };

        _vocabRepoMock.Setup(r => r.SearchAsync(
            null, null, null, null, null, null, 0, 100000, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { sampleWord });

        // Act
        var txt = await _service.ExportToTxtAsync();

        // Assert
        txt.Should().NotBeNullOrWhiteSpace();
        txt.Should().Contain("# Lexora Vocabulary Export");
        txt.Should().Contain("resilient\tkiên cường, phục hồi nhanh\tAdjective\tC1\t/rɪˈzɪl.jənt/\t/rɪˈzɪl.jənt/\tHe is resilient. => Anh ấy rất kiên cường.");
    }

    [Fact]
    public async Task ImportFromTxtAsync_WithTabDelimitedContent_ShouldImportCorrectly()
    {
        // Arrange
        var txt = """
        # Vocabulary list
        resilient	kiên cường	Adjective	C1	/rɪˈzɪl.jənt/	/rɪˈzɪl.jənt/	He is resilient. => Anh ấy rất kiên cường.
        tenacious	ngoan cường	Adjective	C2
        """;

        _vocabRepoMock.Setup(r => r.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        Word? capturedWord = null;
        _vocabRepoMock.Setup(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()))
            .Callback<Word, CancellationToken>((w, _) => capturedWord = w)
            .ReturnsAsync((Word w, CancellationToken _) => w);

        // Act
        var result = await _service.ImportFromTxtAsync(txt);

        // Assert
        result.TotalImported.Should().Be(2);
        result.TotalSkipped.Should().Be(0);
        result.Errors.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportFromTxtAsync_WithHyphenAndColonDelimiters_ShouldParseFlexibleFormats()
    {
        // Arrange
        var txt = """
        # Notes
        perseverance - sự kiên trì bền bỉ
        ubiquitous : phổ biến khắp nơi
        """;

        _vocabRepoMock.Setup(r => r.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        var addedWords = new List<Word>();
        _vocabRepoMock.Setup(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()))
            .Callback<Word, CancellationToken>((w, _) => addedWords.Add(w))
            .ReturnsAsync((Word w, CancellationToken _) => w);

        // Act
        var result = await _service.ImportFromTxtAsync(txt);

        // Assert
        result.TotalImported.Should().Be(2);
        addedWords.Should().HaveCount(2);

        var first = addedWords.FirstOrDefault(w => w.Term == "perseverance");
        first.Should().NotBeNull();
        first!.Meanings.First().DefinitionVi.Should().Be("sự kiên trì bền bỉ");

        var second = addedWords.FirstOrDefault(w => w.Term == "ubiquitous");
        second.Should().NotBeNull();
        second!.Meanings.First().DefinitionVi.Should().Be("phổ biến khắp nơi");
    }

    [Fact]
    public async Task ImportFromTxtAsync_WhenTermAlreadyExists_ShouldSkipDuplicate()
    {
        // Arrange
        var txt = """
        diligent - siêng năng, cần cù
        diligent - siêng năng
        """;

        _vocabRepoMock.Setup(r => r.GetByTermAsync("diligent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.ImportFromTxtAsync(txt);

        // Assert (1 imported, 1 skipped due to intra-file duplicate)
        result.TotalImported.Should().Be(1);
        result.TotalSkipped.Should().Be(1);
    }
}
