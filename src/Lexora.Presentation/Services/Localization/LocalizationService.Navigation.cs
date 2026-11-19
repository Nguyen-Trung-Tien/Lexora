namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterNavigation(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Navigation — vi-VN (Tiếng Việt)
        // ==========================================
        // Navigation Headers
        vi["NavHeader_Overview"] = "TỔNG QUAN";
        vi["NavHeader_Workspaces"] = "KHÔNG GIAN HỌC";
        vi["NavHeader_Learning"] = "HỌC TẬP & KHO TỪ";
        vi["NavHeader_Practice"] = "LUYỆN TẬP & ĐÁNH GIÁ";
        vi["NavHeader_Archive"] = "KHO LƯU TRỮ & DỮ LIỆU";
        vi["Nav_Dashboard"] = "Hôm nay";
        vi["Nav_Roadmap"] = "Lộ trình học tập";
        vi["Nav_KnowledgeStudio"] = "Kho Kiến Thức";
        vi["Nav_PracticeArena"] = "Phòng Luyện Tập";
        vi["Nav_Vocabulary"] = "Từ vựng";
        vi["Nav_Homophones"] = "Từ đồng âm";
        vi["Nav_Review"] = "Ôn tập ngắt quãng";
        vi["Nav_Test"] = "Luyện tập & Thi";
        vi["Nav_Grammar"] = "Ngữ pháp";
        vi["Nav_Phrases"] = "Cụm từ giao tiếp";
        // Learning Storage (Kho lưu trữ học tập)
        vi["Nav_LearningStorage"] = "Kho lưu trữ";
        vi["Nav_Help"] = "Trung tâm hướng dẫn";
        vi["Nav_Settings"] = "Cài đặt";
        vi["Nav_ThemeLight"] = "Chế độ Sáng";
        vi["Nav_ThemeDark"] = "Chế độ Tối";
        vi["Nav_ThemeToggle"] = "Chuyển giao diện";
        vi["Shell_TooltipDashboard"] = "Bảng tổng quan (Ctrl+1)";
        vi["Shell_TooltipRoadmap"] = "Lộ trình học thích ứng & Cây kỹ năng (Ctrl+9)";
        vi["Shell_TooltipVocabulary"] = "Kho từ vựng & Tra cứu (Ctrl+2)";
        vi["Shell_TooltipHomophones"] = "Từ đồng âm & Cặp âm dễ nhầm (Ctrl+3)";
        vi["Shell_TooltipGrammar"] = "Sổ tay ngữ pháp (Ctrl+5)";
        vi["Shell_TooltipPhrases"] = "Cụm từ giao tiếp & Phản xạ (Ctrl+Shift+P)";
        vi["Shell_TooltipReview"] = "Ôn tập ngắt quãng SM-2 (Ctrl+6)";
        vi["Shell_TooltipTest"] = "Luyện tập & Kiểm tra (Ctrl+7)";
        vi["Shell_TooltipLearningStorage"] = "Kho lưu trữ ghi nhớ (Ctrl+8)";
        vi["Shell_TooltipHelp"] = "Trung tâm hướng dẫn & Phím tắt (F1)";
        vi["Shell_TooltipSettings"] = "Cài đặt hệ thống";
        vi["Shell_SearchTooltip"] = "Tìm kiếm thông minh (Ctrl+K)";
        vi["Shell_TooltipThemeToggleLight"] = "Chuyển sang Chế độ Sáng (Ctrl+T)";
        vi["Shell_TooltipThemeToggleDark"] = "Chuyển sang Chế độ Tối (Ctrl+T)";
        vi["Search_Placeholder"] = "Tìm kiếm thông minh (Ctrl+K)...";
        vi["Search_SmartNav_Homophones"] = "Phân biệt từ phát âm tương đồng & Mẹo tránh bẫy";
        vi["Search_NoResults"] = "Không tìm thấy kết quả phù hợp";
        vi["Search_ClosestSuggestions"] = "Gợi ý gần nhất:";
        vi["Search_SmartNav_Dashboard"] = "Trang chủ & Bảng tổng quan";
        vi["Search_SmartNav_Vocab"] = "Kho từ vựng & Tra cứu chi tiết";
        vi["Search_SmartNav_Review"] = "Ôn tập Flashcard ngắt quãng SM-2";
        vi["Search_SmartNav_Test"] = "Kiểm tra trắc nghiệm & Luyện tập";
        vi["Search_SmartNav_Grammar"] = "Sổ tay & Cấu trúc ngữ pháp";
        vi["Search_SmartNav_Settings"] = "Cài đặt hệ thống & Tùy chọn";
        vi["Search_GrammarRule_Prefix"] = "[Ngữ pháp]";
        // Newly Added Shell Navigation & Tooltips
        vi["Shell_SearchPlaceholder"] = "Tìm kiếm thông minh (Ctrl+K)...";
        // Global Search Quick Navigation
        vi["Search_SmartNav_LearningStorage"] = "Kho lưu trữ từ vựng & ngữ pháp yêu thích";
        // Vocabulary Knowledge Graph
        vi["Nav_KnowledgeGraph"] = "Mạng lưới Ngữ nghĩa";
        vi["Nav_ListeningLab"] = "Luyện phát âm & Nghe";
        // Command Palette
        vi["Palette_Placeholder"] = "Tìm từ vựng hoặc điều hướng nhanh... (Ctrl+K)";
        vi["Palette_FooterHint"] = "↑↓ Duyệt  •  Enter Chọn  •  Esc Đóng";
        vi["Palette_SectionActions"] = "Lệnh & Chức năng";
        vi["Palette_SectionVocabulary"] = "Từ vựng";
        vi["Palette_SectionGrammar"] = "Ngữ pháp";
        vi["Palette_DictationLab"] = "Luyện nghe chép chính tả (Dictation Lab)";
        vi["Palette_NoResults"] = "Không tìm thấy kết quả phù hợp";

        // ==========================================
        // Navigation — en-US (English)
        // ==========================================
        // Navigation Headers
        en["NavHeader_Overview"] = "OVERVIEW";
        en["NavHeader_Workspaces"] = "WORKSPACES";
        en["NavHeader_Learning"] = "LEARNING & VOCABULARY";
        en["NavHeader_Practice"] = "PRACTICE & EVALUATION";
        en["NavHeader_Archive"] = "STORAGE & ARCHIVE";
        en["Nav_Dashboard"] = "Today";
        en["Nav_Roadmap"] = "Learning Roadmap";
        en["Nav_KnowledgeStudio"] = "Knowledge Studio";
        en["Nav_PracticeArena"] = "Practice Arena";
        en["Nav_Vocabulary"] = "Vocabulary";
        en["Nav_Homophones"] = "Homophones";
        en["Nav_Review"] = "Spaced Review";
        en["Nav_Test"] = "Practice & Test";
        en["Nav_Grammar"] = "Grammar";
        en["Nav_Phrases"] = "Conversation Phrases";
        // Learning Storage
        en["Nav_LearningStorage"] = "Learning Storage";
        en["Nav_Help"] = "Help & Guide";
        en["Nav_Settings"] = "Settings";
        en["Nav_ThemeLight"] = "Light Mode";
        en["Nav_ThemeDark"] = "Dark Mode";
        en["Nav_ThemeToggle"] = "Toggle Theme";
        en["Shell_TooltipDashboard"] = "Dashboard (Ctrl+1)";
        en["Shell_TooltipRoadmap"] = "Adaptive Roadmap & Skill Tree (Ctrl+9)";
        en["Shell_TooltipVocabulary"] = "Vocabulary & Lookup (Ctrl+2)";
        en["Shell_TooltipHomophones"] = "Homophones & Sound-Alikes (Ctrl+3)";
        en["Shell_TooltipGrammar"] = "Grammar Handbook (Ctrl+5)";
        en["Shell_TooltipPhrases"] = "Conversation Phrases & Shadowing (Ctrl+Shift+P)";
        en["Shell_TooltipReview"] = "Spaced Repetition Review (Ctrl+6)";
        en["Shell_TooltipTest"] = "Practice & Test (Ctrl+7)";
        en["Shell_TooltipLearningStorage"] = "Learning Storage (Ctrl+8)";
        en["Shell_TooltipHelp"] = "Help & Guide (F1)";
        en["Shell_TooltipSettings"] = "Application Settings";
        en["Shell_SearchTooltip"] = "Smart Search (Ctrl+K)";
        en["Shell_TooltipThemeToggleLight"] = "Switch to Light Mode (Ctrl+T)";
        en["Shell_TooltipThemeToggleDark"] = "Switch to Dark Mode (Ctrl+T)";
        en["Search_Placeholder"] = "Smart search (Ctrl+K)...";
        en["Search_SmartNav_Homophones"] = "Distinguish confusing sound-alikes & phonetic traps";
        en["Search_NoResults"] = "No matching results found";
        en["Search_ClosestSuggestions"] = "Closest suggestions:";
        en["Search_SmartNav_Dashboard"] = "Home & Dashboard Overview";
        en["Search_SmartNav_Vocab"] = "Vocabulary Repository & Search";
        en["Search_SmartNav_Review"] = "Spaced Repetition Review (SM-2)";
        en["Search_SmartNav_Test"] = "Quiz & Knowledge Practice";
        en["Search_SmartNav_Grammar"] = "Grammar Handbook & Structures";
        en["Search_SmartNav_Settings"] = "System Settings & Preferences";
        en["Search_GrammarRule_Prefix"] = "[Grammar]";
        // Newly Added Shell Navigation & Tooltips
        en["Shell_SearchPlaceholder"] = "Smart search (Ctrl+K)...";
        // Global Search Quick Navigation
        en["Search_SmartNav_LearningStorage"] = "Saved vocabulary & grammar learning storage";
        // Vocabulary Knowledge Graph
        en["Nav_KnowledgeGraph"] = "Knowledge Graph";
        en["Nav_ListeningLab"] = "Listening Lab";
        // Command Palette
        en["Palette_Placeholder"] = "Search vocabulary or quick navigate... (Ctrl+K)";
        en["Palette_FooterHint"] = "↑↓ Navigate  •  Enter Select  •  Esc Close";
        en["Palette_SectionActions"] = "Commands & Actions";
        en["Palette_SectionVocabulary"] = "Vocabulary";
        en["Palette_SectionGrammar"] = "Grammar";
        en["Palette_DictationLab"] = "Audio Dictation Lab";
        en["Palette_NoResults"] = "No matching commands or words found";
    }
}
