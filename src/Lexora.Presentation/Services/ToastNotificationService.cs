namespace Lexora.Presentation.Services;

public class ToastNotificationService : IToastNotificationService
{
    private readonly ILocalizationService _localizationService;

    public event Action<ToastNotification>? ToastRequested;

    public ToastNotificationService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public void Show(ToastType type, string message, string? title = null, int durationMs = 4000)
    {
        var resolvedTitle = !string.IsNullOrWhiteSpace(title)
            ? title
            : type switch
            {
                ToastType.Success => _localizationService.GetString("Toast_SuccessTitle"),
                ToastType.Error => _localizationService.GetString("Toast_ErrorTitle"),
                ToastType.Warning => _localizationService.GetString("Toast_WarningTitle"),
                _ => _localizationService.GetString("Toast_InfoTitle")
            };

        var notification = new ToastNotification(
            Guid.NewGuid(),
            type,
            resolvedTitle,
            message,
            durationMs);

        ToastRequested?.Invoke(notification);
    }

    public void ShowSuccess(string message, string? title = null, int durationMs = 4000)
        => Show(ToastType.Success, message, title, durationMs);

    public void ShowError(string message, string? title = null, int durationMs = 5000)
        => Show(ToastType.Error, message, title, durationMs);

    public void ShowWarning(string message, string? title = null, int durationMs = 4500)
        => Show(ToastType.Warning, message, title, durationMs);

    public void ShowInfo(string message, string? title = null, int durationMs = 4000)
        => Show(ToastType.Information, message, title, durationMs);
}
