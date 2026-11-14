using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities.Phrases;

public class ConversationPhrase : Entity
{
    public string Category { get; set; } = string.Empty;
    public string ContextTag { get; set; } = string.Empty;
    public string PromptSentence { get; set; } = string.Empty;
    public string PromptTranslation { get; set; } = string.Empty;
    public string SampleResponse { get; set; } = string.Empty;
    public string ResponseTranslation { get; set; } = string.Empty;
    public string AdditionalSuggestions { get; set; } = string.Empty;
    public CefrLevel CefrLevel { get; set; } = CefrLevel.B1;
    public bool IsFavorite { get; set; }
    public string NormalizedPrompt { get; set; } = string.Empty;
    public string NormalizedResponse { get; set; } = string.Empty;

    public void UpdateNormalizedFields()
    {
        NormalizedPrompt = (PromptSentence ?? string.Empty).Trim().ToLowerInvariant();
        NormalizedResponse = (SampleResponse ?? string.Empty).Trim().ToLowerInvariant();
    }
}
