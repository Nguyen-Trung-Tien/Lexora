using Lexora.Application.Interfaces;
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

public class ThemeService : IThemeService
{
    private readonly IUserPreferencesService _preferencesService;
    private Window? _window;
    private ElementTheme _currentTheme = ElementTheme.Default;

    public ElementTheme CurrentTheme => _currentTheme;
    public event EventHandler<ElementTheme>? ThemeChanged;

    public ThemeService(IUserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
        var prefs = _preferencesService.GetPreferences();
        _currentTheme = prefs.ThemeIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    public void Initialize(Window window)
    {
        _window = window;
        ApplyTheme(_currentTheme);
    }

    public void SetTheme(ElementTheme theme)
    {
        _currentTheme = theme;
        ApplyTheme(theme);

        int themeIdx = theme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };

        var currentPrefs = _preferencesService.GetPreferences();
        if (currentPrefs.ThemeIndex != themeIdx)
        {
            _ = _preferencesService.SavePreferencesAsync(currentPrefs with { ThemeIndex = themeIdx });
        }

        ThemeChanged?.Invoke(this, theme);
    }

    public void ToggleTheme()
    {
        var targetTheme = _currentTheme switch
        {
            ElementTheme.Light => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Light,
            _ => (_window?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark
                ? ElementTheme.Light
                : ElementTheme.Dark
        };

        SetTheme(targetTheme);
    }

    private void ApplyTheme(ElementTheme theme)
    {
        if (_window?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }

        if (_window is MainWindow mainWindow)
        {
            mainWindow.SetRootTheme(theme);
            mainWindow.UpdateTitleBarButtons();
        }

        // Dynamically propagate theme change to any currently open modal ContentDialog
        if (SafeDialogExtensions.CurrentlyActiveDialog is { } activeDialog)
        {
            activeDialog.RequestedTheme = theme;
        }
    }
}
