using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Lexora.Presentation.Views;

namespace Lexora.Presentation.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IStatisticsService _statsService;
    private readonly INavigationService _navService;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial int TotalWords { get; set; }

    [ObservableProperty]
    public partial int DueToday { get; set; }

    [ObservableProperty]
    public partial int Mastered { get; set; }

    [ObservableProperty]
    public partial int LearningCount { get; set; }

    [ObservableProperty]
    public partial int StreakDays { get; set; }

    [ObservableProperty]
    public partial int LongestStreakDays { get; set; }

    [ObservableProperty]
    public partial double AccuracyRate { get; set; }

    [ObservableProperty]
    public partial int StudiedTodayCount { get; set; }

    public ObservableCollection<LearningStatusDistributionDto> StatusBreakdown { get; } = new();
    public ObservableCollection<ExamProgressDto> ExamProgressList { get; } = new();
    public ObservableCollection<CefrLevelDistributionDto> CefrDistributionList { get; } = new();
    public ObservableCollection<PosDistributionDto> PosDistributionList { get; } = new();
    public ObservableCollection<TopicProgressDto> TopicProgressList { get; } = new();
    public ObservableCollection<StudyHistoryDayDto> StudyHeatmapList { get; } = new();
    public ObservableCollection<StudySessionDto> RecentSessionsList { get; } = new();

    public DashboardViewModel(IStatisticsService statsService, INavigationService navService)
    {
        _statsService = statsService;
        _navService = navService;
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        IsLoading = true;
        try
        {
            var analytics = await _statsService.GetDetailedAnalyticsAsync();
            var summary = analytics.Summary;

            TotalWords = summary.TotalWords;
            DueToday = summary.DueReviewsToday;
            Mastered = summary.TotalMastered;
            LearningCount = summary.TotalLearning + summary.TotalRemembered;
            StreakDays = summary.CurrentStreakDays;
            LongestStreakDays = summary.LongestStreakDays;
            AccuracyRate = summary.AccuracyRate;
            StudiedTodayCount = summary.StudiedTodayCount;

            StatusBreakdown.Clear();
            foreach (var s in analytics.StatusBreakdown) StatusBreakdown.Add(s);

            ExamProgressList.Clear();
            foreach (var ep in analytics.ExamProgress) ExamProgressList.Add(ep);

            CefrDistributionList.Clear();
            foreach (var c in analytics.CefrDistribution) CefrDistributionList.Add(c);

            PosDistributionList.Clear();
            foreach (var p in analytics.PosDistribution) PosDistributionList.Add(p);

            TopicProgressList.Clear();
            foreach (var tp in analytics.TopicProgress) TopicProgressList.Add(tp);

            StudyHeatmapList.Clear();
            foreach (var h in analytics.Last30DaysHeatmap) StudyHeatmapList.Add(h);

            RecentSessionsList.Clear();
            foreach (var rs in analytics.RecentSessions) RecentSessionsList.Add(rs);
        }
        finally
        {
            IsLoading = false;
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
