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

public class VocabularyKnowledgeGraphServiceTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IVocabularySuggestionService> _mockSuggestionService;
    private readonly VocabularyKnowledgeGraphService _graphService;

    public VocabularyKnowledgeGraphServiceTests()
    {
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockSuggestionService = new Mock<IVocabularySuggestionService>();

        _graphService = new VocabularyKnowledgeGraphService(
            _mockVocabRepo.Object,
            _mockSuggestionService.Object);
    }

    [Fact]
    public async Task BuildGraphAsync_WhenWordExists_BuildsCenterNodeAndSemanticEdges()
    {
        // Arrange
        var wordId = Guid.NewGuid();
        var word = new Word
        {
            Id = wordId,
            Term = "resilient",
            Level = CefrLevel.C1,
            PrimaryPartOfSpeech = PartOfSpeech.Adjective,
            Meanings = new List<WordMeaning>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    WordId = wordId,
                    DefinitionVi = "kiên cường, nhanh phục hồi",
                    DefinitionEn = "able to withstand or recover quickly"
                }
            }
        };

        _mockVocabRepo.Setup(r => r.GetWithDetailsAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(word);

        var synWordDto = new WordDto(
            Guid.NewGuid(), "tough", null, null, null, null, CefrLevel.B2, PartOfSpeech.Adjective, null,
            new List<WordMeaningDto> { new(Guid.NewGuid(), PartOfSpeech.Adjective, "bền bỉ", "strong", null, 0, new List<ExampleSentenceDto>()) },
            new List<string>(), new List<string>(), LearningStatus.Mastered, null, 90.0);

        var antWordDto = new WordDto(
            Guid.NewGuid(), "fragile", null, null, null, null, CefrLevel.B1, PartOfSpeech.Adjective, null,
            new List<WordMeaningDto> { new(Guid.NewGuid(), PartOfSpeech.Adjective, "dễ vỡ", "easily broken", null, 0, new List<ExampleSentenceDto>()) },
            new List<string>(), new List<string>(), LearningStatus.Learning, null, 60.0);

        var suggestions = new WordRelatedSuggestionsDto(
            SourceWordId: wordId,
            SourceTerm: "resilient",
            Synonyms: new List<WordSuggestionItemDto>
            {
                new(synWordDto, SuggestionCategory.Synonym, 85.0, "Synonym match")
            },
            Antonyms: new List<WordSuggestionItemDto>
            {
                new(antWordDto, SuggestionCategory.Antonym, 80.0, "Antonym match")
            },
            WordFamily: new List<WordSuggestionItemDto>(),
            Collocations: new List<WordSuggestionItemDto>(),
            SameTopic: new List<WordSuggestionItemDto>()
        );

        _mockSuggestionService.Setup(s => s.GetSuggestionsAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);

        // Act
        var graph = await _graphService.BuildGraphAsync(wordId);

        // Assert
        graph.Should().NotBeNull();
        graph!.CenterNode.Term.Should().Be("resilient");
        graph.CenterNode.Level.Should().Be(CefrLevel.C1);
        graph.CenterNode.IsCenter.Should().BeTrue();
        graph.ConnectedNodes.Should().NotBeEmpty();
        graph.ConnectedNodes.Should().Contain(n => n.Term == "tough");
        graph.ConnectedNodes.Should().Contain(n => n.Term == "fragile");
    }

    [Fact]
    public async Task BuildGraphAsync_WhenWordDoesNotExist_ReturnsNull()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockVocabRepo.Setup(r => r.GetWithDetailsAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var graph = await _graphService.BuildGraphAsync(missingId);

        // Assert
        graph.Should().BeNull();
    }
}
