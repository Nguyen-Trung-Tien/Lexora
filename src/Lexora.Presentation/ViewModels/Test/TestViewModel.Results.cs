using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.ViewModels;

public partial class TestViewModel
{
    private bool _isFinishingTest = false;

    private async Task FinishAdaptiveTestAsync()
    {
        if (_adaptiveQuizService == null) return;
        IsLoading = true;
        _totalTestTimer.Stop();

        try
        {
            var summary = await _adaptiveQuizService.FinishQuizAsync(_sessionId);
            AdaptiveSummary = summary;

            ScorePercentage = summary.AccuracyPercentage;
            CorrectCount = summary.CorrectCount;
            IncorrectCount = summary.IncorrectCount;
            TimeTakenText = _totalTestTimer.Elapsed.ToString(@"mm\:ss");

            ResultMessage = string.Format(_localizationService.GetString("AdaptiveQuiz_FinalLevelFormat"), summary.FinalEstimatedLevel);
            EstimatedToeicDisplay = summary.EstimatedToeicBand;
            EstimatedIeltsDisplay = summary.EstimatedIeltsBand;
            EstimatedVstepDisplay = summary.EstimatedVstepLevel;

            WeakWordsList.Clear();
            foreach (var w in summary.DifficultWordsToReview)
            {
                WeakWordsList.Add(w);
            }

            IsTestingMode = false;
            IsResultMode = true;

            await SyncRoadmapNodeAfterTestAsync(summary.AccuracyPercentage, summary.CorrectCount);

            if (_studyPlannerService != null)
            {
                try
                {
                    await _studyPlannerService.RecordTaskCompletionAsync(CurriculumTaskType.TakeAdaptiveQuiz, 1);
                }
                catch
                {
                    // Non-critical Study Planner sync
                }
            }
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

            await SyncRoadmapNodeAfterTestAsync(ScorePercentage, CorrectCount);
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

    private async Task SyncRoadmapNodeAfterTestAsync(double scorePercent, int correctCount)
    {
        if (ActiveRoadmapContext == null || _roadmapService == null) return;

        try
        {
            int target = ActiveRoadmapContext.TargetCount > 0 ? ActiveRoadmapContext.TargetCount : 10;
            int progressDelta = scorePercent >= 60.0 ? target : Math.Max(0, correctCount);
            if (progressDelta > 0)
            {
                await _roadmapService.UpdateNodeProgressAsync(ActiveRoadmapContext.NodeId, progressDelta);
            }
            await _roadmapService.SyncRoadmapProgressAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TestViewModel] SyncRoadmapNodeAfterTestAsync error: {ex.Message}");
        }
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
        var weakIds = WeakWordsList.Select(w => w.Id).ToList();
        if (weakIds.Count > 0)
        {
            _navService.Navigate(typeof(ReviewPage), weakIds);
        }
        else
        {
            _navService.Navigate(typeof(ReviewPage));
        }
    }

    [RelayCommand]
    public async Task SaveWeakWordsToStorageAsync()
    {
        if (_storageService == null || WeakWordsList.Count == 0) return;
        try
        {
            var weakIds = WeakWordsList.Select(w => w.Id).ToList();
            var count = await _storageService.MoveWordsToStorageAsync(weakIds, StorageAdditionReason.MultipleErrors, "Failed during test session");
            StatusSeverity = InfoBarSeverity.Success;
            StatusMessage = string.Format(_localizationService.GetString("Test_SavedWeakWordsToStorageSuccess"), count);
        }
        catch (Exception ex)
        {
            StatusSeverity = InfoBarSeverity.Error;
            StatusMessage = ex.Message;
        }
    }

    public void UpdateLanguageState()
    {
        OnPropertyChanged(nameof(NextButtonText));
        if (IsTestingMode && TotalQuestions > 0)
        {
            ProgressText = string.Format(_localizationService.GetString("Quiz_ProgressFormat"), CurrentQuestionIndex + 1, TotalQuestions);
        }
        else if (IsResultMode && TestResult != null)
        {
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
        }
    }
}
