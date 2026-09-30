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
            .AsNoTracking()
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

    public async Task<HashSet<string>> GetAllTitlesAsync(CancellationToken cancellationToken = default)
    {
        var titles = await DbSet
            .AsNoTracking()
            .Select(r => r.Title.ToLower())
            .ToListAsync(cancellationToken);
        return new HashSet<string>(titles, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, GrammarRule>> GetRulesByTitlesWithExamplesAsync(
        IEnumerable<string> titles,
        CancellationToken cancellationToken = default)
    {
        var titleList = titles.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.ToLower()).Distinct().ToList();
        if (titleList.Count == 0) return new Dictionary<string, GrammarRule>(StringComparer.OrdinalIgnoreCase);

        var rules = await DbSet
            .Include(r => r.Examples)
            .Where(r => titleList.Contains(r.Title.ToLower()))
            .ToListAsync(cancellationToken);

        var dict = new Dictionary<string, GrammarRule>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rules)
        {
            dict[r.Title.ToLower()] = r;
        }
        return dict;
    }

    public override async Task AddRangeAsync(IEnumerable<GrammarRule> rules, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(rules, cancellationToken);
    }

    public async Task<int> CountFilteredAsync(
        CefrLevel? level = null,
        ExamType? examType = null,
        string? topic = null,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking().AsQueryable();

        if (level.HasValue && level.Value != CefrLevel.None)
            q = q.Where(gr => gr.Level == level.Value);

        if (examType.HasValue && examType.Value != ExamType.General)
            q = q.Where(gr => gr.ExamTarget == examType.Value);

        if (!string.IsNullOrWhiteSpace(topic))
            q = q.Where(gr => gr.Topic != null && gr.Topic.ToLower() == topic.ToLower());

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            q = q.Where(gr => gr.Title.ToLower().Contains(normalized) ||
                              gr.Structure.ToLower().Contains(normalized) ||
                              gr.ExplanationVi.ToLower().Contains(normalized));
        }

        return await q.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrammarRule>> GetFilteredPagedAsync(
        int skip,
        int take,
        CefrLevel? level = null,
        ExamType? examType = null,
        string? topic = null,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking().Include(gr => gr.Examples).AsQueryable();

        if (level.HasValue && level.Value != CefrLevel.None)
            q = q.Where(gr => gr.Level == level.Value);

        if (examType.HasValue && examType.Value != ExamType.General)
            q = q.Where(gr => gr.ExamTarget == examType.Value);

        if (!string.IsNullOrWhiteSpace(topic))
            q = q.Where(gr => gr.Topic != null && gr.Topic.ToLower() == topic.ToLower());

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            q = q.Where(gr => gr.Title.ToLower().Contains(normalized) ||
                              gr.Structure.ToLower().Contains(normalized) ||
                              gr.ExplanationVi.ToLower().Contains(normalized));
        }

        return await q.OrderBy(gr => gr.OrderIndex)
                      .ThenBy(gr => gr.Title)
                      .Skip(skip)
                      .Take(take)
                      .ToListAsync(cancellationToken);
    }
}

