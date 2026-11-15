using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services.Phrases;

public partial class ConversationPhraseService
{
    private const double MinSm2EaseFactor = 1.3;

    public async Task<IReadOnlyList<PhraseRoleplayCardDto>> GetRoleplayQueueAsync(
        ConversationPhraseFilterDto? filter = null,
        int limit = 20,
        bool useFsrs = false,
        CancellationToken cancellationToken = default)
    {
        int cappedLimit = Math.Clamp(limit, 1, 200);
        var now = DateTime.UtcNow;
        var candidates = await FetchFilteredEntitiesAsync(filter, filter?.SearchQuery, cancellationToken);

        var ordered = candidates
            .OrderByDescending(p => p.NextReviewAt.HasValue && p.NextReviewAt.Value <= now && p.Status != LearningStatus.Mastered)
            .ThenByDescending(p => p.Status == LearningStatus.NotRemembered || p.Status == LearningStatus.Learning)
            .ThenByDescending(p => p.IsFavorite)
            .ThenBy(p => p.NextReviewAt ?? DateTime.MaxValue)
            .ThenBy(p => p.LastReviewedAt ?? DateTime.MinValue)
            .Take(cappedLimit)
            .ToList();

        var engine = ResolveSchedulerEngine(useFsrs);
        var queue = new List<PhraseRoleplayCardDto>(ordered.Count);

        foreach (var phrase in ordered)
        {
            var virtualCard = BuildVirtualReviewCard(phrase);
            var previews = engine.GetIntervalPreview(virtualCard, now);

            queue.Add(new PhraseRoleplayCardDto
            {
                Phrase = phrase.ToDto(),
                KeyPhraseTokens = ParseKeyPhrases(phrase),
                IntervalAgainPreview = previews.AgainText,
                IntervalHardPreview = previews.HardText,
                IntervalGoodPreview = previews.GoodText,
                IntervalEasyPreview = previews.EasyText
            });
        }

        return queue;
    }

    public async Task<PhraseReviewResultDto> SubmitPhraseReviewAsync(
        Guid phraseId,
        ReviewRating rating,
        int responseTimeMs = 0,
        bool useFsrs = false,
        CancellationToken cancellationToken = default)
    {
        var phrase = await repository.GetByIdAsync(phraseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation phrase with ID {phraseId} was not found.");

        var now = DateTime.UtcNow;
        var previousStatus = phrase.Status;
        var engine = ResolveSchedulerEngine(useFsrs);
        var virtualCard = BuildVirtualReviewCard(phrase);
        var scheduled = engine.CalculateNextReview(virtualCard, rating, now);

        // Enforce strict SM-2 minimum Ease Factor invariant: EF >= 1.3
        double safeEaseFactor = Math.Max(MinSm2EaseFactor, scheduled.NewEaseFactor);

        phrase.Status = scheduled.NewStatus;
        phrase.EaseFactor = safeEaseFactor;
        phrase.IntervalDays = scheduled.NewIntervalDays;
        phrase.Repetitions = scheduled.NewRepetitions;
        phrase.Lapses = scheduled.NewLapses;
        phrase.LastReviewedAt = now;
        phrase.NextReviewAt = scheduled.NextReviewAt;
        phrase.UpdatedAt = now;
        phrase.UpdateNormalizedFields();

        bool syncedStudyLog = false;
        if (reviewRepository is not null && vocabularyRepository is not null)
        {
            syncedStudyLog = await SyncLinkedReviewCardAndStudyLogAsync(
                phrase,
                scheduled,
                safeEaseFactor,
                rating,
                responseTimeMs,
                now,
                cancellationToken);
        }

        await repository.UpdateAsync(phrase, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PhraseReviewResultDto(
            PhraseId: phrase.Id,
            Rating: rating,
            PreviousStatus: previousStatus,
            NewStatus: phrase.Status,
            NewEaseFactor: phrase.EaseFactor,
            NewIntervalDays: phrase.IntervalDays,
            Repetitions: phrase.Repetitions,
            Lapses: phrase.Lapses,
            NextReviewAt: phrase.NextReviewAt ?? now.AddDays(1),
            SyncedStudyLog: syncedStudyLog);
    }

    public async Task<ConversationPhraseDto> EnqueueForSpacedRepetitionAsync(
        Guid phraseId,
        CancellationToken cancellationToken = default)
    {
        var phrase = await repository.GetByIdAsync(phraseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation phrase with ID {phraseId} was not found.");

        var now = DateTime.UtcNow;
        phrase.EaseFactor = Math.Max(MinSm2EaseFactor, phrase.EaseFactor);
        phrase.NextReviewAt ??= now;
        if (phrase.Status == LearningStatus.New)
        {
            phrase.Status = LearningStatus.Learning;
        }
        phrase.UpdatedAt = now;
        phrase.UpdateNormalizedFields();

        if (reviewRepository is not null && vocabularyRepository is not null)
        {
            await EnsureLinkedReviewCardAsync(phrase, now, cancellationToken);
        }

        await repository.UpdateAsync(phrase, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return phrase.ToDto();
    }

    private ISpacedRepetitionEngine ResolveSchedulerEngine(bool useFsrs)
    {
        if (useFsrs && fsrsEngine is not null)
        {
            return fsrsEngine;
        }

        return spacedRepetitionEngine ?? new Sm2SpacedRepetitionEngine();
    }

    private static ReviewCard BuildVirtualReviewCard(ConversationPhrase phrase) => new()
    {
        WordId = phrase.LinkedWordId ?? phrase.Id,
        Status = phrase.Status,
        EaseFactor = Math.Max(MinSm2EaseFactor, phrase.EaseFactor),
        IntervalDays = phrase.IntervalDays,
        Repetitions = phrase.Repetitions,
        Lapses = phrase.Lapses,
        LastReviewedAt = phrase.LastReviewedAt,
        NextReviewAt = phrase.NextReviewAt ?? DateTime.UtcNow
    };

    private async Task<bool> SyncLinkedReviewCardAndStudyLogAsync(
        ConversationPhrase phrase,
        SpacedRepetitionResult scheduled,
        double safeEaseFactor,
        ReviewRating rating,
        int responseTimeMs,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var card = await EnsureLinkedReviewCardAsync(phrase, now, cancellationToken);
        card.Status = scheduled.NewStatus;
        card.EaseFactor = safeEaseFactor;
        card.IntervalDays = scheduled.NewIntervalDays;
        card.Repetitions = scheduled.NewRepetitions;
        card.Lapses = scheduled.NewLapses;
        card.LastReviewedAt = now;
        card.NextReviewAt = scheduled.NextReviewAt;
        card.TotalReviews++;
        if (rating >= ReviewRating.Hard)
        {
            card.CorrectReviews++;
        }

        reviewRepository!.AddStudyLog(new StudyLog
        {
            ReviewCardId = card.Id,
            Rating = rating,
            ResponseTimeMs = Math.Max(0, responseTimeMs),
            ResultingStatus = scheduled.NewStatus,
            ResultingIntervalDays = scheduled.NewIntervalDays,
            CreatedAt = now
        });

        await reviewRepository.UpdateStreakAsync(DateOnly.FromDateTime(now), cancellationToken);
        return true;
    }

    private async Task<ReviewCard> EnsureLinkedReviewCardAsync(
        ConversationPhrase phrase,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (phrase.LinkedWordId.HasValue)
        {
            var existingCard = await reviewRepository!.GetByWordIdAsync(phrase.LinkedWordId.Value, cancellationToken);
            if (existingCard is not null)
            {
                return existingCard;
            }
        }

        var marker = BuildUniquePhraseMarkerTerm(phrase);
        var word = await vocabularyRepository!.GetByTermAsync(marker, cancellationToken);
        if (word is null)
        {
            var displayTerm = phrase.PromptSentence.Length > 120
                ? $"{phrase.PromptSentence[..117]}..."
                : phrase.PromptSentence;

            word = new Word
            {
                Term = displayTerm,
                NormalizedTerm = marker,
                Level = phrase.CefrLevel,
                PrimaryPartOfSpeech = PartOfSpeech.Phrase,
                Notes = $"Roleplay Response: {phrase.SampleResponse}"
            };

            word.Meanings.Add(new WordMeaning
            {
                WordId = word.Id,
                Word = word,
                PartOfSpeech = PartOfSpeech.Phrase,
                DefinitionVi = string.IsNullOrWhiteSpace(phrase.ResponseTranslation)
                    ? phrase.SampleResponse
                    : phrase.ResponseTranslation,
                DefinitionEn = phrase.SampleResponse,
                OrderIndex = 0
            });

            var newCard = new ReviewCard
            {
                WordId = word.Id,
                Word = word,
                Status = phrase.Status,
                EaseFactor = Math.Max(MinSm2EaseFactor, phrase.EaseFactor),
                IntervalDays = phrase.IntervalDays,
                Repetitions = phrase.Repetitions,
                Lapses = phrase.Lapses,
                LastReviewedAt = phrase.LastReviewedAt,
                NextReviewAt = phrase.NextReviewAt ?? now
            };

            word.ReviewCard = newCard;
            await vocabularyRepository.AddAsync(word, cancellationToken);
            phrase.LinkedWordId = word.Id;
            return newCard;
        }

        phrase.LinkedWordId = word.Id;
        if (word.ReviewCard is not null)
        {
            return word.ReviewCard;
        }

        var attachedCard = new ReviewCard
        {
            WordId = word.Id,
            Word = word,
            Status = phrase.Status,
            EaseFactor = Math.Max(MinSm2EaseFactor, phrase.EaseFactor),
            IntervalDays = phrase.IntervalDays,
            Repetitions = phrase.Repetitions,
            Lapses = phrase.Lapses,
            LastReviewedAt = phrase.LastReviewedAt,
            NextReviewAt = phrase.NextReviewAt ?? now
        };
        await reviewRepository!.AddAsync(attachedCard, cancellationToken);
        return attachedCard;
    }

    private static string BuildUniquePhraseMarkerTerm(ConversationPhrase phrase)
    {
        var shortId = phrase.Id.ToString("N")[..8];
        var basePrompt = string.IsNullOrWhiteSpace(phrase.NormalizedPrompt)
            ? phrase.PromptSentence.Trim().ToLowerInvariant()
            : phrase.NormalizedPrompt;
        if (basePrompt.Length > 100)
        {
            basePrompt = basePrompt[..100];
        }
        return $"[phrase:{shortId}] {basePrompt}";
    }

    private static IReadOnlyList<string> ParseKeyPhrases(ConversationPhrase phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase.KeyPhrases))
        {
            return [];
        }

        return phrase.KeyPhrases
            .Split([';', '|', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
