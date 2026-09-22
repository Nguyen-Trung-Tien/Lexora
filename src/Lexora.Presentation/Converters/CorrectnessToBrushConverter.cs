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

        var rootElement = (XamlApplication.Current as App)?.MainWindow?.Content as Microsoft.UI.Xaml.FrameworkElement;
        var themeBrush = Services.ThemeResourceHelper.GetThemeBrush(rootElement, brushKey);
        if (themeBrush != null)
        {
            return themeBrush;
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
