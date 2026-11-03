using FluentAssertions;
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
}

