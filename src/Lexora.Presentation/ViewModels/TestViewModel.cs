using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.ViewModels;

public partial class TestViewModel : ObservableObject
{
    private readonly ITestService _testService;
    private readonly IVocabularyService _vocabService;
    private readonly IReviewService _reviewService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly INavigationService _navService;
    private readonly ILocalizationService _localizationService;
    private readonly IUserPreferencesService _preferencesService;

    private Guid _sessionId;
    private List<TestQuestionDto> _allQuestions = new();
    private readonly List<TestAnswerSubmissionDto> _userAnswers = new();
    private readonly Stopwatch _questionTimer = new();
    private readonly Stopwatch _totalTestTimer = new();

    [ObservableProperty]
    public partial bool IsSetupMode { get; set; } = true;

    [ObservableProperty]
    public partial bool IsTestingMode { get; set; } = false;

    [ObservableProperty]
    public partial bool IsResultMode { get; set; } = false;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    // Setup configuration
    [ObservableProperty]
    public partial TestType SelectedTestType { get; set; } = TestType.Mixed;

    [ObservableProperty]
    public partial int QuestionCount { get; set; } = 10;

    [ObservableProperty]
    public partial TopicDto? SelectedTopic { get; set; }

    [ObservableProperty]
    public partial ExamType? SelectedExamType { get; set; }

    [ObservableProperty]
    public partial CefrLevel? SelectedLevel { get; set; }

    [ObservableProperty]
    public partial bool WeakWordsOnly { get; set; } = false;

    public ObservableCollection<TopicDto> Topics { get; } = new();
    public ObservableCollection<ExamType> ExamTypes { get; } = new();
    public ObservableCollection<CefrLevel> CefrLevels { get; } = new();

    // Custom Word List Mode properties
    [ObservableProperty]
    public partial bool IsCustomListMode { get; set; } = false;

    [ObservableProperty]
    public partial string CustomListSearchTerm { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CustomListPastedTerms { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedCustomWordsCount { get; set; } = 0;

    public ObservableCollection<SelectableWordItem> CustomWordCandidates { get; } = new();
    public ObservableCollection<SelectableWordItem> FilteredCustomWordCandidates { get; } = new();

    // Testing Active State
    [ObservableProperty]
    public partial int CurrentQuestionIndex { get; set; } = 0;

    [ObservableProperty]
    public partial int TotalQuestions { get; set; } = 0;

    [ObservableProperty]
    public partial string ProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double ProgressPercent { get; set; } = 0.0;

    [ObservableProperty]
    public partial TestQuestionDto? CurrentQuestion { get; set; }

    [ObservableProperty]
    public partial string? SelectedOption { get; set; }

    [ObservableProperty]
    public partial string UserTextInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAnswerSubmitted { get; set; } = false;

    [ObservableProperty]
    public partial bool IsAnswerCorrect { get; set; } = false;

    [ObservableProperty]
    public partial string ExplanationText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasAudio { get; set; } = false;

    public ObservableCollection<TestOptionItem> CurrentOptions { get; } = new();

    // Result State
    [ObservableProperty]
    public partial TestResultDto? TestResult { get; set; }

    [ObservableProperty]
    public partial double ScorePercentage { get; set; } = 0.0;

    [ObservableProperty]
    public partial int CorrectCount { get; set; } = 0;

    [ObservableProperty]
    public partial int IncorrectCount { get; set; } = 0;

    [ObservableProperty]
    public partial string TimeTakenText { get; set; } = "00:00";

    [ObservableProperty]
    public partial string ResultMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TestScoreTier ResultScoreTier { get; set; } = TestScoreTier.NeedsPractice;

    public ObservableCollection<WordDto> WeakWordsList { get; } = new();
    public ObservableCollection<TestQuestionDto> QuestionResultsList { get; } = new();

    public TestViewModel(
        ITestService testService,
        IVocabularyService vocabService,
        IReviewService reviewService,
        IAudioPlayerService audioPlayer,
        INavigationService navService,
        ILocalizationService localizationService,
        IUserPreferencesService preferencesService)
    {
        _testService = testService;
        _vocabService = vocabService;
        _reviewService = reviewService;
        _audioPlayer = audioPlayer;
        _navService = navService;
        _localizationService = localizationService;
        _preferencesService = preferencesService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            if (Topics.Count == 0)
            {
                var loadedTopics = await _vocabService.GetAllTopicsAsync();
                Topics.Clear();
                foreach (var t in loadedTopics) Topics.Add(t);
            }

            if (ExamTypes.Count == 0)
            {
                ExamTypes.Clear();
                foreach (var et in Enum.GetValues<ExamType>()) ExamTypes.Add(et);
            }

            if (CefrLevels.Count == 0)
            {
                CefrLevels.Clear();
                foreach (var cl in Enum.GetValues<CefrLevel>()) CefrLevels.Add(cl);
            }

            await LoadCustomWordCandidatesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TestViewModel] Initialize error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadCustomWordCandidatesAsync()
    {
        try
        {
            if (CustomWordCandidates.Count == 0)
            {
                var words = await _vocabService.SearchWordsAsync(pageSize: 300);
                CustomWordCandidates.Clear();
                foreach (var w in words)
                {
                    var item = new SelectableWordItem(w);
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectableWordItem.IsSelected))
                        {
                            UpdateSelectedCustomWordsCount();
                        }
                    };
                    CustomWordCandidates.Add(item);
                }
            }
            ApplyCustomWordFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TestViewModel] LoadCustomWordCandidates error: {ex.Message}");
        }
    }

    public void UpdateSelectedCustomWordsCount()
    {
        SelectedCustomWordsCount = CustomWordCandidates.Count(w => w.IsSelected == true);
    }

    [RelayCommand]
    public void ApplyCustomWordFilter()
    {
        FilteredCustomWordCandidates.Clear();
        var term = CustomListSearchTerm?.Trim();
        var list = string.IsNullOrEmpty(term)
            ? CustomWordCandidates
            : CustomWordCandidates.Where(w =>
                w.Word.Term.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                w.Word.PrimaryDefinition.Contains(term, StringComparison.OrdinalIgnoreCase));

        foreach (var item in list)
        {
            FilteredCustomWordCandidates.Add(item);
        }
    }

    [RelayCommand]
    public void SelectAllCustomWords()
    {
        foreach (var item in FilteredCustomWordCandidates)
        {
            item.IsSelected = true;
        }
        UpdateSelectedCustomWordsCount();
    }

    [RelayCommand]
    public void DeselectAllCustomWords()
    {
        foreach (var item in CustomWordCandidates)
        {
            item.IsSelected = false;
        }
        UpdateSelectedCustomWordsCount();
    }

    [RelayCommand]
    public void SelectWeakCustomWords()
    {
        foreach (var item in CustomWordCandidates)
        {
            item.IsSelected = item.Word.LearningStatus == LearningStatus.NotRemembered || item.Word.LearningStatus == null;
        }
        UpdateSelectedCustomWordsCount();
    }

    [RelayCommand]
    public void ApplyPastedTerms()
    {
        if (string.IsNullOrWhiteSpace(CustomListPastedTerms)) return;

        var separators = new[] { ',', '\n', '\r', ';', '|' };
        var terms = CustomListPastedTerms
            .Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int matchedCount = 0;
        foreach (var item in CustomWordCandidates)
        {
            if (terms.Contains(item.Word.Term))
            {
                item.IsSelected = true;
                matchedCount++;
            }
        }

        UpdateSelectedCustomWordsCount();
        StatusSeverity = matchedCount > 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        StatusMessage = matchedCount > 0
            ? string.Format(_localizationService.GetString("Test_Status_PastedMatched"), matchedCount)
            : _localizationService.GetString("Test_Status_PastedNoMatch");
    }

    [RelayCommand]
    public void ResetFilters()
    {
        SelectedTopic = null;
        SelectedExamType = null;
        SelectedLevel = null;
        WeakWordsOnly = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    public void SwitchToFilterMode()
    {
        IsCustomListMode = false;
    }

    [RelayCommand]
    public async Task SwitchToCustomListModeAsync()
    {
        IsCustomListMode = true;
        await LoadCustomWordCandidatesAsync();
    }

    public async Task SetCustomWordSelectionAsync(IEnumerable<Guid> wordIds)
    {
        await LoadCustomWordCandidatesAsync();
        var set = wordIds.ToHashSet();

        var existingIds = CustomWordCandidates.Select(c => c.Word.Id).ToHashSet();
        var missingIds = set.Where(id => !existingIds.Contains(id)).ToList();

        if (missingIds.Count > 0)
        {
            var loadedWords = await _vocabService.GetWordsByIdsAsync(missingIds);
            foreach (var word in loadedWords)
            {
                var newItem = new SelectableWordItem(word);
                newItem.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SelectableWordItem.IsSelected))
                    {
                        UpdateSelectedCustomWordsCount();
                    }
                };
                CustomWordCandidates.Insert(0, newItem);
            }
        }

        foreach (var item in CustomWordCandidates)
        {
            item.IsSelected = set.Contains(item.Word.Id);
        }

        UpdateSelectedCustomWordsCount();
        ApplyCustomWordFilter();
        IsCustomListMode = true;
    }

    [RelayCommand]
    public async Task StartTestAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        StatusSeverity = InfoBarSeverity.Informational;
        StatusMessage = _localizationService.GetString("Test_Status_Preparing");

        try
        {
            List<Guid>? customWordIds = null;
            int qCount = QuestionCount;

            if (IsCustomListMode)
            {
                var selected = CustomWordCandidates.Where(c => c.IsSelected == true).Select(c => c.Word.Id).ToList();
                if (selected.Count == 0)
                {
                    StatusSeverity = InfoBarSeverity.Warning;
                    StatusMessage = _localizationService.GetString("Test_Status_SelectAtLeastOne");
                    IsLoading = false;
                    return;
                }
                customWordIds = selected;
                qCount = selected.Count;
            }

            var config = new TestConfigurationDto(
                Type: SelectedTestType,
                QuestionCount: qCount,
                Level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                TopicId: SelectedTopic?.Id,
                ExamType: SelectedExamType == ExamType.General ? null : SelectedExamType,
                WeakWordsOnly: WeakWordsOnly,
                CustomWordIds: customWordIds
            );

            var session = await _testService.GenerateTestAsync(config);
            _sessionId = session.SessionId;
            _allQuestions = session.Questions;
            _userAnswers.Clear();

            if (_allQuestions.Count == 0)
            {
                StatusSeverity = InfoBarSeverity.Warning;
                StatusMessage = _localizationService.GetString("Test_Status_NoQuestions");
                IsLoading = false;
                return;
            }

            CurrentQuestionIndex = 0;
            TotalQuestions = _allQuestions.Count;
            _totalTestTimer.Restart();

            LoadQuestion(0);

            IsSetupMode = false;
            IsResultMode = false;
            IsTestingMode = true;
        }
        catch (Exception ex)
        {
            StatusSeverity = InfoBarSeverity.Error;
            StatusMessage = string.Format(_localizationService.GetString("Test_ErrorFormat"), ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadQuestion(int index)
    {
        if (index < 0 || index >= _allQuestions.Count) return;

        CurrentQuestionIndex = index;
        CurrentQuestion = _allQuestions[index];
        ProgressText = string.Format(_localizationService.GetString("Quiz_ProgressFormat"), index + 1, TotalQuestions);
        ProgressPercent = (double)(index + 1) / TotalQuestions * 100.0;

        CurrentOptions.Clear();
        foreach (var opt in CurrentQuestion.Options)
        {
            CurrentOptions.Add(new TestOptionItem
            {
                Text = opt,
                State = TestOptionState.Default,
                Glyph = "\uE739"
            });
        }

        SelectedOption = null;
        UserTextInput = string.Empty;
        IsAnswerSubmitted = false;
        IsAnswerCorrect = false;
        ExplanationText = string.Empty;

        HasAudio = !string.IsNullOrEmpty(CurrentQuestion.AudioPath);
        _questionTimer.Restart();

        // Auto play audio for listening quiz
        if (CurrentQuestion.QuestionType == TestType.ListeningQuiz && HasAudio)
        {
            _ = PlayAudioAsync();
        }
    }

    [RelayCommand]
    public void SelectOption(string option)
    {
        if (IsAnswerSubmitted) return;
        SelectedOption = option;
        SubmitAnswer();
    }

    [RelayCommand]
    public void SubmitAnswer()
    {
        if (IsAnswerSubmitted || CurrentQuestion == null) return;

        _questionTimer.Stop();
        int responseTime = (int)_questionTimer.ElapsedMilliseconds;

        string answer = SelectedOption ?? UserTextInput.Trim();
        if (string.IsNullOrEmpty(answer)) return;

        IsAnswerSubmitted = true;
        IsAnswerCorrect = string.Equals(answer.Trim(), CurrentQuestion.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        ExplanationText = CurrentQuestion.Explanation;

        foreach (var optItem in CurrentOptions)
        {
            bool isThisSelected = string.Equals(optItem.Text.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase);
            bool isThisCorrect = string.Equals(optItem.Text.Trim(), CurrentQuestion.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

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
                optItem.Glyph = "\uE739";
            }
        }

        _userAnswers.Add(new TestAnswerSubmissionDto(
            CurrentQuestion.WordId,
            CurrentQuestion.QuestionNumber,
            answer,
            responseTime
        ));
    }

    private bool _isNextInProgress;

    [RelayCommand]
    public async Task NextQuestionAsync()
    {
        if (_isNextInProgress) return;
        _isNextInProgress = true;

        try
        {
            if (!IsAnswerSubmitted)
            {
                // Auto submit empty/current answer if user clicks next directly
                SubmitAnswer();
            }

            if (CurrentQuestionIndex + 1 < _allQuestions.Count)
            {
                LoadQuestion(CurrentQuestionIndex + 1);
            }
            else
            {
                await FinishTestAsync();
            }
        }
        finally
        {
            _isNextInProgress = false;
        }
    }

    private bool _isFinishingTest = false;

    private async Task FinishTestAsync()
    {
        if (_isFinishingTest) return;
        _isFinishingTest = true;
        IsLoading = true;
        _totalTestTimer.Stop();

        try
        {
            var result = await _testService.SubmitTestAsync(_sessionId, _userAnswers);
            TestResult = result;

            ScorePercentage = result.ScorePercentage;
            CorrectCount = result.CorrectCount;
            IncorrectCount = result.IncorrectCount;
            TimeTakenText = $"{result.TimeTaken.Minutes:D2}:{result.TimeTaken.Seconds:D2}";
            ResultScoreTier = result.ScoreTier;

            if (ScorePercentage >= 85.0)
            {
                ResultMessage = _localizationService.GetString("Test_Result_Excellent");
            }
            else if (ScorePercentage >= 60.0)
            {
                ResultMessage = _localizationService.GetString("Test_Result_Good");
            }
            else
            {
                ResultMessage = _localizationService.GetString("Test_Result_NeedsPractice");
            }

            WeakWordsList.Clear();
            foreach (var w in result.WeakWords)
            {
                WeakWordsList.Add(w);
            }

            QuestionResultsList.Clear();
            foreach (var q in result.Questions)
            {
                QuestionResultsList.Add(q);
            }

            IsTestingMode = false;
            IsSetupMode = false;
            IsResultMode = true;
        }
        catch (Exception ex)
        {
            StatusSeverity = InfoBarSeverity.Error;
            StatusMessage = string.Format(_localizationService.GetString("Test_FinalizeErrorFormat"), ex.Message);
        }
        finally
        {
            IsLoading = false;
            _isFinishingTest = false;
        }
    }

    [RelayCommand]
    public async Task PlayAudioAsync()
    {
        if (CurrentQuestion == null) return;
        var accent = _preferencesService.GetPreferences().PreferredAccent ?? "uk";
        await _audioPlayer.PlayPronunciationAsync(CurrentQuestion.TargetWord.Term, accent);
    }

    [RelayCommand]
    public async Task PlayWordAudioAsync(WordDto? word)
    {
        if (word == null) return;
        var accent = _preferencesService.GetPreferences().PreferredAccent ?? "uk";
        await _audioPlayer.PlayPronunciationAsync(word.Term, accent);
    }

    [RelayCommand]
    public async Task RetestWeakWordsAsync()
    {
        if (WeakWordsList.Count == 0) return;

        WeakWordsOnly = true;
        IsCustomListMode = false;
        SelectedTestType = TestType.Mixed;
        QuestionCount = Math.Min(WeakWordsList.Count, 20);
        await StartTestAsync();
    }

    [RelayCommand]
    public void ReviewWeakWordsInSpacedRepetition()
    {
        // Navigate directly to Spaced Review Page
        _navService.Navigate(typeof(Views.ReviewPage));
    }

    [RelayCommand]
    public void RestartSetup()
    {
        CleanupSession();
        IsResultMode = false;
        IsTestingMode = false;
        IsSetupMode = true;
        WeakWordsOnly = false;
    }

    public void CleanupSession()
    {
        _questionTimer.Stop();
        _totalTestTimer.Stop();
        _audioPlayer.Stop();
    }
}
