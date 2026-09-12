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
        var dueCards = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, 1000, cancellationToken);
        if (dueCards.Count > 0)
        {
            var message = $"You have {dueCards.Count} word{(dueCards.Count > 1 ? "s" : "")} due for review today!";
            _logger.LogInformation("Review reminder triggered: {Message}", message);
            DueReviewsFound?.Invoke(this, new ReviewReminderEventArgs(dueCards.Count, message));
            await ShowNotificationAsync("Lexora Review Reminder", message);
        }
    }

    public Task ShowNotificationAsync(string title, string message, string? actionTag = null)
    {
        _logger.LogInformation("[Notification] {Title}: {Message}", title, message);
        return Task.CompletedTask;
    }
}
