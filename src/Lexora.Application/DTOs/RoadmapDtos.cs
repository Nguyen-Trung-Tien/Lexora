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
    public string ItemCountDisplay => $"{CompletedItemCount} / {TargetItemCount}";

    public string StatusGlyph => Status switch
    {
        NodeStatus.Mastered => "\uE73E",  // Checkmark
        NodeStatus.InProgress => "\uE768",// Play / Current
        NodeStatus.Available => "\uE72A", // Arrow / Ready
        _ => "\uE72E"                     // Lock
    };

    public string TypeGlyph => Type switch
    {
        RoadmapNodeType.VocabularySet => "\uE82D", // Book
        RoadmapNodeType.GrammarPoint => "\uE8D2",  // Grammar / List
        RoadmapNodeType.MockTest => "\uE765",      // Test / Exam
        RoadmapNodeType.Checkpoint => "\uE9D2",    // Trophy / Checkpoint
        _ => "\uE768"
    };

    public string TypeDisplay => Type switch
    {
        RoadmapNodeType.VocabularySet => "Vocabulary Set",
        RoadmapNodeType.GrammarPoint => "Grammar Rule",
        RoadmapNodeType.MockTest => "Practice Test",
        RoadmapNodeType.Checkpoint => "Checkpoint",
        _ => Type.ToString()
    };

    public string StatusDisplay => Status switch
    {
        NodeStatus.Mastered => "Mastered",
        NodeStatus.InProgress => "In Progress",
        NodeStatus.Available => "Ready",
        _ => "Locked"
    };
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
    public string PhaseDisplay => $"Phase {OrderIndex + 1}";
    public string NodesSummaryDisplay => $"{CompletedNodesCount}/{TotalNodesCount} mastered";
    public string TargetDateDisplay => TargetCompletionDate.HasValue ? $"Target: {TargetCompletionDate.Value:dd/MM/yyyy}" : string.Empty;
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
    public string NodesMasteredDisplay => $"{MasteredNodes}/{TotalNodes}";
    public string ExamBadgeText => Target switch
    {
        ExamTarget.Toeic => "TOEIC",
        ExamTarget.Ielts => "IELTS",
        ExamTarget.Vstep => "VSTEP",
        _ => "EXAM"
    };

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
