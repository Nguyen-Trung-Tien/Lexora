using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IConversationPhraseService
{
    Task<IReadOnlyList<ConversationPhraseDto>> GetPhrasesAsync(
        ConversationPhraseFilterDto? filter = null,
        CancellationToken cancellationToken = default);

    Task<ConversationPhraseDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ConversationPhraseDto> CreateAsync(
        CreateConversationPhraseDto dto,
        CancellationToken cancellationToken = default);

    Task<ConversationPhraseDto> UpdateAsync(
        UpdateConversationPhraseDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ToggleFavoriteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhraseSmartSuggestionDto>> GetQuickTemplatesAsync(
        string? category = null,
        CancellationToken cancellationToken = default);

    Task<string> GenerateEnhancedSuggestionsAsync(
        string promptSentence,
        string sampleResponse,
        string category,
        CancellationToken cancellationToken = default);
}
