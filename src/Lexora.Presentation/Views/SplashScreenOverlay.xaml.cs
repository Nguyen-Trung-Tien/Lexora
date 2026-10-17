using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.Views;

public sealed partial class SplashScreenOverlay : UserControl
{
    private readonly UISettings _uiSettings = new();
    private TaskCompletionSource<bool>? _entranceTcs;
    private TaskCompletionSource<bool>? _exitTcs;

    public event EventHandler? RetryRequested;
    public event EventHandler? ExitRequested;

    public bool IsAnimationEnabled => _uiSettings.AnimationsEnabled;

    public SplashScreenOverlay()
    {
        InitializeComponent();
        UpdateLocalizedText();

        EntranceStoryboard.Completed += (s, e) => _entranceTcs?.TrySetResult(true);
        ExitStoryboard.Completed += (s, e) =>
        {
            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;
            _exitTcs?.TrySetResult(true);
        };
        ReducedMotionEntrance.Completed += (s, e) => _entranceTcs?.TrySetResult(true);
    }

    public void UpdateLocalizedText()
    {
        TaglineTextBlock.Text = LocalizationHelper.GetString("Splash_Tagline");
        EditionBadgeTextBlock.Text = LocalizationHelper.GetString("Splash_Badge");
        StatusTextBlock.Text = LocalizationHelper.GetString("Splash_StatusStarting");
        Step1TextBlock.Text = LocalizationHelper.GetString("Splash_StepServices");
        Step2TextBlock.Text = LocalizationHelper.GetString("Splash_StepDatabase");
        Step3TextBlock.Text = LocalizationHelper.GetString("Splash_StepData");
        Step4TextBlock.Text = LocalizationHelper.GetString("Splash_StepReady");
        ErrorTitleTextBlock.Text = LocalizationHelper.GetString("Splash_InitFailed");
        ErrorDetailsTextBlock.Text = LocalizationHelper.GetString("Splash_InitFailedDesc");
        RetryButton.Content = LocalizationHelper.GetString("Splash_Retry");
        ExitButton.Content = LocalizationHelper.GetString("Splash_Exit");
    }

    public void UpdateProgress(double progress)
    {
        if (DispatcherQueue != null && !DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => UpdateProgress(progress));
            return;
        }

        var percent = Math.Clamp(progress, 0.0, 1.0);
        StartupProgressBar.Value = percent * 100.0;
        ProgressPercentageTextBlock.Text = $"{(int)Math.Round(percent * 100)}%";

        UpdateStepIndicator(Step1Dot, Step1TextBlock, percent >= 0.10);
        UpdateStepIndicator(Step2Dot, Step2TextBlock, percent >= 0.35);
        UpdateStepIndicator(Step3Dot, Step3TextBlock, percent >= 0.70);
        UpdateStepIndicator(Step4Dot, Step4TextBlock, percent >= 0.98);
    }

    private static void UpdateStepIndicator(Microsoft.UI.Xaml.Shapes.Ellipse dot, TextBlock text, bool isActive)
    {
        if (App.Current.Resources.TryGetValue(
            isActive ? "AccentFillColorDefaultBrush" : "SubtleFillColorSecondaryBrush",
            out var dotBrushObj) && dotBrushObj is Microsoft.UI.Xaml.Media.Brush dotBrush)
        {
            dot.Fill = dotBrush;
        }

        if (App.Current.Resources.TryGetValue(
            isActive ? "AccentTextFillColorPrimaryBrush" : "TextFillColorTertiaryBrush",
            out var textBrushObj) && textBrushObj is Microsoft.UI.Xaml.Media.Brush textBrush)
        {
            text.Foreground = textBrush;
        }
    }

    public async Task PlayEntranceAsync()
    {
        await DispatcherQueue.EnsureOnUIThreadAsync();
        _entranceTcs = new TaskCompletionSource<bool>();

        if (!IsAnimationEnabled)
        {
            ReducedMotionEntrance.Begin();
        }
        else
        {
            EntranceStoryboard.Begin();
        }

        // Safety timeout in case storyboard completion is dropped by WinUI 3
        var entranceTimeout = Task.Delay(1200);
        await Task.WhenAny(_entranceTcs.Task, entranceTimeout);
    }

    public async Task PlayExitAsync()
    {
        await DispatcherQueue.EnsureOnUIThreadAsync();
        IsHitTestVisible = false;
        _exitTcs = new TaskCompletionSource<bool>();

        if (!IsAnimationEnabled)
        {
            RootGrid.Opacity = 0;
            Visibility = Visibility.Collapsed;
            _exitTcs.TrySetResult(true);
        }
        else
        {
            ExitStoryboard.Begin();

            // Safety timeout in case storyboard completion is dropped by WinUI 3
            _ = Task.Delay(400).ContinueWith(_ =>
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    Visibility = Visibility.Collapsed;
                    IsHitTestVisible = false;
                    _exitTcs?.TrySetResult(true);
                });
            });
        }

        var exitTimeout = Task.Delay(500);
        await Task.WhenAny(_exitTcs.Task, exitTimeout);
        await DispatcherQueue.EnsureOnUIThreadAsync();
        Visibility = Visibility.Collapsed;
        IsHitTestVisible = false;
    }

    public void UpdateStatus(string message)
    {
        if (DispatcherQueue != null && !DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => UpdateStatus(message));
            return;
        }

        StatusTextBlock.Text = message;
    }

    public void ShowLoadingState()
    {
        if (DispatcherQueue != null && !DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(ShowLoadingState);
            return;
        }

        LoadingContainer.Visibility = Visibility.Visible;
        ErrorCard.Visibility = Visibility.Collapsed;
    }

    public void ShowError(string errorMessage)
    {
        if (DispatcherQueue != null && !DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => ShowError(errorMessage));
            return;
        }

        LoadingContainer.Visibility = Visibility.Collapsed;
        ErrorDetailsTextBlock.Text = string.IsNullOrWhiteSpace(errorMessage)
            ? LocalizationHelper.GetString("Splash_InitFailedDesc")
            : errorMessage;
        ErrorCard.Visibility = Visibility.Visible;
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        ShowLoadingState();
        RetryRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }
}
