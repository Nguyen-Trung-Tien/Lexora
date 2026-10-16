using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class ReviewViewModel : ObservableObject, IDisposable
{
    private readonly IReviewService _reviewService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IVocabularyService _vocabService;
    private readonly ILocalizationService _localizationService;
    private readonly ISoundFeedbackService? _soundFeedback;
    private readonly IToastNotificationService? _toastService;
    private readonly Stopwatch _cardTimer = new();
    private readonly SemaphoreSlim _ratingLock = new(1, 1);

    private Guid? _currentSessionId;
    private int _initialQueueCount;
    private string? _customTitle;

    public void UpdateActiveSessionTitle()
    {
        ActiveSessionTitle = _customTitle ?? CurrentMode switch
        {
            ReviewSessionMode.LearnNew => _localizationService.GetString("Review_Session_LearnNew"),
            ReviewSessionMode.Difficult => _localizationService.GetString("Review_Session_Difficult"),
            ReviewSessionMode.ByTopic => _localizationService.GetString("Review_Session_ByTopic"),
            ReviewSessionMode.CustomSelection => _localizationService.GetString("Review_Session_CustomSelection"),
            _ => _localizationService.GetString("Review_Session_TodayDue")
        };
    }

    public record ReviewUndoState(
        ReviewCardDto Card,
        ReviewRating Rating,
        int SessionCorrectCount,
        int SessionIncorrectCount,
        int CompletedCount,
        int RemainingCount,
        double ProgressPercent,
        bool IsCompleted
    );

    private readonly Stack<ReviewUndoState> _undoStack = new();

    [ObservableProperty]
    public partial bool CanUndo { get; set; } = false;

    [ObservableProperty]
    public partial string WordScaffold { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActiveSessionTitle { get; set; } = string.Empty;

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
    public partial string ClozeSentenceEn { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ClozeSentenceVi { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasClozeSentence { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCardActive))]
    [NotifyPropertyChangedFor(nameof(IsEmptyQueue))]
    [NotifyPropertyChangedFor(nameof(IsSessionCompleted))]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTypingRecallActive))]
    [NotifyPropertyChangedFor(nameof(IsStandardFrontActive))]
    [NotifyPropertyChangedFor(nameof(IsShowMeaningButtonVisible))]
    [NotifyPropertyChangedFor(nameof(CanRate))]
    public partial bool IsAnswerVisible { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCardActive))]
    [NotifyPropertyChangedFor(nameof(IsEmptyQueue))]
    [NotifyPropertyChangedFor(nameof(CanRate))]
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
    public ObservableCollection<TopicDto> AvailableTopics { get; } = new();

    public ReviewViewModel(
        IReviewService reviewService,
        IAudioPlayerService audioPlayer,
        IUserPreferencesService preferencesService,
        IVocabularyService vocabService,
        ILocalizationService localizationService,
        ISoundFeedbackService? soundFeedback = null,
        IToastNotificationService? toastService = null)
    {
        _reviewService = reviewService;
        _audioPlayer = audioPlayer;
        _preferencesService = preferencesService;
        _vocabService = vocabService;
        _localizationService = localizationService;
        _soundFeedback = soundFeedback;
        _toastService = toastService;

        UpdateActiveSessionTitle();

        var prefs = _preferencesService.GetPreferences();
        SpeechRate = prefs.SpeechRate;
        IsTypingPracticeEnabled = prefs.EnableTypingPractice;
        _audioPlayer.PlaybackRate = SpeechRate;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadAvailableTopicsAsync();
        await RefreshDueCountAsync();
        await StartReviewAsync(CurrentMode);
        await LoadHistoryAsync();
    }

    public async Task LoadAvailableTopicsAsync()
    {
        try
        {
            var topics = await _vocabService.GetAllTopicsAsync();
            AvailableTopics.Clear();
            foreach (var t in topics)
            {
                AvailableTopics.Add(t);
            }
        }
        catch
        {
            // Ignore if topics failed to load
        }
    }

    [RelayCommand]
    public async Task RefreshDueCountAsync()
    {
        try
        {
            DueCardsCount = await _reviewService.GetDueCardsCountAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReviewViewModel] RefreshDueCount error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task StartReviewAsync(ReviewSessionMode mode)
    {
        await StartReviewWithFilterAsync(mode, null, null, null);
    }

    private bool _isStartingReview = false;

    public async Task StartReviewWithFilterAsync(ReviewSessionMode mode, Guid? topicId = null, IReadOnlyList<Guid>? wordIds = null, string? customTitle = null)
    {
        if (_isStartingReview) return;
        _isStartingReview = true;
        IsLoading = true;
        IsCompleted = false;
        IsAnswerVisible = false;
        CurrentMode = mode;
        SessionCorrectCount = 0;
        SessionIncorrectCount = 0;
        SessionAccuracyRate = 0.0;
        CompletedCount = 0;

        _customTitle = customTitle;
        UpdateActiveSessionTitle();

        try
        {
            IReadOnlyList<ReviewCardDto> cards = mode switch
            {
                ReviewSessionMode.LearnNew => await _reviewService.GetNewCardsAsync(limit: 20),
                ReviewSessionMode.Difficult => await _reviewService.GetDifficultCardsAsync(limit: 20),
                ReviewSessionMode.ByTopic when topicId.HasValue => await _reviewService.GetCardsByTopicIdAsync(topicId.Value, limit: 50),
                ReviewSessionMode.CustomSelection when wordIds != null && wordIds.Count > 0 => await _reviewService.GetCardsByWordIdsAsync(wordIds),
                _ => await _reviewService.GetBalancedReviewQueueAsync(limit: 30)
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

            _undoStack.Clear();
            CanUndo = false;

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReviewViewModel] StartReview error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _isStartingReview = false;
        }
    }

    public async Task StartUnitReviewAsync(ContextUnitDto unit)
    {
        var title = string.Format(_localizationService.GetString("Review_Session_ByTopicFormat"), unit.Name);
        await StartReviewWithFilterAsync(ReviewSessionMode.ByTopic, topicId: unit.Id, wordIds: null, customTitle: title);
    }

    public async Task StartCustomWordReviewAsync(IReadOnlyList<Guid> wordIds, string? title = null)
    {
        var defaultTitle = string.Format(_localizationService.GetString("Review_Session_SelectedCount"), wordIds.Count);
        await StartReviewWithFilterAsync(ReviewSessionMode.CustomSelection, topicId: null, wordIds: wordIds, customTitle: title ?? defaultTitle);
    }

    [RelayCommand]
    public void RevealAnswer()
    {
        IsAnswerVisible = true;
        _soundFeedback?.PlayCardFlipSound();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRate))]
    public partial bool IsRatingInProgress { get; set; } = false;

    public bool CanRate => !IsRatingInProgress && IsAnswerVisible && CurrentCard != null;

    [RelayCommand]
    public async Task RateCardAsync(ReviewRating rating)
    {
        if (CurrentCard == null || !_ratingLock.Wait(0)) return;
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReviewViewModel] UndoLastReviewAsync error: {ex.Message}");
            _toastService?.ShowError(ex.Message, _localizationService.GetString("Common_Error"));
        }
        finally
        {
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
            TypingFeedbackMessage = _localizationService.GetString("Review_TypingFeedbackCorrect");
            IsAnswerVisible = true;
            _ = PlayPreferredAudioAsync();
        }
        else
        {
            TypingFeedbackMessage = string.Format(_localizationService.GetString("Review_TypingFeedbackIncorrectFormat"), actual, CurrentCard.Term);
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
        WordScaffold = GenerateWordScaffold(card.Term);

        var preview = _reviewService.GetCardIntervalPreview(card);
        AgainIntervalText = preview.AgainText;
        HardIntervalText = preview.HardText;
        GoodIntervalText = preview.GoodText;
        EasyIntervalText = preview.EasyText;

        if (card.Examples != null && card.Examples.Count > 0)
        {
            var ex = card.Examples.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.SentenceEn) && e.SentenceEn.Contains(card.Term, StringComparison.OrdinalIgnoreCase))
                     ?? card.Examples.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.SentenceEn));

            if (ex != null && !string.IsNullOrWhiteSpace(ex.SentenceEn))
            {
                var pattern = System.Text.RegularExpressions.Regex.Escape(card.Term);
                var masked = System.Text.RegularExpressions.Regex.Replace(ex.SentenceEn, pattern, "[ ________ ]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                ClozeSentenceEn = $"\"{masked}\"";
                ClozeSentenceVi = string.IsNullOrWhiteSpace(ex.SentenceVi) ? string.Empty : $"\"{ex.SentenceVi}\"";
                HasClozeSentence = true;
            }
            else
            {
                ClozeSentenceEn = string.Empty;
                ClozeSentenceVi = string.Empty;
                HasClozeSentence = false;
            }
        }
        else
        {
            ClozeSentenceEn = string.Empty;
            ClozeSentenceVi = string.Empty;
            HasClozeSentence = false;
        }
    }

    private string GenerateWordScaffold(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return string.Empty;
        var words = term.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var parts = new List<string>();
        int totalLetters = 0;

        foreach (var w in words)
        {
            totalLetters += w.Length;
            if (w.Length <= 2)
            {
                parts.Add(w);
            }
            else
            {
                char first = w[0];
                char last = w[^1];
                string blanks = string.Join(" ", Enumerable.Repeat("_", w.Length - 2));
                parts.Add($"{first} {blanks} {last}");
            }
        }

        var countLabel = string.Format(_localizationService.GetString("Review_LettersCountFormat"), totalLetters);
        return $"{string.Join("   ", parts)}  ({countLabel})";
    }

    public void StopAudio()
    {
        _audioPlayer.Stop();
    }

    public void Dispose()
    {
        _ratingLock.Dispose();
        GC.SuppressFinalize(this);
    }
}

