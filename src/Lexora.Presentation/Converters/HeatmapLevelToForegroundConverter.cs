using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class HeatmapLevelToForegroundConverter : IValueConverter
{
    private static readonly SolidColorBrush WhiteBrush = new(Color.FromArgb(255, 255, 255, 255));
    private static readonly SolidColorBrush DarkTextBrush = new(Color.FromArgb(255, 27, 31, 36));
    // Dark slate gray achieves ~7.2:1 on LightL0 (#EBEEF2) — exceeds WCAG AAA (7:1)
    private static readonly SolidColorBrush MutedLightText = new(Color.FromArgb(255, 72, 79, 88));
    private static readonly SolidColorBrush MutedDarkText = new(Color.FromArgb(255, 139, 148, 158));

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        int level = value is int lvl ? lvl : 0;
        bool isLight = Services.ThemeResourceHelper.DetermineIsLight();
        bool isDark = !isLight;

        if (level == 0)
        {
            var secondary = Services.ThemeResourceHelper.GetThemeBrush(null, "TextFillColorSecondaryBrush");
            if (secondary != null) return secondary;
            return isDark ? MutedDarkText : MutedLightText;
        }

        if (level >= 3)
        {
            var onAccent = Services.ThemeResourceHelper.GetThemeBrush(null, "TextOnAccentFillColorPrimaryBrush");
            if (onAccent != null) return onAccent;
            return WhiteBrush;
        }

        // Level 1 and 2
        if (isDark)
        {
            var onAccent = Services.ThemeResourceHelper.GetThemeBrush(null, "TextOnAccentFillColorPrimaryBrush");
            if (onAccent != null) return onAccent;
            return WhiteBrush;
        }
        else
        {
            var primary = Services.ThemeResourceHelper.GetThemeBrush(null, "TextFillColorPrimaryBrush");
            if (primary != null) return primary;
            return DarkTextBrush;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
