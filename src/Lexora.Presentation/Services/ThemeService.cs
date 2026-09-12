using Microsoft.UI.Xaml;

namespace Lexora.Presentation.Services;

public interface IThemeService
{
    ElementTheme CurrentTheme { get; }
    void Initialize(Window window);
    void SetTheme(ElementTheme theme);
    event EventHandler<ElementTheme>? ThemeChanged;
}

public class ThemeService : IThemeService
{
    private Window? _window;
    private ElementTheme _currentTheme = ElementTheme.Default;

    public ElementTheme CurrentTheme => _currentTheme;
    public event EventHandler<ElementTheme>? ThemeChanged;

    public void Initialize(Window window)
    {
        _window = window;
        ApplyTheme(_currentTheme);
    }

    public void SetTheme(ElementTheme theme)
    {
        _currentTheme = theme;
        ApplyTheme(theme);
        ThemeChanged?.Invoke(this, theme);
    }

    private void ApplyTheme(ElementTheme theme)
    {
        if (_window?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
    }
}
