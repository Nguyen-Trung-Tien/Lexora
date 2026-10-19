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
    private static readonly Brush LightSecondaryText = new SolidColorBrush(Color.FromArgb(255, 71, 84, 103)); // #475467
    private static readonly Brush LightTertiaryText = new SolidColorBrush(Color.FromArgb(255, 102, 112, 133)); // #667085 (WCAG AA 4.6:1 on white)
    private static readonly Brush LightAccentText = new SolidColorBrush(Color.FromArgb(255, 0, 82, 163)); // #0052A3 High-contrast Royal Blue
    private static readonly Brush LightCardBackground = new SolidColorBrush(Colors.White);
    private static readonly Brush LightCardStroke = new SolidColorBrush(Color.FromArgb(33, 0, 0, 0)); // Opacity ~0.13
    private static readonly Brush LightSubtleBackground = new SolidColorBrush(Color.FromArgb(255, 238, 242, 246)); // #EEF2F6 Crisp pill/chip surface
    // Semantic background fills — light mode: very light tinted fills (WinUI 3 system values)
    private static readonly Brush LightSuccessBackground = new SolidColorBrush(Color.FromArgb(255, 223, 246, 221)); // #DFF6DD
    private static readonly Brush LightCriticalBackground = new SolidColorBrush(Color.FromArgb(255, 253, 231, 233)); // #FDE7E9
    private static readonly Brush LightCautionBackground = new SolidColorBrush(Color.FromArgb(255, 255, 244, 206)); // #FFF4CE

    private static readonly Brush DarkPrimaryText = new SolidColorBrush(Colors.White);
    private static readonly Brush DarkSecondaryText = new SolidColorBrush(Color.FromArgb(255, 197, 197, 197));
    private static readonly Brush DarkTertiaryText = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160));
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

        // Keys in WinUI 3: "Light" vs "Default" (standard Dark) / "Dark"
        string[] targetThemes = isLight ? ["Light"] : ["Default", "Dark"];

        // 1. Search merged dictionaries in REVERSE order so DesignSystem.xaml takes precedence over XamlControlsResources
        if (Microsoft.UI.Xaml.Application.Current?.Resources?.MergedDictionaries != null)
        {
            var mds = Microsoft.UI.Xaml.Application.Current.Resources.MergedDictionaries;
            for (int i = mds.Count - 1; i >= 0; i--)
            {
                var md = mds[i];
                foreach (var th in targetThemes)
                {
                    if (md.ThemeDictionaries.TryGetValue(th, out var tdObj) &&
                        tdObj is ResourceDictionary td &&
                        td.TryGetValue(key, out var bObj) &&
                        bObj is Brush b)
                    {
                        return SanitizeBrushForTheme(b, key, isLight);
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
                    return SanitizeBrushForTheme(rb, key, isLight);
                }
            }
        }

        // 3. Fallback to bulletproof theme-appropriate colors
        return GetFallbackThemeBrush(key, isLight);
    }

    private static Brush SanitizeBrushForTheme(Brush b, string key, bool isLight)
    {
        if (isLight)
        {
            // Guard against dark accent brush (#60CDFF) leaking into light mode
            if (key == "AccentTextFillColorPrimaryBrush" && b is SolidColorBrush scbAccent && scbAccent.Color.R > 50 && scbAccent.Color.G > 180 && scbAccent.Color.B > 240)
            {
                return LightAccentText;
            }

            // Guard against white/light primary text leaking into light mode
            if (key == "TextFillColorPrimaryBrush" && b is SolidColorBrush scbPrim && scbPrim.Color.R > 200 && scbPrim.Color.G > 200 && scbPrim.Color.B > 200)
            {
                return LightPrimaryText;
            }

            // Guard against faint secondary text leaking into light mode
            if (key == "TextFillColorSecondaryBrush" && b is SolidColorBrush scbSec && scbSec.Color.R > 180 && scbSec.Color.G > 180 && scbSec.Color.B > 180)
            {
                return LightSecondaryText;
            }

            // Guard against faint tertiary text leaking into light mode
            if (key == "TextFillColorTertiaryBrush" && b is SolidColorBrush scbTer && (scbTer.Color.R > 140 || (scbTer.Color.R == scbTer.Color.G && scbTer.Color.G == scbTer.Color.B && scbTer.Color.R > 125)))
            {
                return LightTertiaryText;
            }

            // Guard against dark card background leaking into light mode
            if (key == "CardBackgroundFillColorDefaultBrush" && b is SolidColorBrush scbCard && scbCard.Color.R < 100 && scbCard.Color.G < 100 && scbCard.Color.B < 100)
            {
                return LightCardBackground;
            }
        }
        else
        {
            // DARK MODE GUARDS: Prevent light brushes from leaking into dark mode!
            // Guard against deep royal blue accent text (#0052A3) leaking into dark mode (unreadable on dark surfaces)
            if (key == "AccentTextFillColorPrimaryBrush" && b is SolidColorBrush scbDarkAccent && scbDarkAccent.Color.R < 60 && scbDarkAccent.Color.G < 120 && scbDarkAccent.Color.B > 140)
            {
                return DarkAccentText;
            }

            // Guard against dark primary text leaking into dark mode
            if (key == "TextFillColorPrimaryBrush" && b is SolidColorBrush scbDarkPrim && scbDarkPrim.Color.R < 80 && scbDarkPrim.Color.G < 80 && scbDarkPrim.Color.B < 80)
            {
                return DarkPrimaryText;
            }

            // Guard against dark secondary text leaking into dark mode
            if (key == "TextFillColorSecondaryBrush" && b is SolidColorBrush scbDarkSec && scbDarkSec.Color.R < 100 && scbDarkSec.Color.G < 100 && scbDarkSec.Color.B < 100)
            {
                return DarkSecondaryText;
            }

            // Guard against dark tertiary text leaking into dark mode
            if (key == "TextFillColorTertiaryBrush" && b is SolidColorBrush scbDarkTer && scbDarkTer.Color.R < 120 && scbDarkTer.Color.G < 120 && scbDarkTer.Color.B < 120)
            {
                return DarkTertiaryText;
            }

            // Guard against pure white card background leaking into dark mode
            if (key == "CardBackgroundFillColorDefaultBrush" && b is SolidColorBrush scbDarkCard && scbDarkCard.Color.R > 230 && scbDarkCard.Color.G > 230 && scbDarkCard.Color.B > 230)
            {
                return DarkCardBackground;
            }

            // Guard against light gray subtle background (#EEF2F6) leaking into dark mode
            if (key == "SubtleFillColorSecondaryBrush" && b is SolidColorBrush scbDarkSubtle && scbDarkSubtle.Color.A > 200 && scbDarkSubtle.Color.R > 200)
            {
                return DarkSubtleBackground;
            }

            // Guard against white modal/toast leaking into dark mode
            if ((key == "LexoraModalBackgroundBrush" || key == "LexoraToastBackgroundBrush") && b is SolidColorBrush scbDarkModal && scbDarkModal.Color.R > 200)
            {
                return DarkCardBackground;
            }
        }

        return b;
    }

    private static Brush GetFallbackThemeBrush(string key, bool isLight)
    {
        return key switch
        {
            "TextFillColorPrimaryBrush" => isLight ? LightPrimaryText : DarkPrimaryText,
            "TextFillColorSecondaryBrush" => isLight ? LightSecondaryText : DarkSecondaryText,
            "TextFillColorTertiaryBrush" => isLight ? LightTertiaryText : DarkTertiaryText,
            "AccentTextFillColorPrimaryBrush" => isLight ? LightAccentText : DarkAccentText,
            "CardBackgroundFillColorDefaultBrush" => isLight ? LightCardBackground : DarkCardBackground,
            "CardStrokeColorDefaultBrush" => isLight ? LightCardStroke : DarkCardStroke,
            "SubtleFillColorSecondaryBrush" => isLight ? LightSubtleBackground : DarkSubtleBackground,
            "AccentFillColorDefaultBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 0, 102, 204)) : DarkAccentText,
            "TextOnAccentFillColorPrimaryBrush" => new SolidColorBrush(Colors.White),
            "LexoraThemeCardSelectedBorderBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 0, 102, 204)) : DarkAccentText,
            // Semantic state solid fills (High contrast AA standard)
            "SystemFillColorSuccessBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 8, 116, 67)) : new SolidColorBrush(Color.FromArgb(255, 108, 203, 95)),
            "SystemFillColorCriticalBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 217, 45, 32)) : new SolidColorBrush(Color.FromArgb(255, 255, 153, 164)),
            "SystemFillColorCautionBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 181, 71, 8)) : new SolidColorBrush(Color.FromArgb(255, 255, 213, 128)),
            // Semantic state background fills
            "SystemFillColorSuccessBackgroundBrush" => isLight ? LightSuccessBackground : DarkSuccessBackground,
            "SystemFillColorCriticalBackgroundBrush" => isLight ? LightCriticalBackground : DarkCriticalBackground,
            "SystemFillColorCautionBackgroundBrush" => isLight ? LightCautionBackground : DarkCautionBackground,
            "LexoraModalBackgroundBrush" => isLight ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
            "LexoraModalBorderBrush" => isLight ? LightCardStroke : DarkCardStroke,
            "LexoraToastBackgroundBrush" => isLight ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
            "LexoraToastBorderBrush" => isLight ? LightCardStroke : DarkCardStroke,
            "LexoraFormulaCardBackgroundBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 240, 246, 255)) : new SolidColorBrush(Color.FromArgb(255, 10, 25, 47)),
            "LexoraOptionSelectedBackgroundBrush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 235, 243, 252)) : new SolidColorBrush(Color.FromArgb(255, 12, 37, 72)),
            "HeatmapLevel0Brush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 225, 223, 221)) : new SolidColorBrush(Color.FromArgb(255, 45, 51, 59)),
            "HeatmapLevel1Brush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 155, 233, 168)) : new SolidColorBrush(Color.FromArgb(255, 14, 68, 41)),
            "HeatmapLevel2Brush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 64, 196, 99)) : new SolidColorBrush(Color.FromArgb(255, 0, 109, 50)),
            "HeatmapLevel3Brush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 48, 161, 78)) : new SolidColorBrush(Color.FromArgb(255, 38, 166, 65)),
            "HeatmapLevel4Brush" => isLight ? new SolidColorBrush(Color.FromArgb(255, 33, 110, 57)) : new SolidColorBrush(Color.FromArgb(255, 57, 211, 83)),
            _ => isLight ? LightCardBackground : DarkCardBackground
        };
    }
}
