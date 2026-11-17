namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterDashboard(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // Dashboard — vi-VN (Tiếng Việt)
        // ==========================================
        // Dashboard
        vi["Dashboard_HeroTitle"] = "Ôn tập ngắt quãng (SM-2)";
        vi["Dashboard_OfflineReady"] = "Sẵn sàng ngoại tuyến";
        vi["Dashboard_HeroGreeting"] = "Sẵn sàng chinh phục mục tiêu từ vựng hôm nay?";
        vi["Dashboard_StartReview"] = "Ôn tập ngay";
        vi["Dashboard_QuickPractice"] = "Kiểm tra nhanh";
        vi["Dashboard_TotalWords"] = "Tổng số từ";
        vi["Dashboard_DueToday"] = "Cần ôn hôm nay";
        vi["Dashboard_Mastered"] = "Đã thành thạo";
        vi["Dashboard_Streak"] = "Chuỗi ngày học";
        vi["Dashboard_DaysStreak"] = "ngày liên tiếp";
        vi["Dashboard_RetentionChart"] = "Độ bền ghi nhớ (SM-2 Retention Curve)";
        vi["Dashboard_ActivityHeatmap"] = "Bản đồ nhiệt hoạt động (30 ngày gần nhất)";
        vi["Dashboard_EmptyActivity"] = "Chưa có lượt học gần đây. Hãy ôn tập ngay để thắp sáng chuỗi ngày học!";
        vi["Dashboard_RetentionDistribution"] = "Phân bố ghi nhớ từ vựng (SM-2)";
        vi["Dashboard_RetentionSubtitle"] = "Phân bố từ vựng theo các giai đoạn ghi nhớ lặp lại ngắt quãng";
        vi["Dashboard_OverallAccuracy"] = "Tỷ lệ chính xác chung:";
        vi["Dashboard_ExamTargetProgress"] = "Tiến độ mục tiêu kỳ thi";
        vi["Dashboard_ExamTotal"] = "Tổng";
        vi["Dashboard_ExamLearning"] = "Đang học";
        vi["Dashboard_ExamMastered"] = "Đã thuộc";
        vi["Dashboard_HeatmapTitle"] = "Bản đồ nhiệt học tập hàng ngày (30 ngày)";
        vi["Dashboard_HeatmapSubtitle"] = "Theo dõi tính kiên trì và thói quen ôn tập của bạn mỗi ngày";
        vi["Dashboard_HeatmapLess"] = "Ít";
        vi["Dashboard_HeatmapMore"] = "Nhiều";
        vi["Dashboard_ReviewedToday"] = "Đã ôn hôm nay:";
        vi["Dashboard_CurrentStreak"] = "Chuỗi hiện tại:";
        vi["Dashboard_BestStreak"] = "Kỷ lục chuỗi:";
        vi["Dashboard_DaysUnit"] = "ngày";
        vi["Dashboard_CefrBreakdown"] = "Phân bố theo cấp độ CEFR";
        vi["Dashboard_PosBreakdown"] = "Phân bố theo từ loại";
        vi["Dashboard_RecentSessions"] = "Lịch sử học tập & kiểm tra gần đây";
        vi["Dashboard_NoRecentSessions"] = "Chưa có phiên học nào gần đây. Hãy bắt đầu ôn tập hoặc luyện tập ngay!";
        vi["Dashboard_SessionCards"] = "Số thẻ:";
        vi["Dashboard_SessionCorrect"] = "Chính xác:";
        vi["Dashboard_AdvisorTitle"] = "Cố Vấn Học Tập Thông Minh";
        vi["Dashboard_AdvisorBadge"] = "Đề xuất thông minh";
        vi["Dashboard_ForecastTitle"] = "Dự Báo Khối Lượng Ôn Tập (7 Ngày Tới)";
        vi["Dashboard_ForecastSubtitle"] = "Phân bố số thẻ dự kiến đến hạn theo thuật toán lặp lại ngắt quãng SM-2";
        vi["Dashboard_ForecastTotal"] = "Tổng thẻ 7 ngày tới:";
        vi["Dashboard_AiIntelligenceBadge"] = "Trí tuệ học tập AI";
        vi["Dashboard_AiHighImpactBadge"] = "Gợi ý AI trọng tâm";
        // Dashboard Analytics & Cards
        vi["Dashboard_Cards"] = "Số thẻ:";
        vi["Dashboard_Correct"] = "Chính xác:";
        vi["Dashboard_Total"] = "Tổng";
        vi["Dashboard_Learning"] = "Đang học";
        // Newly Added Dashboard
        vi["Dashboard_HeroBadge"] = "SPACED REPETITION (SM-2)";
        vi["Dashboard_DailyConsistency"] = "Thói quen hàng ngày";
        vi["Dashboard_StudyPlannerAction"] = "Lộ trình học";
        vi["Dashboard_ListeningLabAction"] = "Luyện nghe";
        vi["Dashboard_Subtitle"] = "Tổng quan tiến độ học tập, từ đến hạn và mục tiêu mỗi ngày";
        vi["Dashboard_StatActive"] = "Đang học";
        vi["Dashboard_StatUrgent"] = "Cần ôn";
        vi["Dashboard_StatRetention"] = "Ghi nhớ";
        vi["Dashboard_StatHabit"] = "Thói quen";
        vi["Dashboard_StatDays"] = "ngày";
        vi["Dashboard_TooltipTotalWords"] = "Xem tất cả từ vựng trong kho";
        vi["Dashboard_TooltipDueToday"] = "Bắt đầu ôn tập các từ đến hạn";
        vi["Dashboard_TooltipMastered"] = "Xem các từ vựng đã thành thạo";
        vi["Dashboard_TooltipStreak"] = "Ôn tập mỗi ngày để duy trì chuỗi học";
        vi["Dashboard_GreetingMorning"] = "Chào buổi sáng, bạn học!";
        vi["Dashboard_GreetingAfternoon"] = "Chào buổi chiều, bạn học!";
        vi["Dashboard_GreetingEvening"] = "Chào buổi tối, bạn học!";
        // Dashboard Intelligence & Forgetting Curve
        vi["Dashboard_HighImpactTitle"] = "Gợi ý Tối ưu hoá Học tập Đột phá";
        vi["Dashboard_ForgettingCurveTitle"] = "Đường cong Lãng quên Ebbinghaus vs Trí nhớ Thực tế";
        vi["Dashboard_ForgettingCurveSubtitle"] = "So sánh tỷ lệ duy trì kiến thức thực tế của bạn so với đường suy giảm tự nhiên";
        vi["Dashboard_RetentionTheoretical"] = "Lý thuyết tự nhiên";
        vi["Dashboard_RetentionEmpirical"] = "Thực tế của bạn";
        vi["Dashboard_RetentionExceeds"] = "Vượt trội +{0}%";
        vi["Dashboard_CardsAtStage"] = "{0} từ tại mốc này";
        // Dashboard Study Session Types
        vi["Session_SpacedRepetition"] = "Ôn tập ngắt quãng (SM-2)";
        vi["Session_LearnNew"] = "Học từ vựng mới";
        vi["Session_Difficult"] = "Từ khó & Hay quên";
        vi["Session_ByTopic"] = "Ôn theo chủ đề";
        vi["Session_CustomSelection"] = "Ôn từ chọn lọc";
        vi["Session_QuickPractice"] = "Luyện trắc nghiệm nhanh";
        vi["Session_TestMultipleChoice"] = "Bài thi trắc nghiệm";
        vi["Session_TestFillBlank"] = "Bài thi điền từ";
        vi["Session_TestListening"] = "Bài thi luyện nghe";
        // Data Health Center
        vi["DataHealth_Title"] = "Trung tâm Sức khỏe Dữ liệu & SQLite";
        vi["DataHealth_Subtitle"] = "Chẩn đoán toàn vẹn, tối ưu hóa phân mảnh và sửa chữa dữ liệu tự động";
        vi["DataHealth_HealthScore"] = "Điểm sức khỏe cơ sở dữ liệu";
        vi["DataHealth_Optimal"] = "Tối ưu";
        vi["DataHealth_Good"] = "Tốt";
        vi["DataHealth_NeedsMaintenance"] = "Cần bảo trì";
        vi["DataHealth_Critical"] = "Nguy cấp";
        vi["DataHealth_RunDiagnostics"] = "Chạy chẩn đoán";
        vi["DataHealth_RepairAndOptimize"] = "Tối ưu & Sửa lỗi (1-Click)";
        vi["DataHealth_IntegrityStatus"] = "Toàn vẹn SQLite (Integrity)";
        vi["DataHealth_QuickCheck"] = "Kiểm tra nhanh (Quick Check)";
        vi["DataHealth_DbSize"] = "Kích thước cơ sở dữ liệu";
        vi["DataHealth_WalSize"] = "Kích thước tệp WAL";
        vi["DataHealth_PageStats"] = "Số trang & Phân mảnh";
        vi["DataHealth_Reclaimable"] = "Dung lượng có thể giải phóng";
        vi["DataHealth_Orphans"] = "Bản ghi mồ côi (Orphan Records)";
        vi["DataHealth_MissingCards"] = "Thẻ ôn tập thiếu";
        vi["DataHealth_InvalidCards"] = "Thẻ ôn tập sai thông số";
        vi["DataHealth_AudioCache"] = "Bộ nhớ đệm âm thanh TTS";
        vi["DataHealth_ClearAudioCache"] = "Xóa bộ đệm âm thanh";
        vi["DataHealth_AudioCleared"] = "Đã giải phóng {0} bộ đệm âm thanh";
        vi["DataHealth_RepairSuccess"] = "Tối ưu hóa và sửa lỗi hoàn tất thành công!";
        vi["DataHealth_StatusAnalyzing"] = "Đang phân tích cấu trúc và bản ghi SQLite...";
        vi["DataHealth_StatusOptimizing"] = "Đang tối ưu hóa, dọn dẹp bản ghi mồ côi và thực thi SQLite VACUUM...";
        vi["DataHealth_StatusClearingAudio"] = "Đang xóa các tệp âm thanh trong bộ nhớ đệm...";
        vi["DataHealth_DiagnosticsComplete"] = "Chẩn đoán hoàn tất. Điểm sức khỏe: {0}/100 ({1}).";
        // Study Planner
        vi["StudyPlanner_Title"] = "Kế hoạch Học tập & Lộ trình Mục tiêu";
        vi["StudyPlanner_Subtitle"] = "Phân bổ mục tiêu hàng ngày, theo dõi tiến độ và tối ưu lộ trình thi";
        vi["StudyPlanner_GoalCardTitle"] = "Mục tiêu Chứng chỉ & Kế hoạch";
        vi["StudyPlanner_ExamTarget"] = "Kỳ thi mục tiêu";
        vi["StudyPlanner_TargetScore"] = "Điểm số mục tiêu";
        vi["StudyPlanner_TargetDate"] = "Ngày thi dự kiến";
        vi["StudyPlanner_DailyMinutes"] = "Thời gian học mỗi ngày (phút)";
        vi["StudyPlanner_DailyNewWords"] = "Số từ mới mỗi ngày";
        vi["StudyPlanner_DailyReviews"] = "Chỉ tiêu ôn tập mỗi ngày";
        vi["StudyPlanner_SaveGoal"] = "Lưu mục tiêu";
        vi["StudyPlanner_GoalSaved"] = "Mục tiêu học tập đã được cập nhật thành công!";
        vi["StudyPlanner_TodayPlan"] = "Lộ trình học hôm nay";
        vi["StudyPlanner_OverallProgress"] = "Tiến độ ngày";
        vi["StudyPlanner_DaysRemaining"] = "Còn lại {0} ngày";
        vi["StudyPlanner_PacingOnTrack"] = "Đúng tiến độ";
        vi["StudyPlanner_PacingBehind"] = "Cần tăng tốc";
        vi["StudyPlanner_PacingAhead"] = "Vượt tiến độ";
        vi["StudyPlanner_OpenTask"] = "Mở";
        vi["StudyPlanner_TargetScorePlaceholder"] = "VD: 750+, 7.0, B2";
        // Learning Intelligence
        vi["Intelligence_Title"] = "Phân tích Trí tuệ Học tập";
        vi["Intelligence_Subtitle"] = "Hiệu suất não bộ, vận tốc ghi nhớ và xác suất duy trì trí nhớ Ebbinghaus";
        vi["Intelligence_CognitiveSlots"] = "Khung giờ học tối ưu nhất";
        vi["Intelligence_MasteryVelocity"] = "Vận tốc làm chủ từ vựng";
        vi["Intelligence_ExamReadiness"] = "Chỉ số sẵn sàng kỳ thi";
        vi["Intelligence_WordsPerWeek"] = "từ/tuần";
        vi["LearningIntel_Title"] = "Trí tuệ học tập & Trí nhớ nhận thức";
        vi["LearningIntel_Desc"] = "Dự báo đường cong suy giảm trí nhớ Ebbinghaus, tải nhận thức và tỷ lệ duy trì thời gian thực";
        vi["LearningIntel_PeakTime"] = "Khung giờ vàng";
        vi["LearningIntel_MasteryVelocity"] = "Tốc độ thành thạo";
        vi["LearningIntel_ExamReadiness"] = "Độ sẵn sàng thi";
        vi["LearningIntel_MemoryHealth"] = "Sức khỏe trí nhớ";
        vi["LearningIntel_AccuracySuffix"] = "chính xác";
        vi["LearningIntel_InsufficientData"] = "Chưa đủ dữ liệu";
        vi["LearningIntel_VelocityFormat"] = "+{0} từ/tuần ({1} đã học, {2} thành thạo)";
        vi["LearningIntel_VelocityZero"] = "+0 từ/tuần";
        vi["LearningIntel_Evaluating"] = "Đang đánh giá";
        vi["LearningIntel_PredictedScoresTitle"] = "Dự báo điểm thi (Mô phỏng SM-2 / FSRS)";
        vi["LearningIntel_ConfidenceFormat"] = "Độ tin cậy: {0}%";
        vi["LearningIntel_MasteryFormat"] = "Độ phủ: {0}%";
        vi["LearningIntel_DaysToMilestoneFormat"] = "~{0} ngày tới mốc kế tiếp";
        vi["LearningIntel_MilestoneReached"] = "Đã đạt ngưỡng mục tiêu";
        // Contextual Help Tips on Screens
        vi["Dashboard_Help_Title"] = "Trợ giúp: Bảng tổng quan";
        vi["Dashboard_Help_Subtitle"] = "Theo dõi chỉ số và trạng thái học tập";
        vi["Dashboard_Help_Content"] = "Màn hình Dashboard tổng hợp chuỗi học tập (Streak), số thẻ đến hạn hôm nay, tỷ lệ ghi nhớ và biểu đồ phân bổ CEFR. Nhấn 'Ôn tập ngay' để bắt đầu phiên học.";

        // ==========================================
        // Dashboard — en-US (English)
        // ==========================================
        // Dashboard
        en["Dashboard_HeroTitle"] = "Spaced Repetition (SM-2)";
        en["Dashboard_OfflineReady"] = "Offline Ready";
        en["Dashboard_HeroGreeting"] = "Ready to conquer your vocabulary goals today?";
        en["Dashboard_StartReview"] = "Start Today's Review";
        en["Dashboard_QuickPractice"] = "Quick Practice Test";
        en["Dashboard_TotalWords"] = "Total Vocabulary";
        en["Dashboard_DueToday"] = "Due For Review";
        en["Dashboard_Mastered"] = "Mastered Words";
        en["Dashboard_Streak"] = "Day Streak";
        en["Dashboard_DaysStreak"] = "days streak";
        en["Dashboard_RetentionChart"] = "Memory Retention Curve (SM-2)";
        en["Dashboard_ActivityHeatmap"] = "Activity Heatmap (Last 30 Days)";
        en["Dashboard_EmptyActivity"] = "No recent activity recorded. Complete a review to light up your streak!";
        en["Dashboard_RetentionDistribution"] = "Memory Retention Distribution (SM-2)";
        en["Dashboard_RetentionSubtitle"] = "Distribution of vocabulary across spaced repetition learning stages";
        en["Dashboard_OverallAccuracy"] = "Overall Accuracy:";
        en["Dashboard_ExamTargetProgress"] = "Exam Target Progress";
        en["Dashboard_ExamTotal"] = "Total";
        en["Dashboard_ExamLearning"] = "Learning";
        en["Dashboard_ExamMastered"] = "Mastered";
        en["Dashboard_HeatmapTitle"] = "Daily Study Heatmap (Last 30 Days)";
        en["Dashboard_HeatmapSubtitle"] = "Track your daily study consistency and review habit over time";
        en["Dashboard_HeatmapLess"] = "Less";
        en["Dashboard_HeatmapMore"] = "More";
        en["Dashboard_ReviewedToday"] = "Reviewed Today:";
        en["Dashboard_CurrentStreak"] = "Current Streak:";
        en["Dashboard_BestStreak"] = "Best Streak:";
        en["Dashboard_DaysUnit"] = "days";
        en["Dashboard_CefrBreakdown"] = "CEFR Level Breakdown";
        en["Dashboard_PosBreakdown"] = "Part of Speech Breakdown";
        en["Dashboard_RecentSessions"] = "Recent Learning & Test Sessions";
        en["Dashboard_NoRecentSessions"] = "No recent study sessions yet. Start reviewing or practicing today!";
        en["Dashboard_SessionCards"] = "Cards:";
        en["Dashboard_SessionCorrect"] = "Correct:";
        en["Dashboard_AdvisorTitle"] = "Smart Study Advisor";
        en["Dashboard_AdvisorBadge"] = "Smart Recommendation";
        en["Dashboard_ForecastTitle"] = "Workload Forecast (Next 7 Days)";
        en["Dashboard_ForecastSubtitle"] = "Projected review workload scheduled by the SM-2 algorithm";
        en["Dashboard_ForecastTotal"] = "Total reviews next 7 days:";
        en["Dashboard_AiIntelligenceBadge"] = "AI Intelligence";
        en["Dashboard_AiHighImpactBadge"] = "AI High Impact";
        // Dashboard Analytics & Cards
        en["Dashboard_Cards"] = "Cards:";
        en["Dashboard_Correct"] = "Correct:";
        en["Dashboard_Total"] = "Total";
        en["Dashboard_Learning"] = "Learning";
        // Newly Added Dashboard
        en["Dashboard_HeroBadge"] = "SPACED REPETITION (SM-2)";
        en["Dashboard_DailyConsistency"] = "Daily Consistency";
        en["Dashboard_StudyPlannerAction"] = "Study Planner";
        en["Dashboard_ListeningLabAction"] = "Listening Lab";
        en["Dashboard_Subtitle"] = "Daily progress, due reviews, and study goals at a glance";
        en["Dashboard_StatActive"] = "Active";
        en["Dashboard_StatUrgent"] = "Urgent";
        en["Dashboard_StatRetention"] = "Retention";
        en["Dashboard_StatHabit"] = "Habit";
        en["Dashboard_StatDays"] = "days";
        en["Dashboard_TooltipTotalWords"] = "View all vocabulary words";
        en["Dashboard_TooltipDueToday"] = "Start review session for due words";
        en["Dashboard_TooltipMastered"] = "View mastered words";
        en["Dashboard_TooltipStreak"] = "Review daily to keep your streak alive";
        en["Dashboard_GreetingMorning"] = "Good morning, Learner!";
        en["Dashboard_GreetingAfternoon"] = "Good afternoon, Learner!";
        en["Dashboard_GreetingEvening"] = "Good evening, Learner!";
        // Dashboard Intelligence & Forgetting Curve
        en["Dashboard_HighImpactTitle"] = "High-Impact Learning Recommendation";
        en["Dashboard_ForgettingCurveTitle"] = "Ebbinghaus Forgetting Curve vs Real Retention";
        en["Dashboard_ForgettingCurveSubtitle"] = "Compare your actual retention rate against natural cognitive forgetting decay";
        en["Dashboard_RetentionTheoretical"] = "Theoretical Decay";
        en["Dashboard_RetentionEmpirical"] = "Your Actual Retention";
        en["Dashboard_RetentionExceeds"] = "+{0}% Superior";
        en["Dashboard_CardsAtStage"] = "{0} words at this stage";
        // Dashboard Study Session Types
        en["Session_SpacedRepetition"] = "Spaced Repetition (SM-2)";
        en["Session_LearnNew"] = "Learn New Words";
        en["Session_Difficult"] = "Challenging Words";
        en["Session_ByTopic"] = "Topic Review";
        en["Session_CustomSelection"] = "Selected Words Review";
        en["Session_QuickPractice"] = "Quick Practice";
        en["Session_TestMultipleChoice"] = "Multiple Choice Test";
        en["Session_TestFillBlank"] = "Fill in the Blank Test";
        en["Session_TestListening"] = "Listening Test";
        // Data Health Center
        en["DataHealth_Title"] = "Data Health & SQLite Center";
        en["DataHealth_Subtitle"] = "Integrity diagnostics, fragmentation optimization, and automatic data repair";
        en["DataHealth_HealthScore"] = "Database Health Score";
        en["DataHealth_Optimal"] = "Optimal";
        en["DataHealth_Good"] = "Good";
        en["DataHealth_NeedsMaintenance"] = "Needs Maintenance";
        en["DataHealth_Critical"] = "Critical";
        en["DataHealth_RunDiagnostics"] = "Run Diagnostics";
        en["DataHealth_RepairAndOptimize"] = "Optimize & Repair (1-Click)";
        en["DataHealth_IntegrityStatus"] = "SQLite Integrity";
        en["DataHealth_QuickCheck"] = "Quick Check";
        en["DataHealth_DbSize"] = "Database Size";
        en["DataHealth_WalSize"] = "WAL File Size";
        en["DataHealth_PageStats"] = "Pages & Fragmentation";
        en["DataHealth_Reclaimable"] = "Reclaimable Free Space";
        en["DataHealth_Orphans"] = "Orphan Records";
        en["DataHealth_MissingCards"] = "Missing Review Cards";
        en["DataHealth_InvalidCards"] = "Invalid Review Cards";
        en["DataHealth_AudioCache"] = "TTS Audio Cache";
        en["DataHealth_ClearAudioCache"] = "Clear Audio Cache";
        en["DataHealth_AudioCleared"] = "Freed {0} audio cache";
        en["DataHealth_RepairSuccess"] = "Optimization and data repair completed successfully!";
        en["DataHealth_StatusAnalyzing"] = "Analyzing SQLite schema and records...";
        en["DataHealth_StatusOptimizing"] = "Optimizing, cleaning orphans and executing SQLite VACUUM...";
        en["DataHealth_StatusClearingAudio"] = "Clearing cached audio files...";
        en["DataHealth_DiagnosticsComplete"] = "Diagnostics complete. Health score: {0}/100 ({1}).";
        // Study Planner
        en["StudyPlanner_Title"] = "Study Planner & Goal Curriculum";
        en["StudyPlanner_Subtitle"] = "Paced daily milestones, progress tracking, and exam roadmap";
        en["StudyPlanner_GoalCardTitle"] = "Target Exam & Study Goal";
        en["StudyPlanner_ExamTarget"] = "Target Exam";
        en["StudyPlanner_TargetScore"] = "Target Score";
        en["StudyPlanner_TargetDate"] = "Target Date";
        en["StudyPlanner_DailyMinutes"] = "Daily Study Time (minutes)";
        en["StudyPlanner_DailyNewWords"] = "Daily New Words Target";
        en["StudyPlanner_DailyReviews"] = "Daily Review Target";
        en["StudyPlanner_SaveGoal"] = "Save Goal";
        en["StudyPlanner_GoalSaved"] = "Study goal updated successfully!";
        en["StudyPlanner_TodayPlan"] = "Today's Study Plan";
        en["StudyPlanner_OverallProgress"] = "Daily Progress";
        en["StudyPlanner_DaysRemaining"] = "{0} days remaining";
        en["StudyPlanner_PacingOnTrack"] = "On Track";
        en["StudyPlanner_PacingBehind"] = "Behind Schedule";
        en["StudyPlanner_PacingAhead"] = "Ahead of Schedule";
        en["StudyPlanner_OpenTask"] = "Open";
        en["StudyPlanner_TargetScorePlaceholder"] = "e.g. 750+, 7.0, B2";
        // Learning Intelligence
        en["Intelligence_Title"] = "Learning Intelligence & Memory Analytics";
        en["Intelligence_Subtitle"] = "Cognitive efficiency, mastery velocity, and Ebbinghaus retention probability";
        en["Intelligence_CognitiveSlots"] = "Optimal Peak Study Time";
        en["Intelligence_MasteryVelocity"] = "Vocabulary Mastery Velocity";
        en["Intelligence_ExamReadiness"] = "Exam Readiness Index";
        en["Intelligence_WordsPerWeek"] = "words/week";
        en["LearningIntel_Title"] = "Learning Intelligence & Cognitive Analytics";
        en["LearningIntel_Desc"] = "Ebbinghaus retention forecast, learning velocity, and score estimates";
        en["LearningIntel_PeakTime"] = "Peak Time";
        en["LearningIntel_MasteryVelocity"] = "Mastery Velocity";
        en["LearningIntel_ExamReadiness"] = "Exam Readiness";
        en["LearningIntel_MemoryHealth"] = "Memory Health";
        en["LearningIntel_AccuracySuffix"] = "accuracy";
        en["LearningIntel_InsufficientData"] = "Insufficient data";
        en["LearningIntel_VelocityFormat"] = "+{0} words/wk ({1} learned, {2} mastered)";
        en["LearningIntel_VelocityZero"] = "+0 words/wk";
        en["LearningIntel_Evaluating"] = "Evaluating";
        en["LearningIntel_PredictedScoresTitle"] = "Predictive Exam Score Simulator (SM-2 / FSRS)";
        en["LearningIntel_ConfidenceFormat"] = "Confidence: {0}%";
        en["LearningIntel_MasteryFormat"] = "Mastery: {0}%";
        en["LearningIntel_DaysToMilestoneFormat"] = "~{0} days to next milestone";
        en["LearningIntel_MilestoneReached"] = "Target milestone reached";
        // Contextual Help Tips on Screens
        en["Dashboard_Help_Title"] = "Quick Help: Dashboard";
        en["Dashboard_Help_Subtitle"] = "Track key metrics and learning health";
        en["Dashboard_Help_Content"] = "The Dashboard aggregates your streak, today's due cards, retention rate, and CEFR distribution. Click 'Review Now' to immediately begin a spaced repetition session.";
    }
}
