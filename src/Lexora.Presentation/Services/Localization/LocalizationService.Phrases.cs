namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterPhrases(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Phrases — vi-VN (Tiếng Việt)
        // ==========================================
        vi["Nav_Phrases"] = "Mẫu câu giao tiếp";
        vi["Shell_TooltipPhrases"] = "Mẫu câu chào hỏi & Ngữ cảnh giao tiếp (Ctrl+Shift+P)";
        vi["Phrases_Title"] = "Mẫu câu & Ngữ cảnh giao tiếp";
        vi["Phrases_Subtitle"] = "Kho câu chào hỏi, đối thoại tự nhiên, câu trả lời mẫu & gợi ý mở rộng cho TOEIC, IELTS, VSTEP";
        vi["Phrases_AddNew"] = "Thêm mẫu câu";
        vi["Phrases_SearchPlaceholder"] = "Tìm kiếm câu mở đầu, câu trả lời, dịch nghĩa, danh mục...";
        vi["Phrases_FilterCategory"] = "Danh mục";
        vi["Phrases_FilterAllCategories"] = "Tất cả danh mục";
        vi["Phrases_FilterLevel"] = "Cấp độ CEFR";
        vi["Phrases_FilterAllLevels"] = "Mọi cấp độ";
        vi["Phrases_OnlyFavorites"] = "Yêu thích";
        vi["Phrases_EmptyTitle"] = "Chưa tìm thấy mẫu câu phù hợp";
        vi["Phrases_EmptySubtitle"] = "Thử điều chỉnh từ khóa tìm kiếm, bộ lọc danh mục hoặc tạo thêm mẫu câu giao tiếp mới";
        vi["Phrases_PromptSectionTitle"] = "Câu mở đầu / Chào hỏi tình huống";
        vi["Phrases_ResponseSectionTitle"] = "Câu trả lời mẫu tự nhiên";
        vi["Phrases_SuggestionsSectionTitle"] = "Gợi ý mở rộng & Điểm lưu ý ngữ cảnh";
        vi["Phrases_PlayAudioTooltip"] = "Nghe phát âm tự nhiên";
        vi["Phrases_CopyTooltip"] = "Sao chép câu";
        vi["Phrases_CopiedToast"] = "Đã sao chép vào khay nhớ tạm!";
        vi["Phrases_CreateDialogTitle"] = "Thêm mẫu câu giao tiếp mới";
        vi["Phrases_EditDialogTitle"] = "Chỉnh sửa mẫu câu giao tiếp";
        vi["Phrases_ConfirmDeleteTitle"] = "Xác nhận xóa mẫu câu";
        vi["Phrases_ConfirmDeleteMessage"] = "Bạn có chắc chắn muốn xóa mẫu câu này không? Thao tác này không thể hoàn tác.";
        vi["Phrases_ValidationPromptRequired"] = "Vui lòng nhập câu mở đầu / chào hỏi bằng tiếng Anh.";
        vi["Phrases_ValidationPromptTransRequired"] = "Vui lòng nhập dịch nghĩa tiếng Việt cho câu mở đầu.";
        vi["Phrases_ValidationResponseRequired"] = "Vui lòng nhập câu trả lời mẫu bằng tiếng Anh.";
        vi["Phrases_ValidationResponseTransRequired"] = "Vui lòng nhập dịch nghĩa tiếng Việt cho câu trả lời mẫu.";
        vi["Phrases_ValidationCategoryRequired"] = "Vui lòng chọn hoặc nhập danh mục / ngữ cảnh.";
        vi["Phrases_ToastCreated"] = "Đã thêm mẫu câu mới thành công!";
        vi["Phrases_ToastUpdated"] = "Đã cập nhật mẫu câu thành công!";
        vi["Phrases_ToastDeleted"] = "Đã xóa mẫu câu thành công!";
        vi["Phrases_EditorCategoryHeader"] = "Danh mục ngữ cảnh";
        vi["Phrases_EditorCategoryPlaceholder"] = "VD: Chào hỏi & Làm quen, Công sở, Phỏng vấn...";
        vi["Phrases_EditorTagHeader"] = "Thẻ tình huống (Subtag)";
        vi["Phrases_EditorTagPlaceholder"] = "VD: Lịch hẹn, Phỏng vấn xin việc, Đặt phòng...";
        vi["Phrases_EditorLevelHeader"] = "Cấp độ CEFR";
        vi["Phrases_EditorPromptHeader"] = "Câu mở đầu / Câu hỏi tình huống (English)";
        vi["Phrases_EditorPromptPlaceholder"] = "VD: Good morning! How is your project coming along?...";
        vi["Phrases_EditorPromptTransHeader"] = "Dịch nghĩa câu mở đầu (Tiếng Việt)";
        vi["Phrases_EditorPromptTransPlaceholder"] = "VD: Chào buổi sáng! Dự án của bạn tiến triển thế nào rồi?...";
        vi["Phrases_EditorResponseHeader"] = "Câu trả lời mẫu tự nhiên (English)";
        vi["Phrases_EditorResponsePlaceholder"] = "VD: It's progressing quite well, thanks! We finalized the first phase...";
        vi["Phrases_EditorResponseTransHeader"] = "Dịch nghĩa câu trả lời (Tiếng Việt)";
        vi["Phrases_EditorResponseTransPlaceholder"] = "VD: Mọi thứ đang tiến triển rất tốt, cảm ơn bạn! Chúng tôi đã hoàn thành giai đoạn một...";
        vi["Phrases_EditorSuggestionsHeader"] = "Gợi ý mở rộng & Lưu ý văn hóa/ngữ pháp";
        vi["Phrases_EditorSuggestionsPlaceholder"] = "Cách nói trang trọng, cách nói thân mật, từ vựng thay thế, câu hỏi tiếp nối...";
        vi["Phrases_QuickSuggestionsTitle"] = "Chèn nhanh gợi ý bổ sung:";
        vi["Phrases_InsertFormal"] = "+ Trang trọng (Formal)";
        vi["Phrases_InsertCasual"] = "+ Thân mật (Casual)";
        vi["Phrases_InsertSynonyms"] = "+ Từ vựng nâng cấp";
        vi["Phrases_InsertFollowUp"] = "+ Câu hỏi mở rộng";
        vi["Phrases_TotalCount"] = "Hiển thị {0} mẫu câu";
        vi["Phrases_ActionEdit"] = "Chỉnh sửa";
        vi["Phrases_ActionDelete"] = "Xóa";
        vi["Phrases_CategoryAll"] = "Tất cả danh mục";

        // ==========================================
        // Phrases — en-US (English)
        // ==========================================
        en["Nav_Phrases"] = "Conversation Phrases";
        en["Shell_TooltipPhrases"] = "Conversation Phrases & Scenarios (Ctrl+Shift+P)";
        en["Phrases_Title"] = "Conversation Phrases & Scenarios";
        en["Phrases_Subtitle"] = "Curated greetings, natural dialogues, sample responses & extended tips for TOEIC, IELTS, VSTEP";
        en["Phrases_AddNew"] = "Add Phrase";
        en["Phrases_SearchPlaceholder"] = "Search prompt, response, translation, category...";
        en["Phrases_FilterCategory"] = "Category";
        en["Phrases_FilterAllCategories"] = "All Categories";
        en["Phrases_FilterLevel"] = "CEFR Level";
        en["Phrases_FilterAllLevels"] = "All Levels";
        en["Phrases_OnlyFavorites"] = "Favorites";
        en["Phrases_EmptyTitle"] = "No matching phrases found";
        en["Phrases_EmptySubtitle"] = "Try adjusting your search terms, category filters, or create a new conversation phrase";
        en["Phrases_PromptSectionTitle"] = "Opening Prompt / Greeting";
        en["Phrases_ResponseSectionTitle"] = "Natural Sample Response";
        en["Phrases_SuggestionsSectionTitle"] = "Extended Suggestions & Context Notes";
        en["Phrases_PlayAudioTooltip"] = "Listen to pronunciation";
        en["Phrases_CopyTooltip"] = "Copy phrase";
        en["Phrases_CopiedToast"] = "Copied to clipboard!";
        en["Phrases_CreateDialogTitle"] = "Add New Conversation Phrase";
        en["Phrases_EditDialogTitle"] = "Edit Conversation Phrase";
        en["Phrases_ConfirmDeleteTitle"] = "Confirm Delete Phrase";
        en["Phrases_ConfirmDeleteMessage"] = "Are you sure you want to delete this conversation phrase? This action cannot be undone.";
        en["Phrases_ValidationPromptRequired"] = "Please enter the English prompt/greeting sentence.";
        en["Phrases_ValidationPromptTransRequired"] = "Please enter the Vietnamese translation for the opening sentence.";
        en["Phrases_ValidationResponseRequired"] = "Please enter the English sample response.";
        en["Phrases_ValidationResponseTransRequired"] = "Please enter the Vietnamese translation for the sample response.";
        en["Phrases_ValidationCategoryRequired"] = "Please select or enter a context category.";
        en["Phrases_ToastCreated"] = "New conversation phrase added successfully!";
        en["Phrases_ToastUpdated"] = "Conversation phrase updated successfully!";
        en["Phrases_ToastDeleted"] = "Conversation phrase deleted successfully!";
        en["Phrases_EditorCategoryHeader"] = "Context Category";
        en["Phrases_EditorCategoryPlaceholder"] = "E.g. Greetings & Introductions, Workplace, Interviews...";
        en["Phrases_EditorTagHeader"] = "Context Subtag";
        en["Phrases_EditorTagPlaceholder"] = "E.g. Appointments, Job Interview, Hotel Booking...";
        en["Phrases_EditorLevelHeader"] = "CEFR Level";
        en["Phrases_EditorPromptHeader"] = "Opening Prompt / Situational Question (English)";
        en["Phrases_EditorPromptPlaceholder"] = "E.g. Good morning! How is your project coming along?...";
        en["Phrases_EditorPromptTransHeader"] = "Prompt Translation (Vietnamese)";
        en["Phrases_EditorPromptTransPlaceholder"] = "E.g. Chào buổi sáng! Dự án của bạn tiến triển thế nào rồi?...";
        en["Phrases_EditorResponseHeader"] = "Natural Sample Response (English)";
        en["Phrases_EditorResponsePlaceholder"] = "E.g. It's progressing quite well, thanks! We finalized the first phase...";
        en["Phrases_EditorResponseTransHeader"] = "Response Translation (Vietnamese)";
        en["Phrases_EditorResponseTransPlaceholder"] = "E.g. Mọi thứ đang tiến triển rất tốt, cảm ơn bạn! Chúng tôi đã hoàn thành giai đoạn một...";
        en["Phrases_EditorSuggestionsHeader"] = "Extended Suggestions & Cultural/Grammar Nuances";
        en["Phrases_EditorSuggestionsPlaceholder"] = "Formal variants, casual phrasings, synonyms, follow-up questions...";
        en["Phrases_QuickSuggestionsTitle"] = "Quick Suggestion Inserts:";
        en["Phrases_InsertFormal"] = "+ Formal Variant";
        en["Phrases_InsertCasual"] = "+ Casual Phrasing";
        en["Phrases_InsertSynonyms"] = "+ Synonyms & Vocab";
        en["Phrases_InsertFollowUp"] = "+ Follow-up Question";
        en["Phrases_TotalCount"] = "Showing {0} phrases";
        en["Phrases_ActionEdit"] = "Edit";
        en["Phrases_ActionDelete"] = "Delete";
        en["Phrases_CategoryAll"] = "All Categories";
    }
}
