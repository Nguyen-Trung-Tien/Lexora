using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;

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
    public partial int StreakDays { get; set; }

    [ObservableProperty]
    public partial double AccuracyRate { get; set; }

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
            var summary = await _statsService.GetDashboardSummaryAsync();
            TotalWords = summary.TotalWords;
            DueToday = summary.DueReviewsToday;
            Mastered = summary.TotalMastered;
            StreakDays = summary.CurrentStreakDays;
            AccuracyRate = summary.AccuracyRate;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
