using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces;

public interface IRoadmapService
{
    Task<StudyRoadmapDto?> GetActiveRoadmapAsync(CancellationToken cancellationToken = default);
    Task<StudyRoadmapDto> GenerateAdaptiveRoadmapAsync(GenerateRoadmapRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateNodeProgressAsync(Guid nodeId, int newlyLearnedItems, double? easeFactor = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudyRoadmapDto>> GetAllRoadmapsAsync(CancellationToken cancellationToken = default);
    Task SetActiveRoadmapAsync(Guid roadmapId, CancellationToken cancellationToken = default);
    Task DeleteRoadmapAsync(Guid roadmapId, CancellationToken cancellationToken = default);
}

