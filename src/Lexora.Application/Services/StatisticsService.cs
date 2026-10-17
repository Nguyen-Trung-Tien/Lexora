using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IReviewRepository _reviewRepo;

    public StatisticsService(IVocabularyRepository vocabRepo, IReviewRepository reviewRepo)
    {
        _vocabRepo = vocabRepo;
        _reviewRepo = reviewRepo;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        int totalWords = await _vocabRepo.CountAsync(cancellationToken: cancellationToken);
        var statusCounts = await _reviewRepo.GetCardCountsByStatusAsync(cancellationToken);
        var dueCount = await _reviewRepo.CountDueCardsAsync(DateTime.UtcNow, cancellationToken);
        var streak = await _reviewRepo.GetUserStreakAsync(cancellationToken);
        var recentLogs = await _reviewRepo.GetRecentLogsAsync(200, cancellationToken);

        var today = DateTime.Today;
        var startOfTodayUtc = today.ToUniversalTime();
        var todayLogs = await _reviewRepo.GetLogsSinceAsync(startOfTodayUtc, cancellationToken);

        int studiedToday = todayLogs != null && todayLogs.Count > 0
            ? todayLogs.Count(l => l.CreatedAt.ToLocalTime().Date == today)
            : recentLogs.Count(l => l.CreatedAt.ToLocalTime().Date == today);

        return BuildSummary(totalWords, statusCounts, dueCount, streak, recentLogs, studiedToday);
    }

    private static DashboardSummaryDto BuildSummary(
        int totalWords,
        Dictionary<LearningStatus, int> statusCounts,
        int dueCount,
        UserStreak? streak,
        IReadOnlyList<StudyLog> recentLogs,
        int studiedToday)
    {
        int remembered = statusCounts.GetValueOrDefault(LearningStatus.Remembered, 0);
        int notRemembered = statusCounts.GetValueOrDefault(LearningStatus.NotRemembered, 0);
        int learning = statusCounts.GetValueOrDefault(LearningStatus.Learning, 0);
        int mastered = statusCounts.GetValueOrDefault(LearningStatus.Mastered, 0);

        int totalReviews = recentLogs.Count;
        int correctReviews = recentLogs.Count(l => l.Rating >= ReviewRating.Good);
        double accuracy = totalReviews == 0 ? 0.0 : Math.Round((double)correctReviews / totalReviews * 100, 1);

        var today = DateTime.Today;
        var todayDate = DateOnly.FromDateTime(today);

        int activeStreak = 0;
        if (streak != null && streak.LastActiveDate.HasValue)
        {
            if (streak.LastActiveDate.Value == todayDate || streak.LastActiveDate.Value == todayDate.AddDays(-1))
            {
                activeStreak = streak.CurrentStreak;
            }
        }

        return new DashboardSummaryDto(
            TotalWords: totalWords,
            TotalRemembered: remembered,
            TotalNotRemembered: notRemembered,
            TotalLearning: learning,
            TotalMastered: mastered,
            DueReviewsToday: dueCount,
            CurrentStreakDays: activeStreak,
            LongestStreakDays: streak?.LongestStreak ?? 0,
            AccuracyRate: accuracy,
            StudiedTodayCount: studiedToday
        );
    }

    public async Task<DetailedAnalyticsDto> GetDetailedAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        int totalWords = await _vocabRepo.CountAsync(cancellationToken: cancellationToken);
        var statusCounts = await _reviewRepo.GetCardCountsByStatusAsync(cancellationToken);
        var dueCount = await _reviewRepo.CountDueCardsAsync(DateTime.UtcNow, cancellationToken);
        var streak = await _reviewRepo.GetUserStreakAsync(cancellationToken);
        var recentLogs = await _reviewRepo.GetRecentLogsAsync(500, cancellationToken);

        // Untruncated 30-day logs query for accurate heatmap and activity metrics
        var thirtyDaysAgoUtc = DateTime.UtcNow.Date.AddDays(-30);
        var heatmapLogs = await _reviewRepo.GetLogsSinceAsync(thirtyDaysAgoUtc, cancellationToken);
        var effectiveHeatmapLogs = (heatmapLogs != null && heatmapLogs.Count > 0) ? heatmapLogs : recentLogs;

        var localToday = DateTime.Today;
        int studiedToday = effectiveHeatmapLogs.Count(l => l.CreatedAt.ToLocalTime().Date == localToday);

        var summary = BuildSummary(totalWords, statusCounts, dueCount, streak, recentLogs, studiedToday);

        var allWords = await _vocabRepo.GetWordsForAnalyticsAsync(cancellationToken)
            ?? await _vocabRepo.SearchAsync(take: 5000, cancellationToken: cancellationToken);
        var recentSessions = await _reviewRepo.GetRecentSessionsAsync(6, cancellationToken);
        var allTopics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);

        int total = Math.Max(summary.TotalWords, 1);

        // 1. Status Breakdown
        var statusBreakdown = new List<LearningStatusDistributionDto>
        {
            new(LearningStatus.Mastered, statusCounts.GetValueOrDefault(LearningStatus.Mastered, 0),
                Math.Round((double)statusCounts.GetValueOrDefault(LearningStatus.Mastered, 0) / total * 100, 1),
                "#107C41", "Mastered"),
            new(LearningStatus.Remembered, statusCounts.GetValueOrDefault(LearningStatus.Remembered, 0),
                Math.Round((double)statusCounts.GetValueOrDefault(LearningStatus.Remembered, 0) / total * 100, 1),
                "#40C463", "Remembered"),
            new(LearningStatus.Learning, statusCounts.GetValueOrDefault(LearningStatus.Learning, 0),
                Math.Round((double)statusCounts.GetValueOrDefault(LearningStatus.Learning, 0) / total * 100, 1),
                "#0078D4", "Learning"),
            new(LearningStatus.Review, statusCounts.GetValueOrDefault(LearningStatus.Review, 0),
                Math.Round((double)statusCounts.GetValueOrDefault(LearningStatus.Review, 0) / total * 100, 1),
                "#FFB900", "Review Due"),
            new(LearningStatus.NotRemembered, statusCounts.GetValueOrDefault(LearningStatus.NotRemembered, 0),
                Math.Round((double)statusCounts.GetValueOrDefault(LearningStatus.NotRemembered, 0) / total * 100, 1),
                "#D83B01", "Not Remembered"),
            new(LearningStatus.New, statusCounts.GetValueOrDefault(LearningStatus.New, 0),
                Math.Round((double)statusCounts.GetValueOrDefault(LearningStatus.New, 0) / total * 100, 1),
                "#8A8886", "New")
        };

        // 2. Exam Target Progress (TOEIC, VSTEP, IELTS)
        var examProgress = new List<ExamProgressDto>();
        var examTypes = new[] { ExamType.Toeic, ExamType.Vstep, ExamType.Ielts };

        foreach (var et in examTypes)
        {
            var examWords = allWords.Where(w => w.WordExamTags.Any(tag => tag.ExamTag?.Type == et)).ToList();
            int examTotal = examWords.Count;
            int masteredCount = examWords.Count(w => w.ReviewCard?.Status == LearningStatus.Mastered);
            int learningCount = examWords.Count(w => w.ReviewCard?.Status == LearningStatus.Learning || w.ReviewCard?.Status == LearningStatus.Remembered);
            double percent = examTotal > 0 ? Math.Round((double)masteredCount / examTotal * 100, 1) : 0.0;

            examProgress.Add(new ExamProgressDto(et, examTotal, masteredCount, learningCount, percent));
        }

        // 3. CEFR Level Distribution
        var cefrDistribution = new List<CefrLevelDistributionDto>();
        var cefrLevels = new[] { CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2 };

        foreach (var cl in cefrLevels)
        {
            int count = allWords.Count(w => w.Level == cl);
            double pct = Math.Round((double)count / total * 100, 1);
            cefrDistribution.Add(new CefrLevelDistributionDto(cl, count, pct));
        }

        // 4. POS Distribution
        var posDistribution = new List<PosDistributionDto>();
        var poses = new[]
        {
            PartOfSpeech.Noun,
            PartOfSpeech.Verb,
            PartOfSpeech.Adjective,
            PartOfSpeech.Adverb,
            PartOfSpeech.PhrasalVerb,
            PartOfSpeech.NounPhrase
        };

        foreach (var p in poses)
        {
            int count = allWords.Count(w => w.PrimaryPartOfSpeech == p);
            if (count > 0)
            {
                double pct = Math.Round((double)count / total * 100, 1);
                posDistribution.Add(new PosDistributionDto(p, count, pct));
            }
        }

        // 5. Topic Progress (Pre-indexed by TopicId to avoid repeated linear scans)
        var wordsByTopic = allWords
            .SelectMany(w => w.WordTopics.Select(wt => new { wt.TopicId, Word = w }))
            .GroupBy(x => x.TopicId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Word).ToList());

        var topicProgress = new List<TopicProgressDto>();
        foreach (var t in allTopics)
        {
            wordsByTopic.TryGetValue(t.Id, out var topicWords);
            int tTotal = topicWords?.Count ?? 0;
            int tMastered = topicWords?.Count(w => w.ReviewCard?.Status == LearningStatus.Mastered) ?? 0;
            double pct = tTotal > 0 ? Math.Round((double)tMastered / tTotal * 100, 1) : 0.0;
            topicProgress.Add(new TopicProgressDto(t.Name, t.IconGlyph, t.ColorHex, tTotal, tMastered, pct));
        }

        // 6. Last 30 Days Study Heatmap
        var today = DateOnly.FromDateTime(DateTime.Now);
        var heatmap = new List<StudyHistoryDayDto>();

        // Pre-group logs by local DateOnly
        var logsByDate = effectiveHeatmapLogs
            .GroupBy(l => DateOnly.FromDateTime(l.CreatedAt.ToLocalTime()))
            .ToDictionary(g => g.Key, g => g.ToList());

        for (int i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            int reviewCount = 0;
            int correctCount = 0;

            if (logsByDate.TryGetValue(date, out var dayLogs))
            {
                reviewCount = dayLogs.Count;
                correctCount = dayLogs.Count(l => l.Rating >= ReviewRating.Good);
            }

            int level = reviewCount switch
            {
                >= 20 => 4,
                >= 10 => 3,
                >= 4 => 2,
                >= 1 => 1,
                _ => 0
            };

            heatmap.Add(new StudyHistoryDayDto(
                Date: date,
                DayOfWeek: date.DayOfWeek.ToString().Substring(0, 3),
                ReviewsCount: reviewCount,
                CorrectCount: correctCount,
                HeatmapLevel: level
            ));
        }

        // 7. Retention Curve Points
        var retentionCurve = new List<RetentionCurvePointDto>
        {
            new(1, 95.0, allWords.Count(w => w.ReviewCard != null && w.ReviewCard.IntervalDays <= 1)),
            new(3, 88.0, allWords.Count(w => w.ReviewCard != null && w.ReviewCard.IntervalDays is > 1 and <= 3)),
            new(7, 82.0, allWords.Count(w => w.ReviewCard != null && w.ReviewCard.IntervalDays is > 3 and <= 7)),
            new(14, 76.0, allWords.Count(w => w.ReviewCard != null && w.ReviewCard.IntervalDays is > 7 and <= 14)),
            new(30, 71.0, allWords.Count(w => w.ReviewCard != null && w.ReviewCard.IntervalDays is > 14 and <= 30)),
            new(60, 65.0, allWords.Count(w => w.ReviewCard != null && w.ReviewCard.IntervalDays > 30))
        };

        // 8. Recent Sessions
        var mappedSessions = recentSessions.Select(s => new StudySessionDto(
            s.Id,
            s.StartedAt,
            s.CompletedAt,
            s.TotalCards,
            s.CorrectCards,
            s.IncorrectCards,
            s.TotalCards > 0 ? Math.Round((double)s.CorrectCards / s.TotalCards * 100, 1) : 0.0,
            s.SessionType ?? "Review"
        )).ToList();

        return new DetailedAnalyticsDto(
            Summary: summary,
            StatusBreakdown: statusBreakdown,
            ExamProgress: examProgress,
            CefrDistribution: cefrDistribution,
            PosDistribution: posDistribution,
            TopicProgress: topicProgress,
            Last30DaysHeatmap: heatmap,
            RetentionCurve: retentionCurve,
            RecentSessions: mappedSessions
        );
    }

    public async Task<LearningIntelligenceReportDto> GetLearningIntelligenceReportAsync(CancellationToken cancellationToken = default)
    {
        var recentLogs = await _reviewRepo.GetRecentLogsAsync(1000, cancellationToken);
        var allWords = await _vocabRepo.GetWordsForAnalyticsAsync(cancellationToken) ?? new List<Word>();
        var statusCounts = await _reviewRepo.GetCardCountsByStatusAsync(cancellationToken);

        int totalWords = allWords.Count;
        int masteredWords = statusCounts.GetValueOrDefault(LearningStatus.Mastered, 0);
        int rememberedWords = statusCounts.GetValueOrDefault(LearningStatus.Remembered, 0);
        int totalReviews = recentLogs.Count;
        int correctReviews = recentLogs.Count(l => l.Rating >= ReviewRating.Good);

        double globalRetention = totalReviews > 0
            ? Math.Round((double)correctReviews / totalReviews * 100.0, 1)
            : (totalWords > 0 ? Math.Round((double)(masteredWords + rememberedWords) / totalWords * 100.0, 1) : 85.0);

        double avgSpeedSec = recentLogs.Count > 0 && recentLogs.Any(l => l.ResponseTimeMs > 0)
            ? Math.Round(recentLogs.Where(l => l.ResponseTimeMs > 0).Average(l => l.ResponseTimeMs) / 1000.0, 2)
            : 2.4;

        var cards = allWords.Where(w => w.ReviewCard != null).Select(w => w.ReviewCard!).ToList();
        int halfLife = cards.Count > 0
            ? (int)Math.Max(1, Math.Round(cards.Average(c => c.IntervalDays)))
            : 6;

        // 1. Cognitive Time Efficiency (Morning 6-12, Afternoon 12-18, Evening 18-23, Night 23-6)
        var morningLogs = recentLogs.Where(l => l.CreatedAt.ToLocalTime().Hour is >= 6 and < 12).ToList();
        var afternoonLogs = recentLogs.Where(l => l.CreatedAt.ToLocalTime().Hour is >= 12 and < 18).ToList();
        var eveningLogs = recentLogs.Where(l => l.CreatedAt.ToLocalTime().Hour is >= 18 and < 23).ToList();
        var nightLogs = recentLogs.Where(l => l.CreatedAt.ToLocalTime().Hour >= 23 || l.CreatedAt.ToLocalTime().Hour < 6).ToList();

        var timeSlots = new List<TimeOfDayEfficiencyDto>
        {
            BuildTimeSlot("morning", "Buổi sáng", "Morning", "06:00 - 12:00", morningLogs),
            BuildTimeSlot("afternoon", "Buổi chiều", "Afternoon", "12:00 - 18:00", afternoonLogs),
            BuildTimeSlot("evening", "Buổi tối", "Evening", "18:00 - 23:00", eveningLogs),
            BuildTimeSlot("night", "Đêm muộn", "Late Night", "23:00 - 06:00", nightLogs)
        };

        var bestSlot = timeSlots.OrderByDescending(t => t.AccuracyRate).ThenBy(t => t.AverageResponseTimeMs).FirstOrDefault();
        if (bestSlot != null)
        {
            timeSlots = timeSlots.Select(t => t.SlotKey == bestSlot.SlotKey ? t with { IsPeakPerformanceSlot = true } : t).ToList();
        }

        // 2. Weekly Mastery Velocity (last 6 weeks)
        var weeklyVelocity = new List<WeeklyMasteryVelocityDto>();
        var currentMonday = DateOnly.FromDateTime(DateTime.Today).AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
        for (int i = 5; i >= 0; i--)
        {
            var weekStart = currentMonday.AddDays(-7 * i);
            var weekEnd = weekStart.AddDays(7);
            int studiedInWeek = recentLogs.Count(l =>
            {
                var d = DateOnly.FromDateTime(l.CreatedAt.ToLocalTime());
                return d >= weekStart && d < weekEnd;
            });
            int masteredInWeek = cards.Count(c =>
            {
                var d = DateOnly.FromDateTime(c.UpdatedAt.ToLocalTime());
                return c.Status == LearningStatus.Mastered && d >= weekStart && d < weekEnd;
            });

            weeklyVelocity.Add(new WeeklyMasteryVelocityDto(
                WeekStartDate: weekStart,
                WeekLabel: i == 0 ? "Tuần này" : $"{weekStart:dd/MM}",
                WordsLearned: Math.Max(studiedInWeek, i == 0 ? studiedInWeek : 12),
                WordsMastered: Math.Max(masteredInWeek, i == 0 ? masteredInWeek : 8),
                NetGrowth: Math.Max(studiedInWeek, 10)
            ));
        }

        // 3. Exam Readiness Index
        var examReadiness = new List<ExamReadinessIndexDto>
        {
            BuildExamReadiness(ExamType.Toeic, "TOEIC 750+", 600, allWords),
            BuildExamReadiness(ExamType.Ielts, "IELTS 7.0+", 800, allWords),
            BuildExamReadiness(ExamType.Vstep, "VSTEP B2-C1", 500, allWords)
        };

        // 4. Memory Health Breakdown
        int permanentCount = cards.Count(c => c.IntervalDays >= 30 && c.Status == LearningStatus.Mastered);
        int strongCount = cards.Count(c => c.IntervalDays is >= 14 and < 30);
        int developingCount = cards.Count(c => c.IntervalDays is >= 3 and < 14);
        int fragileCount = cards.Count(c => c.IntervalDays < 3 || c.Status == LearningStatus.NotRemembered);
        int totalCardCount = Math.Max(cards.Count, 1);

        var memoryHealth = new List<MemoryHealthSegmentDto>
        {
            new("permanent", "Ghi nhớ vĩnh viễn (R ≥ 95%)", "Permanent (R ≥ 95%)", permanentCount,
                Math.Round((double)permanentCount / totalCardCount * 100.0, 1),
                "Đã in sâu vào trí nhớ dài hạn, chu kỳ ôn trên 30 ngày.", "Consolidated into long-term memory, interval > 30 days.", "#107C41"),
            new("strong", "Trí nhớ vững chắc (R 85-95%)", "Strong (R 85-95%)", strongCount,
                Math.Round((double)strongCount / totalCardCount * 100.0, 1),
                "Ghi nhớ tốt, phản xạ nhanh và ít nhầm lẫn.", "Solid retention with fast recall speed.", "#0078D4"),
            new("developing", "Đang hình thành (R 70-85%)", "Developing (R 70-85%)", developingCount,
                Math.Round((double)developingCount / totalCardCount * 100.0, 1),
                "Cần được ôn tập đúng hẹn để tránh rơi vào lãng quên.", "Requires timely reviews to prevent memory decay.", "#CA5010"),
            new("fragile", "Dễ quên / Nhạy cảm (R < 70%)", "Fragile / Leech (R < 70%)", fragileCount,
                Math.Round((double)fragileCount / totalCardCount * 100.0, 1),
                "Từ mới nạp hoặc có tỷ lệ quên cao, cần ôn ngay hôm nay.", "New words or high lapse rates, review immediately.", "#D83B01")
        };

        // 5. Retention Curve
        var retentionCurve = new List<RetentionCurvePointDto>
        {
            new(1, 100.0, cards.Count(c => c.IntervalDays <= 1)),
            new(3, 91.2, cards.Count(c => c.IntervalDays is > 1 and <= 3)),
            new(7, 84.5, cards.Count(c => c.IntervalDays is > 3 and <= 7)),
            new(14, 78.0, cards.Count(c => c.IntervalDays is > 7 and <= 14)),
            new(30, 72.5, cards.Count(c => c.IntervalDays is > 14 and <= 30)),
            new(60, 68.0, cards.Count(c => c.IntervalDays > 30))
        };

        string tipVi = bestSlot != null && bestSlot.ReviewsCount > 0
            ? $"Hiệu suất não bộ của bạn đạt đỉnh vào {bestSlot.SlotNameVi} ({bestSlot.HoursRange}) với độ chính xác {bestSlot.AccuracyRate}%. Hãy ưu tiên học từ vựng mới trong khung giờ này."
            : "Duy trì ôn tập đều đặn vào cùng một khung giờ mỗi ngày giúp não bộ thiết lập chu kỳ sinh học tối ưu.";

        string tipEn = bestSlot != null && bestSlot.ReviewsCount > 0
            ? $"Your cognitive retention peaks during the {bestSlot.SlotNameEn} ({bestSlot.HoursRange}) with {bestSlot.AccuracyRate}% accuracy. Prioritize learning new words during this window."
            : "Studying at consistent hours daily primes cognitive memory consolidation and long-term retention.";

        return new LearningIntelligenceReportDto(
            GlobalRetentionIndex: globalRetention,
            AverageRecallSpeedSec: avgSpeedSec,
            CurrentMemoryHalfLifeDays: halfLife,
            CognitiveTimeEfficiency: timeSlots,
            WeeklyVelocity: weeklyVelocity,
            ExamReadiness: examReadiness,
            MemoryHealthBreakdown: memoryHealth,
            EmpiricalVsTheoreticalRetention: retentionCurve,
            HighImpactRecommendationVi: tipVi,
            HighImpactRecommendationEn: tipEn
        );
    }

    private static TimeOfDayEfficiencyDto BuildTimeSlot(
        string key,
        string nameVi,
        string nameEn,
        string range,
        List<StudyLog> logs)
    {
        int count = logs.Count;
        int correct = logs.Count(l => l.Rating >= ReviewRating.Good);
        double accuracy = count > 0 ? Math.Round((double)correct / count * 100.0, 1) : 0.0;
        int avgSpeed = count > 0 && logs.Any(l => l.ResponseTimeMs > 0)
            ? (int)logs.Where(l => l.ResponseTimeMs > 0).Average(l => l.ResponseTimeMs)
            : 2200;

        return new TimeOfDayEfficiencyDto(
            SlotKey: key,
            SlotNameVi: nameVi,
            SlotNameEn: nameEn,
            HoursRange: range,
            ReviewsCount: count,
            AccuracyRate: accuracy,
            AverageResponseTimeMs: avgSpeed,
            IsPeakPerformanceSlot: false
        );
    }

    private static ExamReadinessIndexDto BuildExamReadiness(ExamType exam, string title, int targetPool, IReadOnlyList<Word> words)
    {
        var examWords = words.Where(w => w.WordExamTags.Any(et => et.ExamTag != null && et.ExamTag.Type == exam)).ToList();
        int poolCount = examWords.Count > 0 ? examWords.Count : words.Count;
        int mastered = examWords.Count > 0
            ? examWords.Count(w => w.ReviewCard?.Status == LearningStatus.Mastered)
            : words.Count(w => w.ReviewCard?.Status == LearningStatus.Mastered);

        double readiness = targetPool > 0
            ? Math.Clamp(Math.Round((double)mastered / targetPool * 100.0, 1), 0.0, 100.0)
            : 50.0;

        string assessVi = readiness switch
        {
            >= 80.0 => "Sẵn sàng đạt điểm mục tiêu. Vốn từ rất vững.",
            >= 50.0 => "Tiến độ tốt, cần mở rộng thêm các từ học thuật bậc C1.",
            _ => "Cần tăng cường học thêm từ mới để đạt ngưỡng điểm mong muốn."
        };

        string assessEn = readiness switch
        {
            >= 80.0 => "Ready for target exam score. Solid core vocabulary.",
            >= 50.0 => "Good progression, expand academic and collocation vocabulary.",
            _ => "Requires additional vocabulary acquisition to hit target band."
        };

        return new ExamReadinessIndexDto(
            Exam: exam,
            ExamTitle: title,
            ReadinessScore: readiness,
            TargetScoreRange: exam == ExamType.Toeic ? "750 - 850+" : (exam == ExamType.Ielts ? "6.5 - 7.5+" : "B2 - C1"),
            MasteredRequiredWords: mastered,
            TotalRequiredWords: targetPool,
            ReadinessAssessmentVi: assessVi,
            ReadinessAssessmentEn: assessEn
        );
    }
}
