namespace Lexora.Presentation.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Information
}

public record ToastNotification(
    Guid Id,
    ToastType Type,
    string Title,
    string Message,
    int DurationMs = 4000
);

public interface IToastNotificationService
{
    event Action<ToastNotification>? ToastRequested;
    void Show(ToastType type, string message, string? title = null, int durationMs = 4000);
    void ShowSuccess(string message, string? title = null, int durationMs = 4000);
    void ShowError(string message, string? title = null, int durationMs = 5000);
    void ShowWarning(string message, string? title = null, int durationMs = 4500);
    void ShowInfo(string message, string? title = null, int durationMs = 4000);
}
