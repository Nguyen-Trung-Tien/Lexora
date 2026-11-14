namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterHelp(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Help — vi-VN (Tiếng Việt)
        // ==========================================
        vi["ReleaseNotes_DialogTitle"] = "Nhật ký Phát hành";
        vi["ReleaseNotes_DialogSubtitle"] = "Lịch sử cập nhật và cải tiến chi tiết qua từng phiên bản của Lexora";
        vi["ReleaseNotes_SelectVersion"] = "Chọn phiên bản:";
        vi["ReleaseNotes_CurrentBadge"] = "Phiên bản hiện tại";
        vi["ReleaseNotes_CurrentSuffix"] = "(Hiện tại)";
        vi["ReleaseNotes_NotesContentLabel"] = "Chi tiết tính năng & Cải tiến";
        vi["ReleaseNotes_OfflineNote"] = "Toàn bộ nhật ký phiên bản được lưu trữ ngoại tuyến trực tiếp trong ứng dụng Lexora.";
        vi["ReleaseNotes_CloseButton"] = "Đóng";
        vi["Help_ReleaseNotesLog"] = "Nhật ký phát hành";
        vi["Help_ReleaseNotesLogTooltip"] = "Xem các tính năng mới và cải tiến trong các phiên bản Lexora";
        // ==========================================
        // USER GUIDE & ONBOARDING HUB (vi-VN)
        // ==========================================
        vi["Help_Title"] = "Hướng dẫn sử dụng Lexora";
        vi["Help_Subtitle"] = "Tài liệu chỉ dẫn toàn diện: Thuật toán Spaced Repetition (SM-2 & FSRS), Quản lý từ vựng, Phòng luyện nghe và Hệ thống bài thi thích ứng CAT.";
        vi["Help_HeroBadge"] = "TRUNG TÂM HƯỚNG DẪN & TRỢ GIÚP";
        vi["Help_OnboardingSystem"] = "Hệ thống chỉ dẫn toàn diện";
        vi["Help_OfflineBadge"] = "Hoạt động Ngoại tuyến & SM-2/FSRS";
        vi["Help_SearchPlaceholder"] = "Tìm kiếm hướng dẫn (SM-2, import CSV, phát âm, CAT, sao lưu)...";
        vi["Help_ResetFilter"] = "Đặt lại";
        vi["Help_ResetFilterTooltip"] = "Xóa bộ lọc và tìm kiếm";
        vi["Help_OpenFullGuide"] = "Xem hướng dẫn chi tiết";
        vi["Help_ScrollLeft"] = "Cuộn danh mục sang trái";
        vi["Help_ScrollRight"] = "Cuộn danh mục sang phải";
        vi["Help_ShortcutsTitle"] = "Phím tắt thao tác nhanh:";
        vi["Help_AlgorithmFormulaTitle"] = "Chi tiết giải thuật & Công thức tính:";
        vi["Help_KeyPointsTitle"] = "Các điểm cốt lõi cần nhớ:";
        vi["Help_FooterNoteTitle"] = "100% Khả năng hoạt động Ngoại tuyến (Offline-First)";
        vi["Help_FooterNoteContent"] = "Toàn bộ dữ liệu từ vựng, âm thanh đã tải, tiến độ ôn tập và tài liệu hướng dẫn đều được lưu trữ trực tiếp trên máy của bạn. Lexora không đòi hỏi kết nối Internet thường trực và cam kết bảo vệ toàn vẹn dữ liệu cá nhân.";
        // Categories
        vi["Help_Cat_All"] = "Tất cả";
        vi["Help_Cat_GettingStarted"] = "Bắt đầu & Tổng quan";
        vi["Help_Cat_Vocabulary"] = "Kho từ & Ngữ pháp";
        vi["Help_Cat_SpacedRepetition"] = "Lặp lại ngắt quãng (SM-2/FSRS)";
        vi["Help_Cat_ListeningLab"] = "Phát âm & Luyện nghe";
        vi["Help_Cat_TestMode"] = "Luyện tập & Thi thử (CAT)";
        vi["Help_Cat_BackupSettings"] = "Sao lưu & Cài đặt";
        // Badges
        vi["Help_Badge_Core"] = "CỐT LÕI";
        vi["Help_Badge_Essential"] = "CƠ BẢN";
        vi["Help_Badge_Algorithm"] = "THUẬT TOÁN";
        vi["Help_Badge_Pronunciation"] = "LUYỆN ÂM";
        vi["Help_Badge_ExamPrep"] = "LUYỆN THI";
        vi["Help_Badge_Safety"] = "AN TOÀN";
        // Action Buttons
        vi["Help_Action_GoDashboard"] = "Mở Tổng quan";
        vi["Help_Action_GoVocabulary"] = "Mở Kho từ vựng";
        vi["Help_Action_GoReview"] = "Bắt đầu Ôn tập";
        vi["Help_Action_GoHomophones"] = "Mở Từ đồng âm";
        vi["Help_Action_GoTest"] = "Làm bài kiểm tra";
        vi["Help_Action_GoSettings"] = "Mở Cài đặt";
        // Guide 1: Getting Started & Dashboard
        vi["Help_Guide_GettingStarted_Title"] = "Tổng quan Dashboard & Chỉ số học tập";
        vi["Help_Guide_GettingStarted_Subtitle"] = "Làm chủ màn hình tổng quan, theo dõi chuỗi học tập (Streak) và mức độ ghi nhớ dài hạn.";
        vi["Help_Guide_GettingStarted_Summary"] = "Dashboard là trạm chỉ huy trung tâm cung cấp góc nhìn 360 độ về lộ trình học từ vựng, biểu đồ nhiệt học tập và dự báo thẻ đến hạn.";
        vi["Help_Guide_GettingStarted_Content"] = "Mỗi ngày khi khởi động Lexora, hệ thống tự động quét và tính toán số lượng thẻ đến hạn ôn tập theo đường cong quên Ebbinghaus. Hero Banner ở đầu trang cho phép bạn kích hoạt phiên học chỉ với 1 cú nhấp chuột. Biểu đồ Heatmap trực quan hóa tính kỷ luật học tập theo từng ngày trong năm. Biểu đồ Retention Curve và Memory Health phân loại tình trạng từ vựng theo khung tham chiếu châu Âu CEFR (A1 đến C2) và chứng chỉ mục tiêu (TOEIC, IELTS, VSTEP).";
        vi["Help_Tip_GettingStarted_1"] = "Duy trì Streak đều đặn mỗi ngày (dù chỉ 5-10 phút) giúp củng cố liên kết nơ-ron tốt hơn nhiều so với việc học dồn dập cuối tuần.";
        vi["Help_Tip_GettingStarted_2"] = "Theo dõi mục Thẻ đến hạn (Due Cards) để không bỏ lỡ thời điểm vàng củng cố trí nhớ trước khi thông tin phai nhạt.";
        vi["Help_Tip_GettingStarted_3"] = "Sử dụng phím tắt Ctrl+K để tra cứu nhanh bất kỳ từ vựng hoặc khái niệm ngữ pháp nào trong tích tắc.";
        // Guide 2: Vocabulary & Grammar
        vi["Help_Guide_Vocab_Title"] = "Quản lý kho từ vựng, Ngữ pháp & Dữ liệu";
        vi["Help_Guide_Vocab_Subtitle"] = "Thêm từ mới, phân loại chủ đề (Topic Units), nhập/xuất CSV/JSON và quản lý câu ví dụ.";
        vi["Help_Guide_Vocab_Summary"] = "Hệ thống từ vựng Lexora được chuẩn hóa theo mô hình miền: Từ vựng -> Nhiều lớp nghĩa -> Câu ví dụ ngữ cảnh -> Đơn vị chủ đề -> Âm thanh chuẩn IPA.";
        vi["Help_Guide_Vocab_Content"] = "Bạn có thể tổ chức từ vựng theo 2 chế độ hiển thị: Bảng danh sách chi tiết hoặc Nhóm chủ đề (Topic Units). Khi thêm từ mới, hệ thống tự động hỗ trợ phiên âm IPA chuẩn UK/US, phân loại cấp độ CEFR và ngữ cảnh bài thi. Tính năng Import/Export thông minh cho phép bạn sao lưu hoặc nhập hàng ngàn từ từ Anki, Excel hoặc từ điển cá nhân với cơ chế chống trùng lặp tự động.";
        vi["Help_Tip_Vocab_1"] = "Luôn gắn kèm câu ví dụ thực tế cho mỗi từ vựng để não bộ ghi nhớ ngữ cảnh ứng dụng thay vì học vẹt nghĩa tiếng Việt.";
        vi["Help_Tip_Vocab_2"] = "Có thể chọn nhiều từ cùng lúc (chế độ Chọn hàng loạt) để gán topic hoặc xuất dữ liệu nhanh chóng.";
        vi["Help_Tip_Vocab_3"] = "Định dạng file CSV import chuẩn: term,ipa,meaning,pos,example,cefr,exam (hệ thống tự động phát hiện header).";
        vi["Help_Tip_Vocab_4"] = "Tận dụng Kho lưu trữ (Learning Storage) để lưu lại các câu trích đoạn hay từ sách báo tiếng Anh.";
        // Guide 3: Spaced Repetition (SM-2 & FSRS)
        vi["Help_Guide_SpacedRepetition_Title"] = "Cơ chế Lặp lại ngắt quãng (SM-2 & FSRS Deep-Dive)";
        vi["Help_Guide_SpacedRepetition_Subtitle"] = "Hiểu rõ ý nghĩa 4 mức đánh giá (Again, Hard, Good, Easy) và công thức tính chu kỳ tối ưu.";
        vi["Help_Guide_SpacedRepetition_Summary"] = "Thuật toán tính toán chính xác thời điểm bạn sắp quên một từ để đưa ra ôn tập, giúp ghi nhớ vĩnh viễn với số lần ôn ít nhất.";
        vi["Help_Guide_SpacedRepetition_Formula"] = "SM-2: EF' = EF + (0.1 - (4 - q) * (0.08 + (4 - q) * 0.02)), EF >= 1.3\nChu kỳ: I(1)=1 ngày, I(2)=6 ngày, I(n)=I(n-1)*EF. (q: Điểm chất lượng từ 0 đến 4)\nFSRS: S(t) = S * (1 + Factor * Retrievability) dựa trên độ ổn định (Stability) và khả năng gợi nhớ.";
        vi["Help_Guide_SpacedRepetition_Content"] = "Trong giao diện ôn tập (Review Page), bạn sẽ thấy mặt trước thẻ (từ vựng, phiên âm, nút nghe phát âm). Sau khi tự nhớ lại nghĩa, nhấn Phím Cách (Space) để lật thẻ và chọn 1 trong 4 mức đánh giá phù hợp:\n• Again (Phím 1): Quên hoàn toàn -> Thẻ sẽ được đưa về chu kỳ 1 ngày để học lại ngay.\n• Hard (Phím 2): Nhớ ra nhưng mất nhiều thời gian do dự -> Hệ số EF giảm nhẹ, chu kỳ tăng chậm.\n• Good (Phím 3): Nhớ đúng với nỗ lực tiêu chuẩn -> Chu kỳ nhân theo hệ số Ease Factor hiện tại.\n• Easy (Phím 4): Nhớ tức thì, không tốn chút nỗ lực nào -> Hệ số EF tăng, chu kỳ nhân thêm thưởng.";
        vi["Help_Tip_SpacedRepetition_Again"] = "Again (0-1): Đừng ngần ngại bấm Again nếu bạn quên hoặc dịch sai. Đây là chìa khóa để thuật toán sửa chữa lỗ hổng kiến thức.";
        vi["Help_Tip_SpacedRepetition_Hard"] = "Hard (2): Dùng khi bạn mất hơn 5 giây suy nghĩ hoặc nhầm lẫn một phần ngữ nghĩa.";
        vi["Help_Tip_SpacedRepetition_Good"] = "Good (3): Lựa chọn tiêu chuẩn cho hầu hết các lần nhớ đúng bình thường.";
        vi["Help_Tip_SpacedRepetition_Easy"] = "Easy (4): Chỉ dùng cho những từ bạn đã thuộc như lòng bàn tay và phản xạ tức thì.";
        // Guide 4: Listening Lab & Homophones
        vi["Help_Guide_ListeningLab_Title"] = "Luyện phát âm, Từ đồng âm & Listening Lab";
        vi["Help_Guide_ListeningLab_Subtitle"] = "Phân biệt các cặp âm dễ nhầm, luyện nghe phản xạ và tránh bẫy nghe trong đề thi.";
        vi["Help_Guide_ListeningLab_Summary"] = "Khắc phục triệt để điểm yếu phát âm và nghe của người học qua phòng lab âm thanh đối sánh UK - US và bài tập phân biệt từ đồng âm.";
        vi["Help_Guide_ListeningLab_Content"] = "Tính năng Homophones & Sound-Alikes phân loại các nhóm từ phát âm giống hệt hoặc tương tự nhau (như 'affect' vs 'effect', 'accept' vs 'except', 'their' vs 'there'). Phòng luyện nghe Listening Lab hỗ trợ 3 chế độ chuyên sâu: Đối sánh âm sắc UK/US, Trắc nghiệm phân biệt cặp âm trong 3 giây, và Nghe chép chính tả (Dictation Drill). Bạn có thể điều chỉnh tốc độ từ 0.75x đến 1.5x để rèn luyện thính giác theo từng giai đoạn.";
        vi["Help_Tip_ListeningLab_1"] = "Nghe kỹ sự khác biệt giữa nguyên âm ngắn / dài (ví dụ: /ɪ/ trong 'ship' vs /iː/ trong 'sheep').";
        vi["Help_Tip_ListeningLab_2"] = "Luyện tập chế độ Nghe chép chính tả để hoàn thiện cả kỹ năng nghe lẫn độ chính xác chính tả.";
        vi["Help_Tip_ListeningLab_3"] = "Chuyển đổi linh hoạt giữa giọng Anh - Anh và Anh - Mỹ bằng phím tắt Alt+U và Alt+A.";
        // Guide 5: Test Mode & CAT
        vi["Help_Guide_Test_Title"] = "Luyện tập & Thi thử thích ứng (CAT Engine)";
        vi["Help_Guide_Test_Subtitle"] = "Đo lường năng lực thực tế với thuật toán điều chỉnh độ khó thích ứng theo câu trả lời.";
        vi["Help_Guide_Test_Summary"] = "Hệ thống kiểm tra thông minh không tạo đề ngẫu nhiên nhàm chán mà liên tục điều chỉnh độ khó để xác định chính xác trình độ CEFR của bạn.";
        vi["Help_Guide_Test_Content"] = "Lexora cung cấp 3 hình thức kiểm tra: Ôn tập nhanh 10-20 câu, Bài kiểm tra theo Chủ đề đã học, và Bài thi chuẩn hóa thích ứng (CAT - Computer Adaptive Testing). Trong bài thi CAT, nếu bạn trả lời đúng câu hỏi ở mức B1, hệ thống sẽ tự động nâng độ khó lên câu hỏi B2/C1. Nếu bạn trả lời sai, hệ thống sẽ lùi về mức A2 để kiểm chứng độ vững vàng của nền tảng. Sau khi hoàn thành, bạn nhận được báo cáo phân tích chi tiết từng kỹ năng và đề xuất học bổ sung.";
        vi["Help_Tip_Test_1"] = "Thi thử định kỳ 2 tuần một lần để đánh giá sự tiến bộ của vốn từ vựng trong dài hạn.";
        vi["Help_Tip_Test_2"] = "Xem lại kỹ danh sách 'Câu trả lời sai' sau bài thi để tự động thêm các từ đó vào danh sách ưu tiên ôn tập.";
        vi["Help_Tip_Test_3"] = "Có thể sử dụng các phím số 1, 2, 3, 4 trên bàn phím để chọn nhanh đáp án A, B, C, D.";
        // Guide 6: Backup & Settings
        vi["Help_Guide_Settings_Title"] = "Sao lưu CSDL SQLite & Tùy biến hệ thống";
        vi["Help_Guide_Settings_Subtitle"] = "Bảo vệ toàn vẹn dữ liệu học tập với cơ chế sao lưu tự động và kiểm tra SQLite Header.";
        vi["Help_Guide_Settings_Summary"] = "Toàn bộ thành quả học tập của bạn được lưu trữ an toàn trong cơ sở dữ liệu SQLite cục bộ chuẩn ACID, không phụ thuộc vào bất kỳ dịch vụ đám mây bên thứ ba nào.";
        vi["Help_Guide_Settings_Content"] = "Tại trang Cài đặt (Settings), bạn có thể: Tạo bản sao lưu tức thì (.db), Lên lịch sao lưu tự động hàng ngày/tuần, Phục hồi từ bản sao lưu cũ với cơ chế xác thực an toàn tuyệt đối (kiểm tra SQLite Header, xóa connection pool và rollback dự phòng nếu lỗi). Ngoài ra, bạn có thể tự do chuyển đổi giữa giao diện Sáng / Tối / Hệ thống, lựa chọn thuật toán SM-2 hoặc FSRS và tinh chỉnh tốc độ phát âm thanh mặc định.";
        vi["Help_Tip_Settings_1"] = "Nên xuất bản sao lưu ra thư mục lưu trữ ngoài (USB hoặc Google Drive/OneDrive) định kỳ mỗi tháng.";
        vi["Help_Tip_Settings_2"] = "Lexora tự động giữ lại tối đa 10 bản sao lưu gần nhất trong thư mục an toàn của hệ điều hành.";
        vi["Help_Tip_Settings_3"] = "Nhấn Ctrl+T bất kỳ lúc nào để chuyển đổi nhanh giữa chế độ Sáng và Tối.";

        // ==========================================
        // Help — en-US (English)
        // ==========================================
        en["ReleaseNotes_DialogTitle"] = "Release Notes Log";
        en["ReleaseNotes_DialogSubtitle"] = "Detailed update history and feature improvements across Lexora versions";
        en["ReleaseNotes_SelectVersion"] = "Select version:";
        en["ReleaseNotes_CurrentBadge"] = "Current Version";
        en["ReleaseNotes_CurrentSuffix"] = "(Current)";
        en["ReleaseNotes_NotesContentLabel"] = "Features & Improvements Details";
        en["ReleaseNotes_OfflineNote"] = "All release notes are stored offline directly within the Lexora desktop application.";
        en["ReleaseNotes_CloseButton"] = "Close";
        en["Help_ReleaseNotesLog"] = "Release Notes Log";
        en["Help_ReleaseNotesLogTooltip"] = "View what's new and recent improvements in Lexora versions";
        // ==========================================
        // USER GUIDE & ONBOARDING HUB (en-US)
        // ==========================================
        en["Help_Title"] = "Lexora User Guide & Documentation";
        en["Help_Subtitle"] = "Comprehensive documentation covering Spaced Repetition (SM-2 & FSRS), Vocabulary Management, Listening Lab, and Computer Adaptive Testing (CAT).";
        en["Help_HeroBadge"] = "HELP & USER GUIDE HUB";
        en["Help_OnboardingSystem"] = "Comprehensive Onboarding System";
        en["Help_OfflineBadge"] = "Offline-First & SM-2/FSRS";
        en["Help_SearchPlaceholder"] = "Search user guides (SM-2, import CSV, pronunciation, CAT, backup)...";
        en["Help_ResetFilter"] = "Reset";
        en["Help_ResetFilterTooltip"] = "Clear filters and search query";
        en["Help_OpenFullGuide"] = "View Full Guide";
        en["Help_ScrollLeft"] = "Scroll categories left";
        en["Help_ScrollRight"] = "Scroll categories right";
        en["Help_ShortcutsTitle"] = "Quick Keyboard Shortcuts:";
        en["Help_AlgorithmFormulaTitle"] = "Algorithm Formulation & Calculation Rules:";
        en["Help_KeyPointsTitle"] = "Key Takeaways & Best Practices:";
        en["Help_FooterNoteTitle"] = "100% Offline-First Architecture";
        en["Help_FooterNoteContent"] = "All vocabulary data, cached audio streams, review history, and user guide documentation reside strictly on your local machine. Lexora does not require persistent Internet connectivity and guarantees complete privacy.";
        // Categories
        en["Help_Cat_All"] = "All";
        en["Help_Cat_GettingStarted"] = "Getting Started & Dashboard";
        en["Help_Cat_Vocabulary"] = "Vocabulary & Grammar";
        en["Help_Cat_SpacedRepetition"] = "Spaced Repetition (SM-2/FSRS)";
        en["Help_Cat_ListeningLab"] = "Pronunciation & Listening Lab";
        en["Help_Cat_TestMode"] = "Practice & CAT Exam";
        en["Help_Cat_BackupSettings"] = "Backup & Settings";
        // Badges
        en["Help_Badge_Core"] = "CORE";
        en["Help_Badge_Essential"] = "ESSENTIAL";
        en["Help_Badge_Algorithm"] = "ALGORITHM";
        en["Help_Badge_Pronunciation"] = "PRONUNCIATION";
        en["Help_Badge_ExamPrep"] = "EXAM PREP";
        en["Help_Badge_Safety"] = "DATA SAFETY";
        // Action Buttons
        en["Help_Action_GoDashboard"] = "Open Dashboard";
        en["Help_Action_GoVocabulary"] = "Open Vocabulary";
        en["Help_Action_GoReview"] = "Start Review";
        en["Help_Action_GoHomophones"] = "Open Homophones";
        en["Help_Action_GoTest"] = "Start Test";
        en["Help_Action_GoSettings"] = "Open Settings";
        // Guide 1: Getting Started & Dashboard
        en["Help_Guide_GettingStarted_Title"] = "Dashboard Overview & Learning Intelligence";
        en["Help_Guide_GettingStarted_Subtitle"] = "Master the dashboard, monitor learning streaks, and evaluate long-term memory retention.";
        en["Help_Guide_GettingStarted_Summary"] = "The Dashboard is your central mission control providing a 360-degree view of your learning journey, streak consistency, and due review forecasts.";
        en["Help_Guide_GettingStarted_Content"] = "Every day when opening Lexora, the system automatically computes review cards due according to the Ebbinghaus forgetting curve. The top Hero Banner lets you jump into study immediately with a single click. The heatmap visualizes weekly study consistency, while Retention Curves and Memory Health segments classify vocabulary across CEFR levels (A1 to C2) and target exams (TOEIC, IELTS, VSTEP).";
        en["Help_Tip_GettingStarted_1"] = "Maintaining a daily study streak (even 5-10 minutes) reinforces neural memory pathways far more effectively than cramming.";
        en["Help_Tip_GettingStarted_2"] = "Keep an eye on Due Cards to review at the optimal retention point before forgetting occurs.";
        en["Help_Tip_GettingStarted_3"] = "Use Ctrl+K for instant intelligent global search across all words, idioms, and grammar points.";
        // Guide 2: Vocabulary & Grammar
        en["Help_Guide_Vocab_Title"] = "Vocabulary, Grammar & Storage Management";
        en["Help_Guide_Vocab_Subtitle"] = "Add words, categorize by topics, import/export CSV/JSON, and extract rich contextual sentences.";
        en["Help_Guide_Vocab_Summary"] = "Lexora's vocabulary engine follows strict domain modeling: Word -> Multiple Meanings -> Example Sentences -> Context Units -> Audio IPA.";
        en["Help_Guide_Vocab_Content"] = "Organize your vocabulary in two view modes: Detailed Data Table or Grouped Topic Units. When adding words, the editor provides dual UK/US IPA phonetics, CEFR levels, and exam tags. Smart CSV/JSON Import/Export lets you batch import thousands of words from Anki, Excel, or personal glossaries with built-in deduplication.";
        en["Help_Tip_Vocab_1"] = "Always attach real-world example sentences so your brain remembers practical collocations rather than isolated definitions.";
        en["Help_Tip_Vocab_2"] = "Use batch selection mode to reassign topics, export, or manage multiple terms simultaneously.";
        en["Help_Tip_Vocab_3"] = "Standard CSV import format: term,ipa,meaning,pos,example,cefr,exam (with automatic header detection).";
        en["Help_Tip_Vocab_4"] = "Leverage Learning Storage to collect inspiring sentences and contextual idioms from English reading materials.";
        // Guide 3: Spaced Repetition (SM-2 & FSRS)
        en["Help_Guide_SpacedRepetition_Title"] = "Spaced Repetition Deep-Dive (SM-2 & FSRS Engine)";
        en["Help_Guide_SpacedRepetition_Subtitle"] = "Understand the 4 rating tiers (Again, Hard, Good, Easy) and algorithmic interval calculations.";
        en["Help_Guide_SpacedRepetition_Summary"] = "The algorithm predicts the exact moment of memory decay and schedules reviews to cement permanent recall with minimal repetition.";
        en["Help_Guide_SpacedRepetition_Formula"] = "SM-2: EF' = EF + (0.1 - (4 - q) * (0.08 + (4 - q) * 0.02)), EF >= 1.3\nIntervals: I(1)=1 day, I(2)=6 days, I(n)=I(n-1)*EF. (q: Rating 0 to 4)\nFSRS: Stability-based interval targeting desired memory retrievability.";
        en["Help_Guide_SpacedRepetition_Content"] = "In the Review Page, you are presented with the front card (term, IPA, audio). After attempting recall, press Space to flip and choose among 4 ratings:\n• Again (Key 1): Complete lapse -> resets interval to 1 day for prompt reinforcement.\n• Hard (Key 2): Recalled with notable hesitation -> EF decreases slightly, interval grows modestly.\n• Good (Key 3): Correct recall with standard effort -> interval scaled by current Ease Factor.\n• Easy (Key 4): Instant effortless recall -> EF increases and interval receives an extra bonus multiplier.";
        en["Help_Tip_SpacedRepetition_Again"] = "Again (0-1): Never hesitate to hit Again on lapses. Honesty with the algorithm is critical to memory consolidation.";
        en["Help_Tip_SpacedRepetition_Hard"] = "Hard (2): Use when recall took >5 seconds of struggle or slight semantic confusion.";
        en["Help_Tip_SpacedRepetition_Good"] = "Good (3): The default standard choice for normal, correct recall.";
        en["Help_Tip_SpacedRepetition_Easy"] = "Easy (4): Reserve strictly for words you know like the back of your hand with instant reflex.";
        // Guide 4: Listening Lab & Homophones
        en["Help_Guide_ListeningLab_Title"] = "Pronunciation, Homophones & Listening Lab";
        en["Help_Guide_ListeningLab_Subtitle"] = "Distinguish minimal pairs, master native accents, and avoid common listening test traps.";
        en["Help_Guide_ListeningLab_Summary"] = "Eradicate pronunciation and acoustic confusion through comparative UK-US audio labs and minimal pair drills.";
        en["Help_Guide_ListeningLab_Content"] = "Homophones & Sound-Alikes categorizes terms with identical or deceptively similar phonetics (e.g. affect/effect, accept/except, their/there). The Listening Lab features 3 dedicated modes: Dual UK/US Accent Comparison, Rapid 3-Second Ear Drill, and Typing Dictation. Adjust playback speeds from 0.75x to 1.5x to progressively sharpen your acoustic acuity.";
        en["Help_Tip_ListeningLab_1"] = "Listen closely for vowel length contrasts (e.g., /ɪ/ in 'ship' vs /iː/ in 'sheep').";
        en["Help_Tip_ListeningLab_2"] = "Practice Dictation Mode to simultaneously reinforce ear comprehension and spelling precision.";
        en["Help_Tip_ListeningLab_3"] = "Toggle quickly between British (UK) and American (US) audio accents using Alt+U and Alt+A.";
        // Guide 5: Test Mode & CAT
        en["Help_Guide_Test_Title"] = "Practice & Adaptive Computerized Testing (CAT)";
        en["Help_Guide_Test_Subtitle"] = "Measure true proficiency with dynamic difficulty adjustment powered by Item Response Theory.";
        en["Help_Guide_Test_Summary"] = "Rather than static random exams, Lexora dynamically calibrates question difficulty to pinpoint your exact CEFR mastery level.";
        en["Help_Guide_Test_Content"] = "Lexora offers 3 evaluation formats: Quick 10-20 Card Quiz, Topic-Based Review Test, and Computer Adaptive Testing (CAT). In CAT mode, answering a B1 question correctly automatically promotes subsequent questions to B2/C1. Incorrect responses step down to A2 to verify foundational mastery. Complete reports break down performance by CEFR, exam context, and recommended review lists.";
        en["Help_Tip_Test_1"] = "Take a standardized adaptive test every two weeks to measure longitudinal vocabulary growth.";
        en["Help_Tip_Test_2"] = "Carefully inspect incorrect answers post-exam to automatically queue those words for targeted spaced repetition.";
        en["Help_Tip_Test_3"] = "Use numeric keys 1, 2, 3, 4 to swiftly select multiple-choice options A, B, C, D without touching the mouse.";
        // Guide 6: Backup & Settings
        en["Help_Guide_Settings_Title"] = "SQLite Backup, Restore & Personalization";
        en["Help_Guide_Settings_Subtitle"] = "Protect learning assets with atomic snapshot backups, WAL flushing, and SQLite header validation.";
        en["Help_Guide_Settings_Summary"] = "All your study progress is stored safely in a local ACID SQLite database with zero dependency on third-party cloud services.";
        en["Help_Guide_Settings_Content"] = "In Settings, you can create immediate .db snapshots, configure automatic daily/weekly backup schedules, and restore previous archives with rigorous safety invariants (SQLite header validation, connection pool flushing, and automatic rollback on failure). Customize your workspace with Light/Dark/System themes, toggle between SM-2 and FSRS algorithms, and set default audio speeds.";
        en["Help_Tip_Settings_1"] = "Export backup snapshots to external storage (USB drive or cloud drive) monthly for peace of mind.";
        en["Help_Tip_Settings_2"] = "Lexora automatically retains the 10 most recent automated snapshots in OS-protected app data.";
        en["Help_Tip_Settings_3"] = "Press Ctrl+T at any time to toggle between Light and Dark visual themes.";
    }
}
