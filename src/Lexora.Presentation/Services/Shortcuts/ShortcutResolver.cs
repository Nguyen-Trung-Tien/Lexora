namespace Lexora.Presentation.Services;

/// <summary>
/// Defines the activation scope for a keyboard shortcut.
/// </summary>
public enum ShortcutScope
{
    /// <summary>Shortcut is active across the entire application.</summary>
    Global,

    /// <summary>Shortcut is active only when its target Page is currently loaded.</summary>
    Page,

    /// <summary>Shortcut is active only when a modal ContentDialog is showing.</summary>
    Dialog
}

/// <summary>
/// Resolution priority order when multiple handlers or controls match a keystroke.
/// Follows Lexora AGENTS.md and production UX principles:
/// 1. Native text input controls (TextBox, Search, etc.)
/// 2. Active modal dialog context
/// 3. Focused custom control
/// 4. Active Page-specific shortcuts
/// 5. Global application shortcuts
/// </summary>
public enum ShortcutPriority
{
    NativeControl = 1,
    Dialog = 2,
    FocusedControl = 3,
    Page = 4,
    Global = 5
}

/// <summary>
/// Virtual key modifier flags.
/// </summary>
[Flags]
public enum ShortcutModifiers
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4,
    Windows = 8
}

/// <summary>
/// Standard virtual key codes compatible with Windows VirtualKey.
/// Allows core matching logic to run in non-WinRT unit test environments.
/// </summary>
public enum ShortcutKey
{
    None = 0,
    Back = 8,
    Tab = 9,
    Enter = 13,
    Escape = 27,
    Space = 32,
    PageUp = 33,
    PageDown = 34,
    End = 35,
    Home = 36,
    Left = 37,
    Up = 38,
    Right = 39,
    Down = 40,
    Delete = 46,
    Number0 = 48,
    Number1 = 49,
    Number2 = 50,
    Number3 = 51,
    Number4 = 52,
    Number5 = 53,
    Number6 = 54,
    Number7 = 55,
    Number8 = 56,
    Number9 = 57,
    A = 65, B = 66, C = 67, D = 68, E = 69, F = 70, G = 71, H = 72, I = 73, J = 74, K = 75, L = 76,
    M = 77, N = 78, O = 79, P = 80, Q = 81, R = 82, S = 83, T = 84, U = 85, V = 86, W = 87, X = 88,
    Y = 89, Z = 90,
    NumPad0 = 96,
    NumPad1 = 97,
    NumPad2 = 98,
    NumPad3 = 99,
    NumPad4 = 100,
    NumPad5 = 101,
    NumPad6 = 102,
    NumPad7 = 103,
    NumPad8 = 104,
    NumPad9 = 105,
    F1 = 112, F2 = 113, F3 = 114, F4 = 115, F5 = 116, F6 = 117,
    F7 = 118, F8 = 119, F9 = 120, F10 = 121, F11 = 122, F12 = 123
}

/// <summary>
/// Represents a key chord: a primary key with zero or more modifier keys.
/// </summary>
public readonly record struct ShortcutGesture(ShortcutKey Key, ShortcutModifiers Modifiers = ShortcutModifiers.None)
{
    public override string ToString()
    {
        var parts = new List<string>(3);
        if (Modifiers.HasFlag(ShortcutModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ShortcutModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ShortcutModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ShortcutModifiers.Windows)) parts.Add("Win");

        parts.Add(Key switch
        {
            ShortcutKey.Number0 => "0",
            ShortcutKey.Number1 => "1",
            ShortcutKey.Number2 => "2",
            ShortcutKey.Number3 => "3",
            ShortcutKey.Number4 => "4",
            ShortcutKey.Number5 => "5",
            ShortcutKey.Number6 => "6",
            ShortcutKey.Number7 => "7",
            ShortcutKey.Number8 => "8",
            ShortcutKey.Number9 => "9",
            ShortcutKey.NumPad0 => "NumPad0",
            ShortcutKey.NumPad1 => "NumPad1",
            ShortcutKey.NumPad2 => "NumPad2",
            ShortcutKey.NumPad3 => "NumPad3",
            ShortcutKey.NumPad4 => "NumPad4",
            _ => Key.ToString()
        });

        return string.Join("+", parts);
    }
}

/// <summary>
/// Complete descriptor and handler for a keyboard shortcut in Lexora.
/// </summary>
public sealed class ShortcutDefinition
{
    public required string Id { get; init; }
    public required ShortcutGesture Gesture { get; init; }
    public ShortcutGesture? SecondaryGesture { get; init; }
    public required ShortcutScope Scope { get; init; }
    public Type? TargetPageType { get; init; }
    public required string DescriptionKey { get; init; }
    public required Func<Task> ExecuteAsync { get; init; }
    public Func<bool>? CanExecute { get; init; }
    public bool AllowWhenTextInputFocused { get; init; } = false;
    public bool AllowWhenDialogActive { get; init; } = false;
    public ShortcutPriority Priority { get; init; } = ShortcutPriority.Page;
    public string DisplayGesture => Gesture.ToString();

    public bool Matches(ShortcutKey key, ShortcutModifiers modifiers)
    {
        if (Gesture.Key == key && Gesture.Modifiers == modifiers) return true;
        if (SecondaryGesture.HasValue && SecondaryGesture.Value.Key == key && SecondaryGesture.Value.Modifiers == modifiers) return true;
        return false;
    }
}

/// <summary>
/// Pure, headless shortcut resolution engine.
/// Operates independently of WinUI 3 visual tree, enabling unit testing.
/// </summary>
public sealed class ShortcutResolver
{
    private readonly Dictionary<string, ShortcutDefinition> _shortcuts = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public void Register(ShortcutDefinition shortcut)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        lock (_lock)
        {
            _shortcuts[shortcut.Id] = shortcut;
        }
    }

    public void Unregister(string shortcutId)
    {
        ArgumentNullException.ThrowIfNull(shortcutId);
        lock (_lock)
        {
            _shortcuts.Remove(shortcutId);
        }
    }

    public void RegisterPage(Type pageType, IEnumerable<ShortcutDefinition> shortcuts)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        ArgumentNullException.ThrowIfNull(shortcuts);
        lock (_lock)
        {
            foreach (var shortcut in shortcuts)
            {
                _shortcuts[shortcut.Id] = shortcut;
            }
        }
    }

    public void UnregisterPage(Type pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        lock (_lock)
        {
            var keysToRemove = _shortcuts.Values
                .Where(s => s.TargetPageType == pageType)
                .Select(s => s.Id)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _shortcuts.Remove(key);
            }
        }
    }

    public IReadOnlyList<ShortcutDefinition> GetAll()
    {
        lock (_lock)
        {
            return _shortcuts.Values.OrderBy(s => (int)s.Priority).ThenBy(s => s.Id).ToList();
        }
    }

    public IReadOnlyList<ShortcutDefinition> GetActive(bool isDialogOpen, Type? currentPageType)
    {
        lock (_lock)
        {
            return _shortcuts.Values
                .Where(s =>
                {
                    if (isDialogOpen) return s.AllowWhenDialogActive || s.Scope == ShortcutScope.Dialog;
                    if (s.Scope == ShortcutScope.Global) return true;
                    if (s.Scope == ShortcutScope.Page) return s.TargetPageType == currentPageType;
                    return false;
                })
                .OrderBy(s => (int)s.Priority)
                .ToList();
        }
    }

    /// <summary>
    /// Checks if a key or chord represents native text editing behavior that MUST NOT be hijacked.
    /// </summary>
    public static bool IsNativeTextEditingKey(ShortcutKey key, ShortcutModifiers modifiers)
    {
        // 1. Text editing chords (Ctrl + A, C, V, X, Z, Y, Backspace, Delete)
        if (modifiers == ShortcutModifiers.Control)
        {
            if (key is ShortcutKey.A or ShortcutKey.C or ShortcutKey.V or ShortcutKey.X or ShortcutKey.Z or ShortcutKey.Y or ShortcutKey.Back or ShortcutKey.Delete)
            {
                return true;
            }
        }

        // 2. Selection chords (Shift + Arrows, Home, End)
        if (modifiers == ShortcutModifiers.Shift || modifiers == (ShortcutModifiers.Control | ShortcutModifiers.Shift))
        {
            if (key is ShortcutKey.Left or ShortcutKey.Right or ShortcutKey.Up or ShortcutKey.Down or ShortcutKey.Home or ShortcutKey.End)
            {
                return true;
            }
        }

        // 3. Navigation chords without modifiers
        if (modifiers == ShortcutModifiers.None)
        {
            if (key is >= ShortcutKey.A and <= ShortcutKey.Z) return true;
            if (key is >= ShortcutKey.Number0 and <= ShortcutKey.Number9) return true;
            if (key is >= ShortcutKey.NumPad0 and <= ShortcutKey.NumPad9) return true;
            if (key is ShortcutKey.Space or ShortcutKey.Enter or ShortcutKey.Back or ShortcutKey.Delete or ShortcutKey.Tab) return true;
            if (key is ShortcutKey.Left or ShortcutKey.Right or ShortcutKey.Up or ShortcutKey.Down or ShortcutKey.Home or ShortcutKey.End) return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves the winning shortcut definition based on priority hierarchy and active context.
    /// </summary>
    public ShortcutDefinition? Resolve(ShortcutKey key, ShortcutModifiers modifiers, bool isTextInputFocused, bool isDialogOpen, Type? currentPageType)
    {
        // 1. Text input protection
        if (isTextInputFocused && IsNativeTextEditingKey(key, modifiers))
        {
            return null;
        }

        List<ShortcutDefinition> candidates;
        lock (_lock)
        {
            candidates = _shortcuts.Values
                .Where(s => s.Matches(key, modifiers))
                .ToList();
        }

        if (candidates.Count == 0) return null;

        // 2. Contextual filtering
        var eligible = candidates.Where(s =>
        {
            if (isDialogOpen && !s.AllowWhenDialogActive && s.Scope != ShortcutScope.Dialog)
            {
                return false;
            }

            if (isTextInputFocused && !s.AllowWhenTextInputFocused)
            {
                return false;
            }

            if (s.Scope == ShortcutScope.Page)
            {
                if (s.TargetPageType == null || s.TargetPageType != currentPageType)
                {
                    return false;
                }
            }

            if (s.CanExecute != null && !s.CanExecute())
            {
                return false;
            }

            return true;
        }).ToList();

        if (eligible.Count == 0) return null;

        // 3. Priority resolution (lowest integer priority value wins)
        return eligible.OrderBy(s => (int)s.Priority).FirstOrDefault();
    }
}
