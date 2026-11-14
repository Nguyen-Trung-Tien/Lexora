using Microsoft.UI.Xaml;

namespace Lexora.Presentation.Services;

public interface IThemeService
{
    ElementTheme CurrentTheme { get; }
    void Initialize(Window window);
    void SetTheme(ElementTheme theme);
    void ToggleTheme();
    event EventHandler<ElementTheme>? ThemeChanged;
}
