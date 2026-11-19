using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Lexora.Presentation.Helpers;

/// <summary>
/// Attached property helper that automatically synchronizes FlyoutBase and TeachingTip
/// RequestedTheme with the active application theme before opening, preventing Light/Dark
/// theme mismatch glitches on popups and context menus.
/// </summary>
public static class ThemeSyncHelper
{
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
            tip.Loading -= TeachingTip_Loading;
            if (enabled)
            {
                tip.Loading += TeachingTip_Loading;
                ApplyCurrentTheme(tip);
            }
        }
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
        ApplyCurrentTheme(sender);
    }

    public static void ApplyCurrentTheme(FrameworkElement element)
    {
        element.RequestedTheme = ResolveCurrentTheme(element);
    }

    public static ElementTheme ResolveCurrentTheme(FrameworkElement? reference = null)
    {
        var themeService = App.Current?.Services?.GetService<Services.IThemeService>();
        if (themeService != null && themeService.CurrentTheme != ElementTheme.Default)
        {
            return themeService.CurrentTheme;
        }

        if (reference != null && reference.ActualTheme != ElementTheme.Default)
        {
            return reference.ActualTheme;
        }

        if (App.Current?.MainWindow?.Content is FrameworkElement root && root.ActualTheme != ElementTheme.Default)
        {
            return root.ActualTheme;
        }

        return ElementTheme.Default;
    }
}
