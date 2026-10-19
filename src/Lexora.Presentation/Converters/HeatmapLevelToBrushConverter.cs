using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class HeatmapLevelToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush LightL0 = new(Color.FromArgb(255, 235, 238, 242));
    private static readonly SolidColorBrush LightL1 = new(Color.FromArgb(255, 155, 233, 168));
    private static readonly SolidColorBrush LightL2 = new(Color.FromArgb(255, 64, 196, 99));
    private static readonly SolidColorBrush LightL3 = new(Color.FromArgb(255, 48, 161, 78));
    private static readonly SolidColorBrush LightL4 = new(Color.FromArgb(255, 33, 110, 57));

    private static readonly SolidColorBrush DarkL0 = new(Color.FromArgb(255, 38, 44, 54));
    private static readonly SolidColorBrush DarkL1 = new(Color.FromArgb(255, 14, 68, 41));
    private static readonly SolidColorBrush DarkL2 = new(Color.FromArgb(255, 0, 109, 50));
    private static readonly SolidColorBrush DarkL3 = new(Color.FromArgb(255, 38, 166, 65));
    private static readonly SolidColorBrush DarkL4 = new(Color.FromArgb(255, 57, 211, 83));

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        int level = value is int lvl ? lvl : 0;
        string brushKey = level switch
        {
            1 => "HeatmapLevel1Brush",
            2 => "HeatmapLevel2Brush",
            3 => "HeatmapLevel3Brush",
            4 => "HeatmapLevel4Brush",
            _ => "HeatmapLevel0Brush"
        };

        bool isLight = Services.ThemeResourceHelper.DetermineIsLight();
        bool isDark = !isLight;

        var themeBrush = Services.ThemeResourceHelper.GetThemeBrush(brushKey);
        if (themeBrush != null)
        {
            return themeBrush;
        }

        // High-fidelity fallback
        if (isDark)
        {
            return level switch
            {
                1 => DarkL1,
                2 => DarkL2,
                3 => DarkL3,
                4 => DarkL4,
                _ => DarkL0
            };
        }

        return level switch
        {
            1 => LightL1,
            2 => LightL2,
            3 => LightL3,
            4 => LightL4,
            _ => LightL0
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
