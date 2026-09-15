using System.Globalization;

namespace Lexora.Presentation.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    void SetLanguage(string languageCode);
    string GetString(string key);
    string this[string key] { get; }
    event EventHandler? LanguageChanged;
}

public class LocalizationService : ILocalizationService
{
    private string _currentLanguage = "vi-VN";
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new()
    {
        ["vi-VN"] = new Dictionary<string, string>
        {
            // App & Shell Navigation
            ["AppTitle"] = "Lexora - Quản lý Học Tiếng Anh",
            ["Nav_Dashboard"] = "Tổng quan",
            ["Nav_Vocabulary"] = "Từ vựng",
            ["Nav_Review"] = "Ôn tập ngắt quãng",
            ["Nav_Test"] = "Luyện tập & Thi",
            ["Nav_Grammar"] = "Ngữ pháp",
            ["Nav_Settings"] = "Cài đặt",
            ["Search_Placeholder"] = "Tìm kiếm từ vựng, ngữ pháp... (Ctrl+F)",
            ["Review_Reminder_Title"] = "Nhắc nhở ôn tập",
            ["Review_Now"] = "Ôn tập ngay",

            // Common Actions
            ["Action_Edit"] = "Chỉnh sửa",
            ["Action_Delete"] = "Xóa",
            ["Action_Save"] = "Lưu",
            ["Action_Cancel"] = "Hủy",
            ["Action_Close"] = "Đóng",
            ["Action_Reset"] = "Đặt lại",
            ["Action_Search"] = "Tìm kiếm",
            ["Action_Filter"] = "Bộ lọc",

            // Dashboard
            ["Dashboard_HeroTitle"] = "Ôn tập ngắt quãng (SM-2)",
            ["Dashboard_HeroGreeting"] = "Sẵn sàng chinh phục mục tiêu từ vựng hôm nay?",
            ["Dashboard_StartReview"] = "Bắt đầu ôn tập hôm nay",
            ["Dashboard_QuickPractice"] = "Luyện trắc nghiệm nhanh",
            ["Dashboard_TotalWords"] = "Tổng số từ",
            ["Dashboard_DueToday"] = "Cần ôn hôm nay",
            ["Dashboard_Mastered"] = "Đã thành thạo",
            ["Dashboard_Streak"] = "Chuỗi ngày học",
            ["Dashboard_DaysStreak"] = "ngày liên tiếp",
            ["Dashboard_RetentionChart"] = "Độ bền ghi nhớ (SM-2 Retention Curve)",
            ["Dashboard_ActivityHeatmap"] = "Bản đồ nhiệt hoạt động (30 ngày gần nhất)",
            ["Dashboard_EmptyActivity"] = "Chưa có lượt học gần đây. Hãy ôn tập ngay để thắp sáng chuỗi ngày học!",
            ["Dashboard_RetentionDistribution"] = "Phân bố ghi nhớ từ vựng (SM-2)",
            ["Dashboard_RetentionSubtitle"] = "Phân bố từ vựng theo các giai đoạn ghi nhớ lặp lại ngắt quãng",
            ["Dashboard_OverallAccuracy"] = "Tỷ lệ chính xác chung:",
            ["Dashboard_ExamTargetProgress"] = "Tiến độ mục tiêu kỳ thi",
            ["Dashboard_ExamTotal"] = "Tổng",
            ["Dashboard_ExamLearning"] = "Đang học",
            ["Dashboard_ExamMastered"] = "Đã thuộc",
            ["Dashboard_HeatmapTitle"] = "Bản đồ nhiệt học tập hàng ngày (30 ngày)",
            ["Dashboard_HeatmapSubtitle"] = "Theo dõi tính kiên trì và thói quen ôn tập của bạn mỗi ngày",
            ["Dashboard_HeatmapLess"] = "Ít",
            ["Dashboard_HeatmapMore"] = "Nhiều",
            ["Dashboard_ReviewedToday"] = "Đã ôn hôm nay:",
            ["Dashboard_CurrentStreak"] = "Chuỗi hiện tại:",
            ["Dashboard_BestStreak"] = "Kỷ lục chuỗi:",
            ["Dashboard_DaysUnit"] = "ngày",
            ["Dashboard_CefrBreakdown"] = "Phân bố theo cấp độ CEFR",
            ["Dashboard_PosBreakdown"] = "Phân bố theo từ loại",
            ["Dashboard_RecentSessions"] = "Lịch sử học tập & kiểm tra gần đây",
            ["Dashboard_SessionCards"] = "Số thẻ:",
            ["Dashboard_SessionCorrect"] = "Chính xác:",

            // Vocabulary Hub
            ["Vocab_Title"] = "Kho Từ Vựng",
            ["Vocab_ViewList"] = "Danh sách từ",
            ["Vocab_ViewUnits"] = "Theo chủ đề",
            ["Vocab_SearchPlaceholder"] = "Tìm từ vựng, phiên âm, định nghĩa... (Ctrl+F)",
            ["Vocab_AddWord"] = "Thêm từ mới",
            ["Vocab_ImportExport"] = "Nhập / Xuất",
            ["Vocab_ExportJson"] = "Xuất file JSON",
            ["Vocab_ExportCsv"] = "Xuất file CSV",
            ["Vocab_ImportJson"] = "Nhập từ file JSON...",
            ["Vocab_ImportCsv"] = "Nhập từ file CSV...",
            ["Vocab_TopicFilter"] = "Chủ đề / Bài học",
            ["Vocab_LevelFilter"] = "Trình độ CEFR",
            ["Vocab_PosFilter"] = "Từ loại",
            ["Vocab_StatusFilter"] = "Trạng thái ôn tập",
            ["Vocab_SortBy"] = "Sắp xếp theo",
            ["Vocab_SortDefault"] = "Mặc định (Mới nhất)",
            ["Vocab_SortAlpha"] = "Bảng chữ cái (A - Z)",
            ["Vocab_SortDueDate"] = "Ưu tiên cần ôn",
            ["Vocab_AllTopics"] = "Tất cả chủ đề",
            ["Vocab_AllLevels"] = "Tất cả trình độ",
            ["Vocab_AllPos"] = "Tất cả từ loại",
            ["Vocab_AllStatus"] = "Tất cả trạng thái",
            ["Vocab_WordsCount"] = "từ vựng",
            ["Vocab_EmptyTitle"] = "Không tìm thấy từ vựng nào",
            ["Vocab_EmptySubtitle"] = "Thử xóa bộ lọc tìm kiếm hoặc thêm từ vựng mới để bắt đầu học tập.",
            ["Vocab_SelectPromptTitle"] = "Chọn một từ vựng",
            ["Vocab_SelectPromptSubtitle"] = "Chọn một từ bên trái để xem phát âm chuẩn UK/US, định nghĩa chi tiết Oxford/Cambridge và ví dụ ngữ cảnh.",
            ["Vocab_DefinitionVi"] = "Định nghĩa Tiếng Việt",
            ["Vocab_DefinitionEn"] = "Định nghĩa Tiếng Anh",
            ["Vocab_Examples"] = "Câu ví dụ thực tế",
            ["Vocab_Notes"] = "Ghi chú & Cụm từ đi kèm",
            ["Vocab_Sm2Status"] = "Trạng thái thuật toán SM-2",
            ["Vocab_EaseFactor"] = "Hệ số dễ (EF)",
            ["Vocab_Interval"] = "Khoảng lặp",
            ["Vocab_NextReview"] = "Lần ôn tiếp",

            // Spaced Review
            ["Review_CardProgress"] = "Thẻ ôn tập",
            ["Review_FlipCard"] = "Nhấn Phím Cách (Space) hoặc Click để lật thẻ",
            ["Review_CompletedTitle"] = "Hoàn Thành Ôn Tập Hôm Nay! 🎉",
            ["Review_CompletedSubtitle"] = "Xuất sắc! Bạn đã ôn tập hết các thẻ từ vựng đến hạn hôm nay theo thuật toán SM-2.",
            ["Review_BackDashboard"] = "Về Trang Chủ",
            ["Review_TodayDue"] = "Cần ôn hôm nay",
            ["Review_LearnNew"] = "Học từ mới",
            ["Review_Difficult"] = "Từ khó & Lapses",
            ["Review_TypingDrill"] = "Luyện gõ từ",
            ["Review_Remaining"] = "Còn lại:",
            ["Review_AllCaughtUp"] = "Đã hoàn thành hết! 🎉",
            ["Review_AllCaughtUpDesc"] = "Bạn có 0 thẻ cần ôn trong danh sách này. Thật xuất sắc trong việc duy trì chuỗi học tập!",
            ["Review_ShowMeaning"] = "Hiện nghĩa (Phím cách)",
            ["Review_RatingAgain"] = "Quên (Again)",
            ["Review_RatingHard"] = "Khó (Hard)",
            ["Review_RatingGood"] = "Nhớ (Good)",
            ["Review_RatingEasy"] = "Dễ (Easy)",
            ["Review_SessionStats"] = "Thống kê phiên",
            ["Review_Done"] = "Đã ôn",
            ["Review_Left"] = "Còn lại",
            ["Action_NotRemembered"] = "Chưa nhớ (Again)",
            ["Action_Hard"] = "Khó nhớ (Hard)",
            ["Action_Remembered"] = "Đã nhớ (Good)",
            ["Action_Easy"] = "Rất dễ (Easy)",

            // Practice & Test
            ["Test_Title"] = "Luyện Tập & Kiểm Tra",
            ["Test_Subtitle"] = "Chọn thể loại câu hỏi và số lượng để kiểm tra mức độ ghi nhớ từ vựng",
            ["Test_StartTest"] = "Bắt đầu làm bài thi",
            ["Test_ExitTest"] = "Thoát bài thi",
            ["Test_Question"] = "Câu hỏi",
            ["Test_SubmitAnswer"] = "Xác nhận đáp án",
            ["Test_NextQuestion"] = "Câu tiếp theo",
            ["Test_FinishTest"] = "Hoàn thành bài thi",
            ["Test_ResultTitle"] = "Kết Quả Bài Kiểm Tra",
            ["Test_RetakeTest"] = "Làm lại bài thi",
            ["Test_ReviewWeakWords"] = "Ôn lại các từ làm sai",

            // Grammar
            ["Grammar_Title"] = "Cẩm Nang Ngữ Pháp",
            ["Grammar_SearchPlaceholder"] = "Tìm quy tắc, công thức ngữ pháp, cấu trúc... (Ctrl+F)",
            ["Grammar_AddRule"] = "Thêm quy tắc",
            ["Grammar_Practice"] = "Luyện tập",
            ["Grammar_Structures"] = "Cấu trúc ngữ pháp",
            ["Grammar_EmptyTitle"] = "Không tìm thấy cấu trúc ngữ pháp",
            ["Grammar_EmptySubtitle"] = "Thử xóa bộ lọc hoặc tìm kiếm để xem các bài học ngữ pháp sẵn có.",
            ["Grammar_SelectPromptTitle"] = "Chọn một quy tắc ngữ pháp",
            ["Grammar_SelectPromptSubtitle"] = "Chọn cấu trúc ngữ pháp bên trái để xem công thức, giải thích chuyên sâu và lỗi cần tránh.",
            ["Grammar_StructureFormula"] = "Công thức cấu trúc",
            ["Grammar_Explanation"] = "Giải thích chi tiết",
            ["Grammar_CommonMistakes"] = "⚠️ Lỗi thường gặp & Lưu ý",
            ["Grammar_Examples"] = "Ví dụ minh họa ngữ cảnh",
            ["Grammar_ExitPractice"] = "Thoát luyện tập",

            // Settings
            ["Settings_Title"] = "Cài Đặt Ứng Dụng",
            ["Settings_Theme"] = "Giao diện hiển thị",
            ["Settings_ThemeSubtitle"] = "Tùy chỉnh chế độ sáng, tối hoặc theo cài đặt hệ điều hành Windows.",
            ["Settings_Theme_System"] = "Theo hệ thống Windows",
            ["Settings_Theme_Light"] = "Chế độ Sáng (Light)",
            ["Settings_Theme_Dark"] = "Chế độ Tối (Dark)",
            ["Settings_Language"] = "Ngôn ngữ ứng dụng",
            ["Settings_LanguageSubtitle"] = "Lựa chọn ngôn ngữ giao diện hiển thị cho toàn bộ ứng dụng Lexora.",
            ["Settings_Audio"] = "Giọng đọc & Phát âm",
            ["Settings_AudioSubtitle"] = "Cấu hình âm thanh phát âm từ vựng chuẩn Windows Media TTS.",
            ["Settings_Accent"] = "Chất giọng mặc định",
            ["Settings_Speed"] = "Tốc độ đọc",
            ["Settings_Data"] = "Dữ liệu & Sao lưu",
            ["Settings_DataSubtitle"] = "Quản lý cơ sở dữ liệu SQLite, tạo bản sao lưu an toàn hoặc khôi phục dữ liệu.",
            ["Settings_BackupNow"] = "Sao lưu CSDL ngay",
            ["Settings_RestoreBackup"] = "Khôi phục từ bản sao lưu...",
            ["Settings_Shortcuts"] = "Bảng phím tắt nhanh"
        },
        ["en-US"] = new Dictionary<string, string>
        {
            // App & Shell Navigation
            ["AppTitle"] = "Lexora - English Learning Manager",
            ["Nav_Dashboard"] = "Dashboard",
            ["Nav_Vocabulary"] = "Vocabulary",
            ["Nav_Review"] = "Spaced Review",
            ["Nav_Test"] = "Practice & Test",
            ["Nav_Grammar"] = "Grammar",
            ["Nav_Settings"] = "Settings",
            ["Search_Placeholder"] = "Search words, grammar... (Ctrl+F)",
            ["Review_Reminder_Title"] = "Review Reminder",
            ["Review_Now"] = "Review Now",

            // Common Actions
            ["Action_Edit"] = "Edit",
            ["Action_Delete"] = "Delete",
            ["Action_Save"] = "Save",
            ["Action_Cancel"] = "Cancel",
            ["Action_Close"] = "Close",
            ["Action_Reset"] = "Reset",
            ["Action_Search"] = "Search",
            ["Action_Filter"] = "Filters",

            // Dashboard
            ["Dashboard_HeroTitle"] = "Spaced Repetition (SM-2)",
            ["Dashboard_HeroGreeting"] = "Ready to conquer your vocabulary goals today?",
            ["Dashboard_StartReview"] = "Start Today's Review",
            ["Dashboard_QuickPractice"] = "Quick Practice Test",
            ["Dashboard_TotalWords"] = "Total Vocabulary",
            ["Dashboard_DueToday"] = "Due For Review",
            ["Dashboard_Mastered"] = "Mastered Words",
            ["Dashboard_Streak"] = "Day Streak",
            ["Dashboard_DaysStreak"] = "days streak",
            ["Dashboard_RetentionChart"] = "Memory Retention Curve (SM-2)",
            ["Dashboard_ActivityHeatmap"] = "Activity Heatmap (Last 30 Days)",
            ["Dashboard_EmptyActivity"] = "No recent activity recorded. Complete a review to light up your streak!",
            ["Dashboard_RetentionDistribution"] = "Memory Retention Distribution (SM-2)",
            ["Dashboard_RetentionSubtitle"] = "Distribution of vocabulary across spaced repetition learning stages",
            ["Dashboard_OverallAccuracy"] = "Overall Accuracy:",
            ["Dashboard_ExamTargetProgress"] = "Exam Target Progress",
            ["Dashboard_ExamTotal"] = "Total",
            ["Dashboard_ExamLearning"] = "Learning",
            ["Dashboard_ExamMastered"] = "Mastered",
            ["Dashboard_HeatmapTitle"] = "Daily Study Heatmap (Last 30 Days)",
            ["Dashboard_HeatmapSubtitle"] = "Track your daily study consistency and review habit over time",
            ["Dashboard_HeatmapLess"] = "Less",
            ["Dashboard_HeatmapMore"] = "More",
            ["Dashboard_ReviewedToday"] = "Reviewed Today:",
            ["Dashboard_CurrentStreak"] = "Current Streak:",
            ["Dashboard_BestStreak"] = "Best Streak:",
            ["Dashboard_DaysUnit"] = "days",
            ["Dashboard_CefrBreakdown"] = "CEFR Level Breakdown",
            ["Dashboard_PosBreakdown"] = "Part of Speech Breakdown",
            ["Dashboard_RecentSessions"] = "Recent Learning & Test Sessions",
            ["Dashboard_SessionCards"] = "Cards:",
            ["Dashboard_SessionCorrect"] = "Correct:",

            // Vocabulary Hub
            ["Vocab_Title"] = "Vocabulary Hub",
            ["Vocab_ViewList"] = "Word List",
            ["Vocab_ViewUnits"] = "Context Units",
            ["Vocab_SearchPlaceholder"] = "Search words, phonetics, definitions... (Ctrl+F)",
            ["Vocab_AddWord"] = "Add Word",
            ["Vocab_ImportExport"] = "Import / Export",
            ["Vocab_ExportJson"] = "Export to JSON",
            ["Vocab_ExportCsv"] = "Export to CSV",
            ["Vocab_ImportJson"] = "Import from JSON...",
            ["Vocab_ImportCsv"] = "Import from CSV...",
            ["Vocab_TopicFilter"] = "Unit / Topic",
            ["Vocab_LevelFilter"] = "CEFR Level",
            ["Vocab_PosFilter"] = "Part of Speech",
            ["Vocab_StatusFilter"] = "Review Status",
            ["Vocab_SortBy"] = "Sort by",
            ["Vocab_SortDefault"] = "Default (Newest)",
            ["Vocab_SortAlpha"] = "Alphabetical (A - Z)",
            ["Vocab_SortDueDate"] = "Sort by Due Date",
            ["Vocab_AllTopics"] = "All Topics",
            ["Vocab_AllLevels"] = "All CEFR Levels",
            ["Vocab_AllPos"] = "All Parts of Speech",
            ["Vocab_AllStatus"] = "All Statuses",
            ["Vocab_WordsCount"] = "words",
            ["Vocab_EmptyTitle"] = "No vocabulary found",
            ["Vocab_EmptySubtitle"] = "Try clearing your search filters or add a new word to get started.",
            ["Vocab_SelectPromptTitle"] = "Select a Word",
            ["Vocab_SelectPromptSubtitle"] = "Choose a word on the left to inspect Oxford/Cambridge definitions, phonetics, audio, and examples.",
            ["Vocab_DefinitionVi"] = "Vietnamese Definition",
            ["Vocab_DefinitionEn"] = "English Definition",
            ["Vocab_Examples"] = "Contextual Examples",
            ["Vocab_Notes"] = "Usage Notes & Collocations",
            ["Vocab_Sm2Status"] = "SM-2 Algorithm Status",
            ["Vocab_EaseFactor"] = "Ease Factor (EF)",
            ["Vocab_Interval"] = "Interval",
            ["Vocab_NextReview"] = "Next Review",

            // Spaced Review
            ["Review_CardProgress"] = "Card",
            ["Review_FlipCard"] = "Press Spacebar or Click to Flip Card",
            ["Review_CompletedTitle"] = "Today's Review Completed! 🎉",
            ["Review_CompletedSubtitle"] = "Awesome! You've reviewed all due words scheduled for today according to SM-2.",
            ["Review_BackDashboard"] = "Back to Dashboard",
            ["Review_TodayDue"] = "Today's Due",
            ["Review_LearnNew"] = "Learn New Words",
            ["Review_Difficult"] = "Difficult & Lapses",
            ["Review_TypingDrill"] = "Typing Drill",
            ["Review_Remaining"] = "Remaining:",
            ["Review_AllCaughtUp"] = "All Caught Up! 🎉",
            ["Review_AllCaughtUpDesc"] = "You have 0 pending reviews in this queue. Excellent job keeping your review streak active!",
            ["Review_ShowMeaning"] = "Show Meaning (Space)",
            ["Review_RatingAgain"] = "Again",
            ["Review_RatingHard"] = "Hard",
            ["Review_RatingGood"] = "Good",
            ["Review_RatingEasy"] = "Easy",
            ["Review_SessionStats"] = "Session Stats",
            ["Review_Done"] = "Done",
            ["Review_Left"] = "Left",
            ["Action_NotRemembered"] = "Again",
            ["Action_Hard"] = "Hard",
            ["Action_Remembered"] = "Good",
            ["Action_Easy"] = "Easy",

            // Practice & Test
            ["Test_Title"] = "Practice & Test",
            ["Test_Subtitle"] = "Choose a question format and length to test your retention",
            ["Test_StartTest"] = "Start Test",
            ["Test_ExitTest"] = "Exit Test",
            ["Test_Question"] = "Question",
            ["Test_SubmitAnswer"] = "Submit Answer",
            ["Test_NextQuestion"] = "Next Question",
            ["Test_FinishTest"] = "Finish Test",
            ["Test_ResultTitle"] = "Test Results",
            ["Test_RetakeTest"] = "Retake Test",
            ["Test_ReviewWeakWords"] = "Review Weak Words",

            // Grammar
            ["Grammar_Title"] = "Grammar Master",
            ["Grammar_SearchPlaceholder"] = "Search grammar rules, structures, formulas... (Ctrl+F)",
            ["Grammar_AddRule"] = "Add Rule",
            ["Grammar_Practice"] = "Practice",
            ["Grammar_Structures"] = "Grammar Structures",
            ["Grammar_EmptyTitle"] = "No grammar rules found",
            ["Grammar_EmptySubtitle"] = "Try clearing search or filters to see available grammar lessons.",
            ["Grammar_SelectPromptTitle"] = "Select a Grammar Rule",
            ["Grammar_SelectPromptSubtitle"] = "Choose any rule on the left to inspect formula, explanations, contextual examples, and pitfalls.",
            ["Grammar_StructureFormula"] = "Structure Formula",
            ["Grammar_Explanation"] = "Detailed Explanation",
            ["Grammar_CommonMistakes"] = "⚠️ Common Mistakes & Pitfalls",
            ["Grammar_Examples"] = "Contextual Examples",
            ["Grammar_ExitPractice"] = "Exit Practice",

            // Settings
            ["Settings_Title"] = "Application Settings",
            ["Settings_Theme"] = "Appearance Theme",
            ["Settings_ThemeSubtitle"] = "Choose light, dark or follow your Windows operating system setting.",
            ["Settings_Theme_System"] = "Use system setting",
            ["Settings_Theme_Light"] = "Light Mode",
            ["Settings_Theme_Dark"] = "Dark Mode",
            ["Settings_Language"] = "App Language",
            ["Settings_LanguageSubtitle"] = "Select the interface display language for Lexora application.",
            ["Settings_Audio"] = "Audio & Speech",
            ["Settings_AudioSubtitle"] = "Configure pronunciation voice output using Windows Media TTS.",
            ["Settings_Accent"] = "Default Voice Accent",
            ["Settings_Speed"] = "Speech Speed",
            ["Settings_Data"] = "Data & Backup",
            ["Settings_DataSubtitle"] = "Manage SQLite database, create secure backups or restore existing data.",
            ["Settings_BackupNow"] = "Backup Database Now",
            ["Settings_RestoreBackup"] = "Restore Backup...",
            ["Settings_Shortcuts"] = "Keyboard Shortcuts Guide"
        }
    };

    private readonly Lexora.Infrastructure.Services.IUserPreferencesService _preferencesService;

    public LocalizationService(Lexora.Infrastructure.Services.IUserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
        var prefs = _preferencesService.GetPreferences();
        var savedLang = prefs.LanguageCode;
        if (_resources.ContainsKey(savedLang))
        {
            _currentLanguage = savedLang;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(savedLang);
                CultureInfo.CurrentCulture = new CultureInfo(savedLang);
            }
            catch { /* fallback to default */ }
        }
    }

    public string CurrentLanguage => _currentLanguage;
    public event EventHandler? LanguageChanged;

    public string this[string key] => GetString(key);

    public void SetLanguage(string languageCode)
    {
        if (_resources.ContainsKey(languageCode))
        {
            _currentLanguage = languageCode;
            CultureInfo.CurrentUICulture = new CultureInfo(languageCode);
            CultureInfo.CurrentCulture = new CultureInfo(languageCode);

            var currentPrefs = _preferencesService.GetPreferences();
            if (currentPrefs.LanguageCode != languageCode)
            {
                _ = _preferencesService.SavePreferencesAsync(currentPrefs with { LanguageCode = languageCode });
            }

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
