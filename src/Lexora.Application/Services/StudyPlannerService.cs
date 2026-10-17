using System.Text.Json;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class StudyPlannerService : IStudyPlannerService
{
    private readonly IReviewRepository _reviewRepo;
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IGrammarRepository _grammarRepo;
    private readonly string _storageFilePath;

    private static readonly SemaphoreSlim PlanLock = new(1, 1);

    private class PlannerStorageModel
    {
        public StudyGoalDto? Goal { get; set; }
        public Dictionary<string, Dictionary<string, int>> DailyProgressMap { get; set; } = new();
    }

    public StudyPlannerService(
        IReviewRepository reviewRepo,
        IVocabularyRepository vocabRepo,
        IGrammarRepository grammarRepo,
        string? storagePath = null)
    {
        _reviewRepo = reviewRepo;
        _vocabRepo = vocabRepo;
        _grammarRepo = grammarRepo;

        if (string.IsNullOrEmpty(storagePath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lexoraDir = Path.Combine(appData, "Lexora", "data");
            Directory.CreateDirectory(lexoraDir);
            _storageFilePath = Path.Combine(lexoraDir, "study_planner_state.json");
        }
        else
        {
            _storageFilePath = storagePath;
        }
    }

    public async Task<StudyGoalDto> GetUserGoalAsync(CancellationToken cancellationToken = default)
    {
        var model = await LoadStorageModelAsync(cancellationToken);
        if (model.Goal != null)
        {
            return model.Goal;
        }

        var defaultTargetDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
        return new StudyGoalDto(
            TargetExam: ExamType.Toeic,
            TargetScore: "750+",
            TargetDate: defaultTargetDate,
            DailyMinutesGoal: 25,
            DailyNewWordsGoal: 10,
            DailyReviewTarget: 20,
            IsConfigured: false
        );
    }

    public async Task<StudyGoalDto> SaveUserGoalAsync(StudyGoalDto goal, CancellationToken cancellationToken = default)
    {
        await PlanLock.WaitAsync(cancellationToken);
        try
        {
            var model = await LoadStorageModelAsync(cancellationToken);
            var configuredGoal = goal with { IsConfigured = true };
            model.Goal = configuredGoal;
            await SaveStorageModelAsync(model, cancellationToken);
            return configuredGoal;
        }
        finally
        {
            PlanLock.Release();
        }
    }

    public async Task<DailyCurriculumDto> GetTodayCurriculumAsync(CancellationToken cancellationToken = default)
    {
        var goal = await GetUserGoalAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Real-time metrics
        int dueCount = await _reviewRepo.CountDueCardsAsync(DateTime.UtcNow, cancellationToken);
        var difficultCards = await _reviewRepo.GetDifficultCardsAsync(30, cancellationToken);
        int weakCount = difficultCards?.Count ?? 0;

        var statusCounts = await _reviewRepo.GetCardCountsByStatusAsync(cancellationToken);
        int notRememberedCount = statusCounts != null ? statusCounts.GetValueOrDefault(LearningStatus.NotRemembered, 0) : 0;
        int targetNew = Math.Min(goal.DailyNewWordsGoal > 0 ? goal.DailyNewWordsGoal : 10, Math.Max(notRememberedCount, 5));
        int targetDue = dueCount > 0 ? dueCount : Math.Min(goal.DailyReviewTarget, 15);
        int targetWeak = Math.Min(weakCount, 5);

        var grammarRules = await _grammarRepo.GetAllAsync(cancellationToken);
        GrammarRule? recommendedGrammar = grammarRules?.FirstOrDefault();

        var model = await LoadStorageModelAsync(cancellationToken);
        var dateKey = today.ToString("yyyyMMdd");
        var todayProgress = model.DailyProgressMap.TryGetValue(dateKey, out var dict) ? dict : new Dictionary<string, int>();

        int GetTaskProgress(CurriculumTaskType type) =>
            todayProgress.TryGetValue(type.ToString(), out int val) ? val : 0;

        var tasks = new List<CurriculumTaskDto>
        {
            new CurriculumTaskDto(
                Type: CurriculumTaskType.ReviewDueCards,
                TitleVi: "Ôn tập trí nhớ ngắt quãng (SM-2)",
                TitleEn: "Spaced Repetition Review (SM-2)",
                DescriptionVi: $"Hoàn thành {targetDue} thẻ đến hạn để củng cố trí nhớ dài hạn.",
                DescriptionEn: $"Complete {targetDue} cards due today to strengthen retention.",
                TargetCount: targetDue,
                CompletedCount: Math.Min(GetTaskProgress(CurriculumTaskType.ReviewDueCards), targetDue),
                IsCompleted: GetTaskProgress(CurriculumTaskType.ReviewDueCards) >= targetDue,
                NavigationTag: "Review"
            ),
            new CurriculumTaskDto(
                Type: CurriculumTaskType.LearnNewWords,
                TitleVi: "Nạp từ vựng mới mục tiêu",
                TitleEn: "Acquire New Vocabulary",
                DescriptionVi: $"Khám phá và ghi nhớ {targetNew} từ mới phù hợp mục tiêu {goal.TargetExam}.",
                DescriptionEn: $"Explore and absorb {targetNew} new words aligned with {goal.TargetExam}.",
                TargetCount: targetNew,
                CompletedCount: Math.Min(GetTaskProgress(CurriculumTaskType.LearnNewWords), targetNew),
                IsCompleted: GetTaskProgress(CurriculumTaskType.LearnNewWords) >= targetNew,
                NavigationTag: "Vocabulary"
            )
        };

        if (targetWeak > 0)
        {
            tasks.Add(new CurriculumTaskDto(
                Type: CurriculumTaskType.PracticeWeakWords,
                TitleVi: "Khắc phục từ vựng hay quên",
                TitleEn: "Challenging Words Drill",
                DescriptionVi: $"Luyện tập chuyên sâu {targetWeak} từ hay quên để thoát khỏi bẫy lãng quên.",
                DescriptionEn: $"Practice {targetWeak} challenging words with previous lapses.",
                TargetCount: targetWeak,
                CompletedCount: Math.Min(GetTaskProgress(CurriculumTaskType.PracticeWeakWords), targetWeak),
                IsCompleted: GetTaskProgress(CurriculumTaskType.PracticeWeakWords) >= targetWeak,
                NavigationTag: "Review"
            ));
        }

        if (recommendedGrammar != null)
        {
            tasks.Add(new CurriculumTaskDto(
                Type: CurriculumTaskType.StudyGrammar,
                TitleVi: $"Ngữ pháp trọng điểm: {recommendedGrammar.Title}",
                TitleEn: $"Key Grammar: {recommendedGrammar.Title}",
                DescriptionVi: $"Xem cấu trúc và các câu ví dụ mẫu trong chủ đề {recommendedGrammar.Topic}.",
                DescriptionEn: $"Review structural formulas and practical examples for {recommendedGrammar.Topic}.",
                TargetCount: 1,
                CompletedCount: Math.Min(GetTaskProgress(CurriculumTaskType.StudyGrammar), 1),
                IsCompleted: GetTaskProgress(CurriculumTaskType.StudyGrammar) >= 1,
                NavigationTag: "Grammar"
            ));
        }

        tasks.Add(new CurriculumTaskDto(
            Type: CurriculumTaskType.TakeAdaptiveQuiz,
            TitleVi: "Thử thách trắc nghiệm thích ứng (CAT)",
            TitleEn: "Adaptive Diagnostic Quiz (CAT)",
            DescriptionVi: "Kiểm tra mức độ thành thạo và đo lường trình độ CEFR thực tế hôm nay.",
            DescriptionEn: "Assess word mastery and estimate real-time CEFR level today.",
            TargetCount: 1,
            CompletedCount: Math.Min(GetTaskProgress(CurriculumTaskType.TakeAdaptiveQuiz), 1),
            IsCompleted: GetTaskProgress(CurriculumTaskType.TakeAdaptiveQuiz) >= 1,
            NavigationTag: "Test"
        ));

        int completedTasks = tasks.Count(t => t.IsCompleted);
        double overallProgress = Math.Round((double)completedTasks / tasks.Count * 100.0, 1);
        int daysRemaining = Math.Max(1, goal.TargetDate.DayNumber - today.DayNumber);

        string pacing = overallProgress switch
        {
            >= 100.0 => "Ahead",
            >= 40.0 => "OnTrack",
            _ => "Pending"
        };

        string tipVi = overallProgress switch
        {
            >= 100.0 => "Tuyệt vời! Bạn đã hoàn thành toàn bộ mục tiêu hôm nay. Hãy duy trì chuỗi học tập!",
            >= 50.0 => "Đang tiến triển rất tốt! Hãy tiếp tục để hoàn thành bài tập cuối cùng trước khi kết thúc ngày.",
            _ => "Học đều đặn 20-30 phút mỗi ngày mang lại hiệu quả gấp 5 lần việc học dồn cuối tuần."
        };

        string tipEn = overallProgress switch
        {
            >= 100.0 => "Outstanding! You've accomplished all daily milestones. Keep your study streak burning!",
            >= 50.0 => "Great momentum! Finish the remaining tasks to lock in your daily retention.",
            _ => "Consistent 20-30 minutes daily yields 5x better retention than weekend cramming."
        };

        return new DailyCurriculumDto(
            PlanDate: today,
            TotalEstimatedMinutes: goal.DailyMinutesGoal,
            TotalTasksCount: tasks.Count,
            CompletedTasksCount: completedTasks,
            OverallProgressPercent: overallProgress,
            NewWordsTarget: targetNew,
            DueReviewsTarget: targetDue,
            WeakWordsTarget: targetWeak,
            RecommendedGrammarRuleId: recommendedGrammar?.Id,
            RecommendedGrammarTitle: recommendedGrammar?.Title,
            Tasks: tasks,
            MotivationalTipVi: tipVi,
            MotivationalTipEn: tipEn,
            DaysRemaining: daysRemaining,
            PacingStatus: pacing
        );
    }

    public async Task<DailyCurriculumDto> RecordTaskCompletionAsync(CurriculumTaskType taskType, int completedDelta = 1, CancellationToken cancellationToken = default)
    {
        await PlanLock.WaitAsync(cancellationToken);
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var dateKey = today.ToString("yyyyMMdd");
            var model = await LoadStorageModelAsync(cancellationToken);

            if (!model.DailyProgressMap.TryGetValue(dateKey, out var dict))
            {
                dict = new Dictionary<string, int>();
                model.DailyProgressMap[dateKey] = dict;
            }

            var typeKey = taskType.ToString();
            dict[typeKey] = dict.GetValueOrDefault(typeKey, 0) + completedDelta;

            await SaveStorageModelAsync(model, cancellationToken);
        }
        finally
        {
            PlanLock.Release();
        }

        return await GetTodayCurriculumAsync(cancellationToken);
    }

    private async Task<PlannerStorageModel> LoadStorageModelAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storageFilePath))
        {
            return new PlannerStorageModel();
        }

        try
        {
            using var stream = File.OpenRead(_storageFilePath);
            var model = await JsonSerializer.DeserializeAsync<PlannerStorageModel>(stream, cancellationToken: cancellationToken);
            return model ?? new PlannerStorageModel();
        }
        catch
        {
            return new PlannerStorageModel();
        }
    }

    private async Task SaveStorageModelAsync(PlannerStorageModel model, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(_storageFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var stream = File.Create(_storageFilePath);
        await JsonSerializer.SerializeAsync(stream, model, cancellationToken: cancellationToken);
    }
}
