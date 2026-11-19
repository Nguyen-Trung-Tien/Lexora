using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class StudyAdvisorService : IStudyAdvisorService
{
    private readonly IReviewRepository _reviewRepo;
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IUserPreferencesService? _preferencesService;

    public StudyAdvisorService(
        IReviewRepository reviewRepo,
        IVocabularyRepository vocabRepo,
        ISpacedRepetitionEngine? srEngine = null,
        IUserPreferencesService? preferencesService = null)
    {
        _reviewRepo = reviewRepo;
        _vocabRepo = vocabRepo;
        _preferencesService = preferencesService;
    }

    public async Task<StudyAdviceSummaryDto> GetDailyAdviceAsync(DashboardSummaryDto? existingSummary = null, CancellationToken cancellationToken = default)
    {
        int dueCount = existingSummary?.DueReviewsToday ?? await _reviewRepo.CountDueCardsAsync(DateTime.UtcNow, cancellationToken);
        var difficultCards = await _reviewRepo.GetDifficultCardsAsync(30, cancellationToken);
        int totalWords = existingSummary?.TotalWords ?? await _vocabRepo.CountAsync(cancellationToken: cancellationToken);

        int mastered;
        int remembered;
        int notRemembered;

        if (existingSummary != null)
        {
            mastered = existingSummary.TotalMastered;
            remembered = existingSummary.TotalRemembered;
            notRemembered = existingSummary.TotalNotRemembered;
        }
        else
        {
            var statusCounts = await _reviewRepo.GetCardCountsByStatusAsync(cancellationToken);
            mastered = statusCounts.GetValueOrDefault(LearningStatus.Mastered, 0);
            remembered = statusCounts.GetValueOrDefault(LearningStatus.Remembered, 0);
            notRemembered = statusCounts.GetValueOrDefault(LearningStatus.NotRemembered, 0);
        }

        int totalActive = Math.Max(mastered + remembered + notRemembered, 1);
        double overallRetentionScore = Math.Round((double)(mastered + remembered) / totalActive * 100, 1);
        double masteredRatio = totalWords > 0 ? Math.Min(100.0, (double)(mastered * 1.0 + remembered * 0.5) / totalWords * 100.0) : overallRetentionScore;
        double examReadiness = Math.Round(overallRetentionScore * 0.65 + masteredRatio * 0.35, 1);
        int weakCount = difficultCards.Count;
        int leechCount = difficultCards.Count(c => c.IsLeech || c.Lapses >= 6 || (c.Lapses >= 4 && c.TotalReviews > 0 && c.Confidence < 40.0));
        string algoLabel = string.Equals(_preferencesService?.GetPreferences()?.SpacedRepetitionAlgorithm, "FSRS", StringComparison.OrdinalIgnoreCase)
            ? "FSRS-5"
            : "SM-2";

        // Determine action recommendation
        if (dueCount > 0)
        {
            return new StudyAdviceSummaryDto(
                UrgentReviewCount: dueCount,
                WeakWordsCount: weakCount,
                OverdueCardsCount: dueCount,
                OverallRetentionScore: overallRetentionScore,
                RecommendedAction: StudyAdviceActionType.UrgentReview,
                AdviceTitleVi: $"Ôn tập trí nhớ định kỳ ({algoLabel})",
                AdviceTitleEn: $"Spaced Repetition Review Due ({algoLabel})",
                AdviceDescriptionVi: $"Có {dueCount} từ vựng đã đến ngưỡng lãng quên Ebbinghaus. Ôn tập ngay để củng cố trí nhớ dài hạn.",
                AdviceDescriptionEn: $"You have {dueCount} word{(dueCount > 1 ? "s" : "")} at the forgetting threshold. Review now to consolidate long-term memory.",
                ActionButtonTextVi: $"Ôn tập ngay ({dueCount} từ)",
                ActionButtonTextEn: $"Review Now ({dueCount} cards)",
                TargetExamContext: "General",
                ExamReadinessPercent: examReadiness,
                LeechCardsCount: leechCount
            );
        }

        if (weakCount > 0)
        {
            return new StudyAdviceSummaryDto(
                UrgentReviewCount: 0,
                WeakWordsCount: weakCount,
                OverdueCardsCount: 0,
                OverallRetentionScore: overallRetentionScore,
                RecommendedAction: StudyAdviceActionType.WeakWordsQuiz,
                AdviceTitleVi: "Chiến dịch khắc phục từ vựng yếu",
                AdviceTitleEn: "Strengthen Challenging Words",
                AdviceDescriptionVi: $"Hệ thống ghi nhận {weakCount} từ bạn đã từng quên hoặc gặp khó khăn. Hãy làm một bài trắc nghiệm nhanh để làm chủ chúng.",
                AdviceDescriptionEn: $"Detected {weakCount} challenging words with higher lapse counts. Take a targeted practice quiz to master them.",
                ActionButtonTextVi: $"Luyện tập {weakCount} từ yếu",
                ActionButtonTextEn: $"Practice {weakCount} Weak Words",
                TargetExamContext: "WeakWords",
                ExamReadinessPercent: examReadiness,
                LeechCardsCount: leechCount
            );
        }

        return new StudyAdviceSummaryDto(
            UrgentReviewCount: 0,
            WeakWordsCount: 0,
            OverdueCardsCount: 0,
            OverallRetentionScore: overallRetentionScore,
            RecommendedAction: StudyAdviceActionType.DailyMaintenance,
            AdviceTitleVi: "Trí nhớ vững vàng! Sẵn sàng bứt phá",
            AdviceTitleEn: "Peak Retention! Ready for New Progress",
            AdviceDescriptionVi: "Toàn bộ từ vựng đã được ôn tập đúng hạn. Bạn có thể mở rộng vốn từ mới hoặc thử thách bản thân với bài thi tổng hợp.",
            AdviceDescriptionEn: "All vocabulary is well retained. Explore new words or challenge yourself with a comprehensive mixed exam simulation.",
            ActionButtonTextVi: "Làm bài thi thử nghiệm",
            ActionButtonTextEn: "Start Practice Test",
            TargetExamContext: "Mixed",
            ExamReadinessPercent: examReadiness,
            LeechCardsCount: leechCount
        );
    }

    public async Task<WorkloadForecastDto> Get7DayForecastAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var forecastDict = await _reviewRepo.GetDueForecastCountsAsync(now, 7, cancellationToken);

        var days = new List<DailyForecastItemDto>();
        int totalDue = 0;
        int peakCount = 0;
        string peakLabel = "Today";

        var today = DateOnly.FromDateTime(DateTime.Today);

        for (int i = 0; i < 7; i++)
        {
            var date = today.AddDays(i);
            int count = forecastDict.GetValueOrDefault(date, 0);
            totalDue += count;

            if (count > peakCount)
            {
                peakCount = count;
                peakLabel = i == 0 ? "Today" : date.DayOfWeek.ToString().Substring(0, 3);
            }

            string intensity = count switch
            {
                0 => "Low",
                <= 5 => "Moderate",
                <= 15 => "High",
                _ => "Critical"
            };

            string dayLabel = i switch
            {
                0 => "Today",
                1 => "Tomorrow",
                _ => date.DayOfWeek switch
                {
                    DayOfWeek.Monday => "Mon",
                    DayOfWeek.Tuesday => "Tue",
                    DayOfWeek.Wednesday => "Wed",
                    DayOfWeek.Thursday => "Thu",
                    DayOfWeek.Friday => "Fri",
                    DayOfWeek.Saturday => "Sat",
                    DayOfWeek.Sunday => "Sun",
                    _ => date.ToString("dd/MM")
                }
            };

            days.Add(new DailyForecastItemDto(
                Date: date,
                DayLabel: dayLabel,
                DueCount: count,
                IsToday: i == 0,
                Intensity: intensity
            ));
        }

        return new WorkloadForecastDto(
            Days: days,
            TotalDueNext7Days: totalDue,
            PeakDayDueCount: peakCount,
            PeakDayLabel: peakLabel
        );
    }

    public async Task<TimeBudgetPlanDto> GetTimeBudgetPlanAsync(int availableMinutes = 15, CancellationToken cancellationToken = default)
    {
        int mins = Math.Clamp(availableMinutes, 5, 120);
        // Average throughput ~2.5 cards/minute for mixed review + new words
        int capacityCards = Math.Max(5, (int)Math.Round(mins * 2.5));

        int dueCount = await _reviewRepo.CountDueCardsAsync(DateTime.UtcNow, cancellationToken);
        var difficultCards = await _reviewRepo.GetDifficultCardsAsync(30, cancellationToken);
        var newCards = await _reviewRepo.GetNewCardsAsync(30, cancellationToken);
        var advice = await GetDailyAdviceAsync(null, cancellationToken);

        int leechCount = difficultCards.Count(c => c.IsLeech || c.Lapses >= 6);
        int urgentPlan = Math.Min(dueCount, (int)Math.Ceiling(capacityCards * 0.65));
        int remaining = Math.Max(0, capacityCards - urgentPlan);
        int leechPlan = Math.Min(leechCount, (int)Math.Ceiling(remaining * 0.50));
        remaining = Math.Max(0, remaining - leechPlan);
        int newPlan = Math.Min(newCards.Count, remaining);
        int totalPlan = urgentPlan + leechPlan + newPlan;

        string summaryVi = $"Gói {mins} phút: {urgentPlan} thẻ đến hạn • {leechPlan} từ hay quên • {newPlan} từ mới.";
        string summaryEn = $"{mins}-min plan: {urgentPlan} due reviews • {leechPlan} leech drills • {newPlan} new words.";

        return new TimeBudgetPlanDto(
            AvailableMinutes: mins,
            UrgentReviewCount: urgentPlan,
            LeechDrillCount: leechPlan,
            NewWordsCount: newPlan,
            EstimatedTotalCards: totalPlan,
            ExamReadinessPercent: advice.ExamReadinessPercent,
            SummaryVi: summaryVi,
            SummaryEn: summaryEn
        );
    }
}
