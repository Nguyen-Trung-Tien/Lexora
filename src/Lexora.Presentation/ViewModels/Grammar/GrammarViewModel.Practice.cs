using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class GrammarViewModel
{
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

        _ = RecordGrammarProgressAsync();
    }

    private async Task RecordGrammarProgressAsync()
    {
        try
        {
            if (_studyPlannerService != null)
            {
                await _studyPlannerService.RecordTaskCompletionAsync(CurriculumTaskType.StudyGrammar, 1);
            }
            if (_roadmapService != null)
            {
                await _roadmapService.SyncRoadmapProgressAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarViewModel] RecordGrammarProgressAsync error: {ex.Message}");
        }
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
