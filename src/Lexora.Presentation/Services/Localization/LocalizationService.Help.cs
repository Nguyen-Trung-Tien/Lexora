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
        vi["Help_Subtitle"] = "Cẩm nang hướng dẫn toàn diện: Lộ trình học thích ứng, Thuật toán lặp lại ngắt quãng (SM-2 & FSRS), Quản lý từ vựng, Luyện nghe & Đồ thị tri thức.";
        vi["Help_HeroBadge"] = "TRUNG TÂM HƯỚNG DẪN & TRỢ GIÚP";
        vi["Help_OnboardingSystem"] = "Hệ thống hướng dẫn toàn diện";
        vi["Help_OfflineBadge"] = "Hoạt động Ngoại tuyến & SM-2/FSRS";
        vi["Help_SearchPlaceholder"] = "Tìm kiếm hướng dẫn (lộ trình, SM-2, import CSV, phát âm, CAT, đồ thị, sao lưu)...";
        vi["Help_ResetFilter"] = "Đặt lại";
        vi["Help_ResetFilterTooltip"] = "Xóa bộ lọc danh mục và từ khóa tìm kiếm";
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
        vi["Help_Cat_Roadmap"] = "Lộ trình học (Roadmap)";
        vi["Help_Cat_KnowledgeGraph"] = "Đồ thị từ & Lưu trữ";
        vi["Help_Cat_ListeningLab"] = "Phát âm & Luyện nghe";
        vi["Help_Cat_TestMode"] = "Luyện tập & Thi thử (CAT)";
        vi["Help_Cat_BackupSettings"] = "Sao lưu & Cài đặt";

        // Badges
        vi["Help_Badge_Core"] = "CỐT LÕI";
        vi["Help_Badge_Essential"] = "CƠ BẢN";
        vi["Help_Badge_Algorithm"] = "THUẬT TOÁN";
        vi["Help_Badge_Roadmap"] = "LỘ TRÌNH";
        vi["Help_Badge_Graph"] = "ĐỒ THỊ";
        vi["Help_Badge_Grammar"] = "NGỮ PHÁP";
        vi["Help_Badge_Storage"] = "LƯU TRỮ";
        vi["Help_Badge_Pronunciation"] = "LUYỆN ÂM";
        vi["Help_Badge_ExamPrep"] = "LUYỆN THI";
        vi["Help_Badge_Safety"] = "AN TOÀN";

        // Action Buttons
        vi["Help_Action_GoDashboard"] = "Mở Tổng quan";
        vi["Help_Action_GoVocabulary"] = "Mở Kho từ vựng";
        vi["Help_Action_GoReview"] = "Bắt đầu Ôn tập";
        vi["Help_Action_GoRoadmap"] = "Mở Lộ trình học";
        vi["Help_Action_GoKnowledgeGraph"] = "Mở Đồ thị từ";
        vi["Help_Action_GoGrammar"] = "Mở Ngữ pháp";
        vi["Help_Action_GoStorage"] = "Mở Kho lưu trữ";
        vi["Help_Action_GoHomophones"] = "Mở Từ đồng âm";
        vi["Help_Action_GoTest"] = "Làm bài kiểm tra";
        vi["Help_Action_GoSettings"] = "Mở Cài đặt";

        // Localized Shortcut Descriptions
        vi["Help_Shortcut_Ctrl1"] = "Ctrl+1: Bảng tổng quan (Dashboard)";
        vi["Help_Shortcut_Ctrl2"] = "Ctrl+2: Kho từ vựng (Vocabulary)";
        vi["Help_Shortcut_Ctrl3"] = "Ctrl+3: Từ đồng âm & Listening Lab";
        vi["Help_Shortcut_Ctrl4"] = "Ctrl+4: Đồ thị tri thức (Knowledge Graph)";
        vi["Help_Shortcut_Ctrl5"] = "Ctrl+5: Ngữ pháp & Cấu trúc câu";
        vi["Help_Shortcut_Ctrl6"] = "Ctrl+6: Ôn tập Spaced Repetition";
        vi["Help_Shortcut_Ctrl7"] = "Ctrl+7: Phòng thi thử & CAT Hub";
        vi["Help_Shortcut_Ctrl8"] = "Ctrl+8: Kho lưu trữ ghi nhớ (Storage)";
        vi["Help_Shortcut_Ctrl9"] = "Ctrl+9: Lộ trình học thích ứng (Roadmap)";
        vi["Help_Shortcut_CtrlK"] = "Ctrl+K: Tìm kiếm thông minh toàn cục";
        vi["Help_Shortcut_CtrlN"] = "Ctrl+N: Thêm từ vựng mới";
        vi["Help_Shortcut_F1"] = "F1: Mở Trung tâm Hướng dẫn";
        vi["Help_Shortcut_SpaceFlip"] = "Phím Cách (Space): Lật thẻ ghi nhớ";
        vi["Help_Shortcut_RatingAgain"] = "Phím 1: Quên (Again - Chu kỳ 1 ngày)";
        vi["Help_Shortcut_RatingHard"] = "Phím 2: Khó (Hard - Nhớ chậm)";
        vi["Help_Shortcut_RatingGood"] = "Phím 3: Chuẩn (Good - Nhớ tốt)";
        vi["Help_Shortcut_RatingEasy"] = "Phím 4: Dễ (Easy - Thuộc lòng)";
        vi["Help_Shortcut_CtrlZ"] = "Ctrl+Z: Hoàn tác đánh giá thẻ vừa rồi";
        vi["Help_Shortcut_AltU"] = "Alt+U: Phát âm giọng Anh - Anh (UK Voice)";
        vi["Help_Shortcut_AltA"] = "Alt+A: Phát âm giọng Anh - Mỹ (US Voice)";
        vi["Help_Shortcut_TestKeys"] = "Phím 1-4: Chọn phương án A, B, C, D";
        vi["Help_Shortcut_TestEnter"] = "Phím Enter: Xác nhận nộp bài";
        vi["Help_Shortcut_CtrlT"] = "Ctrl+T: Chuyển đổi giao diện Sáng / Tối";
        vi["Help_Shortcut_RoadmapInspect"] = "Click Node: Xem bài học & Bắt đầu thử thách";
        vi["Help_Shortcut_GraphInspect"] = "Click Node: Tra cứu ngữ nghĩa & Khám phá cụm từ";
        vi["Help_Shortcut_GraphPivot"] = "Nút Pivot: Đặt từ làm tâm điểm mạng lưới";

        // Guide 1: Getting Started & Dashboard
        vi["Help_Guide_GettingStarted_Title"] = "Tổng quan Bảng điều khiển & Chỉ số học tập";
        vi["Help_Guide_GettingStarted_Subtitle"] = "Làm chủ màn hình tổng quan, theo dõi chuỗi học tập (Streak) và mức độ ghi nhớ dài hạn.";
        vi["Help_Guide_GettingStarted_Summary"] = "Dashboard là trạm chỉ huy trung tâm cung cấp góc nhìn toàn diện về tiến độ học tập, biểu đồ nhiệt tính kỷ luật và dự báo thẻ đến hạn theo đường cong lãng quên Ebbinghaus.";
        vi["Help_Guide_GettingStarted_Content"] = "Mỗi khi khởi động Lexora, hệ thống tự động quét và tính toán số lượng thẻ đến hạn ôn tập trong ngày theo đường cong lãng quên Ebbinghaus. Thẻ Hero Banner ở đầu trang cho phép bạn kích hoạt phiên ôn tập chỉ với 1 cú click. Biểu đồ Heatmap trực quan hóa tính kỷ luật học tập qua từng ngày trong năm. Biểu đồ Tỷ lệ duy trì (Retention Curve) và Sức khỏe trí nhớ (Memory Health) phân loại vốn từ theo khung tham chiếu châu Âu CEFR (A1 đến C2) cùng các chứng chỉ mục tiêu (TOEIC, IELTS, VSTEP).";
        vi["Help_Tip_GettingStarted_1"] = "Duy trì chuỗi học (Streak) đều đặn mỗi ngày (dù chỉ 5-10 phút) củng cố liên kết nơ-ron não bộ hiệu quả hơn gấp nhiều lần so với học dồn dập vào cuối tuần.";
        vi["Help_Tip_GettingStarted_2"] = "Theo dõi sát sao mục Thẻ đến hạn (Due Cards) để ôn luyện đúng 'thời điểm vàng' trước khi thông tin phai nhạt khỏi trí nhớ ngắn hạn.";
        vi["Help_Tip_GettingStarted_3"] = "Sử dụng tổ hợp phím Ctrl+K để kích hoạt hộp tìm kiếm thông minh, tra cứu nhanh bất kỳ từ vựng, ngữ pháp hay định nghĩa nào trong tích tắc.";

        // Guide 2: Vocabulary & Topic Units
        vi["Help_Guide_Vocab_Title"] = "Quản lý Kho từ vựng, Chủ đề & Dữ liệu";
        vi["Help_Guide_Vocab_Subtitle"] = "Thêm từ mới, phân loại theo đơn vị chủ đề (Topic Units), nhập/xuất CSV/JSON và làm giàu ngữ cảnh câu ví dụ.";
        vi["Help_Guide_Vocab_Summary"] = "Hệ thống từ vựng Lexora được xây dựng theo chuẩn mô hình miền: Từ vựng -> Đa lớp nghĩa -> Câu ví dụ ngữ cảnh -> Đơn vị chủ đề -> Phiên âm âm thanh chuẩn IPA.";
        vi["Help_Guide_Vocab_Content"] = "Bạn có thể quản lý kho từ vựng linh hoạt qua 2 chế độ: Bảng dữ liệu chi tiết hoặc Nhóm chủ đề (Topic Units). Khi thêm từ mới, hệ thống tự động chuẩn hóa phiên âm IPA Anh - Anh / Anh - Mỹ, gán cấp độ CEFR và ngữ cảnh đề thi. Trình nhập/xuất thông minh hỗ trợ chuyển đổi dữ liệu từ Anki, Excel, Quizlet với cơ chế tự động phát hiện tiêu đề cột và loại trừ từ trùng lặp an toàn.";
        vi["Help_Tip_Vocab_1"] = "Luôn đính kèm câu ví dụ thực tế cho mỗi từ vựng để não bộ tiếp thu ngữ cảnh tự nhiên thay vì học vẹt nghĩa từ đơn lẻ.";
        vi["Help_Tip_Vocab_2"] = "Kích hoạt chế độ Chọn hàng loạt (Batch Selection) để gán chủ đề, xuất file hoặc thay đổi trạng thái của nhiều từ cùng lúc.";
        vi["Help_Tip_Vocab_3"] = "Định dạng file CSV import chuẩn: term,ipa,meaning,pos,example,cefr,exam (hệ thống tự động phát hiện header không phân biệt hoa thường).";
        vi["Help_Tip_Vocab_4"] = "Tận dụng Kho lưu trữ ghi nhớ (Learning Storage) để lưu lại các câu trích dẫn đặc sắc và thành ngữ từ sách báo tiếng Anh.";

        // Guide 3: Spaced Repetition (SM-2 & FSRS Deep-Dive)
        vi["Help_Guide_SpacedRepetition_Title"] = "Thuật toán Lặp lại ngắt quãng (SM-2 & FSRS Deep-Dive)";
        vi["Help_Guide_SpacedRepetition_Subtitle"] = "Thấu hiểu bản chất 4 mức đánh giá (Again, Hard, Good, Easy) và công thức tính chu kỳ tối ưu hóa trí nhớ.";
        vi["Help_Guide_SpacedRepetition_Summary"] = "Thuật toán dự đoán chính xác thời điểm trí nhớ bắt đầu suy giảm để lên lịch ôn tập, giúp ghi nhớ vĩnh viễn với số lần lặp lại tối thiểu.";
        vi["Help_Guide_SpacedRepetition_Formula"] = "SM-2: EF' = EF + (0.1 - (4 - q) * (0.08 + (4 - q) * 0.02)), chặn dưới EF >= 1.3\nChu kỳ: I(1) = 1 ngày, I(2) = 6 ngày, I(n) = ceil(I(n-1) * EF). (q: Điểm đánh giá từ 0 đến 4)\nFSRS: S(t) = S * (1 + Factor * Retrievability) dựa trên độ bền vững (Stability) và khả năng truy hồi.";
        vi["Help_Guide_SpacedRepetition_Content"] = "Trong giao diện Ôn tập (Review Page), bạn sẽ tương tác với mặt trước thẻ (từ vựng, phiên âm IPA, nút phát âm). Sau khi tự truy hồi nghĩa trong tâm trí, nhấn Phím Cách (Space) để lật thẻ và chọn 1 trong 4 mức đánh giá:\n• Again (Phím 1): Quên hoàn toàn -> Chu kỳ được đưa về 1 ngày để củng cố ngay.\n• Hard (Phím 2): Nhớ ra nhưng mất nhiều thời gian do dự (>5 giây) -> Hệ số EF giảm nhẹ, chu kỳ tăng chậm.\n• Good (Phím 3): Nhớ chính xác với nỗ lực tiêu chuẩn -> Chu kỳ nhân theo hệ số Ease Factor hiện tại.\n• Easy (Phím 4): Nhớ tức thì, không tốn chút nỗ lực nào -> Hệ số EF tăng, chu kỳ nhận thêm hệ số thưởng.";
        vi["Help_Tip_SpacedRepetition_Again"] = "Again (0-1): Đừng ngần ngại bấm Again nếu bạn quên hoặc dịch sai. Thành thật với thuật toán là bí quyết quan trọng nhất để củng cố trí nhớ bền vững.";
        vi["Help_Tip_SpacedRepetition_Hard"] = "Hard (2): Dùng khi bạn mất hơn 5 giây suy nghĩ chật vật hoặc còn nhầm lẫn một phần ngữ nghĩa nhỏ.";
        vi["Help_Tip_SpacedRepetition_Good"] = "Good (3): Lựa chọn tiêu chuẩn lý tưởng cho hầu hết các lần nhớ đúng bình thường.";
        vi["Help_Tip_SpacedRepetition_Easy"] = "Easy (4): Chỉ dành riêng cho những từ bạn đã nắm chắc như lòng bàn tay và phản xạ tức thì.";

        // Guide 4: Learning Roadmap & Goal Tracking
        vi["Help_Guide_Roadmap_Title"] = "Lộ trình học thích ứng & Mục tiêu thi cử (Roadmap Hub)";
        vi["Help_Guide_Roadmap_Subtitle"] = "Thiết lập điểm số mục tiêu (TOEIC, IELTS, VSTEP), chinh phục các mốc milestone và mở khóa cây kỹ năng.";
        vi["Help_Guide_Roadmap_Summary"] = "Lộ trình học mô hình hóa đường đi thích ứng dạng serpentine, tự động cân chỉnh hệ số SM-2 Ease Factor ban đầu theo khoảng cách điểm mục tiêu và mở khóa nút tiến trình liên tục.";
        vi["Help_Guide_Roadmap_Formula"] = "Khoảng cách điểm số: Gap = TargetScore - CurrentScore\nHiệu chỉnh EF ban đầu: EF = Clamp(2.5 - (Gap / MaxGap) * 0.5, 2.0, 2.5)\nMở khóa Node: q >= 3 hoặc 100% từ vựng của node đạt trạng thái Mastered.";
        vi["Help_Guide_Roadmap_Content"] = "Lexora cho phép bạn lựa chọn kỳ thi mục tiêu (TOEIC 450-990, IELTS 5.0-8.5, VSTEP B1-C1). Hệ thống RoadmapEngine tự động tính toán khoảng cách điểm số (Score Gap) để điều chỉnh độ khó và hệ số ghi nhớ ban đầu (EF từ 2.0 đến 2.5). Đồ thị đường cong uốn lượn (Bézier Spline) hiển thị các mốc Milestone từ căn bản đến nâng cao. Mỗi Node thể hiện một cụm kỹ năng hoặc chủ đề từ vựng: khi bạn đạt điểm đánh giá q >= 3 hoặc học đủ số từ yêu cầu, node tiếp theo sẽ được tự động mở khóa (Unlocked) kèm hiệu ứng chuyển động mượt mà.";
        vi["Help_Tip_Roadmap_1"] = "Nhấp chuột vào bất kỳ node nào trên bản đồ lộ trình để xem chi tiết số từ mục tiêu, độ khó và tiến độ hoàn thành.";
        vi["Help_Tip_Roadmap_2"] = "Nút màu xanh lục biểu thị Node đã làm chủ hoàn toàn (Mastered), màu xanh dương là Node đang học (In Progress), và màu xám là Node đang khóa (Locked).";
        vi["Help_Tip_Roadmap_3"] = "Hoàn thành lộ trình TOEIC hoặc IELTS giúp bạn bao phủ toàn diện 100% vốn từ thường gặp trong các đề thi thật gần đây.";

        // Guide 5: Knowledge Graph & Semantic Associations
        vi["Help_Guide_KnowledgeGraph_Title"] = "Đồ thị tri thức (Knowledge Graph) & Cụm ngữ nghĩa";
        vi["Help_Guide_KnowledgeGraph_Subtitle"] = "Khám phá mạng lưới liên tưởng từ vựng, từ đồng nghĩa, trái nghĩa và mở rộng vốn từ trực quan.";
        vi["Help_Guide_KnowledgeGraph_Summary"] = "Không còn học từ đơn lẻ rời rạc. Đồ thị tri thức kết nối các từ vựng theo mối quan hệ ngữ nghĩa, giúp tăng khả năng ghi nhớ liên tưởng lên gấp 3 lần.";
        vi["Help_Guide_KnowledgeGraph_Content"] = "Trang Đồ thị tri thức (Knowledge Graph) mô phỏng cấu trúc nơ-ron liên tưởng của bộ não. Mỗi từ vựng là một nút (Node) kết nối với các từ đồng nghĩa (Synonyms), từ trái nghĩa (Antonyms), cùng họ từ (Derivatives) hoặc cùng cụm chủ đề. Bạn có thể kéo thả, phóng to thu nhỏ canvas mượt mà, nhấp chọn từ bất kỳ để nghe phát âm, xem ví dụ và nhấn nút 'Pivot' để dịch chuyển toàn bộ tâm điểm đồ thị sang từ đó.";
        vi["Help_Tip_KnowledgeGraph_1"] = "Nhấn nút 'Pivot' trên thẻ chi tiết để khám phá mạng lưới từ vựng mở rộng xoay quanh một từ mới.";
        vi["Help_Tip_KnowledgeGraph_2"] = "Lọc đồ thị theo cấp độ CEFR (B1, B2, C1) để tập trung mở rộng vốn từ học thuật đúng tầm với mục tiêu.";
        vi["Help_Tip_KnowledgeGraph_3"] = "Học từ theo cặp đồng nghĩa / trái nghĩa giúp bạn phản xạ nhanh hơn khi làm bài thi Viết (Writing) và Đọc (Reading).";

        // Guide 6: Grammar & Pattern Suggester
        vi["Help_Guide_Grammar_Title"] = "Cẩm nang Ngữ pháp & Cấu trúc câu (Grammar Rules)";
        vi["Help_Guide_Grammar_Subtitle"] = "Làm chủ công thức ngữ pháp, điểm dễ nhầm lẫn và ứng dụng câu vào ngữ cảnh thực tế.";
        vi["Help_Guide_Grammar_Summary"] = "Tra cứu và thực hành hơn 50+ cấu trúc ngữ pháp then chốt được phân loại chặt chẽ theo khung CEFR và bài thi TOEIC/IELTS.";
        vi["Help_Guide_Grammar_Content"] = "Hệ thống Ngữ pháp Lexora cung cấp đầy đủ: Công thức cấu trúc, Giải thích bản chất, Lưu ý lỗi thường gặp và các câu ví dụ minh họa song ngữ. Chế độ Luyện tập ngữ pháp (Practice Mode) giúp bạn kiểm tra khả năng vận dụng quy tắc ngay lập tức qua các bài trắc nghiệm thông minh và chấm điểm tức thì.";
        vi["Help_Tip_Grammar_1"] = "Đọc kỹ phần 'Lỗi thường gặp' của mỗi điểm ngữ pháp để tránh các bẫy phổ biến trong bài thi TOEIC Part 5 & 6.";
        vi["Help_Tip_Grammar_2"] = "Sử dụng tính năng 'Từ vựng liên kết' để nắm bắt những động từ, tính từ thường đi kèm với cấu trúc đó.";
        vi["Help_Tip_Grammar_3"] = "Nhấn 'Luyện tập' sau mỗi lần học quy tắc mới để củng cố ngay vào trí nhớ dài hạn.";

        // Guide 7: Learning Storage & Personal Notebooks
        vi["Help_Guide_Storage_Title"] = "Kho lưu trữ ghi nhớ & Sổ tay ngữ cảnh (Learning Storage)";
        vi["Help_Guide_Storage_Subtitle"] = "Lưu ngữ cảnh, câu trích dẫn đặc sắc, gắn nhãn danh mục và tạo đề ôn tập nhanh.";
        vi["Help_Guide_Storage_Summary"] = "Kho lưu trữ cá nhân giúp bạn thu thập các mẫu câu, cụm từ đắt giá từ sách báo, phim ảnh và tài liệu học tiếng Anh hàng ngày.";
        vi["Help_Guide_Storage_Content"] = "Trong quá trình học và đọc tài liệu tiếng Anh, bạn có thể lưu lại các câu ví dụ truyền cảm hứng, cấu trúc collocations hay vào Learning Storage. Hệ thống hỗ trợ gắn thẻ chủ đề, đánh dấu yêu thích và sinh bài tập trắc nghiệm tự động từ chính các câu bạn đã lưu để biến chúng thành vốn ngôn ngữ chủ động.";
        vi["Help_Tip_Storage_1"] = "Gán nhãn hashtag (#IELTS_Writing, #Collocation) cho các câu trích đoạn để dễ dàng lọc lại trước kỳ thi.";
        vi["Help_Tip_Storage_2"] = "Dùng nút 'Sinh bài tập' để biến các đoạn văn trích dẫn thành bài tập điền từ thú vị.";
        vi["Help_Tip_Storage_3"] = "Bấm biểu tượng loa để nghe toàn bộ câu ví dụ với giọng đọc bản ngữ chuẩn xác.";

        // Guide 8: Pronunciation, Homophones & Listening Lab
        vi["Help_Guide_ListeningLab_Title"] = "Luyện phát âm, Từ đồng âm & Phòng Lab Luyện nghe";
        vi["Help_Guide_ListeningLab_Subtitle"] = "Phân biệt các cặp âm tối thiểu (minimal pairs), làm chủ giọng bản xứ và tránh bẫy nghe trong đề thi.";
        vi["Help_Guide_ListeningLab_Summary"] = "Khắc phục triệt để điểm yếu phát âm và phản xạ nghe qua phòng lab đối sánh âm thanh UK - US và bài tập phân biệt từ đồng âm.";
        vi["Help_Guide_ListeningLab_Content"] = "Tính năng Homophones & Sound-Alikes phân loại các nhóm từ phát âm giống hệt hoặc tương tự nhau (như 'affect' vs 'effect', 'accept' vs 'except', 'their' vs 'there'). Phòng luyện nghe Listening Lab hỗ trợ 3 chế độ chuyên sâu: Đối sánh âm sắc UK/US, Trắc nghiệm phân biệt cặp âm trong 3 giây, và Nghe chép chính tả (Dictation Drill). Bạn có thể điều chỉnh tốc độ từ 0.75x đến 1.5x để rèn luyện thính giác theo từng giai đoạn.";
        vi["Help_Tip_ListeningLab_1"] = "Lắng nghe kỹ sự khác biệt giữa các nguyên âm ngắn và nguyên âm dài (ví dụ: /ɪ/ trong 'ship' vs /iː/ trong 'sheep').";
        vi["Help_Tip_ListeningLab_2"] = "Luyện tập chế độ Nghe chép chính tả (Dictation) để rèn luyện đồng thời cả phản xạ nghe phân tích lẫn độ chính xác chính tả.";
        vi["Help_Tip_ListeningLab_3"] = "Chuyển đổi linh hoạt giữa giọng Anh - Anh và Anh - Mỹ bằng phím tắt Alt+U và Alt+A.";

        // Guide 9: Practice & Adaptive Computerized Testing (CAT Engine)
        vi["Help_Guide_Test_Title"] = "Luyện tập & Bài thi thích ứng máy tính (CAT Engine)";
        vi["Help_Guide_Test_Subtitle"] = "Đo lường năng lực thực tế với thuật toán tự động cân chỉnh độ khó câu hỏi theo Lý thuyết Ứng đáp Câu hỏi (IRT).";
        vi["Help_Guide_Test_Summary"] = "Hệ thống kiểm tra thông minh không tạo đề ngẫu nhiên nhàm chán mà liên tục điều chỉnh độ khó để xác định chuẩn xác trình độ CEFR thực tế của bạn.";
        vi["Help_Guide_Test_Content"] = "Lexora cung cấp 3 hình thức kiểm tra: Ôn tập nhanh 10-20 câu, Bài kiểm tra theo Chủ đề đã học, và Bài thi chuẩn hóa thích ứng (CAT - Computer Adaptive Testing). Trong bài thi CAT, nếu bạn trả lời đúng câu hỏi ở mức B1, hệ thống sẽ tự động nâng độ khó lên câu hỏi B2/C1. Nếu bạn trả lời sai, hệ thống sẽ lùi về mức A2 để kiểm chứng độ vững vàng của nền tảng. Sau khi hoàn thành, bạn nhận được báo cáo phân tích chi tiết từng kỹ năng và đề xuất học bổ sung.";
        vi["Help_Tip_Test_1"] = "Thi thử định kỳ 2 tuần một lần để đánh giá sự tiến bộ của vốn từ vựng trong dài hạn.";
        vi["Help_Tip_Test_2"] = "Xem lại kỹ danh sách 'Câu trả lời sai' sau bài thi để tự động đưa các từ đó vào danh sách ưu tiên ôn tập lặp lại ngắt quãng.";
        vi["Help_Tip_Test_3"] = "Sử dụng các phím số 1, 2, 3, 4 trên bàn phím để chọn nhanh đáp án A, B, C, D mà không cần rê chuột.";

        // Guide 10: SQLite Database Backup, Restore & Personalization
        vi["Help_Guide_Settings_Title"] = "Sao lưu CSDL SQLite, Khôi phục an toàn & Tùy biến";
        vi["Help_Guide_Settings_Subtitle"] = "Bảo vệ toàn vẹn dữ liệu học tập với cơ chế sao lưu tức thì, xả WAL và kiểm tra SQLite Header.";
        vi["Help_Guide_Settings_Summary"] = "Toàn bộ thành quả học tập của bạn được lưu trữ an toàn trong cơ sở dữ liệu SQLite cục bộ chuẩn ACID, hoàn toàn độc lập với dịch vụ đám mây bên thứ ba.";
        vi["Help_Guide_Settings_Content"] = "Tại trang Cài đặt (Settings), bạn có thể: Tạo bản sao lưu tức thì (.db), Lên lịch sao lưu tự động hàng ngày/tuần, Phục hồi từ bản sao lưu cũ với cơ chế xác thực an toàn tuyệt đối (kiểm tra SQLite Header, xóa connection pool và rollback dự phòng nếu lỗi). Ngoài ra, bạn có thể tự do chuyển đổi giữa giao diện Sáng / Tối / Hệ thống, lựa chọn thuật toán SM-2 hoặc FSRS và tinh chỉnh tốc độ phát âm thanh mặc định.";
        vi["Help_Tip_Settings_1"] = "Nên xuất bản sao lưu ra thư mục lưu trữ ngoài (USB hoặc Google Drive/OneDrive) định kỳ mỗi tháng để an tâm tuyệt đối.";
        vi["Help_Tip_Settings_2"] = "Lexora tự động lưu giữ tối đa 10 bản sao lưu gần nhất trong thư mục an toàn của hệ điều hành.";
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
        en["Help_Subtitle"] = "Comprehensive learning documentation: Adaptive Roadmaps, Spaced Repetition (SM-2 & FSRS), Vocabulary Management, Listening Lab, and Knowledge Graph.";
        en["Help_HeroBadge"] = "HELP & USER GUIDE HUB";
        en["Help_OnboardingSystem"] = "Comprehensive Onboarding System";
        en["Help_OfflineBadge"] = "Offline-First & SM-2/FSRS";
        en["Help_SearchPlaceholder"] = "Search guides (roadmap, SM-2, CSV import, pronunciation, CAT, knowledge graph, backup)...";
        en["Help_ResetFilter"] = "Reset";
        en["Help_ResetFilterTooltip"] = "Clear category filters and search query";
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
        en["Help_Cat_Roadmap"] = "Adaptive Roadmap & Goals";
        en["Help_Cat_KnowledgeGraph"] = "Knowledge Graph & Storage";
        en["Help_Cat_ListeningLab"] = "Pronunciation & Listening Lab";
        en["Help_Cat_TestMode"] = "Practice & CAT Exam";
        en["Help_Cat_BackupSettings"] = "Backup & Settings";

        // Badges
        en["Help_Badge_Core"] = "CORE";
        en["Help_Badge_Essential"] = "ESSENTIAL";
        en["Help_Badge_Algorithm"] = "ALGORITHM";
        en["Help_Badge_Roadmap"] = "ROADMAP";
        en["Help_Badge_Graph"] = "KNOWLEDGE GRAPH";
        en["Help_Badge_Grammar"] = "GRAMMAR";
        en["Help_Badge_Storage"] = "NOTEBOOKS";
        en["Help_Badge_Pronunciation"] = "PRONUNCIATION";
        en["Help_Badge_ExamPrep"] = "EXAM PREP";
        en["Help_Badge_Safety"] = "DATA SAFETY";

        // Action Buttons
        en["Help_Action_GoDashboard"] = "Open Dashboard";
        en["Help_Action_GoVocabulary"] = "Open Vocabulary";
        en["Help_Action_GoReview"] = "Start Review";
        en["Help_Action_GoRoadmap"] = "Open Roadmap";
        en["Help_Action_GoKnowledgeGraph"] = "Open Knowledge Graph";
        en["Help_Action_GoGrammar"] = "Open Grammar";
        en["Help_Action_GoStorage"] = "Open Storage";
        en["Help_Action_GoHomophones"] = "Open Homophones";
        en["Help_Action_GoTest"] = "Start Test";
        en["Help_Action_GoSettings"] = "Open Settings";

        // Localized Shortcut Descriptions
        en["Help_Shortcut_Ctrl1"] = "Ctrl+1: Dashboard Mission Control";
        en["Help_Shortcut_Ctrl2"] = "Ctrl+2: Vocabulary Hub";
        en["Help_Shortcut_Ctrl3"] = "Ctrl+3: Homophones & Listening Lab";
        en["Help_Shortcut_Ctrl4"] = "Ctrl+4: Knowledge Graph Network";
        en["Help_Shortcut_Ctrl5"] = "Ctrl+5: Grammar Patterns & Rules";
        en["Help_Shortcut_Ctrl6"] = "Ctrl+6: Spaced Repetition Review";
        en["Help_Shortcut_Ctrl7"] = "Ctrl+7: Test & CAT Exam Hub";
        en["Help_Shortcut_Ctrl8"] = "Ctrl+8: Learning Storage & Notebooks";
        en["Help_Shortcut_Ctrl9"] = "Ctrl+9: Adaptive Learning Roadmap";
        en["Help_Shortcut_CtrlK"] = "Ctrl+K: Smart Global Search";
        en["Help_Shortcut_CtrlN"] = "Ctrl+N: Add New Word";
        en["Help_Shortcut_F1"] = "F1: Open Help & Guide Hub";
        en["Help_Shortcut_SpaceFlip"] = "Space: Flip Flashcard";
        en["Help_Shortcut_RatingAgain"] = "Key 1: Again (Lapse - 1 day interval)";
        en["Help_Shortcut_RatingHard"] = "Key 2: Hard (Hesitant recall)";
        en["Help_Shortcut_RatingGood"] = "Key 3: Good (Standard recall)";
        en["Help_Shortcut_RatingEasy"] = "Key 4: Easy (Instant effortless recall)";
        en["Help_Shortcut_CtrlZ"] = "Ctrl+Z: Undo Last Card Rating";
        en["Help_Shortcut_AltU"] = "Alt+U: British Voice (UK Accent)";
        en["Help_Shortcut_AltA"] = "Alt+A: American Voice (US Accent)";
        en["Help_Shortcut_TestKeys"] = "Keys 1-4: Select Option A, B, C, D";
        en["Help_Shortcut_TestEnter"] = "Enter: Confirm & Submit Response";
        en["Help_Shortcut_CtrlT"] = "Ctrl+T: Toggle Light / Dark Theme";
        en["Help_Shortcut_RoadmapInspect"] = "Click Node: Inspect Lesson & Launch Challenge";
        en["Help_Shortcut_GraphInspect"] = "Click Node: Inspect Semantics & Explore Cluster";
        en["Help_Shortcut_GraphPivot"] = "Pivot Button: Center Network on Concept";

        // Guide 1: Getting Started & Dashboard
        en["Help_Guide_GettingStarted_Title"] = "Dashboard Overview & Learning Intelligence";
        en["Help_Guide_GettingStarted_Subtitle"] = "Master the central dashboard, monitor learning streaks, and evaluate long-term memory retention.";
        en["Help_Guide_GettingStarted_Summary"] = "The Dashboard is your central mission control, offering a 360-degree perspective on your learning journey, streak consistency, and review forecasts governed by the Ebbinghaus forgetting curve.";
        en["Help_Guide_GettingStarted_Content"] = "Whenever you launch Lexora, the system automatically scans and calculates cards due for review based on the Ebbinghaus forgetting curve. The top Hero Banner lets you jump into your daily review session with a single click. The activity Heatmap visualizes study discipline across every day of the year, while the Retention Curve and Memory Health charts classify your vocabulary across CEFR levels (A1 to C2) and target exams (TOEIC, IELTS, VSTEP).";
        en["Help_Tip_GettingStarted_1"] = "Maintaining a daily study streak (even for 5-10 minutes) reinforces neural memory pathways far more effectively than weekend cramming.";
        en["Help_Tip_GettingStarted_2"] = "Keep a close eye on Due Cards to review at the optimal retention window before information slips from short-term memory.";
        en["Help_Tip_GettingStarted_3"] = "Press Ctrl+K to trigger smart global search and look up any vocabulary term, grammar rule, or definition instantly.";

        // Guide 2: Vocabulary & Topic Units
        en["Help_Guide_Vocab_Title"] = "Vocabulary, Topic Units & Data Management";
        en["Help_Guide_Vocab_Subtitle"] = "Add words, organize into topic units, import/export CSV/JSON, and master rich contextual sentences.";
        en["Help_Guide_Vocab_Summary"] = "Lexora's vocabulary architecture is strictly domain-driven: Term -> Multiple Meanings -> Contextual Example Sentences -> Topic Units -> Standard IPA Audio.";
        en["Help_Guide_Vocab_Content"] = "Manage your vocabulary seamlessly across two viewing modes: Detailed Data Table or Grouped Topic Units. When adding words, the editor provides standardized UK/US IPA phonetics, CEFR levels, and exam tags. The intelligent Import/Export parser effortlessly ingests data from Anki, Excel, or Quizlet with automatic column header detection and safe deduplication.";
        en["Help_Tip_Vocab_1"] = "Always attach authentic example sentences so your brain encodes natural collocations rather than memorizing isolated words.";
        en["Help_Tip_Vocab_2"] = "Use Batch Selection mode to reassign topics, export data, or update the learning status of multiple words simultaneously.";
        en["Help_Tip_Vocab_3"] = "Standard CSV import schema: term,ipa,meaning,pos,example,cefr,exam (the parser automatically detects headers case-insensitively).";
        en["Help_Tip_Vocab_4"] = "Leverage Learning Storage to collect inspiring excerpts, idioms, and collocations encountered in English reading materials.";

        // Guide 3: Spaced Repetition (SM-2 & FSRS Deep-Dive)
        en["Help_Guide_SpacedRepetition_Title"] = "Spaced Repetition Deep-Dive (SM-2 & FSRS Engines)";
        en["Help_Guide_SpacedRepetition_Subtitle"] = "Understand the core 4 rating tiers (Again, Hard, Good, Easy) and algorithmic interval calculation mechanics.";
        en["Help_Guide_SpacedRepetition_Summary"] = "The algorithm calculates the exact point of memory decay to schedule reviews, cementing permanent retention with minimal repetitions.";
        en["Help_Guide_SpacedRepetition_Formula"] = "SM-2: EF' = EF + (0.1 - (4 - q) * (0.08 + (4 - q) * 0.02)), bounded at EF >= 1.3\nIntervals: I(1) = 1 day, I(2) = 6 days, I(n) = ceil(I(n-1) * EF). (q: Quality rating 0 to 4)\nFSRS: S(t) = S * (1 + Factor * Retrievability) based on memory stability and retrievability.";
        en["Help_Guide_SpacedRepetition_Content"] = "In the Review Page, you encounter the flashcard front (word term, IPA phonetics, audio playback). After actively retrieving the definition in your mind, press Space to reveal the back and choose among 4 ratings:\n• Again (Key 1): Complete lapse -> Resets interval to 1 day for prompt reinforcement.\n• Hard (Key 2): Recalled with significant hesitation (>5s) -> EF decreases slightly, interval grows modestly.\n• Good (Key 3): Correct recall with normal effort -> Interval scales by the current Ease Factor.\n• Easy (Key 4): Instant, effortless recall -> EF increases and the interval receives an extra bonus multiplier.";
        en["Help_Tip_SpacedRepetition_Again"] = "Again (0-1): Never hesitate to select Again upon forgetting. Honesty with the algorithm is the foundation of durable memory consolidation.";
        en["Help_Tip_SpacedRepetition_Hard"] = "Hard (2): Use when recall required over 5 seconds of struggle or slight semantic uncertainty.";
        en["Help_Tip_SpacedRepetition_Good"] = "Good (3): The optimal standard choice for normal, successful recall.";
        en["Help_Tip_SpacedRepetition_Easy"] = "Easy (4): Reserve strictly for words you know intuitively with instantaneous reflex.";

        // Guide 4: Learning Roadmap & Goal Tracking
        en["Help_Guide_Roadmap_Title"] = "Adaptive Learning Roadmap & Exam Pathways";
        en["Help_Guide_Roadmap_Subtitle"] = "Set target scores (TOEIC, IELTS, VSTEP), conquer milestones, and unlock dynamic skill pathways.";
        en["Help_Guide_Roadmap_Summary"] = "The Roadmap visualizes an adaptive serpentine skill tree, dynamically calibrates initial SM-2 Ease Factors based on target score gaps, and unlocks downstream milestones progressively.";
        en["Help_Guide_Roadmap_Formula"] = "Score Gap Calibration: Gap = TargetScore - CurrentScore\nStarting EF Tuning: EF = Clamp(2.5 - (Gap / MaxGap) * 0.5, 2.0, 2.5)\nNode Unlocking: q >= 3 or 100% of node items reached Mastered state.";
        en["Help_Guide_Roadmap_Content"] = "Lexora tailors your path based on your target exam (TOEIC 450-990, IELTS 5.0-8.5, VSTEP B1-C1). The RoadmapEngine dynamically computes your score gap to adjust starting difficulty and initial memory parameters (EF between 2.0 and 2.5). The smooth Bézier spline canvas guides you through milestones from fundamentals to exam mastery. Each node encapsulates a skill unit: reaching rating q >= 3 or learning 100% of target terms automatically unlocks downstream nodes with smooth connected animations.";
        en["Help_Tip_Roadmap_1"] = "Click any node on the roadmap canvas to inspect target vocabulary counts, difficulty tiers, and completion progress.";
        en["Help_Tip_Roadmap_2"] = "Green nodes indicate Mastered status, blue nodes represent Active learning, and grey nodes denote Locked milestones.";
        en["Help_Tip_Roadmap_3"] = "Completing a TOEIC or IELTS roadmap guarantees comprehensive coverage of all high-frequency vocabulary found in actual recent exams.";

        // Guide 5: Knowledge Graph & Semantic Associations
        en["Help_Guide_KnowledgeGraph_Title"] = "Knowledge Graph & Semantic Clusters";
        en["Help_Guide_KnowledgeGraph_Subtitle"] = "Explore interactive lexical networks, synonym/antonym associations, and visual vocabulary clusters.";
        en["Help_Guide_KnowledgeGraph_Summary"] = "Break free from isolated rote learning. The Knowledge Graph links words by semantic relationships, multiplying associative memory retention threefold.";
        en["Help_Guide_KnowledgeGraph_Content"] = "The Knowledge Graph mirrors the brain's associative neural network. Each word acts as a node linked to synonyms, antonyms, word families, and topic clusters. You can fluidly pan, zoom, click any node to listen to audio pronunciation, inspect contextual examples, and press 'Pivot' to re-center the entire graph around that new concept.";
        en["Help_Tip_KnowledgeGraph_1"] = "Click the 'Pivot' button on the detail card to dynamically branch out into vocabulary related to the selected word.";
        en["Help_Tip_KnowledgeGraph_2"] = "Filter the graph by CEFR levels (B1, B2, C1) to strategically expand academic vocabulary aligned with your goals.";
        en["Help_Tip_KnowledgeGraph_3"] = "Studying antonym and synonym pairs drastically sharpens active recall in Writing and Reading exam sections.";

        // Guide 6: Grammar & Pattern Suggester
        en["Help_Guide_Grammar_Title"] = "Grammar Handbook & Sentence Patterns";
        en["Help_Guide_Grammar_Subtitle"] = "Master grammar formulas, avoid subtle syntactic errors, and apply rules in authentic contexts.";
        en["Help_Guide_Grammar_Summary"] = "Look up and practice 50+ essential syntactic structures rigorously mapped to CEFR standards and TOEIC/IELTS test formats.";
        en["Help_Guide_Grammar_Content"] = "The Lexora Grammar engine delivers complete structural formulas, underlying explanations, common mistake warnings, and bilingual example sentences. The interactive Practice Mode allows you to test rule comprehension immediately through smart adaptive quizzes with instant evaluation.";
        en["Help_Tip_Grammar_1"] = "Carefully review the 'Common Mistakes' section for each rule to avoid prevalent traps in TOEIC Part 5 & 6.";
        en["Help_Tip_Grammar_2"] = "Utilize the 'Linked Vocab' feature to see which high-yield verbs and adjectives frequently pair with specific patterns.";
        en["Help_Tip_Grammar_3"] = "Click 'Practice' immediately after reviewing a new rule to solidify understanding into long-term memory.";

        // Guide 7: Learning Storage & Personal Notebooks
        en["Help_Guide_Storage_Title"] = "Learning Storage & Contextual Notebooks";
        en["Help_Guide_Storage_Subtitle"] = "Archive context, collect inspiring excerpts, tag categories, and generate instant quiz drills.";
        en["Help_Guide_Storage_Summary"] = "A personal archive to curate rich sentences, idioms, and collocations discovered during your daily English reading.";
        en["Help_Guide_Storage_Content"] = "While studying or reading English articles and books, save memorable sentences, idioms, or collocations to Learning Storage. Organize items with custom hashtag categories, favorite stars, and auto-generate interactive quiz drills from your saved excerpts to turn passive reading into active mastery.";
        en["Help_Tip_Storage_1"] = "Assign category hashtags (#IELTS_Writing, #Collocation) to sentences for efficient review before exams.";
        en["Help_Tip_Storage_2"] = "Use the 'Generate Drill' button to convert your saved sentence excerpts into engaging fill-in-the-blank exercises.";
        en["Help_Tip_Storage_3"] = "Click the audio speaker icon to listen to entire sentence excerpts read in clear native English pronunciation.";

        // Guide 8: Pronunciation, Homophones & Listening Lab
        en["Help_Guide_ListeningLab_Title"] = "Pronunciation, Homophones & Listening Lab";
        en["Help_Guide_ListeningLab_Subtitle"] = "Distinguish minimal acoustic pairs, master native accents, and avoid common listening exam traps.";
        en["Help_Guide_ListeningLab_Summary"] = "Eradicate acoustic confusion and pronunciation flaws through comparative UK-US audio labs and minimal pair drills.";
        en["Help_Guide_ListeningLab_Content"] = "Homophones & Sound-Alikes categorizes terms with identical or deceptively similar phonetics (e.g. affect/effect, accept/except, their/there). The Listening Lab features 3 dedicated modes: Dual UK/US Accent Comparison, Rapid 3-Second Ear Drill, and Typing Dictation. Adjust playback speeds from 0.75x to 1.5x to progressively sharpen your acoustic acuity.";
        en["Help_Tip_ListeningLab_1"] = "Listen attentively to vowel length contrasts (e.g., /ɪ/ in 'ship' versus /iː/ in 'sheep').";
        en["Help_Tip_ListeningLab_2"] = "Practice Typing Dictation Mode to simultaneously reinforce phonetic decoding and orthographic spelling precision.";
        en["Help_Tip_ListeningLab_3"] = "Toggle seamlessly between British (UK) and American (US) audio voices using Alt+U and Alt+A.";

        // Guide 9: Practice & Adaptive Computerized Testing (CAT Engine)
        en["Help_Guide_Test_Title"] = "Practice & Computer Adaptive Testing (CAT Engine)";
        en["Help_Guide_Test_Subtitle"] = "Measure true proficiency with dynamic difficulty adjustment powered by Item Response Theory (IRT).";
        en["Help_Guide_Test_Summary"] = "Rather than generating static random quizzes, Lexora dynamically calibrates question difficulty to pinpoint your exact CEFR mastery level.";
        en["Help_Guide_Test_Content"] = "Lexora provides 3 evaluation formats: Quick 10-20 Card Quiz, Topic-Based Review Test, and Computer Adaptive Testing (CAT). In CAT mode, answering a B1 question correctly automatically promotes subsequent questions to B2/C1. Incorrect responses step down to A2 to verify foundational mastery. Comprehensive post-test analytics break down performance by CEFR, exam context, and recommended review lists.";
        en["Help_Tip_Test_1"] = "Take a standardized adaptive test every two weeks to measure longitudinal vocabulary growth.";
        en["Help_Tip_Test_2"] = "Carefully inspect incorrect answers post-exam to automatically queue those words for targeted spaced repetition.";
        en["Help_Tip_Test_3"] = "Use numeric keys 1, 2, 3, 4 to swiftly select multiple-choice options A, B, C, D without touching the mouse.";

        // Guide 10: SQLite Database Backup, Restore & Personalization
        en["Help_Guide_Settings_Title"] = "SQLite Database Backup, Safe Restore & Personalization";
        en["Help_Guide_Settings_Subtitle"] = "Protect your learning assets with atomic snapshot backups, WAL flushing, and SQLite header validation.";
        en["Help_Guide_Settings_Summary"] = "All your study progress is stored safely in a local ACID-compliant SQLite database with zero dependency on third-party cloud services.";
        en["Help_Guide_Settings_Content"] = "In Settings, you can create immediate .db snapshots, configure automatic daily/weekly backup schedules, and restore previous archives with rigorous safety invariants (SQLite header validation, connection pool flushing, and automatic rollback on failure). Customize your workspace with Light/Dark/System themes, toggle between SM-2 and FSRS algorithms, and set default audio speeds.";
        en["Help_Tip_Settings_1"] = "Export backup snapshots to external storage (USB drive or cloud drive) monthly for complete peace of mind.";
        en["Help_Tip_Settings_2"] = "Lexora automatically retains the 10 most recent automated snapshots in OS-protected app storage.";
        en["Help_Tip_Settings_3"] = "Press Ctrl+T at any time to toggle smoothly between Light and Dark visual themes.";
    }
}
