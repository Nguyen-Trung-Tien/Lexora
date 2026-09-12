using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;

using Lexora.Infrastructure.Services;

namespace Lexora.Presentation.ViewModels;

public partial class ReviewViewModel : ObservableObject
{
    private readonly IReviewService _reviewService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly IUserPreferencesService _preferencesService;
    private readonly Stopwatch _cardTimer = new();

    private Guid? _currentSessionId;
    private int _initialQueueCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeechRateLabel))]
    public partial double SpeechRate { get; set; } = 1.0;

    public string SpeechRateLabel => $"{SpeechRate:0.##}x";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTypingRecallActive))]
    [NotifyPropertyChangedFor(nameof(IsStandardFrontActive))]
    [NotifyPropertyChangedFor(nameof(IsShowMeaningButtonVisible))]
    public partial bool IsTypingPracticeEnabled { get; set; } = false;

    public bool IsTypingRecallActive => IsTypingPracticeEnabled && !IsAnswerVisible;
    public bool IsStandardFrontActive => !IsTypingPracticeEnabled || IsAnswerVisible;
    public bool IsShowMeaningButtonVisible => !IsAnswerVisible && !IsTypingPracticeEnabled;

    [ObservableProperty]
    public partial string TypedWord { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool? IsTypingCorrect { get; set; } = null;

    [ObservableProperty]
    public partial string TypingFeedbackMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsHintVisible { get; set; } = false;

    [ObservableProperty]
    public partial string HintText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCardActive))]
    [NotifyPropertyChangedFor(nameof(IsEmptyQueue))]
    [NotifyPropertyChangedFor(nameof(IsSessionCompleted))]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTypingRecallActive))]
    [NotifyPropertyChangedFor(nameof(IsStandardFrontActive))]
    [NotifyPropertyChangedFor(nameof(IsShowMeaningButtonVisible))]
    public partial bool IsAnswerVisible { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCardActive))]
    [NotifyPropertyChangedFor(nameof(IsEmptyQueue))]
    public partial ReviewCardDto? CurrentCard { get; set; }

    [ObservableProperty]
    public partial int RemainingCount { get; set; }

    [ObservableProperty]
    public partial int CompletedCount { get; set; }

    [ObservableProperty]
    public partial int TotalQueueCount { get; set; }

    [ObservableProperty]
    public partial double ProgressPercent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyQueue))]
    [NotifyPropertyChangedFor(nameof(IsSessionCompleted))]
    public partial bool IsCompleted { get; set; } = false;

    public bool IsCardActive => CurrentCard != null && !IsLoading;
    public bool IsEmptyQueue => CurrentCard == null && !IsCompleted && !IsLoading;
    public bool IsSessionCompleted => IsCompleted && !IsLoading;

    [ObservableProperty]
    public partial ReviewSessionMode CurrentMode { get; set; } = ReviewSessionMode.TodayDue;

    [ObservableProperty]
    public partial string AgainIntervalText { get; set; } = "< 1d";

    [ObservableProperty]
    public partial string HardIntervalText { get; set; } = "1d";

    [ObservableProperty]
    public partial string GoodIntervalText { get; set; } = "4d";

    [ObservableProperty]
    public partial string EasyIntervalText { get; set; } = "7d";

    // Session stats
    [ObservableProperty]
    public partial int SessionCorrectCount { get; set; }

    [ObservableProperty]
    public partial int SessionIncorrectCount { get; set; }

    [ObservableProperty]
    public partial double SessionAccuracyRate { get; set; }

    [ObservableProperty]
    public partial string SessionDurationText { get; set; } = "00:00";

    // Counts for mode badges
    [ObservableProperty]
    public partial int DueCardsCount { get; set; }

    public ObservableCollection<ReviewCardDto> ReviewQueue { get; } = new();
    public ObservableCollection<StudyLogDto> RecentLogs { get; } = new();
    public ObservableCollection<StudySessionDto> RecentSessions { get; } = new();

    public ReviewViewModel(IReviewService reviewService, IAudioPlayerService audioPlayer, IUserPreferencesService preferencesService)
    {
        _reviewService = reviewService;
        _audioPlayer = audioPlayer;
        _preferencesService = preferencesService;

        var prefs = _preferencesService.GetPreferences();
        SpeechRate = prefs.SpeechRate;
        IsTypingPracticeEnabled = prefs.EnableTypingPractice;
        _audioPlayer.PlaybackRate = SpeechRate;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await RefreshDueCountAsync();
        await StartReviewAsync(CurrentMode);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    public async Task RefreshDueCountAsync()
    {
        DueCardsCount = await _reviewService.GetDueCardsCountAsync();
    }

    [RelayCommand]
    public async Task StartReviewAsync(ReviewSessionMode mode)
    {
        IsLoading = true;
        IsCompleted = false;
        IsAnswerVisible = false;
        CurrentMode = mode;
        SessionCorrectCount = 0;
        SessionIncorrectCount = 0;
        SessionAccuracyRate = 0.0;
        CompletedCount = 0;

        try
        {
            IReadOnlyList<ReviewCardDto> cards = mode switch
            {
                ReviewSessionMode.LearnNew => await _reviewService.GetNewCardsAsync(limit: 20),
                ReviewSessionMode.Difficult => await _reviewService.GetDifficultCardsAsync(limit: 20),
                _ => await _reviewService.GetDueCardsAsync(limit: 30)
            };

            // If in TodayDue mode and empty, fallback to new cards automatically
            if (mode == ReviewSessionMode.TodayDue && cards.Count == 0)
            {
                cards = await _reviewService.GetNewCardsAsync(limit: 15);
            }

            ReviewQueue.Clear();
            foreach (var c in cards)
            {
                ReviewQueue.Add(c);
            }

            _initialQueueCount = ReviewQueue.Count;
            TotalQueueCount = _initialQueueCount;
            RemainingCount = ReviewQueue.Count;
            ProgressPercent = 0.0;

            if (ReviewQueue.Count > 0)
            {
                var session = await _reviewService.StartSessionAsync(mode, ReviewQueue.Count);
                _currentSessionId = session.Id;
                IsCompleted = false;

                SetCurrentCard(ReviewQueue[0]);
            }
            else
            {
                CurrentCard = null;
                _currentSessionId = null;
                IsCompleted = false;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void RevealAnswer()
    {
        IsAnswerVisible = true;
    }

    private bool _isRatingInProgress;

    [RelayCommand]
    public async Task RateCardAsync(ReviewRating rating)
    {
        if (CurrentCard == null || _isRatingInProgress) return;
        _isRatingInProgress = true;

        try
        {
            _cardTimer.Stop();
            int responseTimeMs = (int)_cardTimer.ElapsedMilliseconds;

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

            await _reviewService.SubmitReviewAsync(CurrentCard.Id, rating, responseTimeMs, _currentSessionId);

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

                await RefreshDueCountAsync();
                await LoadHistoryAsync();
            }
        }
        finally
        {
            _isRatingInProgress = false;
        }
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
    public async Task LoadHistoryAsync()
    {
        var logs = await _reviewService.GetRecentLogsAsync(15);
        RecentLogs.Clear();
        foreach (var l in logs)
        {
            RecentLogs.Add(l);
        }

        var sessions = await _reviewService.GetRecentSessionsAsync(5);
        RecentSessions.Clear();
        foreach (var s in sessions)
        {
            RecentSessions.Add(s);
        }
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

    [RelayCommand]
    public async Task ToggleTypingPracticeAsync()
    {
        IsTypingPracticeEnabled = !IsTypingPracticeEnabled;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { EnableTypingPractice = IsTypingPracticeEnabled });
    }

    [RelayCommand]
    public void CheckTypedAnswer()
    {
        if (CurrentCard == null || string.IsNullOrWhiteSpace(TypedWord)) return;

        var expected = CurrentCard.Term.Trim();
        var actual = TypedWord.Trim();

        bool isCorrect = string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        IsTypingCorrect = isCorrect;

        if (isCorrect)
        {
            TypingFeedbackMessage = "Chính xác! Hoàn hảo 👏";
            IsAnswerVisible = true;
            _ = PlayUkAudioAsync();
        }
        else
        {
            TypingFeedbackMessage = $"Chưa chính xác: \"{actual}\" ➔ Đáp án đúng: \"{CurrentCard.Term}\"";
            IsAnswerVisible = true;
        }
    }

    [RelayCommand]
    public void ProvideHint()
    {
        if (CurrentCard == null) return;
        var term = CurrentCard.Term.Trim();
        if (term.Length <= 2)
        {
            HintText = term;
        }
        else
        {
            char first = term[0];
            char last = term[^1];
            string dashes = string.Join(" ", Enumerable.Repeat("_", term.Length - 2));
            HintText = $"{first} {dashes} {last}";
        }
        IsHintVisible = true;
    }

    [RelayCommand]
    public void SkipTyping()
    {
        IsAnswerVisible = true;
    }

    private void SetCurrentCard(ReviewCardDto card)
    {
        CurrentCard = card;
        _cardTimer.Restart();

        TypedWord = string.Empty;
        IsTypingCorrect = null;
        TypingFeedbackMessage = string.Empty;
        IsHintVisible = false;
        HintText = string.Empty;

        var preview = _reviewService.GetCardIntervalPreview(card);
        AgainIntervalText = preview.AgainText;
        HardIntervalText = preview.HardText;
        GoodIntervalText = preview.GoodText;
        EasyIntervalText = preview.EasyText;
    }
}
