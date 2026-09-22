using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class GrammarViewModel : ObservableObject
{
    private readonly IGrammarService _grammarService;
    private readonly IGrammarExportImportService _grammarExportImportService;
    private readonly Services.ILocalizationService _localizationService;
    private readonly Services.ISoundFeedbackService? _soundFeedback;
    private readonly List<GrammarQuizSubmissionDto> _practiceSubmissions = new();

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial GrammarRuleDto? SelectedRule { get; set; }

    [ObservableProperty]
    public partial CefrLevel? SelectedLevel { get; set; }

    [ObservableProperty]
    public partial ExamType? SelectedExamTarget { get; set; }

    [ObservableProperty]
    public partial string? SelectedTopic { get; set; }

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

    public GrammarViewModel(
        IGrammarService grammarService,
        IGrammarExportImportService grammarExportImportService,
        Services.ILocalizationService localizationService,
        Services.ISoundFeedbackService? soundFeedback = null)
    {
        _grammarService = grammarService;
        _grammarExportImportService = grammarExportImportService;
        _localizationService = localizationService;
        _soundFeedback = soundFeedback;
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

        IsLoading = true;
        try
        {
            var results = await _grammarService.GetFilteredRulesAsync(
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                examType: SelectedExamTarget == ExamType.General ? null : SelectedExamTarget,
                topic: SelectedTopic,
                search: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                cancellationToken: ct
            );

            if (ct.IsCancellationRequested) return;

            Rules.Clear();
            foreach (var r in results)
            {
                Rules.Add(r);
            }

            if (Rules.Count > 0)
            {
                if (SelectedRule == null || !Rules.Any(r => r.Id == SelectedRule.Id))
                {
                    SelectedRule = Rules[0];
                }
            }
            else
            {
                SelectedRule = null;
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
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    public void ResetFilters()
    {
        SearchQuery = string.Empty;
        SelectedLevel = null;
        SelectedExamTarget = null;
        SelectedTopic = null;
    }

    [RelayCommand]
    public async Task AddRuleAsync(GrammarRuleCreateOrUpdateDto dto)
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

    [RelayCommand]
    public async Task UpdateRuleAsync(GrammarRuleCreateOrUpdateDto dto)
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

    [RelayCommand]
    public async Task DeleteRuleAsync(Guid id)
    {
        await _grammarService.DeleteRuleAsync(id);
        await LoadTopicsAsync();
        await LoadRulesAsync();
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

    public async Task<int> DeleteSelectedRulesAsync()
    {
        if (SelectedRuleIds.Count == 0 || _isDeletingRules) return 0;
        _isDeletingRules = true;
        IsLoading = true;

        try
        {
            var idsToDelete = SelectedRuleIds.ToList();
            int count = await _grammarService.DeleteRulesAsync(idsToDelete);
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
            IsLoading = false;
        }
    }

    // ==========================================
    // Grammar Practice / Quiz methods
    // ==========================================
    [RelayCommand]
    public async Task StartPracticeAsync()
    {
        IsLoading = true;
        try
        {
            var questions = await _grammarService.GenerateGrammarQuizAsync(
                count: 5,
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                examType: SelectedExamTarget == ExamType.General ? null : SelectedExamTarget
            );

            if (questions.Count == 0) return;

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

        if (_localizationService.CurrentLanguage == "vi-VN")
        {
            ResultSummaryMessage = $"Bạn đã hoàn thành bài luyện tập với {result.CorrectCount}/{result.TotalQuestions} câu trả lời chính xác (Đạt {result.ScorePercentage:F0}%).";
        }
        else
        {
            ResultSummaryMessage = $"You completed the practice with {result.CorrectCount}/{result.TotalQuestions} correct answers ({result.ScorePercentage:F0}% score).";
        }

        _soundFeedback?.PlayReviewCompleteSound();

        IsPracticeMode = false;
        IsPracticeResultMode = true;
    }

    private void UpdatePracticeStatusTip()
    {
        if (!IsAnswerSubmitted)
        {
            PracticeStatusTip = _localizationService.CurrentLanguage == "vi-VN"
                ? "Chọn một phương án để kiểm tra cấu trúc"
                : "Select an option to check structure formula";
        }
        else
        {
            PracticeStatusTip = _localizationService.CurrentLanguage == "vi-VN"
                ? "Nhấn 'Câu tiếp theo' để tiếp tục"
                : "Click 'Next Question' to continue";
        }
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
}
