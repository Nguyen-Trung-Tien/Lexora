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
        InitializeComponent();
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();

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
        _timer.Stop();
        _isDismissing = false;
        _isPaused = false;

        _totalDurationMs = Math.Max(1500, notification.DurationMs);
        _remainingMs = _totalDurationMs;
        TimeProgressBar.Value = 100;

        TitleTextBlock.Text = notification.Title;
        MessageTextBlock.Text = notification.Message;

        ApplyToastTheme(notification.Type);

        bool wasVisible = ToastRoot.Visibility == Visibility.Visible;
        ToastRoot.Visibility = Visibility.Visible;

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
        string glyph = "\uE946";

        switch (type)
        {
            case ToastType.Success:
                glyph = "\uE73E"; // Checkmark
                if (App.Current.Resources.TryGetValue("SystemFillColorSuccessBrush", out var sBrush))
                    themeBrush = sBrush as Brush;
                break;

            case ToastType.Error:
                glyph = "\uE783"; // Critical error
                if (App.Current.Resources.TryGetValue("SystemFillColorCriticalBrush", out var cBrush))
                    themeBrush = cBrush as Brush;
                break;

            case ToastType.Warning:
                glyph = "\uE7BA"; // Warning
                if (App.Current.Resources.TryGetValue("SystemFillColorCautionBrush", out var wBrush))
                    themeBrush = wBrush as Brush;
                break;

            case ToastType.Information:
            default:
                glyph = "\uE946"; // Info
                if (App.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out var aBrush))
                    themeBrush = aBrush as Brush;
                break;
        }

        themeBrush ??= (App.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out var def) ? def as Brush : null);

        TypeIcon.Glyph = glyph;
        if (themeBrush != null)
        {
            TypeIcon.Foreground = themeBrush;
            TimeProgressBar.Foreground = themeBrush;
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

        ExitStoryboard.Completed -= ExitStoryboard_Completed;
        ExitStoryboard.Completed += ExitStoryboard_Completed;
        ExitStoryboard.Begin();
    }

    private void ExitStoryboard_Completed(object? sender, object e)
    {
        ExitStoryboard.Completed -= ExitStoryboard_Completed;
        ToastRoot.Visibility = Visibility.Collapsed;
        _isDismissing = false;
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
