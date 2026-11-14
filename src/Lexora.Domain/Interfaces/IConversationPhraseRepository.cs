using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Interfaces;

public interface IConversationPhraseRepository : IRepository<ConversationPhrase>
{
    Task<IReadOnlyList<ConversationPhrase>> GetFilteredAsync(
        string? category = null,
        CefrLevel? level = null,
        bool? onlyFavorites = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationPhrase>> GetFavoritesAsync(CancellationToken cancellationToken = default);
}
