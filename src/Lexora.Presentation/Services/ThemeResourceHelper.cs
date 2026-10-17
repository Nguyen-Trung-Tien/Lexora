using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Lexora.Presentation.Services;

public static class ThemeResourceHelper
{
    // High-contrast, theme-accurate fallback brushes to guarantee readability
    private static readonly Brush LightPrimaryText = new SolidColorBrush(Color.FromArgb(255, 24, 24, 24));
    private static readonly Brush LightSecondaryText = new SolidColorBrush(Color.FromArgb(255, 92, 92, 92));
    private static readonly Brush LightAccentText = new SolidColorBrush(Color.FromArgb(255, 0, 95, 184)); // High-contrast Windows 11 Royal Blue
    private static readonly Brush LightCardBackground = new SolidColorBrush(Colors.White);
    private static readonly Brush LightCardStroke = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
    private static readonly Brush LightSubtleBackground = new SolidColorBrush(Color.FromArgb(14, 0, 0, 0));
    // Semantic background fills — light mode: very light tinted fills (WinUI 3 system values)
    private static readonly Brush LightSuccessBackground = new SolidColorBrush(Color.FromArgb(255, 223, 246, 221)); // #DFF6DD
    private static readonly Brush LightCriticalBackground = new SolidColorBrush(Color.FromArgb(255, 253, 231, 233)); // #FDE7E9
    private static readonly Brush LightCautionBackground = new SolidColorBrush(Color.FromArgb(255, 255, 244, 206)); // #FFF4CE

    private static readonly Brush DarkPrimaryText = new SolidColorBrush(Colors.White);
    private static readonly Brush DarkSecondaryText = new SolidColorBrush(Color.FromArgb(255, 197, 197, 197));
    private static readonly Brush DarkAccentText = new SolidColorBrush(Color.FromArgb(255, 96, 205, 255)); // Fluent Cyan
    private static readonly Brush DarkCardBackground = new SolidColorBrush(Color.FromArgb(255, 43, 43, 43));
    private static readonly Brush DarkCardStroke = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
    private static readonly Brush DarkSubtleBackground = new SolidColorBrush(Color.FromArgb(16, 255, 255, 255));
    // Semantic background fills — dark mode: dark muted tinted fills (WinUI 3 dark theme values)
    private static readonly Brush DarkSuccessBackground = new SolidColorBrush(Color.FromArgb(255, 57, 61, 27));  // #393D1B olive-green
    private static readonly Brush DarkCriticalBackground = new SolidColorBrush(Color.FromArgb(255, 68, 39, 38)); // #442726 dark-red
    private static readonly Brush DarkCautionBackground = new SolidColorBrush(Color.FromArgb(255, 67, 53, 25));  // #433519 dark-amber

    public static bool DetermineIsLight(FrameworkElement? element = null)
    {
        // 1. Explicit theme preference from IThemeService
        try
        {
            var themeService = App.Current?.Services?.GetService<IThemeService>();
            if (themeService != null && themeService.CurrentTheme != ElementTheme.Default)
            {
                return themeService.CurrentTheme == ElementTheme.Light;
            }
        }
        catch { }

        // 2. Element's own ActualTheme (if not Default)
        if (element != null && element.ActualTheme != ElementTheme.Default)
        {
            return element.ActualTheme == ElementTheme.Light;
        }

        // 3. Element's XamlRoot content ActualTheme
        if (element?.XamlRoot?.Content is FrameworkElement xamlRootElement && xamlRootElement.ActualTheme != ElementTheme.Default)
        {
            return xamlRootElement.ActualTheme == ElementTheme.Light;
        }

        // 4. MainWindow root content ActualTheme
        try
        {
            if (App.Current?.MainWindow?.Content is FrameworkElement root && root.ActualTheme != ElementTheme.Default)
            {
                return root.ActualTheme == ElementTheme.Light;
            }
        }
        catch { }

        // 5. Query Windows system theme via UISettings (Background color luminosity)
        // In Windows Light mode: Background is pure White (255, 255, 255) -> sum = 765 > 384
        // In Windows Dark mode: Background is pure Black (0, 0, 0) -> sum = 0 < 384
        try
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            var bg = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
            return (bg.R + bg.G + bg.B) > 384;
        }
        catch { }

        // 6. Query Windows Registry for AppsUseLightTheme
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int val)
            {
                return val != 0;
            }
        }
        catch { }

        // 7. Ultimate fallback
        return App.Current?.RequestedTheme == ApplicationTheme.Light;
    }

    public static Brush GetThemeBrush(string key) => GetThemeBrush(null, key);

    public static Brush GetThemeBrush(FrameworkElement? element, string key)
    {
        bool isLight = DetermineIsLight(element);

        // Keys in WinUI 3 / XamlControlsResources: "Light" vs "Default" (Dark)
        // Keys in custom DesignSystem.xaml: "Light" vs "Dark"
        string[] targetThemes = isLight ? ["Light"] : ["Dark", "Default"];

        // 1. Search merged dictionaries (contains XamlControlsResources with system Light/Default dictionaries)
        if (Microsoft.UI.Xaml.Application.Current?.Resources?.MergedDictionaries != null)
        {
            foreach (var md in Microsoft.UI.Xaml.Application.Current.Resources.MergedDictionaries)
            {
                foreach (var th in targetThemes)
                {
                    if (md.ThemeDictionaries.TryGetValue(th, out var tdObj) &&
                        tdObj is ResourceDictionary td &&
                        td.TryGetValue(key, out var bObj) &&
                        bObj is Brush b)
                    {
                        // Guard against dark accent brush (#60CDFF) leaking into light mode
                        if (isLight && key == "AccentTextFillColorPrimaryBrush" && b is SolidColorBrush scbAccent && scbAccent.Color.R > 50 && scbAccent.Color.G > 180 && scbAccent.Color.B > 240)
                        {
                            return LightAccentText;
                        }

                        // Guard against white/light primary text leaking into light mode
                        if (isLight && key == "TextFillColorPrimaryBrush" && b is SolidColorBrush scbPrim && scbPrim.Color.R > 200 && scbPrim.Color.G > 200 && scbPrim.Color.B > 200)
                        {
                            return LightPrimaryText;
                        }

                        // Guard against faint secondary text leaking into light mode
                        if (isLight && key == "TextFillColorSecondaryBrush" && b is SolidColorBrush scbSec && scbSec.Color.R > 180 && scbSec.Color.G > 180 && scbSec.Color.B > 180)
                        {
                            return LightSecondaryText;
                        }

                        return b;
                    }
                }
            }
        }

        // 2. Search root ThemeDictionaries
        if (Microsoft.UI.Xaml.Application.Current?.Resources?.ThemeDictionaries != null)
        {
            foreach (var th in targetThemes)
            {
                if (Microsoft.UI.Xaml.Application.Current.Resources.ThemeDictionaries.TryGetValue(th, out var rootTdObj) &&
                    rootTdObj is ResourceDictionary rootTd &&
                    rootTd.TryGetValue(key, out var rbObj) &&
                    rbObj is Brush rb)
                {
                    if (isLight && key == "AccentTextFillColorPrimaryBrush" && rb is SolidColorBrush scbAccent && scbAccent.Color.R > 50 && scbAccent.Color.G > 180 && scbAccent.Color.B > 240)
                    {
                        return LightAccentText;
                    }
                    if (isLight && key == "TextFillColorPrimaryBrush" && rb is SolidColorBrush scbPrim && scbPrim.Color.R > 200 && scbPrim.Color.G > 200 && scbPrim.Color.B > 200)
                    {
                        return LightPrimaryText;
                    }
                    if (isLight && key == "TextFillColorSecondaryBrush" && rb is SolidColorBrush scbSec && scbSec.Color.R > 180 && scbSec.Color.G > 180 && scbSec.Color.B > 180)
                    {
                        return LightSecondaryText;
                    }
                    return rb;
                }
            }
        }

        // 3. Fallback to bulletproof theme-appropriate colors
        return key switch
        {
            "TextFillColorPrimaryBrush" => isLight ? LightPrimaryText : DarkPrimaryText,
            "TextFillColorSecondaryBrush" => isLight ? LightSecondaryText : DarkSecondaryText,
            "AccentTextFillColorPrimaryBrush" => isLight ? LightAccentText : DarkAccentText,
            "CardBackgroundFillColorDefaultBrush" => isLight ? LightCardBackground : DarkCardBackground,
            "CardStrokeColorDefaultBrush" => isLight ? LightCardStroke : DarkCardStroke,
            "SubtleFillColorSecondaryBrush" => isLight ? LightSubtleBackground : DarkSubtleBackground,
            "AccentFillColorDefaultBrush" => isLight ? LightAccentText : DarkAccentText,
            "TextOnAccentFillColorPrimaryBrush" => new SolidColorBrush(Colors.White),
            // Semantic state solid fills (High contrast AA standard)
            "SystemFillColorSuccessBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 16, 124, 65)) : new SolidColorBrush(Color.FromArgb(255, 108, 203, 95)),
            "SystemFillColorCriticalBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 196, 43, 28)) : new SolidColorBrush(Color.FromArgb(255, 255, 153, 164)),
            "SystemFillColorCautionBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 157, 93, 0)) : new SolidColorBrush(Color.FromArgb(255, 255, 213, 128)),
            // Semantic state background fills
            "SystemFillColorSuccessBackgroundBrush" => isLight ? LightSuccessBackground : DarkSuccessBackground,
            "SystemFillColorCriticalBackgroundBrush" => isLight ? LightCriticalBackground : DarkCriticalBackground,
            "SystemFillColorCautionBackgroundBrush" => isLight ? LightCautionBackground : DarkCautionBackground,
            "LexoraModalBackgroundBrush" => isLight ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
            "LexoraModalBorderBrush" => isLight ? LightCardStroke : DarkCardStroke,
            _ => isLight ? LightCardBackground : DarkCardBackground
        };
    }
}
