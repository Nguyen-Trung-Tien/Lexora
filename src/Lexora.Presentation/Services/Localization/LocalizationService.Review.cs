namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterReview(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Review — vi-VN (Tiếng Việt)
        // ==========================================
        vi["Review_Reminder_Title"] = "Nhắc nhở ôn tập";
        vi["Review_Now"] = "Ôn tập ngay";
        vi["Review_RetentionLabel"] = "Độ bền ghi nhớ";
        vi["Review_PredictedInterval"] = "Tái xuất hiện sau";
        // Spaced Review
        vi["Review_Undo"] = "Hoàn tác";
        vi["Review_UndoTooltip"] = "Hoàn tác đánh giá thẻ vừa rồi (Ctrl+Z)";
        vi["Review_UndoSuccess"] = "Đã hoàn tác đánh giá thẻ ôn tập vừa rồi.";
        vi["Review_CardProgress"] = "Thẻ ôn tập";
        vi["Review_FlipCard"] = "Nhấn Phím Cách (Space) hoặc Click để lật thẻ";
        vi["Review_CompletedTitle"] = "Hoàn Thành Ôn Tập Hôm Nay!";
        vi["Review_CompletedSubtitle"] = "Xuất sắc! Bạn đã ôn tập hết các thẻ từ vựng đến hạn hôm nay theo thuật toán SM-2.";
        vi["Review_BackDashboard"] = "Về Trang Chủ";
        vi["Review_TodayDue"] = "Cần ôn hôm nay";
        vi["Review_LearnNew"] = "Học từ mới";
        vi["Review_Difficult"] = "Từ khó & Lapses";
        vi["Review_TypingDrill"] = "Luyện gõ từ";
        vi["Review_Remaining"] = "Còn lại:";
        vi["Review_AllCaughtUp"] = "Đã hoàn thành hết!";
        vi["Review_AllCaughtUpDesc"] = "Bạn có 0 thẻ cần ôn trong danh sách này. Thật xuất sắc trong việc duy trì chuỗi học tập!";
        vi["Review_ShowMeaning"] = "Hiện nghĩa (Phím cách)";
        vi["Review_RatingAgain"] = "Quên (Again)";
        vi["Review_RatingHard"] = "Khó (Hard)";
        vi["Review_RatingGood"] = "Nhớ (Good)";
        vi["Review_RatingEasy"] = "Dễ (Easy)";
        vi["Review_FocusMode"] = "Tập trung";
        vi["Review_FocusModeTooltip"] = "Bật/Tắt chế độ ôn tập tập trung tối đa (Zen Mode)";
        vi["Review_RecentActivityTitle"] = "Hoạt động ôn tập gần đây";
        vi["Review_SessionStats"] = "Thống kê phiên";
        vi["Review_Done"] = "Đã ôn";
        vi["Review_Left"] = "Còn lại";
        // Shell & Reminders
        vi["Review_Reminder_Msg"] = "Bạn có {0} từ vựng đã đến hạn ôn tập ngắt quãng (SM-2).";
        // Review Active Session Titles
        vi["Review_Session_LearnNew"] = "Học từ vựng mới";
        vi["Review_Session_Difficult"] = "Từ vựng khó & Hay quên";
        vi["Review_Session_ByTopic"] = "Ôn tập theo Unit ngữ cảnh";
        vi["Review_Session_CustomSelection"] = "Ôn tập danh sách từ chọn lọc";
        vi["Review_Session_TodayDue"] = "Ôn tập đến hạn hôm nay";
        vi["Review_Session_SelectedCount"] = "Ôn {0} từ đã chọn";
        // Additional Review Workspace
        vi["Review_Progress"] = "Tiến độ: ";
        vi["Review_SpellingDrill"] = "Luyện gõ chính tả";
        vi["Review_ExampleContext"] = "Ví dụ ngữ cảnh:";
        vi["Review_PatternHint"] = "Gợi ý mẫu:";
        vi["Review_TypePlaceholder"] = "Nhập từ tiếng Anh...";
        vi["Review_BtnCheck"] = "Kiểm tra";
        vi["Review_BtnHint"] = "Gợi ý";
        vi["Review_BtnShowAnswer"] = "Hiện đáp án";
        vi["Review_AudioUk"] = "Phát âm (UK)";
        vi["Review_AudioUs"] = "Phát âm (US)";
        vi["Review_ReplayAudio"] = "Nghe lại phát âm (R)";
        vi["Review_DefinitionSection"] = "Định nghĩa & Ý nghĩa";
        vi["Review_Accuracy"] = "Độ chính xác";
        vi["Review_Sm2Insights"] = "Thông số ghi nhớ SM-2";
        vi["Review_StatusLabel"] = "Trạng thái:";
        vi["Review_IntervalLabel"] = "Khoảng cách ôn:";
        vi["Review_EaseFactorLabel"] = "Hệ số dễ (EF):";
        vi["Review_LapsesLabel"] = "Số lần quên:";
        vi["Review_HotkeysTitle"] = "Phím tắt thao tác";
        vi["Review_HotkeyFlip"] = "Phím cách: Lật thẻ";
        vi["Review_HotkeyRate"] = "1 - 4: Đánh giá độ nhớ";
        vi["Review_SessionCompletedTitle"] = "Hoàn thành phiên học!";
        vi["Review_SessionCompletedDesc"] = "Bạn đã ôn tập xuất sắc tất cả các thẻ trong phiên này!";
        vi["Review_TotalStudied"] = "Tổng số thẻ đã học";
        vi["Review_CorrectCount"] = "Số câu nhớ đúng";
        vi["Review_BtnMoreCards"] = "Ôn thêm thẻ khác";
        vi["Review_BtnLearnNew"] = "Học từ vựng mới";
        // Newly Added Review
        vi["Review_TypingFeedbackCorrect"] = "Chính xác! Hoàn hảo";
        vi["Review_TypingFeedbackIncorrectFormat"] = "Chưa chính xác: \"{0}\" ➔ Đáp án đúng: \"{1}\"";
        vi["Review_LettersCountFormat"] = "{0} ký tự";
        vi["Review_UnitPrefix"] = "Chủ đề";
        vi["Review_Session_ByTopicFormat"] = "Chủ đề: {0}";
        // Review Presets Dynamic Flyout
        vi["Review_PresetFlyoutTitle"] = "Bộ lọc Ôn tập Thông minh (Smart Presets)";
        vi["Review_PresetFlyoutSubtitle"] = "Chọn phương thức ôn tập tối ưu hoá theo mục tiêu hiện tại";
        vi["Review_CardsAvailableBadge"] = "{0} từ sẵn sàng";
        // Smart Review & Memory Stability
        vi["SmartReview_Presets"] = "Chế độ ôn tập thông minh";
        vi["SmartReview_PresetsTitle"] = "Chế độ Ôn tập Thông minh";
        vi["SmartReview_UrgentOverdue"] = "Cần ôn gấp (Quá hạn)";
        vi["SmartReview_HighForgettingRisk"] = "Nguy cơ quên cao (R < 70%)";
        vi["SmartReview_ExamHighYield"] = "Trọng tâm bài thi (High-Yield)";
        vi["SmartReview_FreshLearned"] = "Từ mới nạp (Củng cố)";
        vi["SmartReview_LongTermConsolidation"] = "Khắc sâu dài hạn";
        vi["SmartReview_MemoryStability"] = "Độ bền trí nhớ";
        vi["SmartReview_Retrievability"] = "Khả năng nhớ";
        vi["SmartReview_HealthPermanent"] = "Vĩnh viễn";
        vi["SmartReview_HealthStable"] = "Vững chắc";
        vi["SmartReview_HealthDeveloping"] = "Đang nhớ";
        vi["SmartReview_HealthFragile"] = "Dễ quên";
        vi["SmartReview_PredictedInterval"] = "Dự kiến chu kỳ tiếp theo";
        vi["SmartReview_Stability"] = "Độ bền trí nhớ";
        vi["SmartReview_HalfLife"] = "Chu kỳ bán rã";
        vi["Review_Help_Title"] = "Trợ giúp: Ôn tập Spaced Repetition";
        vi["Review_Help_Subtitle"] = "Quy tắc đánh giá 4 mức nhớ";
        vi["Review_Help_Content"] = "Nhấn Phím Cách (Space) để lật thẻ. Chọn 1 (Again: quên hẳn), 2 (Hard: nhớ khó khăn), 3 (Good: nhớ chuẩn), 4 (Easy: thuộc lòng). Dùng Ctrl+Z nếu đánh giá nhầm.";

        // ==========================================
        // Review — en-US (English)
        // ==========================================
        en["Review_Reminder_Title"] = "Review Reminder";
        en["Review_Now"] = "Review Now";
        en["Review_RetentionLabel"] = "Memory Retention";
        en["Review_PredictedInterval"] = "Next review in";
        // Spaced Review
        en["Review_Undo"] = "Undo";
        en["Review_UndoTooltip"] = "Undo last card rating (Ctrl+Z)";
        en["Review_UndoSuccess"] = "Last card rating undone successfully.";
        en["Review_CardProgress"] = "Card";
        en["Review_FlipCard"] = "Press Spacebar or Click to Flip Card";
        en["Review_CompletedTitle"] = "Today's Review Completed!";
        en["Review_CompletedSubtitle"] = "Awesome! You've reviewed all due words scheduled for today according to SM-2.";
        en["Review_BackDashboard"] = "Back to Dashboard";
        en["Review_TodayDue"] = "Today's Due";
        en["Review_LearnNew"] = "Learn New Words";
        en["Review_Difficult"] = "Difficult & Lapses";
        en["Review_TypingDrill"] = "Typing Drill";
        en["Review_Remaining"] = "Remaining:";
        en["Review_AllCaughtUp"] = "All Caught Up!";
        en["Review_AllCaughtUpDesc"] = "You have 0 pending reviews in this queue. Excellent job keeping your review streak active!";
        en["Review_ShowMeaning"] = "Show Meaning (Space)";
        en["Review_RatingAgain"] = "Again";
        en["Review_RatingHard"] = "Hard";
        en["Review_RatingGood"] = "Good";
        en["Review_RatingEasy"] = "Easy";
        en["Review_FocusMode"] = "Focus Mode";
        en["Review_FocusModeTooltip"] = "Toggle distraction-free Zen study mode";
        en["Review_RecentActivityTitle"] = "Recent Study Activity";
        en["Review_SessionStats"] = "Session Stats";
        en["Review_Done"] = "Done";
        en["Review_Left"] = "Left";
        // Shell & Reminders
        en["Review_Reminder_Msg"] = "You have {0} word(s) ready for spaced repetition review (SM-2).";
        // Review Active Session Titles
        en["Review_Session_LearnNew"] = "Learn New Words";
        en["Review_Session_Difficult"] = "Challenging & Lapsed Words";
        en["Review_Session_ByTopic"] = "Context Unit Review";
        en["Review_Session_CustomSelection"] = "Custom Selected Words";
        en["Review_Session_TodayDue"] = "Today's Due Reviews";
        en["Review_Session_SelectedCount"] = "Review {0} Selected Words";
        // Additional Review Workspace
        en["Review_Progress"] = "Progress: ";
        en["Review_SpellingDrill"] = "Spelling Drill";
        en["Review_ExampleContext"] = "Example Context:";
        en["Review_PatternHint"] = "Pattern hint:";
        en["Review_TypePlaceholder"] = "Type the English term...";
        en["Review_BtnCheck"] = "Check";
        en["Review_BtnHint"] = "Hint";
        en["Review_BtnShowAnswer"] = "Show Answer";
        en["Review_AudioUk"] = "Audio (UK)";
        en["Review_AudioUs"] = "Audio (US)";
        en["Review_ReplayAudio"] = "Replay Pronunciation (R)";
        en["Review_DefinitionSection"] = "Definition & Meaning";
        en["Review_Accuracy"] = "Accuracy";
        en["Review_Sm2Insights"] = "SM-2 Memory Insights";
        en["Review_StatusLabel"] = "Status:";
        en["Review_IntervalLabel"] = "Interval:";
        en["Review_EaseFactorLabel"] = "Ease Factor:";
        en["Review_LapsesLabel"] = "Lapses:";
        en["Review_HotkeysTitle"] = "Hotkeys";
        en["Review_HotkeyFlip"] = "Space: Flip card";
        en["Review_HotkeyRate"] = "1 - 4: Rate recall";
        en["Review_SessionCompletedTitle"] = "Session Completed!";
        en["Review_SessionCompletedDesc"] = "You've successfully reviewed all cards in this session!";
        en["Review_TotalStudied"] = "Total Studied";
        en["Review_CorrectCount"] = "Correct";
        en["Review_BtnMoreCards"] = "Review More Cards";
        en["Review_BtnLearnNew"] = "Learn New Words";
        // Newly Added Review
        en["Review_TypingFeedbackCorrect"] = "Correct! Well done";
        en["Review_TypingFeedbackIncorrectFormat"] = "Incorrect: \"{0}\" ➔ Correct answer: \"{1}\"";
        en["Review_LettersCountFormat"] = "{0} letters";
        en["Review_UnitPrefix"] = "Unit";
        en["Review_Session_ByTopicFormat"] = "Unit: {0}";
        // Review Presets Dynamic Flyout
        en["Review_PresetFlyoutTitle"] = "Smart Review Presets";
        en["Review_PresetFlyoutSubtitle"] = "Choose an optimized review mode tailored to your current learning goal";
        en["Review_CardsAvailableBadge"] = "{0} cards ready";
        // Smart Review & Memory Stability
        en["SmartReview_Presets"] = "Smart Presets";
        en["SmartReview_PresetsTitle"] = "Smart Review Presets";
        en["SmartReview_UrgentOverdue"] = "Urgent Overdue";
        en["SmartReview_HighForgettingRisk"] = "High Forgetting Risk (R < 70%)";
        en["SmartReview_ExamHighYield"] = "Exam High-Yield";
        en["SmartReview_FreshLearned"] = "Recently Learned (Consolidation)";
        en["SmartReview_LongTermConsolidation"] = "Long-Term Consolidation";
        en["SmartReview_MemoryStability"] = "Memory Stability";
        en["SmartReview_Retrievability"] = "Retrievability";
        en["SmartReview_HealthPermanent"] = "Permanent";
        en["SmartReview_HealthStable"] = "Stable";
        en["SmartReview_HealthDeveloping"] = "Developing";
        en["SmartReview_HealthFragile"] = "Fragile";
        en["SmartReview_PredictedInterval"] = "Predicted next interval";
        en["SmartReview_Stability"] = "Memory Stability";
        en["SmartReview_HalfLife"] = "Half-Life";
        en["Review_Help_Title"] = "Quick Help: Spaced Repetition";
        en["Review_Help_Subtitle"] = "4-Tier Rating Guidelines";
        en["Review_Help_Content"] = "Press Space to flip the card. Choose 1 (Again: complete blackout), 2 (Hard: hesitation), 3 (Good: standard recall), 4 (Easy: instant reflex). Press Ctrl+Z to undo mistakes.";
    }
}
