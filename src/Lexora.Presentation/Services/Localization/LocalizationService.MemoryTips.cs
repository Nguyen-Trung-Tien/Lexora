namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterMemoryTips(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Memory Tips & Mnemonics — vi-VN (Tiếng Việt)
        // ==========================================
        vi["Nav_MemoryTips"] = "Mẹo ghi nhớ & Làm bài";
        vi["Shell_TooltipMemoryTips"] = "Mẹo ghi nhớ & Mẹo làm bài (Ctrl+Shift+M)";

        vi["MemoryTips_Title"] = "Mẹo ghi nhớ & Bí kíp làm bài";
        vi["MemoryTips_Subtitle"] = "Kho câu thần chú, mẹo liên tưởng và bẫy đề thi kinh điển cho TOEIC, IELTS, VSTEP";
        vi["MemoryTips_StatTotal"] = "Tổng số mẹo:";
        vi["MemoryTips_StatPinned"] = "Đã ghim:";
        vi["MemoryTips_StatTraps"] = "Bẫy đề thi:";

        vi["MemoryTips_AddButton"] = "Thêm mẹo mới";
        vi["MemoryTips_SmartImport"] = "Nhập thông minh";
        vi["MemoryTips_SmartImport_Summary"] = "Đã nhập {0} mẹo mới, bỏ qua {1} mục trùng/lỗi.";
        vi["MemoryTips_SearchPlaceholder"] = "Tìm mẹo, câu thần chú, bẫy đề thi hoặc thẻ tag...";
        vi["MemoryTips_CategoryAll"] = "Mọi phân loại";
        vi["MemoryTips_TechniqueAll"] = "Mọi kỹ thuật nhớ";
        vi["MemoryTips_ExamAll"] = "Mọi kỳ thi";
        vi["MemoryTips_OnlyPinned"] = "Chỉ xem đã ghim";
        vi["MemoryTips_ResetFilters"] = "Đặt lại";
        vi["MemoryTips_FirstPage"] = "Trang đầu";
        vi["MemoryTips_PrevPage"] = "Trang trước";
        vi["MemoryTips_NextPage"] = "Trang sau";
        vi["MemoryTips_LastPage"] = "Trang cuối";
        vi["MemoryTips_PageSize"] = "Số lượng/trang:";
        vi["MemoryTips_PageSizeSuffix"] = "/ trang";
        vi["MemoryTips_ShowingRange"] = "Hiển thị {0}-{1} / {2} mẹo";

        vi["MemoryTips_ViewSplit"] = "Danh sách & Chi tiết";
        vi["MemoryTips_ViewGrid"] = "Lưới thẻ";
        vi["MemoryTips_TooltipSplit"] = "Xem dạng Danh sách & Chi tiết (Split View)";
        vi["MemoryTips_TooltipGrid"] = "Xem dạng Lưới thẻ (Grid View)";
        vi["MemoryTips_FilterAll"] = "Tất cả";
        vi["MemoryTips_FilterPinned"] = "Đã ghim";
        vi["MemoryTips_FilterTraps"] = "Mẹo thi & Bẫy";
        vi["MemoryTips_CopyHook"] = "Sao chép câu thần chú";
        vi["MemoryTips_CopySuccess"] = "Đã sao chép câu thần chú vào khay nhớ tạm!";
        vi["MemoryTips_NoSelectedTip"] = "Chọn một mẹo ghi nhớ ở danh sách bên trái để xem nội dung chi tiết";
        vi["MemoryTips_DetailHeader"] = "Chi tiết mẹo ghi nhớ";
        vi["MemoryTips_TipsCount"] = "mẹo";

        vi["MemoryTips_EmptyTitle"] = "Chưa tìm thấy mẹo ghi nhớ phù hợp";
        vi["MemoryTips_EmptySubtitle"] = "Thử thay đổi bộ lọc tìm kiếm hoặc nhấn 'Thêm mẹo mới' để lưu câu thần chú học tập của bạn.";

        vi["MemoryTips_HookLabel"] = "CÂU THẦN CHÚ / MẸO NHỚ NHANH";
        vi["MemoryTips_ExplanationLabel"] = "GIẢI THÍCH CHI TIẾT";
        vi["MemoryTips_ExampleLabel"] = "VÍ DỤ ÁP DỤNG";
        vi["MemoryTips_TrapLabel"] = "BẪY ĐỀ THI HAY GẶP / LƯU Ý";
        vi["MemoryTips_TagsLabel"] = "Thẻ tag:";

        vi["MemoryTips_Pin"] = "Ghim lên đầu";
        vi["MemoryTips_Unpin"] = "Bỏ ghim";
        vi["MemoryTips_Edit"] = "Chỉnh sửa";
        vi["MemoryTips_Delete"] = "Xóa mẹo";
        vi["MemoryTips_DeleteConfirmTitle"] = "Xác nhận xóa mẹo";
        vi["MemoryTips_DeleteConfirmMessage"] = "Bạn có chắc chắn muốn xóa mẹo ghi nhớ này không? Thao tác này không thể hoàn tác.";
        vi["MemoryTips_AddSuccess"] = "Thêm mẹo ghi nhớ thành công.";
        vi["MemoryTips_UpdateSuccess"] = "Cập nhật mẹo ghi nhớ thành công.";
        vi["MemoryTips_DeleteSuccess"] = "Xóa mẹo ghi nhớ thành công.";

        // Dialog Keys
        vi["MemoryTips_DialogAddTitle"] = "Thêm mẹo ghi nhớ mới";
        vi["MemoryTips_DialogEditTitle"] = "Chỉnh sửa mẹo ghi nhớ";
        vi["MemoryTips_FieldTitle"] = "Tiêu đề mẹo *";
        vi["MemoryTips_FieldTitlePlaceholder"] = "Ví dụ: Thứ tự tính từ trước danh từ (OpSASCOMP)";
        vi["MemoryTips_FieldCategory"] = "Phân loại *";
        vi["MemoryTips_FieldTechnique"] = "Kỹ thuật ghi nhớ *";
        vi["MemoryTips_FieldExamTarget"] = "Kỳ thi mục tiêu";
        vi["MemoryTips_FieldHook"] = "Câu thần chú / Mẹo nhớ nhanh *";
        vi["MemoryTips_FieldHookPlaceholder"] = "Ví dụ: Ông Sáu Ăn Súp Cua Ông Mập Phì";
        vi["MemoryTips_FieldExplanation"] = "Nội dung giải thích chi tiết *";
        vi["MemoryTips_FieldExplanationPlaceholder"] = "Giải thích chi tiết ý nghĩa, cơ chế và cách vận dụng mẹo...";
        vi["MemoryTips_FieldExample"] = "Ví dụ minh họa";
        vi["MemoryTips_FieldExamplePlaceholder"] = "Ví dụ: A lovely small old round black Vietnamese dining table...";
        vi["MemoryTips_FieldTrap"] = "Bẫy đề thi hay gặp / Lưu ý";
        vi["MemoryTips_FieldTrapPlaceholder"] = "Ví dụ: Đề thi TOEIC hay đổi chỗ Size và Color...";
        vi["MemoryTips_FieldTags"] = "Thẻ tag (phân tách bằng dấu phẩy)";
        vi["MemoryTips_FieldTagsPlaceholder"] = "Ví dụ: Adjectives, Grammar, TOEIC, VSTEP";
        vi["MemoryTips_FieldIsPinned"] = "Ghim mẹo này lên đầu danh sách";

        vi["MemoryTips_ValidationTitleRequired"] = "Vui lòng nhập tiêu đề mẹo ghi nhớ.";
        vi["MemoryTips_ValidationHookRequired"] = "Vui lòng nhập câu thần chú hoặc mẹo nhớ nhanh.";
        vi["MemoryTips_ValidationExplanationRequired"] = "Vui lòng nhập nội dung giải thích chi tiết.";
        vi["MemoryTips_QuickTemplates"] = "Mẫu câu thần chú gợi ý:";
        vi["MemoryTips_ApplyTemplate"] = "Điền mẫu";

        // Categories
        vi["TipCategory_Vocabulary"] = "Từ vựng";
        vi["TipCategory_Grammar"] = "Ngữ pháp";
        vi["TipCategory_Tenses"] = "Thì tiếng Anh";
        vi["TipCategory_Expressions"] = "Cách diễn đạt";
        vi["TipCategory_ExamStrategy"] = "Mẹo làm bài";
        vi["TipCategory_General"] = "Tổng hợp";

        // Techniques
        vi["MnemonicTechnique_Acrostic"] = "Câu thần chú / Viết tắt (Acrostic)";
        vi["MnemonicTechnique_RhymeAndRhythm"] = "Thơ, vè, vần điệu (Rhyme)";
        vi["MnemonicTechnique_VisualImagery"] = "Hình ảnh liên tưởng (Visual)";
        vi["MnemonicTechnique_SoundAlike"] = "Cầu nối âm thanh (Sound-alike)";
        vi["MnemonicTechnique_Storytelling"] = "Câu chuyện liên tưởng (Story)";
        vi["MnemonicTechnique_Etymology"] = "Gốc từ, tiền tố, hậu tố (Roots)";
        vi["MnemonicTechnique_EliminationRule"] = "Quy tắc loại trừ đề thi";
        vi["MnemonicTechnique_Other"] = "Kỹ thuật khác";

        // ==========================================
        // Memory Tips & Mnemonics — en-US (English)
        // ==========================================
        en["Nav_MemoryTips"] = "Memory Tips & Strategies";
        en["Shell_TooltipMemoryTips"] = "Memory Tips & Exam Strategies (Ctrl+Shift+M)";

        en["MemoryTips_Title"] = "Memory Tips & Exam Strategies";
        en["MemoryTips_Subtitle"] = "High-yield mnemonic hooks, cognitive associations, and classic exam traps for TOEIC, IELTS, and VSTEP";
        en["MemoryTips_StatTotal"] = "Total Tips:";
        en["MemoryTips_StatPinned"] = "Pinned:";
        en["MemoryTips_StatTraps"] = "Exam Traps:";

        en["MemoryTips_AddButton"] = "Add New Tip";
        en["MemoryTips_SmartImport"] = "Smart Import";
        en["MemoryTips_SmartImport_Summary"] = "Imported {0} new tips, skipped {1} duplicate/invalid items.";
        en["MemoryTips_SearchPlaceholder"] = "Search tips, mnemonic hooks, traps, or tags...";
        en["MemoryTips_CategoryAll"] = "All Categories";
        en["MemoryTips_TechniqueAll"] = "All Techniques";
        en["MemoryTips_ExamAll"] = "All Exams";
        en["MemoryTips_OnlyPinned"] = "Only Pinned";
        en["MemoryTips_ResetFilters"] = "Reset";
        en["MemoryTips_FirstPage"] = "First page";
        en["MemoryTips_PrevPage"] = "Previous page";
        en["MemoryTips_NextPage"] = "Next page";
        en["MemoryTips_LastPage"] = "Last page";
        en["MemoryTips_PageSize"] = "Per page:";
        en["MemoryTips_PageSizeSuffix"] = "/ page";
        en["MemoryTips_ShowingRange"] = "Showing {0}-{1} of {2} tips";

        en["MemoryTips_ViewSplit"] = "List & Details";
        en["MemoryTips_ViewGrid"] = "Card Grid";
        en["MemoryTips_TooltipSplit"] = "Split View: List & Detailed Inspector";
        en["MemoryTips_TooltipGrid"] = "Grid View: Multi-column Card Grid";
        en["MemoryTips_FilterAll"] = "All";
        en["MemoryTips_FilterPinned"] = "Pinned";
        en["MemoryTips_FilterTraps"] = "Strategies & Traps";
        en["MemoryTips_CopyHook"] = "Copy Mnemonic Hook";
        en["MemoryTips_CopySuccess"] = "Mnemonic hook copied to clipboard!";
        en["MemoryTips_NoSelectedTip"] = "Select a memory tip from the list on the left to view details";
        en["MemoryTips_DetailHeader"] = "Memory Tip Details";
        en["MemoryTips_TipsCount"] = "tips";

        en["MemoryTips_EmptyTitle"] = "No matching memory tips found";
        en["MemoryTips_EmptySubtitle"] = "Try adjusting your search filters or click 'Add New Tip' to save your own learning mnemonics.";

        en["MemoryTips_HookLabel"] = "MNEMONIC HOOK / QUICK RECALL";
        en["MemoryTips_ExplanationLabel"] = "DETAILED EXPLANATION";
        en["MemoryTips_ExampleLabel"] = "APPLICATION EXAMPLE";
        en["MemoryTips_TrapLabel"] = "COMMON EXAM TRAP / WATCH OUT";
        en["MemoryTips_TagsLabel"] = "Tags:";

        en["MemoryTips_Pin"] = "Pin to Top";
        en["MemoryTips_Unpin"] = "Unpin";
        en["MemoryTips_Edit"] = "Edit";
        en["MemoryTips_Delete"] = "Delete Tip";
        en["MemoryTips_DeleteConfirmTitle"] = "Confirm Delete Tip";
        en["MemoryTips_DeleteConfirmMessage"] = "Are you sure you want to delete this memory tip? This action cannot be undone.";
        en["MemoryTips_AddSuccess"] = "Memory tip added successfully.";
        en["MemoryTips_UpdateSuccess"] = "Memory tip updated successfully.";
        en["MemoryTips_DeleteSuccess"] = "Memory tip deleted successfully.";

        // Dialog Keys
        en["MemoryTips_DialogAddTitle"] = "Add New Memory Tip";
        en["MemoryTips_DialogEditTitle"] = "Edit Memory Tip";
        en["MemoryTips_FieldTitle"] = "Tip Title *";
        en["MemoryTips_FieldTitlePlaceholder"] = "e.g., Adjective Order OpSASCOMP";
        en["MemoryTips_FieldCategory"] = "Category *";
        en["MemoryTips_FieldTechnique"] = "Mnemonic Technique *";
        en["MemoryTips_FieldExamTarget"] = "Exam Target";
        en["MemoryTips_FieldHook"] = "Mnemonic Hook / Quick Recall *";
        en["MemoryTips_FieldHookPlaceholder"] = "e.g., FANBOYS: For, And, Nor, But, Or, Yet, So";
        en["MemoryTips_FieldExplanation"] = "Detailed Explanation *";
        en["MemoryTips_FieldExplanationPlaceholder"] = "Explain the logic, mnemonic breakdown, and application rules...";
        en["MemoryTips_FieldExample"] = "Application Example";
        en["MemoryTips_FieldExamplePlaceholder"] = "e.g., A lovely small old round black Vietnamese dining table...";
        en["MemoryTips_FieldTrap"] = "Common Exam Trap / Watch Out";
        en["MemoryTips_FieldTrapPlaceholder"] = "e.g., Exams often invert Size and Color...";
        en["MemoryTips_FieldTags"] = "Tags (comma separated)";
        en["MemoryTips_FieldTagsPlaceholder"] = "e.g., Adjectives, Grammar, TOEIC, VSTEP";
        en["MemoryTips_FieldIsPinned"] = "Pin this tip to the top of the list";

        en["MemoryTips_ValidationTitleRequired"] = "Please enter a title for the memory tip.";
        en["MemoryTips_ValidationHookRequired"] = "Please enter the mnemonic hook or recall phrase.";
        en["MemoryTips_ValidationExplanationRequired"] = "Please enter the detailed explanation.";
        en["MemoryTips_QuickTemplates"] = "Suggested popular templates:";
        en["MemoryTips_ApplyTemplate"] = "Use Template";

        // Categories
        en["TipCategory_Vocabulary"] = "Vocabulary";
        en["TipCategory_Grammar"] = "Grammar";
        en["TipCategory_Tenses"] = "Tenses";
        en["TipCategory_Expressions"] = "Expressions";
        en["TipCategory_ExamStrategy"] = "Exam Strategy";
        en["TipCategory_General"] = "General";

        // Techniques
        en["MnemonicTechnique_Acrostic"] = "Acrostic / Acronym Hook";
        en["MnemonicTechnique_RhymeAndRhythm"] = "Rhyme & Rhythm";
        en["MnemonicTechnique_VisualImagery"] = "Visual Imagery";
        en["MnemonicTechnique_SoundAlike"] = "Sound-Alike Connection";
        en["MnemonicTechnique_Storytelling"] = "Storytelling Association";
        en["MnemonicTechnique_Etymology"] = "Etymology & Affixes";
        en["MnemonicTechnique_EliminationRule"] = "Elimination Rule & Indicators";
        en["MnemonicTechnique_Other"] = "Other Technique";
    }
}
