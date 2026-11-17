using Lexora.Presentation.Views.Common;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;

namespace Lexora.Presentation.Services;

public sealed class ErrorHandlingService : IErrorHandlingService
{
    private readonly ILocalizationService _localizationService;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<ErrorHandlingService>? _logger;
    private int _isShowingDialog;

    public ErrorHandlingService(
        ILocalizationService localizationService,
        IToastNotificationService toastService,
        ILogger<ErrorHandlingService>? logger = null)
    {
        _localizationService = localizationService;
        _toastService = toastService;
        _logger = logger;
    }

    public async Task HandleExceptionAsync(
        Exception exception,
        string source,
        string? userMessage = null,
        bool showDialog = true)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _logger?.LogError(exception, "Unhandled or reported error in {Source}: {Message}", source, exception.Message);
        App.LogCrash(source, exception, userMessage);

        var friendlyMessage = !string.IsNullOrWhiteSpace(userMessage)
            ? userMessage
            : exception.Message;

        if (!showDialog)
        {
            _toastService.Show(
                ToastType.Error,
                friendlyMessage,
                durationMs: 6500,
                actionText: _localizationService.GetString("Toast_CopyError"),
                actionCallback: () => CopyTextToClipboard($"[{source}] {exception}"));
            return;
        }

        if (Interlocked.CompareExchange(ref _isShowingDialog, 1, 0) != 0)
        {
            // Avoid dialog storm if multiple exceptions fire simultaneously
            return;
        }

        try
        {
            var dispatcher = App.Current?.UIDispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                var tcs = new TaskCompletionSource();
                dispatcher.TryEnqueue(async () =>
                {
                    try
                    {
                        await ShowExceptionDialogOnUIThreadAsync(exception, source, friendlyMessage);
                    }
                    finally
                    {
                        tcs.TrySetResult();
                    }
                });
                await tcs.Task;
            }
            else
            {
                await ShowExceptionDialogOnUIThreadAsync(exception, source, friendlyMessage);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isShowingDialog, 0);
        }
    }

    private async Task ShowExceptionDialogOnUIThreadAsync(Exception exception, string source, string friendlyMessage)
    {
        var xamlRoot = App.Current?.MainWindow?.Content?.XamlRoot;
        if (xamlRoot == null)
        {
            _toastService.ShowError(friendlyMessage, durationMs: 6500);
            return;
        }

        var dialog = new ExceptionDialog(_localizationService, exception, source, friendlyMessage);
        await dialog.ShowAsyncSafe(xamlRoot);
    }

    private static void CopyTextToClipboard(string text)
    {
        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }
        catch
        {
            // Clipboard access may fail if window is closing or locked
        }
    }
}
