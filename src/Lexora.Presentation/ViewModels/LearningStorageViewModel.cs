using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class LearningStorageViewModel : ObservableObject, IDisposable
{
    private readonly SemaphoreSlim _dbLock = new(1, 1);
    private readonly ILearningStorageService _storageService;
    private readonly Services.ILocalizationService _localizationService;
    private readonly IAudioPlayerService? _audioPlayer;
    private readonly Services.ISoundFeedbackService? _soundFeedback;
    private readonly IUserPreferencesService? _preferencesService;
    private readonly DispatcherQueue? _dispatcherQueue;

    public Services.ILocalizationService Loc => _localizationService;

    // Active View / Tab: 0 = Vocabulary, 1 = Grammar
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVocabTabSelected))]
    [NotifyPropertyChangedFor(nameof(IsGrammarTabSelected))]
    public partial int SelectedTab { get; set; } = 0;

    public bool IsVocabTabSelected => SelectedTab == 0;
    public bool IsGrammarTabSelected => SelectedTab == 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyState))]
    public partial bool IsLoading { get; set; } = true;

    public bool IsEmptyState => !IsLoading && StorageItems.Count == 0;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedStatusFilter { get; set; } = "All";

    [ObservableProperty]
    public partial LearningStorageItemDto? SelectedItem { get; set; }

    // Multi-Selection State
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCount))]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial bool IsSelectionMode { get; set; } = false;

    public List<Guid> SelectedItemIds { get; } = new();

    public int SelectedCount => SelectedItemIds.Count;
    public bool HasSelection => SelectedItemIds.Count > 0;

    // Statistics Cards
    [ObservableProperty]
    public partial int TotalItems { get; set; } = 0;

    [ObservableProperty]
    public partial int VocabCount { get; set; } = 0;

    [ObservableProperty]
    public partial int GrammarCount { get; set; } = 0;

    [ObservableProperty]
    public partial int WeakCount { get; set; } = 0;

    [ObservableProperty]
    public partial int LearningCount { get; set; } = 0;

    [ObservableProperty]
    public partial int MasteredCount { get; set; } = 0;

    [ObservableProperty]
    public partial int DueTodayCount { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvgRetention))]
    [NotifyPropertyChangedFor(nameof(FormattedAvgRetention))]
    public partial double AverageRetention { get; set; } = 0.0;

    public double AvgRetention => AverageRetention;
    public string FormattedAvgRetention => $"{Math.Round(AverageRetention, 1)}%";

    // Pagination
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    public partial int TotalCount { get; set; } = 0;

    public int PageSize { get; set; } = 20;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public string PageDisplayInfo => string.Format(
        _localizationService.GetString("Storage_Pagination_Format"),
        CurrentPage, Math.Max(1, TotalPages), TotalCount);

    public ObservableCollection<LearningStorageItemDto> StorageItems { get; } = new();

    // Practice Flow State
    [ObservableProperty]
    public partial bool IsPracticeActive { get; set; } = false;

    [ObservableProperty]
    public partial bool IsPracticeLoading { get; set; } = false;

    [ObservableProperty]
    public partial int CurrentPracticeIndex { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentQuestionPrompt))]
    [NotifyPropertyChangedFor(nameof(CurrentQuestionSubPrompt))]
    [NotifyPropertyChangedFor(nameof(CurrentQuestionExplanation))]
    public partial StoragePracticeQuestionDto? CurrentQuestion { get; set; }

    public string CurrentQuestionPrompt => CurrentQuestion?.Prompt ?? string.Empty;
    public string CurrentQuestionSubPrompt => CurrentQuestion?.SubPrompt ?? string.Empty;
    public string CurrentQuestionExplanation => CurrentQuestion?.Explanation ?? string.Empty;

    [ObservableProperty]
    public partial string? PracticeSelectedAnswer { get; set; }

    [ObservableProperty]
    public partial bool HasSelectedPracticeAnswer { get; set; } = false;

    [ObservableProperty]
    public partial bool IsPracticeAnswerSubmitted { get; set; } = false;

    [ObservableProperty]
    public partial bool IsCurrentAnswerCorrect { get; set; } = false;

    [ObservableProperty]
    public partial bool IsPracticeCompleted { get; set; } = false;

    [ObservableProperty]
    public partial string PracticeProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PracticeResultSummaryText { get; set; } = string.Empty;

    public ObservableCollection<TestOptionItem> CurrentOptions { get; } = new();

    public event Action? NoPracticeQuestionsAvailable;
    public event Action<string>? PracticeFailed;

    private readonly List<StoragePracticeQuestionDto> _practiceQuestions = new();
    private readonly List<StoragePracticeSubmissionDto> _practiceSubmissions = new();

    public LearningStorageViewModel(
        ILearningStorageService storageService,
        Services.ILocalizationService localizationService,
        IAudioPlayerService? audioPlayer = null,
        Services.ISoundFeedbackService? soundFeedback = null,
        IUserPreferencesService? preferencesService = null)
    {
        _storageService = storageService;
        _localizationService = localizationService;
        _audioPlayer = audioPlayer;
        _soundFeedback = soundFeedback;
        _preferencesService = preferencesService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? App.Current?.UIDispatcherQueue
            ?? App.Current?.MainWindow?.DispatcherQueue;

        _localizationService.LanguageChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(PageDisplayInfo));
        };

        _storageService.StorageChanged += OnStorageChanged;
    }

    public bool IsOperating => _isDeletingItems || _isAddingItems;

    private void OnStorageChanged(object? sender, EventArgs e)
    {
        if (_isDeletingItems || _isAddingItems) return; // Prevent concurrent reload and UI thread collision during bulk operations

        if (_dispatcherQueue != null)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                await RefreshAllAsync();
            });
        }
        else
        {
            _ = RefreshAllAsync();
        }
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await RefreshAllAsync();
    }

    public async Task RefreshAllAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            await LoadStatisticsCoreAsync();
            await LoadItemsCoreAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    [RelayCommand]
    public async Task LoadStatisticsAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            await LoadStatisticsCoreAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task LoadStatisticsCoreAsync()
    {
        try
        {
            var stats = await _storageService.GetStatisticsAsync();
            TotalItems = stats.Total;
            VocabCount = stats.VocabCount;
            GrammarCount = stats.GrammarCount;
            WeakCount = stats.WeakCount;
            LearningCount = stats.LearningCount;
            MasteredCount = stats.MasteredCount;
            DueTodayCount = stats.DueTodayCount;
            AverageRetention = stats.AverageRetention;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] LoadStatistics error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadItemsAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            await LoadItemsCoreAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task LoadItemsCoreAsync()
    {
        IsLoading = true;
        try
        {
            var itemType = SelectedTab == 0 ? StorageItemType.Vocabulary : StorageItemType.Grammar;
            StorageLearningStatus? status = SelectedStatusFilter switch
            {
                "Weak" => StorageLearningStatus.Weak,
                "Learning" => StorageLearningStatus.Learning,
                "New" => StorageLearningStatus.New,
                "Mastered" => StorageLearningStatus.Mastered,
                _ => null
            };

            bool? dueTodayOnly = SelectedStatusFilter == "DueToday" ? true : null;

            var result = await _storageService.GetPagedItemsAsync(
                itemType: itemType,
                status: status,
                search: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                dueTodayOnly: dueTodayOnly,
                page: CurrentPage,
                pageSize: PageSize
            );

            StorageItems.Clear();
            foreach (var item in result.Items)
            {
                StorageItems.Add(item);
            }

            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, result.TotalPages);
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] LoadItems error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SwitchTabAsync(int tabIndex)
    {
        if (SelectedTab == tabIndex) return;
        SelectedTab = tabIndex;
        SelectedItemIds.Clear();
        IsSelectionMode = false;
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        CurrentPage = 1;
        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task ApplyFilterAsync(string filter)
    {
        SelectedStatusFilter = filter;
        SelectedItemIds.Clear();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        CurrentPage = 1;
        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task SearchAsync(string query)
    {
        SearchQuery = query;
        SelectedItemIds.Clear();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        CurrentPage = 1;
        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadItemsAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadItemsAsync();
        }
    }

    [RelayCommand]
    public void ToggleSelectionMode()
    {
        IsSelectionMode = !IsSelectionMode;
        if (!IsSelectionMode)
        {
            SelectedItemIds.Clear();
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    [RelayCommand]
    public void SelectAllItems()
    {
        SelectedItemIds.Clear();
        foreach (var item in StorageItems)
        {
            SelectedItemIds.Add(item.Id);
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedItemIds.Clear();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    public void ToggleItemSelection(Guid id)
    {
        if (SelectedItemIds.Contains(id))
        {
            SelectedItemIds.Remove(id);
        }
        else
        {
            SelectedItemIds.Add(id);
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    public void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    private bool _isDeletingItems = false;
    private bool _isAddingItems = false;

    public async Task<int> AddWordsToStorageAsync(
        IEnumerable<Guid> wordIds,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var targetIds = wordIds.Distinct().Where(id => id != Guid.Empty).ToList();
        if (targetIds.Count == 0 || _isAddingItems || _isDeletingItems) return 0;
        _isAddingItems = true;

        if (_dispatcherQueue != null)
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
        }
        IsLoading = true;

        try
        {
            int addedCount = await _storageService.MoveWordsToStorageAsync(
                targetIds, StorageAdditionReason.Manual, note, cancellationToken);

            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }

            // Immediate UI update: switch to Vocabulary tab and refresh list & metrics
            SelectedTab = 0;
            CurrentPage = 1;
            await RefreshAllAsync();

            return addedCount;
        }
        finally
        {
            _isAddingItems = false;
            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }
            IsLoading = false;
        }
    }

    public async Task<int> AddGrammarsToStorageAsync(
        IEnumerable<Guid> grammarRuleIds,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var targetIds = grammarRuleIds.Distinct().Where(id => id != Guid.Empty).ToList();
        if (targetIds.Count == 0 || _isAddingItems || _isDeletingItems) return 0;
        _isAddingItems = true;

        if (_dispatcherQueue != null)
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
        }
        IsLoading = true;

        try
        {
            int addedCount = await _storageService.MoveGrammarsToStorageAsync(
                targetIds, StorageAdditionReason.Manual, note, cancellationToken);

            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }

            // Immediate UI update: switch to Grammar tab and refresh list & metrics
            SelectedTab = 1;
            CurrentPage = 1;
            await RefreshAllAsync();

            return addedCount;
        }
        finally
        {
            _isAddingItems = false;
            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }
            IsLoading = false;
        }
    }

    public async Task<int> DeleteSelectedItemsAsync(
        IEnumerable<Guid>? ids = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        var targetIds = ids?.Distinct().ToList() ?? SelectedItemIds.ToList();
        if (targetIds.Count == 0 || _isDeletingItems || _isAddingItems) return 0;
        _isDeletingItems = true;

        if (_dispatcherQueue != null)
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
        }
        IsLoading = true;

        try
        {
            int count = await Task.Run(
                () => _storageService.RemoveRangeFromStorageAsync(targetIds, cancellationToken, progress),
                cancellationToken);

            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }

            // Immediate UI update: remove deleted items from in-memory collection immediately
            var deletedSet = new HashSet<Guid>(targetIds);
            for (int i = StorageItems.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(StorageItems[i].Id))
                {
                    StorageItems.RemoveAt(i);
                }
            }

            TotalCount = Math.Max(0, TotalCount - count);
            if (SelectedItem != null && deletedSet.Contains(SelectedItem.Id))
            {
                SelectedItem = StorageItems.FirstOrDefault();
            }

            SelectedItemIds.Clear();
            IsSelectionMode = false;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsEmptyState));

            // Reload statistics to sync badges and metrics
            await LoadStatisticsAsync();

            // Handle page overflow if the current page was emptied
            if (StorageItems.Count == 0 && CurrentPage > 1)
            {
                CurrentPage--;
                await LoadItemsAsync();
            }
            else if (StorageItems.Count == 0 && TotalCount > 0)
            {
                await LoadItemsAsync();
            }

            return count;
        }
        finally
        {
            _isDeletingItems = false;
            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task MarkAsMasteredAsync(LearningStorageItemDto? item)
    {
        if (item == null) return;
        try
        {
            await _storageService.MarkAsMasteredAsync(item.Id);
            _soundFeedback?.PlaySuccessSound();
            await LoadStatisticsAsync();
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] MarkAsMastered error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task RemoveItemAsync(LearningStorageItemDto? item)
    {
        if (item == null) return;
        try
        {
            await _storageService.RemoveFromStorageAsync(item.Id);
            StorageItems.Remove(item);
            TotalCount = Math.Max(0, TotalCount - 1);
            await LoadStatisticsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] RemoveItem error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task UpdateStatusAsync((Guid Id, StorageLearningStatus Status) param)
    {
        try
        {
            await _storageService.UpdateStatusAsync(param.Id, param.Status);
            await LoadStatisticsAsync();
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] UpdateStatus error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveNoteAsync((Guid Id, string Note) param)
    {
        try
        {
            await _storageService.UpdateNoteAsync(param.Id, param.Note);
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] SaveNote error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task PlayAudioAsync(string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || _audioPlayer == null) return;
        try
        {
            var accent = _preferencesService?.GetPreferences().PreferredAccent ?? "us";
            await _audioPlayer.PlayPronunciationAsync(term, accent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] PlayAudio error: {ex.Message}");
        }
    }

    // --- Practice Flow ---
    [RelayCommand]
    public async Task StartPracticeAsync()
    {
        IsPracticeLoading = true;
        IsPracticeActive = true;
        IsPracticeCompleted = false;
        CurrentPracticeIndex = 0;
        _practiceQuestions.Clear();
        _practiceSubmissions.Clear();
        CurrentOptions.Clear();
        PracticeSelectedAnswer = null;
        HasSelectedPracticeAnswer = false;
        IsPracticeAnswerSubmitted = false;

        try
        {
            var itemType = SelectedTab == 0 ? StorageItemType.Vocabulary : StorageItemType.Grammar;
            var questions = await _storageService.GeneratePracticeAsync(itemType, limit: 15, prioritizeWeak: true);

            if (questions.Count == 0)
            {
                IsPracticeActive = false;
                NoPracticeQuestionsAvailable?.Invoke();
                return;
            }

            _practiceQuestions.AddRange(questions);
            DisplayCurrentQuestion();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] StartPractice error: {ex.Message}");
            IsPracticeActive = false;
            PracticeFailed?.Invoke(ex.Message);
        }
        finally
        {
            IsPracticeLoading = false;
        }
    }

    private void DisplayCurrentQuestion()
    {
        if (CurrentPracticeIndex >= 0 && CurrentPracticeIndex < _practiceQuestions.Count)
        {
            CurrentQuestion = _practiceQuestions[CurrentPracticeIndex];
            PracticeSelectedAnswer = null;
            HasSelectedPracticeAnswer = false;
            IsPracticeAnswerSubmitted = false;
            IsCurrentAnswerCorrect = false;

            CurrentOptions.Clear();
            var letters = new[] { "A", "B", "C", "D", "E", "F" };
            for (int i = 0; i < CurrentQuestion.Options.Count; i++)
            {
                var label = i < letters.Length ? letters[i] : (i + 1).ToString();
                CurrentOptions.Add(new TestOptionItem
                {
                    Text = CurrentQuestion.Options[i],
                    Letter = label,
                    Glyph = label,
                    State = TestOptionState.Default
                });
            }

            PracticeProgressText = string.Format(
                _localizationService.GetString("Storage_Practice_QuestionFormat"),
                CurrentPracticeIndex + 1, _practiceQuestions.Count);
        }
    }

    [RelayCommand]
    public void SelectPracticeAnswer(string answer)
    {
        if (IsPracticeAnswerSubmitted || string.IsNullOrWhiteSpace(answer)) return;

        PracticeSelectedAnswer = answer;
        HasSelectedPracticeAnswer = true;

        foreach (var opt in CurrentOptions)
        {
            if (string.Equals(opt.Text, answer, StringComparison.OrdinalIgnoreCase))
            {
                opt.State = TestOptionState.Selected;
            }
            else
            {
                opt.State = TestOptionState.Default;
            }
        }
    }

    [RelayCommand]
    public void SubmitPracticeAnswer()
    {
        if (CurrentQuestion == null || string.IsNullOrWhiteSpace(PracticeSelectedAnswer) || IsPracticeAnswerSubmitted) return;

        IsPracticeAnswerSubmitted = true;
        var trimmedUser = PracticeSelectedAnswer.Trim();
        var trimmedCorrect = CurrentQuestion.CorrectAnswer.Trim();
        IsCurrentAnswerCorrect = string.Equals(trimmedUser, trimmedCorrect, StringComparison.OrdinalIgnoreCase);

        foreach (var opt in CurrentOptions)
        {
            bool isThisCorrect = string.Equals(opt.Text.Trim(), trimmedCorrect, StringComparison.OrdinalIgnoreCase);
            bool isThisUser = string.Equals(opt.Text.Trim(), trimmedUser, StringComparison.OrdinalIgnoreCase);

            if (isThisCorrect)
            {
                opt.State = isThisUser ? TestOptionState.SelectedCorrect : TestOptionState.RevealedCorrect;
                opt.Glyph = "\uE73E"; // Checkmark
            }
            else if (isThisUser)
            {
                opt.State = TestOptionState.SelectedIncorrect;
                opt.Glyph = "\uE711"; // Cross
            }
            else
            {
                opt.State = TestOptionState.Default;
            }
        }

        if (IsCurrentAnswerCorrect)
        {
            _soundFeedback?.PlaySuccessSound();
        }
        else
        {
            _soundFeedback?.PlayErrorSound();
        }

        _practiceSubmissions.Add(new StoragePracticeSubmissionDto(
            CurrentQuestion.StorageItemId,
            PracticeSelectedAnswer
        ));
    }

    [RelayCommand]
    public async Task NextPracticeQuestionAsync()
    {
        if (!IsPracticeAnswerSubmitted) return;

        if (CurrentPracticeIndex + 1 < _practiceQuestions.Count)
        {
            CurrentPracticeIndex++;
            DisplayCurrentQuestion();
        }
        else
        {
            // Evaluate whole practice session
            await FinishPracticeAsync();
        }
    }

    private StoragePracticeResultDto? _lastPracticeResult;

    [RelayCommand]
    public async Task FinishPracticeAsync()
    {
        try
        {
            var result = await _storageService.EvaluatePracticeAsync(_practiceQuestions, _practiceSubmissions);
            _lastPracticeResult = result;
            IsPracticeCompleted = true;

            PracticeResultSummaryText = string.Format(
                _localizationService.GetString("Storage_Practice_ResultSummary"),
                result.Correct, result.Total, result.ScorePercent);

            await LoadStatisticsAsync();
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] FinishPractice error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void ClosePractice()
    {
        IsPracticeActive = false;
        IsPracticeCompleted = false;
        _lastPracticeResult = null;
        CurrentQuestion = null;
        CurrentOptions.Clear();
        PracticeSelectedAnswer = null;
        HasSelectedPracticeAnswer = false;
        IsPracticeAnswerSubmitted = false;
    }

    public void UpdateLanguageState()
    {
        OnPropertyChanged(nameof(PageDisplayInfo));
        if (IsPracticeActive && _practiceQuestions.Count > 0)
        {
            PracticeProgressText = string.Format(
                _localizationService.GetString("Storage_Practice_QuestionFormat"),
                CurrentPracticeIndex + 1, _practiceQuestions.Count);
        }
        else if (IsPracticeCompleted && _lastPracticeResult != null)
        {
            PracticeResultSummaryText = string.Format(
                _localizationService.GetString("Storage_Practice_ResultSummary"),
                _lastPracticeResult.Correct, _lastPracticeResult.Total, _lastPracticeResult.ScorePercent);
        }
    }

    public void Dispose()
    {
        _storageService.StorageChanged -= OnStorageChanged;
        _dbLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
