using Lexora.Domain.Entities.Test;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

public class ItemCalibrationRepository : Repository<ItemCalibration>, IItemCalibrationRepository
{
    public ItemCalibrationRepository(LexoraDbContext context) : base(context)
    {
    }

    public async Task<ItemCalibration?> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.WordId == wordId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ItemCalibration>> GetByWordIdsAsync(
        IEnumerable<Guid> wordIds,
        CancellationToken cancellationToken = default)
    {
        var idList = wordIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, ItemCalibration>();
        }

        var items = await DbSet
            .Where(x => x.WordId != null && idList.Contains(x.WordId.Value))
            .ToListAsync(cancellationToken);

        return items
            .Where(x => x.WordId.HasValue)
            .ToDictionary(x => x.WordId!.Value, x => x);
    }
}
