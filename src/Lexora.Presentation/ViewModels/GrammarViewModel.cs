using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class GrammarViewModel : ObservableObject
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
            ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
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

    [RelayCommand]
    public async Task LoadTopicsAsync()
    {
        try
        {
            var topics = await _grammarService.GetAllTopicsAsync();
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            Topics.Clear();
            foreach (var t in topics) Topics.Add(t);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarViewModel] LoadTopicsAsync error: {ex.Message}");
        }
    }

    private CancellationTokenSource? _loadCts;

    [RelayCommand]
    public async Task LoadRulesAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;
        try
        {
            var pagedResult = await _grammarService.GetPagedRulesAsync(
                page: CurrentPage,
                pageSize: PageSize,
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                examType: SelectedExamTarget == ExamType.General ? null : SelectedExamTarget,
                topic: SelectedTopic,
                search: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                cancellationToken: ct
            );

            if (ct.IsCancellationRequested) return;

            await _dispatcherQueue.EnsureOnUIThreadAsync();

            TotalRuleCount = pagedResult.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalRuleCount / PageSize));

            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
                pagedResult = await _grammarService.GetPagedRulesAsync(
                    page: CurrentPage,
                    pageSize: PageSize,
                    level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                    examType: SelectedExamTarget == ExamType.General ? null : SelectedExamTarget,
                    topic: SelectedTopic,
                    search: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                    cancellationToken: ct
                );
                if (ct.IsCancellationRequested) return;
                await _dispatcherQueue.EnsureOnUIThreadAsync();
                TotalRuleCount = pagedResult.TotalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalRuleCount / PageSize));
            }
            if (CurrentPage < 1) CurrentPage = 1;

            Rules.Clear();
            foreach (var r in pagedResult.Rules)
            {
                Rules.Add(r);
            }

            if (Rules.Count > 0)
            {
                if (SelectedRule == null || !Rules.Any(r => r.Id == SelectedRule.Id))
                {
                    SelectedRule = Rules[0];
                }
                ClosestSuggestions = Array.Empty<string>();
                HasClosestSuggestions = false;
            }
            else
            {
                SelectedRule = null;
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    try
                    {
                        var smartRes = await _grammarService.SmartSearchRulesAsync(
                            search: SearchQuery,
                            level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                            examType: SelectedExamTarget == ExamType.General ? null : SelectedExamTarget,
                            topic: SelectedTopic,
                            page: 1,
                            pageSize: 1,
                            cancellationToken: ct);

                        if (smartRes.ClosestSuggestions.Count > 0)
                        {
                            ClosestSuggestions = smartRes.ClosestSuggestions;
                            HasClosestSuggestions = true;
                        }
                        else
                        {
                            ClosestSuggestions = Array.Empty<string>();
                            HasClosestSuggestions = false;
                        }
                    }
                    catch
                    {
                        ClosestSuggestions = Array.Empty<string>();
                        HasClosestSuggestions = false;
                    }
                }
                else
                {
                    ClosestSuggestions = Array.Empty<string>();
                    HasClosestSuggestions = false;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancelled queries
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarViewModel] LoadRulesAsync error: {ex.Message}");
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public async Task FirstPageAsync()
    {
        if (CurrentPage != 1)
        {
            CurrentPage = 1;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public async Task LastPageAsync()
    {
        if (CurrentPage != TotalPages)
        {
            CurrentPage = TotalPages;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public async Task ChangePageSizeAsync(int newSize)
    {
        if (newSize > 0 && PageSize != newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public void ResetFilters()
    {
        SearchQuery = string.Empty;
        SelectedLevel = null;
        SelectedExamTarget = null;
        SelectedTopic = null;
        CurrentPage = 1;
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

    // ==========================================
    // Grammar Practice / Quiz methods
    // ==========================================
    private bool _isStartingPractice = false;

    [RelayCommand]
    public async Task StartPracticeAsync()
    {
        if (_isStartingPractice) return;
        _isStartingPractice = true;
        IsLoading = true;
        try
        {
            var questions = await _grammarService.GenerateGrammarQuizAsync(
                count: 5,
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                examType: SelectedExamTarget == ExamType.General ? null : SelectedExamTarget
            );

            if (questions.Count == 0)
            {
                NoPracticeQuestionsAvailable?.Invoke();
                return;
            }

            PracticeQuestions.Clear();
            foreach (var q in questions) PracticeQuestions.Add(q);

            TotalPracticeQuestions = PracticeQuestions.Count;
            CurrentQuestionIndex = 0;
            _practiceSubmissions.Clear();

            LoadPracticeQuestion(0);

            IsPracticeResultMode = false;
            IsPracticeMode = true;
        }
        finally
        {
            IsLoading = false;
            _isStartingPractice = false;
        }
    }

    private void LoadPracticeQuestion(int index)
    {
        if (index < 0 || index >= PracticeQuestions.Count) return;

        CurrentQuestionIndex = index;
        CurrentPracticeQuestion = PracticeQuestions[index];
        PracticeProgressText = string.Format(_localizationService.GetString("Quiz_ProgressFormat"), index + 1, TotalPracticeQuestions);
        PracticeProgressPercent = (double)(index + 1) / TotalPracticeQuestions * 100.0;
        PracticeStepBadge = $"{_localizationService.GetString("Test_Question").ToUpperInvariant()} {index + 1} / {TotalPracticeQuestions}";

        CurrentPracticeOptions.Clear();
        for (int i = 0; i < CurrentPracticeQuestion.Options.Count; i++)
        {
            char letter = (char)('A' + i);
            CurrentPracticeOptions.Add(new TestOptionItem
            {
                Text = CurrentPracticeQuestion.Options[i],
                State = TestOptionState.Default,
                Glyph = "\uE739",
                Letter = letter.ToString()
            });
        }

        SelectedOption = null;
        IsAnswerSubmitted = false;
        IsAnswerCorrect = false;
        ExplanationText = string.Empty;

        UpdatePracticeStatusTip();
        UpdateNextButtonText();
    }

    [RelayCommand]
    public void SelectPracticeOption(string option)
    {
        if (IsAnswerSubmitted || CurrentPracticeQuestion == null) return;

        SelectedOption = option;
        IsAnswerSubmitted = true;
        IsAnswerCorrect = string.Equals(option.Trim(), CurrentPracticeQuestion.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        ExplanationText = CurrentPracticeQuestion.Explanation;

        if (IsAnswerCorrect)
        {
            _soundFeedback?.PlayStreakSound();
        }
        else
        {
            _soundFeedback?.PlaySoftTapSound();
        }

        foreach (var optItem in CurrentPracticeOptions)
        {
            bool isThisSelected = string.Equals(optItem.Text.Trim(), option.Trim(), StringComparison.OrdinalIgnoreCase);
            bool isThisCorrect = string.Equals(optItem.Text.Trim(), CurrentPracticeQuestion.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isThisSelected)
            {
                if (isThisCorrect)
                {
                    optItem.State = TestOptionState.SelectedCorrect;
                    optItem.Glyph = "\uE73E";
                }
                else
                {
                    optItem.State = TestOptionState.SelectedIncorrect;
                    optItem.Glyph = "\uE711";
                }
            }
            else if (isThisCorrect)
            {
                optItem.State = TestOptionState.RevealedCorrect;
                optItem.Glyph = "\uE73E";
            }
            else
            {
                optItem.State = TestOptionState.Default;
            }
        }

        _practiceSubmissions.Add(new GrammarQuizSubmissionDto(
            CurrentPracticeQuestion.RuleId,
            CurrentPracticeQuestion.QuestionNumber,
            option
        ));

        UpdatePracticeStatusTip();
    }

    [RelayCommand]
    public void NextPracticeQuestion()
    {
        if (CurrentQuestionIndex + 1 < PracticeQuestions.Count)
        {
            LoadPracticeQuestion(CurrentQuestionIndex + 1);
        }
        else
        {
            FinishPractice();
        }
    }

    private void FinishPractice()
    {
        var result = _grammarService.EvaluateQuiz(PracticeQuestions.ToList(), _practiceSubmissions);
        PracticeResult = result;

        var template = _localizationService.GetString("Grammar_Practice_ResultSummaryFormat");
        ResultSummaryMessage = string.Format(template, result.CorrectCount, result.TotalQuestions, $"{result.ScorePercentage:F0}");

        _soundFeedback?.PlayReviewCompleteSound();

        IsPracticeMode = false;
        IsPracticeResultMode = true;
    }

    private void UpdatePracticeStatusTip()
    {
        PracticeStatusTip = !IsAnswerSubmitted
            ? _localizationService.GetString("Grammar_Practice_SelectOptionTip")
            : _localizationService.GetString("Grammar_Practice_NextQuestionTip");
    }

    private void UpdateNextButtonText()
    {
        bool isLast = CurrentQuestionIndex + 1 >= TotalPracticeQuestions;
        NextButtonText = isLast
            ? _localizationService.GetString("Test_FinishTest")
            : _localizationService.GetString("Test_NextQuestion");
    }

    [RelayCommand]
    public void ExitPractice()
    {
        IsPracticeMode = false;
        IsPracticeResultMode = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Import / Export
    // ─────────────────────────────────────────────────────────────────────────

    public Task<string> ExportToJsonAsync(IEnumerable<Guid>? ruleIds = null, IProgress<ProgressReport>? progress = null)
        => _grammarExportImportService.ExportToJsonAsync(ruleIds, progress: progress);

    public Task<string> ExportToCsvAsync(IEnumerable<Guid>? ruleIds = null, IProgress<ProgressReport>? progress = null)
        => _grammarExportImportService.ExportToCsvAsync(ruleIds, progress: progress);

    public Task<string> ExportToTxtAsync(IEnumerable<Guid>? ruleIds = null, IProgress<ProgressReport>? progress = null)
        => _grammarExportImportService.ExportToTxtAsync(ruleIds, progress: progress);

    public async Task<ImportResult> ImportSmartAsync(
        string content,
        string? fileNameHint = null,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null)
    {
        var result = await _grammarExportImportService.ImportSmartAsync(content, fileNameHint, conflictResolution, progress: progress);
        await RefreshAfterImportAsync();
        return result;
    }

    public Task<ImportResult> ImportDataAsync(
        string content,
        string format,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null)
        => format.ToLowerInvariant() switch
        {
            "json" => _grammarExportImportService.ImportFromJsonAsync(content, conflictResolution, progress: progress),
            "csv"  => _grammarExportImportService.ImportFromCsvAsync(content, conflictResolution, progress: progress),
            "txt"  => _grammarExportImportService.ImportFromTxtAsync(content, conflictResolution, progress: progress),
            _      => Task.FromResult(new ImportResult(0, 0, [$"Unsupported format: {format}"], 0))
        };

    public string GetSampleTemplate(string format)
        => _grammarExportImportService.GetSampleTemplate(format);

    /// <summary>Refreshes the rule list after a successful import.</summary>
    public async Task RefreshAfterImportAsync()
    {
        await LoadTopicsAsync();
        await LoadRulesAsync();
    }

    [RelayCommand]
    public async Task ApplyClosestSuggestionAsync(string? suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion)) return;
        SearchQuery = suggestion;
        ClosestSuggestions = Array.Empty<string>();
        HasClosestSuggestions = false;
        CurrentPage = 1;
        await LoadRulesAsync();
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

    public void UpdateLanguageState()
    {
        if (IsPracticeMode && TotalPracticeQuestions > 0)
        {
            PracticeProgressText = string.Format(_localizationService.GetString("Quiz_ProgressFormat"), CurrentQuestionIndex + 1, TotalPracticeQuestions);
            PracticeStepBadge = $"{_localizationService.GetString("Test_Question").ToUpperInvariant()} {CurrentQuestionIndex + 1} / {TotalPracticeQuestions}";
            UpdatePracticeStatusTip();
            UpdateNextButtonText();
        }
        else if (IsPracticeResultMode && PracticeResult != null)
        {
            var template = _localizationService.GetString("Grammar_Practice_ResultSummaryFormat");
            ResultSummaryMessage = string.Format(template, PracticeResult.CorrectCount, PracticeResult.TotalQuestions, $"{PracticeResult.ScorePercentage:F0}");
        }
    }
}
