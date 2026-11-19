using Lexora.Domain.Entities.Test;

namespace Lexora.Domain.Interfaces;

public interface IItemCalibrationRepository : IRepository<ItemCalibration>
{
    Task<ItemCalibration?> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, ItemCalibration>> GetByWordIdsAsync(IEnumerable<Guid> wordIds, CancellationToken cancellationToken = default);
}
