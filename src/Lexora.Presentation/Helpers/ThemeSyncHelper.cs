using System.Runtime.CompilerServices;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Lexora.Presentation.Helpers;

/// <summary>
/// Attached property helper that automatically synchronizes FlyoutBase and TeachingTip
/// RequestedTheme with the active application theme before opening, preventing Light/Dark
/// theme mismatch glitches on popups and context menus.
/// </summary>
public static class ThemeSyncHelper
{
    private sealed class TeachingTipSubscription
    {
        public long? IsOpenCallbackToken { get; set; }
        public EventHandler<ElementTheme>? ThemeChangedHandler { get; set; }
    }

    private static readonly ConditionalWeakTable<TeachingTip, TeachingTipSubscription> _tipSubscriptions = new();

    public static readonly DependencyProperty SyncThemeProperty =
        DependencyProperty.RegisterAttached(
            "SyncTheme",
            typeof(bool),
            typeof(ThemeSyncHelper),
            new PropertyMetadata(false, OnSyncThemeChanged));

    public static bool GetSyncTheme(DependencyObject obj) => (bool)obj.GetValue(SyncThemeProperty);
    public static void SetSyncTheme(DependencyObject obj, bool value) => obj.SetValue(SyncThemeProperty, value);

    private static void OnSyncThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool enabled) return;

        if (d is FlyoutBase flyout)
        {
            flyout.Opening -= Flyout_Opening;
            if (enabled)
            {
                flyout.Opening += Flyout_Opening;
            }
        }
        else if (d is TeachingTip tip)
        {
            CleanupTeachingTip(tip);
            if (enabled)
            {
                tip.Loading += TeachingTip_Loading;
                tip.Loaded += TeachingTip_Loaded;
                tip.Unloaded += TeachingTip_Unloaded;

                var sub = _tipSubscriptions.GetOrCreateValue(tip);
                sub.IsOpenCallbackToken = tip.RegisterPropertyChangedCallback(TeachingTip.IsOpenProperty, OnTeachingTipIsOpenChanged);

                if (tip.IsLoaded)
                {
                    SubscribeThemeChanged(tip, sub);
                }

                SyncTeachingTipTheme(tip);
            }
        }
    }

    private static void CleanupTeachingTip(TeachingTip tip)
    {
        tip.Loading -= TeachingTip_Loading;
        tip.Loaded -= TeachingTip_Loaded;
        tip.Unloaded -= TeachingTip_Unloaded;

        if (_tipSubscriptions.TryGetValue(tip, out var sub))
        {
            if (sub.IsOpenCallbackToken is { } token)
            {
                tip.UnregisterPropertyChangedCallback(TeachingTip.IsOpenProperty, token);
                sub.IsOpenCallbackToken = null;
            }

            UnsubscribeThemeChanged(sub);
            _tipSubscriptions.Remove(tip);
        }
    }

    private static void SubscribeThemeChanged(TeachingTip tip, TeachingTipSubscription sub)
    {
        if (sub.ThemeChangedHandler != null) return;

        var themeService = App.Current?.Services?.GetService<IThemeService>();
        if (themeService == null) return;

        var weakTip = new WeakReference<TeachingTip>(tip);
        sub.ThemeChangedHandler = (_, _) =>
        {
            if (weakTip.TryGetTarget(out var target))
            {
                SyncTeachingTipTheme(target);
            }
        };
        themeService.ThemeChanged += sub.ThemeChangedHandler;
    }

    private static void UnsubscribeThemeChanged(TeachingTipSubscription sub)
    {
        if (sub.ThemeChangedHandler == null) return;

        var themeService = App.Current?.Services?.GetService<IThemeService>();
        if (themeService != null)
        {
            themeService.ThemeChanged -= sub.ThemeChangedHandler;
        }
        sub.ThemeChangedHandler = null;
    }

    private static void Flyout_Opening(object? sender, object e)
    {
        if (sender is MenuFlyout menuFlyout)
        {
            var theme = ResolveCurrentTheme(menuFlyout.Target as FrameworkElement);
            var presenterStyle = new Style(typeof(MenuFlyoutPresenter));
            if (Microsoft.UI.Xaml.Application.Current?.Resources.TryGetValue("LexoraMenuFlyoutPresenterStyle", out var baseStyle) == true
                && baseStyle is Style style)
            {
                presenterStyle.BasedOn = style;
            }
            presenterStyle.Setters.Add(new Setter(FrameworkElement.RequestedThemeProperty, theme));
            menuFlyout.MenuFlyoutPresenterStyle = presenterStyle;
        }
        else if (sender is Flyout flyout && flyout.Content is FrameworkElement content)
        {
            content.RequestedTheme = ResolveCurrentTheme(flyout.Target as FrameworkElement);
        }
    }

    private static void TeachingTip_Loading(FrameworkElement sender, object args)
    {
        if (sender is TeachingTip tip)
        {
            SyncTeachingTipTheme(tip);
        }
        else
        {
            ApplyCurrentTheme(sender);
        }
    }

    private static void TeachingTip_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TeachingTip tip)
        {
            var sub = _tipSubscriptions.GetOrCreateValue(tip);
            SubscribeThemeChanged(tip, sub);
            SyncTeachingTipTheme(tip);
        }
    }

    private static void TeachingTip_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is TeachingTip tip && _tipSubscriptions.TryGetValue(tip, out var sub))
        {
            UnsubscribeThemeChanged(sub);
        }
    }

    private static void OnTeachingTipIsOpenChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (sender is TeachingTip tip && tip.IsOpen)
        {
            SyncTeachingTipTheme(tip);
        }
    }

    private static void SyncTeachingTipTheme(TeachingTip tip)
    {
        var theme = ResolveCurrentTheme(tip);
        tip.RequestedTheme = theme;

        if (tip.Content is FrameworkElement contentFe)
        {
            contentFe.RequestedTheme = theme;
        }

        if (tip.IsOpen && tip.XamlRoot is { } xamlRoot)
        {
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot))
            {
                popup.RequestedTheme = theme;
                if (popup.Child is FrameworkElement child)
                {
                    child.RequestedTheme = theme;
                }
            }
        }
    }

    public static void ApplyCurrentTheme(FrameworkElement element)
    {
        element.RequestedTheme = ResolveCurrentTheme(element);
    }

    public static ElementTheme ResolveCurrentTheme(FrameworkElement? reference = null)
    {
        var themeService = App.Current?.Services?.GetService<IThemeService>();
        if (themeService != null && themeService.CurrentTheme != ElementTheme.Default)
        {
            return themeService.CurrentTheme;
        }

        if (App.Current?.MainWindow?.Content is FrameworkElement root && root.ActualTheme != ElementTheme.Default)
        {
            return root.ActualTheme;
        }

        if (reference != null && reference.ActualTheme != ElementTheme.Default)
        {
            return reference.ActualTheme;
        }

        return ElementTheme.Default;
    }
}
