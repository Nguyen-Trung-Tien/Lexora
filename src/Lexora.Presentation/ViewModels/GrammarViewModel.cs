using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class GrammarViewModel : ObservableObject, IDisposable
{
    private readonly IGrammarService _grammarService;
    private readonly IGrammarExportImportService _grammarExportImportService;
    private readonly Services.ILocalizationService _localizationService;
    private readonly Services.ISoundFeedbackService? _soundFeedback;
    private readonly ISmartSearchService? _smartSearchService;
    private readonly List<GrammarQuizSubmissionDto> _practiceSubmissions = new();
    // Captured on the UI thread so post-await mutations can be safely marshalled back.
    private readonly DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyState))]
    public partial bool IsLoading { get; set; } = true;

    public bool IsEmptyState => !IsLoading && Rules.Count == 0;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<string> ClosestSuggestions { get; set; } = Array.Empty<string>();

    [ObservableProperty]
    public partial bool HasClosestSuggestions { get; set; }

    [ObservableProperty]
    public partial string? TypoSuggestion { get; set; }

    [ObservableProperty]
    public partial bool HasTypoSuggestion { get; set; }

    [ObservableProperty]
    public partial GrammarRelatedSuggestionsDto? RelatedSuggestions { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingSuggestions { get; set; }

    [ObservableProperty]
    public partial bool HasRelatedSuggestions { get; set; }

    [ObservableProperty]
    public partial string SelectedSuggestionTab { get; set; } = "All";

    [ObservableProperty]
    public partial GrammarRuleDto? SelectedRule { get; set; }

    [ObservableProperty]
    public partial CefrLevel? SelectedLevel { get; set; }

    [ObservableProperty]
    public partial ExamType? SelectedExamTarget { get; set; }

    [ObservableProperty]
    public partial string? SelectedTopic { get; set; }

    // Pagination State
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    [NotifyPropertyChangedFor(nameof(ItemRangeDisplayInfo))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    [NotifyPropertyChangedFor(nameof(ItemRangeDisplayInfo))]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemRangeDisplayInfo))]
    public partial int TotalRuleCount { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemRangeDisplayInfo))]
    public partial int PageSize { get; set; } = 20;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public string PageDisplayInfo => $"{CurrentPage} / {Math.Max(1, TotalPages)}";
    public string ItemRangeDisplayInfo => TotalRuleCount > 0
        ? $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalRuleCount)}-{Math.Min(CurrentPage * PageSize, TotalRuleCount)} / {TotalRuleCount}"
        : "0 / 0";

    partial void OnSearchQueryChanged(string value) => CurrentPage = 1;
    partial void OnSelectedLevelChanged(CefrLevel? value) => CurrentPage = 1;
    partial void OnSelectedExamTargetChanged(ExamType? value) => CurrentPage = 1;
    partial void OnSelectedTopicChanged(string? value) => CurrentPage = 1;
    partial void OnSelectedRuleChanged(GrammarRuleDto? value) => _ = LoadRuleSuggestionsAsync(value);

    // Practice Mode State
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMainMode))]
    public partial bool IsPracticeMode { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMainMode))]
    public partial bool IsPracticeResultMode { get; set; } = false;

    public bool IsMainMode => !IsPracticeMode && !IsPracticeResultMode;

    [ObservableProperty]
    public partial int CurrentQuestionIndex { get; set; } = 0;

    [ObservableProperty]
    public partial int TotalPracticeQuestions { get; set; } = 0;

    [ObservableProperty]
    public partial GrammarQuizQuestionDto? CurrentPracticeQuestion { get; set; }

    [ObservableProperty]
    public partial string? SelectedOption { get; set; }

    [ObservableProperty]
    public partial bool IsAnswerSubmitted { get; set; } = false;

    [ObservableProperty]
    public partial bool IsAnswerCorrect { get; set; } = false;

    [ObservableProperty]
    public partial string ExplanationText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PracticeProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double PracticeProgressPercent { get; set; } = 0.0;

    [ObservableProperty]
    public partial string PracticeStepBadge { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PracticeStatusTip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NextButtonText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResultSummaryMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial GrammarPracticeResultDto? PracticeResult { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCount))]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial bool IsSelectionMode { get; set; } = false;

    public ObservableCollection<Guid> SelectedRuleIds { get; } = new();

    public int SelectedCount => SelectedRuleIds.Count;
    public bool HasSelection => SelectedRuleIds.Count > 0;

    public ObservableCollection<GrammarRuleDto> Rules { get; } = new();
    public ObservableCollection<CefrLevel> CefrLevels { get; } = new();
    public ObservableCollection<ExamType> ExamTypes { get; } = new();
    public ObservableCollection<string> Topics { get; } = new();
    public ObservableCollection<TestOptionItem> CurrentPracticeOptions { get; } = new();
    public ObservableCollection<GrammarQuizQuestionDto> PracticeQuestions { get; } = new();

    /// <summary>Raised when StartPracticeAsync generates zero questions for the selected filters.</summary>
    public event Action? NoPracticeQuestionsAvailable;

    public GrammarViewModel(
        IGrammarService grammarService,
        IGrammarExportImportService grammarExportImportService,
        Services.ILocalizationService localizationService,
        Services.ISoundFeedbackService? soundFeedback = null,
        ISmartSearchService? smartSearchService = null)
    {
        _grammarService = grammarService;
        _grammarExportImportService = grammarExportImportService;
        _localizationService = localizationService;
        _soundFeedback = soundFeedback;
        _smartSearchService = smartSearchService;
        // Must be captured on the UI thread (constructor is always called from UI thread).
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? App.Current?.UIDispatcherQueue
            ?? App.Current?.MainWindow?.DispatcherQueue;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (CefrLevels.Count == 0)
        {
            foreach (var l in Enum.GetValues<CefrLevel>()) CefrLevels.Add(l);
        }

        if (ExamTypes.Count == 0)
        {
            foreach (var e in Enum.GetValues<ExamType>()) ExamTypes.Add(e);
        }

        await LoadTopicsAsync();
        await LoadRulesAsync();
    }

    private bool _isSavingRule = false;

    [RelayCommand]
    public async Task AddRuleAsync(GrammarRuleCreateOrUpdateDto dto)
    {
        if (_isSavingRule) return;
        _isSavingRule = true;
        try
        {
            var created = await _grammarService.CreateRuleAsync(dto);
            await LoadTopicsAsync();
            await LoadRulesAsync();
            SelectedRule = Rules.FirstOrDefault(r => r.Id == created.Id);
            if (SelectedRule == null)
            {
                ResetFilters();
                await LoadRulesAsync();
                SelectedRule = Rules.FirstOrDefault(r => r.Id == created.Id);
            }
        }
        finally
        {
            _isSavingRule = false;
        }
    }

    [RelayCommand]
    public async Task UpdateRuleAsync(GrammarRuleCreateOrUpdateDto dto)
    {
        if (_isSavingRule) return;
        _isSavingRule = true;
        try
        {
            var updated = await _grammarService.UpdateRuleAsync(dto);
            await LoadTopicsAsync();
            await LoadRulesAsync();
            SelectedRule = Rules.FirstOrDefault(r => r.Id == updated.Id);
            if (SelectedRule == null)
            {
                ResetFilters();
                await LoadRulesAsync();
                SelectedRule = Rules.FirstOrDefault(r => r.Id == updated.Id);
            }
        }
        finally
        {
            _isSavingRule = false;
        }
    }

    private bool _isDeletingSingleRule = false;

    [RelayCommand]
    public async Task DeleteRuleAsync(Guid id)
    {
        if (_isDeletingSingleRule) return;
        _isDeletingSingleRule = true;
        try
        {
            var rule = Rules.FirstOrDefault(r => r.Id == id);
            string title = rule?.Title ?? string.Empty;

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (SelectedRule?.Id == id)
            {
                SelectedRule = null;
            }

            await _grammarService.DeleteRuleAsync(id);

            await LoadTopicsAsync();
            await LoadRulesAsync();
        }
        finally
        {
            _isDeletingSingleRule = false;
        }
    }

    [RelayCommand]
    public void ToggleSelectionMode()
    {
        IsSelectionMode = !IsSelectionMode;
        if (!IsSelectionMode)
        {
            SelectedRuleIds.Clear();
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    [RelayCommand]
    public void SelectAllRules()
    {
        SelectedRuleIds.Clear();
        foreach (var rule in Rules)
        {
            SelectedRuleIds.Add(rule.Id);
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedRuleIds.Clear();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    public void ToggleRuleSelection(Guid id)
    {
        if (SelectedRuleIds.Contains(id))
        {
            SelectedRuleIds.Remove(id);
        }
        else
        {
            SelectedRuleIds.Add(id);
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    private bool _isDeletingRules = false;

    public async Task<int> DeleteSelectedRulesAsync(IEnumerable<Guid>? ids = null)
    {
        var targetIds = ids?.Distinct().ToList() ?? SelectedRuleIds.ToList();
        if (targetIds.Count == 0 || _isDeletingRules) return 0;
        _isDeletingRules = true;
        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;

        try
        {
            if (SelectedRule != null && targetIds.Contains(SelectedRule.Id))
            {
                SelectedRule = null;
            }

            int count = await Task.Run(() => _grammarService.DeleteRulesAsync(targetIds));

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            SelectedRuleIds.Clear();
            IsSelectionMode = false;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));

            await LoadTopicsAsync();
            await LoadRulesAsync();
            return count;
        }
        finally
        {
            _isDeletingRules = false;
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (IsLoading)
            {
                IsLoading = false;
            }
        }
    }

    private CancellationTokenSource? _suggestionsCts;

    [RelayCommand]
    public async Task LoadRuleSuggestionsAsync(GrammarRuleDto? rule = null)
    {
        var targetRule = rule ?? SelectedRule;
        if (targetRule == null)
        {
            RelatedSuggestions = null;
            HasRelatedSuggestions = false;
            return;
        }

        _suggestionsCts?.Cancel();
        _suggestionsCts?.Dispose();
        _suggestionsCts = new CancellationTokenSource();
        var ct = _suggestionsCts.Token;

        try
        {
            IsLoadingSuggestions = true;
            var suggestions = await _grammarService.GetGrammarSuggestionsAsync(targetRule.Id, ct);
            if (ct.IsCancellationRequested) return;

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            RelatedSuggestions = suggestions;
            HasRelatedSuggestions = suggestions != null && suggestions.TotalSuggestionsCount > 0;
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarViewModel] LoadRuleSuggestionsAsync error: {ex.Message}");
        }
        finally
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            IsLoadingSuggestions = false;
        }
    }

    [RelayCommand]
    public async Task SelectSuggestedRuleAsync(GrammarSuggestionItemDto? suggestion)
    {
        if (suggestion == null) return;

        var existing = Rules.FirstOrDefault(r => r.Id == suggestion.Rule.Id);
        if (existing != null)
        {
            SelectedRule = existing;
            return;
        }

        var rule = await _grammarService.GetRuleByIdAsync(suggestion.Rule.Id);
        if (rule != null)
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (!Rules.Any(r => r.Id == rule.Id))
            {
                Rules.Insert(0, rule);
            }
            SelectedRule = rule;
        }
    }

    public void Dispose()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        _suggestionsCts?.Cancel();
        _suggestionsCts?.Dispose();
        _suggestionsCts = null;

        GC.SuppressFinalize(this);
    }
}
