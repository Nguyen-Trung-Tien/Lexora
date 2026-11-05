using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

public class RoadmapRepository : Repository<StudyRoadmap>, IRoadmapRepository
{
    public RoadmapRepository(LexoraDbContext context) : base(context)
    {
    }

    public async Task<StudyRoadmap?> GetActiveRoadmapWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        var active = await Context.StudyRoadmaps
            .Include(r => r.Milestones)
                .ThenInclude(m => m.Nodes)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(r => r.IsActive, cancellationToken);

        if (active != null)
        {
            return active;
        }

        // Fallback auto-recovery: If no roadmap is currently marked active, but roadmaps exist in DB,
        // automatically activate and return the latest one to prevent UI blanking / state loss.
        var latest = await Context.StudyRoadmaps
            .Include(r => r.Milestones)
                .ThenInclude(m => m.Nodes)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest != null)
        {
            latest.IsActive = true;
            await Context.SaveChangesAsync(cancellationToken);
            return latest;
        }

        return null;
    }

    public async Task<StudyRoadmap?> GetRoadmapWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.StudyRoadmaps
            .Include(r => r.Milestones)
                .ThenInclude(m => m.Nodes)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StudyRoadmap>> GetRoadmapsByTargetAsync(ExamTarget target, CancellationToken cancellationToken = default)
    {
        return await Context.StudyRoadmaps
            .Where(r => r.Target == target)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateNodeStatusAsync(Guid nodeId, NodeStatus status, int completedItems, CancellationToken cancellationToken = default)
    {
        var node = await Context.RoadmapNodes.FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);
        if (node != null)
        {
            node.Status = status;
            node.CompletedItemCount = completedItems;
            node.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task DeactivateAllRoadmapsAsync(CancellationToken cancellationToken = default)
    {
        var activeRoadmaps = await Context.StudyRoadmaps.Where(r => r.IsActive).ToListAsync(cancellationToken);
        foreach (var r in activeRoadmaps)
        {
            r.IsActive = false;
            r.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task SetActiveRoadmapAsync(Guid roadmapId, CancellationToken cancellationToken = default)
    {
        var all = await Context.StudyRoadmaps.ToListAsync(cancellationToken);
        foreach (var r in all)
        {
            bool shouldBeActive = (r.Id == roadmapId);
            if (r.IsActive != shouldBeActive)
            {
                r.IsActive = shouldBeActive;
                r.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

