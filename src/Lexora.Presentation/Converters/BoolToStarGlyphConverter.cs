using Microsoft.UI.Xaml.Data;

namespace Lexora.Presentation.Converters;

public class BoolToStarGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        return value is true ? "\uE735" : "\uE734";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
