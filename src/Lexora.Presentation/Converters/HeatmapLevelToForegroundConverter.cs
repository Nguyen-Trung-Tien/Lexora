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
    // Neutral gray achieves ~4.6:1 on LightL0 (#EBEEF2) — passes WCAG AA
    private static readonly SolidColorBrush MutedLightText = new(Color.FromArgb(255, 100, 100, 100));
    private static readonly SolidColorBrush MutedDarkText = new(Color.FromArgb(255, 139, 148, 158));

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        int level = value is int lvl ? lvl : 0;

        bool isDark = false;
        if (XamlApplication.Current != null)
        {
            if (XamlApplication.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                isDark = true;
            }
            else if (XamlApplication.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out var cardBg) && cardBg is SolidColorBrush scb)
            {
                isDark = (scb.Color.R * 0.299 + scb.Color.G * 0.587 + scb.Color.B * 0.114) < 128;
            }
        }

        if (level == 0)
        {
            return isDark ? MutedDarkText : MutedLightText;
        }

        if (level >= 3)
        {
            return WhiteBrush;
        }

        // Level 1 and 2
        return isDark ? WhiteBrush : DarkTextBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
