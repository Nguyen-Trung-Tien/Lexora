using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class TestViewModel : ObservableObject
{
    private readonly ITestService _testService;
    private readonly IVocabularyService _vocabService;
    private readonly IReviewService _reviewService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly INavigationService _navService;

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

    // Testing Active State
    [ObservableProperty]
    public partial int CurrentQuestionIndex { get; set; } = 0;

    [ObservableProperty]
    public partial int TotalQuestions { get; set; } = 0;

    [ObservableProperty]
    public partial string ProgressText { get; set; } = "Question 1 of 10";

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

    public ObservableCollection<string> CurrentOptions { get; } = new();

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
    public partial string ResultBadgeColor { get; set; } = "#107C41";

    public ObservableCollection<WordDto> WeakWordsList { get; } = new();
    public ObservableCollection<TestQuestionDto> QuestionResultsList { get; } = new();

    public TestViewModel(
        ITestService testService,
        IVocabularyService vocabService,
        IReviewService reviewService,
        IAudioPlayerService audioPlayer,
        INavigationService navService)
    {
        _testService = testService;
        _vocabService = vocabService;
        _reviewService = reviewService;
        _audioPlayer = audioPlayer;
        _navService = navService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
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
    }

    [RelayCommand]
    public async Task StartTestAsync()
    {
        IsLoading = true;
        StatusMessage = "Preparing test questions...";

        try
        {
            var config = new TestConfigurationDto(
                Type: SelectedTestType,
                QuestionCount: QuestionCount,
                Level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                TopicId: SelectedTopic?.Id,
                ExamType: SelectedExamType == ExamType.General ? null : SelectedExamType,
                WeakWordsOnly: WeakWordsOnly
            );

            var session = await _testService.GenerateTestAsync(config);
            _sessionId = session.SessionId;
            _allQuestions = session.Questions;
            _userAnswers.Clear();

            if (_allQuestions.Count == 0)
            {
                StatusMessage = "No questions could be generated. Please add more vocabulary first.";
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
            StatusMessage = $"Error: {ex.Message}";
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
        ProgressText = $"Question {index + 1} of {TotalQuestions}";
        ProgressPercent = (double)(index + 1) / TotalQuestions * 100.0;

        CurrentOptions.Clear();
        foreach (var opt in CurrentQuestion.Options)
        {
            CurrentOptions.Add(opt);
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

    private async Task FinishTestAsync()
    {
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

            if (ScorePercentage >= 85.0)
            {
                ResultMessage = "Excellent! Outstanding mastery!";
                ResultBadgeColor = "#107C41"; // Green
            }
            else if (ScorePercentage >= 60.0)
            {
                ResultMessage = "Good job! Keep reviewing to improve!";
                ResultBadgeColor = "#0078D4"; // Blue
            }
            else
            {
                ResultMessage = "Needs practice. Let's review the weak words!";
                ResultBadgeColor = "#D83B01"; // Orange/Red
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
            StatusMessage = $"Failed to finalize test: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task PlayAudioAsync()
    {
        if (CurrentQuestion == null) return;
        await _audioPlayer.PlayPronunciationAsync(CurrentQuestion.TargetWord.Term, "us");
    }

    [RelayCommand]
    public async Task PlayWordAudioAsync(WordDto? word)
    {
        if (word == null) return;
        await _audioPlayer.PlayPronunciationAsync(word.Term, "us");
    }

    [RelayCommand]
    public async Task RetestWeakWordsAsync()
    {
        if (WeakWordsList.Count == 0) return;

        WeakWordsOnly = true;
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
        IsResultMode = false;
        IsTestingMode = false;
        IsSetupMode = true;
        WeakWordsOnly = false;
    }
}
