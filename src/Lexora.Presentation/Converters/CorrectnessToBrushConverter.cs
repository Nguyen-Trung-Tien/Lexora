using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class CorrectnessToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        string brushKey;

        if (value is bool isCorrect)
        {
            brushKey = isCorrect ? "SystemFillColorSuccessBrush" : "SystemFillColorCriticalBrush";
        }
        else
        {
            brushKey = "CardStrokeColorDefaultBrush";
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
