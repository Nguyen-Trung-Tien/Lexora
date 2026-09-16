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

    public ReviewService(
        IReviewRepository reviewRepo,
        ISpacedRepetitionEngine srEngine,
        IUnitOfWork unitOfWork)
    {
        _reviewRepo = reviewRepo;
        _srEngine = srEngine;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ReviewCardDto>> GetDueCardsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var cards = await _reviewRepo.GetDueCardsAsync(DateTime.UtcNow, limit, cancellationToken);
        return cards.Select(MapToDto).ToList();
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
        var result = _srEngine.CalculateNextReview(card, rating, DateTime.UtcNow);

        // Apply SM-2 results
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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

        return _srEngine.GetIntervalPreview(dummyCard);
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

        return _srEngine.CalculateRetentionProbability(dummyCard);
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
                : _srEngine.CalculateRetentionProbability(c));

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
