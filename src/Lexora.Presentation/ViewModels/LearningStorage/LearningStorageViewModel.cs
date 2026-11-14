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

    private readonly EventHandler _languageChangedHandler;

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

        _languageChangedHandler = (s, e) =>
        {
            OnPropertyChanged(nameof(PageDisplayInfo));
        };
        _localizationService.LanguageChanged += _languageChangedHandler;

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
        try
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
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] RefreshAllAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadStatisticsAsync()
    {
        try
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
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] LoadStatisticsAsync error: {ex.Message}");
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

        if (StorageItems.Count > 0)
        {
            bool isVi = _localizationService.CurrentLanguage.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
            var updated = StorageItems.Select(item =>
            {
                string def = item.ItemType == StorageItemType.Vocabulary
                    ? (!isVi && !string.IsNullOrWhiteSpace(item.Word?.Meanings.FirstOrDefault()?.DefinitionEn)
                        ? item.Word!.Meanings.First().DefinitionEn!
                        : (item.Word?.Meanings.FirstOrDefault()?.DefinitionVi ?? string.Empty))
                    : (!isVi && !string.IsNullOrWhiteSpace(item.GrammarRule?.ExplanationEn)
                        ? item.GrammarRule!.ExplanationEn!
                        : (item.GrammarRule?.ExplanationVi ?? string.Empty));

                return item with { CustomDefinitionOrExplanation = def };
            }).ToList();

            StorageItems.Clear();
            foreach (var it in updated)
            {
                StorageItems.Add(it);
            }
        }
    }

    public void Dispose()
    {
        _localizationService.LanguageChanged -= _languageChangedHandler;
        _storageService.StorageChanged -= OnStorageChanged;
        _dbLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
