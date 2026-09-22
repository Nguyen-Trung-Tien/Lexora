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
    private readonly ILocalizationService _localizationService;
    private readonly IStudyAdvisorService _advisorService;

    [ObservableProperty]
    public partial string GreetingText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial StudyAdviceSummaryDto? DailyAdvice { get; set; }

    [ObservableProperty]
    public partial WorkloadForecastDto? WorkloadForecast { get; set; }

    [ObservableProperty]
    public partial string AdviceTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AdviceDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AdviceButtonText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TotalForecastNext7Days { get; set; } = 0;

    public ObservableCollection<DailyForecastItemDto> ForecastDaysList { get; } = new();

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial int TotalWords { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDueReviews))]
    [NotifyPropertyChangedFor(nameof(DueReviewMessage))]
    public partial int DueToday { get; set; }

    public bool HasDueReviews => DueToday > 0;

    public string DueReviewMessage
    {
        get
        {
            if (DueToday <= 0) return string.Empty;
            var template = _localizationService.GetString("Review_Reminder_Msg");
            return template.Contains("{0}") ? string.Format(template, DueToday) : $"{template} {DueToday}";
        }
    }

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

    public DashboardViewModel(
        IStatisticsService statsService,
        INavigationService navService,
        ILocalizationService localizationService,
        IStudyAdvisorService advisorService)
    {
        _statsService = statsService;
        _navService = navService;
        _localizationService = localizationService;
        _advisorService = advisorService;

        UpdateGreeting();
    }

    public void UpdateLanguageState()
    {
        UpdateGreeting();
        UpdateAdviceDisplay();
        UpdateForecastDisplay();
        OnPropertyChanged(nameof(DueReviewMessage));
    }

    private void UpdateGreeting()
    {
        int hour = DateTime.Now.Hour;
        if (hour < 12)
            GreetingText = _localizationService.GetString("Dashboard_GreetingMorning");
        else if (hour < 18)
            GreetingText = _localizationService.GetString("Dashboard_GreetingAfternoon");
        else
            GreetingText = _localizationService.GetString("Dashboard_GreetingEvening");
    }

    private void UpdateAdviceDisplay()
    {
        if (DailyAdvice == null) return;
        bool isVi = _localizationService.CurrentLanguage.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        AdviceTitle = isVi ? DailyAdvice.AdviceTitleVi : DailyAdvice.AdviceTitleEn;
        AdviceDescription = isVi ? DailyAdvice.AdviceDescriptionVi : DailyAdvice.AdviceDescriptionEn;
        AdviceButtonText = isVi ? DailyAdvice.ActionButtonTextVi : DailyAdvice.ActionButtonTextEn;
    }

    private string GetLocalizedDayLabel(string dayLabel)
    {
        return dayLabel switch
        {
            "Today" => _localizationService.GetString("Day_Today"),
            "Tomorrow" => _localizationService.GetString("Day_Tomorrow"),
            "Mon" => _localizationService.GetString("Day_Mon"),
            "Tue" => _localizationService.GetString("Day_Tue"),
            "Wed" => _localizationService.GetString("Day_Wed"),
            "Thu" => _localizationService.GetString("Day_Thu"),
            "Fri" => _localizationService.GetString("Day_Fri"),
            "Sat" => _localizationService.GetString("Day_Sat"),
            "Sun" => _localizationService.GetString("Day_Sun"),
            _ => dayLabel
        };
    }

    private void UpdateForecastDisplay()
    {
        ForecastDaysList.Clear();
        if (WorkloadForecast != null)
        {
            TotalForecastNext7Days = WorkloadForecast.TotalDueNext7Days;
            foreach (var d in WorkloadForecast.Days)
            {
                ForecastDaysList.Add(d with { DayLabel = GetLocalizedDayLabel(d.DayLabel) });
            }
        }
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        IsLoading = true;
        try
        {
            var analytics = await _statsService.GetDetailedAnalyticsAsync();
            DailyAdvice = await _advisorService.GetDailyAdviceAsync(analytics.Summary);
            WorkloadForecast = await _advisorService.Get7DayForecastAsync();

            UpdateAdviceDisplay();
            UpdateForecastDisplay();

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadDashboardAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
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
