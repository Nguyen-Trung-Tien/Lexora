namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterHomophones(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Homophones — vi-VN (Tiếng Việt)
        // ==========================================
        // Homophones & Sound-Alikes
        vi["Homophones_Title"] = "Từ đồng âm & Cặp âm dễ nhầm";
        vi["Homophones_Subtitle"] = "Phân biệt từ phát âm tương đồng, tránh bẫy nghe và viết trong TOEIC, IELTS, VSTEP";
        vi["Homophones_AddGroup"] = "Thêm nhóm từ";
        vi["Homophones_SmartImport"] = "Nhập thông minh";
        vi["Homophones_SearchPlaceholder"] = "Tìm từ hoặc phiên âm IPA...";
        vi["Homophones_CreateDialogTitle"] = "Thêm nhóm từ đồng âm mới";
        vi["Homophones_EditDialogTitle"] = "Chỉnh sửa nhóm từ đồng âm";
        vi["Homophones_ValidationTitleRequired"] = "Vui lòng nhập tên nhóm từ.";
        vi["Homophones_ValidationWordsRequired"] = "Vui lòng nhập đầy đủ từ vựng và nghĩa cho cả 2 từ.";
        vi["Homophones_ConfirmDeleteMessage"] = "Bạn có chắc chắn muốn xóa nhóm từ '{0}' không?";
        vi["Homophones_SmartImport_Title"] = "Nhập thông minh - Nhóm từ đồng âm";
        vi["Homophones_SmartImport_Subtitle"] = "Tự động phân tích file JSON, CSV hoặc TXT chứa các cụm từ đồng âm";
        vi["Homophones_SmartImport_AnalyzeBtn"] = "Phân tích nhóm từ";
        vi["Homophones_ServiceNotAvailable"] = "Dịch vụ nhập/xuất từ đồng âm không khả dụng";
        vi["Homophones_FilterAllTypes"] = "Tất cả loại";
        vi["Homophones_TypeExact"] = "Đồng âm (Exact)";
        vi["Homophones_TypeNear"] = "Gần đồng âm (Near)";
        vi["Homophones_TypePair"] = "Cặp tối thiểu (Pair)";
        vi["Homophones_FilterAllLevels"] = "Mọi cấp độ";
        vi["Homophones_EmptyList"] = "Không tìm thấy nhóm từ phù hợp";
        vi["Homophones_TipTitle"] = "Mẹo phân biệt & Ngữ cảnh sử dụng";
        vi["Homophones_ComparisonTitle"] = "Đối chiếu các từ thành phần";
        vi["Homophones_VietnameseMeaning"] = "Nghĩa tiếng Việt";
        vi["Homophones_ContextualExample"] = "Câu ví dụ trong ngữ cảnh";
        vi["Homophones_Toast_GroupSaved"] = "Đã thêm nhóm từ '{0}' thành công!";
        vi["Homophones_Toast_GroupUpdated"] = "Đã cập nhật nhóm từ '{0}' thành công!";
        vi["Homophones_Toast_GroupDeleted"] = "Đã xóa nhóm từ '{0}' thành công!";
        vi["Homophones_Toast_ExportSuccess"] = "Đã xuất tệp '{0}' thành công!";
        vi["Homophones_ExportJson"] = "Xuất file JSON (.json)";
        vi["Homophones_ExportCsv"] = "Xuất bảng tính CSV (.csv)";
        vi["Homophones_ExportTxt"] = "Xuất Flashcard TXT (.txt)";
        vi["Homophones_NoExample"] = "(Không có câu ví dụ)";
        vi["Homophones_SavedToStorageSuccess"] = "Đã lưu từ '{0}' vào Kho lưu trữ thành công!";
        vi["Homophones_ViewInVocabulary"] = "Xem trong Kho từ vựng";
        vi["Homophones_SaveToStorage"] = "Lưu vào Kho lưu trữ";
        vi["Homophones_EditorSectionInfo"] = "Thông tin nhóm phát âm";
        vi["Homophones_EditorTitleHeader"] = "Tên nhóm từ (vd: bare / bear)";
        vi["Homophones_EditorTitlePlaceholder"] = "Từ 1 / Từ 2...";
        vi["Homophones_EditorIpaHeader"] = "Phiên âm IPA chung";
        vi["Homophones_EditorIpaPlaceholder"] = "VD: /beə(r)/...";
        vi["Homophones_EditorTypeHeader"] = "Loại phân loại";
        vi["Homophones_EditorLevelHeader"] = "Cấp độ CEFR";
        vi["Homophones_EditorExamHeader"] = "Mục tiêu thi";
        vi["Homophones_EditorTipHeader"] = "Mẹo ghi nhớ phân biệt (Differentiation Tip)";
        vi["Homophones_EditorTipPlaceholder"] = "Ghi chú điểm khác biệt về nghĩa, ngữ cảnh, cấu tạo từ để tránh nhầm...";
        vi["Homophones_EditorWord1Badge"] = "Từ thứ 1 (Word A)";
        vi["Homophones_EditorWord2Badge"] = "Từ thứ 2 (Word B)";
        vi["Homophones_EditorTermHeader"] = "Từ vựng";
        vi["Homophones_EditorTerm1Placeholder"] = "VD: bare...";
        vi["Homophones_EditorTerm2Placeholder"] = "VD: bear...";
        vi["Homophones_EditorPosHeader"] = "Từ loại";
        vi["Homophones_EditorMeaningHeader"] = "Nghĩa tiếng Việt";
        vi["Homophones_EditorMeaning1Placeholder"] = "Nghĩa của từ 1...";
        vi["Homophones_EditorMeaning2Placeholder"] = "Nghĩa của từ 2...";
        vi["Homophones_EditorExampleHeader"] = "Câu ví dụ tiếng Anh";
        vi["Homophones_EditorExamplePlaceholder"] = "Câu ví dụ minh họa...";
        vi["Homophones_EditorExampleTransHeader"] = "Bản dịch câu ví dụ";
        vi["Homophones_EditorExampleTransPlaceholder"] = "Dịch nghĩa câu ví dụ...";
        vi["Homophones_ItemsRangeEmpty"] = "0 nhóm";
        vi["Homophones_ItemsRangeFormat"] = "Hiển thị {0}–{1} / {2} nhóm";
        vi["Homophones_SuggestionGroupBadge"] = "Nhóm đồng âm";
        vi["Homophones_DidYouMeanFormat"] = "Ý bạn là '{0}'?";
        vi["Homophones_SuggestionHintFormat"] = "Gợi ý: {0}";

        // ==========================================
        // Homophones — en-US (English)
        // ==========================================
        // Homophones & Sound-Alikes
        en["Homophones_Title"] = "Homophones & Sound-Alikes";
        en["Homophones_Subtitle"] = "Master confusing phonetic pairs and avoid listening traps in TOEIC, IELTS, VSTEP";
        en["Homophones_AddGroup"] = "Add Group";
        en["Homophones_SmartImport"] = "Smart Import";
        en["Homophones_SearchPlaceholder"] = "Search terms or IPA...";
        en["Homophones_CreateDialogTitle"] = "New Homophone Group";
        en["Homophones_EditDialogTitle"] = "Edit Homophone Group";
        en["Homophones_ValidationTitleRequired"] = "Please enter a group title.";
        en["Homophones_ValidationWordsRequired"] = "Please enter term and meaning for both words.";
        en["Homophones_ConfirmDeleteMessage"] = "Are you sure you want to delete homophone group '{0}'?";
        en["Homophones_SmartImport_Title"] = "Smart Import - Homophone Groups";
        en["Homophones_SmartImport_Subtitle"] = "Automatically parse JSON, CSV, or TXT homophone clusters";
        en["Homophones_SmartImport_AnalyzeBtn"] = "Analyze Groups";
        en["Homophones_ServiceNotAvailable"] = "Homophone export/import service unavailable";
        en["Homophones_FilterAllTypes"] = "All Types";
        en["Homophones_TypeExact"] = "Exact Homophone";
        en["Homophones_TypeNear"] = "Near Homophone";
        en["Homophones_TypePair"] = "Minimal Pair";
        en["Homophones_FilterAllLevels"] = "All Levels";
        en["Homophones_EmptyList"] = "No matching word groups found";
        en["Homophones_TipTitle"] = "Differentiation Tip & Usage Context";
        en["Homophones_ComparisonTitle"] = "Side-by-Side Word Comparison";
        en["Homophones_VietnameseMeaning"] = "Vietnamese Definition";
        en["Homophones_ContextualExample"] = "Contextual Example Sentence";
        en["Homophones_Toast_GroupSaved"] = "Homophone group '{0}' added successfully!";
        en["Homophones_Toast_GroupUpdated"] = "Homophone group '{0}' updated successfully!";
        en["Homophones_Toast_GroupDeleted"] = "Homophone group '{0}' deleted successfully!";
        en["Homophones_Toast_ExportSuccess"] = "Exported '{0}' successfully!";
        en["Homophones_ExportJson"] = "Export JSON File (.json)";
        en["Homophones_ExportCsv"] = "Export CSV Spreadsheet (.csv)";
        en["Homophones_ExportTxt"] = "Export Flashcard TXT (.txt)";
        en["Homophones_NoExample"] = "(No example sentence)";
        en["Homophones_SavedToStorageSuccess"] = "Saved word '{0}' to Learning Storage successfully!";
        en["Homophones_ViewInVocabulary"] = "View in Vocabulary";
        en["Homophones_SaveToStorage"] = "Save to Learning Storage";
        en["Homophones_EditorSectionInfo"] = "Homophone Group Details";
        en["Homophones_EditorTitleHeader"] = "Group Title (e.g. bare / bear)";
        en["Homophones_EditorTitlePlaceholder"] = "Word 1 / Word 2...";
        en["Homophones_EditorIpaHeader"] = "Shared Phonetic IPA";
        en["Homophones_EditorIpaPlaceholder"] = "E.g. /beə(r)/...";
        en["Homophones_EditorTypeHeader"] = "Classification Type";
        en["Homophones_EditorLevelHeader"] = "CEFR Level";
        en["Homophones_EditorExamHeader"] = "Target Exam";
        en["Homophones_EditorTipHeader"] = "Differentiation Tip & Memory Hook";
        en["Homophones_EditorTipPlaceholder"] = "Note distinctions in meaning, context, or spelling to prevent traps...";
        en["Homophones_EditorWord1Badge"] = "First Word (Word A)";
        en["Homophones_EditorWord2Badge"] = "Second Word (Word B)";
        en["Homophones_EditorTermHeader"] = "Vocabulary Term";
        en["Homophones_EditorTerm1Placeholder"] = "E.g. bare...";
        en["Homophones_EditorTerm2Placeholder"] = "E.g. bear...";
        en["Homophones_EditorPosHeader"] = "Part of Speech";
        en["Homophones_EditorMeaningHeader"] = "Definition / Meaning";
        en["Homophones_EditorMeaning1Placeholder"] = "Meaning of word 1...";
        en["Homophones_EditorMeaning2Placeholder"] = "Meaning of word 2...";
        en["Homophones_EditorExampleHeader"] = "English Example Sentence";
        en["Homophones_EditorExamplePlaceholder"] = "Illustrative example sentence...";
        en["Homophones_EditorExampleTransHeader"] = "Example Translation";
        en["Homophones_EditorExampleTransPlaceholder"] = "Vietnamese/English translation...";
        en["Homophones_ItemsRangeEmpty"] = "0 groups";
        en["Homophones_ItemsRangeFormat"] = "Showing {0}–{1} of {2} groups";
        en["Homophones_SuggestionGroupBadge"] = "Homophone Group";
        en["Homophones_DidYouMeanFormat"] = "Did you mean '{0}'?";
        en["Homophones_SuggestionHintFormat"] = "Suggestion: {0}";
    }
}
