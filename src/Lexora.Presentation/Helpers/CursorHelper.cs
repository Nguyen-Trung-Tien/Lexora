using System.Reflection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;

namespace Lexora.Presentation.Helpers;

/// <summary>
/// Attached property helper enabling declarative cursor customization (e.g. Hand pointer cursor)
/// for any UIElement in WinUI 3 desktop applications.
/// </summary>
public static class CursorHelper
{
    private static readonly PropertyInfo? ProtectedCursorProperty = typeof(UIElement).GetProperty(
        "ProtectedCursor",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly Lazy<InputCursor> HandCursor = new(() =>
        InputSystemCursor.Create(InputSystemCursorShape.Hand));

    public static readonly DependencyProperty CursorProperty =
        DependencyProperty.RegisterAttached(
            "Cursor",
            typeof(string),
            typeof(CursorHelper),
            new PropertyMetadata(null, OnCursorChanged));

    public static string? GetCursor(DependencyObject element) =>
        (string?)element.GetValue(CursorProperty);

    public static void SetCursor(DependencyObject element, string? value) =>
        element.SetValue(CursorProperty, value);

    /// <summary>
    /// Directly applies a hand/pointer cursor to the target UIElement.
    /// </summary>
    public static void ApplyHandCursor(UIElement? element)
    {
        if (element == null) return;
        try
        {
            ProtectedCursorProperty?.SetValue(element, HandCursor.Value);
        }
        catch { }
    }

    private static void OnCursorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && e.NewValue is string cursorType && string.Equals(cursorType, "Hand", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                ProtectedCursorProperty?.SetValue(element, HandCursor.Value);
            }
            catch
            {
                // Silently fallback if cursor subsystem is constrained in test or headless environments
            }
        }
    }
}
