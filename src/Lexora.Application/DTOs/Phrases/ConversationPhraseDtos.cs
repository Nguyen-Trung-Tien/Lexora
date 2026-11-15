using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Lexora.Application.Common;
using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public class ConversationPhraseDto : INotifyPropertyChanged
{
    private Guid _id;
    private string _category = string.Empty;
    private string _contextTag = string.Empty;
    private string _promptSentence = string.Empty;
    private string _promptTranslation = string.Empty;
    private string _sampleResponse = string.Empty;
    private string _responseTranslation = string.Empty;
    private string _additionalSuggestions = string.Empty;
    private CefrLevel _cefrLevel = CefrLevel.B1;
    private PhraseRegister _register = PhraseRegister.Neutral;
    private ExamTarget? _examTarget;
    private string _keyPhrases = string.Empty;
    private string _formalVariation = string.Empty;
    private string _casualVariation = string.Empty;
    private string _commonMistakesVi = string.Empty;
    private bool _isFavorite;
    private LearningStatus _status = LearningStatus.New;
    private double _easeFactor = 2.5;
    private int _intervalDays;
    private int _repetitions;
    private int _lapses;
    private DateTime? _lastReviewedAt;
    private DateTime? _nextReviewAt;
    private Guid? _linkedWordId;
    private bool _isPlayingPrompt;
    private bool _isPlayingResponse;
    private DateTime _createdAt;
    private DateTime _updatedAt;

    public Guid Id { get => _id; set => SetField(ref _id, value); }
    public string Category { get => _category; set => SetField(ref _category, value); }
    public string ContextTag
    {
        get => _contextTag;
        set
        {
            if (SetField(ref _contextTag, value))
            {
                OnPropertyChanged(nameof(HasContextTag));
            }
        }
    }
    public string PromptSentence { get => _promptSentence; set => SetField(ref _promptSentence, value); }
    public string PromptTranslation { get => _promptTranslation; set => SetField(ref _promptTranslation, value); }
    public string SampleResponse { get => _sampleResponse; set => SetField(ref _sampleResponse, value); }
    public string ResponseTranslation { get => _responseTranslation; set => SetField(ref _responseTranslation, value); }
    public string AdditionalSuggestions
    {
        get => _additionalSuggestions;
        set
        {
            if (SetField(ref _additionalSuggestions, value))
            {
                OnPropertyChanged(nameof(HasAdditionalSuggestions));
            }
        }
    }
    public CefrLevel CefrLevel { get => _cefrLevel; set => SetField(ref _cefrLevel, value); }
    public PhraseRegister Register
    {
        get => _register;
        set
        {
            if (SetField(ref _register, value))
            {
                OnPropertyChanged(nameof(RegisterLabel));
            }
        }
    }
    public ExamTarget? ExamTarget
    {
        get => _examTarget;
        set
        {
            if (SetField(ref _examTarget, value))
            {
                OnPropertyChanged(nameof(ExamTargetLabel));
                OnPropertyChanged(nameof(HasExamTarget));
            }
        }
    }
    public string KeyPhrases
    {
        get => _keyPhrases;
        set
        {
            if (SetField(ref _keyPhrases, value))
            {
                OnPropertyChanged(nameof(HasKeyPhrases));
            }
        }
    }
    public string FormalVariation
    {
        get => _formalVariation;
        set
        {
            if (SetField(ref _formalVariation, value))
            {
                OnPropertyChanged(nameof(HasFormalVariation));
                OnPropertyChanged(nameof(HasVariations));
            }
        }
    }
    public string CasualVariation
    {
        get => _casualVariation;
        set
        {
            if (SetField(ref _casualVariation, value))
            {
                OnPropertyChanged(nameof(HasCasualVariation));
                OnPropertyChanged(nameof(HasVariations));
            }
        }
    }
    public string CommonMistakesVi
    {
        get => _commonMistakesVi;
        set
        {
            if (SetField(ref _commonMistakesVi, value))
            {
                OnPropertyChanged(nameof(HasCommonMistakesVi));
            }
        }
    }
    public bool IsFavorite { get => _isFavorite; set => SetField(ref _isFavorite, value); }
    public LearningStatus Status
    {
        get => _status;
        set
        {
            if (SetField(ref _status, value))
            {
                OnPropertyChanged(nameof(IsDueForReview));
                OnPropertyChanged(nameof(IsQueuedForReview));
                OnPropertyChanged(nameof(StatusLabel));
            }
        }
    }
    public double EaseFactor
    {
        get => _easeFactor;
        set
        {
            if (SetField(ref _easeFactor, value))
            {
                OnPropertyChanged(nameof(EaseFactorText));
            }
        }
    }
    public int IntervalDays
    {
        get => _intervalDays;
        set
        {
            if (SetField(ref _intervalDays, value))
            {
                OnPropertyChanged(nameof(IntervalLabel));
            }
        }
    }
    public int Repetitions { get => _repetitions; set => SetField(ref _repetitions, value); }
    public int Lapses { get => _lapses; set => SetField(ref _lapses, value); }
    public DateTime? LastReviewedAt { get => _lastReviewedAt; set => SetField(ref _lastReviewedAt, value); }
    public DateTime? NextReviewAt
    {
        get => _nextReviewAt;
        set
        {
            if (SetField(ref _nextReviewAt, value))
            {
                OnPropertyChanged(nameof(IsDueForReview));
                OnPropertyChanged(nameof(IsQueuedForReview));
                OnPropertyChanged(nameof(IntervalLabel));
            }
        }
    }
    public Guid? LinkedWordId
    {
        get => _linkedWordId;
        set
        {
            if (SetField(ref _linkedWordId, value))
            {
                OnPropertyChanged(nameof(IsQueuedForReview));
            }
        }
    }
    public bool IsPlayingPrompt
    {
        get => _isPlayingPrompt;
        set
        {
            if (SetField(ref _isPlayingPrompt, value))
            {
                OnPropertyChanged(nameof(PromptAudioGlyph));
                OnPropertyChanged(nameof(IsAnyAudioPlaying));
            }
        }
    }
    public bool IsPlayingResponse
    {
        get => _isPlayingResponse;
        set
        {
            if (SetField(ref _isPlayingResponse, value))
            {
                OnPropertyChanged(nameof(ResponseAudioGlyph));
                OnPropertyChanged(nameof(IsAnyAudioPlaying));
            }
        }
    }
    public DateTime CreatedAt { get => _createdAt; set => SetField(ref _createdAt, value); }
    public DateTime UpdatedAt { get => _updatedAt; set => SetField(ref _updatedAt, value); }

    public bool HasContextTag => !string.IsNullOrWhiteSpace(ContextTag);
    public bool HasAdditionalSuggestions => !string.IsNullOrWhiteSpace(AdditionalSuggestions);
    public bool HasKeyPhrases => !string.IsNullOrWhiteSpace(KeyPhrases);
    public bool HasFormalVariation => !string.IsNullOrWhiteSpace(FormalVariation);
    public bool HasCasualVariation => !string.IsNullOrWhiteSpace(CasualVariation);
    public bool HasVariations => HasFormalVariation || HasCasualVariation;
    public bool HasCommonMistakesVi => !string.IsNullOrWhiteSpace(CommonMistakesVi);
    public bool HasExamTarget => ExamTarget.HasValue;
    public string RegisterLabel => Register.ToString();
    public string ExamTargetLabel => ExamTarget?.ToString().ToUpperInvariant() ?? "General";
    public string StatusLabel => Status.ToString();
    public string PromptAudioGlyph => IsPlayingPrompt ? "\uE769" : "\uE767";
    public string ResponseAudioGlyph => IsPlayingResponse ? "\uE769" : "\uE767";
    public bool IsAnyAudioPlaying => IsPlayingPrompt || IsPlayingResponse;
    public bool IsDueForReview => NextReviewAt.HasValue && NextReviewAt.Value <= DateTime.UtcNow && Status != LearningStatus.Mastered;
    public bool IsQueuedForReview => NextReviewAt.HasValue || LinkedWordId.HasValue || Status != LearningStatus.New;
    public string IntervalLabel => IntervalDays <= 0 ? (NextReviewAt.HasValue ? "< 1d" : string.Empty) : $"{IntervalDays}d";
    public string EaseFactorText => EaseFactor.ToString("F2");

    public void UpdateFrom(ConversationPhraseDto source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Category = source.Category;
        ContextTag = source.ContextTag;
        PromptSentence = source.PromptSentence;
        PromptTranslation = source.PromptTranslation;
        SampleResponse = source.SampleResponse;
        ResponseTranslation = source.ResponseTranslation;
        AdditionalSuggestions = source.AdditionalSuggestions;
        CefrLevel = source.CefrLevel;
        Register = source.Register;
        ExamTarget = source.ExamTarget;
        KeyPhrases = source.KeyPhrases;
        FormalVariation = source.FormalVariation;
        CasualVariation = source.CasualVariation;
        CommonMistakesVi = source.CommonMistakesVi;
        IsFavorite = source.IsFavorite;
        Status = source.Status;
        EaseFactor = source.EaseFactor;
        IntervalDays = source.IntervalDays;
        Repetitions = source.Repetitions;
        Lapses = source.Lapses;
        LastReviewedAt = source.LastReviewedAt;
        NextReviewAt = source.NextReviewAt;
        LinkedWordId = source.LinkedWordId;
        UpdatedAt = source.UpdatedAt;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    public PhraseRegister Register { get; set; } = PhraseRegister.Neutral;
    public ExamTarget? ExamTarget { get; set; }
    public string? KeyPhrases { get; set; }
    public string? FormalVariation { get; set; }
    public string? CasualVariation { get; set; }
    public string? CommonMistakesVi { get; set; }
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
    public PhraseRegister Register { get; set; } = PhraseRegister.Neutral;
    public ExamTarget? ExamTarget { get; set; }
    public string? KeyPhrases { get; set; }
    public string? FormalVariation { get; set; }
    public string? CasualVariation { get; set; }
    public string? CommonMistakesVi { get; set; }
    public bool IsFavorite { get; set; }
}

public class ConversationPhraseFilterDto
{
    public string? Category { get; set; }
    public CefrLevel? CefrLevel { get; set; }
    public PhraseRegister? Register { get; set; }
    public ExamTarget? ExamTarget { get; set; }
    public bool? OnlyFavorites { get; set; }
    public bool? OnlyDueForReview { get; set; }
    public string? SearchQuery { get; set; }
}

public class PhraseSmartSuggestionDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TextToAppend { get; set; } = string.Empty;
    public string CategoryTag { get; set; } = string.Empty;
    public PhraseRegister Register { get; set; } = PhraseRegister.Neutral;
    public ExamTarget? ExamTarget { get; set; }
    public string KeyPhrases { get; set; } = string.Empty;
    public string FormalVariation { get; set; } = string.Empty;
    public string CasualVariation { get; set; } = string.Empty;
    public string CommonMistakesVi { get; set; } = string.Empty;
}

public class PhraseRoleplayCardDto
{
    public ConversationPhraseDto Phrase { get; set; } = new();
    public IReadOnlyList<string> KeyPhraseTokens { get; set; } = Array.Empty<string>();
    public string IntervalAgainPreview { get; set; } = "< 1d";
    public string IntervalHardPreview { get; set; } = "1d";
    public string IntervalGoodPreview { get; set; } = "1d";
    public string IntervalEasyPreview { get; set; } = "4d";
}

public record PhraseReviewResultDto(
    Guid PhraseId,
    ReviewRating Rating,
    LearningStatus PreviousStatus,
    LearningStatus NewStatus,
    double NewEaseFactor,
    int NewIntervalDays,
    int Repetitions,
    int Lapses,
    DateTime NextReviewAt,
    bool SyncedStudyLog
);

public class ConversationPhraseExportModel
{
    public string Category { get; set; } = string.Empty;
    public string ContextTag { get; set; } = string.Empty;
    public string PromptSentence { get; set; } = string.Empty;
    public string PromptTranslation { get; set; } = string.Empty;
    public string SampleResponse { get; set; } = string.Empty;
    public string ResponseTranslation { get; set; } = string.Empty;
    public string? AdditionalSuggestions { get; set; }

    [JsonConverter(typeof(FlexibleCefrLevelJsonConverter))]
    public CefrLevel CefrLevel { get; set; } = CefrLevel.B1;

    public PhraseRegister Register { get; set; } = PhraseRegister.Neutral;
    public ExamTarget? ExamTarget { get; set; }
    public string? KeyPhrases { get; set; }
    public string? FormalVariation { get; set; }
    public string? CasualVariation { get; set; }
    public string? CommonMistakesVi { get; set; }
    public bool IsFavorite { get; set; }
}
