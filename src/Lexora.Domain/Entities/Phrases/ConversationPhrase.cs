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
    public PhraseRegister Register { get; set; } = PhraseRegister.Neutral;
    public ExamTarget? ExamTarget { get; set; }
    public string KeyPhrases { get; set; } = string.Empty;
    public string FormalVariation { get; set; } = string.Empty;
    public string CasualVariation { get; set; } = string.Empty;
    public string CommonMistakesVi { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }

    // Spaced Repetition (SM-2) tracking for Roleplay & Shadowing review
    public LearningStatus Status { get; set; } = LearningStatus.New;
    public double EaseFactor { get; set; } = 2.5;
    public int IntervalDays { get; set; }
    public int Repetitions { get; set; }
    public int Lapses { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public DateTime? NextReviewAt { get; set; }
    public Guid? LinkedWordId { get; set; }

    public string NormalizedPrompt { get; set; } = string.Empty;
    public string NormalizedResponse { get; set; } = string.Empty;

    public void UpdateNormalizedFields()
    {
        NormalizedPrompt = (PromptSentence ?? string.Empty).Trim().ToLowerInvariant();
        NormalizedResponse = (SampleResponse ?? string.Empty).Trim().ToLowerInvariant();
        if (EaseFactor < 1.3)
        {
            EaseFactor = 1.3;
        }
    }
}
