using FluentAssertions;
using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class RoadmapEngineTests
{
    private readonly RoadmapEngine _engine = new();

    [Fact]
    public void GenerateAdaptiveRoadmap_Toeic_ShouldGenerateOrderedMilestonesAndScurveCanvasCoordinates()
    {
        // Act
        var roadmap = _engine.GenerateAdaptiveRoadmap("TOEIC 750 Pathway", ExamTarget.Toeic, 450, 750, 12);

        // Assert
        roadmap.Should().NotBeNull();
        roadmap.Target.Should().Be(ExamTarget.Toeic);
        roadmap.StartingScore.Should().Be(450);
        roadmap.TargetScore.Should().Be(750);
        roadmap.Milestones.Should().HaveCount(3);

        var firstMilestone = roadmap.Milestones.First();
        firstMilestone.Nodes.Should().NotBeEmpty();

        var firstNode = firstMilestone.Nodes.First();
        firstNode.Status.Should().Be(NodeStatus.Available, "The entry node of the skill tree must be unlocked and available");
        firstNode.CanvasX.Should().BeGreaterThan(0);
        firstNode.CanvasY.Should().BeGreaterThan(0);

        // Verify remaining nodes are locked
        var subsequentNodes = roadmap.Milestones.SelectMany(m => m.Nodes).Skip(1);
        subsequentNodes.Should().OnlyContain(n => n.Status == NodeStatus.Locked);
    }

    [Fact]
    public void GenerateAdaptiveRoadmap_Ielts_ShouldTargetAcademicCompetencies()
    {
        // Act
        var roadmap = _engine.GenerateAdaptiveRoadmap("IELTS 7.5 Pathway", ExamTarget.Ielts, 6.0, 7.5, 16);

        // Assert
        roadmap.Milestones.Should().HaveCount(3);
        roadmap.Milestones.First().Nodes.Should().Contain(n => n.Title.Contains("Academic Word List"));
    }

    [Fact]
    public void GenerateAdaptiveRoadmap_Vstep_ShouldTargetB1B2Competencies()
    {
        // Act
        var roadmap = _engine.GenerateAdaptiveRoadmap("VSTEP B2 Pathway", ExamTarget.Vstep, 0, 8.0, 10);

        // Assert
        roadmap.Milestones.Should().HaveCount(2);
        roadmap.Milestones.First().Nodes.Should().Contain(n => n.Title.Contains("Social Interaction"));
    }

    [Fact]
    public async Task RoadmapService_UpdateNodeProgress_ShouldUnlockNextNodeWhenMastered()
    {
        // Arrange
        var mockRepo = new Mock<IRoadmapRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var testRoadmap = _engine.GenerateAdaptiveRoadmap("Test Goal", ExamTarget.Toeic, 400, 700, 8);
        var firstMilestone = testRoadmap.Milestones.First();
        var node1 = firstMilestone.Nodes.ElementAt(0);
        var node2 = firstMilestone.Nodes.ElementAt(1);

        mockRepo.Setup(r => r.GetActiveRoadmapWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testRoadmap);

        var service = new RoadmapService(mockRepo.Object, mockUow.Object);

        // Act: Complete all items in node 1
        var success = await service.UpdateNodeProgressAsync(node1.Id, node1.TargetItemCount, 2.7);

        // Assert
        success.Should().BeTrue();
        node1.Status.Should().Be(NodeStatus.Mastered);
        node2.Status.Should().Be(NodeStatus.Available, "Next node should be automatically unlocked when predecessor is mastered");
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoadmapService_UpdateNodeProgress_WhenAllMilestoneNodesMastered_ShouldMarkMilestoneCompleted()
    {
        // Arrange
        var mockRepo = new Mock<IRoadmapRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var testRoadmap = _engine.GenerateAdaptiveRoadmap("Test Goal", ExamTarget.Toeic, 400, 700, 8);
        var firstMilestone = testRoadmap.Milestones.First();

        mockRepo.Setup(r => r.GetActiveRoadmapWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testRoadmap);

        var service = new RoadmapService(mockRepo.Object, mockUow.Object);

        // Act: Complete all nodes in first milestone
        foreach (var node in firstMilestone.Nodes)
        {
            await service.UpdateNodeProgressAsync(node.Id, node.TargetItemCount, 2.5);
        }

        // Assert
        firstMilestone.IsCompleted.Should().BeTrue();
        firstMilestone.Nodes.Should().OnlyContain(n => n.Status == NodeStatus.Mastered);
    }

    [Fact]
    public async Task RoadmapService_UpdateNodeProgress_WithPartialItems_ShouldSetInProgress()
    {
        // Arrange
        var mockRepo = new Mock<IRoadmapRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var testRoadmap = _engine.GenerateAdaptiveRoadmap("Test Goal", ExamTarget.Toeic, 400, 700, 8);
        var firstMilestone = testRoadmap.Milestones.First();
        var node1 = firstMilestone.Nodes.First();

        mockRepo.Setup(r => r.GetActiveRoadmapWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testRoadmap);

        var service = new RoadmapService(mockRepo.Object, mockUow.Object);

        // Act: Complete only 5 items out of target
        var success = await service.UpdateNodeProgressAsync(node1.Id, 5, 2.3);

        // Assert
        success.Should().BeTrue();
        node1.Status.Should().Be(NodeStatus.InProgress);
        node1.CompletedItemCount.Should().Be(5);
        node1.EaseFactorWeight.Should().Be(2.3);
    }

    [Fact]
    public async Task RoadmapService_UpdateNodeProgress_WhenNodeNotFound_ShouldReturnFalse()
    {
        // Arrange
        var mockRepo = new Mock<IRoadmapRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var testRoadmap = _engine.GenerateAdaptiveRoadmap("Test Goal", ExamTarget.Toeic, 400, 700, 8);

        mockRepo.Setup(r => r.GetActiveRoadmapWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testRoadmap);

        var service = new RoadmapService(mockRepo.Object, mockUow.Object);

        // Act: update non-existent node
        var success = await service.UpdateNodeProgressAsync(Guid.NewGuid(), 10);

        // Assert
        success.Should().BeFalse();
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GenerateAdaptiveRoadmap_WithAvailableTopics_ShouldBindReferenceIdsAndNames()
    {
        // Arrange
        var topicId = Guid.NewGuid();
        var topics = new List<Topic>
        {
            new() { Id = topicId, Name = "Business & Career" }
        };
        var grammarId = Guid.NewGuid();
        var rules = new List<GrammarRule>
        {
            new() { Id = grammarId, Title = "Tenses", Topic = "Tenses" }
        };

        // Act
        var roadmap = _engine.GenerateAdaptiveRoadmap("TOEIC Goal", ExamTarget.Toeic, 400, 700, 10, topics, rules);

        // Assert
        var firstNode = roadmap.Milestones.First().Nodes.First();
        firstNode.ReferenceId.Should().Be(topicId);
        firstNode.ReferenceName.Should().Be("Business & Career");

        var grammarNode = roadmap.Milestones.First().Nodes.FirstOrDefault(n => n.Type == RoadmapNodeType.GrammarPoint);
        grammarNode.Should().NotBeNull();
        grammarNode!.ReferenceId.Should().Be(grammarId);
        grammarNode.ReferenceName.Should().Be("Tenses");
    }

    [Fact]
    public async Task RoadmapService_SyncRoadmapProgressAsync_CalculatesRealProgress_UnlocksNextNode()
    {
        // Arrange
        var mockRepo = new Mock<IRoadmapRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockVocabRepo = new Mock<IVocabularyRepository>();

        var topicId = Guid.NewGuid();
        var testRoadmap = _engine.GenerateAdaptiveRoadmap("Test Goal", ExamTarget.Toeic, 400, 700, 8);
        var firstNode = testRoadmap.Milestones.First().Nodes.First();
        firstNode.ReferenceId = topicId;
        firstNode.TargetItemCount = 2;
        firstNode.CompletedItemCount = 0;

        mockRepo.Setup(r => r.GetActiveRoadmapWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testRoadmap);

        // Mock 2 words in this topic with status Mastered
        var word1 = new Word { Id = Guid.NewGuid(), Term = "contract" };
        word1.WordTopics.Add(new WordTopic { WordId = word1.Id, TopicId = topicId });
        word1.ReviewCard = new ReviewCard { WordId = word1.Id, Status = LearningStatus.Mastered };

        var word2 = new Word { Id = Guid.NewGuid(), Term = "negotiate" };
        word2.WordTopics.Add(new WordTopic { WordId = word2.Id, TopicId = topicId });
        word2.ReviewCard = new ReviewCard { WordId = word2.Id, Status = LearningStatus.Review };

        mockVocabRepo.Setup(v => v.GetWordsForAnalyticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { word1, word2 });

        var service = new RoadmapService(mockRepo.Object, mockUow.Object, mockVocabRepo.Object);

        // Act
        var result = await service.SyncRoadmapProgressAsync();

        // Assert
        result.Should().NotBeNull();
        firstNode.CompletedItemCount.Should().Be(2);
        firstNode.Status.Should().Be(NodeStatus.Mastered);
        var secondNode = testRoadmap.Milestones.First().Nodes.ElementAt(1);
        secondNode.Status.Should().Be(NodeStatus.Available, "Next node should be unlocked after sync detects completion");
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoadmapService_GetNodeActionContextAsync_ReturnsActionContextWithLinkedWords()
    {
        // Arrange
        var mockRepo = new Mock<IRoadmapRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockVocabRepo = new Mock<IVocabularyRepository>();

        var topicId = Guid.NewGuid();
        var testRoadmap = _engine.GenerateAdaptiveRoadmap("Test Goal", ExamTarget.Toeic, 400, 700, 8);
        var firstNode = testRoadmap.Milestones.First().Nodes.First();
        firstNode.ReferenceId = topicId;
        firstNode.ReferenceName = "Business & Career";

        mockRepo.Setup(r => r.GetActiveRoadmapWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testRoadmap);

        var wordId = Guid.NewGuid();
        var word1 = new Word { Id = wordId, Term = "contract" };
        word1.WordTopics.Add(new WordTopic { WordId = wordId, TopicId = topicId });

        mockVocabRepo.Setup(v => v.GetWordsForAnalyticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { word1 });

        var service = new RoadmapService(mockRepo.Object, mockUow.Object, mockVocabRepo.Object);

        // Act
        var context = await service.GetNodeActionContextAsync(firstNode.Id);

        // Assert
        context.Should().NotBeNull();
        context!.NodeId.Should().Be(firstNode.Id);
        context.TopicId.Should().Be(topicId);
        context.TopicName.Should().Be("Business & Career");
        context.LinkedWordIds.Should().Contain(wordId);
        context.HasWords.Should().BeTrue();
    }

    [Fact]
    public void GenerateAdaptiveRoadmap_HighStartingScoreToeic_ShouldSkipFoundationPhase()
    {
        // Act
        var roadmap = _engine.GenerateAdaptiveRoadmap("TOEIC 850 Sprint", ExamTarget.Toeic, 700, 850, 8);

        // Assert
        roadmap.Milestones.Should().HaveCount(2);
        roadmap.Milestones.First().Title.Should().Contain("Phase 2");
    }

    [Fact]
    public void GenerateAdaptiveRoadmap_WithDistinctTopics_ShouldPreferUnusedTopics()
    {
        // Arrange
        var topics = new List<Topic>
        {
            new() { Name = "Business & Career" },
            new() { Name = "Daily Life" },
            new() { Name = "Technology" },
            new() { Name = "Finance" }
        };

        // Act
        var roadmap = _engine.GenerateAdaptiveRoadmap("TOEIC 750", ExamTarget.Toeic, 450, 750, 12, topics);

        // Assert
        var vocabTopicIds = roadmap.Milestones
            .SelectMany(m => m.Nodes)
            .Where(n => n.Type == RoadmapNodeType.VocabularySet && n.ReferenceId.HasValue)
            .Select(n => n.ReferenceId!.Value)
            .ToList();

        vocabTopicIds.Distinct().Count().Should().Be(4);
    }

    [Fact]
    public async Task RoadmapService_SyncRoadmapProgress_ShouldAutoAdvanceGrammarNodeFromStudySessions()
    {
        // Arrange
        var mockRepo = new Mock<IRoadmapRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockReviewRepo = new Mock<IReviewRepository>();

        var roadmap = _engine.GenerateAdaptiveRoadmap("TOEIC Grammar Sync", ExamTarget.Toeic, 450, 750, 12);
        roadmap.CreatedAt = DateTime.UtcNow.AddDays(-2);
        var grammarNode = roadmap.Milestones.SelectMany(m => m.Nodes).First(n => n.Type == RoadmapNodeType.GrammarPoint);
        grammarNode.Status = NodeStatus.Available;
        grammarNode.TargetItemCount = 10;

        mockRepo.Setup(r => r.GetActiveRoadmapWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roadmap);

        mockReviewRepo.Setup(r => r.GetRecentSessionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudySession>
            {
                new()
                {
                    StartedAt = DateTime.UtcNow.AddHours(-1),
                    CompletedAt = DateTime.UtcNow.AddHours(-1),
                    SessionType = "Grammar_Tenses",
                    TotalCards = 10,
                    CorrectCards = 9
                }
            });

        var service = new RoadmapService(mockRepo.Object, mockUow.Object, reviewRepository: mockReviewRepo.Object);

        // Act
        var result = await service.SyncRoadmapProgressAsync();

        // Assert
        result.Should().NotBeNull();
        grammarNode.CompletedItemCount.Should().BeGreaterThanOrEqualTo(9);
        grammarNode.Status.Should().Be(NodeStatus.InProgress);
    }

    [Fact]
    public async Task RoadmapService_SyncRoadmapProgress_ShouldFilterTestSessionsByExamTypeAndNotDoubleCount()
    {
        // Arrange
        var mockRepo = new Mock<IRoadmapRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockReviewRepo = new Mock<IReviewRepository>();

        var roadmap = _engine.GenerateAdaptiveRoadmap("TOEIC Test Sync", ExamTarget.Toeic, 450, 750, 12);
        roadmap.CreatedAt = DateTime.UtcNow.AddDays(-2);
        var testNodes = roadmap.Milestones.SelectMany(m => m.Nodes)
            .Where(n => n.Type is RoadmapNodeType.Checkpoint or RoadmapNodeType.MockTest)
            .ToList();
        testNodes[0].Status = NodeStatus.Available;
        testNodes[0].TargetItemCount = 10;
        testNodes[1].Status = NodeStatus.Available;
        testNodes[1].TargetItemCount = 10;

        mockRepo.Setup(r => r.GetActiveRoadmapWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roadmap);

        mockReviewRepo.Setup(r => r.GetRecentSessionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudySession>
            {
                // Unrelated exam type should be ignored
                new()
                {
                    StartedAt = DateTime.UtcNow.AddHours(-2),
                    CompletedAt = DateTime.UtcNow.AddHours(-2),
                    SessionType = "Test_Ielts",
                    TotalCards = 20,
                    CorrectCards = 20
                },
                // Single matching TOEIC session should only apply to the first node
                new()
                {
                    StartedAt = DateTime.UtcNow.AddHours(-1),
                    CompletedAt = DateTime.UtcNow.AddHours(-1),
                    SessionType = "Test_Toeic",
                    TotalCards = 10,
                    CorrectCards = 10
                }
            });

        var service = new RoadmapService(mockRepo.Object, mockUow.Object, reviewRepository: mockReviewRepo.Object);

        // Act
        await service.SyncRoadmapProgressAsync();

        // Assert
        testNodes[0].Status.Should().Be(NodeStatus.Mastered);
        testNodes[1].CompletedItemCount.Should().Be(0);
    }

    [Fact]
    public void StudyRoadmapDto_ShouldCalculateDailyPacingMetrics()
    {
        // Arrange
        var roadmap = _engine.GenerateAdaptiveRoadmap("TOEIC Pacing", ExamTarget.Toeic, 450, 750, 12);
        var dto = roadmap.ToDto();

        // Assert
        dto.RemainingItems.Should().Be(dto.TotalTargetItems);
        dto.RecommendedDailyItems.Should().BeGreaterThan(0);
        dto.PacingStatusKey.Should().BeOneOf("Roadmap_Pace_Ahead", "Roadmap_Pace_OnTrack", "Roadmap_Pace_Behind");
    }
}

