using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

/// <summary>
/// EF Core SQLite repository for ConversationPhrase with AsNoTracking read optimization
/// and multi-criteria filtering across scenarios, CEFR levels, registers, exams, and due queue.
/// </summary>
public class ConversationPhraseRepository(LexoraDbContext context)
    : Repository<ConversationPhrase>(context), IConversationPhraseRepository
{
    public Task<IReadOnlyList<ConversationPhrase>> GetFilteredAsync(
        string? category = null,
        CefrLevel? level = null,
        bool? onlyFavorites = null,
        string? search = null,
        CancellationToken cancellationToken = default)
        => GetFilteredAdvancedAsync(
            category: category,
            level: level,
            register: null,
            examTarget: null,
            onlyFavorites: onlyFavorites,
            onlyDueForReview: null,
            search: search,
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<ConversationPhrase>> GetFilteredAdvancedAsync(
        string? category = null,
        CefrLevel? level = null,
        PhraseRegister? register = null,
        ExamTarget? examTarget = null,
        bool? onlyFavorites = null,
        bool? onlyDueForReview = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            var trimmedCat = category.Trim().ToLowerInvariant();
            var aliases = ExpandScenarioAliases(trimmedCat);
            if (aliases.Count == 1)
            {
                var pattern = $"%{aliases[0]}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Category.ToLower(), pattern) ||
                    EF.Functions.Like(p.ContextTag.ToLower(), pattern));
            }
            else
            {
                var p0 = $"%{aliases[0]}%";
                var p1 = $"%{aliases[1]}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Category.ToLower(), p0) ||
                    EF.Functions.Like(p.ContextTag.ToLower(), p0) ||
                    EF.Functions.Like(p.Category.ToLower(), p1) ||
                    EF.Functions.Like(p.ContextTag.ToLower(), p1));
            }
        }

        if (level.HasValue && level.Value != CefrLevel.None)
        {
            query = query.Where(p => p.CefrLevel == level.Value);
        }

        if (register.HasValue)
        {
            query = query.Where(p => p.Register == register.Value);
        }

        if (examTarget.HasValue)
        {
            query = query.Where(p => p.ExamTarget == examTarget.Value);
        }

        if (onlyFavorites == true)
        {
            query = query.Where(p => p.IsFavorite);
        }

        if (onlyDueForReview == true)
        {
            var utcNow = DateTime.UtcNow;
            query = query.Where(p =>
                p.Status != LearningStatus.Mastered &&
                (!p.NextReviewAt.HasValue || p.NextReviewAt.Value <= utcNow));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim().ToLowerInvariant()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.NormalizedPrompt, term) ||
                EF.Functions.Like(p.NormalizedResponse, term) ||
                EF.Functions.Like(p.PromptTranslation.ToLower(), term) ||
                EF.Functions.Like(p.ResponseTranslation.ToLower(), term) ||
                EF.Functions.Like(p.ContextTag.ToLower(), term) ||
                EF.Functions.Like(p.Category.ToLower(), term) ||
                EF.Functions.Like(p.KeyPhrases.ToLower(), term) ||
                EF.Functions.Like(p.FormalVariation.ToLower(), term) ||
                EF.Functions.Like(p.CasualVariation.ToLower(), term) ||
                EF.Functions.Like(p.CommonMistakesVi.ToLower(), term) ||
                EF.Functions.Like(p.AdditionalSuggestions.ToLower(), term));
        }

        return await query
            .OrderByDescending(p => p.IsFavorite)
            .ThenByDescending(p => p.UpdatedAt)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationPhrase?> GetByNormalizedPromptAsync(
        string normalizedPrompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(normalizedPrompt))
        {
            return null;
        }

        var key = normalizedPrompt.Trim().ToLowerInvariant();
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.NormalizedPrompt == key, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationPhrase>> GetDuePhrasesAsync(
        DateTime utcNow,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        int cappedLimit = Math.Clamp(limit, 1, 200);
        return await DbSet
            .AsNoTracking()
            .Where(p => p.Status != LearningStatus.Mastered &&
                        (p.NextReviewAt == null || p.NextReviewAt <= utcNow))
            .OrderBy(p => p.NextReviewAt == null ? 1 : 0)
            .ThenBy(p => p.NextReviewAt ?? DateTime.MaxValue)
            .ThenByDescending(p => p.IsFavorite)
            .Take(cappedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(p => !string.IsNullOrEmpty(p.Category))
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationPhrase>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(p => p.IsFavorite)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyList<string> ExpandScenarioAliases(string normalizedCategory)
    {
        var terms = normalizedCategory
            .Split(['&', '/', ',', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length >= 3 && t != "and" && t != "the")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (terms.Count == 0)
        {
            return [normalizedCategory];
        }

        // Keep original plus primary token aliases (max 2 for parameterized EF.Functions.Like translation)
        if (terms.Count == 1)
        {
            return terms[0] switch
            {
                "workplace" => ["workplace", "office"],
                "business" => ["business", "toeic"],
                "academic" => ["academic", "ielts"],
                "travel" => ["travel", "airport"],
                "daily life" => ["daily", "social"],
                _ => [terms[0]]
            };
        }

        return [terms[0], terms[1]];
    }
}
