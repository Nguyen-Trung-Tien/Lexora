using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

public class ConversationPhraseRepository : Repository<ConversationPhrase>, IConversationPhraseRepository
{
    public ConversationPhraseRepository(LexoraDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ConversationPhrase>> GetFilteredAsync(
        string? category = null,
        CefrLevel? level = null,
        bool? onlyFavorites = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
        {
            var catLower = category.Trim().ToLowerInvariant();
            q = q.Where(p => p.Category.ToLower() == catLower);
        }

        if (level.HasValue && level.Value != CefrLevel.None)
        {
            q = q.Where(p => p.CefrLevel == level.Value);
        }

        if (onlyFavorites.HasValue && onlyFavorites.Value)
        {
            q = q.Where(p => p.IsFavorite);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(p =>
                p.NormalizedPrompt.Contains(term) ||
                p.PromptTranslation.ToLower().Contains(term) ||
                p.NormalizedResponse.Contains(term) ||
                p.ResponseTranslation.ToLower().Contains(term) ||
                p.Category.ToLower().Contains(term) ||
                p.ContextTag.ToLower().Contains(term));
        }

        return await q
            .OrderBy(p => p.Category)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Select(p => p.Category)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationPhrase>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(p => p.IsFavorite)
            .OrderBy(p => p.Category)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
