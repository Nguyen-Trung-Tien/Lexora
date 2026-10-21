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

public class KnowledgeGraphExportImportServiceTests
{
    private readonly Mock<IVocabularyKnowledgeGraphService> _mockGraphService;
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly KnowledgeGraphExportImportService _service;

    public KnowledgeGraphExportImportServiceTests()
    {
        _mockGraphService = new Mock<IVocabularyKnowledgeGraphService>();
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _service = new KnowledgeGraphExportImportService(
            _mockGraphService.Object,
            _mockVocabRepo.Object,
            _mockUnitOfWork.Object);
    }

    private static VocabularyKnowledgeGraphDto CreateSampleGraph(Guid wordId, string term)
    {
        var center = new GraphNodeDto(wordId, term, KnowledgeGraphNodeType.Center, "từ bỏ", CefrLevel.B2, PartOfSpeech.Verb, "/əˈbændən/", null, false, true, 1.0, "Core Concept");
        var synNode = new GraphNodeDto(Guid.NewGuid(), "desert", KnowledgeGraphNodeType.Synonym, "bỏ rơi", CefrLevel.B1, PartOfSpeech.Verb, "/dɪˈzɜːt/", null, false, false, 0.9, "Synonym", Explanation: "Bỏ rơi nơi chốn");
        var antNode = new GraphNodeDto(Guid.NewGuid(), "maintain", KnowledgeGraphNodeType.Antonym, "duy trì", CefrLevel.B2, PartOfSpeech.Verb, "/meɪnˈteɪn/", null, false, false, 0.85, "Antonym", Explanation: "Duy trì hiện trạng");

        var edges = new List<GraphEdgeDto>
        {
            new(center.Id, synNode.Id, center.Term, synNode.Term, KnowledgeGraphNodeType.Synonym, "Synonym"),
            new(center.Id, antNode.Id, center.Term, antNode.Term, KnowledgeGraphNodeType.Antonym, "Antonym")
        };

        return new VocabularyKnowledgeGraphDto(
            center,
            new List<GraphNodeDto> { synNode, antNode },
            edges,
            1, 1, 0, 0, 0, 2);
    }

    [Fact]
    public async Task ExportToJsonAsync_WithWordId_ReturnsValidJsonPackage()
    {
        var wordId = Guid.NewGuid();
        var sampleGraph = CreateSampleGraph(wordId, "abandon");
        _mockGraphService.Setup(s => s.BuildGraphAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleGraph);

        var json = await _service.ExportToJsonAsync(wordId);

        json.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("centerTerm").GetString().Should().Be("abandon");
        root.GetProperty("nodes").GetArrayLength().Should().Be(3);
        root.GetProperty("edges").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithWordId_ReturnsCsvWithHeadersAndRelations()
    {
        var wordId = Guid.NewGuid();
        var sampleGraph = CreateSampleGraph(wordId, "abandon");
        _mockGraphService.Setup(s => s.BuildGraphAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleGraph);

        var csv = await _service.ExportToCsvAsync(wordId);

        csv.Should().StartWith("SourceTerm,TargetTerm,Relationship,Weight,Level,Explanation");
        csv.Should().Contain("abandon,desert,Synonym");
        csv.Should().Contain("abandon,maintain,Antonym");
    }

    [Fact]
    public async Task ExportToMarkdownMindmapAsync_ReturnsFormattedObsidianMindmap()
    {
        var wordId = Guid.NewGuid();
        var sampleGraph = CreateSampleGraph(wordId, "abandon");
        _mockGraphService.Setup(s => s.BuildGraphAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleGraph);

        var md = await _service.ExportToMarkdownMindmapAsync(wordId);

        md.Should().Contain("# abandon");
        md.Should().Contain("## 🔄 Synonyms (Từ đồng nghĩa)");
        md.Should().Contain("## ⚡ Antonyms (Từ trái nghĩa)");
        md.Should().Contain("**desert**");
        md.Should().Contain("**maintain**");
    }

    [Fact]
    public async Task AnalyzeImportAsync_DetectsValidRelationsAndMissingWords()
    {
        var csv = @"SourceTerm,TargetTerm,Relationship,Weight,Level,Explanation
abandon,desert,Synonym,0.92,B1,Từ bỏ hoặc bỏ rơi
abandon,maintain,Antonym,0.95,B2,Từ bỏ vs. Duy trì";

        var existingWord = new Word { Id = Guid.NewGuid(), Term = "abandon" };
        _mockVocabRepo.Setup(r => r.GetByTermAsync("abandon", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWord);
        _mockVocabRepo.Setup(r => r.GetByTermAsync("desert", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);
        _mockVocabRepo.Setup(r => r.GetByTermAsync("maintain", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        var result = await _service.AnalyzeImportAsync(csv, "csv");

        result.TotalCount.Should().Be(2);
        result.ValidCount.Should().Be(2);
        result.MissingWordsCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CommitImportAsync_AtomicallyAppendsRelationsToWordNotes()
    {
        var sourceWord = new Word { Id = Guid.NewGuid(), Term = "abandon", Notes = "" };
        var targetWord = new Word { Id = Guid.NewGuid(), Term = "desert", Notes = "" };

        _mockVocabRepo.Setup(r => r.GetByTermAsync("abandon", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceWord);
        _mockVocabRepo.Setup(r => r.GetByTermAsync("desert", It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetWord);

        var items = new List<KnowledgeGraphImportItemDto>
        {
            new()
            {
                RowNumber = 1,
                SourceTerm = "abandon",
                TargetTerm = "desert",
                RelationshipType = KnowledgeGraphNodeType.Synonym,
                IsValid = true,
                IsDuplicate = false
            }
        };

        var analysis = new KnowledgeGraphImportAnalysisResultDto(items, 1, 1, 0, 0, Array.Empty<string>());
        var commitResult = await _service.CommitImportAsync(analysis, autoCreateMissingWords: false);

        commitResult.Success.Should().BeTrue();
        commitResult.ImportedRelationsCount.Should().Be(1);
        commitResult.UpdatedWordsCount.Should().Be(1);
        sourceWord.Notes.Should().Contain("[syn: desert]");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CuratedAcademicThesaurus_ReturnsAccurateVerifiedRelations()
    {
        var relations = CuratedAcademicThesaurus.GetRelationsForTerm("abundant");

        relations.Should().NotBeEmpty();
        relations.Should().Contain(r => r.RelatedTerm == "plentiful" && r.RelationType == "Synonym");
        relations.Should().Contain(r => r.RelatedTerm == "scarce" && r.RelationType == "Antonym");
    }

    [Fact]
    public async Task CommitImportAsync_WhenAutoCreateMissingWordsIsTrue_CreatesWordWithReviewCardAndInvalidatesCache()
    {
        var mockCache = new Mock<ISmartSearchCacheService>();
        var serviceWithCache = new KnowledgeGraphExportImportService(
            _mockGraphService.Object,
            _mockVocabRepo.Object,
            _mockUnitOfWork.Object,
            mockCache.Object);

        Word? capturedWord = null;
        _mockVocabRepo.Setup(r => r.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);
        _mockVocabRepo.Setup(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()))
            .Callback<Word, CancellationToken>((w, _) => capturedWord = w)
            .ReturnsAsync((Word w, CancellationToken _) => w);

        var items = new List<KnowledgeGraphImportItemDto>
        {
            new()
            {
                RowNumber = 1,
                SourceTerm = "abandon",
                TargetTerm = "desert",
                RelationshipType = KnowledgeGraphNodeType.Synonym,
                IsValid = true,
                IsDuplicate = false
            }
        };

        var analysis = new KnowledgeGraphImportAnalysisResultDto(items, 1, 1, 2, 0, Array.Empty<string>());
        var commitResult = await serviceWithCache.CommitImportAsync(analysis, autoCreateMissingWords: true);

        commitResult.Success.Should().BeTrue();
        commitResult.CreatedWordsCount.Should().Be(2);

        capturedWord.Should().NotBeNull();
        capturedWord!.ReviewCard.Should().NotBeNull();
        capturedWord.ReviewCard!.EaseFactor.Should().BeGreaterThanOrEqualTo(1.3);
        capturedWord.ReviewCard.EaseFactor.Should().Be(2.5);
        capturedWord.ReviewCard.IntervalDays.Should().Be(0);

        mockCache.Verify(c => c.InvalidateVocabulary(), Times.Once);
    }
}
