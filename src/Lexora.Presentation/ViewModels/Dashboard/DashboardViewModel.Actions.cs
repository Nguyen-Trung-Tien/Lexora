using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Views;

namespace Lexora.Presentation.ViewModels;

public partial class DashboardViewModel
{
    [RelayCommand]
    public async Task LoadDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoadingDashboard) return;
        _isLoadingDashboard = true;
        IsLoading = true;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _loadCts.Token;

        try
        {
            var analytics = await _statsService.GetDetailedAnalyticsAsync(token);
            DailyAdvice = await _advisorService.GetDailyAdviceAsync(analytics.Summary, token);
            WorkloadForecast = await _advisorService.Get7DayForecastAsync(token);
            IntelligenceReport = await _statsService.GetLearningIntelligenceReportAsync(token);

            UpdateAdviceDisplay();
            UpdateForecastDisplay();
            UpdateIntelligenceDisplay();

            var summary = analytics.Summary;

            TotalWords = summary.TotalWords;
            DueToday = summary.DueReviewsToday;
            Mastered = summary.TotalMastered;
            LearningCount = summary.TotalLearning + summary.TotalRemembered;
            StreakDays = summary.CurrentStreakDays;
            LongestStreakDays = summary.LongestStreakDays;
            AccuracyRate = summary.AccuracyRate;
            StudiedTodayCount = summary.StudiedTodayCount;

            _rawStatusBreakdown = analytics.StatusBreakdown;
            _rawExamProgress = analytics.ExamProgress;
            _rawCefrDistribution = analytics.CefrDistribution;
            _rawPosDistribution = analytics.PosDistribution;
            _rawHeatmap = analytics.Last30DaysHeatmap;
            _rawRecentSessions = analytics.RecentSessions;

            UpdateLocalizedStatusAndExams();
            UpdateLocalizedDistributions();
            UpdateLocalizedHeatmap();
            UpdateLocalizedRecentSessions();

            TopicProgressList.Clear();
            foreach (var tp in analytics.TopicProgress) TopicProgressList.Add(tp);

            if (_roadmapService != null)
            {
                ActiveRoadmap = await _roadmapService.SyncRoadmapProgressAsync(token)
                    ?? await _roadmapService.GetActiveRoadmapAsync(token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadDashboardAsync error: {ex.Message}");
        }
        finally
        {
            _isLoadingDashboard = false;
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void NavigateToRoadmap()
    {
        _navService.Navigate(typeof(RoadmapPage));
    }

    [RelayCommand]
    public async Task ContinueRoadmapAsync(CancellationToken cancellationToken = default)
    {
        if (IsSubmitting) return;
        if (ActiveRoadmap?.NextActiveNode == null || _roadmapService == null)
        {
            _navService.Navigate(typeof(RoadmapPage));
            return;
        }

        IsSubmitting = true;
        var targetNode = ActiveRoadmap.NextActiveNode;
        try
        {
            var context = await _roadmapService.GetNodeActionContextAsync(targetNode.Id, cancellationToken);

            switch (targetNode.Type)
            {
                case RoadmapNodeType.VocabularySet:
                    if (context?.TopicId != null && !string.IsNullOrWhiteSpace(context.TopicName))
                    {
                        var topicDto = new TopicDto(context.TopicId.Value, context.TopicName, null, null, null);
                        _navService.Navigate(typeof(ReviewPage), topicDto);
                    }
                    else if (context != null && context.LinkedWordIds.Count > 0)
                    {
                        _navService.Navigate(typeof(ReviewPage), context.LinkedWordIds);
                    }
                    else
                    {
                        _navService.Navigate(typeof(ReviewPage));
                    }
                    break;

                case RoadmapNodeType.GrammarPoint:
                    string grammarQuery = context?.GrammarQuery ?? targetNode.ReferenceName ?? targetNode.Title;
                    _navService.Navigate(typeof(GrammarPage), grammarQuery);
                    break;

                case RoadmapNodeType.MockTest:
                case RoadmapNodeType.Checkpoint:
                    if (context != null)
                    {
                        _navService.Navigate(typeof(TestPage), context);
                    }
                    else
                    {
                        var fallbackExamType = ActiveRoadmap?.Target switch
                        {
                            ExamTarget.Toeic => ExamType.Toeic,
                            ExamTarget.Ielts => ExamType.Ielts,
                            ExamTarget.Vstep => ExamType.Vstep,
                            _ => (ExamType?)null
                        };
                        _navService.Navigate(typeof(TestPage), new RoadmapNodeActionContextDto(
                            targetNode.Id,
                            targetNode.Title,
                            targetNode.Type,
                            targetNode.ReferenceId,
                            targetNode.ReferenceName,
                            Array.Empty<Guid>(),
                            targetNode.TargetItemCount,
                            targetNode.CompletedItemCount,
                            null,
                            null,
                            fallbackExamType));
                    }
                    break;

                default:
                    _navService.Navigate(typeof(RoadmapPage));
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] ContinueRoadmapAsync error: {ex.Message}");
            _navService.Navigate(typeof(RoadmapPage));
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public void ExecuteAdviceAction()
    {
        if (DailyAdvice == null) return;
        switch (DailyAdvice.RecommendedAction)
        {
            case StudyAdviceActionType.UrgentReview:
                _navService.Navigate(typeof(ReviewPage));
                break;
            case StudyAdviceActionType.WeakWordsQuiz:
                _navService.Navigate(typeof(TestPage), true);
                break;
            case StudyAdviceActionType.MilestonePractice:
                _navService.Navigate(typeof(TestPage));
                break;
            default:
                _navService.Navigate(typeof(VocabularyPage));
                break;
        }
    }

    [RelayCommand]
    public void StartReviewSession()
    {
        _navService.Navigate(typeof(ReviewPage));
    }

    [RelayCommand]
    public void StartPracticeTest()
    {
        _navService.Navigate(typeof(TestPage));
    }

    [RelayCommand]
    public void OpenVocabulary()
    {
        _navService.Navigate(typeof(VocabularyPage));
    }
}
