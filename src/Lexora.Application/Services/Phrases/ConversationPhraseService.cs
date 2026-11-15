using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services.Phrases;

/// <summary>
/// Application service managing Conversation Phrases, contextual filtering,
/// interactive roleplay spaced repetition, and secure import/export workflows.
/// </summary>
public partial class ConversationPhraseService(
    IConversationPhraseRepository repository,
    IUnitOfWork unitOfWork,
    ISpacedRepetitionEngine? spacedRepetitionEngine = null,
    IFsrsSpacedRepetitionEngine? fsrsEngine = null,
    IFuzzySearchService? fuzzySearchService = null,
    IReviewRepository? reviewRepository = null,
    IVocabularyRepository? vocabularyRepository = null) : IConversationPhraseService
{
    public async Task<ConversationPhraseDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var phrase = await repository.GetByIdAsync(id, cancellationToken);
        return phrase?.ToDto();
    }

    public async Task<ConversationPhraseDto> CreateAsync(
        CreateConversationPhraseDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateRequiredFields(
            dto.Category,
            dto.PromptSentence,
            dto.PromptTranslation,
            dto.SampleResponse,
            dto.ResponseTranslation);

        var entity = new ConversationPhrase
        {
            Category = NormalizeCategory(dto.Category),
            ContextTag = TrimOrEmpty(dto.ContextTag),
            PromptSentence = dto.PromptSentence.Trim(),
            PromptTranslation = dto.PromptTranslation.Trim(),
            SampleResponse = dto.SampleResponse.Trim(),
            ResponseTranslation = dto.ResponseTranslation.Trim(),
            AdditionalSuggestions = TrimOrEmpty(dto.AdditionalSuggestions),
            CefrLevel = dto.CefrLevel,
            Register = dto.Register,
            ExamTarget = dto.ExamTarget,
            KeyPhrases = TrimOrEmpty(dto.KeyPhrases),
            FormalVariation = TrimOrEmpty(dto.FormalVariation),
            CasualVariation = TrimOrEmpty(dto.CasualVariation),
            CommonMistakesVi = TrimOrEmpty(dto.CommonMistakesVi),
            IsFavorite = dto.IsFavorite,
            Status = LearningStatus.New,
            EaseFactor = 2.5
        };

        entity.UpdateNormalizedFields();

        await repository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }

    public async Task<ConversationPhraseDto> UpdateAsync(
        UpdateConversationPhraseDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateRequiredFields(
            dto.Category,
            dto.PromptSentence,
            dto.PromptTranslation,
            dto.SampleResponse,
            dto.ResponseTranslation);

        var entity = await repository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation phrase with ID {dto.Id} was not found.");

        entity.Category = NormalizeCategory(dto.Category);
        entity.ContextTag = TrimOrEmpty(dto.ContextTag);
        entity.PromptSentence = dto.PromptSentence.Trim();
        entity.PromptTranslation = dto.PromptTranslation.Trim();
        entity.SampleResponse = dto.SampleResponse.Trim();
        entity.ResponseTranslation = dto.ResponseTranslation.Trim();
        entity.AdditionalSuggestions = TrimOrEmpty(dto.AdditionalSuggestions);
        entity.CefrLevel = dto.CefrLevel;
        entity.Register = dto.Register;
        entity.ExamTarget = dto.ExamTarget;
        entity.KeyPhrases = TrimOrEmpty(dto.KeyPhrases);
        entity.FormalVariation = TrimOrEmpty(dto.FormalVariation);
        entity.CasualVariation = TrimOrEmpty(dto.CasualVariation);
        entity.CommonMistakesVi = TrimOrEmpty(dto.CommonMistakesVi);
        entity.IsFavorite = dto.IsFavorite;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdateNormalizedFields();

        await repository.UpdateAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation phrase with ID {id} was not found.");

        await repository.DeleteAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ToggleFavoriteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation phrase with ID {id} was not found.");

        entity.IsFavorite = !entity.IsFavorite;
        entity.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.IsFavorite;
    }

    private static void ValidateRequiredFields(
        string? category,
        string? promptSentence,
        string? promptTranslation,
        string? sampleResponse,
        string? responseTranslation)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category cannot be empty.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(promptSentence))
        {
            throw new ArgumentException("Prompt sentence cannot be empty.", nameof(promptSentence));
        }

        if (string.IsNullOrWhiteSpace(promptTranslation))
        {
            throw new ArgumentException("Prompt translation cannot be empty.", nameof(promptTranslation));
        }

        if (string.IsNullOrWhiteSpace(sampleResponse))
        {
            throw new ArgumentException("Sample response cannot be empty.", nameof(sampleResponse));
        }

        if (string.IsNullOrWhiteSpace(responseTranslation))
        {
            throw new ArgumentException("Response translation cannot be empty.", nameof(responseTranslation));
        }
    }

    private static string NormalizeCategory(string? category)
        => string.IsNullOrWhiteSpace(category) ? "Daily Life" : category.Trim();

    private static string TrimOrEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
