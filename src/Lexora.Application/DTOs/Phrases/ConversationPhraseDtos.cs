using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public class ConversationPhraseDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ContextTag { get; set; } = string.Empty;
    public string PromptSentence { get; set; } = string.Empty;
    public string PromptTranslation { get; set; } = string.Empty;
    public string SampleResponse { get; set; } = string.Empty;
    public string ResponseTranslation { get; set; } = string.Empty;
    public string AdditionalSuggestions { get; set; } = string.Empty;
    public CefrLevel CefrLevel { get; set; } = CefrLevel.B1;
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateConversationPhraseDto
{
    public string Category { get; set; } = string.Empty;
    public string ContextTag { get; set; } = string.Empty;
    public string PromptSentence { get; set; } = string.Empty;
    public string PromptTranslation { get; set; } = string.Empty;
    public string SampleResponse { get; set; } = string.Empty;
    public string ResponseTranslation { get; set; } = string.Empty;
    public string? AdditionalSuggestions { get; set; }
    public CefrLevel CefrLevel { get; set; } = CefrLevel.B1;
    public bool IsFavorite { get; set; }
}

public class UpdateConversationPhraseDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ContextTag { get; set; } = string.Empty;
    public string PromptSentence { get; set; } = string.Empty;
    public string PromptTranslation { get; set; } = string.Empty;
    public string SampleResponse { get; set; } = string.Empty;
    public string ResponseTranslation { get; set; } = string.Empty;
    public string? AdditionalSuggestions { get; set; }
    public CefrLevel CefrLevel { get; set; } = CefrLevel.B1;
    public bool IsFavorite { get; set; }
}

public class ConversationPhraseFilterDto
{
    public string? Category { get; set; }
    public CefrLevel? CefrLevel { get; set; }
    public bool? OnlyFavorites { get; set; }
    public string? SearchQuery { get; set; }
}

public class PhraseSmartSuggestionDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TextToAppend { get; set; } = string.Empty;
    public string CategoryTag { get; set; } = string.Empty;
}
