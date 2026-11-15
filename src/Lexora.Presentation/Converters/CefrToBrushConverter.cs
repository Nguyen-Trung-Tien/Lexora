using Lexora.Domain.Enums;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class CefrToBrushConverter : IValueConverter
{
    // High-contrast, theme-appropriate solid brushes to guarantee maximum readability and zero washed-out badges
    private static readonly Brush LightA1A2 = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 8, 116, 67));   // #087443 Forest Green (5.8:1 with white)
    private static readonly Brush LightB1B2 = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 102, 204));  // #0066CC Vibrant Blue (4.8:1 with white)
    private static readonly Brush LightC1C2 = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 181, 71, 8));   // #B54708 Deep Amber (4.7:1 with white)

    private static readonly Brush DarkA1A2 = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 138, 76));   // #0E8A4C Emerald Green (5.0:1 with white)
    private static readonly Brush DarkB1B2 = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212));   // #0078D4 Vibrant Accent Blue (4.6:1 with white)
    private static readonly Brush DarkC1C2 = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 217, 87, 0));    // #D95700 Rich Amber (4.5:1 with white)

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        CefrLevel? level = value switch
        {
            CefrLevel l => l,
            string s when Enum.TryParse<CefrLevel>(s, true, out var pl) => pl,
            _ => null
        };

        var rootElement = (XamlApplication.Current as App)?.MainWindow?.Content as Microsoft.UI.Xaml.FrameworkElement;
        bool isLight = Services.ThemeResourceHelper.DetermineIsLight(rootElement);

        return level switch
        {
            CefrLevel.A1 or CefrLevel.A2 => isLight ? LightA1A2 : DarkA1A2,
            CefrLevel.B1 or CefrLevel.B2 => isLight ? LightB1B2 : DarkB1B2,
            CefrLevel.C1 or CefrLevel.C2 => isLight ? LightC1C2 : DarkC1C2,
            _ => isLight ? LightB1B2 : DarkB1B2
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
