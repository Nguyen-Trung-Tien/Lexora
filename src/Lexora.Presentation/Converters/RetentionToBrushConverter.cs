using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class RetentionToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        double percentage = 0.0;
        if (value is double d) percentage = d;
        else if (value is int i) percentage = i;
        else if (value is float f) percentage = f;
        else if (value != null && double.TryParse(value.ToString(), out var parsed)) percentage = parsed;

        string brushKey = percentage switch
        {
            >= 85.0 => "SystemFillColorSuccessBrush",
            >= 60.0 => "AccentTextFillColorPrimaryBrush",
            >= 40.0 => "SystemFillColorCautionBrush",
            _ => "SystemFillColorCriticalBrush"
        };

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
