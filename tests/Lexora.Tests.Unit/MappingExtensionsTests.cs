using FluentAssertions;
using Lexora.Application.Common;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Xunit;

namespace Lexora.Tests.Unit;

public class MappingExtensionsTests
{
    [Fact]
    public void Word_ToDto_ShouldMapAllFieldsAccurately()
    {
        // Arrange
        var id = Guid.NewGuid();
        var word = new Word
        {
            Id = id,
            Term = "collaborate",
            NormalizedTerm = "collaborate",
            PhoneticUk = "/kəˈlæb.ə.reɪt/",
            PhoneticUs = "/kəˈlæb.ə.reɪt/",
            Level = CefrLevel.B2,
            PrimaryPartOfSpeech = PartOfSpeech.Verb,
            Meanings = new List<WordMeaning>
            {
                new()
                {
                    PartOfSpeech = PartOfSpeech.Verb,
                    DefinitionVi = "Hợp tác",
                    DefinitionEn = "To work jointly on an activity or project"
                }
            }
        };

        // Act
        var dto = word.ToDto();

        // Assert
        dto.Id.Should().Be(id);
        dto.Term.Should().Be("collaborate");
        dto.Level.Should().Be(CefrLevel.B2);
        dto.PrimaryPartOfSpeech.Should().Be(PartOfSpeech.Verb);
        dto.Meanings.Should().HaveCount(1);
        dto.Meanings[0].DefinitionVi.Should().Be("Hợp tác");
    }

    [Fact]
    public void ReviewCard_ToDto_ShouldMapSpacedRepetitionProperties()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var wordId = Guid.NewGuid();
        var word = new Word
        {
            Id = wordId,
            Term = "resilience",
            NormalizedTerm = "resilience",
            Level = CefrLevel.C1,
            PrimaryPartOfSpeech = PartOfSpeech.Noun
        };

        var card = new ReviewCard
        {
            Id = cardId,
            WordId = wordId,
            Word = word,
            EaseFactor = 2.5,
            IntervalDays = 6,
            Repetitions = 2,
            Lapses = 0,
            Status = LearningStatus.Learning
        };

        // Act
        var dto = card.ToDto();

        // Assert
        dto.Id.Should().Be(cardId);
        dto.WordId.Should().Be(wordId);
        dto.Term.Should().Be("resilience");
        dto.EaseFactor.Should().Be(2.5);
        dto.IntervalDays.Should().Be(6);
        dto.Repetitions.Should().Be(2);
        dto.Status.Should().Be(LearningStatus.Learning);
    }

    [Fact]
    public void GrammarRule_ToDto_ShouldMapGrammarFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var rule = new GrammarRule
        {
            Id = id,
            Title = "Present Perfect Tense",
            Structure = "S + have/has + V3/ed",
            ExplanationVi = "Diễn tả hành động xảy ra trong quá khứ kéo dài đến hiện tại.",
            ExplanationEn = "Expresses an action that began in the past and continues to the present.",
            Level = CefrLevel.B1,
            ExamTarget = ExamType.General,
            Topic = "Tenses"
        };

        // Act
        var dto = rule.ToDto();

        // Assert
        dto.Id.Should().Be(id);
        dto.Title.Should().Be("Present Perfect Tense");
        dto.Structure.Should().Be("S + have/has + V3/ed");
        dto.Level.Should().Be(CefrLevel.B1);
        dto.Topic.Should().Be("Tenses");
    }

    [Fact]
    public void StudyRoadmap_ToDto_ShouldMapRoadmapAndHierarchy()
    {
        // Arrange
        var roadmapId = Guid.NewGuid();
        var milestoneId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var roadmap = new StudyRoadmap
        {
            Id = roadmapId,
            Title = "TOEIC 750 Fast Track",
            Target = ExamTarget.Toeic,
            StartingScore = 450,
            TargetScore = 750,
            Milestones = new List<RoadmapMilestone>
            {
                new()
                {
                    Id = milestoneId,
                    RoadmapId = roadmapId,
                    OrderIndex = 1,
                    Title = "Foundation Sprint",
                    Nodes = new List<RoadmapNode>
                    {
                        new()
                        {
                            Id = nodeId,
                            MilestoneId = milestoneId,
                            OrderIndex = 1,
                            Title = "Office Vocabulary Set",
                            Type = RoadmapNodeType.VocabularySet,
                            Status = NodeStatus.Available,
                            TargetItemCount = 50,
                            CompletedItemCount = 20
                        }
                    }
                }
            }
        };

        // Act
        var dto = roadmap.ToDto();

        // Assert
        dto.Id.Should().Be(roadmapId);
        dto.Title.Should().Be("TOEIC 750 Fast Track");
        dto.Milestones.Should().HaveCount(1);
        dto.Milestones[0].Title.Should().Be("Foundation Sprint");
        dto.Milestones[0].Nodes.Should().HaveCount(1);
        dto.Milestones[0].Nodes[0].Title.Should().Be("Office Vocabulary Set");
    }

    [Fact]
    public void LearningStorageItem_ToDto_ShouldMapStorageFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var item = new LearningStorageItem
        {
            Id = id,
            ItemType = StorageItemType.Vocabulary,
            ReasonAdded = StorageAdditionReason.MultipleErrors,
            Status = StorageLearningStatus.Weak,
            CorrectCount = 1,
            IncorrectCount = 2,
            RetentionProbability = 33.3
        };

        // Act
        var dto = item.ToDto();

        // Assert
        dto.Id.Should().Be(id);
        dto.ItemType.Should().Be(StorageItemType.Vocabulary);
        dto.ReasonAdded.Should().Be(StorageAdditionReason.MultipleErrors);
        dto.Status.Should().Be(StorageLearningStatus.Weak);
        dto.RetentionProbability.Should().Be(33.3);
    }
}
