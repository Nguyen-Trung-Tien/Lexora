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
            return rating switch
            {
                ReviewRating.Again => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 196, 43,  28)),  // red
                ReviewRating.Hard  => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 157, 93,   0)),  // amber
                ReviewRating.Good  => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16,  124, 65)),  // green
                ReviewRating.Easy  => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0,   103, 192)), // blue
                _                  => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Maps a <see cref="ReviewRating"/> to a foreground <see cref="Brush"/> (always white for readability).
/// </summary>
public sealed class ReviewRatingToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => new SolidColorBrush(Colors.White);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
