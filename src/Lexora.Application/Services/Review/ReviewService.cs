using Lexora.Application.Common;
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
    private readonly IRoadmapRepository? _roadmapRepo;

    public ReviewService(
        IReviewRepository reviewRepo,
        ISpacedRepetitionEngine srEngine,
        IUnitOfWork unitOfWork,
        ILearningStorageRepository? storageRepo = null,
        IFsrsSpacedRepetitionEngine? fsrsEngine = null,
        IUserPreferencesService? preferencesService = null,
        IRoadmapRepository? roadmapRepo = null)
    {
        _reviewRepo = reviewRepo;
        _srEngine = srEngine;
        _unitOfWork = unitOfWork;
        _storageRepo = storageRepo;
        _fsrsEngine = fsrsEngine;
        _preferencesService = preferencesService;
        _roadmapRepo = roadmapRepo;
    }

    private async Task<ExamTarget?> GetActiveExamTargetAsync(CancellationToken cancellationToken)
    {
        if (_roadmapRepo == null) return null;
        try
        {
            var roadmap = await _roadmapRepo.GetActiveRoadmapWithDetailsAsync(cancellationToken);
            return roadmap?.Target;
        }
        catch
        {
            return null;
        }
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

        // 1. Fetch Due Cards (prioritized by FSRS Retrievability / Urgency + ExamTarget boost)
        var activeExam = await GetActiveExamTargetAsync(cancellationToken);
        var dueCards = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, dueQuota * 2, cancellationToken);
        var orderedDue = _fsrsEngine != null
            ? dueCards.OrderByDescending(c => _fsrsEngine.CalculatePriorityScore(c, DateTime.UtcNow, activeExam)).ToList()
            : dueCards.ToList();
        foreach (var c in orderedDue)
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

        return InterleaveCards(result);
    }

    private static IReadOnlyList<ReviewCardDto> InterleaveCards(List<ReviewCardDto> cards)
    {
        if (cards.Count <= 2) return cards;

        var remaining = new List<ReviewCardDto>(cards);
        var interleaved = new List<ReviewCardDto>(cards.Count);

        while (remaining.Count > 0)
        {
            int pickIndex = 0;
            if (interleaved.Count > 0)
            {
                var prev = interleaved[^1];
                for (int i = 0; i < remaining.Count; i++)
                {
                    if (remaining[i].PrimaryPartOfSpeech != prev.PrimaryPartOfSpeech)
                    {
                        pickIndex = i;
                        break;
                    }
                }
            }

            interleaved.Add(remaining[pickIndex]);
            remaining.RemoveAt(pickIndex);
        }

        return interleaved;
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
        bool usedHint = false,
        CancellationToken cancellationToken = default)
    {
        var card = await _reviewRepo.GetByIdAsync(reviewCardId, cancellationToken);
        if (card == null)
            throw new KeyNotFoundException($"ReviewCard with ID {reviewCardId} not found");

        var prevStatus = card.Status;
        var (engine, desiredRetention, isFsrs) = GetActiveEngine();

        // Response-time & hint guardrail: prevent interval inflation when Easy is clicked after slow recall (> 10s) or using a hint
        var effectiveRating = (rating == ReviewRating.Easy && (usedHint || responseTimeMs > 10_000))
            ? ReviewRating.Good
            : rating;

        var nowUtc = DateTime.UtcNow;
        SpacedRepetitionResult result;
        if (isFsrs && _fsrsEngine != null)
        {
            var fsrsState = _fsrsEngine.CalculateFsrsNextReview(card, effectiveRating, nowUtc, desiredRetention);
            int balancedInterval = fsrsState.IntervalDays;
            DateTime balancedNextReview = fsrsState.NextReviewAt;

            if (balancedInterval >= 4)
            {
                var forecastStart = nowUtc.Date.AddDays(Math.Max(1, balancedInterval - 3));
                var forecast = await _reviewRepo.GetDueForecastCountsAsync(forecastStart, 7, cancellationToken);
                balancedInterval = _fsrsEngine.ApplyWorkloadFuzz(balancedInterval, forecast, nowUtc);
                balancedNextReview = nowUtc.Date.AddDays(balancedInterval);
            }

            double mappedEaseFactor = Math.Clamp(1.3 + (10.0 - fsrsState.Difficulty) * 0.15, 1.3, 3.0);
            mappedEaseFactor = Math.Round(mappedEaseFactor, 2);

            card.Stability = fsrsState.Stability;
            card.Difficulty = fsrsState.Difficulty;
            card.DesiredRetention = desiredRetention;

            result = new SpacedRepetitionResult(
                NewEaseFactor: mappedEaseFactor,
                NewIntervalDays: balancedInterval,
                NewRepetitions: fsrsState.Repetitions,
                NewLapses: fsrsState.Lapses,
                NewStatus: fsrsState.Status,
                NextReviewAt: balancedNextReview
            );
        }
        else
        {
            if (_fsrsEngine != null)
            {
                var fsrsState = _fsrsEngine.CalculateFsrsNextReview(card, effectiveRating, nowUtc, desiredRetention);
                card.Stability = fsrsState.Stability;
                card.Difficulty = fsrsState.Difficulty;
            }
            result = _srEngine.CalculateNextReview(card, effectiveRating, nowUtc);
        }

        // Apply algorithm results (enforcing EF >= 1.3 invariant)
        if (card.LastReviewedAt.HasValue)
        {
            card.LastElapsedDays = Math.Round(Math.Max(0.0, (nowUtc - card.LastReviewedAt.Value).TotalDays), 2);
        }
        if (responseTimeMs > 0)
        {
            card.AverageResponseTimeMs = card.AverageResponseTimeMs <= 0
                ? responseTimeMs
                : (int)Math.Round(card.AverageResponseTimeMs * 0.7 + responseTimeMs * 0.3);
        }

        card.EaseFactor = Math.Max(1.3, result.NewEaseFactor);
        card.IntervalDays = result.NewIntervalDays;
        card.Repetitions = result.NewRepetitions;
        card.Lapses = result.NewLapses;
        card.Status = result.NewStatus;
        card.NextReviewAt = result.NextReviewAt;
        card.LastReviewedAt = nowUtc;
        card.TotalReviews++;
        if (rating >= ReviewRating.Good)
        {
            card.CorrectReviews++;
        }
        card.IsLeech = _fsrsEngine != null
            ? _fsrsEngine.IsLeechCard(card)
            : (card.Lapses >= 6 || (card.Lapses >= 4 && card.TotalReviews > 0 && card.Confidence < 40.0));
        card.UpdatedAt = nowUtc;

        // Synchronize parent word timestamp
        if (card.Word != null)
        {
            card.Word.UpdatedAt = DateTime.UtcNow;
        }

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
            card.NextReviewAt,
            card.IsLeech,
            card.Stability
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
        double? previousStability = null,
        double? previousDifficulty = null,
        DateTime? previousLastReviewedAt = null,
        double? previousLastElapsedDays = null,
        bool? previousIsLeech = null,
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
                        existingStorage.Status = previousStatus switch
                        {
                            LearningStatus.Mastered => StorageLearningStatus.Mastered,
                            LearningStatus.Remembered => StorageLearningStatus.Learning,
                            _ => StorageLearningStatus.Weak
                        };
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
        if (previousStability.HasValue) card.Stability = previousStability.Value;
        if (previousDifficulty.HasValue) card.Difficulty = previousDifficulty.Value;
        if (previousLastReviewedAt.HasValue) card.LastReviewedAt = previousLastReviewedAt.Value;
        if (previousLastElapsedDays.HasValue) card.LastElapsedDays = previousLastElapsedDays.Value;
        if (previousIsLeech.HasValue) card.IsLeech = previousIsLeech.Value;
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
            if (remembered)
            {
                card.CorrectReviews++;
            }
            else
            {
                card.IntervalDays = 1;
                card.Repetitions = 0;
                card.Lapses++;
            }
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

    private static ReviewCard BuildShadowCard(ReviewCardDto card, double desiredRetention) => new()
    {
        Id = card.Id,
        WordId = card.WordId,
        Status = card.Status,
        EaseFactor = card.EaseFactor,
        IntervalDays = card.IntervalDays,
        Repetitions = card.Repetitions,
        Lapses = card.Lapses,
        Stability = card.Stability,
        Difficulty = card.Difficulty,
        IsLeech = card.IsLeech,
        DesiredRetention = desiredRetention,
        LastReviewedAt = card.LastReviewedAt,
        NextReviewAt = card.NextReviewAt,
        TotalReviews = card.TotalReviews,
        CorrectReviews = card.CorrectReviews
    };

    public IntervalPreviewDto GetCardIntervalPreview(ReviewCardDto card)
    {
        var (engine, desiredRetention, isFsrs) = GetActiveEngine();
        var dummyCard = BuildShadowCard(card, desiredRetention);

        if (isFsrs && _fsrsEngine != null)
        {
            return _fsrsEngine.GetIntervalPreview(dummyCard, DateTime.UtcNow, desiredRetention);
        }

        return engine.GetIntervalPreview(dummyCard);
    }

    public double GetRetentionProbability(ReviewCardDto card)
    {
        var (engine, desiredRetention, _) = GetActiveEngine();
        var dummyCard = BuildShadowCard(card, desiredRetention);
        return engine.CalculateRetentionProbability(dummyCard);
    }

    public MemoryStabilityMetricsDto GetMemoryStabilityMetrics(ReviewCardDto card)
    {
        double retrievability = GetRetentionProbability(card);
        double stabilityDays = card.Stability > 0.01
            ? card.Stability
            : Math.Max(1.0, card.IntervalDays * (card.EaseFactor / 2.5));
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

    public ReviewRating? SuggestRatingByResponseTime(ReviewCardDto card, int responseTimeMs, bool usedHint, bool? isTypingCorrect)
    {
        if (isTypingCorrect == false)
        {
            return ReviewRating.Again;
        }

        if (usedHint)
        {
            return ReviewRating.Hard;
        }

        if (responseTimeMs <= 0)
        {
            return null;
        }

        if (responseTimeMs < 1800 && card.Lapses == 0)
        {
            return ReviewRating.Easy;
        }

        if (responseTimeMs > 6500)
        {
            return ReviewRating.Hard;
        }

        return ReviewRating.Good;
    }

    public async Task<ReviewAnalyticsSummaryDto> GetReviewAnalyticsSummaryAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var sinceUtc = DateTime.UtcNow.AddDays(-Math.Max(1, days));
        var logs = await _reviewRepo.GetLogsSinceAsync(sinceUtc, cancellationToken);
        var difficult = await _reviewRepo.GetDifficultCardsAsync(100, cancellationToken);
        var (_, desiredRetention, isFsrs) = GetActiveEngine();

        int total = logs.Count;
        int passed = logs.Count(l => l.Rating >= ReviewRating.Good);
        double trueRetention = total == 0 ? 100.0 : Math.Round((double)passed / total * 100.0, 1);
        double targetPct = Math.Round(desiredRetention * 100.0, 1);

        var timedLogs = logs.Where(l => l.ResponseTimeMs > 0 && l.ResponseTimeMs < 60_000).ToList();
        int avgResponseMs = timedLogs.Count == 0 ? 0 : (int)Math.Round(timedLogs.Average(l => l.ResponseTimeMs));

        int leechCount = difficult.Count(c => c.IsLeech || (_fsrsEngine != null ? _fsrsEngine.IsLeechCard(c) : c.Lapses >= 6));

        double recommendedRetention = targetPct;
        string adviceKey = "Review_Calibration_Optimal";

        if (total >= 15)
        {
            double delta = trueRetention - targetPct;
            if (delta < -6.0)
            {
                recommendedRetention = Math.Min(95.0, targetPct + 3.0);
                adviceKey = "Review_Calibration_RaiseRetention";
            }
            else if (delta > 6.0 && targetPct > 85.0)
            {
                recommendedRetention = Math.Max(85.0, targetPct - 2.0);
                adviceKey = "Review_Calibration_LowerRetention";
            }
        }

        return new ReviewAnalyticsSummaryDto(
            TrueRetentionPercent: trueRetention,
            TargetRetentionPercent: targetPct,
            RecommendedRetentionPercent: recommendedRetention,
            TotalReviewsInPeriod: total,
            LeechCount: leechCount,
            AverageResponseTimeMs: avgResponseMs,
            AlgorithmName: isFsrs ? "FSRS v5" : "SM-2",
            CalibrationAdviceKey: adviceKey
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
        var now = DateTime.UtcNow;
        var activeExam = await GetActiveExamTargetAsync(cancellationToken);

        switch (mode)
        {
            case SmartReviewPresetMode.UrgentOverdue:
            case SmartReviewPresetMode.StandardDue:
            {
                var due = await _reviewRepo.GetDueCardsAsync(now, limit * 2, cancellationToken);
                var ordered = _fsrsEngine != null
                    ? due.OrderByDescending(c => _fsrsEngine.CalculatePriorityScore(c, now, activeExam)).Take(limit)
                    : due.Take(limit);
                return InterleaveCards(ordered.Select(MapToDto).ToList());
            }
            case SmartReviewPresetMode.HighForgettingRisk:
            case SmartReviewPresetMode.LeechRescue:
            {
                var difficult = await _reviewRepo.GetDifficultCardsAsync(limit, cancellationToken);
                var ordered = difficult
                    .OrderByDescending(c => c.IsLeech || (_fsrsEngine != null && _fsrsEngine.IsLeechCard(c)))
                    .ThenByDescending(c => c.Lapses)
                    .ThenByDescending(c => c.Difficulty);
                return ordered.Select(MapToDto).ToList();
            }
            case SmartReviewPresetMode.ExamHighYield:
            case SmartReviewPresetMode.HighYieldExam:
            {
                var allDue = await _reviewRepo.GetDueCardsAsync(now, limit * 2, cancellationToken);
                var examCards = allDue
                    .Where(c => c.Word?.WordExamTags != null && c.Word.WordExamTags.Count > 0)
                    .OrderByDescending(c => _fsrsEngine != null ? _fsrsEngine.CalculatePriorityScore(c, now, activeExam) : 0)
                    .Take(limit)
                    .ToList();
                if (examCards.Count > 0) return examCards.Select(MapToDto).ToList();
                return allDue.Take(limit).Select(MapToDto).ToList();
            }
            case SmartReviewPresetMode.FreshLearned:
            case SmartReviewPresetMode.SpeedBlitz:
            {
                var due = await _reviewRepo.GetDueCardsAsync(now, limit * 2, cancellationToken);
                var blitzCandidates = due
                    .OrderByDescending(c => c.Status is LearningStatus.Remembered or LearningStatus.Mastered)
                    .ThenByDescending(c => c.Stability)
                    .Take(limit)
                    .ToList();
                if (blitzCandidates.Count >= 5) return blitzCandidates.Select(MapToDto).ToList();
                return await GetBalancedReviewQueueAsync(limit, cancellationToken);
            }
            case SmartReviewPresetMode.LongTermConsolidation:
            case SmartReviewPresetMode.ContextImmersion:
            {
                var allDue = await _reviewRepo.GetDueCardsAsync(now, limit * 2, cancellationToken);
                var withExamples = allDue
                    .Where(c => c.Word?.Meanings != null && c.Word.Meanings.Any(m => m.Examples.Count > 0))
                    .OrderByDescending(c => _fsrsEngine != null ? _fsrsEngine.CalculatePriorityScore(c, now, activeExam) : 0)
                    .Take(limit)
                    .ToList();
                if (withExamples.Count > 0) return withExamples.Select(MapToDto).ToList();
                return allDue.Take(limit).Select(MapToDto).ToList();
            }
            default:
            {
                var due = await _reviewRepo.GetDueCardsAsync(now, limit, cancellationToken);
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

    public async Task<IReadOnlyList<DailyWorkloadPointDto>> Get14DayWorkloadSimulationAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var forecast = await _reviewRepo.GetDueForecastCountsAsync(now, 14, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rawPoints = new List<(DateOnly Date, string Label, int Count)>(14);
        int maxCount = 0;

        for (int i = 0; i < 14; i++)
        {
            var date = today.AddDays(i);
            int count = forecast.GetValueOrDefault(date, 0);
            if (count > maxCount) maxCount = count;
            string label = i == 0 ? "Today" : date.ToString("dd/MM");
            rawPoints.Add((date, label, count));
        }

        return rawPoints
            .Select(p => new DailyWorkloadPointDto(p.Date, p.Label, p.Count, maxCount > 0 && p.Count == maxCount))
            .ToList();
    }

    public async Task<IReadOnlyList<ReviewCardDto>> GetLeechCardsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var difficult = await _reviewRepo.GetDifficultCardsAsync(Math.Max(limit * 2, 40), cancellationToken);
        return difficult
            .Where(c => c.IsLeech || (_fsrsEngine != null ? _fsrsEngine.IsLeechCard(c) : c.Lapses >= 6))
            .Take(limit)
            .Select(MapToDto)
            .ToList();
    }

    private ReviewCardDto MapToDto(ReviewCard c) => c.ToDto(GetActiveEngine().engine);
}
