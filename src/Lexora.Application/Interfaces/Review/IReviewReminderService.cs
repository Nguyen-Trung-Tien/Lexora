namespace Lexora.Application.Interfaces;

public interface IReviewReminderService
{
    Task CheckAndNotifyDueReviewsAsync(CancellationToken cancellationToken = default);
    Task ShowNotificationAsync(string title, string message, string? actionTag = null);
}
