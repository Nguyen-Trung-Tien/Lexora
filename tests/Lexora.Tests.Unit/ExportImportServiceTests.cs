using System.Text.Json;
using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class ExportImportServiceTests
{
    private readonly Mock<IVocabularyRepository> _vocabRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ExportImportService _service;

    public ExportImportServiceTests()
    {
        _vocabRepoMock = new Mock<IVocabularyRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new ExportImportService(_vocabRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ExportToJsonAsync_ShouldReturnValidJson()
    {
        // Arrange
        var sampleWord = new Word
        {
            Term = "innovative",
            NormalizedTerm = "innovative",
            PhoneticUk = "/ˈɪn.ə.veɪ.tɪv/",
            Level = CefrLevel.B2,
            PrimaryPartOfSpeech = PartOfSpeech.Adjective,
            Meanings = new List<WordMeaning>
            {
                new WordMeaning
                {
                    PartOfSpeech = PartOfSpeech.Adjective,
                    DefinitionVi = "sáng tạo, đổi mới",
                    DefinitionEn = "advanced and original",
                    OrderIndex = 0,
                    Examples = new List<ExampleSentence>
                    {
                        new ExampleSentence
                        {
                            SentenceEn = "An innovative product.",
                            SentenceVi = "Một sản phẩm sáng tạo."
                        }
                    }
                }
            }
        };

        _vocabRepoMock.Setup(r => r.SearchAsync(
            null, null, null, null, null, null, 0, 100000, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { sampleWord });

        // Act
        var json = await _service.ExportToJsonAsync();

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("innovative");
        json.Should().Contain("sáng tạo, đổi mới");
    }

    [Fact]
    public async Task ImportFromJsonAsync_WhenGivenValidJson_ShouldImportWordAndCreateReviewCard()
    {
        // Arrange
        var json = """
        [
          {
            "term": "versatile",
            "phoneticUk": "/ˈvɜː.sə.taɪl/",
            "level": "B2",
            "primaryPartOfSpeech": "Adjective",
            "meanings": [
              {
                "partOfSpeech": "Adjective",
                "definitionVi": "linh hoạt, đa năng",
                "examples": [
                  {
                    "sentenceEn": "A versatile tool.",
                    "sentenceVi": "Một công cụ đa năng."
                  }
                ]
              }
            ],
            "topics": [],
            "examTags": []
          }
        ]
        """;

        _vocabRepoMock.Setup(r => r.GetAllTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Topic>());
        _vocabRepoMock.Setup(r => r.GetAllExamTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExamTag>());
        _vocabRepoMock.Setup(r => r.GetByTermAsync("versatile", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        Word? addedWord = null;
        _vocabRepoMock.Setup(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()))
            .Callback<Word, CancellationToken>((w, ct) => addedWord = w)
            .ReturnsAsync((Word w, CancellationToken ct) => w);

        // Act
        var result = await _service.ImportFromJsonAsync(json);

        // Assert
        result.TotalImported.Should().Be(1);
        result.TotalSkipped.Should().Be(0);
        result.Errors.Should().BeEmpty();

        addedWord.Should().NotBeNull();
        addedWord!.Term.Should().Be("versatile");
        addedWord.Level.Should().Be(CefrLevel.B2);
        addedWord.Meanings.Should().HaveCount(1);
        addedWord.Meanings.First().DefinitionVi.Should().Be("linh hoạt, đa năng");
        addedWord.ReviewCard.Should().NotBeNull();
        addedWord.ReviewCard!.Status.Should().Be(LearningStatus.New);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportFromCsvAsync_WhenGivenValidCsv_ShouldImportCorrectly()
    {
        // Arrange
        var csv = """
        Term,Level,PartOfSpeech,PhoneticUk,PhoneticUs,MeaningsVi,Examples,Topics,ExamTags,Notes
        "diligent","B2","Adjective","/ˈdɪl.ɪ.dʒənt/","","chăm chỉ, cần cù","A diligent student. => Một học sinh cần cù.","","",""
        """;

        _vocabRepoMock.Setup(r => r.GetByTermAsync("diligent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        Word? addedWord = null;
        _vocabRepoMock.Setup(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()))
            .Callback<Word, CancellationToken>((w, ct) => addedWord = w)
            .ReturnsAsync((Word w, CancellationToken ct) => w);

        // Act
        var result = await _service.ImportFromCsvAsync(csv);

        // Assert
        result.TotalImported.Should().Be(1);
        addedWord.Should().NotBeNull();
        addedWord!.Term.Should().Be("diligent");
        addedWord.Meanings.Should().HaveCount(1);
        addedWord.Meanings.First().DefinitionVi.Should().Be("chăm chỉ, cần cù");
        addedWord.Meanings.First().Examples.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("=cmd|' /C calc'!A0")]
    [InlineData("+SUM(1+1)")]
    [InlineData("-cmd")]
    [InlineData("@evil")]
    public async Task ExportToCsvAsync_WhenFieldStartsWithFormulaTriggers_ShouldPrependSingleQuoteToNeutralizeInjection(string maliciousInput)
    {
        // Arrange
        var dangerousWord = new Word
        {
            Term = maliciousInput,
            NormalizedTerm = maliciousInput.ToLowerInvariant(),
            Level = CefrLevel.B2,
            PrimaryPartOfSpeech = PartOfSpeech.Noun,
            Meanings = new List<WordMeaning>
            {
                new WordMeaning
                {
                    DefinitionVi = "định nghĩa",
                    PartOfSpeech = PartOfSpeech.Noun
                }
            }
        };

        _vocabRepoMock.Setup(r => r.SearchAsync(
            null, null, null, null, null, null, 0, 100000, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { dangerousWord });

        // Act
        var csv = await _service.ExportToCsvAsync();

        // Assert
        csv.Should().NotBeNullOrWhiteSpace();
        // Malicious formula trigger MUST be neutralized with leading single quote
        csv.Should().Contain($"\"'{maliciousInput}\"");
    }
}
