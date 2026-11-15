using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Enums;

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

    // Interactive Roleplay & Spaced Repetition (SM-2 / FSRS)
    Task<IReadOnlyList<PhraseRoleplayCardDto>> GetRoleplayQueueAsync(
        ConversationPhraseFilterDto? filter = null,
        int limit = 20,
        bool useFsrs = false,
        CancellationToken cancellationToken = default);

    Task<PhraseReviewResultDto> SubmitPhraseReviewAsync(
        Guid phraseId,
        ReviewRating rating,
        int responseTimeMs = 0,
        bool useFsrs = false,
        CancellationToken cancellationToken = default);

    Task<ConversationPhraseDto> EnqueueForSpacedRepetitionAsync(
        Guid phraseId,
        CancellationToken cancellationToken = default);

    // Import & Export (20MB limit, SafePathHelper validation, Trim & Dedup)
    Task<string> ExportToJsonAsync(
        IEnumerable<Guid>? phraseIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null);

    Task<string> ExportToCsvAsync(
        IEnumerable<Guid>? phraseIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null);

    Task<string> ExportToTxtAsync(
        IEnumerable<Guid>? phraseIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null);

    Task<string> ExportToFileAsync(
        string targetPath,
        string allowedBaseDirectory,
        DataFormat format,
        IEnumerable<Guid>? phraseIds = null,
        CancellationToken cancellationToken = default);

    Task<ImportResult> ImportFromJsonAsync(
        string jsonContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null);

    Task<ImportResult> ImportFromCsvAsync(
        string csvContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null);

    Task<ImportResult> ImportFromTxtAsync(
        string txtContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null);

    Task<ImportResult> ImportSmartAsync(
        string content,
        string? fileNameHint = null,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null);

    string GetSampleTemplate(string format);
}
