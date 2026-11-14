using Lexora.Application.DTOs;
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

    public static string FormatPartOfSpeech(PartOfSpeech pos)
    {
        var loc = App.Current?.Services?.GetService<ILocalizationService>();
        if (loc == null) return pos.ToString();
        return pos switch
        {
            PartOfSpeech.Noun => loc.GetString("WordEditor_PosNoun"),
            PartOfSpeech.Verb => loc.GetString("WordEditor_PosVerb"),
            PartOfSpeech.Adjective => loc.GetString("WordEditor_PosAdj"),
            PartOfSpeech.Adverb => loc.GetString("WordEditor_PosAdv"),
            PartOfSpeech.PhrasalVerb => loc.GetString("WordEditor_PosPhrasalVerb"),
            PartOfSpeech.NounPhrase => loc.GetString("WordEditor_PosNounPhrase"),
            PartOfSpeech.Idiom => loc.GetString("WordEditor_PosIdiom"),
            PartOfSpeech.Phrase => loc.GetString("WordEditor_PosPhrase"),
            PartOfSpeech.Preposition => loc.GetString("WordEditor_PosPreposition"),
            PartOfSpeech.Conjunction => loc.GetString("WordEditor_PosConjunction"),
            PartOfSpeech.Interjection => loc.GetString("WordEditor_PosInterjection"),
            _ => pos.ToString()
        };
    }

    public static string FormatExamType(ExamType exam)
    {
        var loc = App.Current?.Services?.GetService<ILocalizationService>();
        if (loc == null) return exam.ToString();
        return exam switch
        {
            ExamType.Toeic => loc.GetString("Exam_Name_Toeic"),
            ExamType.Vstep => loc.GetString("Exam_Name_Vstep"),
            ExamType.Ielts => loc.GetString("Exam_Name_Ielts"),
            ExamType.General => loc.GetString("Exam_Name_General"),
            _ => exam.ToString()
        };
    }

    public static string FormatCefrLevel(CefrLevel level)
    {
        var loc = App.Current?.Services?.GetService<ILocalizationService>();
        if (loc == null) return level.ToString();
        return level switch
        {
            CefrLevel.None => loc.GetString("Cefr_None"),
            _ => level.ToString()
        };
    }

    public static string FormatStorageStatus(StorageLearningStatus status)
    {
        var loc = App.Current?.Services?.GetService<ILocalizationService>();
        if (loc == null) return status.ToString();
        return status switch
        {
            StorageLearningStatus.Weak => loc.GetString("Storage_FilterWeak"),
            StorageLearningStatus.Learning => loc.GetString("Storage_FilterLearning"),
            StorageLearningStatus.New => loc.GetString("Storage_FilterNew"),
            StorageLearningStatus.Mastered => loc.GetString("Storage_FilterMastered"),
            _ => status.ToString()
        };
    }

    public static string FormatAdditionReason(StorageAdditionReason reason)
    {
        var loc = App.Current?.Services?.GetService<ILocalizationService>();
        if (loc == null) return reason.ToString();
        return reason switch
        {
            StorageAdditionReason.Manual => loc.GetString("Storage_Reason_Manual"),
            StorageAdditionReason.MultipleErrors => loc.GetString("Storage_Reason_MultipleErrors"),
            StorageAdditionReason.LowRetention => loc.GetString("Storage_Reason_LowRetention"),
            StorageAdditionReason.Lapsed => loc.GetString("Storage_Reason_Lapsed"),
            _ => reason.ToString()
        };
    }

    public static string FormatSessionType(string? sessionType)
    {
        if (string.IsNullOrWhiteSpace(sessionType)) return string.Empty;
        var loc = App.Current?.Services?.GetService<ILocalizationService>();
        if (loc == null) return sessionType;
        return sessionType switch
        {
            "DailyReview" or "SpacedRepetition" or "Review" or "TodayDue" => loc.GetString("Session_SpacedRepetition"),
            "LearnNew" => loc.GetString("Session_LearnNew"),
            "Difficult" => loc.GetString("Session_Difficult"),
            "ByTopic" => loc.GetString("Session_ByTopic"),
            "CustomSelection" => loc.GetString("Session_CustomSelection"),
            "QuickPractice" or "QuickTest" => loc.GetString("Session_QuickPractice"),
            "Test_MultipleChoice" => loc.GetString("Session_TestMultipleChoice"),
            "Test_FillBlank" => loc.GetString("Session_TestFillBlank"),
            "Test_Listening" => loc.GetString("Session_TestListening"),
            _ => sessionType
        };
    }

    public static string FormatCurriculumTaskTitle(CurriculumTaskDto? task)
    {
        if (task == null) return string.Empty;
        return FormatTaskTitle(task.TitleVi, task.TitleEn);
    }

    public static string FormatCurriculumTaskDescription(CurriculumTaskDto? task)
    {
        if (task == null) return string.Empty;
        return FormatTaskDescription(task.DescriptionVi, task.DescriptionEn);
    }

    public static string FormatTaskTitle(string? titleVi, string? titleEn)
    {
        var loc = App.Current?.Services?.GetService<ILocalizationService>();
        var isVi = loc?.CurrentLanguage?.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ?? true;
        return isVi ? (titleVi ?? string.Empty) : (titleEn ?? titleVi ?? string.Empty);
    }

    public static string FormatTaskDescription(string? descVi, string? descEn)
    {
        var loc = App.Current?.Services?.GetService<ILocalizationService>();
        var isVi = loc?.CurrentLanguage?.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ?? true;
        return isVi ? (descVi ?? string.Empty) : (descEn ?? descVi ?? string.Empty);
    }

    public static string FormatBilingual(string? textVi, string? textEn)
    {
        var loc = App.Current?.Services?.GetService<ILocalizationService>();
        var isVi = loc?.CurrentLanguage?.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ?? true;
        return isVi ? (textVi ?? string.Empty) : (textEn ?? textVi ?? string.Empty);
    }
}

