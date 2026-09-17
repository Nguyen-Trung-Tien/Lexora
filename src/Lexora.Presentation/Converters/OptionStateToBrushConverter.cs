using Lexora.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Lexora.Presentation.Converters;

public class OptionStateToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is not TestOptionState state)
        {
            return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        string param = parameter as string ?? "Background";
        string brushKey = param switch
        {
            "Background" => state switch
            {
                TestOptionState.SelectedCorrect or TestOptionState.RevealedCorrect => "SystemFillColorSuccessBrush",
                TestOptionState.SelectedIncorrect => "SystemFillColorCriticalBrush",
                _ => "SubtleFillColorSecondaryBrush"
            },
            "Foreground" => state switch
            {
                TestOptionState.SelectedCorrect or TestOptionState.RevealedCorrect or TestOptionState.SelectedIncorrect => "TextOnAccentFillColorPrimaryBrush",
                _ => "TextFillColorSecondaryBrush"
            },
            "CardBorder" => state switch
            {
                TestOptionState.SelectedCorrect or TestOptionState.RevealedCorrect => "SystemFillColorSuccessBrush",
                TestOptionState.SelectedIncorrect => "SystemFillColorCriticalBrush",
                _ => "CardStrokeColorDefaultBrush"
            },
            "CardBackground" => state switch
            {
                TestOptionState.SelectedCorrect or TestOptionState.RevealedCorrect or TestOptionState.SelectedIncorrect => "SubtleFillColorSecondaryBrush",
                _ => "CardBackgroundFillColorDefaultBrush"
            },
            _ => "CardStrokeColorDefaultBrush"
        };

        if (XamlApplication.Current?.Resources != null)
        {
            var rootElement = (XamlApplication.Current as App)?.MainWindow?.Content as FrameworkElement;
            var themeKey = (rootElement?.ActualTheme == ElementTheme.Light) ? "Light" : "Default";

            if (XamlApplication.Current.Resources.ThemeDictionaries.TryGetValue(themeKey, out var dictObj) &&
                dictObj is ResourceDictionary themeDict &&
                themeDict.TryGetValue(brushKey, out var bObj) &&
                bObj is Brush themeBrush)
            {
                return themeBrush;
            }

            if (XamlApplication.Current.Resources.TryGetValue(brushKey, out var res) && res is Brush brush)
            {
                return brush;
            }
        }

        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class OptionStateToThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is TestOptionState state && state != TestOptionState.Default)
        {
            return new Thickness(2);
        }
        return new Thickness(1);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
