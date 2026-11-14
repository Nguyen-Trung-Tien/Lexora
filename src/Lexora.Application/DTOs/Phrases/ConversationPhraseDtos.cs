using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    private bool _isFavorite;
    private DateTime _createdAt;
    private DateTime _updatedAt;

    public Guid Id { get => _id; set => SetField(ref _id, value); }
    public string Category { get => _category; set => SetField(ref _category, value); }
    public string ContextTag { get => _contextTag; set => SetField(ref _contextTag, value); }
    public string PromptSentence { get => _promptSentence; set => SetField(ref _promptSentence, value); }
    public string PromptTranslation { get => _promptTranslation; set => SetField(ref _promptTranslation, value); }
    public string SampleResponse { get => _sampleResponse; set => SetField(ref _sampleResponse, value); }
    public string ResponseTranslation { get => _responseTranslation; set => SetField(ref _responseTranslation, value); }
    public string AdditionalSuggestions { get => _additionalSuggestions; set => SetField(ref _additionalSuggestions, value); }
    public CefrLevel CefrLevel { get => _cefrLevel; set => SetField(ref _cefrLevel, value); }
    public bool IsFavorite { get => _isFavorite; set => SetField(ref _isFavorite, value); }
    public DateTime CreatedAt { get => _createdAt; set => SetField(ref _createdAt, value); }
    public DateTime UpdatedAt { get => _updatedAt; set => SetField(ref _updatedAt, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
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
