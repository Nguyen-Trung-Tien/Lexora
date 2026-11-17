using Lexora.Domain.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class CefrToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        CefrLevel? level = value switch
        {
            CefrLevel l => l,
            string s when Enum.TryParse<CefrLevel>(s, true, out var pl) => pl,
            _ => null
        };

        var resourceKey = level switch
        {
            CefrLevel.A1 or CefrLevel.A2 => "LexoraCefrA1A2Brush",
            CefrLevel.B1 or CefrLevel.B2 => "LexoraCefrB1B2Brush",
            CefrLevel.C1 or CefrLevel.C2 => "LexoraCefrC1C2Brush",
            _ => "LexoraCefrB1B2Brush"
        };

        var rootElement = (XamlApplication.Current as App)?.MainWindow?.Content as FrameworkElement;
        return Services.ThemeResourceHelper.GetThemeBrush(rootElement, resourceKey)
               ?? Services.ThemeResourceHelper.GetThemeBrush(rootElement, "AccentFillColorDefaultBrush")!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
