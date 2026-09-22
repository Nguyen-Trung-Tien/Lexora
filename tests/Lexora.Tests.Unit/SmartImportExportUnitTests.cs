using System.Text.Json;
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

public class SmartImportExportUnitTests
{
    [Theory]
    [InlineData("[{\"term\": \"apple\"}]", DataFormat.Json)]
    [InlineData("{\"term\": \"apple\"}", DataFormat.Json)]
    [InlineData("```json\n[{\"term\": \"apple\"}]\n```", DataFormat.Json)]
    [InlineData("Term,Definition,POS,Level\napple,quả táo,Noun,A1", DataFormat.Csv)]
    [InlineData("Term;Definition;POS;Level\napple;quả táo;Noun;A1", DataFormat.Csv)]
    [InlineData("apple\tquả táo\nbanana\tquả chuối", DataFormat.Txt)]
    [InlineData("apple : quả táo\nbanana : quả chuối", DataFormat.Txt)]
    [InlineData("apple - quả táo", DataFormat.Txt)]
    [InlineData("# Present Simple\n## Structure\nS + V", DataFormat.Txt)]
    public void SmartContentDetector_DetectFormat_ShouldDetectExpectedFormat(string content, DataFormat expected)
    {
        var detected = SmartContentDetector.DetectFormat(content);
        detected.Should().Be(expected);
    }

    [Theory]
    [InlineData("words.json", DataFormat.Json)]
    [InlineData("words.csv", DataFormat.Csv)]
    [InlineData("words.tsv", DataFormat.Txt)]
    [InlineData("words.txt", DataFormat.Txt)]
    public void SmartContentDetector_DetectFormat_WithFileNameHint_ShouldResolveFormat(string fileName, DataFormat expected)
    {
        var detected = SmartContentDetector.DetectFormat("ambiguous text", fileName);
        detected.Should().Be(expected);
    }

    [Fact]
    public void SmartContentDetector_DetectFormat_EmptyContent_ShouldReturnUnknown()
    {
        SmartContentDetector.DetectFormat("   ").Should().Be(DataFormat.Unknown);
        SmartContentDetector.DetectFormat(null).Should().Be(DataFormat.Unknown);
    }

    [Fact]
    public async Task Vocabulary_ImportSmartAsync_ShouldAutoDetectJson_AndSkipExisting()
    {
        // Arrange
        var vocabRepo = new Mock<IVocabularyRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new ExportImportService(vocabRepo.Object, uow.Object);

        var existingWord = new Word { Term = "existing", NormalizedTerm = "existing" };
        vocabRepo.Setup(r => r.GetByTermAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWord);

        var json = """
        [
            { "term": "existing", "meanings": [{ "definitionVi": "đã có" }] },
            { "term": "newword", "meanings": [{ "definitionVi": "từ mới" }] }
        ]
        """;

        // Act
        var result = await service.ImportSmartAsync(json, conflictResolution: ImportConflictResolution.Skip);

        // Assert
        result.TotalImported.Should().Be(1);
        result.TotalSkipped.Should().Be(1);
        result.TotalUpdated.Should().Be(0);
        vocabRepo.Verify(r => r.AddAsync(It.Is<Word>(w => w.Term == "newword"), It.IsAny<CancellationToken>()), Times.Once);
        vocabRepo.Verify(r => r.UpdateAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Vocabulary_ImportSmartAsync_ShouldOverwriteAndMerge_WhenConflictResolutionIsOverwrite()
    {
        // Arrange
        var vocabRepo = new Mock<IVocabularyRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new ExportImportService(vocabRepo.Object, uow.Object);

        var existingWord = new Word
        {
            Term = "resilient",
            NormalizedTerm = "resilient",
            Level = CefrLevel.None,
            Meanings = new List<WordMeaning>
            {
                new WordMeaning { DefinitionVi = "kiên cường", OrderIndex = 0 }
            }
        };

        vocabRepo.Setup(r => r.GetByTermAsync("resilient", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWord);

        var json = """
        [
            {
                "term": "resilient",
                "level": "C1",
                "meanings": [
                    { "definitionVi": "phục hồi nhanh" }
                ]
            }
        ]
        """;

        // Act
        var result = await service.ImportSmartAsync(json, conflictResolution: ImportConflictResolution.Overwrite);

        // Assert
        result.TotalImported.Should().Be(0);
        result.TotalSkipped.Should().Be(0);
        result.TotalUpdated.Should().Be(1);
        existingWord.Level.Should().Be(CefrLevel.C1);
        existingWord.Meanings.Should().HaveCount(2); // kiên cường + phục hồi nhanh
        vocabRepo.Verify(r => r.UpdateAsync(existingWord, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Vocabulary_ExportToJsonAsync_WithSelectiveWordIds_ShouldFilterWords()
    {
        // Arrange
        var vocabRepo = new Mock<IVocabularyRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new ExportImportService(vocabRepo.Object, uow.Object);

        var word1 = new Word { Id = Guid.NewGuid(), Term = "alpha", NormalizedTerm = "alpha" };

        vocabRepo.Setup(r => r.GetByIdsWithDetailsAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.Contains(word1.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { word1 });

        // Act
        var json = await service.ExportToJsonAsync(new List<Guid> { word1.Id });

        // Assert
        json.Should().Contain("alpha");
    }

    [Fact]
    public void Vocabulary_GetSampleTemplate_ShouldReturnValidSample()
    {
        var vocabRepo = new Mock<IVocabularyRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new ExportImportService(vocabRepo.Object, uow.Object);

        var jsonSample = service.GetSampleTemplate("json");
        jsonSample.Should().Contain("perseverance");

        var csvSample = service.GetSampleTemplate("csv");
        csvSample.Should().Contain("collaborate");

        var txtSample = service.GetSampleTemplate("txt");
        txtSample.Should().Contain("perseverance");
    }

    [Fact]
    public async Task Grammar_ExportToTxtAsync_And_ImportFromTxtAsync_ShouldWorkSymmetrically()
    {
        // Arrange
        var grammarRepo = new Mock<IGrammarRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new GrammarExportImportService(grammarRepo.Object, uow.Object);

        var rule = new GrammarRule
        {
            Id = Guid.NewGuid(),
            Title = "Present Continuous",
            ExamTarget = ExamType.Ielts,
            Level = CefrLevel.A2,
            Topic = "Tenses",
            Structure = "S + am/is/are + V-ing",
            ExplanationVi = "Diễn tả hành động đang xảy ra",
            ExplanationEn = "Express actions happening now",
            OrderIndex = 1,
            Examples = new List<GrammarExample>
            {
                new GrammarExample
                {
                    SentenceEn = "She is reading a book.",
                    SentenceVi = "Cô ấy đang đọc sách.",
                    Note = "Action in progress"
                }
            }
        };

        grammarRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GrammarRule> { rule });

        // Act - Export
        var exportedTxt = await service.ExportToTxtAsync();

        exportedTxt.Should().Contain("# Present Continuous");
        exportedTxt.Should().Contain("Target: Ielts");
        exportedTxt.Should().Contain("Level: A2");
        exportedTxt.Should().Contain("She is reading a book.");

        // Act - Import into repo with no existing rules
        grammarRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GrammarRule>());

        var importResult = await service.ImportFromTxtAsync(exportedTxt);

        // Assert
        importResult.TotalImported.Should().Be(1);
        importResult.TotalSkipped.Should().Be(0);
        grammarRepo.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<GrammarRule>>(rules => rules.Any(g => g.Title == "Present Continuous")), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Grammar_ImportSmartAsync_ShouldAutoDetectAndHandleConflicts()
    {
        // Arrange
        var grammarRepo = new Mock<IGrammarRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new GrammarExportImportService(grammarRepo.Object, uow.Object);

        var existingRule = new GrammarRule
        {
            Id = Guid.NewGuid(),
            Title = "Passive Voice",
            ExplanationVi = "Giải thích cũ",
            Examples = new List<GrammarExample>()
        };

        grammarRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GrammarRule> { existingRule });

        var json = """
        [
            {
                "title": "Passive Voice",
                "structure": "S + be + V3/ed",
                "explanationVi": "Giải thích mới",
                "examples": [
                    { "sentenceEn": "The cake was eaten.", "sentenceVi": "Cái bánh đã bị ăn." }
                ]
            },
            {
                "title": "Conditionals",
                "structure": "If + S + V, S + will + V"
            }
        ]
        """;

        // Act with Overwrite
        var result = await service.ImportSmartAsync(json, conflictResolution: ImportConflictResolution.Overwrite);

        // Assert
        result.TotalImported.Should().Be(1); // Conditionals
        result.TotalUpdated.Should().Be(1);  // Passive Voice updated
        existingRule.Structure.Should().Be("S + be + V3/ed");
        existingRule.ExplanationVi.Should().Be("Giải thích mới");
        existingRule.Examples.Should().HaveCount(1);
        grammarRepo.Verify(r => r.UpdateAsync(existingRule, It.IsAny<CancellationToken>()), Times.Once);
        grammarRepo.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<GrammarRule>>(rules => rules.Any(g => g.Title == "Conditionals")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Grammar_GetSampleTemplate_ShouldReturnValidSamples()
    {
        var grammarRepo = new Mock<IGrammarRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new GrammarExportImportService(grammarRepo.Object, uow.Object);

        service.GetSampleTemplate("json").Should().Contain("Present Perfect");
        service.GetSampleTemplate("csv").Should().Contain("First Conditional");
        service.GetSampleTemplate("txt").Should().Contain("First Conditional");
    }
}
