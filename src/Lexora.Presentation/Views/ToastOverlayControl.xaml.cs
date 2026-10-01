using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Lexora.Presentation.Views;

public sealed partial class ToastOverlayControl : UserControl
{
    public ILocalizationService Loc { get; }
    private readonly DispatcherTimer _timer;
    private int _totalDurationMs = 4000;
    private int _remainingMs = 4000;
    private bool _isPaused;
    private bool _isDismissing;

    public ToastOverlayControl()
    {
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += Timer_Tick;

        Loaded += ToastOverlayControl_Loaded;
        Unloaded += ToastOverlayControl_Unloaded;
    }

    private void ToastOverlayControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (App.Current?.Services?.GetService<IToastNotificationService>() is { } toastService)
        {
            toastService.ToastRequested += OnToastRequested;
        }
    }

    private void ToastOverlayControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (App.Current?.Services?.GetService<IToastNotificationService>() is { } toastService)
        {
            toastService.ToastRequested -= OnToastRequested;
        }
        _timer.Stop();
    }

    private void OnToastRequested(ToastNotification notification)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            Show(notification);
        });
    }

    public void Show(ToastNotification notification)
    {
        // Stop any running animations immediately to avoid conflicting storyboard completions
        EntranceStoryboard.Stop();
        UpdatePulseStoryboard.Stop();
        ExitStoryboard.Stop();

        _timer.Stop();
        _isDismissing = false;
        _isPaused = false;

        _totalDurationMs = Math.Max(1500, notification.DurationMs);
        _remainingMs = _totalDurationMs;
        TimeProgressBar.Value = 100;

        TitleTextBlock.Text = notification.Title;
        TitleTextBlock.Visibility = string.IsNullOrWhiteSpace(notification.Title) ? Visibility.Collapsed : Visibility.Visible;

        MessageTextBlock.Text = notification.Message;
        ToolTipService.SetToolTip(MessageTextBlock, notification.Message);

        ApplyToastTheme(notification.Type);

        IsHitTestVisible = true;
        bool wasVisible = ToastRoot.Visibility == Visibility.Visible;
        ToastRoot.Visibility = Visibility.Visible;
        ToastRoot.Opacity = 1.0;
        ToastTranslate.Y = 0;
        ToastScale.ScaleX = 1.0;
        ToastScale.ScaleY = 1.0;

        if (wasVisible)
        {
            UpdatePulseStoryboard.Begin();
        }
        else
        {
            EntranceStoryboard.Begin();
        }

        _timer.Start();
    }

    private void ApplyToastTheme(ToastType type)
    {
        Brush? themeBrush = null;
        Brush? badgeBgBrush = null;
        string glyph = "\uE946";

        switch (type)
        {
            case ToastType.Success:
                glyph = "\uE73E"; // Checkmark
                if (App.Current.Resources.TryGetValue("SystemFillColorSuccessBrush", out var sBrush))
                    themeBrush = sBrush as Brush;
                if (App.Current.Resources.TryGetValue("SystemFillColorSuccessBackgroundBrush", out var sBgBrush))
                    badgeBgBrush = sBgBrush as Brush;
                break;

            case ToastType.Error:
                glyph = "\uE783"; // Critical error
                if (App.Current.Resources.TryGetValue("SystemFillColorCriticalBrush", out var cBrush))
                    themeBrush = cBrush as Brush;
                if (App.Current.Resources.TryGetValue("SystemFillColorCriticalBackgroundBrush", out var cBgBrush))
                    badgeBgBrush = cBgBrush as Brush;
                break;

            case ToastType.Warning:
                glyph = "\uE7BA"; // Warning
                if (App.Current.Resources.TryGetValue("SystemFillColorCautionBrush", out var wBrush))
                    themeBrush = wBrush as Brush;
                if (App.Current.Resources.TryGetValue("SystemFillColorCautionBackgroundBrush", out var wBgBrush))
                    badgeBgBrush = wBgBrush as Brush;
                break;

            case ToastType.Information:
            default:
                glyph = "\uE946"; // Info
                if (App.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out var def))
                    themeBrush = def as Brush;
                if (App.Current.Resources.TryGetValue("SubtleFillColorSecondaryBrush", out var iBgBrush))
                    badgeBgBrush = iBgBrush as Brush;
                break;
        }

        themeBrush ??= (App.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out var fallbackBrush) ? fallbackBrush as Brush : null);
        badgeBgBrush ??= (App.Current.Resources.TryGetValue("SubtleFillColorSecondaryBrush", out var fallbackBg) ? fallbackBg as Brush : null);

        TypeIcon.Glyph = glyph;
        if (themeBrush != null)
        {
            TypeIcon.Foreground = themeBrush;
            TimeProgressBar.Foreground = themeBrush;
        }
        if (badgeBgBrush != null)
        {
            IconBadgeBorder.Background = badgeBgBrush;
        }
    }

    private void Timer_Tick(object? sender, object e)
    {
        if (_isPaused || _isDismissing) return;

        _remainingMs -= 50;
        TimeProgressBar.Value = Math.Max(0, ((double)_remainingMs / _totalDurationMs) * 100);

        if (_remainingMs <= 0)
        {
            Dismiss();
        }
    }

    public void Dismiss()
    {
        if (_isDismissing) return;
        _isDismissing = true;
        _timer.Stop();

        ExitStoryboard.Stop();
        ExitStoryboard.Completed -= ExitStoryboard_Completed;
        ExitStoryboard.Completed += ExitStoryboard_Completed;
        ExitStoryboard.Begin();
    }

    private void ExitStoryboard_Completed(object? sender, object e)
    {
        ExitStoryboard.Completed -= ExitStoryboard_Completed;
        if (_isDismissing)
        {
            ToastRoot.Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;
            _isDismissing = false;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    private void ToastRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPaused = true;
    }

    private void ToastRoot_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPaused = false;
    }
}
