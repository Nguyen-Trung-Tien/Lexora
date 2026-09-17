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
    private readonly Services.ILocalizationService _localizationService;
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
    public partial GrammarPracticeResultDto? PracticeResult { get; set; }

    public ObservableCollection<GrammarRuleDto> Rules { get; } = new();
    public ObservableCollection<CefrLevel> CefrLevels { get; } = new();
    public ObservableCollection<ExamType> ExamTypes { get; } = new();
    public ObservableCollection<string> Topics { get; } = new();
    public ObservableCollection<string> CurrentPracticeOptions { get; } = new();
    public ObservableCollection<GrammarQuizQuestionDto> PracticeQuestions { get; } = new();

    public GrammarViewModel(
        IGrammarService grammarService,
        Services.ILocalizationService localizationService)
    {
        _grammarService = grammarService;
        _localizationService = localizationService;
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
        var topics = await _grammarService.GetAllTopicsAsync();
        Topics.Clear();
        foreach (var t in topics) Topics.Add(t);
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
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    public async Task AddRuleAsync(GrammarRuleCreateOrUpdateDto dto)
    {
        var created = await _grammarService.CreateRuleAsync(dto);
        await LoadTopicsAsync();
        await LoadRulesAsync();
        SelectedRule = Rules.FirstOrDefault(r => r.Id == created.Id);
    }

    [RelayCommand]
    public async Task UpdateRuleAsync(GrammarRuleCreateOrUpdateDto dto)
    {
        var updated = await _grammarService.UpdateRuleAsync(dto);
        await LoadTopicsAsync();
        await LoadRulesAsync();
        SelectedRule = Rules.FirstOrDefault(r => r.Id == updated.Id);
    }

    [RelayCommand]
    public async Task DeleteRuleAsync(Guid id)
    {
        await _grammarService.DeleteRuleAsync(id);
        await LoadTopicsAsync();
        await LoadRulesAsync();
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

        CurrentPracticeOptions.Clear();
        foreach (var opt in CurrentPracticeQuestion.Options)
        {
            CurrentPracticeOptions.Add(opt);
        }

        SelectedOption = null;
        IsAnswerSubmitted = false;
        IsAnswerCorrect = false;
        ExplanationText = string.Empty;
    }

    [RelayCommand]
    public void SelectPracticeOption(string option)
    {
        if (IsAnswerSubmitted || CurrentPracticeQuestion == null) return;

        SelectedOption = option;
        IsAnswerSubmitted = true;
        IsAnswerCorrect = string.Equals(option.Trim(), CurrentPracticeQuestion.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        ExplanationText = CurrentPracticeQuestion.Explanation;

        _practiceSubmissions.Add(new GrammarQuizSubmissionDto(
            CurrentPracticeQuestion.RuleId,
            CurrentPracticeQuestion.QuestionNumber,
            option
        ));
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

        IsPracticeMode = false;
        IsPracticeResultMode = true;
    }

    [RelayCommand]
    public void ExitPractice()
    {
        IsPracticeMode = false;
        IsPracticeResultMode = false;
    }
}
