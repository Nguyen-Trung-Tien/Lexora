using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class RoadmapNode : Entity
{
    public Guid MilestoneId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RoadmapNodeType Type { get; set; } = RoadmapNodeType.VocabularySet;
    public NodeStatus Status { get; set; } = NodeStatus.Locked;
    public int OrderIndex { get; set; }
    public double CanvasX { get; set; }
    public double CanvasY { get; set; }
    public int TargetItemCount { get; set; }
    public int CompletedItemCount { get; set; }
    public double EaseFactorWeight { get; set; } = 2.5;
    public Guid? ReferenceId { get; set; }

    // Navigation property
    public RoadmapMilestone? Milestone { get; set; }
}

