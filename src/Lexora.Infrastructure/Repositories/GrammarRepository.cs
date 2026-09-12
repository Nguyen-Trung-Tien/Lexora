using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

public class GrammarRepository : Repository<GrammarRule>, IGrammarRepository
{
    public GrammarRepository(LexoraDbContext context) : base(context)
    {
    }

    public async Task<GrammarRule?> GetWithExamplesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(gr => gr.Examples)
            .FirstOrDefaultAsync(gr => gr.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<GrammarRule>> GetByLevelAsync(CefrLevel level, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(gr => gr.Examples)
            .Where(gr => gr.Level == level)
            .OrderBy(gr => gr.OrderIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrammarRule>> GetByExamTargetAsync(ExamType examType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(gr => gr.Examples)
            .Where(gr => gr.ExamTarget == examType)
            .OrderBy(gr => gr.OrderIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrammarRule>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim().ToLowerInvariant();
        return await DbSet
            .Include(gr => gr.Examples)
            .Where(gr => gr.Title.ToLower().Contains(normalized) || gr.Structure.ToLower().Contains(normalized))
            .OrderBy(gr => gr.OrderIndex)
            .ToListAsync(cancellationToken);
    }
}
