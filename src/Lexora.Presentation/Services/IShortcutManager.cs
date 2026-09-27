using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Lexora.Presentation.Services;

/// <summary>
/// Centralized service orchestrating keyboard shortcuts across the application.
/// </summary>
public interface IShortcutManager
{
    /// <summary>The currently active page type in navigation context.</summary>
    Type? CurrentPageType { get; }

    /// <summary>Notifies the manager that navigation to a new page occurred.</summary>
    void SetCurrentPage(Type? pageType);

    /// <summary>Indicates if an asynchronous shortcut operation is currently executing.</summary>
    bool IsRunning { get; }

    /// <summary>Registers an individual shortcut.</summary>
    void RegisterShortcut(ShortcutDefinition shortcut);

    /// <summary>Unregisters a shortcut by its unique ID.</summary>
    void UnregisterShortcut(string shortcutId);

    /// <summary>Registers a batch of Page-scoped shortcuts for a specific page type.</summary>
    void RegisterPageShortcuts(Type pageType, IEnumerable<ShortcutDefinition> shortcuts);

    /// <summary>Unregisters all shortcuts registered for a specific page type.</summary>
    void UnregisterPageShortcuts(Type pageType);

    /// <summary>
    /// Processes a KeyDown event from WinUI 3 XAML root, resolving priority, context, and re-entrancy.
    /// Returns true if the shortcut was resolved and executed.
    /// </summary>
    bool ProcessKeyDown(KeyRoutedEventArgs e, XamlRoot? xamlRoot);

    /// <summary>
    /// Pure core processor for key events, usable in unit testing and headless validation.
    /// </summary>
    bool ProcessKeyDownCore(ShortcutKey key, ShortcutModifiers modifiers, bool isTextInputFocused, bool isDialogOpen);

    /// <summary>Retrieves all registered shortcuts for documentation/keymap presentation.</summary>
    IReadOnlyList<ShortcutDefinition> GetAllShortcuts();

    /// <summary>Retrieves all shortcuts currently eligible for execution in the active context.</summary>
    IReadOnlyList<ShortcutDefinition> GetActiveShortcuts();

    /// <summary>Raised whenever the shortcut collection changes.</summary>
    event EventHandler? ShortcutsChanged;
}
