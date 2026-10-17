using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class DataHealthCenterDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly IDataHealthService _healthService;
    public ILocalizationService Loc { get; }

    private DatabaseHealthReportDto? _report;
    private bool _isBusy;
    private string _statusMessage = string.Empty;
    private bool _hasResultFeedback;
    private InfoBarSeverity _feedbackSeverity = InfoBarSeverity.Informational;
    private string _feedbackTitle = string.Empty;
    private string _feedbackMessage = string.Empty;

    public DatabaseHealthReportDto? Report
    {
        get => _report;
        set
        {
            _report = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HealthScoreText));
            OnPropertyChanged(nameof(HealthRatingDescription));
            OnPropertyChanged(nameof(FragmentationText));
            OnPropertyChanged(nameof(OrphansText));
            OnPropertyChanged(nameof(AudioCacheText));
            OnPropertyChanged(nameof(WalLabel));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !_isBusy;

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool HasResultFeedback
    {
        get => _hasResultFeedback;
        set { _hasResultFeedback = value; OnPropertyChanged(); }
    }

    public InfoBarSeverity FeedbackSeverity
    {
        get => _feedbackSeverity;
        set { _feedbackSeverity = value; OnPropertyChanged(); }
    }

    public string FeedbackTitle
    {
        get => _feedbackTitle;
        set { _feedbackTitle = value; OnPropertyChanged(); }
    }

    public string FeedbackMessage
    {
        get => _feedbackMessage;
        set { _feedbackMessage = value; OnPropertyChanged(); }
    }

    public string HealthScoreText => Report?.HealthScore.ToString() ?? "--";

    public string HealthRatingDescription => Report?.HealthRating switch
    {
        DatabaseHealthRating.Optimal => Loc.GetString("DataHealth_Optimal"),
        DatabaseHealthRating.Good => Loc.GetString("DataHealth_Good"),
        DatabaseHealthRating.NeedsMaintenance => Loc.GetString("DataHealth_NeedsMaintenance"),
        _ => Loc.GetString("DataHealth_Critical")
    };

    public string FragmentationText => Report != null
        ? $"{Report.PageCount} pages ({Report.FragmentationPercent:F1}% free)"
        : "--";

    public string OrphansText => Report != null
        ? $"{Report.OrphanMeaningsCount + Report.OrphanExamplesCount} orphans, {Report.MissingReviewCardsCount} missing cards"
        : "--";

    public string AudioCacheText => Report != null
        ? $"{Report.AudioCacheFilesCount} files ({Report.FormattedAudioCacheSize})"
        : "--";

    public string WalLabel => Report != null ? $"(WAL: {Report.FormattedWalSize})" : string.Empty;

    public DataHealthCenterDialog(IDataHealthService healthService, ILocalizationService loc)
    {
        _healthService = healthService;
        Loc = loc;
        this.InitializeComponent();

        if (App.Current?.Services?.GetService<Services.IThemeService>() is { } themeService && themeService.CurrentTheme != ElementTheme.Default)
        {
            RequestedTheme = themeService.CurrentTheme;
        }

        Loaded += async (s, e) =>
        {
            await RunDiagnosticsInternalAsync();
        };
    }

    private async void RunDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        await RunDiagnosticsInternalAsync();
    }

    private async Task RunDiagnosticsInternalAsync()
    {
        IsBusy = true;
        StatusMessage = "Analyzing SQLite schema and records...";
        HasResultFeedback = false;

        try
        {
            Report = await _healthService.RunDiagnosticsAsync();
            FeedbackTitle = Loc.GetString("Toast_SuccessTitle");
            FeedbackMessage = $"Diagnostics complete. Health score: {Report.HealthScore}/100 ({HealthRatingDescription}).";
            FeedbackSeverity = Report.HasIssues ? InfoBarSeverity.Warning : InfoBarSeverity.Success;
            HasResultFeedback = true;
        }
        catch (Exception ex)
        {
            FeedbackTitle = Loc.GetString("Toast_ErrorTitle");
            FeedbackMessage = ex.Message;
            FeedbackSeverity = InfoBarSeverity.Error;
            HasResultFeedback = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void RepairAndOptimize_Click(object sender, RoutedEventArgs e)
    {
        IsBusy = true;
        StatusMessage = "Optimizing, cleaning orphans and executing SQLite VACUUM...";
        HasResultFeedback = false;

        try
        {
            var res = await _healthService.RepairAndOptimizeAsync(vacuumDatabase: true, clearAudioCache: false);
            FeedbackTitle = Loc.GetString("DataHealth_RepairSuccess");
            FeedbackMessage = res.Summary;
            FeedbackSeverity = InfoBarSeverity.Success;
            HasResultFeedback = true;

            // Re-run diagnostics
            Report = await _healthService.RunDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            FeedbackTitle = Loc.GetString("Toast_ErrorTitle");
            FeedbackMessage = ex.Message;
            FeedbackSeverity = InfoBarSeverity.Error;
            HasResultFeedback = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ClearAudioCache_Click(object sender, RoutedEventArgs e)
    {
        IsBusy = true;
        StatusMessage = "Clearing cached audio files...";
        HasResultFeedback = false;

        try
        {
            long freed = await _healthService.ClearAudioCacheAsync();
            string freedFormatted = freed < 1024 * 1024 ? $"{freed / 1024.0:F1} KB" : $"{freed / (1024.0 * 1024.0):F2} MB";
            FeedbackTitle = Loc.GetString("Toast_SuccessTitle");
            FeedbackMessage = string.Format(Loc.GetString("DataHealth_AudioCleared"), freedFormatted);
            FeedbackSeverity = InfoBarSeverity.Success;
            HasResultFeedback = true;

            Report = await _healthService.RunDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            FeedbackTitle = Loc.GetString("Toast_ErrorTitle");
            FeedbackMessage = ex.Message;
            FeedbackSeverity = InfoBarSeverity.Error;
            HasResultFeedback = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
