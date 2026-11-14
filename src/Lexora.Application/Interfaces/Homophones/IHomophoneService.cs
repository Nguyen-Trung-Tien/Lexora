using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces;

public interface IHomophoneService
{
    Task<IReadOnlyList<HomophoneGroupDto>> GetAllGroupsAsync(
        HomophoneType? typeFilter = null,
        CefrLevel? levelFilter = null,
        ExamType? examFilter = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<HomophoneGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HomophoneGroupDto> CreateGroupAsync(CreateHomophoneGroupDto dto, CancellationToken cancellationToken = default);
    Task<HomophoneGroupDto> UpdateGroupAsync(UpdateHomophoneGroupDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HomophoneGroupDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
