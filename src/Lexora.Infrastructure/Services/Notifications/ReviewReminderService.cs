using Lexora.Application.Interfaces;
using Lexora.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Services;

public class ReviewReminderEventArgs : EventArgs
{
    public int DueCount { get; }
    public string Message { get; }

    public ReviewReminderEventArgs(int dueCount, string message)
    {
        DueCount = dueCount;
        Message = message;
    }
}

public class ReviewReminderService : IReviewReminderService
{
    private readonly IReviewRepository _reviewRepo;
    private readonly ILogger<ReviewReminderService> _logger;

    public event EventHandler<ReviewReminderEventArgs>? DueReviewsFound;

    public ReviewReminderService(
        IReviewRepository reviewRepo,
        ILogger<ReviewReminderService> logger)
    {
        _reviewRepo = reviewRepo;
        _logger = logger;
    }

    public async Task CheckAndNotifyDueReviewsAsync(CancellationToken cancellationToken = default)
    {
        var dueCount = await _reviewRepo.CountDueCardsAsync(DateTime.UtcNow, cancellationToken);
        if (dueCount > 0)
        {
            var message = $"You have {dueCount} word{(dueCount > 1 ? "s" : "")} due for review today!";
            _logger.LogInformation("Review reminder triggered: {Message}", message);
            DueReviewsFound?.Invoke(this, new ReviewReminderEventArgs(dueCount, message));
            await ShowNotificationAsync("Lexora Review Reminder", message);
        }
    }

    public Task ShowNotificationAsync(string title, string message, string? actionTag = null)
    {
        _logger.LogInformation("[Notification] {Title}: {Message}", title, message);
        return Task.CompletedTask;
    }
}
