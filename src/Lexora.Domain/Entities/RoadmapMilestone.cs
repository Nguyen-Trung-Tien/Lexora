using Lexora.Domain.Common;

namespace Lexora.Domain.Entities;

public class RoadmapMilestone : Entity
{
    public Guid RoadmapId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public bool IsCompleted { get; set; }

    // Navigation properties
    public StudyRoadmap? Roadmap { get; set; }
    public ICollection<RoadmapNode> Nodes { get; set; } = new List<RoadmapNode>();
}

