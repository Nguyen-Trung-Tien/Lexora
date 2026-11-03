using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class StudyRoadmap : Entity
{
    public string Title { get; set; } = string.Empty;
    public ExamTarget Target { get; set; } = ExamTarget.Toeic;
    public double StartingScore { get; set; }
    public double TargetScore { get; set; }
    public DateTime TargetDate { get; set; } = DateTime.UtcNow.AddMonths(3);
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<RoadmapMilestone> Milestones { get; set; } = new List<RoadmapMilestone>();
}

