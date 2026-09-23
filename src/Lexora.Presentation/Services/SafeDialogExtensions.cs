using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Services;

/// <summary>
/// Thread-safe and re-entrancy safe dialog display coordinator for WinUI 3.
/// Prevents COMException and "An async operation was already in progress" / "Only a single ContentDialog can be open at any time" crashes.
/// </summary>
public static class SafeDialogExtensions
{
    // Timeout prevents permanent deadlock if a previous dialog crashed while holding the semaphore.
    private static readonly TimeSpan _semaphoreTimeout = TimeSpan.FromSeconds(5);
    private static SemaphoreSlim _dialogSemaphore = new(1, 1);
    private static ContentDialog? _currentlyActiveDialog;

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
                    ?? App.Current.MainWindow?.DispatcherQueue
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
                // Force-reset the semaphore so future dialogs can display.
                System.Diagnostics.Debug.WriteLine("[SafeDialog] Semaphore timeout — resetting to recover from prior deadlock.");
                _currentlyActiveDialog = null;
                _dialogSemaphore = new SemaphoreSlim(1, 1);
                // Try one more time after reset.
                await _dialogSemaphore.WaitAsync();
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
                var stableRoot = App.Current.MainWindow?.Content?.XamlRoot ?? preferredRoot ?? dialog.XamlRoot;
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

                _currentlyActiveDialog = dialog;
                var result = await dialog.ShowAsync();
                return result;
            }
            finally
            {
                _currentlyActiveDialog = null;
                _dialogSemaphore.Release();
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
    /// Safely hides the dialog if it is currently open without throwing exceptions.
    /// </summary>
    public static void HideSafe(this ContentDialog dialog)
    {
        try
        {
            dialog.Hide();
        }
        catch
        {
            // Ignore if already hidden or not in visual tree
        }
    }

    /// <summary>
    /// Closes whatever dialog is currently open, if any.
    /// </summary>
    public static void HideActiveDialog()
    {
        try
        {
            _currentlyActiveDialog?.Hide();
        }
        catch
        {
            // Ignore
        }
    }
}
