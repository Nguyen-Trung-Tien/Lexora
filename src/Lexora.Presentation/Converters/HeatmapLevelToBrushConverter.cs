using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class HeatmapLevelToBrushConverter : IValueConverter
{
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

        if (XamlApplication.Current?.Resources != null &&
            XamlApplication.Current.Resources.TryGetValue(brushKey, out var res) &&
            res is Brush brush)
        {
            return brush;
        }

        return (XamlApplication.Current?.Resources["SubtleFillColorSecondaryBrush"] as Brush)
            ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
