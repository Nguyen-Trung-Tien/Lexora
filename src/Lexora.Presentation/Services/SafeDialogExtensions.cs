using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Lexora.Presentation.Services;

/// <summary>
/// Thread-safe and re-entrancy safe dialog display coordinator for WinUI 3.
/// Prevents COMException and "An async operation was already in progress" / "Only a single ContentDialog can be open at any time" crashes.
/// </summary>
public static class SafeDialogExtensions
{
    // Timeout prevents permanent deadlock if a previous dialog crashed while holding the semaphore.
    private static readonly TimeSpan _semaphoreTimeout = TimeSpan.FromSeconds(5);
    private static readonly SemaphoreSlim _dialogSemaphore = new(1, 1);
    private static ContentDialog? _currentlyActiveDialog;

    /// <summary>
    /// Indicates whether a modal ContentDialog is currently active on screen.
    /// Used by ShortcutManager to prevent navigation and background mutations during modal workflows.
    /// </summary>
    public static bool IsDialogOpen => _currentlyActiveDialog != null;

    /// <summary>
    /// Gets the currently active ContentDialog instance, if any.
    /// </summary>
    public static ContentDialog? CurrentlyActiveDialog => _currentlyActiveDialog;

    /// <summary>
    /// Displays a ContentDialog safely, serializing dialog requests and automatically resolving XamlRoot.
    /// Returns ContentDialogResult.None if the dialog cannot be shown or fails.
    /// </summary>
    public static async Task<ContentDialogResult> ShowAsyncSafe(this ContentDialog dialog, XamlRoot? preferredRoot = null)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        try
        {
            DispatcherQueue? dispatcher = null;
            try
            {
                dispatcher = dialog.DispatcherQueue 
                    ?? App.Current?.UIDispatcherQueue
                    ?? App.Current?.MainWindow?.DispatcherQueue
                    ?? (preferredRoot?.Content as FrameworkElement)?.DispatcherQueue;
            }
            catch { }

            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                await dispatcher.EnsureOnUIThreadAsync();
            }

            // Use a timeout to avoid permanent deadlock if a prior crash left the semaphore locked.
            bool acquired = await _dialogSemaphore.WaitAsync(_semaphoreTimeout);
            if (!acquired)
            {
                System.Diagnostics.Debug.WriteLine("[SafeDialog] Semaphore timeout — another dialog is currently active. Display skipped to prevent collision.");
                return ContentDialogResult.None;
            }

            try
            {
                if (dispatcher != null && !dispatcher.HasThreadAccess)
                {
                    await dispatcher.EnsureOnUIThreadAsync();
                }

                // If a previous dialog is still recorded as active, safely hide it
                if (_currentlyActiveDialog != null && _currentlyActiveDialog != dialog)
                {
                    _currentlyActiveDialog.HideSafe();
                    await Task.Delay(50);
                    if (dispatcher != null && !dispatcher.HasThreadAccess)
                    {
                        await dispatcher.EnsureOnUIThreadAsync();
                    }
                }

                // Always prioritize the top-level main window root over temporary/closing popup roots
                var stableRoot = App.Current?.MainWindow?.Content?.XamlRoot ?? preferredRoot ?? dialog.XamlRoot;
                if (stableRoot != null)
                {
                    dialog.XamlRoot = stableRoot;
                }

                if (dialog.XamlRoot == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SafeDialog] Warning: XamlRoot is null; dialog display skipped to prevent crash.");
                    LogDialogCrash(dialog.GetType().Name, new InvalidOperationException("XamlRoot could not be resolved when displaying ContentDialog."));
                    return ContentDialogResult.None;
                }

                // Automatically synchronize dialog RequestedTheme with active visual theme (Light / Dark)
                if (dialog.RequestedTheme == ElementTheme.Default)
                {
                    var themeService = App.Current?.Services?.GetService<IThemeService>();
                    if (themeService != null && themeService.CurrentTheme != ElementTheme.Default)
                    {
                        dialog.RequestedTheme = themeService.CurrentTheme;
                    }
                    else if ((preferredRoot?.Content ?? App.Current?.MainWindow?.Content) is FrameworkElement fe && fe.ActualTheme != ElementTheme.Default)
                    {
                        dialog.RequestedTheme = fe.ActualTheme;
                    }
                }

                // Guarantee absolute centering within the window/XamlRoot
                dialog.HorizontalAlignment = HorizontalAlignment.Center;
                dialog.VerticalAlignment = VerticalAlignment.Center;

                // Optimize standard confirmation/notification dialogs (direct ContentDialog instances)
                if (dialog.GetType() == typeof(ContentDialog))
                {
                    // Apply compact, balanced proportions (320px - 420px wide instead of bloated default)
                    dialog.Resources["ContentDialogMinWidth"] = 320.0;
                    dialog.Resources["ContentDialogMaxWidth"] = 420.0;
                    dialog.CornerRadius = new CornerRadius(14);

                    // Enhance raw string message into an elegant Fluent 2 card layout with contextual state icon
                    if (dialog.Content is string textContent && !string.IsNullOrWhiteSpace(textContent))
                    {
                        dialog.Content = CreateCompactConfirmationContent(dialog.Title?.ToString(), textContent);
                    }
                }

                // Ensure all dialog buttons inherit the hand pointer cursor upon rendering
                dialog.Opened += Dialog_Opened_ApplyPointerCursor;

                _currentlyActiveDialog = dialog;
                var result = await dialog.ShowAsync();

                if (dispatcher != null && !dispatcher.HasThreadAccess)
                {
                    await dispatcher.EnsureOnUIThreadAsync();
                }

                return result;
            }
            finally
            {
                _currentlyActiveDialog = null;
                await Task.Yield();
                _dialogSemaphore.Release();

                if (dispatcher != null && !dispatcher.HasThreadAccess)
                {
                    try
                    {
                        await dispatcher.EnsureOnUIThreadAsync();
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SafeDialog] Exception during ShowAsyncSafe: {ex.Message}");
            LogDialogCrash(dialog.GetType().Name, ex);
            return ContentDialogResult.None;
        }
    }

    private static void LogDialogCrash(string dialogName, Exception ex)
    {
        try
        {
            var logDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lexora");
            System.IO.Directory.CreateDirectory(logDir);
            var logPath = System.IO.Path.Combine(logDir, "crash.log");
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SafeDialog] Failed to show {dialogName}\n{ex.GetType().FullName}: {ex.Message}\nStackTrace:\n{ex.StackTrace}\nInnerException: {ex.InnerException}\n\n";
            System.IO.File.AppendAllText(logPath, text);
        }
        catch { }
    }

    /// <summary>
    /// Logs Smart Import errors to the local diagnostic crash.log file.
    /// </summary>
    public static void LogSmartImportError(string source, Exception ex)
    {
        try
        {
            var logDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lexora");
            System.IO.Directory.CreateDirectory(logDir);
            var logPath = System.IO.Path.Combine(logDir, "crash.log");
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SmartImport] [{source}] Error: {ex.GetType().FullName}: {ex.Message}\nStackTrace:\n{ex.StackTrace}\nInnerException: {ex.InnerException}\n\n";
            System.IO.File.AppendAllText(logPath, text);
        }
        catch { }
    }

    /// <summary>
    /// Safely hides the dialog if it is currently open without throwing exceptions.
    /// Marshals to the UI thread if called from a background worker.
    /// </summary>
    public static void HideSafe(this ContentDialog dialog)
    {
        try
        {
            var dispatcher = dialog.DispatcherQueue 
                ?? App.Current?.UIDispatcherQueue 
                ?? App.Current?.MainWindow?.DispatcherQueue;
            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(() =>
                {
                    try { dialog.Hide(); } catch { }
                });
                return;
            }

            dialog.Hide();
        }
        catch
        {
            // Ignore if already hidden or not in visual tree
        }
    }

    /// <summary>
    /// Closes whatever dialog is currently open, if any.
    /// Marshals to the UI thread if called from a background worker.
    /// </summary>
    public static void HideActiveDialog()
    {
        try
        {
            var active = _currentlyActiveDialog;
            if (active == null) return;

            var dispatcher = active.DispatcherQueue 
                ?? App.Current?.UIDispatcherQueue 
                ?? App.Current?.MainWindow?.DispatcherQueue;
            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(() =>
                {
                    try { active.Hide(); } catch { }
                });
                return;
            }

            active.Hide();
        }
        catch
        {
            // Ignore
        }
    }

    private static UIElement CreateCompactConfirmationContent(string? title, string textMessage)
    {
        var isDestructive = (title != null && (
            title.Contains("Xóa", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("Hủy", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("CẢNH BÁO", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("Warning", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("Remove", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("Nguy hiểm", StringComparison.OrdinalIgnoreCase)
        )) || textMessage.Contains("CẢNH BÁO", StringComparison.OrdinalIgnoreCase)
           || textMessage.Contains("bị thay thế", StringComparison.OrdinalIgnoreCase)
           || textMessage.Contains("xóa vĩnh viễn", StringComparison.OrdinalIgnoreCase)
           || textMessage.Contains("ghi đè", StringComparison.OrdinalIgnoreCase);

        var stack = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 4, 0, 4),
            MaxWidth = 380
        };

        var rowGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 12
        };

        var iconBorder = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(10),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 1, 0, 0)
        };

        var icon = new FontIcon
        {
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (isDestructive)
        {
            iconBorder.Background = TryGetThemeBrush("SystemFillColorCriticalBackgroundBrush");
            iconBorder.BorderBrush = TryGetThemeBrush("SystemFillColorCriticalBrush");
            iconBorder.BorderThickness = new Thickness(1);
            icon.Glyph = "\uE74D"; // Trash / Delete
            icon.Foreground = TryGetThemeBrush("SystemFillColorCriticalBrush");
        }
        else
        {
            iconBorder.Background = TryGetThemeBrush("SubtleFillColorSecondaryBrush");
            iconBorder.BorderBrush = TryGetThemeBrush("CardStrokeColorDefaultBrush");
            iconBorder.BorderThickness = new Thickness(1);
            icon.Glyph = "\uE9CE"; // Info / Question
            icon.Foreground = TryGetThemeBrush("AccentTextFillColorPrimaryBrush");
        }

        iconBorder.Child = icon;
        Grid.SetColumn(iconBorder, 0);
        rowGrid.Children.Add(iconBorder);

        var textBlock = new TextBlock
        {
            Text = textMessage,
            FontSize = 13,
            LineHeight = 19,
            TextWrapping = TextWrapping.Wrap,
            Foreground = TryGetThemeBrush("TextFillColorPrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(textBlock, 1);
        rowGrid.Children.Add(textBlock);

        stack.Children.Add(rowGrid);
        return stack;
    }

    private static Brush TryGetThemeBrush(string resourceKey)
    {
        if (App.Current?.Resources?.TryGetValue(resourceKey, out var res) == true && res is Brush brush)
        {
            return brush;
        }

        return resourceKey switch
        {
            "SystemFillColorCriticalBrush" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 217, 45, 32)),
            "SystemFillColorCriticalBackgroundBrush" => new SolidColorBrush(Windows.UI.Color.FromArgb(40, 217, 45, 32)),
            "AccentTextFillColorPrimaryBrush" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 102, 204)),
            "SubtleFillColorSecondaryBrush" => new SolidColorBrush(Windows.UI.Color.FromArgb(25, 128, 128, 128)),
            "CardStrokeColorDefaultBrush" => new SolidColorBrush(Windows.UI.Color.FromArgb(30, 128, 128, 128)),
            "TextFillColorPrimaryBrush" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 32, 32, 32)),
            _ => new SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };
    }

    private static void Dialog_Opened_ApplyPointerCursor(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        try
        {
            ApplyHandCursorRecursive(sender);
        }
        catch { }
    }

    private static void ApplyHandCursorRecursive(DependencyObject root)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Button btn)
            {
                Helpers.CursorHelper.ApplyHandCursor(btn);
            }
            else if (child != null)
            {
                ApplyHandCursorRecursive(child);
            }
        }
    }
}
