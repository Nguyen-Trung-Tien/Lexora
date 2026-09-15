using Lexora.Domain.Enums;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        string brushKey = "AccentFillColorDefaultBrush";

        if (value is LearningStatus status)
        {
            brushKey = status switch
            {
                LearningStatus.Mastered or LearningStatus.Remembered => "SystemFillColorSuccessBrush",
                LearningStatus.Learning => "AccentFillColorDefaultBrush",
                LearningStatus.Review => "SystemFillColorCautionBrush",
                LearningStatus.NotRemembered => "SystemFillColorCriticalBrush",
                LearningStatus.New => "TextFillColorTertiaryBrush",
                _ => "AccentFillColorDefaultBrush"
            };
        }
        else if (value is string str && Enum.TryParse<LearningStatus>(str, true, out var parsedStatus))
        {
            brushKey = parsedStatus switch
            {
                LearningStatus.Mastered or LearningStatus.Remembered => "SystemFillColorSuccessBrush",
                LearningStatus.Learning => "AccentFillColorDefaultBrush",
                LearningStatus.Review => "SystemFillColorCautionBrush",
                LearningStatus.NotRemembered => "SystemFillColorCriticalBrush",
                LearningStatus.New => "TextFillColorTertiaryBrush",
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
