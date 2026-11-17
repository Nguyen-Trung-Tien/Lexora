using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Lexora.Presentation.Views.Common;

public sealed class ToastCardItem : INotifyPropertyChanged
{
    private double _remainingPercent = 100.0;

    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ActionText { get; init; }
    public Action? ActionCallback { get; init; }
    public string CloseTooltip { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = "\uE946";
    public Brush? AccentBrush { get; init; }
    public Brush? BadgeBackground { get; init; }
    public double TotalDurationMs { get; init; } = 4000;
    public double RemainingMs { get; set; } = 4000;
    public bool IsPaused { get; set; }

    public Visibility TitleVisibility => string.IsNullOrWhiteSpace(Title) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ActionVisibility => string.IsNullOrWhiteSpace(ActionText) ? Visibility.Collapsed : Visibility.Visible;

    public double RemainingFraction => Math.Clamp(_remainingPercent / 100.0, 0.0, 1.0);

    public double RemainingPercent
    {
        get => _remainingPercent;
        set
        {
            if (Math.Abs(_remainingPercent - value) > 0.05)
            {
                _remainingPercent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RemainingFraction));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed partial class ToastOverlayControl : UserControl
{
    private const int MaxVisibleToasts = 3;
    private const int TickIntervalMs = 16;

    public ILocalizationService Loc { get; }
    private readonly ObservableCollection<ToastCardItem> _visibleToasts = new();
    private readonly Queue<ToastNotification> _pendingQueue = new();
    private readonly DispatcherTimer _tickTimer;
    private long _lastTickTimestamp;

    public ToastOverlayControl()
    {
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        InitializeComponent();

        ToastItemsControl.ItemsSource = _visibleToasts;

        _tickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(TickIntervalMs)
        };
        _tickTimer.Tick += TickTimer_Tick;

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
        _tickTimer.Stop();
    }

    private void OnToastRequested(ToastNotification notification)
    {
        DispatcherQueue.TryEnqueue(() => Show(notification));
    }

    public void Show(ToastNotification notification)
    {
        if (_visibleToasts.Count >= MaxVisibleToasts)
        {
            _pendingQueue.Enqueue(notification);
            return;
        }

        AddToastCard(notification);
    }

    public void Dismiss()
    {
        _pendingQueue.Clear();
        _visibleToasts.Clear();
        UpdateTimerAndHitTest();
    }

    private void AddToastCard(ToastNotification notification)
    {
        var durationMs = Math.Max(1500, notification.DurationMs);
        var (glyph, accentBrush, badgeBgBrush) = ResolveTheme(notification.Type);

        var card = new ToastCardItem
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            ActionText = notification.ActionText,
            ActionCallback = notification.ActionCallback,
            CloseTooltip = Loc.GetString("Toast_Close"),
            IconGlyph = glyph,
            AccentBrush = accentBrush,
            BadgeBackground = badgeBgBrush,
            TotalDurationMs = durationMs,
            RemainingMs = durationMs,
            RemainingPercent = 100.0
        };

        _visibleToasts.Insert(0, card);
        UpdateTimerAndHitTest();
    }

    private void RemoveToastById(Guid id)
    {
        for (int i = _visibleToasts.Count - 1; i >= 0; i--)
        {
            if (_visibleToasts[i].Id == id)
            {
                _visibleToasts.RemoveAt(i);
                break;
            }
        }

        while (_visibleToasts.Count < MaxVisibleToasts && _pendingQueue.Count > 0)
        {
            AddToastCard(_pendingQueue.Dequeue());
        }

        UpdateTimerAndHitTest();
    }

    private void UpdateTimerAndHitTest()
    {
        bool hasToasts = _visibleToasts.Count > 0;
        IsHitTestVisible = hasToasts;

        if (hasToasts && !_tickTimer.IsEnabled)
        {
            _lastTickTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            _tickTimer.Start();
        }
        else if (!hasToasts && _tickTimer.IsEnabled)
        {
            _tickTimer.Stop();
        }
    }

    private void TickTimer_Tick(object? sender, object e)
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        double elapsedMs = (_lastTickTimestamp == 0)
            ? TickIntervalMs
            : Math.Clamp((now - _lastTickTimestamp) * 1000.0 / System.Diagnostics.Stopwatch.Frequency, 1.0, 100.0);
        _lastTickTimestamp = now;

        List<Guid>? expiredIds = null;

        foreach (var item in _visibleToasts)
        {
            if (item.IsPaused) continue;

            item.RemainingMs -= elapsedMs;
            if (item.RemainingMs <= 0)
            {
                item.RemainingPercent = 0;
                expiredIds ??= new List<Guid>();
                expiredIds.Add(item.Id);
            }
            else
            {
                item.RemainingPercent = item.RemainingMs / item.TotalDurationMs * 100.0;
            }
        }

        if (expiredIds != null)
        {
            foreach (var id in expiredIds)
            {
                RemoveToastById(id);
            }
        }
    }

    private (string Glyph, Brush? AccentBrush, Brush? BadgeBgBrush) ResolveTheme(ToastType type)
    {
        Brush? accentBrush;
        Brush? badgeBgBrush;
        string glyph;

        switch (type)
        {
            case ToastType.Success:
                glyph = "\uE73E";
                accentBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBrush");
                badgeBgBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBackgroundBrush");
                break;

            case ToastType.Error:
                glyph = "\uE783";
                accentBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
                badgeBgBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBackgroundBrush");
                break;

            case ToastType.Warning:
                glyph = "\uE7BA";
                accentBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCautionBrush");
                badgeBgBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCautionBackgroundBrush");
                break;

            case ToastType.Information:
            default:
                glyph = "\uE946";
                accentBrush = ThemeResourceHelper.GetThemeBrush(this, "AccentFillColorDefaultBrush");
                badgeBgBrush = ThemeResourceHelper.GetThemeBrush(this, "SubtleFillColorSecondaryBrush");
                break;
        }

        accentBrush ??= ThemeResourceHelper.GetThemeBrush(this, "AccentFillColorDefaultBrush");
        badgeBgBrush ??= ThemeResourceHelper.GetThemeBrush(this, "SubtleFillColorSecondaryBrush");
        return (glyph, accentBrush, badgeBgBrush);
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
        {
            var item = _visibleToasts.FirstOrDefault(t => t.Id == id);
            var callback = item?.ActionCallback;
            RemoveToastById(id);
            callback?.Invoke();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
        {
            RemoveToastById(id);
        }
    }

    private void ToastCard_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ToastCardItem item })
        {
            item.IsPaused = true;
        }
    }

    private void ToastCard_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ToastCardItem item })
        {
            item.IsPaused = false;
        }
    }
}
