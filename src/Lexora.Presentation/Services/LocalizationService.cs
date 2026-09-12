using System.Globalization;

namespace Lexora.Presentation.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    void SetLanguage(string languageCode);
    string GetString(string key);
    event EventHandler? LanguageChanged;
}

public class LocalizationService : ILocalizationService
{
    private string _currentLanguage = "vi-VN";
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new()
    {
        ["vi-VN"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "Lexora - Quản lý Học Tiếng Anh",
            ["Nav_Dashboard"] = "Tổng quan",
            ["Nav_Vocabulary"] = "Từ vựng",
            ["Nav_Review"] = "Ôn tập ngắt quãng",
            ["Nav_Test"] = "Luyện tập & Kiểm tra",
            ["Nav_Grammar"] = "Ngữ pháp",
            ["Nav_Settings"] = "Cài đặt",
            ["Dashboard_TotalWords"] = "Tổng số từ",
            ["Dashboard_DueToday"] = "Cần ôn hôm nay",
            ["Dashboard_Mastered"] = "Đã thành thạo",
            ["Dashboard_Streak"] = "Chuỗi ngày học",
            ["Dashboard_StartReview"] = "Bắt đầu ôn tập hôm nay",
            ["Settings_Theme"] = "Giao diện",
            ["Settings_Theme_System"] = "Theo hệ thống",
            ["Settings_Theme_Light"] = "Sáng",
            ["Settings_Theme_Dark"] = "Tối",
            ["Settings_Language"] = "Ngôn ngữ ứng dụng",
            ["Search_Placeholder"] = "Tìm kiếm từ vựng, nghĩa...",
            ["Filter_AllLevels"] = "Tất cả trình độ",
            ["Action_Remembered"] = "Nhớ (Good)",
            ["Action_NotRemembered"] = "Chưa nhớ (Again)",
            ["Action_Easy"] = "Rất dễ (Easy)",
            ["Action_Hard"] = "Khó (Hard)"
        },
        ["en-US"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "Lexora - English Learning Manager",
            ["Nav_Dashboard"] = "Dashboard",
            ["Nav_Vocabulary"] = "Vocabulary",
            ["Nav_Review"] = "Spaced Review",
            ["Nav_Test"] = "Practice & Test",
            ["Nav_Grammar"] = "Grammar",
            ["Nav_Settings"] = "Settings",
            ["Dashboard_TotalWords"] = "Total Vocabulary",
            ["Dashboard_DueToday"] = "Due For Review",
            ["Dashboard_Mastered"] = "Mastered Words",
            ["Dashboard_Streak"] = "Day Streak",
            ["Dashboard_StartReview"] = "Start Today's Review",
            ["Settings_Theme"] = "Appearance Theme",
            ["Settings_Theme_System"] = "Use system setting",
            ["Settings_Theme_Light"] = "Light",
            ["Settings_Theme_Dark"] = "Dark",
            ["Settings_Language"] = "App Language",
            ["Search_Placeholder"] = "Search words, meanings...",
            ["Filter_AllLevels"] = "All CEFR Levels",
            ["Action_Remembered"] = "Remembered (Good)",
            ["Action_NotRemembered"] = "Not Remembered (Again)",
            ["Action_Easy"] = "Easy",
            ["Action_Hard"] = "Hard"
        }
    };

    public string CurrentLanguage => _currentLanguage;
    public event EventHandler? LanguageChanged;

    public void SetLanguage(string languageCode)
    {
        if (_resources.ContainsKey(languageCode))
        {
            _currentLanguage = languageCode;
            CultureInfo.CurrentUICulture = new CultureInfo(languageCode);
            CultureInfo.CurrentCulture = new CultureInfo(languageCode);
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string GetString(string key)
    {
        if (_resources.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var val))
        {
            return val;
        }

        // Fallback to en-US
        if (_resources["en-US"].TryGetValue(key, out var fallbackVal))
        {
            return fallbackVal;
        }

        return key;
    }
}
