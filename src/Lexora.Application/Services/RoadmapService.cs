using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class RoadmapService : IRoadmapService
{
    private readonly IRoadmapRepository _roadmapRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVocabularyRepository? _vocabularyRepository;
    private readonly IGrammarRepository? _grammarRepository;
    private readonly RoadmapEngine _roadmapEngine;

    public RoadmapService(
        IRoadmapRepository roadmapRepository,
        IUnitOfWork unitOfWork,
        IVocabularyRepository? vocabularyRepository = null,
        IGrammarRepository? grammarRepository = null)
    {
        _roadmapRepository = roadmapRepository ?? throw new ArgumentNullException(nameof(roadmapRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _vocabularyRepository = vocabularyRepository;
        _grammarRepository = grammarRepository;
        _roadmapEngine = new RoadmapEngine();
    }

    public async Task<StudyRoadmapDto?> GetActiveRoadmapAsync(CancellationToken cancellationToken = default)
    {
        var roadmap = await _roadmapRepository.GetActiveRoadmapWithDetailsAsync(cancellationToken);
        return roadmap == null ? null : MapToDto(roadmap);
    }

    public async Task<StudyRoadmapDto> GenerateAdaptiveRoadmapAsync(
        GenerateRoadmapRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Deactivate existing active roadmaps
        var active = await _roadmapRepository.GetActiveRoadmapWithDetailsAsync(cancellationToken);
        if (active != null)
        {
            active.IsActive = false;
            await _roadmapRepository.UpdateAsync(active, cancellationToken);
        }

        // 2. Fetch available Topics & Grammar Rules to bind real entities
        IReadOnlyList<Topic>? availableTopics = null;
        if (_vocabularyRepository != null)
        {
            availableTopics = await _vocabularyRepository.GetAllTopicsAsync(cancellationToken);
        }

        IReadOnlyList<GrammarRule>? availableGrammarRules = null;
        if (_grammarRepository != null)
        {
            availableGrammarRules = await _grammarRepository.GetAllAsync(cancellationToken);
        }

        // 3. Generate new roadmap graph via RoadmapEngine
        var newRoadmap = _roadmapEngine.GenerateAdaptiveRoadmap(
            request.Title,
            request.Target,
            request.StartingScore,
            request.TargetScore,
            request.DurationWeeks,
            availableTopics,
            availableGrammarRules);

        await _roadmapRepository.AddAsync(newRoadmap, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(newRoadmap);
    }

    public async Task<bool> UpdateNodeProgressAsync(
        Guid nodeId,
        int newlyLearnedItems,
        double? easeFactor = null,
        CancellationToken cancellationToken = default)
    {
        var activeRoadmap = await _roadmapRepository.GetActiveRoadmapWithDetailsAsync(cancellationToken);
        if (activeRoadmap == null) return false;

        RoadmapNode? targetNode = null;
        RoadmapMilestone? parentMilestone = null;

        foreach (var milestone in activeRoadmap.Milestones)
        {
            var node = milestone.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                targetNode = node;
                parentMilestone = milestone;
                break;
            }
        }

        if (targetNode == null || parentMilestone == null) return false;

        // Update progress
        targetNode.CompletedItemCount = Math.Min(targetNode.TargetItemCount, targetNode.CompletedItemCount + newlyLearnedItems);
        if (easeFactor.HasValue)
        {
            targetNode.EaseFactorWeight = Math.Max(1.3, easeFactor.Value);
        }

        if (targetNode.CompletedItemCount >= targetNode.TargetItemCount)
        {
            targetNode.Status = NodeStatus.Mastered;

            // Unlock next node in milestone or subsequent milestone
            UnlockNextNode(activeRoadmap, targetNode);
        }
        else if (targetNode.Status == NodeStatus.Available)
        {
            targetNode.Status = NodeStatus.InProgress;
        }

        // Check milestone completion
        if (parentMilestone.Nodes.All(n => n.Status == NodeStatus.Mastered))
        {
            parentMilestone.IsCompleted = true;
        }

        await _roadmapRepository.UpdateAsync(activeRoadmap, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<StudyRoadmapDto?> SyncRoadmapProgressAsync(CancellationToken cancellationToken = default)
    {
        var activeRoadmap = await _roadmapRepository.GetActiveRoadmapWithDetailsAsync(cancellationToken);
        if (activeRoadmap == null) return null;

        IReadOnlyList<Word>? words = null;
        if (_vocabularyRepository != null)
        {
            words = await _vocabularyRepository.GetWordsForAnalyticsAsync(cancellationToken);
        }

        bool anyUpdated = false;

        foreach (var milestone in activeRoadmap.Milestones.OrderBy(m => m.OrderIndex))
        {
            foreach (var node in milestone.Nodes.OrderBy(n => n.OrderIndex))
            {
                if (node.Type == RoadmapNodeType.VocabularySet && words != null)
                {
                    int learnedCount = 0;

                    if (node.ReferenceId.HasValue)
                    {
                        learnedCount = words.Count(w =>
                            w.WordTopics.Any(wt => wt.TopicId == node.ReferenceId.Value) &&
                            w.ReviewCard != null && w.ReviewCard.Status != LearningStatus.New);
                    }
                    else if (!string.IsNullOrWhiteSpace(node.ReferenceName))
                    {
                        learnedCount = words.Count(w =>
                            w.WordTopics.Any(wt => wt.Topic != null && wt.Topic.Name.Contains(node.ReferenceName, StringComparison.OrdinalIgnoreCase)) &&
                            w.ReviewCard != null && w.ReviewCard.Status != LearningStatus.New);
                    }
                    else
                    {
                        var examType = activeRoadmap.Target switch
                        {
                            ExamTarget.Toeic => ExamType.Toeic,
                            ExamTarget.Ielts => ExamType.Ielts,
                            ExamTarget.Vstep => ExamType.Vstep,
                            _ => (ExamType?)null
                        };

                        if (examType.HasValue)
                        {
                            learnedCount = words.Count(w =>
                                w.WordExamTags.Any(wet => wet.ExamTag != null && wet.ExamTag.Type == examType.Value) &&
                                w.ReviewCard != null && w.ReviewCard.Status != LearningStatus.New);
                        }
                    }

                    int target = node.TargetItemCount > 0 ? node.TargetItemCount : 30;
                    if (learnedCount != node.CompletedItemCount)
                    {
                        node.CompletedItemCount = Math.Min(target, Math.Max(node.CompletedItemCount, learnedCount));
                        anyUpdated = true;
                    }

                    if (node.CompletedItemCount >= target)
                    {
                        if (node.Status != NodeStatus.Mastered)
                        {
                            node.Status = NodeStatus.Mastered;
                            UnlockNextNode(activeRoadmap, node);
                            anyUpdated = true;
                        }
                    }
                    else if (node.CompletedItemCount > 0 && node.Status == NodeStatus.Available)
                    {
                        node.Status = NodeStatus.InProgress;
                        anyUpdated = true;
                    }
                }
            }

            if (milestone.Nodes.All(n => n.Status == NodeStatus.Mastered))
            {
                if (!milestone.IsCompleted)
                {
                    milestone.IsCompleted = true;
                    anyUpdated = true;
                }
            }
        }

        if (anyUpdated)
        {
            await _roadmapRepository.UpdateAsync(activeRoadmap, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(activeRoadmap);
    }

    public async Task<RoadmapNodeActionContextDto?> GetNodeActionContextAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var activeRoadmap = await _roadmapRepository.GetActiveRoadmapWithDetailsAsync(cancellationToken);
        if (activeRoadmap == null) return null;

        RoadmapNode? targetNode = null;
        foreach (var m in activeRoadmap.Milestones)
        {
            targetNode = m.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (targetNode != null) break;
        }

        if (targetNode == null) return null;

        List<Guid> linkedWordIds = new();
        Guid? topicId = null;
        string? topicName = targetNode.ReferenceName;

        if (targetNode.Type == RoadmapNodeType.VocabularySet && _vocabularyRepository != null)
        {
            var allWords = await _vocabularyRepository.GetWordsForAnalyticsAsync(cancellationToken);

            if (targetNode.ReferenceId.HasValue)
            {
                topicId = targetNode.ReferenceId.Value;
                linkedWordIds = allWords
                    .Where(w => w.WordTopics.Any(wt => wt.TopicId == topicId.Value))
                    .Select(w => w.Id)
                    .Take(targetNode.TargetItemCount > 0 ? targetNode.TargetItemCount : 50)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(targetNode.ReferenceName))
            {
                var matchedWords = allWords
                    .Where(w => w.WordTopics.Any(wt => wt.Topic != null && wt.Topic.Name.Contains(targetNode.ReferenceName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (matchedWords.Count > 0)
                {
                    topicId = matchedWords.First().WordTopics.First(wt => wt.Topic != null && wt.Topic.Name.Contains(targetNode.ReferenceName, StringComparison.OrdinalIgnoreCase)).TopicId;
                    linkedWordIds = matchedWords.Select(w => w.Id).Take(targetNode.TargetItemCount > 0 ? targetNode.TargetItemCount : 50).ToList();
                }
            }

            if (linkedWordIds.Count == 0)
            {
                var examType = activeRoadmap.Target switch
                {
                    ExamTarget.Toeic => ExamType.Toeic,
                    ExamTarget.Ielts => ExamType.Ielts,
                    ExamTarget.Vstep => ExamType.Vstep,
                    _ => (ExamType?)null
                };

                if (examType.HasValue)
                {
                    linkedWordIds = allWords
                        .Where(w => w.WordExamTags.Any(wet => wet.ExamTag != null && wet.ExamTag.Type == examType.Value))
                        .Select(w => w.Id)
                        .Take(targetNode.TargetItemCount > 0 ? targetNode.TargetItemCount : 30)
                        .ToList();
                }
            }
        }

        Guid? grammarRuleId = targetNode.Type == RoadmapNodeType.GrammarPoint ? targetNode.ReferenceId : null;
        string? grammarQuery = targetNode.Type == RoadmapNodeType.GrammarPoint ? (targetNode.ReferenceName ?? targetNode.Title) : null;

        return new RoadmapNodeActionContextDto(
            targetNode.Id,
            targetNode.Title,
            targetNode.Type,
            topicId,
            topicName,
            linkedWordIds,
            targetNode.TargetItemCount,
            targetNode.CompletedItemCount,
            grammarQuery,
            grammarRuleId);
    }

    private static void UnlockNextNode(StudyRoadmap roadmap, RoadmapNode completedNode)
    {
        var allNodesOrdered = roadmap.Milestones
            .OrderBy(m => m.OrderIndex)
            .SelectMany(m => m.Nodes.OrderBy(n => n.OrderIndex))
            .ToList();

        int currentIndex = allNodesOrdered.FindIndex(n => n.Id == completedNode.Id);
        if (currentIndex >= 0 && currentIndex + 1 < allNodesOrdered.Count)
        {
            var nextNode = allNodesOrdered[currentIndex + 1];
            if (nextNode.Status == NodeStatus.Locked)
            {
                nextNode.Status = NodeStatus.Available;
            }
        }
    }

    public async Task<IReadOnlyList<StudyRoadmapDto>> GetAllRoadmapsAsync(CancellationToken cancellationToken = default)
    {
        var roadmaps = await _roadmapRepository.GetAllAsync(cancellationToken);
        var result = new List<StudyRoadmapDto>();

        foreach (var r in roadmaps)
        {
            var details = await _roadmapRepository.GetRoadmapWithDetailsAsync(r.Id, cancellationToken);
            if (details != null)
            {
                result.Add(MapToDto(details));
            }
        }

        return result;
    }

    public async Task SetActiveRoadmapAsync(Guid roadmapId, CancellationToken cancellationToken = default)
    {
        var roadmaps = await _roadmapRepository.GetAllAsync(cancellationToken);
        foreach (var r in roadmaps)
        {
            r.IsActive = (r.Id == roadmapId);
            await _roadmapRepository.UpdateAsync(r, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRoadmapAsync(Guid roadmapId, CancellationToken cancellationToken = default)
    {
        var roadmap = await _roadmapRepository.GetByIdAsync(roadmapId, cancellationToken);
        if (roadmap != null)
        {
            await _roadmapRepository.DeleteAsync(roadmap, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static StudyRoadmapDto MapToDto(StudyRoadmap entity)
    {
        var milestonesDto = entity.Milestones
            .OrderBy(m => m.OrderIndex)
            .Select(m => new MilestoneDto(
                m.Id,
                m.RoadmapId,
                m.Title,
                m.Description,
                m.OrderIndex,
                m.TargetCompletionDate,
                m.IsCompleted,
                m.Nodes
                    .OrderBy(n => n.OrderIndex)
                    .Select(n => new RoadmapNodeDto(
                        n.Id,
                        n.MilestoneId,
                        n.Title,
                        n.Description,
                        n.Type,
                        n.Status,
                        n.OrderIndex,
                        n.CanvasX,
                        n.CanvasY,
                        n.TargetItemCount,
                        n.CompletedItemCount,
                        n.EaseFactorWeight,
                        n.ReferenceId,
                        n.ReferenceName))
                    .ToList()))
            .ToList();

        return new StudyRoadmapDto(
            entity.Id,
            entity.Title,
            entity.Target,
            entity.StartingScore,
            entity.TargetScore,
            entity.TargetDate,
            entity.IsActive,
            milestonesDto);
    }
}
