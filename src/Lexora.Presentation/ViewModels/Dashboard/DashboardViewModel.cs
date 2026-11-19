using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IStatisticsService _statsService;
    private readonly INavigationService _navService;
    private readonly ILocalizationService _localizationService;
    private readonly IStudyAdvisorService _advisorService;
    private readonly IRoadmapService? _roadmapService;
    private readonly IReviewService? _reviewService;
    private readonly EventHandler? _languageChangedHandler;
    private CancellationTokenSource? _loadCts;
    private bool _isLoadingDashboard;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveRoadmap))]
    public partial StudyRoadmapDto? ActiveRoadmap { get; set; }

    public bool HasActiveRoadmap => ActiveRoadmap != null;

    [ObservableProperty]
    public partial string GreetingText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial StudyAdviceSummaryDto? DailyAdvice { get; set; }

    [ObservableProperty]
    public partial WorkloadForecastDto? WorkloadForecast { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<DailyWorkloadPointDto> Workload14DaysList { get; set; } = Array.Empty<DailyWorkloadPointDto>();

    [ObservableProperty]
    public partial TimeBudgetPlanDto? TimeBudgetPlan { get; set; }

    [ObservableProperty]
    public partial string TimeBudgetSummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedBudgetMinutes { get; set; } = 15;

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
    public partial LearningIntelligenceReportDto? IntelligenceReport { get; set; }

    [ObservableProperty]
    public partial string PeakEfficiencyDisplay { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VelocitySummaryDisplay { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExamReadinessDisplay { get; set; } = string.Empty;

    public ObservableCollection<MemoryHealthSegmentDto> MemoryHealthList { get; } = new();
    public ObservableCollection<RetentionCurvePointDto> RetentionCurveList { get; } = new();
    public ObservableCollection<ExamScorePredictionDto> ExamPredictionsList { get; } = new();

    [ObservableProperty]
    public partial string HighImpactRecommendation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSubmitting { get; set; }

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

    [ObservableProperty]
    public partial bool HasRecentSessions { get; set; }

    private List<LearningStatusDistributionDto>? _rawStatusBreakdown;
    private List<ExamProgressDto>? _rawExamProgress;
    private List<CefrLevelDistributionDto>? _rawCefrDistribution;
    private List<PosDistributionDto>? _rawPosDistribution;
    private List<StudyHistoryDayDto>? _rawHeatmap;
    private List<StudySessionDto>? _rawRecentSessions;

    public DashboardViewModel(
        IStatisticsService statsService,
        INavigationService navService,
        ILocalizationService localizationService,
        IStudyAdvisorService advisorService,
        IRoadmapService? roadmapService = null,
        IReviewService? reviewService = null)
    {
        _statsService = statsService;
        _navService = navService;
        _localizationService = localizationService;
        _advisorService = advisorService;
        _roadmapService = roadmapService;
        _reviewService = reviewService;
        _languageChangedHandler = (s, e) => UpdateLanguageState();
        _localizationService.LanguageChanged += _languageChangedHandler;

        UpdateGreeting();
    }

    public void UpdateLanguageState()
    {
        UpdateGreeting();
        UpdateAdviceDisplay();
        UpdateForecastDisplay();
        UpdateIntelligenceDisplay();
        UpdateLocalizedStatusAndExams();
        UpdateLocalizedDistributions();
        UpdateLocalizedHeatmap();
        UpdateLocalizedRecentSessions();
        OnPropertyChanged(nameof(DueReviewMessage));
    }

    public void Dispose()
    {
        if (_languageChangedHandler != null)
        {
            _localizationService.LanguageChanged -= _languageChangedHandler;
        }
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        GC.SuppressFinalize(this);
    }
}
