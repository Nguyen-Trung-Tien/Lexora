using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Lexora.Presentation.Services;

/// <summary>
/// Production-grade centralized Shortcut Manager for Lexora.
/// Enforces predictable routing, strict priority hierarchy, native input protection,
/// dialog isolation, and anti-reentrancy debounce.
/// </summary>
public sealed class ShortcutManager : IShortcutManager
{
    private readonly ShortcutResolver _resolver = new();
    private readonly INavigationService _navigationService;
    private long _lastExecutionTick;
    private volatile bool _isExecuting;

    public Type? CurrentPageType { get; private set; }
    public bool IsRunning => _isExecuting;

    public event EventHandler? ShortcutsChanged;
    public event EventHandler? GlobalSearchRequested;

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    public ShortcutManager(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.Navigated += (s, pageType) => SetCurrentPage(pageType);

        RegisterDefaultGlobalShortcuts();
    }

    public void SetCurrentPage(Type? pageType)
    {
        CurrentPageType = pageType;
        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RegisterDefaultGlobalShortcuts()
    {
        RegisterShortcut(new ShortcutDefinition
        {
            Id = "Global.NavigateDashboard",
            Gesture = new ShortcutGesture(ShortcutKey.Number1, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Shell_TooltipDashboard",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = false,
            AllowWhenTextInputFocused = false,
            ExecuteAsync = () =>
            {
                _navigationService.Navigate(typeof(Views.DashboardPage));
                return Task.CompletedTask;
            }
        });

        RegisterShortcut(new ShortcutDefinition
        {
            Id = "Global.NavigateVocabulary",
            Gesture = new ShortcutGesture(ShortcutKey.Number2, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Shell_TooltipVocabulary",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = false,
            AllowWhenTextInputFocused = false,
            ExecuteAsync = () =>
            {
                _navigationService.Navigate(typeof(Views.VocabularyPage));
                return Task.CompletedTask;
            }
        });

        RegisterShortcut(new ShortcutDefinition
        {
            Id = "Global.NavigateReview",
            Gesture = new ShortcutGesture(ShortcutKey.Number3, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Shell_TooltipReview",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = false,
            AllowWhenTextInputFocused = false,
            ExecuteAsync = () =>
            {
                _navigationService.Navigate(typeof(Views.ReviewPage));
                return Task.CompletedTask;
            }
        });

        RegisterShortcut(new ShortcutDefinition
        {
            Id = "Global.NavigateGrammar",
            Gesture = new ShortcutGesture(ShortcutKey.Number4, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Shell_TooltipGrammar",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = false,
            AllowWhenTextInputFocused = false,
            ExecuteAsync = () =>
            {
                _navigationService.Navigate(typeof(Views.GrammarPage));
                return Task.CompletedTask;
            }
        });

        RegisterShortcut(new ShortcutDefinition
        {
            Id = "Global.NavigateTest",
            Gesture = new ShortcutGesture(ShortcutKey.Number5, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Shell_TooltipTest",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = false,
            AllowWhenTextInputFocused = false,
            ExecuteAsync = () =>
            {
                _navigationService.Navigate(typeof(Views.TestPage));
                return Task.CompletedTask;
            }
        });

        RegisterShortcut(new ShortcutDefinition
        {
            Id = "Global.NavigateLearningStorage",
            Gesture = new ShortcutGesture(ShortcutKey.Number6, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Shell_TooltipLearningStorage",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = false,
            AllowWhenTextInputFocused = false,
            ExecuteAsync = () =>
            {
                _navigationService.Navigate(typeof(Views.LearningStoragePage));
                return Task.CompletedTask;
            }
        });

        RegisterShortcut(new ShortcutDefinition
        {
            Id = "Global.NavigateDownloadCenter",
            Gesture = new ShortcutGesture(ShortcutKey.Number7, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Shell_TooltipDownloadCenter",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = false,
            AllowWhenTextInputFocused = false,
            ExecuteAsync = () =>
            {
                _navigationService.Navigate(typeof(Views.DownloadCenterPage));
                return Task.CompletedTask;
            }
        });

        RegisterShortcut(new ShortcutDefinition
        {
            Id = "Global.SearchFocus",
            Gesture = new ShortcutGesture(ShortcutKey.K, ShortcutModifiers.Control),
            SecondaryGesture = new ShortcutGesture(ShortcutKey.F, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Shell_GlobalSearchPlaceholder",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = false,
            AllowWhenTextInputFocused = true, // Allow invoking search from text inputs
            ExecuteAsync = () =>
            {
                GlobalSearchRequested?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            }
        });

        RegisterShortcut(new ShortcutDefinition
        {
            Id = "Global.ToggleTheme",
            Gesture = new ShortcutGesture(ShortcutKey.T, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Nav_ThemeToggle",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = true, // Enable theme toggle even while dialog is open
            AllowWhenTextInputFocused = false,
            ExecuteAsync = () =>
            {
                var themeService = App.Current?.Services?.GetService<IThemeService>();
                themeService?.ToggleTheme();
                return Task.CompletedTask;
            }
        });
    }

    public void RegisterShortcut(ShortcutDefinition shortcut)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        _resolver.Register(shortcut);
        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UnregisterShortcut(string shortcutId)
    {
        ArgumentNullException.ThrowIfNull(shortcutId);
        _resolver.Unregister(shortcutId);
        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RegisterPageShortcuts(Type pageType, IEnumerable<ShortcutDefinition> shortcuts)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        ArgumentNullException.ThrowIfNull(shortcuts);

        _resolver.RegisterPage(pageType, shortcuts);
        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UnregisterPageShortcuts(Type pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        _resolver.UnregisterPage(pageType);
        ShortcutsChanged?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<ShortcutDefinition> GetAllShortcuts()
    {
        return _resolver.GetAll();
    }

    public IReadOnlyList<ShortcutDefinition> GetActiveShortcuts()
    {
        bool isDialog = SafeDialogExtensions.IsDialogOpen;
        return _resolver.GetActive(isDialog, CurrentPageType);
    }

    /// <summary>
    /// Extracts the current physical modifier keys synchronously via user32 GetKeyState.
    /// </summary>
    public static ShortcutModifiers GetCurrentModifiers()
    {
        ShortcutModifiers mods = ShortcutModifiers.None;
        if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) mods |= ShortcutModifiers.Control;
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) mods |= ShortcutModifiers.Shift;
        if ((GetKeyState(VK_MENU) & 0x8000) != 0) mods |= ShortcutModifiers.Alt;
        if ((GetKeyState(VK_LWIN) & 0x8000) != 0 || (GetKeyState(VK_RWIN) & 0x8000) != 0) mods |= ShortcutModifiers.Windows;
        return mods;
    }

    /// <summary>
    /// Determines whether the focused UI element represents an active text input control
    /// or is nested within one (e.g. internal TextBox inside AutoSuggestBox).
    /// </summary>
    public static bool IsTextInputActive(object? focusedElement)
    {
        if (focusedElement == null) return false;
        if (focusedElement is TextBox or PasswordBox or RichEditBox or AutoSuggestBox) return true;

        if (focusedElement is DependencyObject dobj)
        {
            var parent = VisualTreeHelper.GetParent(dobj);
            while (parent != null)
            {
                if (parent is TextBox or PasswordBox or RichEditBox or AutoSuggestBox) return true;
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a key or chord represents native text editing behavior that MUST NOT be hijacked.
    /// </summary>
    public static bool IsNativeTextEditingKey(ShortcutKey key, ShortcutModifiers modifiers)
    {
        return ShortcutResolver.IsNativeTextEditingKey(key, modifiers);
    }

    /// <summary>
    /// WinUI 3 entry point. Resolves modifiers and focus state, then delegates to ProcessKeyDownCore.
    /// </summary>
    public bool ProcessKeyDown(KeyRoutedEventArgs e, XamlRoot? xamlRoot)
    {
        ArgumentNullException.ThrowIfNull(e);

        var key = (ShortcutKey)(int)e.Key;
        var modifiers = GetCurrentModifiers();

        object? focusedElement = xamlRoot != null ? FocusManager.GetFocusedElement(xamlRoot) : null;
        bool isTextInput = IsTextInputActive(focusedElement);
        bool isDialogOpen = SafeDialogExtensions.IsDialogOpen;

        bool handled = ProcessKeyDownCore(key, modifiers, isTextInput, isDialogOpen);
        if (handled)
        {
            e.Handled = true;
        }
        return handled;
    }

    /// <summary>
    /// Core resolution logic with 5-tier priority hierarchy and anti-reentrancy debounce.
    /// </summary>
    public bool ProcessKeyDownCore(ShortcutKey key, ShortcutModifiers modifiers, bool isTextInputFocused, bool isDialogOpen)
    {
        // 1. Reentrancy and rapid debounce guard (120ms threshold)
        long now = Environment.TickCount64;
        if (_isExecuting || (now - _lastExecutionTick < 120))
        {
            return false;
        }

        // 2. Resolve via ShortcutResolver
        var winner = _resolver.Resolve(key, modifiers, isTextInputFocused, isDialogOpen, CurrentPageType);
        if (winner == null) return false;

        // 3. Execute with re-entrancy locking
        _lastExecutionTick = now;
        _isExecuting = true;

        _ = ExecuteActionSafelyAsync(winner);
        return true;
    }

    private async Task ExecuteActionSafelyAsync(ShortcutDefinition shortcut)
    {
        try
        {
            await shortcut.ExecuteAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShortcutManager] Execution error in {shortcut.Id}: {ex.Message}");
        }
        finally
        {
            _isExecuting = false;
        }
    }
}
