using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class LearningStorageViewModel
{
    private StoragePracticeResultDto? _lastPracticeResult;

    // --- Practice Flow ---
    [RelayCommand]
    public async Task StartPracticeAsync()
    {
        IsPracticeLoading = true;
        IsPracticeActive = true;
        IsPracticeCompleted = false;
        CurrentPracticeIndex = 0;
        _practiceQuestions.Clear();
        _practiceSubmissions.Clear();
        CurrentOptions.Clear();
        PracticeSelectedAnswer = null;
        HasSelectedPracticeAnswer = false;
        IsPracticeAnswerSubmitted = false;

        try
        {
            var itemType = SelectedTab == 0 ? StorageItemType.Vocabulary : StorageItemType.Grammar;
            var questions = await _storageService.GeneratePracticeAsync(itemType, limit: 15, prioritizeWeak: true);

            if (questions.Count == 0)
            {
                IsPracticeActive = false;
                NoPracticeQuestionsAvailable?.Invoke();
                return;
            }

            _practiceQuestions.AddRange(questions);
            DisplayCurrentQuestion();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] StartPractice error: {ex.Message}");
            IsPracticeActive = false;
            PracticeFailed?.Invoke(ex.Message);
        }
        finally
        {
            IsPracticeLoading = false;
        }
    }

    private void DisplayCurrentQuestion()
    {
        if (CurrentPracticeIndex >= 0 && CurrentPracticeIndex < _practiceQuestions.Count)
        {
            CurrentQuestion = _practiceQuestions[CurrentPracticeIndex];
            PracticeSelectedAnswer = null;
            HasSelectedPracticeAnswer = false;
            IsPracticeAnswerSubmitted = false;
            IsCurrentAnswerCorrect = false;

            CurrentOptions.Clear();
            var letters = new[] { "A", "B", "C", "D", "E", "F" };
            for (int i = 0; i < CurrentQuestion.Options.Count; i++)
            {
                var label = i < letters.Length ? letters[i] : (i + 1).ToString();
                CurrentOptions.Add(new TestOptionItem
                {
                    Text = CurrentQuestion.Options[i],
                    Letter = label,
                    Glyph = label,
                    State = TestOptionState.Default
                });
            }

            PracticeProgressText = string.Format(
                _localizationService.GetString("Storage_Practice_QuestionFormat"),
                CurrentPracticeIndex + 1, _practiceQuestions.Count);
        }
    }

    [RelayCommand]
    public void SelectPracticeAnswer(string answer)
    {
        if (IsPracticeAnswerSubmitted || string.IsNullOrWhiteSpace(answer)) return;

        PracticeSelectedAnswer = answer;
        HasSelectedPracticeAnswer = true;

        foreach (var opt in CurrentOptions)
        {
            if (string.Equals(opt.Text, answer, StringComparison.OrdinalIgnoreCase))
            {
                opt.State = TestOptionState.Selected;
            }
            else
            {
                opt.State = TestOptionState.Default;
            }
        }
    }

    [RelayCommand]
    public void SubmitPracticeAnswer()
    {
        if (CurrentQuestion == null || string.IsNullOrWhiteSpace(PracticeSelectedAnswer) || IsPracticeAnswerSubmitted) return;

        IsPracticeAnswerSubmitted = true;
        var trimmedUser = PracticeSelectedAnswer.Trim();
        var trimmedCorrect = CurrentQuestion.CorrectAnswer.Trim();
        IsCurrentAnswerCorrect = string.Equals(trimmedUser, trimmedCorrect, StringComparison.OrdinalIgnoreCase);

        foreach (var opt in CurrentOptions)
        {
            bool isThisCorrect = string.Equals(opt.Text.Trim(), trimmedCorrect, StringComparison.OrdinalIgnoreCase);
            bool isThisUser = string.Equals(opt.Text.Trim(), trimmedUser, StringComparison.OrdinalIgnoreCase);

            if (isThisCorrect)
            {
                opt.State = isThisUser ? TestOptionState.SelectedCorrect : TestOptionState.RevealedCorrect;
                opt.Glyph = "\uE73E"; // Checkmark
            }
            else if (isThisUser)
            {
                opt.State = TestOptionState.SelectedIncorrect;
                opt.Glyph = "\uE711"; // Cross
            }
            else
            {
                opt.State = TestOptionState.Default;
            }
        }

        if (IsCurrentAnswerCorrect)
        {
            _soundFeedback?.PlaySuccessSound();
        }
        else
        {
            _soundFeedback?.PlayErrorSound();
        }

        _practiceSubmissions.Add(new StoragePracticeSubmissionDto(
            CurrentQuestion.StorageItemId,
            PracticeSelectedAnswer
        ));
    }

    [RelayCommand]
    public async Task NextPracticeQuestionAsync()
    {
        if (!IsPracticeAnswerSubmitted) return;

        if (CurrentPracticeIndex + 1 < _practiceQuestions.Count)
        {
            CurrentPracticeIndex++;
            DisplayCurrentQuestion();
        }
        else
        {
            // Evaluate whole practice session
            await FinishPracticeAsync();
        }
    }

    [RelayCommand]
    public async Task FinishPracticeAsync()
    {
        try
        {
            var result = await _storageService.EvaluatePracticeAsync(_practiceQuestions, _practiceSubmissions);
            _lastPracticeResult = result;
            IsPracticeCompleted = true;

            PracticeResultSummaryText = string.Format(
                _localizationService.GetString("Storage_Practice_ResultSummary"),
                result.Correct, result.Total, result.ScorePercent);

            await LoadStatisticsAsync();
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] FinishPractice error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void ClosePractice()
    {
        IsPracticeActive = false;
        IsPracticeCompleted = false;
        _lastPracticeResult = null;
        CurrentQuestion = null;
        CurrentOptions.Clear();
        PracticeSelectedAnswer = null;
        HasSelectedPracticeAnswer = false;
        IsPracticeAnswerSubmitted = false;
    }
}
