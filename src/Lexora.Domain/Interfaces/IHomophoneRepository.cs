using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Interfaces;

public interface IHomophoneRepository : IRepository<HomophoneGroup>
{
    Task<HomophoneGroup?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HomophoneGroup>> GetAllWithItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HomophoneGroup>> GetFilteredAsync(
        HomophoneType? type = null,
        CefrLevel? level = null,
        ExamType? examType = null,
        string? query = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HomophoneGroup>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetAllGroupTitlesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, HomophoneGroup>> GetGroupsByTitlesWithItemsAsync(IEnumerable<string> titles, CancellationToken cancellationToken = default);
    void RemoveItems(IEnumerable<HomophoneItem> items);
    void AddItems(IEnumerable<HomophoneItem> items);
}
