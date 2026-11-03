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
    private readonly RoadmapEngine _roadmapEngine;

    public RoadmapService(
        IRoadmapRepository roadmapRepository,
        IUnitOfWork unitOfWork)
    {
        _roadmapRepository = roadmapRepository ?? throw new ArgumentNullException(nameof(roadmapRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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

        // 2. Generate new roadmap graph via RoadmapEngine
        var newRoadmap = _roadmapEngine.GenerateAdaptiveRoadmap(
            request.Title,
            request.Target,
            request.StartingScore,
            request.TargetScore,
            request.DurationWeeks);

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
                        n.ReferenceId))
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

