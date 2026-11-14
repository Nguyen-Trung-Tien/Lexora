using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.ViewModels;

public partial class TestViewModel
{
    private Task? _pendingAdaptiveSubmitTask;
    private bool _isNextInProgress;

    private void LoadQuestion(int index)
    {
        if (index < 0 || index >= _allQuestions.Count) return;

        CurrentQuestionIndex = index;
        CurrentQuestion = _allQuestions[index];
        IsLastQuestion = index + 1 >= TotalQuestions;
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
    public void SubmitAnswer() => SubmitAnswerInternal(allowEmpty: false);

    public void SubmitAnswerInternal(bool allowEmpty)
    {
        if (IsAnswerSubmitted || CurrentQuestion == null) return;

        _questionTimer.Stop();
        int responseTime = (int)_questionTimer.ElapsedMilliseconds;

        string answer = SelectedOption ?? UserTextInput?.Trim() ?? string.Empty;
        if (!allowEmpty && string.IsNullOrEmpty(answer)) return;

        IsAnswerSubmitted = true;
        IsAnswerCorrect = !string.IsNullOrEmpty(answer) && string.Equals(answer.Trim(), CurrentQuestion.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        ExplanationText = CurrentQuestion.Explanation;

        foreach (var optItem in CurrentOptions)
        {
            bool isThisSelected = !string.IsNullOrEmpty(answer) && string.Equals(optItem.Text.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase);
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

        if (IsAdaptiveMode && _adaptiveQuizService != null)
        {
            int selectedOptIndex = -1;
            for (int i = 0; i < CurrentOptions.Count; i++)
            {
                if (!string.IsNullOrEmpty(answer) && string.Equals(CurrentOptions[i].Text.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    selectedOptIndex = i;
                    break;
                }
            }

            _pendingAdaptiveSubmitTask = SubmitAdaptiveAnswerAsync(selectedOptIndex, responseTime);
        }
    }

    private async Task SubmitAdaptiveAnswerAsync(int selectedOptionIndex, int responseTime)
    {
        if (_adaptiveQuizService == null || CurrentQuestion == null) return;
        try
        {
            var eval = await _adaptiveQuizService.SubmitAnswerAsync(_sessionId, CurrentQuestion.QuestionNumber, selectedOptionIndex, responseTime);
            CalibratedLevel = eval.NewCalibratedLevel;
            CalibratedLevelDisplay = eval.NewCalibratedLevel.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TestViewModel] Adaptive submit error: {ex.Message}");
        }
    }

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
                SubmitAnswerInternal(allowEmpty: true);
            }

            if (CurrentQuestionIndex + 1 < _allQuestions.Count)
            {
                LoadQuestion(CurrentQuestionIndex + 1);
            }
            else if (IsAdaptiveMode && _adaptiveQuizService != null)
            {
                if (_pendingAdaptiveSubmitTask != null)
                {
                    await _pendingAdaptiveSubmitTask;
                    _pendingAdaptiveSubmitTask = null;
                }

                var nextQ = await _adaptiveQuizService.GetNextQuestionAsync(_sessionId);
                if (nextQ != null)
                {
                    LoadAdaptiveQuestion(nextQ);
                }
                else
                {
                    await FinishAdaptiveTestAsync();
                }
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

    public async Task StartAdaptiveTestAsync()
    {
        if (_adaptiveQuizService == null) return;
        IsLoading = true;
        StatusSeverity = InfoBarSeverity.Informational;
        StatusMessage = _localizationService.GetString("Test_Status_Preparing");

        try
        {
            var config = new AdaptiveQuizConfigDto(
                QuestionCount: QuestionCount > 0 ? QuestionCount : 10,
                ExamTarget: SelectedExamType ?? ExamType.General,
                InitialLevel: SelectedLevel ?? CefrLevel.B1,
                FocusWeakWords: WeakWordsOnly
            );

            var firstQuestion = await _adaptiveQuizService.StartQuizAsync(config);
            if (firstQuestion == null)
            {
                StatusSeverity = InfoBarSeverity.Warning;
                StatusMessage = _localizationService.GetString("Test_Status_NoQuestions");
                return;
            }

            IsAdaptiveMode = true;
            AdaptiveSummary = null;
            IsSetupMode = false;
            IsResultMode = false;
            IsTestingMode = true;

            _sessionId = firstQuestion.SessionId;
            TotalQuestions = config.QuestionCount;
            _userAnswers.Clear();
            _totalTestTimer.Restart();

            LoadAdaptiveQuestion(firstQuestion);
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

    private void LoadAdaptiveQuestion(AdaptiveQuestionDto adaptiveQ)
    {
        CurrentQuestionIndex = Math.Max(0, adaptiveQ.QuestionIndex - 1);
        var dummyWord = new WordDto(
            Id: adaptiveQ.WordId,
            Term: adaptiveQ.PromptTerm,
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: adaptiveQ.AudioPath,
            Level: adaptiveQ.DifficultyLevel,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: null,
            Meanings: [],
            Topics: [],
            ExamTags: [],
            LearningStatus: null,
            NextReviewAt: null,
            Confidence: 0.0
        );

        var testQ = new TestQuestionDto(
            WordId: adaptiveQ.WordId,
            QuestionNumber: adaptiveQ.QuestionIndex,
            QuestionType: adaptiveQ.QuestionType,
            Prompt: adaptiveQ.QuestionText,
            SubPrompt: adaptiveQ.ExampleSentence,
            AudioPath: adaptiveQ.AudioPath,
            CorrectAnswer: adaptiveQ.Options[adaptiveQ.CorrectOptionIndex],
            Options: adaptiveQ.Options.ToList(),
            Explanation: adaptiveQ.Explanation,
            TargetWord: dummyWord
        );

        CurrentQuestion = testQ;
        CalibratedLevel = adaptiveQ.DifficultyLevel;
        CalibratedLevelDisplay = adaptiveQ.DifficultyLevel.ToString();
        IsLastQuestion = adaptiveQ.QuestionIndex >= TotalQuestions;

        ProgressText = string.Format(_localizationService.GetString("Quiz_ProgressFormat"), adaptiveQ.QuestionIndex, TotalQuestions);
        ProgressPercent = (double)adaptiveQ.QuestionIndex / TotalQuestions * 100.0;

        CurrentOptions.Clear();
        foreach (var opt in adaptiveQ.Options)
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
        HasAudio = !string.IsNullOrEmpty(testQ.AudioPath);
        _questionTimer.Restart();
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
}
