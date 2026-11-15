using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class PhrasesViewModel
{
    private readonly Stopwatch _roleplayCardTimer = new();

    public ObservableCollection<PhraseRoleplayCardDto> RoleplayQueue { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRoleplayCard))]
    [NotifyPropertyChangedFor(nameof(RoleplayProgressText))]
    public partial bool IsRoleplayMode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRoleplayCard))]
    [NotifyPropertyChangedFor(nameof(RoleplayCompletedVisible))]
    [NotifyPropertyChangedFor(nameof(RoleplayProgressText))]
    [NotifyPropertyChangedFor(nameof(HasCurrentKeyPhrases))]
    public partial PhraseRoleplayCardDto? CurrentRoleplayCard { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoleplayProgressText))]
    public partial int CurrentRoleplayIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoleplayProgressText))]
    [NotifyPropertyChangedFor(nameof(RoleplayCompletedVisible))]
    public partial int RoleplayTotalCount { get; set; }

    [ObservableProperty]
    public partial int RoleplayCompletedCount { get; set; }

    [ObservableProperty]
    public partial bool IsRoleplayAnswerRevealed { get; set; }

    [ObservableProperty]
    public partial string UserPracticeDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool UseFsrsScheduler { get; set; }

    public bool HasRoleplayCard => IsRoleplayMode && CurrentRoleplayCard is not null;
    public bool RoleplayCompletedVisible => IsRoleplayMode && RoleplayTotalCount > 0 && CurrentRoleplayCard is null;
    public bool HasCurrentKeyPhrases => (CurrentRoleplayCard?.KeyPhraseTokens.Count ?? 0) > 0;
    public string RoleplayProgressText => RoleplayTotalCount == 0
        ? "0 / 0"
        : $"{Math.Min(CurrentRoleplayIndex + 1, RoleplayTotalCount)} / {RoleplayTotalCount}";

    [RelayCommand]
    public async Task StartRoleplayAsync()
    {
        if (IsSubmitting || _disposed) return;

        IsSubmitting = true;
        StopAudio();

        try
        {
            var queue = await phraseService.GetRoleplayQueueAsync(
                BuildCurrentFilter(),
                limit: 20,
                useFsrs: UseFsrsScheduler);

            RoleplayQueue.Clear();
            foreach (var card in queue)
            {
                RoleplayQueue.Add(card);
            }

            RoleplayTotalCount = RoleplayQueue.Count;
            RoleplayCompletedCount = 0;
            CurrentRoleplayIndex = 0;
            IsRoleplayAnswerRevealed = false;
            UserPracticeDraft = string.Empty;

            if (RoleplayTotalCount == 0)
            {
                CurrentRoleplayCard = null;
                IsRoleplayMode = false;
                SetStatus(localizationService.GetString("Phrases_RoleplayEmpty"), isError: false);
                return;
            }

            CurrentRoleplayCard = RoleplayQueue[0];
            IsRoleplayMode = true;
            _roleplayCardTimer.Restart();
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorLoad")}: {ex.Message}", isError: true);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public async Task ExitRoleplayAsync()
    {
        StopAudio();
        _roleplayCardTimer.Stop();
        IsRoleplayMode = false;
        CurrentRoleplayCard = null;
        IsRoleplayAnswerRevealed = false;
        UserPracticeDraft = string.Empty;
        await RefreshPhrasesAsync(resetPage: false);
    }

    [RelayCommand]
    public void RevealRoleplayAnswer()
    {
        IsRoleplayAnswerRevealed = true;
    }

    [RelayCommand]
    public async Task PlayRoleplayPromptAsync()
    {
        if (CurrentRoleplayCard?.Phrase is { } phrase)
        {
            await PlayPromptAudioAsync(phrase);
        }
    }

    [RelayCommand]
    public async Task PlayRoleplayResponseAsync()
    {
        if (CurrentRoleplayCard?.Phrase is { } phrase)
        {
            await PlayResponseAudioAsync(phrase);
        }
    }

    [RelayCommand]
    public Task RateRoleplayAgainAsync() => RateCurrentRoleplayAsync(ReviewRating.Again);

    [RelayCommand]
    public Task RateRoleplayHardAsync() => RateCurrentRoleplayAsync(ReviewRating.Hard);

    [RelayCommand]
    public Task RateRoleplayGoodAsync() => RateCurrentRoleplayAsync(ReviewRating.Good);

    [RelayCommand]
    public Task RateRoleplayEasyAsync() => RateCurrentRoleplayAsync(ReviewRating.Easy);

    [RelayCommand]
    public async Task EnqueuePhraseForReviewAsync(ConversationPhraseDto? phrase)
    {
        if (phrase is null || IsSubmitting) return;

        IsSubmitting = true;
        try
        {
            var updated = await phraseService.EnqueueForSpacedRepetitionAsync(phrase.Id);
            phrase.UpdateFrom(updated);
            SyncItemInCollections(updated);
            UpdateSummaryCounters();
            SetStatus(localizationService.GetString("Phrases_EnqueuedReview"), isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorSave")}: {ex.Message}", isError: true);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task RateCurrentRoleplayAsync(ReviewRating rating)
    {
        if (CurrentRoleplayCard is null || IsSubmitting) return;

        IsSubmitting = true;
        StopAudio();

        try
        {
            int elapsedMs = (int)Math.Clamp(_roleplayCardTimer.ElapsedMilliseconds, 0, int.MaxValue);
            var result = await phraseService.SubmitPhraseReviewAsync(
                CurrentRoleplayCard.Phrase.Id,
                rating,
                elapsedMs,
                UseFsrsScheduler);

            CurrentRoleplayCard.Phrase.Status = result.NewStatus;
            CurrentRoleplayCard.Phrase.EaseFactor = result.NewEaseFactor;
            CurrentRoleplayCard.Phrase.IntervalDays = result.NewIntervalDays;
            CurrentRoleplayCard.Phrase.Repetitions = result.Repetitions;
            CurrentRoleplayCard.Phrase.Lapses = result.Lapses;
            CurrentRoleplayCard.Phrase.NextReviewAt = result.NextReviewAt;

            SyncItemInCollections(CurrentRoleplayCard.Phrase);
            RoleplayCompletedCount++;
            CurrentRoleplayIndex++;
            IsRoleplayAnswerRevealed = false;
            UserPracticeDraft = string.Empty;

            if (CurrentRoleplayIndex < RoleplayQueue.Count)
            {
                CurrentRoleplayCard = RoleplayQueue[CurrentRoleplayIndex];
                _roleplayCardTimer.Restart();
            }
            else
            {
                CurrentRoleplayCard = null;
                _roleplayCardTimer.Stop();
            }

            UpdateSummaryCounters();
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorSave")}: {ex.Message}", isError: true);
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
