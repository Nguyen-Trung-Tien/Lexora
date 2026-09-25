using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class OperationProgressDialog : ContentDialog
{
    public OperationProgressDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        var actualTheme = (App.Current?.MainWindow?.Content as FrameworkElement)?.ActualTheme;
        if (actualTheme is ElementTheme.Light or ElementTheme.Dark)
        {
            RequestedTheme = actualTheme.Value;
        }
        else if (App.Current?.Services?.GetService<IThemeService>() is { } themeService && themeService.CurrentTheme != ElementTheme.Default)
        {
            RequestedTheme = themeService.CurrentTheme;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DialogEntranceStoryboard.Begin();
    }

    /// <summary>
    /// Updates progress to a specific percentage (0 - 100) with descriptive messages.
    /// </summary>
    public void SetDeterminate(int percent, string message, string? detail = null)
    {
        if (DispatcherQueue != null && !DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => SetDeterminate(percent, message, detail));
            return;
        }

        percent = Math.Clamp(percent, 0, 100);

        ActiveProgressPanel.Visibility = Visibility.Visible;
        CompletedPanel.Visibility = Visibility.Collapsed;

        MainProgressRing.IsIndeterminate = false;
        MainProgressRing.Value = percent;

        LinearProgressBar.IsIndeterminate = false;
        LinearProgressBar.Value = percent;

        PercentTextBlock.Text = $"{percent}%";
        StepMessageText.Text = message;

        if (!string.IsNullOrWhiteSpace(detail))
        {
            StepDetailText.Text = detail;
            StepDetailText.Visibility = Visibility.Visible;
        }
        else
        {
            StepDetailText.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Switches to smooth indeterminate animation for operations with unknown duration.
    /// </summary>
    public void SetIndeterminate(string message, string? detail = null)
    {
        if (DispatcherQueue != null && !DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => SetIndeterminate(message, detail));
            return;
        }

        ActiveProgressPanel.Visibility = Visibility.Visible;
        CompletedPanel.Visibility = Visibility.Collapsed;

        MainProgressRing.IsIndeterminate = true;
        LinearProgressBar.IsIndeterminate = true;

        PercentTextBlock.Text = "...";
        StepMessageText.Text = message;

        if (!string.IsNullOrWhiteSpace(detail))
        {
            StepDetailText.Text = detail;
            StepDetailText.Visibility = Visibility.Visible;
        }
        else
        {
            StepDetailText.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Shows the high-fidelity completed state with summary metrics cards and enables the Close button.
    /// </summary>
    public void SetCompleted(string title, string summary, int imported = 0, int skipped = 0, int errors = 0, string? closeButtonText = null)
    {
        if (DispatcherQueue != null && !DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => SetCompleted(title, summary, imported, skipped, errors, closeButtonText));
            return;
        }

        ActiveProgressPanel.Visibility = Visibility.Collapsed;
        CompletedPanel.Visibility = Visibility.Visible;

        // Visual badge: Green success checkmark
        StatusBadgeBorder.Background = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBackgroundBrush");
        StatusBadgeBorder.BorderBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBrush");
        StatusBadgeIcon.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBrush");
        StatusBadgeIcon.Glyph = "\uE73E";

        if (!string.IsNullOrWhiteSpace(title) && title.Contains("{0}"))
        {
            title = title.Replace("{0}", "").Trim(' ', ':', '-');
        }

        CompletedTitleText.Text = title;
        CompletedSummaryText.Text = summary;

        StatImportedCount.Text = imported.ToString();
        StatSkippedCount.Text = skipped.ToString();
        StatErrorCount.Text = errors.ToString();

        // If no metrics to display (e.g. export or backup), hide the metrics grid
        if (imported == 0 && skipped == 0 && errors == 0)
        {
            MetricsGrid.Visibility = Visibility.Collapsed;
        }
        else
        {
            MetricsGrid.Visibility = Visibility.Visible;
        }

        var defaultCloseText = App.Current?.Services?.GetService<ILocalizationService>()?.GetString("Action_Close") ?? "Close";
        CloseButtonText = !string.IsNullOrWhiteSpace(closeButtonText) ? closeButtonText : defaultCloseText;
    }

    /// <summary>
    /// Displays an error or failure state.
    /// </summary>
    public void SetError(string title, string errorMessage, string? closeButtonText = null)
    {
        if (DispatcherQueue != null && !DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => SetError(title, errorMessage, closeButtonText));
            return;
        }

        ActiveProgressPanel.Visibility = Visibility.Collapsed;
        CompletedPanel.Visibility = Visibility.Visible;

        var defaultCloseText = App.Current?.Services?.GetService<ILocalizationService>()?.GetString("Action_Close") ?? "Close";
        CloseButtonText = !string.IsNullOrWhiteSpace(closeButtonText) ? closeButtonText : defaultCloseText;

        // Visual badge: Red critical cross icon
        StatusBadgeBorder.Background = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBackgroundBrush");
        StatusBadgeBorder.BorderBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
        StatusBadgeIcon.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
        StatusBadgeIcon.Glyph = "\uEA39";

        if (!string.IsNullOrWhiteSpace(title) && title.Contains("{0}"))
        {
            title = title.Replace("{0}", "").Trim(' ', ':', '-');
        }

        CompletedTitleText.Text = title;
        CompletedSummaryText.Text = errorMessage;
        MetricsGrid.Visibility = Visibility.Collapsed;

        CloseButtonText = closeButtonText;
    }
}
