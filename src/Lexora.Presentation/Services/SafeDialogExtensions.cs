using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Services;

/// <summary>
/// Thread-safe and re-entrancy safe dialog display coordinator for WinUI 3.
/// Prevents COMException and "An async operation was already in progress" / "Only a single ContentDialog can be open at any time" crashes.
/// </summary>
public static class SafeDialogExtensions
{
    private static readonly SemaphoreSlim _dialogSemaphore = new(1, 1);
    private static ContentDialog? _currentlyActiveDialog;

    /// <summary>
    /// Displays a ContentDialog safely, serializing dialog requests and automatically resolving XamlRoot.
    /// Returns ContentDialogResult.None if the dialog cannot be shown or fails.
    /// </summary>
    public static async Task<ContentDialogResult> ShowAsyncSafe(this ContentDialog dialog, XamlRoot? preferredRoot = null)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        // Wait to acquire the dialog semaphore
        await _dialogSemaphore.WaitAsync();
        try
        {
            // Ensure any previous dialog had time to detach from the visual tree
            await Task.Yield();

            // Resolve XamlRoot
            if (dialog.XamlRoot == null)
            {
                dialog.XamlRoot = preferredRoot ?? App.Current.MainWindow?.Content?.XamlRoot;
            }

            if (dialog.XamlRoot == null)
            {
                System.Diagnostics.Debug.WriteLine("[SafeDialog] Warning: XamlRoot is null; dialog display skipped to prevent crash.");
                return ContentDialogResult.None;
            }

            _currentlyActiveDialog = dialog;
            var result = await dialog.ShowAsync();
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SafeDialog] Exception during ShowAsync: {ex.Message}");
            return ContentDialogResult.None;
        }
        finally
        {
            _currentlyActiveDialog = null;
            // Allow visual tree cleanup before next dialog can acquire lock
            await Task.Yield();
            _dialogSemaphore.Release();
        }
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
