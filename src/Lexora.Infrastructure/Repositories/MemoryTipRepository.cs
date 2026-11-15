using Lexora.Domain.Entities.MemoryTips;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

/// <summary>
/// EF Core repository implementation for MemoryTip entity.
/// Enforces AsNoTracking on all read queries for maximum read performance and zero tracker overhead.
/// </summary>
public class MemoryTipRepository : Repository<MemoryTip>, IMemoryTipRepository
{
    public MemoryTipRepository(LexoraDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<MemoryTip>> GetFilteredAsync(
        TipCategory? category = null,
        MnemonicTechnique? technique = null,
        ExamTarget? examTarget = null,
        bool? onlyPinned = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(category, technique, examTarget, onlyPinned, search);

        return await query
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<MemoryTip> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        TipCategory? category = null,
        MnemonicTechnique? technique = null,
        ExamTarget? examTarget = null,
        bool? onlyPinned = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Max(1, pageSize);

        var query = BuildFilteredQuery(category, technique, examTarget, onlyPinned, search);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<MemoryTip> BuildFilteredQuery(
        TipCategory? category,
        MnemonicTechnique? technique,
        ExamTarget? examTarget,
        bool? onlyPinned,
        string? search)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        if (technique.HasValue)
        {
            query = query.Where(t => t.Technique == technique.Value);
        }

        if (examTarget.HasValue)
        {
            query = query.Where(t => t.ExamTarget == examTarget.Value);
        }

        if (onlyPinned.HasValue && onlyPinned.Value)
        {
            query = query.Where(t => t.IsPinned);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(term) ||
                t.MnemonicHook.ToLower().Contains(term) ||
                t.Explanation.ToLower().Contains(term) ||
                (t.CommonTrap != null && t.CommonTrap.ToLower().Contains(term)) ||
                (t.Tags != null && t.Tags.ToLower().Contains(term)));
        }

        return query;
    }

    public async Task<IReadOnlyList<MemoryTip>> GetPinnedTipsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(t => t.IsPinned)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryTip>> GetByLinkedWordIdAsync(Guid wordId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(t => t.LinkedWordId == wordId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryTip>> GetByLinkedGrammarRuleIdAsync(Guid grammarRuleId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(t => t.LinkedGrammarRuleId == grammarRuleId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var rawTags = await DbSet.AsNoTracking()
            .Where(t => !string.IsNullOrEmpty(t.Tags))
            .Select(t => t.Tags!)
            .ToListAsync(cancellationToken);

        return rawTags
            .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
