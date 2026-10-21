using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI.ViewManagement;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.Views;

public sealed partial class SplashScreenOverlay : UserControl
{
    private static BitmapImage? _preloadedLogo;

    public static BitmapImage PreloadAsset()
    {
        if (_preloadedLogo != null) return _preloadedLogo;
        try
        {
            _preloadedLogo = new BitmapImage
            {
                UriSource = new Uri("ms-appx:///Assets/brand/lexora-logo-splash.png"),
                DecodePixelWidth = 256,
                DecodePixelHeight = 256,
                DecodePixelType = DecodePixelType.Logical
            };
        }
        catch
        {
            // Graceful fallback
        }
        return _preloadedLogo!;
    }

    private readonly UISettings _uiSettings = new();
    private TaskCompletionSource<bool>? _entranceTcs;
    private TaskCompletionSource<bool>? _exitTcs;

    private double _currentVisualProgress = 0.12;
    private double _targetProgress = 0.12;
    private readonly DispatcherTimer _progressAnimationTimer = new();
    private TaskCompletionSource<bool>? _progressCompletionTcs;

    public event EventHandler? RetryRequested;
    public event EventHandler? ExitRequested;

    public bool IsAnimationEnabled => _uiSettings.AnimationsEnabled;

    public SplashScreenOverlay()
    {
        InitializeComponent();
        if (_preloadedLogo != null)
        {
            LogoTile.Source = _preloadedLogo;
        }
        else
        {
            var preloaded = PreloadAsset();
            if (preloaded != null)
            {
                LogoTile.Source = preloaded;
            }
        }
        UpdateLocalizedText();

        _progressAnimationTimer.Interval = TimeSpan.FromMilliseconds(16);
        _progressAnimationTimer.Tick += OnProgressAnimationTick;

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

        _targetProgress = Math.Clamp(progress, 0.0, 1.0);

        if (!IsAnimationEnabled)
        {
            _currentVisualProgress = _targetProgress;
            RenderProgress(_currentVisualProgress);
            if (_currentVisualProgress >= 1.0)
            {
                _progressCompletionTcs?.TrySetResult(true);
            }
            return;
        }

        if (_targetProgress >= 1.0 && (_progressCompletionTcs == null || _progressCompletionTcs.Task.IsCompleted))
        {
            _progressCompletionTcs = new TaskCompletionSource<bool>();
        }

        if (!_progressAnimationTimer.IsEnabled)
        {
            _progressAnimationTimer.Start();
        }
    }

    private void OnProgressAnimationTick(object? sender, object e)
    {
        var diff = _targetProgress - _currentVisualProgress;
        if (Math.Abs(diff) < 0.003)
        {
            _currentVisualProgress = _targetProgress;
            RenderProgress(_currentVisualProgress);
            if (_currentVisualProgress >= 1.0)
            {
                _progressAnimationTimer.Stop();
                _progressCompletionTcs?.TrySetResult(true);
            }
            else if (Math.Abs(_targetProgress - _currentVisualProgress) < 0.001)
            {
                _progressAnimationTimer.Stop();
            }
            return;
        }

        // Fluid ease-out step for smooth visual motion
        double step = Math.Max(0.006, Math.Abs(diff) * 0.08);
        if (diff > 0)
        {
            _currentVisualProgress = Math.Min(_targetProgress, _currentVisualProgress + step);
        }
        else
        {
            _currentVisualProgress = Math.Max(_targetProgress, _currentVisualProgress - step);
        }

        RenderProgress(_currentVisualProgress);

        if (_currentVisualProgress >= 1.0)
        {
            _progressAnimationTimer.Stop();
            _progressCompletionTcs?.TrySetResult(true);
        }
    }

    private void RenderProgress(double progress)
    {
        var percent = Math.Clamp(progress, 0.0, 1.0);
        StartupProgressBar.Value = percent * 100.0;
        ProgressPercentageTextBlock.Text = $"{(int)Math.Round(percent * 100)}%";

        UpdateStepIndicator(Step1Dot, Step1TextBlock, isDone: percent >= 0.35, isCurrent: percent >= 0.10 && percent < 0.35);
        UpdateStepIndicator(Step2Dot, Step2TextBlock, isDone: percent >= 0.70, isCurrent: percent >= 0.35 && percent < 0.70);
        UpdateStepIndicator(Step3Dot, Step3TextBlock, isDone: percent >= 0.98, isCurrent: percent >= 0.70 && percent < 0.98);
        UpdateStepIndicator(Step4Dot, Step4TextBlock, isDone: percent >= 1.0, isCurrent: percent >= 0.98);
    }

    public async Task WaitForProgressCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentVisualProgress >= 1.0) return;

        if (_progressCompletionTcs == null || _progressCompletionTcs.Task.IsCompleted)
        {
            _progressCompletionTcs = new TaskCompletionSource<bool>();
        }

        using var reg = cancellationToken.Register(() => _progressCompletionTcs.TrySetCanceled());
        var timeout = Task.Delay(2500, cancellationToken);
        await Task.WhenAny(_progressCompletionTcs.Task, timeout);
    }

    private static void UpdateStepIndicator(Microsoft.UI.Xaml.Shapes.Ellipse dot, TextBlock text, bool isDone, bool isCurrent)
    {
        string dotBrushKey = isDone
            ? "SystemFillColorSuccessBrush"
            : (isCurrent ? "AccentFillColorDefaultBrush" : "CardStrokeColorDefaultBrush");

        string textBrushKey = isDone
            ? "TextFillColorPrimaryBrush"
            : (isCurrent ? "AccentTextFillColorPrimaryBrush" : "TextFillColorTertiaryBrush");

        var rootElement = (App.Current as App)?.MainWindow?.Content as Microsoft.UI.Xaml.FrameworkElement;
        var dotBrush = ThemeResourceHelper.GetThemeBrush(rootElement, dotBrushKey);
        if (dotBrush != null)
        {
            dot.Fill = dotBrush;
        }

        var textBrush = ThemeResourceHelper.GetThemeBrush(rootElement, textBrushKey);
        if (textBrush != null)
        {
            text.Foreground = textBrush;
        }

        text.FontWeight = (isDone || isCurrent) ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    }

    public async Task PlayEntranceAsync()
    {
        await DispatcherQueue.EnsureOnUIThreadAsync();
        _entranceTcs = new TaskCompletionSource<bool>();

        if (!IsAnimationEnabled)
        {
            LogoTileCard.Opacity = 1.0;
            TypographyContainer.Opacity = 1.0;
            LoadingContainer.Opacity = 1.0;
            GlowingDivider.Opacity = 1.0;
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

        _currentVisualProgress = 0.12;
        _targetProgress = 0.12;
        RenderProgress(0.12);

        if (!IsAnimationEnabled)
        {
            LogoTileCard.Opacity = 1.0;
            TypographyContainer.Opacity = 1.0;
            LoadingContainer.Opacity = 1.0;
            GlowingDivider.Opacity = 1.0;
        }
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
