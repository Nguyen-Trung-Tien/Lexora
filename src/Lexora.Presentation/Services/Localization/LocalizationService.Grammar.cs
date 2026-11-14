namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterGrammar(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Grammar — vi-VN (Tiếng Việt)
        // ==========================================
        vi["Grammar_SmartImport"] = "Nhập thông minh (Tự động)...";
        vi["Grammar_ImportFailed"] = "Lỗi nhập ngữ pháp: {0}";
        vi["Grammar_RelatedTitle"] = "Cấu trúc liên quan & Gợi ý";
        vi["Grammar_RelatedSubtitle"] = "Cấu trúc tương tự, điểm ngữ pháp dễ nhầm lẫn và từ vựng liên kết";
        vi["Grammar_TabSimilar"] = "Tương tự";
        vi["Grammar_TabConfused"] = "Dễ nhầm lẫn";
        vi["Grammar_TabSameTopic"] = "Cùng chủ đề";
        vi["Grammar_TabRelatedVocab"] = "Từ vựng liên kết";
        vi["Grammar_LinkedVocab"] = "Từ vựng liên kết";
        vi["Grammar_TabAll"] = "Tất cả";
        vi["Grammar_NoSuggestionsFound"] = "Chưa có gợi ý liên quan cho cấu trúc này";
        vi["Grammar_SortRelevance"] = "Độ liên quan";
        // Grammar
        vi["Grammar_Title"] = "Cẩm Nang Ngữ Pháp";
        vi["Grammar_SearchPlaceholder"] = "Tìm quy tắc, công thức ngữ pháp, cấu trúc... (Ctrl+F)";
        vi["Grammar_AllExams"] = "Tất cả kỳ thi";
        vi["Grammar_AddRule"] = "Thêm quy tắc";
        vi["Grammar_Practice"] = "Luyện tập";
        vi["Grammar_Structures"] = "Cấu trúc ngữ pháp";
        vi["Grammar_EmptyTitle"] = "Không tìm thấy cấu trúc ngữ pháp";
        vi["Grammar_EmptySubtitle"] = "Thử xóa bộ lọc hoặc tìm kiếm để xem các bài học ngữ pháp sẵn có.";
        vi["Grammar_SelectPromptTitle"] = "Chọn một quy tắc ngữ pháp";
        vi["Grammar_SelectPromptSubtitle"] = "Chọn cấu trúc ngữ pháp bên trái để xem công thức, giải thích chuyên sâu và lỗi cần tránh.";
        vi["Grammar_StructureFormula"] = "Công thức cấu trúc";
        vi["Grammar_Explanation"] = "Giải thích chi tiết";
        vi["Grammar_CommonMistakes"] = "⚠️ Lỗi thường gặp & Lưu ý";
        vi["Grammar_Examples"] = "Ví dụ minh họa ngữ cảnh";
        vi["Grammar_ExitPractice"] = "Thoát luyện tập";
        vi["Grammar_PracticeComplete"] = "Hoàn thành bài luyện tập ngữ pháp! 🎉";
        vi["Grammar_Score"] = "Điểm số";
        vi["Grammar_Correct"] = "Chính xác";
        vi["Grammar_Incorrect"] = "Chưa đúng";
        vi["Grammar_FeedbackCorrect"] = "Chính xác! 🎉";
        vi["Grammar_FeedbackIncorrect"] = "Chưa chính xác!";
        vi["Grammar_PracticeAgain"] = "Luyện tập tiếp";
        vi["Grammar_BackToRules"] = "Quay lại cẩm nang";
        vi["Grammar_PracticeTitle"] = "Luyện tập cấu trúc ngữ pháp";
        vi["Grammar_ReviewQuestions"] = "Chi tiết các câu hỏi luyện tập";
        vi["Grammar_YourAnswer"] = "Lựa chọn của bạn";
        vi["Grammar_CorrectAnswer"] = "Công thức đúng";
        vi["Grammar_CategoryFilter"] = "Chuyên đề ngữ pháp";
        vi["Grammar_AllCategories"] = "Tất cả chuyên đề";
        vi["Grammar_ConfirmBulkDeleteTitle"] = "Xác nhận xóa quy tắc";
        vi["Grammar_ConfirmBulkDeleteMsg"] = "Bạn có chắc chắn muốn xóa {0} quy tắc ngữ pháp đã chọn? Hành động này không thể hoàn tác.";
        vi["Grammar_Status_RuleAdded"] = "Đã thêm quy tắc '{0}' thành công.";
        vi["Grammar_Status_RuleUpdated"] = "Đã cập nhật quy tắc '{0}' thành công.";
        vi["Grammar_Status_RuleDeleted"] = "Đã xóa quy tắc '{0}' thành công.";
        vi["Grammar_BulkDeleteSuccess"] = "Đã xóa thành công {0} quy tắc ngữ pháp.";
        // Additional Grammar
        vi["Grammar_DeleteTitle"] = "Xóa quy tắc ngữ pháp";
        vi["Grammar_DeleteConfirm"] = "Bạn có chắc chắn muốn xóa quy tắc ngữ pháp '{0}' không?";
        vi["Grammar_TooltipAdd"] = "Thêm quy tắc mới (Ctrl+N)";
        vi["Grammar_TooltipPractice"] = "Luyện tập ngữ pháp (Ctrl+P)";
        vi["Grammar_Practice_NoQuestions"] = "Chưa có câu hỏi nào cho quy tắc này. Vui lòng chọn quy tắc khác hoặc thêm câu hỏi vào.";
        vi["Grammar_TooltipEdit"] = "Chỉnh sửa quy tắc (Ctrl+E)";
        vi["Grammar_TooltipDelete"] = "Xóa quy tắc (Del)";
        vi["Grammar_ExportJson"] = "Xuất JSON";
        vi["Grammar_ExportCsv"] = "Xuất CSV";
        vi["Grammar_ImportJson"] = "Nhập JSON";
        vi["Grammar_ImportCsv"] = "Nhập CSV";
        vi["Grammar_ExportCompleteTitle"] = "Xuất ngữ pháp hoàn tất";
        vi["Grammar_ExportCompleteMsg"] = "Đã xuất {0} quy tắc ngữ pháp sang {1} thành công.";
        vi["Grammar_ImportJsonTitle"] = "Nhập ngữ pháp (JSON)";
        vi["Grammar_ImportCsvTitle"] = "Nhập ngữ pháp (CSV)";
        vi["Grammar_ImportPastePlaceholderJson"] = "Dán nội dung JSON vào đây hoặc để trống để chọn tệp...";
        vi["Grammar_ImportPastePlaceholderCsv"] = "Dán nội dung CSV vào đây hoặc để trống để chọn tệp...";
        vi["Grammar_ExportFailed"] = "Lỗi xuất ngữ pháp";
        vi["Grammar_ExportAll"] = "Xuất toàn bộ ({0} quy tắc)";
        vi["Grammar_ExportSelected"] = "Xuất các quy tắc đã chọn ({0} quy tắc)";
        vi["Grammar_ExportTxt"] = "Xuất file TXT (Văn bản)";
        vi["Grammar_ImportTxt"] = "Nhập từ file TXT...";
        vi["Grammar_ImportTxtTitle"] = "Nhập ngữ pháp (TXT)";
        vi["Grammar_FirstPage"] = "Trang đầu";
        vi["Grammar_PrevPage"] = "Trang trước";
        vi["Grammar_NextPage"] = "Trang sau";
        vi["Grammar_LastPage"] = "Trang cuối";
        vi["Grammar_PageSize"] = "Số lượng/trang:";
        vi["Grammar_PageSizeSuffix"] = "/ trang";
        vi["Grammar_NavTab"] = "Ngữ pháp";
        vi["Grammar_Practice_ResultSummaryFormat"] = "Bạn đã hoàn thành bài luyện tập với {0}/{1} câu trả lời chính xác (Đạt {2}%).";
        vi["Grammar_Practice_SelectOptionTip"] = "Chọn một phương án để kiểm tra cấu trúc";
        vi["Grammar_Practice_NextQuestionTip"] = "Nhấn 'Câu tiếp theo' để tiếp tục";
        vi["Grammar_SmartImport_Title"] = "Nhập dữ liệu Ngữ pháp thông minh";
        vi["Grammar_SmartImport_Subtitle"] = "Dán trực tiếp hoặc tải tệp (JSON, CSV, TXT) chứa danh sách cấu trúc ngữ pháp, giải thích và ví dụ câu.";
        vi["Grammar_SmartImport_AnalyzeBtn"] = "Tiến hành nhập dữ liệu";
        vi["Grammar_ServiceNotAvailable"] = "Dịch vụ nhập/xuất ngữ pháp không khả dụng.";

        // ==========================================
        // Grammar — en-US (English)
        // ==========================================
        en["Grammar_SmartImport"] = "Smart Import (Auto-detect)...";
        en["Grammar_ImportFailed"] = "Grammar import failed: {0}";
        en["Grammar_RelatedTitle"] = "Related Structures & Suggestions";
        en["Grammar_RelatedSubtitle"] = "Similar structures, commonly confused grammar, and linked vocabulary";
        en["Grammar_TabSimilar"] = "Similar";
        en["Grammar_TabConfused"] = "Confused";
        en["Grammar_TabSameTopic"] = "Same Topic";
        en["Grammar_TabRelatedVocab"] = "Linked Vocab";
        en["Grammar_LinkedVocab"] = "Linked Vocabulary";
        en["Grammar_TabAll"] = "All";
        en["Grammar_NoSuggestionsFound"] = "No related suggestions found for this rule";
        en["Grammar_SortRelevance"] = "Relevance";
        // Grammar
        en["Grammar_Title"] = "Grammar Master";
        en["Grammar_SearchPlaceholder"] = "Search grammar rules, structures, formulas... (Ctrl+F)";
        en["Grammar_AllExams"] = "All Target Exams";
        en["Grammar_AddRule"] = "Add Rule";
        en["Grammar_Practice"] = "Practice";
        en["Grammar_Structures"] = "Grammar Structures";
        en["Grammar_EmptyTitle"] = "No grammar rules found";
        en["Grammar_EmptySubtitle"] = "Try clearing search or filters to see available grammar lessons.";
        en["Grammar_SelectPromptTitle"] = "Select a Grammar Rule";
        en["Grammar_SelectPromptSubtitle"] = "Choose any rule on the left to inspect formula, explanations, contextual examples, and pitfalls.";
        en["Grammar_StructureFormula"] = "Structure Formula";
        en["Grammar_Explanation"] = "Detailed Explanation";
        en["Grammar_CommonMistakes"] = "⚠️ Common Mistakes & Pitfalls";
        en["Grammar_Examples"] = "Contextual Examples";
        en["Grammar_ExitPractice"] = "Exit Practice";
        en["Grammar_PracticeComplete"] = "Grammar Practice Complete! 🎉";
        en["Grammar_Score"] = "Score";
        en["Grammar_Correct"] = "Correct";
        en["Grammar_Incorrect"] = "Incorrect";
        en["Grammar_FeedbackCorrect"] = "Correct! 🎉";
        en["Grammar_FeedbackIncorrect"] = "Incorrect!";
        en["Grammar_PracticeAgain"] = "Try Another Practice";
        en["Grammar_BackToRules"] = "Back to Rules";
        en["Grammar_PracticeTitle"] = "Grammar Structure Practice";
        en["Grammar_ReviewQuestions"] = "Practice Questions Review";
        en["Grammar_YourAnswer"] = "Your answer";
        en["Grammar_CorrectAnswer"] = "Correct formula";
        en["Grammar_CategoryFilter"] = "Grammar Category";
        en["Grammar_AllCategories"] = "All Categories";
        en["Grammar_ConfirmBulkDeleteTitle"] = "Confirm Rule Deletion";
        en["Grammar_ConfirmBulkDeleteMsg"] = "Are you sure you want to delete {0} selected grammar rules? This action cannot be undone.";
        en["Grammar_Status_RuleAdded"] = "Grammar rule '{0}' added successfully.";
        en["Grammar_Status_RuleUpdated"] = "Grammar rule '{0}' updated successfully.";
        en["Grammar_Status_RuleDeleted"] = "Grammar rule '{0}' deleted successfully.";
        en["Grammar_BulkDeleteSuccess"] = "Successfully deleted {0} grammar rule(s).";
        // Additional Grammar
        en["Grammar_DeleteTitle"] = "Delete Grammar Rule";
        en["Grammar_DeleteConfirm"] = "Are you sure you want to delete grammar rule '{0}'?";
        en["Grammar_TooltipAdd"] = "Add Rule (Ctrl+N)";
        en["Grammar_TooltipPractice"] = "Practice (Ctrl+P)";
        en["Grammar_Practice_NoQuestions"] = "No questions available for this rule. Please select another rule or add questions.";
        en["Grammar_TooltipEdit"] = "Edit Grammar Rule (Ctrl+E)";
        en["Grammar_TooltipDelete"] = "Delete Rule (Del)";
        en["Grammar_ExportJson"] = "Export JSON";
        en["Grammar_ExportCsv"] = "Export CSV";
        en["Grammar_ImportJson"] = "Import JSON";
        en["Grammar_ImportCsv"] = "Import CSV";
        en["Grammar_ExportCompleteTitle"] = "Grammar Export Complete";
        en["Grammar_ExportCompleteMsg"] = "Successfully exported {0} grammar rules to {1}.";
        en["Grammar_ImportJsonTitle"] = "Import Grammar (JSON)";
        en["Grammar_ImportCsvTitle"] = "Import Grammar (CSV)";
        en["Grammar_ImportPastePlaceholderJson"] = "Paste JSON content here or leave empty to choose file...";
        en["Grammar_ImportPastePlaceholderCsv"] = "Paste CSV content here or leave empty to choose file...";
        en["Grammar_ExportFailed"] = "Grammar Export Failed";
        en["Grammar_ExportAll"] = "Export All ({0} rules)";
        en["Grammar_ExportSelected"] = "Export Selected ({0} rules)";
        en["Grammar_ExportTxt"] = "Export TXT (Text)";
        en["Grammar_ImportTxt"] = "Import from TXT...";
        en["Grammar_ImportTxtTitle"] = "Import Grammar (TXT)";
        en["Grammar_FirstPage"] = "First page";
        en["Grammar_PrevPage"] = "Previous page";
        en["Grammar_NextPage"] = "Next page";
        en["Grammar_LastPage"] = "Last page";
        en["Grammar_PageSize"] = "Per page:";
        en["Grammar_PageSizeSuffix"] = "/ page";
        en["Grammar_NavTab"] = "Grammar";
        en["Grammar_Practice_ResultSummaryFormat"] = "You completed the practice with {0}/{1} correct answers ({2}% score).";
        en["Grammar_Practice_SelectOptionTip"] = "Select an option to check structure formula";
        en["Grammar_Practice_NextQuestionTip"] = "Click 'Next Question' to continue";
        en["Grammar_SmartImport_Title"] = "Smart Grammar Data Import";
        en["Grammar_SmartImport_Subtitle"] = "Directly paste or load files (JSON, CSV, TXT) containing grammar rules, explanations, and example sentences.";
        en["Grammar_SmartImport_AnalyzeBtn"] = "Start Import";
        en["Grammar_ServiceNotAvailable"] = "Grammar export/import service not available.";
    }
}
