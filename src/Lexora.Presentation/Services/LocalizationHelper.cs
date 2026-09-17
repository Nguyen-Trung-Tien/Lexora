using Lexora.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Presentation.Services;

public static class LocalizationHelper
{
    public static string GetString(string key)
    {
        var loc = App.Current.Services.GetService<ILocalizationService>();
        return loc?.GetString(key) ?? key;
    }

    public static string FormatRating(ReviewRating rating)
    {
        var loc = App.Current.Services.GetService<ILocalizationService>();
        if (loc == null) return rating.ToString();
        return rating switch
        {
            ReviewRating.Again => loc.GetString("Review_RatingAgain"),
            ReviewRating.Hard  => loc.GetString("Review_RatingHard"),
            ReviewRating.Good  => loc.GetString("Review_RatingGood"),
            ReviewRating.Easy  => loc.GetString("Review_RatingEasy"),
            _                  => rating.ToString()
        };
    }

    public static string FormatStatus(LearningStatus? status)
    {
        if (!status.HasValue) return string.Empty;
        var loc = App.Current.Services.GetService<ILocalizationService>();
        if (loc == null) return status.Value.ToString();
        return status.Value switch
        {
            LearningStatus.New           => loc.GetString("Status_New"),
            LearningStatus.Learning      => loc.GetString("Status_Learning"),
            LearningStatus.Remembered    => loc.GetString("Status_Remembered"),
            LearningStatus.Mastered      => loc.GetString("Status_Mastered"),
            LearningStatus.NotRemembered => loc.GetString("Status_NotRemembered"),
            _                            => status.Value.ToString()
        };
    }

    public static string FormatInterval(int intervalDays)
    {
        var loc = App.Current.Services.GetService<ILocalizationService>();
        if (intervalDays <= 0)
        {
            return loc?.GetString("Time_LessThanOneDay") ?? "< 1 d";
        }
        var format = loc?.GetString("Time_DaysShortFormat") ?? "{0} d";
        return string.Format(format, intervalDays);
    }

    public static string FormatRelativeTime(DateTime createdAt)
    {
        var loc = App.Current.Services.GetService<ILocalizationService>();
        var diff = DateTime.UtcNow - createdAt.ToUniversalTime();

        if (diff.TotalMinutes < 1)
        {
            return loc?.GetString("Time_JustNow") ?? "Just now";
        }
        if (diff.TotalMinutes < 60)
        {
            var format = loc?.GetString("Time_MinutesAgoFormat") ?? "{0} min ago";
            return string.Format(format, (int)diff.TotalMinutes);
        }
        if (diff.TotalHours < 24)
        {
            var format = loc?.GetString("Time_HoursAgoFormat") ?? "{0} h ago";
            return string.Format(format, (int)diff.TotalHours);
        }
        if (diff.TotalDays < 2)
        {
            return loc?.GetString("Time_Yesterday") ?? "Yesterday";
        }
        var daysFormat = loc?.GetString("Time_DaysAgoFormat") ?? "{0} d ago";
        return string.Format(daysFormat, (int)diff.TotalDays);
    }

    public static string FormatCorrectness(bool? isCorrect)
    {
        var loc = App.Current.Services.GetService<ILocalizationService>();
        if (isCorrect == true)
        {
            return loc?.GetString("Test_Correct") ?? "Correct";
        }
        return loc?.GetString("Test_Incorrect") ?? "Incorrect";
    }
}

