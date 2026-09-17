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

    private static readonly Brush DarkPrimaryText = new SolidColorBrush(Colors.White);
    private static readonly Brush DarkSecondaryText = new SolidColorBrush(Color.FromArgb(255, 197, 197, 197));
    private static readonly Brush DarkAccentText = new SolidColorBrush(Color.FromArgb(255, 96, 205, 255)); // Fluent Cyan
    private static readonly Brush DarkCardBackground = new SolidColorBrush(Color.FromArgb(255, 43, 43, 43));
    private static readonly Brush DarkCardStroke = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
    private static readonly Brush DarkSubtleBackground = new SolidColorBrush(Color.FromArgb(16, 255, 255, 255));

    public static Brush GetThemeBrush(FrameworkElement? element, string key)
    {
        bool isLight = false;
        if (element != null && element.ActualTheme != ElementTheme.Default)
        {
            isLight = element.ActualTheme == ElementTheme.Light;
        }
        else if (App.Current?.MainWindow?.Content is FrameworkElement root && root.ActualTheme != ElementTheme.Default)
        {
            isLight = root.ActualTheme == ElementTheme.Light;
        }
        else
        {
            isLight = (App.Current?.RequestedTheme == ApplicationTheme.Light);
        }

        string themeKey = isLight ? "Light" : "Default";

        // 1. Search merged dictionaries (contains XamlControlsResources with system Light/Default dictionaries)
        if (Microsoft.UI.Xaml.Application.Current?.Resources?.MergedDictionaries != null)
        {
            foreach (var md in Microsoft.UI.Xaml.Application.Current.Resources.MergedDictionaries)
            {
                if (md.ThemeDictionaries.TryGetValue(themeKey, out var tdObj) &&
                    tdObj is ResourceDictionary td &&
                    td.TryGetValue(key, out var bObj) &&
                    bObj is Brush b)
                {
                    // Guard against dark accent brush (#60CDFF) leaking into light mode
                    if (isLight && key == "AccentTextFillColorPrimaryBrush" && b is SolidColorBrush scb && scb.Color.R > 50 && scb.Color.G > 180 && scb.Color.B > 240)
                    {
                        return LightAccentText;
                    }
                    return b;
                }
            }
        }

        // 2. Search root ThemeDictionaries
        if (Microsoft.UI.Xaml.Application.Current?.Resources?.ThemeDictionaries != null)
        {
            if (Microsoft.UI.Xaml.Application.Current.Resources.ThemeDictionaries.TryGetValue(themeKey, out var rootTdObj) &&
                rootTdObj is ResourceDictionary rootTd &&
                rootTd.TryGetValue(key, out var rbObj) &&
                rbObj is Brush rb)
            {
                return rb;
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
            _ => isLight ? LightPrimaryText : DarkPrimaryText
        };
    }
}
