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
    private readonly ISpacedRepetitionEngine _srEngine;

    public StudyAdvisorService(
        IReviewRepository reviewRepo,
        IVocabularyRepository vocabRepo,
        ISpacedRepetitionEngine srEngine)
    {
        _reviewRepo = reviewRepo;
        _vocabRepo = vocabRepo;
        _srEngine = srEngine;
    }

    public async Task<StudyAdviceSummaryDto> GetDailyAdviceAsync(CancellationToken cancellationToken = default)
    {
        int dueCount = await _reviewRepo.CountDueCardsAsync(DateTime.UtcNow, cancellationToken);
        var difficultCards = await _reviewRepo.GetDifficultCardsAsync(30, cancellationToken);
        var statusCounts = await _reviewRepo.GetCardCountsByStatusAsync(cancellationToken);
        int totalWords = await _vocabRepo.CountAsync(cancellationToken: cancellationToken);

        int mastered = statusCounts.GetValueOrDefault(LearningStatus.Mastered, 0);
        int remembered = statusCounts.GetValueOrDefault(LearningStatus.Remembered, 0);
        int notRemembered = statusCounts.GetValueOrDefault(LearningStatus.NotRemembered, 0);
        int totalActive = Math.Max(mastered + remembered + notRemembered, 1);

        double overallRetentionScore = Math.Round((double)(mastered + remembered) / totalActive * 100, 1);
        int weakCount = difficultCards.Count;

        // Determine action recommendation
        if (dueCount > 0)
        {
            return new StudyAdviceSummaryDto(
                UrgentReviewCount: dueCount,
                WeakWordsCount: weakCount,
                OverdueCardsCount: dueCount,
                OverallRetentionScore: overallRetentionScore,
                RecommendedAction: StudyAdviceActionType.UrgentReview,
                AdviceTitleVi: "Ôn tập trí nhớ định kỳ (SM-2)",
                AdviceTitleEn: "Spaced Repetition Review Due",
                AdviceDescriptionVi: $"Có {dueCount} từ vựng đã đến ngưỡng lãng quên Ebbinghaus. Ôn tập ngay để củng cố trí nhớ dài hạn.",
                AdviceDescriptionEn: $"You have {dueCount} word{(dueCount > 1 ? "s" : "")} at the forgetting threshold. Review now to consolidate long-term memory.",
                ActionButtonTextVi: $"Ôn tập ngay ({dueCount} từ)",
                ActionButtonTextEn: $"Review Now ({dueCount} cards)",
                TargetExamContext: "General"
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
                TargetExamContext: "WeakWords"
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
            TargetExamContext: "Mixed"
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
                0 => "Hôm nay",
                1 => "Ngày mai",
                _ => date.DayOfWeek switch
                {
                    DayOfWeek.Monday => "T2",
                    DayOfWeek.Tuesday => "T3",
                    DayOfWeek.Wednesday => "T4",
                    DayOfWeek.Thursday => "T5",
                    DayOfWeek.Friday => "T6",
                    DayOfWeek.Saturday => "T7",
                    DayOfWeek.Sunday => "CN",
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
}
