namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterDialogs(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Dialogs — vi-VN (Tiếng Việt)
        // ==========================================
        // Word Editor Dialog
        vi["WordEditor_TitleAdd"] = "Thêm từ vựng mới";
        vi["WordEditor_TitleEdit"] = "Chỉnh sửa từ vựng: {0}";
        vi["WordEditor_Save"] = "Lưu từ vựng";
        vi["WordEditor_Cancel"] = "Hủy bỏ";
        vi["WordEditor_ValidationTitle"] = "Vui lòng kiểm tra lại thông tin";
        vi["WordEditor_ErrTermRequired"] = "Vui lòng nhập từ hoặc cụm từ tiếng Anh.";
        vi["WordEditor_ErrMeaningRequired"] = "Vui lòng nhập ít nhất một định nghĩa tiếng Việt cho từ.";
        vi["WordEditor_HeaderTerm"] = "Thuật ngữ (Từ vựng) *";
        vi["WordEditor_PlaceholderTerm"] = "Nhập từ hoặc cụm từ tiếng Anh...";
        vi["WordEditor_HeaderPos"] = "Từ loại chính";
        vi["WordEditor_HeaderMultiPos"] = "Phân loại từ loại";
        vi["WordEditor_MultiPosHint"] = "Chọn từ loại chính và các góc từ liên quan:";
        vi["WordEditor_PrimaryPosLabel"] = "Từ loại chính:";
        vi["WordEditor_PosNoun"] = "Danh từ";
        vi["WordEditor_PosVerb"] = "Động từ";
        vi["WordEditor_PosAdj"] = "Tính từ";
        vi["WordEditor_PosAdv"] = "Trạng từ";
        vi["WordEditor_PosPhrasalVerb"] = "Cụm động từ";
        vi["WordEditor_PosNounPhrase"] = "Cụm danh từ";
        vi["WordEditor_PosPreposition"] = "Giới từ";
        vi["WordEditor_PosConjunction"] = "Liên từ";
        vi["WordEditor_PosIdiom"] = "Thành ngữ";
        // Word Editor & POS
        vi["WordEditor_PosPhrase"] = "Cụm từ";
        vi["WordEditor_PosInterjection"] = "Thán từ";
        // New Multi-POS Hierarchy
        vi["WordEditor_SectionBasicInfo"] = "Thông tin cơ bản";
        vi["WordEditor_SectionPosAndMeanings"] = "Từ loại & Tầng nghĩa";
        vi["WordEditor_SectionPosAndMeaningsDesc"] = "Một từ có thể thuộc nhiều từ loại (Noun, Verb...). Mỗi từ loại chứa các định nghĩa và ví dụ tương ứng.";
        vi["WordEditor_BtnAddPos"] = "Thêm từ loại";
        vi["WordEditor_AddPosFlyoutTitle"] = "Chọn từ loại cần thêm:";
        vi["WordEditor_PrimaryPosBadge"] = "★ Từ loại chính";
        vi["WordEditor_SetAsPrimary"] = "Đặt làm chính";
        vi["WordEditor_TooltipSetAsPrimary"] = "Đặt từ loại này làm từ loại chính của từ vựng";
        vi["WordEditor_TooltipDeletePos"] = "Xóa từ loại này cùng tất cả tầng nghĩa";
        vi["WordEditor_ConfirmDeletePosPrompt"] = "Xóa từ loại này cùng tất cả tầng nghĩa đã nhập?";
        vi["WordEditor_BtnAddMeaningForPos"] = "Thêm tầng nghĩa cho {0}";
        vi["WordEditor_ErrDuplicatePos"] = "Từ loại này đã tồn tại trong danh sách.";
        vi["WordEditor_ErrPosNeedsMeaning"] = "Từ loại '{0}' cần có ít nhất một định nghĩa tiếng Việt.";
        vi["WordEditor_Saving"] = "Đang lưu từ vựng...";
        vi["WordEditor_HeaderLevel"] = "Trình độ CEFR";
        vi["WordEditor_HeaderPhoneticUk"] = "Phiên âm UK";
        vi["WordEditor_PlaceholderPhoneticUk"] = "/.../ (UK)";
        vi["WordEditor_HeaderPhoneticUs"] = "Phiên âm US";
        vi["WordEditor_PlaceholderPhoneticUs"] = "/.../ (US)";
        vi["WordEditor_HeaderNotes"] = "Ghi chú thêm";
        vi["WordEditor_PlaceholderNotes"] = "Từ đồng nghĩa, trái nghĩa, lưu ý phát âm...";
        vi["WordEditor_SectionMeanings"] = "Tầng nghĩa & Định nghĩa";
        vi["WordEditor_SectionMeaningsDesc"] = "Mỗi từ vựng có thể có một hoặc nhiều tầng nghĩa và ngữ cảnh sử dụng.";
        vi["WordEditor_BtnAddMeaning"] = "Thêm tầng nghĩa";
        vi["WordEditor_MeaningBadge"] = "Tầng nghĩa #{0}";
        vi["WordEditor_HeaderMeaningPos"] = "Từ loại tầng nghĩa";
        vi["WordEditor_HeaderDefinitionVi"] = "Định nghĩa Tiếng Việt *";
        vi["WordEditor_PlaceholderDefinitionVi"] = "Định nghĩa tiếng Việt rõ ràng, súc tích...";
        vi["WordEditor_HeaderDefinitionEn"] = "Định nghĩa Tiếng Anh";
        vi["WordEditor_PlaceholderDefinitionEn"] = "Định nghĩa tiếng Anh (Oxford/Cambridge)...";
        vi["WordEditor_HeaderExplanation"] = "Ngữ cảnh / Sắc thái sử dụng";
        vi["WordEditor_PlaceholderExplanation"] = "Khi nào nên dùng, văn phong trang trọng hay thân mật...";
        vi["WordEditor_SectionExamples"] = "Câu ví dụ minh họa";
        vi["WordEditor_HeaderExampleEn"] = "Câu ví dụ Tiếng Anh";
        vi["WordEditor_PlaceholderExampleEn"] = "Câu ví dụ tiếng Anh...";
        vi["WordEditor_HeaderExampleVi"] = "Bản dịch Tiếng Việt";
        vi["WordEditor_PlaceholderExampleVi"] = "Bản dịch nghĩa tiếng Việt...";
        vi["WordEditor_BtnAddExample"] = "Thêm ví dụ";
        vi["WordEditor_TooltipDeleteMeaning"] = "Xóa tầng nghĩa này";
        vi["WordEditor_TooltipDeleteExample"] = "Xóa câu ví dụ này";
        vi["WordEditor_SectionTopicsExams"] = "Chủ đề & Kỳ thi liên quan";
        vi["WordEditor_SectionTopicsExamsDesc"] = "Gắn nhãn để lọc và làm bài thi theo chủ đề.";
        vi["WordEditor_HeaderTopics"] = "Chủ đề học tập";
        vi["WordEditor_HeaderExams"] = "Kỳ thi áp dụng";
        vi["WordEditor_EmptyTopics"] = "Chưa có chủ đề nào";
        vi["WordEditor_EmptyExams"] = "Chưa có thẻ kỳ thi nào";
        vi["WordEditor_AddTopic"] = "Thêm chủ đề mới";
        vi["WordEditor_AddTopicFlyoutTitle"] = "Tạo nhanh chủ đề mới";
        // Grammar Editor Dialog
        vi["GrammarEditor_TitleAdd"] = "Thêm quy tắc ngữ pháp mới";
        vi["GrammarEditor_TitleEdit"] = "Chỉnh sửa quy tắc ngữ pháp: {0}";
        vi["GrammarEditor_Save"] = "Lưu quy tắc";
        vi["GrammarEditor_Cancel"] = "Hủy bỏ";
        vi["GrammarEditor_ValidationTitle"] = "Lỗi kiểm tra dữ liệu";
        vi["GrammarEditor_ErrTitleRequired"] = "Vui lòng nhập tên quy tắc ngữ pháp.";
        vi["GrammarEditor_ErrStructureRequired"] = "Vui lòng nhập công thức cấu trúc ngữ pháp.";
        vi["GrammarEditor_ErrExplanationViRequired"] = "Vui lòng nhập phần giải thích chi tiết bằng Tiếng Việt.";
        vi["GrammarEditor_HeaderTitle"] = "Tên quy tắc ngữ pháp *";
        vi["GrammarEditor_PlaceholderTitle"] = "Ví dụ: Present Perfect Continuous";
        vi["GrammarEditor_HeaderStructure"] = "Công thức cấu trúc *";
        vi["GrammarEditor_PlaceholderStructure"] = "Ví dụ: S + have/has + been + V-ing";
        vi["GrammarEditor_HeaderLevel"] = "Cấp độ CEFR";
        vi["GrammarEditor_HeaderExam"] = "Mục tiêu kỳ thi";
        vi["GrammarEditor_HeaderTopic"] = "Chuyên đề";
        vi["GrammarEditor_PlaceholderTopic"] = "Ví dụ: Tenses, Passive Voice, Conditionals...";
        vi["GrammarEditor_HeaderExplanationVi"] = "Giải thích chi tiết (Tiếng Việt) *";
        vi["GrammarEditor_PlaceholderExplanationVi"] = "Giải thích cặn kẽ cách dùng, dấu hiệu nhận biết và ngữ cảnh...";
        vi["GrammarEditor_HeaderExplanationEn"] = "Giải thích bằng Tiếng Anh (Tùy chọn)";
        vi["GrammarEditor_PlaceholderExplanationEn"] = "Nuances, usage notes, and exceptions in English...";
        vi["GrammarEditor_SectionMistakes"] = "Lỗi thường gặp & Bẫy đề thi";
        vi["GrammarEditor_HeaderMistakes"] = "Lỗi thường gặp & Lưu ý bẫy đề thi";
        vi["GrammarEditor_PlaceholderMistakes"] = "Các lỗi thí sinh hay mắc phải, mẹo phân biệt với cấu trúc tương đương...";
        vi["GrammarEditor_SectionExamples"] = "Câu ví dụ minh họa";
        vi["GrammarEditor_SectionExamplesDesc"] = "Thêm các câu ví dụ song ngữ kèm ghi chú cách dùng cụ thể";
        vi["GrammarEditor_BtnAddExample"] = "Thêm ví dụ";
        vi["GrammarEditor_ExampleBadge"] = "Ví dụ #{0}";
        vi["GrammarEditor_HeaderSentenceEn"] = "Câu ví dụ Tiếng Anh *";
        vi["GrammarEditor_PlaceholderSentenceEn"] = "Ví dụ: I have been working here for three years.";
        vi["GrammarEditor_HeaderSentenceVi"] = "Bản dịch Tiếng Việt *";
        vi["GrammarEditor_PlaceholderSentenceVi"] = "Bản dịch nghĩa tiếng Việt chuẩn xác...";
        vi["GrammarEditor_HeaderNote"] = "Ghi chú ngữ pháp bổ sung";
        vi["GrammarEditor_PlaceholderNote"] = "Lưu ý về thì, trạng từ, đảo ngữ...";
        vi["GrammarEditor_TooltipRemoveExample"] = "Xóa câu ví dụ này";

        // Exception & Error Dialog
        vi["ExceptionDialog_Title"] = "Đã xảy ra sự cố không mong muốn";
        vi["ExceptionDialog_Subtitle"] = "Ứng dụng gặp lỗi trong quá trình xử lý. Dữ liệu của bạn vẫn an toàn; bạn có thể sao chép chi tiết lỗi hoặc tiếp tục sử dụng.";
        vi["ExceptionDialog_DetailsHeader"] = "Chi tiết kỹ thuật (Stack Trace)";
        vi["ExceptionDialog_CopyDetails"] = "Sao chép lỗi";
        vi["ExceptionDialog_Copied"] = "Đã sao chép!";
        vi["ExceptionDialog_OpenLogs"] = "Mở thư mục Log";
        vi["ExceptionDialog_Close"] = "Đóng";
        vi["Toast_CopyError"] = "Sao chép";
        vi["Toast_ViewDetails"] = "Chi tiết";

        // ==========================================
        // Dialogs — en-US (English)
        // ==========================================
        // Word Editor Dialog
        en["WordEditor_TitleAdd"] = "Add New Vocabulary";
        en["WordEditor_TitleEdit"] = "Edit Vocabulary: {0}";
        en["WordEditor_Save"] = "Save Vocabulary";
        en["WordEditor_Cancel"] = "Cancel";
        en["WordEditor_ValidationTitle"] = "Please check required fields";
        en["WordEditor_ErrTermRequired"] = "Please enter an English word or phrase.";
        en["WordEditor_ErrMeaningRequired"] = "Please enter at least one Vietnamese definition.";
        en["WordEditor_HeaderTerm"] = "Term (Word / Phrase) *";
        en["WordEditor_PlaceholderTerm"] = "Enter English word or phrase...";
        en["WordEditor_HeaderPos"] = "Primary Part of Speech";
        en["WordEditor_HeaderMultiPos"] = "Parts of Speech Classification";
        en["WordEditor_MultiPosHint"] = "Select primary part of speech and related tags:";
        en["WordEditor_PrimaryPosLabel"] = "Primary POS:";
        en["WordEditor_PosNoun"] = "Noun";
        en["WordEditor_PosVerb"] = "Verb";
        en["WordEditor_PosAdj"] = "Adjective";
        en["WordEditor_PosAdv"] = "Adverb";
        en["WordEditor_PosPhrasalVerb"] = "Phrasal Verb";
        en["WordEditor_PosNounPhrase"] = "Noun Phrase";
        en["WordEditor_PosPreposition"] = "Preposition";
        en["WordEditor_PosConjunction"] = "Conjunction";
        en["WordEditor_PosIdiom"] = "Idiom";
        // Word Editor & POS
        en["WordEditor_PosPhrase"] = "Phrase";
        en["WordEditor_PosInterjection"] = "Interjection";
        // New Multi-POS Hierarchy
        en["WordEditor_SectionBasicInfo"] = "Basic Information";
        en["WordEditor_SectionPosAndMeanings"] = "Parts of Speech & Meanings";
        en["WordEditor_SectionPosAndMeaningsDesc"] = "A word can belong to multiple parts of speech (Noun, Verb...). Each part of speech contains its definitions and examples.";
        en["WordEditor_BtnAddPos"] = "Add Part of Speech";
        en["WordEditor_AddPosFlyoutTitle"] = "Select part of speech to add:";
        en["WordEditor_PrimaryPosBadge"] = "★ Primary POS";
        en["WordEditor_SetAsPrimary"] = "Set as Primary";
        en["WordEditor_TooltipSetAsPrimary"] = "Set this as the primary part of speech";
        en["WordEditor_TooltipDeletePos"] = "Delete this part of speech and its meanings";
        en["WordEditor_ConfirmDeletePosPrompt"] = "Delete this part of speech and all its entered meanings?";
        en["WordEditor_BtnAddMeaningForPos"] = "Add meaning for {0}";
        en["WordEditor_ErrDuplicatePos"] = "This part of speech already exists in the list.";
        en["WordEditor_ErrPosNeedsMeaning"] = "Part of speech '{0}' must have at least one Vietnamese definition.";
        en["WordEditor_Saving"] = "Saving vocabulary...";
        en["WordEditor_HeaderLevel"] = "CEFR Level";
        en["WordEditor_HeaderPhoneticUk"] = "UK Phonetic";
        en["WordEditor_PlaceholderPhoneticUk"] = "/.../ (UK)";
        en["WordEditor_HeaderPhoneticUs"] = "US Phonetic";
        en["WordEditor_PlaceholderPhoneticUs"] = "/.../ (US)";
        en["WordEditor_HeaderNotes"] = "Additional Notes";
        en["WordEditor_PlaceholderNotes"] = "Synonyms, antonyms, usage notes...";
        en["WordEditor_SectionMeanings"] = "Meanings & Definitions";
        en["WordEditor_SectionMeaningsDesc"] = "A word can have one or more meanings and contextual nuances.";
        en["WordEditor_BtnAddMeaning"] = "Add Meaning";
        en["WordEditor_MeaningBadge"] = "Meaning #{0}";
        en["WordEditor_HeaderMeaningPos"] = "Meaning Part of Speech";
        en["WordEditor_HeaderDefinitionVi"] = "Vietnamese Definition *";
        en["WordEditor_PlaceholderDefinitionVi"] = "Clear, concise Vietnamese definition...";
        en["WordEditor_HeaderDefinitionEn"] = "English Definition";
        en["WordEditor_PlaceholderDefinitionEn"] = "English definition (Oxford/Cambridge)...";
        en["WordEditor_HeaderExplanation"] = "Context & Nuance";
        en["WordEditor_PlaceholderExplanation"] = "When to use, formal or informal tone...";
        en["WordEditor_SectionExamples"] = "Example Sentences";
        en["WordEditor_HeaderExampleEn"] = "English Example Sentence";
        en["WordEditor_PlaceholderExampleEn"] = "English example sentence...";
        en["WordEditor_HeaderExampleVi"] = "Vietnamese Translation";
        en["WordEditor_PlaceholderExampleVi"] = "Vietnamese translation...";
        en["WordEditor_BtnAddExample"] = "Add Example";
        en["WordEditor_TooltipDeleteMeaning"] = "Delete this meaning";
        en["WordEditor_TooltipDeleteExample"] = "Delete this example";
        en["WordEditor_SectionTopicsExams"] = "Topics & Exam Tags";
        en["WordEditor_SectionTopicsExamsDesc"] = "Tag for filtering and targeted practice tests.";
        en["WordEditor_HeaderTopics"] = "Study Topics";
        en["WordEditor_HeaderExams"] = "Applicable Exams";
        en["WordEditor_EmptyTopics"] = "No topics available";
        en["WordEditor_EmptyExams"] = "No exam tags available";
        en["WordEditor_AddTopic"] = "Add New Topic";
        en["WordEditor_AddTopicFlyoutTitle"] = "Quick Create Topic";
        // Grammar Editor Dialog
        en["GrammarEditor_TitleAdd"] = "Add New Grammar Rule";
        en["GrammarEditor_TitleEdit"] = "Edit Grammar Rule: {0}";
        en["GrammarEditor_Save"] = "Save Rule";
        en["GrammarEditor_Cancel"] = "Cancel";
        en["GrammarEditor_ValidationTitle"] = "Validation Error";
        en["GrammarEditor_ErrTitleRequired"] = "Please enter a rule title.";
        en["GrammarEditor_ErrStructureRequired"] = "Please enter the structure formula.";
        en["GrammarEditor_ErrExplanationViRequired"] = "Please provide the Vietnamese explanation.";
        en["GrammarEditor_HeaderTitle"] = "Rule Title *";
        en["GrammarEditor_PlaceholderTitle"] = "e.g. Present Perfect Continuous";
        en["GrammarEditor_HeaderStructure"] = "Structure Formula *";
        en["GrammarEditor_PlaceholderStructure"] = "e.g. S + have/has + been + V-ing";
        en["GrammarEditor_HeaderLevel"] = "CEFR Level";
        en["GrammarEditor_HeaderExam"] = "Target Exam";
        en["GrammarEditor_HeaderTopic"] = "Topic";
        en["GrammarEditor_PlaceholderTopic"] = "e.g. Tenses, Passive Voice, Conditionals...";
        en["GrammarEditor_HeaderExplanationVi"] = "Vietnamese Explanation *";
        en["GrammarEditor_PlaceholderExplanationVi"] = "Detailed usage instructions, time indicators, and context...";
        en["GrammarEditor_HeaderExplanationEn"] = "English Explanation (Optional)";
        en["GrammarEditor_PlaceholderExplanationEn"] = "Nuances, usage notes, and exceptions in English...";
        en["GrammarEditor_SectionMistakes"] = "Common Mistakes & Pitfalls";
        en["GrammarEditor_HeaderMistakes"] = "Common Mistakes & Pitfalls";
        en["GrammarEditor_PlaceholderMistakes"] = "Common learner pitfalls, test traps, tips to avoid errors...";
        en["GrammarEditor_SectionExamples"] = "Contextual Examples";
        en["GrammarEditor_SectionExamplesDesc"] = "Add bilingual sample sentences with specific grammatical notes";
        en["GrammarEditor_BtnAddExample"] = "Add Example";
        en["GrammarEditor_ExampleBadge"] = "Example #{0}";
        en["GrammarEditor_HeaderSentenceEn"] = "English Sentence *";
        en["GrammarEditor_PlaceholderSentenceEn"] = "e.g. I have been working here for three years.";
        en["GrammarEditor_HeaderSentenceVi"] = "Vietnamese Translation *";
        en["GrammarEditor_PlaceholderSentenceVi"] = "Accurate Vietnamese translation...";
        en["GrammarEditor_HeaderNote"] = "Grammar Note";
        en["GrammarEditor_PlaceholderNote"] = "Notes on tenses, adverbs, inversions...";
        en["GrammarEditor_TooltipRemoveExample"] = "Remove this example";

        // Exception & Error Dialog
        en["ExceptionDialog_Title"] = "Something went wrong";
        en["ExceptionDialog_Subtitle"] = "The application encountered an unexpected error. Your data remains safe; you can copy the diagnostic details or continue working.";
        en["ExceptionDialog_DetailsHeader"] = "Technical Details (Stack Trace)";
        en["ExceptionDialog_CopyDetails"] = "Copy Details";
        en["ExceptionDialog_Copied"] = "Copied!";
        en["ExceptionDialog_OpenLogs"] = "Open Log Folder";
        en["ExceptionDialog_Close"] = "Close";
        en["Toast_CopyError"] = "Copy";
        en["Toast_ViewDetails"] = "Details";
    }
}
