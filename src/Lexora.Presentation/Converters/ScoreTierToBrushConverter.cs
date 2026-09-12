using Lexora.Application.DTOs;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class ScoreTierToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        string brushKey = "AccentFillColorDefaultBrush";

        if (value is TestScoreTier tier)
        {
            brushKey = tier switch
            {
                TestScoreTier.Excellent => "SystemFillColorSuccessBrush",
                TestScoreTier.Good => "AccentFillColorDefaultBrush",
                TestScoreTier.NeedsPractice => "SystemFillColorCriticalBrush",
                _ => "AccentFillColorDefaultBrush"
            };
        }
        else if (value is double score)
        {
            brushKey = score switch
            {
                >= 85.0 => "SystemFillColorSuccessBrush",
                >= 60.0 => "AccentFillColorDefaultBrush",
                _ => "SystemFillColorCriticalBrush"
            };
        }

        if (XamlApplication.Current?.Resources != null &&
            XamlApplication.Current.Resources.TryGetValue(brushKey, out var res) &&
            res is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
