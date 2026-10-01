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

    [Fact]
    public async Task ExportToJsonAsync_WhenWordHasMultiplePartsOfSpeech_ShouldExportAllPartsOfSpeech()
    {
        // Arrange
        var multiPosWord = new Word
        {
            Term = "run",
            NormalizedTerm = "run",
            Level = CefrLevel.A1,
            PrimaryPartOfSpeech = PartOfSpeech.Verb,
            Meanings = new List<WordMeaning>
            {
                new WordMeaning
                {
                    PartOfSpeech = PartOfSpeech.Verb,
                    DefinitionVi = "chạy",
                    OrderIndex = 0
                },
                new WordMeaning
                {
                    PartOfSpeech = PartOfSpeech.Noun,
                    DefinitionVi = "chuyến chạy, quãng đường",
                    OrderIndex = 1
                }
            }
        };

        _vocabRepoMock.Setup(r => r.SearchAsync(
            null, null, null, null, null, null, 0, 100000, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { multiPosWord });

        // Act
        var json = await _service.ExportToJsonAsync();

        // Assert
        json.Should().Contain("run");
        json.Should().Contain("Verb");
        json.Should().Contain("Noun");
        json.Should().Contain("allPartsOfSpeech");
    }

    [Fact]
    public async Task ImportFromCsvAsync_WithMultiPosAndAbbreviations_ShouldParseCorrectly()
    {
        // Arrange
        var csv = """
        Term,Level,PartOfSpeech,PhoneticUk,PhoneticUs,MeaningsVi,Examples,Topics,ExamTags,Notes
        "collaborate","upper intermediate","v, n","/kəˈlæb.ə.reɪt/","","[Verb] hợp tác = collaborate (trong dự án) | [Noun] sự hợp tác","We collaborate. => Chúng tôi hợp tác.","Business","TOEIC",""
        """;

        _vocabRepoMock.Setup(r => r.GetByTermAsync("collaborate", It.IsAny<CancellationToken>()))
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
        addedWord!.Term.Should().Be("collaborate");
        addedWord.Level.Should().Be(CefrLevel.B2); // "upper intermediate" -> B2
        addedWord.PrimaryPartOfSpeech.Should().Be(PartOfSpeech.Verb);
        addedWord.Meanings.Should().HaveCount(2);

        // Meaning 1: Verb with English definition and explanation
        var m1 = addedWord.Meanings.ElementAt(0);
        m1.PartOfSpeech.Should().Be(PartOfSpeech.Verb);
        m1.DefinitionVi.Should().Be("hợp tác");
        m1.DefinitionEn.Should().Be("collaborate");
        m1.Explanation.Should().Be("trong dự án");

        // Meaning 2: Noun
        var m2 = addedWord.Meanings.ElementAt(1);
        m2.PartOfSpeech.Should().Be(PartOfSpeech.Noun);
        m2.DefinitionVi.Should().Be("sự hợp tác");
    }

    [Theory]
    [InlineData("n", PartOfSpeech.Noun)]
    [InlineData("v", PartOfSpeech.Verb)]
    [InlineData("adj", PartOfSpeech.Adjective)]
    [InlineData("adv", PartOfSpeech.Adverb)]
    [InlineData("prep", PartOfSpeech.Preposition)]
    [InlineData("conj", PartOfSpeech.Conjunction)]
    [InlineData("idiom", PartOfSpeech.Idiom)]
    [InlineData("phrasal verb", PartOfSpeech.PhrasalVerb)]
    [InlineData("noun phrase", PartOfSpeech.NounPhrase)]
    [InlineData("thành ngữ", PartOfSpeech.Idiom)]
    [InlineData("cụm động từ", PartOfSpeech.PhrasalVerb)]
    public void ParsePartOfSpeech_ShouldRecognizeStandardAbbreviationsAndTerms(string input, PartOfSpeech expected)
    {
        var result = ExportImportService.ParsePartOfSpeech(input);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task ExportVocabularyOnlyJsonAsync_ShouldReturnJsonWithOnlyTermsAndMeanings()
    {
        // Arrange
        var sampleWord = new Word
        {
            Id = Guid.NewGuid(),
            Term = "serendipity",
            Meanings = new List<WordMeaning>
            {
                new()
                {
                    PartOfSpeech = PartOfSpeech.Noun,
                    DefinitionVi = "sự may mắn tình cờ",
                    DefinitionEn = "the occurrence and development of events by chance in a happy way",
                    Explanation = "thường dùng trong văn học"
                }
            }
        };

        _vocabRepoMock.Setup(r => r.SearchAsync(
            null, null, null, null, null, null, 0, 100000, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { sampleWord });

        // Act
        var json = await _service.ExportVocabularyOnlyJsonAsync();

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().Be(1);

        var first = root[0];
        first.GetProperty("term").GetString().Should().Be("serendipity");
        var meanings = first.GetProperty("meanings");
        meanings.GetArrayLength().Should().Be(1);
        meanings[0].GetProperty("definitionVi").GetString().Should().Be("sự may mắn tình cờ");

        // Should NOT contain grammar, study logs or extra domain properties
        first.TryGetProperty("grammarRules", out _).Should().BeFalse();
        first.TryGetProperty("studyLogs", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ExportVocabularyOnlyCsvAsync_ShouldReturnPureCsvHeaderAndRow()
    {
        // Arrange
        var sampleWord = new Word
        {
            Id = Guid.NewGuid(),
            Term = "ephemeral",
            Meanings = new List<WordMeaning>
            {
                new()
                {
                    PartOfSpeech = PartOfSpeech.Adjective,
                    DefinitionVi = "phù du, chóng tàn",
                    DefinitionEn = "lasting for a very short time"
                }
            }
        };

        _vocabRepoMock.Setup(r => r.SearchAsync(
            null, null, null, null, null, null, 0, 100000, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { sampleWord });

        // Act
        var csv = await _service.ExportVocabularyOnlyCsvAsync();

        // Assert
        csv.Should().StartWith("Term,DefinitionVi,DefinitionEn,PartOfSpeech");
        csv.Should().Contain("ephemeral");
        csv.Should().Contain("\"phù du, chóng tàn\"");
    }

    [Fact]
    public async Task ExportVocabularyOnlyTxtAsync_ShouldReturnFormattedTextLines()
    {
        // Arrange
        var sampleWord = new Word
        {
            Id = Guid.NewGuid(),
            Term = "resilience",
            Meanings = new List<WordMeaning>
            {
                new()
                {
                    PartOfSpeech = PartOfSpeech.Noun,
                    DefinitionVi = "sự kiên cường, khả năng phục hồi"
                }
            }
        };

        _vocabRepoMock.Setup(r => r.SearchAsync(
            null, null, null, null, null, null, 0, 100000, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { sampleWord });

        // Act
        var txt = await _service.ExportVocabularyOnlyTxtAsync();

        // Assert
        txt.Should().Contain("resilience\tsự kiên cường, khả năng phục hồi");
    }
}
