using Microsoft.UI.Xaml.Data;

namespace Lexora.Presentation.Converters;

public class HeatmapLevelToForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        int level = value is int lvl ? lvl : 0;
        bool isLight = Services.ThemeResourceHelper.DetermineIsLight();

        if (level == 0)
        {
            return Services.ThemeResourceHelper.GetThemeBrush(null, "TextFillColorSecondaryBrush");
        }

        if (level >= 3 || !isLight)
        {
            return Services.ThemeResourceHelper.GetThemeBrush(null, "TextOnAccentFillColorPrimaryBrush");
        }

        // Level 1 and 2 in Light mode (pastel green backgrounds) require dark primary text
        return Services.ThemeResourceHelper.GetThemeBrush(null, "TextFillColorPrimaryBrush");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
