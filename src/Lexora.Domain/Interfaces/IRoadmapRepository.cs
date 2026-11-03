using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Interfaces;

public interface IRoadmapRepository : IRepository<StudyRoadmap>
{
    Task<StudyRoadmap?> GetActiveRoadmapWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<StudyRoadmap?> GetRoadmapWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudyRoadmap>> GetRoadmapsByTargetAsync(ExamTarget target, CancellationToken cancellationToken = default);
    Task UpdateNodeStatusAsync(Guid nodeId, NodeStatus status, int completedItems, CancellationToken cancellationToken = default);
}

