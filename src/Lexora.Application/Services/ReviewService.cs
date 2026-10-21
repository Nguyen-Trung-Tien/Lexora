using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepo;
    private readonly ISpacedRepetitionEngine _srEngine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILearningStorageRepository? _storageRepo;
    private readonly IFsrsSpacedRepetitionEngine? _fsrsEngine;
    private readonly IUserPreferencesService? _preferencesService;

    public ReviewService(
        IReviewRepository reviewRepo,
        ISpacedRepetitionEngine srEngine,
        IUnitOfWork unitOfWork,
        ILearningStorageRepository? storageRepo = null,
        IFsrsSpacedRepetitionEngine? fsrsEngine = null,
        IUserPreferencesService? preferencesService = null)
    {
        _reviewRepo = reviewRepo;
        _srEngine = srEngine;
        _unitOfWork = unitOfWork;
        _storageRepo = storageRepo;
        _fsrsEngine = fsrsEngine;
        _preferencesService = preferencesService;
    }

    private (ISpacedRepetitionEngine engine, double desiredRetention, bool isFsrs) GetActiveEngine()
    {
        var prefs = _preferencesService?.GetPreferences();
        bool isFsrs = _fsrsEngine != null && string.Equals(prefs?.SpacedRepetitionAlgorithm, "FSRS", StringComparison.OrdinalIgnoreCase);
        double retention = prefs?.TargetRetention > 0 ? prefs.TargetRetention : 0.90;
        return (isFsrs ? (ISpacedRepetitionEngine)_fsrsEngine! : _srEngine, retention, isFsrs);
    }

    public async Task<IReadOnlyList<ReviewCardDto>> GetDueCardsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var cards = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, limit, cancellationToken);
        return cards.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ReviewCardDto>> GetBalancedReviewQueueAsync(int limit = 30, CancellationToken cancellationToken = default)
    {
        if (limit <= 0) limit = 30;

        // Quotas: 50% mature due cards, 30% weak/lapsed, 20% new cards
        int dueQuota = (int)Math.Ceiling(limit * 0.50);
        int weakQuota = (int)Math.Ceiling(limit * 0.30);
        int newQuota = limit - dueQuota - weakQuota;
        if (newQuota <= 0) newQuota = (int)Math.Ceiling(limit * 0.20);

        var result = new List<ReviewCardDto>();
        var seenCardIds = new HashSet<Guid>();

        // 1. Fetch Due Cards
        var dueCards = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, dueQuota * 2, cancellationToken);
        foreach (var c in dueCards)
        {
            if (result.Count >= dueQuota) break;
            if (seenCardIds.Add(c.Id))
            {
                result.Add(MapToDto(c));
            }
        }

        // 2. Fetch Weak / Difficult Cards
        var weakCards = await _reviewRepo.GetDifficultCardsAsync(weakQuota * 2, cancellationToken);
        int addedWeak = 0;
        foreach (var c in weakCards)
        {
            if (addedWeak >= weakQuota || result.Count >= limit) break;
            if (seenCardIds.Add(c.Id))
            {
                result.Add(MapToDto(c));
                addedWeak++;
            }
        }

        // 3. Fetch New Cards
        var newCards = await _reviewRepo.GetNewCardsAsync(newQuota * 2, cancellationToken);
        int addedNew = 0;
        foreach (var c in newCards)
        {
            if (addedNew >= newQuota || result.Count >= limit) break;
            if (seenCardIds.Add(c.Id))
            {
                result.Add(MapToDto(c));
                addedNew++;
            }
        }

        // 4. Backfill from remaining due cards if under limit
        if (result.Count < limit)
        {
            foreach (var c in dueCards)
            {
                if (result.Count >= limit) break;
                if (seenCardIds.Add(c.Id))
                {
                    result.Add(MapToDto(c));
                }
            }
        }

        // 5. Backfill from remaining new cards if still under limit
        if (result.Count < limit)
        {
            foreach (var c in newCards)
            {
                if (result.Count >= limit) break;
                if (seenCardIds.Add(c.Id))
                {
                    result.Add(MapToDto(c));
                }
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<ReviewCardDto>> GetNewCardsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var cards = await _reviewRepo.GetNewCardsAsync(limit, cancellationToken);
        return cards.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ReviewCardDto>> GetDifficultCardsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var cards = await _reviewRepo.GetDifficultCardsAsync(limit, cancellationToken);
        return cards.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ReviewCardDto>> GetCardsByTopicIdAsync(Guid topicId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var cards = await _reviewRepo.GetCardsByTopicIdAsync(topicId, limit, cancellationToken);
        return cards.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ReviewCardDto>> GetCardsByWordIdsAsync(IReadOnlyList<Guid> wordIds, CancellationToken cancellationToken = default)
    {
        var cards = await _reviewRepo.GetCardsByWordIdsAsync(wordIds, cancellationToken);
        return cards.Select(MapToDto).ToList();
    }

    public async Task<int> GetDueCardsCountAsync(CancellationToken cancellationToken = default)
    {
        return await _reviewRepo.CountDueCardsAsync(DateTime.UtcNow, cancellationToken);
    }

    public async Task<StudySessionDto> StartSessionAsync(ReviewSessionMode mode, int limit = 30, CancellationToken cancellationToken = default)
    {
        var session = new StudySession
        {
            StartedAt = DateTime.UtcNow,
            SessionType = mode.ToString()
        };

        var created = await _reviewRepo.CreateStudySessionAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToSessionDto(created);
    }

    public async Task<StudySessionDto> CompleteSessionAsync(Guid sessionId, int totalCards, int correctCount, int incorrectCount, CancellationToken cancellationToken = default)
    {
        var session = await _reviewRepo.GetStudySessionByIdAsync(sessionId, cancellationToken);
        if (session == null)
            throw new KeyNotFoundException($"StudySession with ID {sessionId} not found");

        session.CompletedAt = DateTime.UtcNow;
        session.TotalCards = totalCards;
        session.CorrectCards = correctCount;
        session.IncorrectCards = incorrectCount;
        session.UpdatedAt = DateTime.UtcNow;

        await _reviewRepo.UpdateStudySessionAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToSessionDto(session);
    }

    public async Task<ReviewEvaluationResultDto> SubmitReviewAsync(
        Guid reviewCardId,
        ReviewRating rating,
        int responseTimeMs = 0,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var card = await _reviewRepo.GetByIdAsync(reviewCardId, cancellationToken);
        if (card == null)
            throw new KeyNotFoundException($"ReviewCard with ID {reviewCardId} not found");

        var prevStatus = card.Status;
        var (engine, desiredRetention, isFsrs) = GetActiveEngine();

        SpacedRepetitionResult result;
        if (isFsrs && _fsrsEngine != null)
        {
            var fsrsState = _fsrsEngine.CalculateFsrsNextReview(card, rating, DateTime.UtcNow, desiredRetention);
            double mappedEaseFactor = Math.Clamp(1.3 + (10.0 - fsrsState.Difficulty) * 0.15, 1.3, 3.0);
            mappedEaseFactor = Math.Round(mappedEaseFactor, 2);

            result = new SpacedRepetitionResult(
                NewEaseFactor: mappedEaseFactor,
                NewIntervalDays: fsrsState.IntervalDays,
                NewRepetitions: fsrsState.Repetitions,
                NewLapses: fsrsState.Lapses,
                NewStatus: fsrsState.Status,
                NextReviewAt: fsrsState.NextReviewAt
            );
        }
        else
        {
            result = _srEngine.CalculateNextReview(card, rating, DateTime.UtcNow);
        }

        // Apply algorithm results
        card.EaseFactor = result.NewEaseFactor;
        card.IntervalDays = result.NewIntervalDays;
        card.Repetitions = result.NewRepetitions;
        card.Lapses = result.NewLapses;
        card.Status = result.NewStatus;
        card.NextReviewAt = result.NextReviewAt;
        card.LastReviewedAt = DateTime.UtcNow;
        card.TotalReviews++;
        if (rating >= ReviewRating.Good)
        {
            card.CorrectReviews++;
        }
        card.UpdatedAt = DateTime.UtcNow;

        // Log the review action
        var log = new StudyLog
        {
            ReviewCardId = card.Id,
            StudySessionId = sessionId,
            Rating = rating,
            ResponseTimeMs = responseTimeMs,
            ResultingStatus = card.Status,
            ResultingIntervalDays = card.IntervalDays,
            CreatedAt = DateTime.UtcNow
        };
        _reviewRepo.AddStudyLog(log);

        // Update streak (using local user date to reflect user's real calendar day)
        await _reviewRepo.UpdateStreakAsync(DateOnly.FromDateTime(DateTime.Now), cancellationToken);

        await _reviewRepo.UpdateAsync(card, cancellationToken);

        // Auto-sync with Learning Storage when user lapses or rates Again
        if (rating == ReviewRating.Again && _storageRepo != null)
        {
            var existingStorage = await _storageRepo.GetByWordIdAsync(card.WordId, cancellationToken);
            if (existingStorage == null)
            {
                var storageItem = new LearningStorageItem
                {
                    ItemType = StorageItemType.Vocabulary,
                    WordId = card.WordId,
                    Status = StorageLearningStatus.Weak,
                    ReasonAdded = StorageAdditionReason.Lapsed,
                    EaseFactor = card.EaseFactor,
                    IntervalDays = 1,
                    Repetitions = 0,
                    Lapses = card.Lapses,
                    RetentionProbability = engine.CalculateRetentionProbability(card),
                    NextReviewAt = card.NextReviewAt,
                    LastReviewedAt = card.LastReviewedAt
                };
                await _storageRepo.AddAsync(storageItem, cancellationToken);
            }
            else
            {
                existingStorage.Status = StorageLearningStatus.Weak;
                existingStorage.Lapses = card.Lapses;
                existingStorage.IntervalDays = 1;
                existingStorage.NextReviewAt = card.NextReviewAt;
                existingStorage.RetentionProbability = engine.CalculateRetentionProbability(card);
                existingStorage.UpdatedAt = DateTime.UtcNow;
                await _storageRepo.UpdateAsync(existingStorage, cancellationToken);
            }
        }
        else if (card.Status == LearningStatus.Mastered && _storageRepo != null)
        {
            var existingStorage = await _storageRepo.GetByWordIdAsync(card.WordId, cancellationToken);
            if (existingStorage != null && existingStorage.Status != StorageLearningStatus.Mastered)
            {
                existingStorage.Status = StorageLearningStatus.Mastered;
                existingStorage.IntervalDays = card.IntervalDays;
                existingStorage.NextReviewAt = card.NextReviewAt;
                existingStorage.RetentionProbability = engine.CalculateRetentionProbability(card);
                existingStorage.UpdatedAt = DateTime.UtcNow;
                await _storageRepo.UpdateAsync(existingStorage, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if ((rating == ReviewRating.Again || card.Status == LearningStatus.Mastered) && _storageRepo != null)
        {
            LearningStorageService.NotifyGlobalStorageChanged();
        }

        return new ReviewEvaluationResultDto(
            card.Id,
            rating,
            prevStatus,
            card.Status,
            card.IntervalDays,
            card.NextReviewAt
        );
    }

    public async Task UndoReviewAsync(
        Guid reviewCardId,
        LearningStatus previousStatus,
        int previousInterval,
        double previousEaseFactor,
        int previousRepetitions,
        int previousLapses,
        DateTime previousNextReviewAt,
        CancellationToken cancellationToken = default)
    {
        var card = await _reviewRepo.GetByIdAsync(reviewCardId, cancellationToken);
        if (card == null) return;

        // Retrieve the last study log to accurately roll back metrics
        var lastLog = await _reviewRepo.GetLastStudyLogAsync(reviewCardId, cancellationToken);
        if (lastLog != null)
        {
            if (lastLog.Rating >= ReviewRating.Good && card.CorrectReviews > 0)
            {
                card.CorrectReviews--;
            }

            // Roll back Learning Storage entry if this review was rated Again
            if (lastLog.Rating == ReviewRating.Again && _storageRepo != null)
            {
                var existingStorage = await _storageRepo.GetByWordIdAsync(card.WordId, cancellationToken);
                if (existingStorage != null)
                {
                    if (existingStorage.ReasonAdded == StorageAdditionReason.Lapsed && existingStorage.Repetitions == 0)
                    {
                        await _storageRepo.DeleteAsync(existingStorage, cancellationToken);
                    }
                    else
                    {
                        existingStorage.Lapses = previousLapses;
                        existingStorage.IntervalDays = previousInterval;
                        existingStorage.NextReviewAt = previousNextReviewAt;
                        existingStorage.RetentionProbability = GetActiveEngine().engine.CalculateRetentionProbability(card);
                        existingStorage.UpdatedAt = DateTime.UtcNow;
                        await _storageRepo.UpdateAsync(existingStorage, cancellationToken);
                    }
                }
            }

            await _reviewRepo.DeleteStudyLogAsync(lastLog, cancellationToken);
        }

        card.Status = previousStatus;
        card.IntervalDays = previousInterval;
        card.EaseFactor = previousEaseFactor;
        card.Repetitions = previousRepetitions;
        card.Lapses = previousLapses;
        card.NextReviewAt = previousNextReviewAt;
        if (card.TotalReviews > 0) card.TotalReviews--;
        card.UpdatedAt = DateTime.UtcNow;

        await _reviewRepo.UpdateAsync(card, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (lastLog?.Rating == ReviewRating.Again && _storageRepo != null)
        {
            LearningStorageService.NotifyGlobalStorageChanged();
        }
    }

    public async Task MarkStatusQuickAsync(Guid wordId, bool remembered, CancellationToken cancellationToken = default)
    {
        var card = await _reviewRepo.GetByWordIdAsync(wordId, cancellationToken);
        if (card == null)
        {
            card = new ReviewCard
            {
                WordId = wordId,
                Status = remembered ? LearningStatus.Remembered : LearningStatus.NotRemembered,
                NextReviewAt = remembered ? DateTime.UtcNow.AddDays(3) : DateTime.UtcNow.AddDays(1)
            };
            await _reviewRepo.AddAsync(card, cancellationToken);
        }
        else
        {
            card.Status = remembered ? LearningStatus.Remembered : LearningStatus.NotRemembered;
            card.NextReviewAt = remembered ? DateTime.UtcNow.AddDays(Math.Max(2, card.IntervalDays)) : DateTime.UtcNow.AddDays(1);
            card.TotalReviews++;
            if (remembered) card.CorrectReviews++;
            card.UpdatedAt = DateTime.UtcNow;
            await _reviewRepo.UpdateAsync(card, cancellationToken);
        }

        // Also record a study log and streak
        var log = new StudyLog
        {
            ReviewCardId = card.Id,
            Rating = remembered ? ReviewRating.Good : ReviewRating.Again,
            ResultingStatus = card.Status,
            ResultingIntervalDays = card.IntervalDays,
            CreatedAt = DateTime.UtcNow
        };
        _reviewRepo.AddStudyLog(log);

        await _reviewRepo.UpdateStreakAsync(DateOnly.FromDateTime(DateTime.Now), cancellationToken);

        if (!remembered && _storageRepo != null)
        {
            var existingStorage = await _storageRepo.GetByWordIdAsync(wordId, cancellationToken);
            if (existingStorage == null)
            {
                var storageItem = new LearningStorageItem
                {
                    ItemType = StorageItemType.Vocabulary,
                    WordId = wordId,
                    Status = StorageLearningStatus.Weak,
                    ReasonAdded = StorageAdditionReason.Manual,
                    EaseFactor = card.EaseFactor,
                    IntervalDays = 1,
                    Repetitions = 0,
                    Lapses = card.Lapses,
                    RetentionProbability = GetActiveEngine().engine.CalculateRetentionProbability(card),
                    NextReviewAt = card.NextReviewAt,
                    LastReviewedAt = card.LastReviewedAt
                };
                await _storageRepo.AddAsync(storageItem, cancellationToken);
            }
            else
            {
                existingStorage.Status = StorageLearningStatus.Weak;
                existingStorage.NextReviewAt = card.NextReviewAt;
                existingStorage.UpdatedAt = DateTime.UtcNow;
                await _storageRepo.UpdateAsync(existingStorage, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!remembered && _storageRepo != null)
        {
            LearningStorageService.NotifyGlobalStorageChanged();
        }
    }

    public IntervalPreviewDto GetCardIntervalPreview(ReviewCardDto card)
    {
        var dummyCard = new ReviewCard
        {
            EaseFactor = card.EaseFactor,
            IntervalDays = card.IntervalDays,
            Repetitions = card.Repetitions,
            Lapses = card.Lapses,
            Status = card.Status
        };

        var (engine, _, _) = GetActiveEngine();
        return engine.GetIntervalPreview(dummyCard);
    }

    public double GetRetentionProbability(ReviewCardDto card)
    {
        var dummyCard = new ReviewCard
        {
            Id = card.Id,
            WordId = card.WordId,
            Status = card.Status,
            EaseFactor = card.EaseFactor,
            IntervalDays = card.IntervalDays,
            Repetitions = card.Repetitions,
            Lapses = card.Lapses,
            LastReviewedAt = card.LastReviewedAt,
            NextReviewAt = card.NextReviewAt
        };

        var (engine, _, _) = GetActiveEngine();
        return engine.CalculateRetentionProbability(dummyCard);
    }

    public MemoryStabilityMetricsDto GetMemoryStabilityMetrics(ReviewCardDto card)
    {
        double retrievability = GetRetentionProbability(card);
        double stabilityDays = Math.Max(1.0, card.IntervalDays * (card.EaseFactor / 2.5));
        var preview = GetCardIntervalPreview(card);

        string healthLevel = retrievability switch
        {
            >= 85.0 => "Permanent",
            >= 70.0 => "Stable",
            >= 50.0 => "Developing",
            _ => "Fragile"
        };

        return new MemoryStabilityMetricsDto(
            WordId: card.WordId,
            Term: card.Term,
            StabilityDays: Math.Round(stabilityDays, 1),
            RetrievabilityPercent: retrievability,
            EaseFactor: card.EaseFactor,
            Repetitions: card.Repetitions,
            Lapses: card.Lapses,
            IntervalIfAgain: preview.AgainText,
            IntervalIfHard: preview.HardText,
            IntervalIfGood: preview.GoodText,
            IntervalIfEasy: preview.EasyText,
            MemoryHealthLevel: healthLevel
        );
    }

    public async Task<IReadOnlyList<SmartReviewPresetInfo>> GetSmartPresetsAsync(CancellationToken cancellationToken = default)
    {
        int dueCount = await _reviewRepo.CountDueCardsAsync(DateTime.UtcNow, cancellationToken);
        var difficult = await _reviewRepo.GetDifficultCardsAsync(50, cancellationToken);
        var newCards = await _reviewRepo.GetNewCardsAsync(50, cancellationToken);

        var presets = new List<SmartReviewPresetInfo>
        {
            new SmartReviewPresetInfo(
                Mode: SmartReviewPresetMode.StandardDue,
                TitleVi: "Ôn tập định kỳ SM-2",
                TitleEn: "Spaced Repetition Due",
                DescriptionVi: "Ôn các từ đã chạm ngưỡng lãng quên Ebbinghaus theo thuật toán chuẩn.",
                DescriptionEn: "Review words reaching the forgetting threshold based on SM-2.",
                IconGlyph: "\uE895",
                AvailableCardsCount: dueCount
            ),
            new SmartReviewPresetInfo(
                Mode: SmartReviewPresetMode.SpeedBlitz,
                TitleVi: "Speed Blitz (Phản xạ siêu tốc)",
                TitleEn: "Speed Blitz Reflex",
                DescriptionVi: "Luyện phản xạ tức thì 3 giây/từ cho các từ vựng đã nắm bắt.",
                DescriptionEn: "High-speed 3-second rapid recall drills to achieve fluency.",
                IconGlyph: "\uE945",
                AvailableCardsCount: Math.Max(dueCount, 25)
            ),
            new SmartReviewPresetInfo(
                Mode: SmartReviewPresetMode.LeechRescue,
                TitleVi: "Leech Rescue (Giải cứu từ khó)",
                TitleEn: "Leech & Difficult Word Rescue",
                DescriptionVi: "Tập trung giải cứu các từ hay quên (Lapses ≥ 1) bằng gợi ý chuyên sâu.",
                DescriptionEn: "Rescue stubborn words with past lapses and low ease factors.",
                IconGlyph: "\uE7BA",
                AvailableCardsCount: difficult?.Count ?? 0
            ),
            new SmartReviewPresetInfo(
                Mode: SmartReviewPresetMode.HighYieldExam,
                TitleVi: "Trọng tâm Đề thi (High-Yield)",
                TitleEn: "High-Yield Exam Focus",
                DescriptionVi: "Ưu tiên ôn luyện từ vựng xuất hiện nhiều trong TOEIC / IELTS / VSTEP.",
                DescriptionEn: "Focus exclusively on frequent exam-tagged vocabulary.",
                IconGlyph: "\uE7BE",
                AvailableCardsCount: Math.Min(30, dueCount + (newCards?.Count ?? 0))
            ),
            new SmartReviewPresetInfo(
                Mode: SmartReviewPresetMode.ContextImmersion,
                TitleVi: "Đắm chìm Ngữ cảnh (Context)",
                TitleEn: "Contextual Immersion",
                DescriptionVi: "Luyện tập kèm câu ví dụ mẫu, cụm từ kết hợp và nghe phát âm.",
                DescriptionEn: "Practice words accompanied by authentic example sentences.",
                IconGlyph: "\uE8D2",
                AvailableCardsCount: Math.Min(30, dueCount + 10)
            )
        };

        return presets.Where(p => p != null).ToList();
    }

    public async Task<IReadOnlyList<ReviewCardDto>> GetPresetQueueAsync(SmartReviewPresetMode mode, int limit = 30, CancellationToken cancellationToken = default)
    {
        switch (mode)
        {
            case SmartReviewPresetMode.UrgentOverdue:
            case SmartReviewPresetMode.StandardDue:
            {
                var due = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, limit, cancellationToken);
                return due.Select(MapToDto).ToList();
            }
            case SmartReviewPresetMode.HighForgettingRisk:
            case SmartReviewPresetMode.LeechRescue:
            {
                var difficult = await _reviewRepo.GetDifficultCardsAsync(limit, cancellationToken);
                return difficult.Select(MapToDto).ToList();
            }
            case SmartReviewPresetMode.ExamHighYield:
            case SmartReviewPresetMode.HighYieldExam:
            {
                var allDue = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, limit * 2, cancellationToken);
                var examCards = allDue.Where(c => c.Word?.WordExamTags != null && c.Word.WordExamTags.Count > 0).Take(limit).ToList();
                if (examCards.Count > 0) return examCards.Select(MapToDto).ToList();
                return allDue.Take(limit).Select(MapToDto).ToList();
            }
            case SmartReviewPresetMode.FreshLearned:
            case SmartReviewPresetMode.SpeedBlitz:
            {
                var due = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, limit, cancellationToken);
                if (due.Count >= 5) return due.Select(MapToDto).ToList();
                return await GetBalancedReviewQueueAsync(limit, cancellationToken);
            }
            case SmartReviewPresetMode.LongTermConsolidation:
            case SmartReviewPresetMode.ContextImmersion:
            {
                var allDue = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, limit * 2, cancellationToken);
                var withExamples = allDue.Where(c => c.Word?.Meanings != null && c.Word.Meanings.Any(m => m.Examples.Count > 0)).Take(limit).ToList();
                if (withExamples.Count > 0) return withExamples.Select(MapToDto).ToList();
                return allDue.Take(limit).Select(MapToDto).ToList();
            }
            default:
            {
                var due = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, limit, cancellationToken);
                return due.Select(MapToDto).ToList();
            }
        }
    }

    public async Task<IReadOnlyList<StudySessionDto>> GetRecentSessionsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var sessions = await _reviewRepo.GetRecentSessionsAsync(limit, cancellationToken);
        return sessions.Select(MapToSessionDto).ToList();
    }

    public async Task<IReadOnlyList<StudyLogDto>> GetRecentLogsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var logs = await _reviewRepo.GetRecentLogsAsync(limit, cancellationToken);
        return logs.Select(l => new StudyLogDto(
            l.Id,
            l.ReviewCardId,
            l.ReviewCard?.Word?.Term ?? "Word",
            l.Rating,
            l.ResponseTimeMs,
            l.ResultingStatus,
            l.ResultingIntervalDays,
            l.CreatedAt
        )).ToList();
    }

    private static StudySessionDto MapToSessionDto(StudySession ss)
    {
        double accuracy = ss.TotalCards == 0 ? 0.0 : Math.Round((double)ss.CorrectCards / ss.TotalCards * 100, 1);
        return new StudySessionDto(
            ss.Id,
            ss.StartedAt,
            ss.CompletedAt,
            ss.TotalCards,
            ss.CorrectCards,
            ss.IncorrectCards,
            accuracy,
            ss.SessionType
        );
    }

    private ReviewCardDto MapToDto(ReviewCard c)
    {
        var word = c.Word;
        var primaryMeaning = word?.Meanings?.OrderBy(m => m.OrderIndex).FirstOrDefault();
        var examples = (primaryMeaning?.Examples ?? Enumerable.Empty<ExampleSentence>())
            .Select(e => new ExampleSentenceDto(e.Id, e.SentenceEn, e.SentenceVi, e.AudioPath))
            .ToList();

        double confidence = c.Confidence > 0.0
            ? c.Confidence
            : (c.TotalReviews > 0
                ? Math.Round((double)c.CorrectReviews / c.TotalReviews * 100, 1)
                : GetActiveEngine().engine.CalculateRetentionProbability(c));

        return new ReviewCardDto(
            c.Id,
            c.WordId,
            word?.Term ?? string.Empty,
            word?.PhoneticUk,
            word?.PhoneticUs,
            word?.AudioUkPath,
            word?.AudioUsPath,
            word?.PrimaryPartOfSpeech ?? PartOfSpeech.Noun,
            word?.Level ?? CefrLevel.None,
            primaryMeaning?.DefinitionVi ?? string.Empty,
            primaryMeaning?.DefinitionEn,
            examples,
            c.Status,
            c.EaseFactor,
            c.IntervalDays,
            c.Repetitions,
            c.Lapses,
            c.LastReviewedAt,
            c.NextReviewAt,
            c.TotalReviews,
            c.CorrectReviews,
            confidence
        );
    }
}
