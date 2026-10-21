using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

public class HomophoneRepository : Repository<HomophoneGroup>, IHomophoneRepository
{
    public HomophoneRepository(LexoraDbContext context) : base(context)
    {
    }

    public async Task<HomophoneGroup?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<HomophoneGroup>> GetAllWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(g => g.Items)
            .OrderBy(g => g.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HomophoneGroup>> GetFilteredAsync(
        HomophoneType? type = null,
        CefrLevel? level = null,
        ExamType? examType = null,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var q = DbSet
            .AsNoTracking()
            .Include(g => g.Items)
            .AsQueryable();

        if (type.HasValue)
        {
            q = q.Where(g => g.Type == type.Value);
        }

        if (level.HasValue && level.Value != CefrLevel.None)
        {
            q = q.Where(g => g.DifficultyLevel == level.Value);
        }

        if (examType.HasValue)
        {
            q = q.Where(g => g.ExamTag == examType.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            var stripped = normalized.Trim('/', '[', ']', ' ');
            q = q.Where(g => g.Title.ToLower().Contains(normalized) ||
                             g.PhoneticIpa.ToLower().Contains(normalized) ||
                             (!string.IsNullOrEmpty(stripped) && g.PhoneticIpa.ToLower().Contains(stripped)) ||
                             (g.DifferentiationTip != null && g.DifferentiationTip.ToLower().Contains(normalized)) ||
                             g.Items.Any(i => i.Term.ToLower().Contains(normalized) ||
                                              i.MeaningVi.ToLower().Contains(normalized) ||
                                              (i.MeaningEn != null && i.MeaningEn.ToLower().Contains(normalized))));
        }

        return await q.OrderBy(g => g.Title).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HomophoneGroup>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return await GetFilteredAsync(query: query, cancellationToken: cancellationToken);
    }

    public async Task<HashSet<string>> GetAllGroupTitlesAsync(CancellationToken cancellationToken = default)
    {
        var titles = await DbSet
            .AsNoTracking()
            .Select(g => g.Title.Trim().ToLower())
            .ToListAsync(cancellationToken);

        return titles.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, HomophoneGroup>> GetGroupsByTitlesWithItemsAsync(IEnumerable<string> titles, CancellationToken cancellationToken = default)
    {
        var titleSet = titles.Select(t => t.Trim().ToLowerInvariant()).ToHashSet();
        var groups = await DbSet
            .Include(g => g.Items)
            .Where(g => titleSet.Contains(g.Title.ToLower()))
            .ToListAsync(cancellationToken);

        return groups.ToDictionary(g => g.Title.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
    }

    public void RemoveItems(IEnumerable<HomophoneItem> items)
    {
        Context.HomophoneItems.RemoveRange(items);
    }

    public void AddItems(IEnumerable<HomophoneItem> items)
    {
        Context.HomophoneItems.AddRange(items);
    }
}
