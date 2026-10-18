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
    private int _totalDurationMs = 4000;
    private bool _isPaused;
    private bool _isDismissing;
    private Action? _currentActionCallback;

    public ToastOverlayControl()
    {
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        InitializeComponent();

        ProgressStoryboard.Completed += ProgressStoryboard_Completed;

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
        ProgressStoryboard.Stop();
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
        ProgressStoryboard.Stop();

        _isDismissing = false;
        _isPaused = false;

        _totalDurationMs = Math.Max(1500, notification.DurationMs);
        ProgressBarScale.ScaleX = 1.0;
        ProgressAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(_totalDurationMs));

        TitleTextBlock.Text = notification.Title;
        TitleTextBlock.Visibility = string.IsNullOrWhiteSpace(notification.Title) ? Visibility.Collapsed : Visibility.Visible;

        MessageTextBlock.Text = notification.Message;
        ToolTipService.SetToolTip(MessageTextBlock, notification.Message);

        if (!string.IsNullOrWhiteSpace(notification.ActionText))
        {
            ActionButton.Content = notification.ActionText;
            ActionButton.Visibility = Visibility.Visible;
            _currentActionCallback = notification.ActionCallback;
        }
        else
        {
            ActionButton.Visibility = Visibility.Collapsed;
            _currentActionCallback = null;
        }

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

        ProgressStoryboard.Begin();
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
                themeBrush = Services.ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBrush");
                badgeBgBrush = Services.ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBackgroundBrush");
                break;

            case ToastType.Error:
                glyph = "\uE783"; // Critical error
                themeBrush = Services.ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
                badgeBgBrush = Services.ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBackgroundBrush");
                break;

            case ToastType.Warning:
                glyph = "\uE7BA"; // Warning
                themeBrush = Services.ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCautionBrush");
                badgeBgBrush = Services.ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCautionBackgroundBrush");
                break;

            case ToastType.Information:
            default:
                glyph = "\uE946"; // Info
                themeBrush = Services.ThemeResourceHelper.GetThemeBrush(this, "AccentFillColorDefaultBrush");
                badgeBgBrush = Services.ThemeResourceHelper.GetThemeBrush(this, "SubtleFillColorSecondaryBrush");
                break;
        }

        themeBrush ??= Services.ThemeResourceHelper.GetThemeBrush(this, "AccentFillColorDefaultBrush");
        badgeBgBrush ??= Services.ThemeResourceHelper.GetThemeBrush(this, "SubtleFillColorSecondaryBrush");

        TypeIcon.Glyph = glyph;
        if (themeBrush != null)
        {
            TypeIcon.Foreground = themeBrush;
            TimeProgressBar.Background = themeBrush;
        }
        if (badgeBgBrush != null)
        {
            IconBadgeBorder.Background = badgeBgBrush;
        }
    }

    private void ProgressStoryboard_Completed(object? sender, object e)
    {
        if (!_isDismissing && !_isPaused)
        {
            Dismiss();
        }
    }

    public void Dismiss()
    {
        if (_isDismissing) return;
        _isDismissing = true;
        ProgressStoryboard.Stop();

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

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        var callback = _currentActionCallback;
        _currentActionCallback = null;
        Dismiss();
        callback?.Invoke();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _currentActionCallback = null;
        Dismiss();
    }

    private void ToastRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (_isDismissing) return;
        _isPaused = true;
        ProgressStoryboard.Pause();
    }

    private void ToastRoot_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (_isDismissing || !_isPaused) return;
        _isPaused = false;
        ProgressStoryboard.Resume();
    }
}
