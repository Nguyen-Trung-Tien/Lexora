using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class ReviewViewModel
{
    public bool CanRate => !IsSubmitting && !IsRatingInProgress && IsAnswerVisible && CurrentCard != null;

    [RelayCommand]
    public async Task RateCardAsync(ReviewRating rating)
    {
        if (CurrentCard == null || IsSubmitting || !_ratingLock.Wait(0)) return;
        IsSubmitting = true;
        IsRatingInProgress = true;

        try
        {
            _cardTimer.Stop();
            int responseTimeMs = (int)_cardTimer.ElapsedMilliseconds;

            var undoState = new ReviewUndoState(
                CurrentCard,
                rating,
                SessionCorrectCount,
                SessionIncorrectCount,
                CompletedCount,
                RemainingCount,
                ProgressPercent,
                IsCompleted
            );

            await _reviewService.SubmitReviewAsync(CurrentCard.Id, rating, responseTimeMs, _currentSessionId);

            if (_studyPlannerService != null)
            {
                try
                {
                    var taskType = CurrentMode switch
                    {
                        ReviewSessionMode.Difficult => CurriculumTaskType.PracticeWeakWords,
                        ReviewSessionMode.LearnNew => CurriculumTaskType.LearnNewWords,
                        _ => CurriculumTaskType.ReviewDueCards
                    };
                    await _studyPlannerService.RecordTaskCompletionAsync(taskType, 1);
                }
                catch
                {
                    // Non-critical Study Planner sync
                }
            }

            _undoStack.Push(undoState);
            CanUndo = true;

            if (rating >= ReviewRating.Good)
            {
                SessionCorrectCount++;
            }
            else
            {
                SessionIncorrectCount++;
            }

            int totalReviewed = SessionCorrectCount + SessionIncorrectCount;
            SessionAccuracyRate = totalReviewed == 0 ? 0.0 : Math.Round((double)SessionCorrectCount / totalReviewed * 100, 1);

            if (ReviewQueue.Count > 0)
            {
                ReviewQueue.RemoveAt(0);
            }

            CompletedCount++;
            RemainingCount = ReviewQueue.Count;
            ProgressPercent = _initialQueueCount == 0 ? 100.0 : Math.Round((double)CompletedCount / _initialQueueCount * 100, 1);
            IsAnswerVisible = false;

            if (ReviewQueue.Count > 0)
            {
                SetCurrentCard(ReviewQueue[0]);
            }
            else
            {
                CurrentCard = null;
                IsCompleted = true;

                if (_currentSessionId.HasValue)
                {
                    await _reviewService.CompleteSessionAsync(
                        _currentSessionId.Value,
                        _initialQueueCount,
                        SessionCorrectCount,
                        SessionIncorrectCount);
                }

                // Fluent Sound Feedback: Triumphant streak chime if user performed well, or calming accomplishment chime
                if (SessionCorrectCount > 0 && SessionCorrectCount >= SessionIncorrectCount)
                {
                    _soundFeedback?.PlayStreakSound();
                }
                else
                {
                    _soundFeedback?.PlayReviewCompleteSound();
                }

                await RefreshDueCountAsync();
                await LoadHistoryAsync();

                if (_roadmapService != null)
                {
                    try
                    {
                        await _roadmapService.SyncRoadmapProgressAsync();
                    }
                    catch
                    {
                        // Background sync failure ignored safely
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _cardTimer.Start();
            System.Diagnostics.Debug.WriteLine($"[ReviewViewModel] SubmitRatingAsync error: {ex.Message}");
            _toastService?.ShowError(ex.Message, _localizationService.GetString("Common_Error"));
        }
        finally
        {
            IsSubmitting = false;
            IsRatingInProgress = false;
            try { _ratingLock.Release(); } catch (ObjectDisposedException) { }
        }
    }

    [RelayCommand]
    public async Task UndoLastReviewAsync()
    {
        if (_undoStack.Count == 0 || !_ratingLock.Wait(0)) return;
        IsRatingInProgress = true;

        try
        {
            var state = _undoStack.Pop();
            CanUndo = _undoStack.Count > 0;

            await _reviewService.UndoReviewAsync(
                state.Card.Id,
                state.Card.Status,
                state.Card.IntervalDays,
                state.Card.EaseFactor,
                state.Card.Repetitions,
                state.Card.Lapses,
                state.Card.NextReviewAt);

            SessionCorrectCount = state.SessionCorrectCount;
            SessionIncorrectCount = state.SessionIncorrectCount;
            CompletedCount = state.CompletedCount;
            RemainingCount = state.RemainingCount;
            ProgressPercent = state.ProgressPercent;
            IsCompleted = false;

            // Prepend card back to queue
            ReviewQueue.Insert(0, state.Card);
            SetCurrentCard(state.Card);
            IsAnswerVisible = true;

            int totalReviewed = SessionCorrectCount + SessionIncorrectCount;
            SessionAccuracyRate = totalReviewed == 0 ? 0.0 : Math.Round((double)SessionCorrectCount / totalReviewed * 100, 1);

            _toastService?.ShowSuccess(_localizationService.GetString("Review_UndoSuccess"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReviewViewModel] UndoLastReviewAsync error: {ex.Message}");
            _toastService?.ShowError(ex.Message, _localizationService.GetString("Common_Error"));
        }
        finally
        {
            IsSubmitting = false;
            IsRatingInProgress = false;
            try { _ratingLock.Release(); } catch (ObjectDisposedException) { }
        }
    }

    [RelayCommand]
    public async Task PlayPreferredAudioAsync()
    {
        if (CurrentCard == null) return;
        var accent = _preferencesService.GetPreferences().PreferredAccent ?? "uk";
        await _audioPlayer.PlayPronunciationAsync(CurrentCard.Term, accent);
    }

    [RelayCommand]
    public async Task PlayUkAudioAsync()
    {
        if (CurrentCard == null) return;
        await _audioPlayer.PlayPronunciationAsync(CurrentCard.Term, "uk");
    }

    [RelayCommand]
    public async Task PlayUsAudioAsync()
    {
        if (CurrentCard == null) return;
        await _audioPlayer.PlayPronunciationAsync(CurrentCard.Term, "us");
    }

    [RelayCommand]
    public async Task SetSpeechRateAsync(double rate)
    {
        SpeechRate = rate;
        _audioPlayer.PlaybackRate = rate;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { SpeechRate = rate });
    }

    [RelayCommand]
    public void ToggleSlowSpeech()
    {
        double newRate = Math.Abs(SpeechRate - 0.75) < 0.05 ? 1.0 : 0.75;
        _ = SetSpeechRateAsync(newRate);
    }
}
