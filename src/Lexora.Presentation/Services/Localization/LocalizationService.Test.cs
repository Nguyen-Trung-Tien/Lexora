namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterTest(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Test — vi-VN (Tiếng Việt)
        // ==========================================
        // Practice & Test
        vi["Test_Title"] = "Luyện Tập & Kiểm Tra";
        vi["Test_Subtitle"] = "Chọn thể loại câu hỏi và số lượng để kiểm tra mức độ ghi nhớ từ vựng";
        vi["Test_StartTest"] = "Bắt đầu thi";
        vi["Test_ExitTest"] = "Thoát bài thi";
        vi["Test_Question"] = "Câu hỏi";
        vi["Test_SubmitAnswer"] = "Xác nhận đáp án";
        vi["Test_NextQuestion"] = "Câu tiếp theo";
        vi["Test_FinishTest"] = "Hoàn thành bài thi";
        vi["Test_ResultTitle"] = "Kết Quả Bài Kiểm Tra";
        vi["Test_RetakeTest"] = "Làm lại";
        vi["Test_ReviewWeakWords"] = "Ôn từ sai";
        vi["Test_ChooseFormat"] = "1. Chọn dạng bài kiểm tra";
        vi["Test_ChooseFormatDesc"] = "Chọn định dạng câu hỏi để kiểm tra khả năng nhận diện, chính tả hoặc phản xạ từ";
        vi["Test_FormatMixed"] = "Kiểm tra hỗn hợp";
        vi["Test_FormatMixedDesc"] = "Kết hợp đa dạng các dạng bài";
        vi["Test_FormatWordToMeaning"] = "Từ vựng → Ý nghĩa";
        vi["Test_FormatWordToMeaningDesc"] = "Nhìn từ tiếng Anh chọn nghĩa";
        vi["Test_FormatMeaningToWord"] = "Ý nghĩa → Từ vựng";
        vi["Test_FormatMeaningToWordDesc"] = "Nhìn định nghĩa chọn từ đúng";
        vi["Test_FormatFillBlank"] = "Điền vào chỗ trống";
        vi["Test_FormatFillBlankDesc"] = "Điền từ thích hợp vào câu";
        vi["Test_FormatPartOfSpeech"] = "Xác định từ loại";
        vi["Test_FormatPartOfSpeechDesc"] = "Danh từ, động từ, tính từ...";
        vi["Test_FormatAudioQuiz"] = "Luyện nghe chọn từ";
        vi["Test_FormatAudioQuizDesc"] = "Nghe phát âm chuẩn bản ngữ";
        vi["Test_Configuration"] = "2. Cấu hình bài kiểm tra";
        vi["Test_ConfigDesc"] = "Tùy chỉnh số lượng câu hỏi, kỳ thi mục tiêu và phạm vi";
        vi["Test_QuestionCount"] = "Số lượng câu hỏi:";
        vi["Test_Count5"] = "5 Câu";
        vi["Test_Count10"] = "10 Câu";
        vi["Test_Count20"] = "20 Câu";
        vi["Test_Count30"] = "30 Câu";
        vi["Test_QuestionSource"] = "Nguồn từ vựng:";
        vi["Test_FiltersAndTopics"] = "Bộ lọc & Chủ đề";
        vi["Test_CustomSelection"] = "Tự chọn danh sách từ";
        vi["Test_AllExams"] = "Tất cả kỳ thi";
        vi["Test_AllTopics"] = "Tất cả chủ đề";
        vi["Test_AllLevels"] = "Tất cả trình độ";
        vi["Test_ResetFilters"] = "Đặt lại bộ lọc";
        vi["Test_TargetWeakOnly"] = "Chỉ tập trung từ vựng yếu";
        vi["Test_TargetWeakOff"] = "Tắt (Bao gồm tất cả từ)";
        vi["Test_TargetWeakOn"] = "Bật (Tập trung từ chưa nhớ & quên)";
        vi["Test_SearchCustomPool"] = "Tìm kiếm từ trong kho...";
        vi["Test_SelectAll"] = "Chọn tất cả";
        vi["Test_ClearSelection"] = "Bỏ chọn";
        vi["Test_WeakWordsFilter"] = "Từ còn yếu";
        vi["Test_PasteWordList"] = "Dán danh sách từ";
        vi["Test_PastePrompt"] = "Dán danh sách từ vựng (phân tách bởi dấu phẩy hoặc dòng mới):";
        vi["Test_ApplyPasted"] = "Áp dụng";
        vi["Test_SelectedCount"] = "Đã chọn:";
        vi["Test_WordsForTest"] = "từ để kiểm tra";
        vi["Test_ReadyToBegin"] = "Sẵn sàng bắt đầu?";
        vi["Test_ReadyDesc"] = "Rèn luyện khả năng phản xạ và ghi nhận chỉ số SM-2 theo bài luyện tập đã cấu hình.";
        vi["Test_ShortcutsHint"] = "Phím tắt: 1-4: Chọn đáp án | Space: Nghe lại | Enter: Tiếp tục";
        vi["Test_ListenAudio"] = "Nghe phát âm";
        vi["Test_ResultSubtitle"] = "Xem lại tổng kết kết quả và thực hiện kế hoạch ôn tập Lặp lại ngắt quãng (SM-2) đề xuất bên dưới.";
        vi["Test_Correct"] = "Số câu đúng";
        vi["Test_Incorrect"] = "Số câu sai";
        vi["Test_Accuracy"] = "Độ chính xác";
        vi["Test_TimeTaken"] = "Thời gian làm bài";
        vi["Test_ReviewInSm2"] = "Ôn tập SM-2";
        vi["Test_SaveWeakWordsToStorage"] = "Lưu vào kho";
        vi["Test_SavedWeakWordsToStorageSuccess"] = "Đã lưu thành công {0} từ yếu vào Kho lưu trữ cá nhân.";
        vi["Test_WeakWordsSection"] = "Từ vựng cần ôn tập lại";
        vi["Test_NeedsReview"] = "Cần ôn tập";
        vi["Test_DetailedReview"] = "Chi tiết từng câu hỏi & đáp án";
        vi["Test_YourAnswer"] = "Đáp án của bạn:";
        vi["Test_CorrectAnswerPrefix"] = "• Đáp án đúng:";
        vi["Test_SeverityError"] = "Lỗi cấu hình bài thi";
        vi["Test_SeverityWarning"] = "Cảnh báo bài thi";
        vi["Test_SeverityInfo"] = "Thông tin bài thi";
        // Test & Quiz Dynamic Messages
        vi["Quiz_ProgressFormat"] = "Câu {0} / {1}";
        vi["Test_Status_Preparing"] = "Đang chuẩn bị câu hỏi kiểm tra...";
        vi["Test_Status_SelectAtLeastOne"] = "Vui lòng chọn ít nhất 1 từ trong danh sách để kiểm tra.";
        vi["Test_Status_NoQuestions"] = "Không thể sinh câu hỏi. Vui lòng thêm từ vựng trước.";
        vi["Test_Status_PastedMatched"] = "Đã tìm thấy và chọn {0} từ từ danh sách dán.";
        vi["Test_Status_PastedNoMatch"] = "Không tìm thấy từ nào phù hợp trong kho từ vựng.";
        vi["Test_Result_Excellent"] = "Xuất sắc! Khả năng ghi nhớ tuyệt vời!";
        vi["Test_Result_Good"] = "Làm tốt lắm! Tiếp tục duy trì và cải thiện!";
        vi["Test_Result_NeedsPractice"] = "Cần luyện tập thêm. Hãy ôn tập lại các từ bị quên!";
        // Additional Test
        vi["Test_MetricTotalQuestions"] = "Tổng câu hỏi";
        vi["Test_MetricTimeElapsed"] = "Thời gian";
        // Newly Added Test
        vi["Test_ErrorFormat"] = "Lỗi tạo bài thi: {0}";
        vi["Test_FinalizeErrorFormat"] = "Không thể hoàn thành bài thi: {0}";
        vi["Test_QuestionOfFormat"] = "Câu hỏi {0} / {1}";
        // Adaptive Quiz (CAT)
        vi["AdaptiveQuiz_Title"] = "Trắc nghiệm Thích ứng Thông minh (CAT)";
        vi["AdaptiveQuiz_Subtitle"] = "Độ khó tự động điều chỉnh theo năng lực thực tế để ước tính chính xác trình độ";
        vi["AdaptiveQuiz_Start"] = "Bắt đầu bài thi thích ứng";
        vi["AdaptiveQuiz_EstimatedCefr"] = "Ước tính trình độ";
        vi["AdaptiveQuiz_QuestionProgress"] = "Câu {0}/{1}";
        vi["AdaptiveQuiz_SummaryTitle"] = "Báo cáo Kết quả Trắc nghiệm Thích ứng";
        vi["AdaptiveQuiz_EstimatedScores"] = "Dự đoán Điểm thi Chứng chỉ";
        vi["AdaptiveQuiz_ReviewWeakWords"] = "Thêm các từ sai vào Ôn tập";
        vi["AdaptiveQuiz_FinalLevelFormat"] = "Trình độ ước tính: {0}";
        vi["AdaptiveQuiz_CatModeTitle"] = "Kiểm tra thích ứng (CAT)";
        vi["AdaptiveQuiz_CatModeDesc"] = "Thuật toán tự động hiệu chỉnh độ khó CEFR của câu hỏi tiếp theo dựa trên độ chính xác";
        vi["AdaptiveQuiz_CurrentLevel"] = "Trình độ hiện tại";
        vi["AdaptiveQuiz_ReportTitle"] = "Báo cáo phân tích bài kiểm tra thích ứng";
        vi["AdaptiveQuiz_ReportDesc"] = "Đánh giá chi tiết năng lực ngôn ngữ theo khung tham chiếu chuẩn CEFR";
        vi["AdaptiveQuiz_AiCalibrated"] = "Chuẩn hóa AI";
        vi["AdaptiveQuiz_CatCalibration"] = "Định chuẩn CAT";
        vi["AdaptiveQuiz_FinalCefr"] = "CEFR cuối cùng:";
        vi["AdaptiveQuiz_ToeicEstimated"] = "Dự đoán TOEIC";
        vi["AdaptiveQuiz_IeltsEstimated"] = "Dự đoán IELTS";
        vi["AdaptiveQuiz_VstepEstimated"] = "Dự đoán VSTEP";
        vi["Test_Help_Title"] = "Trợ giúp: Bài kiểm tra & Thi thử";
        vi["Test_Help_Subtitle"] = "Kiểm tra năng lực thích ứng (CAT)";
        vi["Test_Help_Content"] = "Chọn chế độ Luyện tập theo chủ đề hoặc Thi chuẩn hóa thích ứng CAT. Dùng phím 1-4 để chọn đáp án và Enter để xác nhận nộp bài.";

        // ==========================================
        // Test — en-US (English)
        // ==========================================
        // Practice & Test
        en["Test_Title"] = "Practice & Test";
        en["Test_Subtitle"] = "Choose a question format and length to test your retention";
        en["Test_StartTest"] = "Start Test";
        en["Test_ExitTest"] = "Exit Test";
        en["Test_Question"] = "Question";
        en["Test_SubmitAnswer"] = "Submit Answer";
        en["Test_NextQuestion"] = "Next Question";
        en["Test_FinishTest"] = "Finish Test";
        en["Test_ResultTitle"] = "Test Results";
        en["Test_RetakeTest"] = "Retake";
        en["Test_ReviewWeakWords"] = "Review Mistakes";
        en["Test_ChooseFormat"] = "1. Choose Test Format";
        en["Test_ChooseFormatDesc"] = "Select question style to test recognition, spelling, or active recall";
        en["Test_FormatMixed"] = "Mixed Test";
        en["Test_FormatMixedDesc"] = "Balanced combination";
        en["Test_FormatWordToMeaning"] = "Word → Meaning";
        en["Test_FormatWordToMeaningDesc"] = "Term to definition";
        en["Test_FormatMeaningToWord"] = "Meaning → Word";
        en["Test_FormatMeaningToWordDesc"] = "Definition to term";
        en["Test_FormatFillBlank"] = "Fill in Blank";
        en["Test_FormatFillBlankDesc"] = "Context sentence blanks";
        en["Test_FormatPartOfSpeech"] = "Part of Speech";
        en["Test_FormatPartOfSpeechDesc"] = "Noun, Verb, Adj, etc.";
        en["Test_FormatAudioQuiz"] = "Audio Quiz";
        en["Test_FormatAudioQuizDesc"] = "Native audio quiz";
        en["Test_Configuration"] = "2. Test Configuration";
        en["Test_ConfigDesc"] = "Configure question count, target exams, and scope";
        en["Test_QuestionCount"] = "Number of Questions:";
        en["Test_Count5"] = "5 Questions";
        en["Test_Count10"] = "10 Questions";
        en["Test_Count20"] = "20 Questions";
        en["Test_Count30"] = "30 Questions";
        en["Test_QuestionSource"] = "Question Source:";
        en["Test_FiltersAndTopics"] = "Filters & Topics";
        en["Test_CustomSelection"] = "Custom Word Selection";
        en["Test_AllExams"] = "All Exams";
        en["Test_AllTopics"] = "All Topics";
        en["Test_AllLevels"] = "All Levels";
        en["Test_ResetFilters"] = "Reset Filters";
        en["Test_TargetWeakOnly"] = "Target Weak Words Only";
        en["Test_TargetWeakOff"] = "Off (Include all words)";
        en["Test_TargetWeakOn"] = "On (Focus on lapsed & low-confidence words)";
        en["Test_SearchCustomPool"] = "Search word pool...";
        en["Test_SelectAll"] = "Select All";
        en["Test_ClearSelection"] = "Clear";
        en["Test_WeakWordsFilter"] = "Weak Words";
        en["Test_PasteWordList"] = "Paste Word List";
        en["Test_PastePrompt"] = "Paste vocabulary terms (separated by commas or newlines):";
        en["Test_ApplyPasted"] = "Apply";
        en["Test_SelectedCount"] = "Selected:";
        en["Test_WordsForTest"] = "words for test";
        en["Test_ReadyToBegin"] = "Ready to begin?";
        en["Test_ReadyDesc"] = "Sharpen recall accuracy and log SM-2 metrics with your configured drill.";
        en["Test_ShortcutsHint"] = "Shortcuts: 1-4: Select | Space: Audio | Enter: Next";
        en["Test_ListenAudio"] = "Listen Audio";
        en["Test_ResultSubtitle"] = "Review your performance summary and follow the recommended Spaced Repetition study plan below.";
        en["Test_Correct"] = "Correct";
        en["Test_Incorrect"] = "Incorrect";
        en["Test_Accuracy"] = "Accuracy";
        en["Test_TimeTaken"] = "Time Taken";
        en["Test_ReviewInSm2"] = "Review in SM-2";
        en["Test_SaveWeakWordsToStorage"] = "Save to Storage";
        en["Test_SavedWeakWordsToStorageSuccess"] = "Successfully saved {0} weak words to Learning Storage.";
        en["Test_WeakWordsSection"] = "Weak Words Requiring Review";
        en["Test_NeedsReview"] = "Needs Review";
        en["Test_DetailedReview"] = "Detailed Questions & Answers Review";
        en["Test_YourAnswer"] = "Your answer:";
        en["Test_CorrectAnswerPrefix"] = "• Correct answer:";
        en["Test_SeverityError"] = "Test Configuration Error";
        en["Test_SeverityWarning"] = "Test Warning";
        en["Test_SeverityInfo"] = "Test Information";
        // Test & Quiz Dynamic Messages
        en["Quiz_ProgressFormat"] = "Question {0} of {1}";
        en["Test_Status_Preparing"] = "Preparing test questions...";
        en["Test_Status_SelectAtLeastOne"] = "Please select at least 1 word from the list to test.";
        en["Test_Status_NoQuestions"] = "No questions could be generated. Please add more vocabulary first.";
        en["Test_Status_PastedMatched"] = "Found and selected {0} words from pasted list.";
        en["Test_Status_PastedNoMatch"] = "No matching words found in vocabulary pool.";
        en["Test_Result_Excellent"] = "Excellent! Outstanding mastery!";
        en["Test_Result_Good"] = "Good job! Keep reviewing to improve!";
        en["Test_Result_NeedsPractice"] = "Needs practice. Let's review the weak words!";
        // Additional Test
        en["Test_MetricTotalQuestions"] = "Total Questions";
        en["Test_MetricTimeElapsed"] = "Time Elapsed";
        // Newly Added Test
        en["Test_ErrorFormat"] = "Error creating test: {0}";
        en["Test_FinalizeErrorFormat"] = "Failed to finalize test: {0}";
        en["Test_QuestionOfFormat"] = "Question {0} of {1}";
        // Adaptive Quiz (CAT)
        en["AdaptiveQuiz_Title"] = "Computerized Adaptive Testing (CAT)";
        en["AdaptiveQuiz_Subtitle"] = "Dynamically calibrates question difficulty to measure your true CEFR level";
        en["AdaptiveQuiz_Start"] = "Start Adaptive Quiz";
        en["AdaptiveQuiz_EstimatedCefr"] = "Estimated Level";
        en["AdaptiveQuiz_QuestionProgress"] = "Question {0}/{1}";
        en["AdaptiveQuiz_SummaryTitle"] = "Adaptive Diagnostic Quiz Report";
        en["AdaptiveQuiz_EstimatedScores"] = "Estimated Exam Score Projections";
        en["AdaptiveQuiz_ReviewWeakWords"] = "Add Weak Words to Review";
        en["AdaptiveQuiz_FinalLevelFormat"] = "Estimated Level: {0}";
        en["AdaptiveQuiz_CatModeTitle"] = "Computerized Adaptive Testing (CAT)";
        en["AdaptiveQuiz_CatModeDesc"] = "Algorithm automatically recalibrates question CEFR difficulty based on real-time accuracy";
        en["AdaptiveQuiz_CurrentLevel"] = "Estimated Level";
        en["AdaptiveQuiz_ReportTitle"] = "Adaptive Diagnostic Quiz Report";
        en["AdaptiveQuiz_ReportDesc"] = "In-depth CEFR linguistic proficiency diagnosis and cognitive trajectory";
        en["AdaptiveQuiz_AiCalibrated"] = "AI Calibrated";
        en["AdaptiveQuiz_CatCalibration"] = "CAT Calibration";
        en["AdaptiveQuiz_FinalCefr"] = "Final CEFR:";
        en["AdaptiveQuiz_ToeicEstimated"] = "TOEIC Estimated";
        en["AdaptiveQuiz_IeltsEstimated"] = "IELTS Estimated";
        en["AdaptiveQuiz_VstepEstimated"] = "VSTEP Estimated";
        en["Test_Help_Title"] = "Quick Help: Practice & Testing";
        en["Test_Help_Subtitle"] = "Adaptive evaluation engine (CAT)";
        en["Test_Help_Content"] = "Choose between Topic Practice or standardized Computer Adaptive Testing (CAT). Use keys 1-4 to choose options and Enter to submit.";
    }
}
