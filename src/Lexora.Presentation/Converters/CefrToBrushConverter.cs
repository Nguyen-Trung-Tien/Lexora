using Lexora.Domain.Enums;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class CefrToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        string brushKey = "AccentFillColorDefaultBrush";

        if (value is CefrLevel level)
        {
            brushKey = level switch
            {
                CefrLevel.A1 or CefrLevel.A2 => "SystemFillColorSuccessBrush",
                CefrLevel.B1 or CefrLevel.B2 => "AccentFillColorDefaultBrush",
                CefrLevel.C1 or CefrLevel.C2 => "SystemFillColorCautionBrush",
                _ => "AccentFillColorDefaultBrush"
            };
        }
        else if (value is string str && Enum.TryParse<CefrLevel>(str, true, out var parsedLevel))
        {
            brushKey = parsedLevel switch
            {
                CefrLevel.A1 or CefrLevel.A2 => "SystemFillColorSuccessBrush",
                CefrLevel.B1 or CefrLevel.B2 => "AccentFillColorDefaultBrush",
                CefrLevel.C1 or CefrLevel.C2 => "SystemFillColorCautionBrush",
                _ => "AccentFillColorDefaultBrush"
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
