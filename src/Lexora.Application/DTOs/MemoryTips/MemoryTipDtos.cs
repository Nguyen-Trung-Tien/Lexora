using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs.MemoryTips;

/// <summary>
/// Observable DTO representing a high-yield memory tip, mnemonic hook, or exam trap.
/// Implements INotifyPropertyChanged for real-time WinUI 3 two-way data-binding reactivity.
/// </summary>
public class MemoryTipDto : INotifyPropertyChanged
{
    private Guid _id;
    private string _title = string.Empty;
    private string _mnemonicHook = string.Empty;
    private string _explanation = string.Empty;
    private string? _example;
    private string? _commonTrap;
    private TipCategory _category = TipCategory.Vocabulary;
    private MnemonicTechnique _technique = MnemonicTechnique.Acrostic;
    private ExamTarget? _examTarget;
    private bool _isPinned;
    private string? _tags;
    private Guid? _linkedWordId;
    private Guid? _linkedGrammarRuleId;
    private DateTime _createdAt;
    private DateTime _updatedAt;

    public Guid Id { get => _id; set => SetField(ref _id, value); }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string MnemonicHook
    {
        get => _mnemonicHook;
        set => SetField(ref _mnemonicHook, value);
    }

    public string Explanation
    {
        get => _explanation;
        set => SetField(ref _explanation, value);
    }

    public string? Example
    {
        get => _example;
        set
        {
            if (SetField(ref _example, value))
            {
                OnPropertyChanged(nameof(HasExample));
            }
        }
    }

    public string? CommonTrap
    {
        get => _commonTrap;
        set
        {
            if (SetField(ref _commonTrap, value))
            {
                OnPropertyChanged(nameof(HasCommonTrap));
            }
        }
    }

    public TipCategory Category
    {
        get => _category;
        set
        {
            if (SetField(ref _category, value))
            {
                OnPropertyChanged(nameof(CategoryName));
            }
        }
    }

    public MnemonicTechnique Technique
    {
        get => _technique;
        set
        {
            if (SetField(ref _technique, value))
            {
                OnPropertyChanged(nameof(TechniqueName));
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
                OnPropertyChanged(nameof(HasExamTarget));
                OnPropertyChanged(nameof(ExamTargetLabel));
            }
        }
    }

    public bool IsPinned
    {
        get => _isPinned;
        set => SetField(ref _isPinned, value);
    }

    public string? Tags
    {
        get => _tags;
        set
        {
            if (SetField(ref _tags, value))
            {
                OnPropertyChanged(nameof(HasTags));
            }
        }
    }

    public Guid? LinkedWordId
    {
        get => _linkedWordId;
        set => SetField(ref _linkedWordId, value);
    }

    public Guid? LinkedGrammarRuleId
    {
        get => _linkedGrammarRuleId;
        set => SetField(ref _linkedGrammarRuleId, value);
    }

    public DateTime CreatedAt { get => _createdAt; set => SetField(ref _createdAt, value); }
    public DateTime UpdatedAt { get => _updatedAt; set => SetField(ref _updatedAt, value); }

    // Read-only computed UI presentation properties
    public bool HasExample => !string.IsNullOrWhiteSpace(Example);
    public bool HasCommonTrap => !string.IsNullOrWhiteSpace(CommonTrap);
    public bool HasTags => !string.IsNullOrWhiteSpace(Tags);
    public bool HasExamTarget => ExamTarget.HasValue;

    public string CategoryName => Category switch
    {
        TipCategory.Vocabulary => "Vocabulary",
        TipCategory.Grammar => "Grammar",
        TipCategory.Tenses => "Tenses",
        TipCategory.Expressions => "Expressions",
        TipCategory.ExamStrategy => "Exam Strategy",
        _ => "General"
    };

    public string TechniqueName => Technique switch
    {
        MnemonicTechnique.Acrostic => "Acrostic / Hook",
        MnemonicTechnique.RhymeAndRhythm => "Rhyme & Rhythm",
        MnemonicTechnique.VisualImagery => "Visual Imagery",
        MnemonicTechnique.SoundAlike => "Sound-Alike",
        MnemonicTechnique.Storytelling => "Storytelling",
        MnemonicTechnique.Etymology => "Etymology & Roots",
        MnemonicTechnique.EliminationRule => "Elimination Rule",
        _ => "General Tip"
    };

    public string ExamTargetLabel => ExamTarget switch
    {
        Lexora.Domain.Enums.ExamTarget.Toeic => "TOEIC",
        Lexora.Domain.Enums.ExamTarget.Ielts => "IELTS",
        Lexora.Domain.Enums.ExamTarget.Vstep => "VSTEP",
        _ => string.Empty
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// Creation request payload for a new memory tip.
/// </summary>
public record CreateMemoryTipRequest
{
    public string Title { get; init; } = string.Empty;
    public string MnemonicHook { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public string? Example { get; init; }
    public string? CommonTrap { get; init; }
    public TipCategory Category { get; init; } = TipCategory.Vocabulary;
    public MnemonicTechnique Technique { get; init; } = MnemonicTechnique.Acrostic;
    public ExamTarget? ExamTarget { get; init; }
    public bool IsPinned { get; init; }
    public string? Tags { get; init; }
    public Guid? LinkedWordId { get; init; }
    public Guid? LinkedGrammarRuleId { get; init; }
}

/// <summary>
/// Mutation request payload to update an existing memory tip.
/// </summary>
public record UpdateMemoryTipRequest
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string MnemonicHook { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public string? Example { get; init; }
    public string? CommonTrap { get; init; }
    public TipCategory Category { get; init; } = TipCategory.Vocabulary;
    public MnemonicTechnique Technique { get; init; } = MnemonicTechnique.Acrostic;
    public ExamTarget? ExamTarget { get; init; }
    public bool IsPinned { get; init; }
    public string? Tags { get; init; }
    public Guid? LinkedWordId { get; init; }
    public Guid? LinkedGrammarRuleId { get; init; }
}

/// <summary>
/// Statistical summary of memory tips for dashboard and header badges.
/// </summary>
public record MemoryTipStatisticsDto
{
    public int TotalCount { get; init; }
    public int PinnedCount { get; init; }
    public int TrapCount { get; init; }
    public int VocabularyCount { get; init; }
    public int GrammarCount { get; init; }
    public int ExamStrategyCount { get; init; }
}

/// <summary>
/// Paginated result container for Memory Tips.
/// </summary>
public record MemoryTipsPagedResultDto(
    IReadOnlyList<MemoryTipDto> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize)) : 1;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
