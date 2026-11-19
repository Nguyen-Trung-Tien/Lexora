namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterCommon(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Common — vi-VN (Tiếng Việt)
        // ==========================================
        // App & Shell Navigation
        vi["AppTitle"] = "Lexora - Quản lý Học Tiếng Anh";
        // Common Actions
        vi["Action_Edit"] = "Chỉnh sửa";
        vi["Action_Delete"] = "Xóa";
        vi["Action_Save"] = "Lưu";
        vi["Action_Cancel"] = "Hủy";
        vi["Action_Close"] = "Đóng";
        vi["Action_Reset"] = "Đặt lại";
        vi["Action_Search"] = "Tìm kiếm";
        vi["Action_Filter"] = "Bộ lọc";
        vi["Action_Copy"] = "Sao chép";
        vi["Action_Copied"] = "Đã sao chép!";
        vi["Action_Select"] = "Chọn";
        vi["Action_SelectMultiple"] = "Chọn nhiều";
        vi["Action_SelectAll"] = "Chọn tất cả";
        vi["Action_DeselectAll"] = "Bỏ chọn";
        vi["Action_DeleteSelected"] = "Xóa đã chọn ({0})";
        vi["Action_CancelSelection"] = "Hủy chọn";
        vi["Action_Refresh"] = "Làm mới";
        vi["Action_Export"] = "Xuất tệp";
        vi["Action_Import"] = "Nhập tệp";
        vi["Action_Back"] = "Quay lại";
        // Toast Notifications
        vi["Toast_SuccessTitle"] = "Thành công";
        vi["Toast_ErrorTitle"] = "Đã xảy ra lỗi";
        vi["Toast_WarningTitle"] = "Cảnh báo";
        vi["Toast_InfoTitle"] = "Thông báo";
        vi["Toast_Close"] = "Đóng thông báo";
        vi["Status_Mastered"] = "Đã thành thạo";
        vi["Status_Remembered"] = "Đã nhớ";
        vi["Status_Learning"] = "Đang học";
        vi["Status_ReviewDue"] = "Cần ôn tập";
        vi["Status_NotRemembered"] = "Chưa nhớ";
        vi["Status_New"] = "Từ mới";
        vi["Exam_Name_Toeic"] = "Luyện thi TOEIC";
        vi["Exam_Name_Vstep"] = "Luyện thi VSTEP B1-C1";
        vi["Exam_Name_Ielts"] = "Luyện thi IELTS Academic";
        vi["Exam_Name_General"] = "Từ vựng chung";
        vi["Exam_Target_Toeic"] = "Mục tiêu điểm 600 - 850+";
        vi["Exam_Target_Vstep"] = "Mục tiêu bậc B1 - C1";
        vi["Exam_Target_Ielts"] = "Mục tiêu band 6.5 - 8.0+";
        vi["Exam_Target_General"] = "Làm chủ tiếng Anh hàng ngày";
        vi["Action_NotRemembered"] = "Chưa nhớ (Again)";
        vi["Action_Hard"] = "Khó nhớ (Hard)";
        vi["Action_Remembered"] = "Đã nhớ (Good)";
        vi["Action_Easy"] = "Rất dễ (Easy)";
        // Statuses
        vi["Status_Review"] = "Cần ôn tập";
        // Calendar Days
        vi["Day_Today"] = "Hôm nay";
        vi["Day_Tomorrow"] = "Ngày mai";
        vi["Day_Mon"] = "T2";
        vi["Day_Tue"] = "T3";
        vi["Day_Wed"] = "T4";
        vi["Day_Thu"] = "T5";
        vi["Day_Fri"] = "T6";
        vi["Day_Sat"] = "T7";
        vi["Day_Sun"] = "CN";
        vi["Export_OpenFile"] = "Mở file";
        vi["Export_OpenFolder"] = "Mở thư mục";
        vi["Progress_SummaryFormatWithUpdated"] = "Đã nhập mới: {0} | Đã cập nhật: {1} | Bỏ qua: {2} | Lỗi: {3}";
        // Newly Added Common Actions
        vi["Action_View"] = "Xem";
        vi["Action_Review"] = "Ôn tập";
        vi["Action_Quiz"] = "Thi thử";
        vi["Action_More"] = "Thêm";
        // Newly Added Splash Screen
        vi["Splash_Tagline"] = "HỌC TỪ VỰNG THÔNG MINH & LẶP LẠI NGẮT QUÃNG";
        vi["Splash_Badge"] = "PHIÊN BẢN DESKTOP • NGOẠI TUYẾN";
        vi["Splash_StepServices"] = "Hệ thống";
        vi["Splash_StepDatabase"] = "Cơ sở dữ liệu";
        vi["Splash_StepData"] = "Dữ liệu học";
        vi["Splash_StepReady"] = "Sẵn sàng";
        vi["Splash_InitFailed"] = "Không thể khởi tạo Lexora";
        vi["Splash_InitFailedDesc"] = "Đã có lỗi xảy ra khi tải cơ sở dữ liệu SQLite hoặc cấu hình ứng dụng.";
        vi["Splash_Retry"] = "Thử lại";
        vi["Splash_Exit"] = "Đóng ứng dụng";
        vi["Splash_StatusStarting"] = "Đang khởi tạo...";
        vi["Splash_StatusBooting"] = "Khởi động hệ thống Lexora...";
        vi["Splash_StatusCheckingDb"] = "Đang kiểm tra cơ sở dữ liệu SQLite...";
        vi["Splash_StatusLoadingData"] = "Đang tải dữ liệu từ vựng & cài đặt...";
        vi["Splash_StatusReady"] = "Khởi động hoàn tất!";
        vi["Splash_StatusErrorFormat"] = "Lỗi khởi tạo: {0}";
        // Newly Added Time & Relative Formatting
        vi["Time_JustNow"] = "Vừa xong";
        vi["Time_MinutesAgoFormat"] = "{0} phút trước";
        vi["Time_HoursAgoFormat"] = "{0} giờ trước";
        vi["Time_Yesterday"] = "Hôm qua";
        vi["Time_DaysAgoFormat"] = "{0} ngày trước";
        vi["Time_DaysShortFormat"] = "{0} ngày";
        vi["Time_LessThanOneDay"] = "< 1 ngày";
        // Operation Progress & Loading
        vi["Progress_ImportingTitle"] = "Đang nhập dữ liệu từ vựng";
        vi["Progress_ExportingTitle"] = "Đang xuất dữ liệu từ vựng";
        vi["Progress_BackupTitle"] = "Đang tạo bản sao lưu cơ sở dữ liệu";
        vi["Progress_RestoreTitle"] = "Đang khôi phục dữ liệu";
        vi["Progress_GeneratingTestTitle"] = "Đang khởi tạo bài kiểm tra";
        vi["Progress_ProcessingCount"] = "Đang xử lý: {0} / {1}";
        vi["Progress_CompletedTitle"] = "Hoàn tất thành công!";
        vi["Progress_DefaultPreparing"] = "Đang chuẩn bị dữ liệu...";
        vi["Progress_CompletedSummary"] = "Toàn bộ dữ liệu đã được xử lý an toàn.";
        vi["Progress_StatImported"] = "Đã nhập";
        vi["Progress_StatSkipped"] = "Bỏ qua";
        vi["Progress_StatError"] = "Lỗi";
        vi["Common_Warning"] = "Cảnh báo";
        vi["Common_Error"] = "Lỗi";
        vi["Common_Success"] = "Thành công";
        vi["Progress_ItemsCountFormat"] = "{0} / {1} mục";
        vi["Progress_SummaryFormat"] = "Đã nhập: {0} • Bỏ qua: {1} • Lỗi: {2}";
        vi["Progress_ReadingFile"] = "Đang đọc và phân tích tệp tin...";
        vi["Progress_ValidatingWords"] = "Đang kiểm tra và nhập từ vựng...";
        vi["Progress_SavingToDb"] = "Đang ghi vào cơ sở dữ liệu...";
        vi["Progress_PreparingExport"] = "Đang trích xuất dữ liệu từ vựng...";
        vi["Progress_Serializing"] = "Đang định dạng tệp tin...";
        vi["Progress_BackupFlushing"] = "Đang dọn dẹp bộ nhớ đệm WAL...";
        vi["Progress_BackupCopying"] = "Đang tạo bản sao lưu an toàn...";
        vi["Progress_BackupVerifying"] = "Đang kiểm tra tính toàn vẹn cơ sở dữ liệu...";
        vi["Progress_RestoreVerifying"] = "Đang xác thực cấu trúc tệp sao lưu...";
        vi["Progress_RestoreSnapshot"] = "Đang tạo bản snapshot dự phòng an toàn...";
        vi["Progress_RestoreApplying"] = "Đang khôi phục cơ sở dữ liệu...";
        vi["Progress_RestorePages"] = "Đang khôi phục các trang dữ liệu...";
        vi["Progress_RestoreChecking"] = "Đang kiểm tra toàn vẹn sau khi khôi phục...";
        vi["Progress_GeneratingQuestions"] = "Đang tổng hợp các dạng câu hỏi...";
        vi["Progress_ShufflingOptions"] = "Đang tạo phương án lựa chọn và đảo ngẫu nhiên...";
        vi["Progress_LoadingData"] = "Đang tải dữ liệu...";
        vi["Progress_PleaseWait"] = "Vui lòng đợi trong giây lát...";
        vi["Action_Add"] = "Thêm mới";
        vi["Common_OperationCancelled"] = "Thao tác đã bị hủy.";
        vi["Common_ErrorPrefix"] = "Lỗi: {0}";
        vi["Common_FileReadError"] = "Lỗi đọc tệp: {0}";
        vi["Common_ParseError"] = "Lỗi phân tích: {0}";
        // Background Engine / Progress
        vi["Progress_EngineSubtitle"] = "Trình xử lý tác vụ nền Lexora";
        vi["Progress_Badge"] = "TIẾN ĐỘ";
        vi["Cefr_None"] = "Chưa phân loại";
        // Extended Learning Intelligence & CAT
        vi["Day_Unit"] = "ngày";

        // ==========================================
        // Common — en-US (English)
        // ==========================================
        // App & Shell Navigation
        en["AppTitle"] = "Lexora - English Learning Manager";
        // Common Actions
        en["Action_Edit"] = "Edit";
        en["Action_Delete"] = "Delete";
        en["Action_Save"] = "Save";
        en["Action_Cancel"] = "Cancel";
        en["Action_Close"] = "Close";
        en["Action_Reset"] = "Reset";
        en["Action_Search"] = "Search";
        en["Action_Filter"] = "Filters";
        en["Action_Copy"] = "Copy";
        en["Action_Copied"] = "Copied!";
        en["Action_Select"] = "Select";
        en["Action_SelectMultiple"] = "Select Multiple";
        en["Action_SelectAll"] = "Select All";
        en["Action_DeselectAll"] = "Deselect All";
        en["Action_DeleteSelected"] = "Delete Selected ({0})";
        en["Action_CancelSelection"] = "Cancel Selection";
        en["Action_Refresh"] = "Refresh";
        en["Action_Export"] = "Export";
        en["Action_Import"] = "Import";
        en["Action_Back"] = "Back";
        // Toast Notifications
        en["Toast_SuccessTitle"] = "Success";
        en["Toast_ErrorTitle"] = "Error Occurred";
        en["Toast_WarningTitle"] = "Warning";
        en["Toast_InfoTitle"] = "Information";
        en["Toast_Close"] = "Dismiss";
        en["Status_Mastered"] = "Mastered";
        en["Status_Remembered"] = "Remembered";
        en["Status_Learning"] = "Learning";
        en["Status_ReviewDue"] = "Review Due";
        en["Status_NotRemembered"] = "Not Remembered";
        en["Status_New"] = "New";
        en["Exam_Name_Toeic"] = "TOEIC Preparation";
        en["Exam_Name_Vstep"] = "VSTEP B1-C1";
        en["Exam_Name_Ielts"] = "IELTS Academic";
        en["Exam_Name_General"] = "General Vocabulary";
        en["Exam_Target_Toeic"] = "Target score 600 - 850+";
        en["Exam_Target_Vstep"] = "Target level B1 - C1";
        en["Exam_Target_Ielts"] = "Target band 6.5 - 8.0+";
        en["Exam_Target_General"] = "Daily English Mastery";
        en["Action_NotRemembered"] = "Again";
        en["Action_Hard"] = "Hard";
        en["Action_Remembered"] = "Good";
        en["Action_Easy"] = "Easy";
        // Statuses
        en["Status_Review"] = "Review Due";
        // Calendar Days
        en["Day_Today"] = "Today";
        en["Day_Tomorrow"] = "Tomorrow";
        en["Day_Mon"] = "Mon";
        en["Day_Tue"] = "Tue";
        en["Day_Wed"] = "Wed";
        en["Day_Thu"] = "Thu";
        en["Day_Fri"] = "Fri";
        en["Day_Sat"] = "Sat";
        en["Day_Sun"] = "Sun";
        en["Export_OpenFile"] = "Open File";
        en["Export_OpenFolder"] = "Open Folder";
        en["Progress_SummaryFormatWithUpdated"] = "Imported: {0} | Updated: {1} | Skipped: {2} | Errors: {3}";
        // Newly Added Common Actions
        en["Action_View"] = "View";
        en["Action_Review"] = "Review";
        en["Action_Quiz"] = "Quiz";
        en["Action_More"] = "More";
        // Newly Added Splash Screen
        en["Splash_Tagline"] = "INTELLIGENT VOCABULARY & SPACED REPETITION";
        en["Splash_Badge"] = "DESKTOP EDITION • OFFLINE FIRST";
        en["Splash_StepServices"] = "Core";
        en["Splash_StepDatabase"] = "Database";
        en["Splash_StepData"] = "Study Data";
        en["Splash_StepReady"] = "Ready";
        en["Splash_InitFailed"] = "Could not initialize Lexora";
        en["Splash_InitFailedDesc"] = "An error occurred while loading the SQLite database or configuration.";
        en["Splash_Retry"] = "Retry";
        en["Splash_Exit"] = "Exit App";
        en["Splash_StatusStarting"] = "Initializing...";
        en["Splash_StatusBooting"] = "Starting Lexora core systems...";
        en["Splash_StatusCheckingDb"] = "Checking SQLite database & schema...";
        en["Splash_StatusLoadingData"] = "Loading vocabulary & preferences...";
        en["Splash_StatusReady"] = "Ready to launch!";
        en["Splash_StatusErrorFormat"] = "Initialization error: {0}";
        // Newly Added Time & Relative Formatting
        en["Time_JustNow"] = "Just now";
        en["Time_MinutesAgoFormat"] = "{0} min ago";
        en["Time_HoursAgoFormat"] = "{0} h ago";
        en["Time_Yesterday"] = "Yesterday";
        en["Time_DaysAgoFormat"] = "{0} d ago";
        en["Time_DaysShortFormat"] = "{0} d";
        en["Time_LessThanOneDay"] = "< 1 d";
        // Operation Progress & Loading
        en["Progress_ImportingTitle"] = "Importing Vocabulary Data";
        en["Progress_ExportingTitle"] = "Exporting Vocabulary Data";
        en["Progress_BackupTitle"] = "Creating Database Backup";
        en["Progress_RestoreTitle"] = "Restoring Database";
        en["Progress_GeneratingTestTitle"] = "Generating Practice Test";
        en["Progress_ProcessingCount"] = "Processing: {0} / {1}";
        en["Progress_CompletedTitle"] = "Completed Successfully!";
        en["Progress_DefaultPreparing"] = "Preparing data...";
        en["Progress_CompletedSummary"] = "All data processed securely.";
        en["Progress_StatImported"] = "Imported";
        en["Progress_StatSkipped"] = "Skipped";
        en["Progress_StatError"] = "Errors";
        en["Common_Warning"] = "Warning";
        en["Common_Error"] = "Error";
        en["Common_Success"] = "Success";
        en["Progress_ItemsCountFormat"] = "{0} / {1} items";
        en["Progress_SummaryFormat"] = "Imported: {0} • Skipped: {1} • Errors: {2}";
        en["Progress_ReadingFile"] = "Reading and parsing file...";
        en["Progress_ValidatingWords"] = "Validating and importing terms...";
        en["Progress_SavingToDb"] = "Writing to database...";
        en["Progress_PreparingExport"] = "Extracting vocabulary records...";
        en["Progress_Serializing"] = "Formatting export payload...";
        en["Progress_BackupFlushing"] = "Flushing database memory pools...";
        en["Progress_BackupCopying"] = "Creating secure database snapshot...";
        en["Progress_BackupVerifying"] = "Verifying database integrity...";
        en["Progress_RestoreVerifying"] = "Verifying backup header integrity...";
        en["Progress_RestoreSnapshot"] = "Creating pre-restore safety snapshot...";
        en["Progress_RestoreApplying"] = "Restoring database state...";
        en["Progress_RestorePages"] = "Restoring database pages...";
        en["Progress_RestoreChecking"] = "Verifying restored database integrity...";
        en["Progress_GeneratingQuestions"] = "Compiling test question types...";
        en["Progress_ShufflingOptions"] = "Generating and shuffling answer options...";
        en["Progress_LoadingData"] = "Loading data...";
        en["Progress_PleaseWait"] = "Please wait a moment...";
        en["Action_Add"] = "Add New";
        en["Common_OperationCancelled"] = "Operation was cancelled.";
        en["Common_ErrorPrefix"] = "Error: {0}";
        en["Common_FileReadError"] = "File read error: {0}";
        en["Common_ParseError"] = "Parsing error: {0}";
        // Background Engine / Progress
        en["Progress_EngineSubtitle"] = "Lexora Background Task Engine";
        en["Progress_Badge"] = "PROGRESS";
        en["Cefr_None"] = "Unclassified";
        // Extended Learning Intelligence & CAT
        en["Day_Unit"] = "days";
    }
}
