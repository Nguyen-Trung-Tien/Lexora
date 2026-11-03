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
        return await Context.StudyRoadmaps
            .Include(r => r.Milestones.OrderBy(m => m.OrderIndex))
                .ThenInclude(m => m.Nodes.OrderBy(n => n.OrderIndex))
            .FirstOrDefaultAsync(r => r.IsActive, cancellationToken);
    }

    public async Task<StudyRoadmap?> GetRoadmapWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.StudyRoadmaps
            .Include(r => r.Milestones.OrderBy(m => m.OrderIndex))
                .ThenInclude(m => m.Nodes.OrderBy(n => n.OrderIndex))
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
            await Context.SaveChangesAsync(cancellationToken);
        }
    }
}

