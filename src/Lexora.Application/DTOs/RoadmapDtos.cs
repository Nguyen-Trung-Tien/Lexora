using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record RoadmapNodeDto(
    Guid Id,
    Guid MilestoneId,
    string Title,
    string? Description,
    RoadmapNodeType Type,
    NodeStatus Status,
    int OrderIndex,
    double CanvasX,
    double CanvasY,
    int TargetItemCount,
    int CompletedItemCount,
    double EaseFactorWeight,
    Guid? ReferenceId,
    string? ReferenceName = null)
{
    public double ProgressPercentage => TargetItemCount > 0 
        ? Math.Clamp((double)CompletedItemCount / TargetItemCount * 100.0, 0.0, 100.0) 
        : (Status == NodeStatus.Mastered ? 100.0 : 0.0);

    public string ProgressPercentageDisplay => $"{ProgressPercentage:F0}%";
    public string EaseFactorWeightDisplay => $"EF: {EaseFactorWeight:F2}";
    public bool IsActionable => Status == NodeStatus.Available || Status == NodeStatus.InProgress;
    public bool CanStartPractice => Status == NodeStatus.Available || Status == NodeStatus.InProgress || Status == NodeStatus.Mastered;
    public bool HasLinkedContent => ReferenceId.HasValue || !string.IsNullOrWhiteSpace(ReferenceName);
    public string ReferenceDisplay => !string.IsNullOrWhiteSpace(ReferenceName) ? ReferenceName : Title;
}

public record MilestoneDto(
    Guid Id,
    Guid RoadmapId,
    string Title,
    string? Description,
    int OrderIndex,
    DateTime? TargetCompletionDate,
    bool IsCompleted,
    IReadOnlyList<RoadmapNodeDto> Nodes)
{
    public double ProgressPercentage => Nodes.Count > 0 
        ? Nodes.Average(n => n.ProgressPercentage) 
        : (IsCompleted ? 100.0 : 0.0);

    public int CompletedNodesCount => Nodes.Count(n => n.Status == NodeStatus.Mastered);
    public int TotalNodesCount => Nodes.Count;
}

public record StudyRoadmapDto(
    Guid Id,
    string Title,
    ExamTarget Target,
    double StartingScore,
    double TargetScore,
    DateTime TargetDate,
    bool IsActive,
    IReadOnlyList<MilestoneDto> Milestones)
{
    public double OverallProgress => Milestones.Count > 0 
        ? Milestones.Average(m => m.ProgressPercentage) 
        : 0.0;

    public string OverallProgressDisplay => $"{OverallProgress:F1}%";
    public string TargetScoreDisplay => $"Goal: {TargetScore:F0}";
    public string StartingScoreDisplay => $"Start: {StartingScore:F0}";
    public string TargetDateDisplay => $"Target: {TargetDate:dd/MM/yyyy}";
    public string TotalNodesDisplay => $"/ {TotalNodes}";
    public int DaysRemaining => Math.Max(0, (TargetDate.Date - DateTime.UtcNow.Date).Days);
    public int TotalNodes => Milestones.Sum(m => m.TotalNodesCount);
    public int MasteredNodes => Milestones.Sum(m => m.CompletedNodesCount);

    public RoadmapNodeDto? NextActiveNode => Milestones
        .OrderBy(m => m.OrderIndex)
        .SelectMany(m => m.Nodes.OrderBy(n => n.OrderIndex))
        .FirstOrDefault(n => n.Status == NodeStatus.InProgress || n.Status == NodeStatus.Available);
}

public record GenerateRoadmapRequestDto(
    string Title,
    ExamTarget Target,
    double StartingScore,
    double TargetScore,
    int DurationWeeks);

public record RoadmapNodeActionContextDto(
    Guid NodeId,
    string NodeTitle,
    RoadmapNodeType NodeType,
    Guid? TopicId,
    string? TopicName,
    IReadOnlyList<Guid> LinkedWordIds,
    int TargetCount,
    int CompletedCount,
    string? GrammarQuery,
    Guid? GrammarRuleId)
{
    public bool HasWords => LinkedWordIds.Count > 0;
    public bool HasTopic => TopicId.HasValue;
    public bool HasGrammar => GrammarRuleId.HasValue || !string.IsNullOrWhiteSpace(GrammarQuery);
}
