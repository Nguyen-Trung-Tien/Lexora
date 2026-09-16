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

        int remembered = statusCounts.GetValueOrDefault(LearningStatus.Remembered, 0);
        int notRemembered = statusCounts.GetValueOrDefault(LearningStatus.NotRemembered, 0);
        int learning = statusCounts.GetValueOrDefault(LearningStatus.Learning, 0);
        int mastered = statusCounts.GetValueOrDefault(LearningStatus.Mastered, 0);

        int totalReviews = recentLogs.Count;
        int correctReviews = recentLogs.Count(l => l.Rating >= ReviewRating.Good);
        double accuracy = totalReviews == 0 ? 0.0 : Math.Round((double)correctReviews / totalReviews * 100, 1);

        var today = DateTime.Today;
        int studiedToday = recentLogs.Count(l => l.CreatedAt.ToLocalTime().Date == today);

        return new DashboardSummaryDto(
            TotalWords: totalWords,
            TotalRemembered: remembered,
            TotalNotRemembered: notRemembered,
            TotalLearning: learning,
            TotalMastered: mastered,
            DueReviewsToday: dueCount,
            CurrentStreakDays: streak?.CurrentStreak ?? 0,
            LongestStreakDays: streak?.LongestStreak ?? 0,
            AccuracyRate: accuracy,
            StudiedTodayCount: studiedToday
        );
    }

    public async Task<DetailedAnalyticsDto> GetDetailedAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var summary = await GetDashboardSummaryAsync(cancellationToken);
        var allWords = await _vocabRepo.GetWordsForAnalyticsAsync(cancellationToken)
            ?? await _vocabRepo.SearchAsync(take: 5000, cancellationToken: cancellationToken);
        var statusCounts = await _reviewRepo.GetCardCountsByStatusAsync(cancellationToken);
        var recentLogs = await _reviewRepo.GetRecentLogsAsync(500, cancellationToken);
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
                "#D83B01", "Chưa nhớ"),
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

        // 5. Topic Progress
        var topicProgress = new List<TopicProgressDto>();
        foreach (var t in allTopics)
        {
            var topicWords = allWords.Where(w => w.WordTopics.Any(wt => wt.TopicId == t.Id)).ToList();
            int tTotal = topicWords.Count;
            int tMastered = topicWords.Count(w => w.ReviewCard?.Status == LearningStatus.Mastered);
            double pct = tTotal > 0 ? Math.Round((double)tMastered / tTotal * 100, 1) : 0.0;
            topicProgress.Add(new TopicProgressDto(t.Name, t.IconGlyph, t.ColorHex, tTotal, tMastered, pct));
        }

        // 6. Last 30 Days Study Heatmap
        var today = DateOnly.FromDateTime(DateTime.Now);
        var heatmap = new List<StudyHistoryDayDto>();

        // Pre-group logs by local DateOnly
        var logsByDate = recentLogs
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
}
