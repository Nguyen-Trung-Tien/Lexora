using Lexora.Domain.Enums;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Lexora.Presentation.Converters;

/// <summary>
/// Maps a <see cref="ReviewRating"/> to a semantic background <see cref="Brush"/>
/// for the rating badge in Recent Study Activity.
/// </summary>
public sealed class ReviewRatingToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ReviewRating rating)
        {
            string brushKey = rating switch
            {
                ReviewRating.Again => "SystemFillColorCriticalBrush",
                ReviewRating.Hard  => "SystemFillColorCautionBrush",
                ReviewRating.Good  => "SystemFillColorSuccessBrush",
                ReviewRating.Easy  => "AccentFillColorDefaultBrush",
                _                  => "SubtleFillColorSecondaryBrush"
            };

            var rootElement = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow?.Content as Microsoft.UI.Xaml.FrameworkElement;
            var themeBrush = Services.ThemeResourceHelper.GetThemeBrush(rootElement, brushKey);
            if (themeBrush != null)
            {
                return themeBrush;
            }

            if (Microsoft.UI.Xaml.Application.Current?.Resources != null &&
                Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue(brushKey, out var res) &&
                res is Brush brush)
            {
                return brush;
            }
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Maps a <see cref="ReviewRating"/> to a foreground <see cref="Brush"/> (guaranteeing high contrast against badge backgrounds).
/// </summary>
public sealed class ReviewRatingToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var rootElement = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow?.Content as Microsoft.UI.Xaml.FrameworkElement;
        var brush = Services.ThemeResourceHelper.GetThemeBrush(rootElement, "TextOnAccentFillColorPrimaryBrush");
        if (brush != null)
        {
            return brush;
        }
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
