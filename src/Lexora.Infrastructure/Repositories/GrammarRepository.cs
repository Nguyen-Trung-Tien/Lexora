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
            .AsNoTracking()
            .Include(gr => gr.Examples)
            .Where(gr => gr.Level == level)
            .OrderBy(gr => gr.OrderIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrammarRule>> GetByExamTargetAsync(ExamType examType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(gr => gr.Examples)
            .Where(gr => gr.ExamTarget == examType)
            .OrderBy(gr => gr.OrderIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrammarRule>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim().ToLowerInvariant();
        return await DbSet
            .AsNoTracking()
            .Include(gr => gr.Examples)
            .Where(gr => gr.Title.ToLower().Contains(normalized) || gr.Structure.ToLower().Contains(normalized))
            .OrderBy(gr => gr.OrderIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrammarRule>> GetFilteredAsync(
        CefrLevel? level = null,
        ExamType? examType = null,
        string? topic = null,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking().Include(gr => gr.Examples).AsQueryable();

        if (level.HasValue && level.Value != CefrLevel.None)
        {
            q = q.Where(gr => gr.Level == level.Value);
        }

        if (examType.HasValue && examType.Value != ExamType.General)
        {
            q = q.Where(gr => gr.ExamTarget == examType.Value);
        }

        if (!string.IsNullOrWhiteSpace(topic))
        {
            q = q.Where(gr => gr.Topic != null && gr.Topic.ToLower() == topic.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            q = q.Where(gr => gr.Title.ToLower().Contains(normalized) ||
                              gr.Structure.ToLower().Contains(normalized) ||
                              gr.ExplanationVi.ToLower().Contains(normalized));
        }

        return await q.OrderBy(gr => gr.OrderIndex).ThenBy(gr => gr.Title).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAllTopicsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(gr => !string.IsNullOrEmpty(gr.Topic))
            .Select(gr => gr.Topic!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);
    }

    public void RemoveExamples(IEnumerable<GrammarExample> examples)
    {
        Context.GrammarExamples.RemoveRange(examples);
    }

    public void AddExamples(IEnumerable<GrammarExample> examples)
    {
        Context.GrammarExamples.AddRange(examples);
    }
}

