using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class VocabularyViewModel : ObservableObject
{
    private readonly IVocabularyService _vocabService;
    private readonly IReviewService _reviewService;
    private readonly IExportImportService _exportImportService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly IFuzzySearchService _fuzzySearchService;
    private readonly ISmartSearchService? _smartSearchService;
    private readonly ILocalizationService _localizationService;
    private readonly IUserPreferencesService _preferencesService;
    private readonly List<string> _knownTerms = new();
    // Captured on the UI thread so post-await mutations can be safely marshalled back.
    private readonly DispatcherQueue? _dispatcherQueue;
    private CancellationTokenSource? _suggestionsCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyState))]
    public partial bool IsLoading { get; set; } = true;

    public bool IsEmptyState => !IsLoading && Words.Count == 0;

    [ObservableProperty]
    public partial string SearchTerm { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? TypoSuggestion { get; set; }

    [ObservableProperty]
    public partial bool HasTypoSuggestion { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> ClosestSuggestions { get; set; } = Array.Empty<string>();

    [ObservableProperty]
    public partial bool HasClosestSuggestions { get; set; }

    [ObservableProperty]
    public partial WordRelatedSuggestionsDto? RelatedSuggestions { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingSuggestions { get; set; }

    [ObservableProperty]
    public partial bool HasRelatedSuggestions { get; set; }

    [ObservableProperty]
    public partial string SelectedSuggestionTab { get; set; } = "All";

    [ObservableProperty]
    public partial CefrLevel? SelectedLevel { get; set; }

    [ObservableProperty]
    public partial PartOfSpeech? SelectedPartOfSpeech { get; set; }

    [ObservableProperty]
    public partial Guid? SelectedTopicId { get; set; }

    [ObservableProperty]
    public partial ExamType? SelectedExamType { get; set; }

    [ObservableProperty]
    public partial LearningStatus? SelectedStatus { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterAllSelected))]
    [NotifyPropertyChangedFor(nameof(IsFilterDueTodaySelected))]
    [NotifyPropertyChangedFor(nameof(IsFilterWeakSelected))]
    [NotifyPropertyChangedFor(nameof(IsFilterLearningSelected))]
    [NotifyPropertyChangedFor(nameof(IsFilterMasteredSelected))]
    [NotifyPropertyChangedFor(nameof(IsFilterNewSelected))]
    public partial string SelectedQuickFilter { get; set; } = "All";

    public bool IsFilterAllSelected => SelectedQuickFilter == "All";
    public bool IsFilterDueTodaySelected => SelectedQuickFilter == "DueToday";
    public bool IsFilterWeakSelected => SelectedQuickFilter == "Weak";
    public bool IsFilterLearningSelected => SelectedQuickFilter == "Learning";
    public bool IsFilterMasteredSelected => SelectedQuickFilter == "Mastered";
    public bool IsFilterNewSelected => SelectedQuickFilter == "New";

    [ObservableProperty]
    public partial string SortBy { get; set; } = "term";

    [ObservableProperty]
    public partial bool IsAscending { get; set; } = true;

    [ObservableProperty]
    public partial WordDto? SelectedWord { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsUnitViewMode { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCount))]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial bool IsSelectionMode { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    [NotifyPropertyChangedFor(nameof(ItemRangeDisplayInfo))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    [NotifyPropertyChangedFor(nameof(ItemRangeDisplayInfo))]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemRangeDisplayInfo))]
    public partial int TotalWordCount { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemRangeDisplayInfo))]
    public partial int PageSize { get; set; } = 50;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public string PageDisplayInfo => $"{CurrentPage} / {Math.Max(1, TotalPages)}";
    public string ItemRangeDisplayInfo => TotalWordCount > 0
        ? $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalWordCount)}-{Math.Min(CurrentPage * PageSize, TotalWordCount)} / {TotalWordCount}"
        : "0 / 0";

    partial void OnSearchTermChanged(string value) => CurrentPage = 1;
    partial void OnSelectedLevelChanged(CefrLevel? value) => CurrentPage = 1;
    partial void OnSelectedPartOfSpeechChanged(PartOfSpeech? value) => CurrentPage = 1;
    partial void OnSelectedTopicIdChanged(Guid? value) => CurrentPage = 1;
    partial void OnSelectedExamTypeChanged(ExamType? value) => CurrentPage = 1;
    partial void OnSelectedStatusChanged(LearningStatus? value) => CurrentPage = 1;
    partial void OnSelectedQuickFilterChanged(string value) => CurrentPage = 1;
    partial void OnSortByChanged(string value) => CurrentPage = 1;
    partial void OnIsAscendingChanged(bool value) => CurrentPage = 1;
    partial void OnSelectedWordChanged(WordDto? value) => _ = LoadWordSuggestionsAsync(value);

    public ObservableCollection<Guid> SelectedWordIds { get; } = new();

    public int SelectedCount => SelectedWordIds.Count;
    public bool HasSelection => SelectedWordIds.Count > 0;

    public ObservableCollection<WordDto> Words { get; } = new();
    public ObservableCollection<TopicDto> AvailableTopics { get; } = new();
    public ObservableCollection<ExamTagDto> AvailableExamTags { get; } = new();
    public ObservableCollection<ContextUnitDto> ContextUnits { get; } = new();
    public ObservableCollection<ContextUnitDto> FilteredContextUnits { get; } = new();

    public VocabularyViewModel(
        IVocabularyService vocabService,
        IReviewService reviewService,
        IExportImportService exportImportService,
        IAudioPlayerService audioPlayer,
        IFuzzySearchService fuzzySearchService,
        ILocalizationService localizationService,
        IUserPreferencesService preferencesService,
        ISmartSearchService? smartSearchService = null)
    {
        _vocabService = vocabService;
        _reviewService = reviewService;
        _exportImportService = exportImportService;
        _audioPlayer = audioPlayer;
        _fuzzySearchService = fuzzySearchService;
        _localizationService = localizationService;
        _preferencesService = preferencesService;
        _smartSearchService = smartSearchService;
        // Must be captured on the UI thread (constructor is always called from UI thread).
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadFiltersAsync();
        await LoadContextUnitsAsync();
        await LoadWordsAsync();
    }

    [RelayCommand]
    public async Task LoadContextUnitsAsync()
    {
        try
        {
            var units = await _vocabService.GetContextUnitsAsync();
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            ContextUnits.Clear();
            foreach (var u in units)
            {
                ContextUnits.Add(u);
            }
            ApplyUnitFilter();
        }
        catch
        {
            // Keep collection stable on error
        }
    }

    public void ApplyUnitFilter()
    {
        FilteredContextUnits.Clear();
        var query = SearchTerm?.Trim();
        var list = string.IsNullOrEmpty(query)
            ? ContextUnits
            : ContextUnits.Where(u =>
                (!string.IsNullOrEmpty(u.Name) && u.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(u.Description) && u.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (u.Words != null && u.Words.Any(w => !string.IsNullOrEmpty(w.Term) && w.Term.Contains(query, StringComparison.OrdinalIgnoreCase))));

        foreach (var unit in list)
        {
            FilteredContextUnits.Add(unit);
        }
    }

    [RelayCommand]
    public void SwitchToListView()
    {
        IsUnitViewMode = false;
    }

    [RelayCommand]
    public void SwitchToUnitView()
    {
        IsUnitViewMode = true;
    }

    [RelayCommand]
    public async Task FilterByUnitAsync(ContextUnitDto? unit)
    {
        if (unit == null || unit.Id == Guid.Empty)
        {
            SelectedTopicId = null;
        }
        else
        {
            SelectedTopicId = unit.Id;
        }
        IsUnitViewMode = false;
        await LoadWordsAsync();
    }

    [RelayCommand]
    public async Task LoadFiltersAsync()
    {
        try
        {
            var topics = await _vocabService.GetAllTopicsAsync();
            var tags = await _vocabService.GetAllExamTagsAsync();
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            AvailableTopics.Clear();
            foreach (var t in topics)
            {
                AvailableTopics.Add(t);
            }

            AvailableExamTags.Clear();
            foreach (var et in tags)
            {
                AvailableExamTags.Add(et);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyViewModel] LoadFiltersAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SelectQuickFilterAsync(string filter)
    {
        try
        {
            SelectedQuickFilter = filter;
            switch (filter)
            {
                case "All":
                case "DueToday":
                case "Weak":
                    SelectedStatus = null;
                    break;
                case "Learning":
                    SelectedStatus = LearningStatus.Learning;
                    break;
                case "Mastered":
                    SelectedStatus = LearningStatus.Mastered;
                    break;
                case "New":
                    SelectedStatus = LearningStatus.New;
                    break;
            }

            await LoadWordsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyViewModel] SelectQuickFilter error: {ex.Message}");
        }
    }

    private CancellationTokenSource? _loadCts;

    [RelayCommand]
    public async Task LoadWordsAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;
        try
        {
            List<WordDto> resultList;

            bool isDueToday = SelectedQuickFilter == "DueToday";
            bool isWeak = SelectedQuickFilter == "Weak";

            var total = await _vocabService.CountWordsAsync(
                search: string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                pos: SelectedPartOfSpeech,
                topicId: SelectedTopicId,
                examType: SelectedExamType,
                status: SelectedStatus,
                isDueToday: isDueToday ? true : null,
                isWeak: isWeak ? true : null,
                cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

            TotalWordCount = total;
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalWordCount / PageSize));

            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var results = await _vocabService.SearchWordsAsync(
                search: string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                pos: SelectedPartOfSpeech,
                topicId: SelectedTopicId,
                examType: SelectedExamType,
                status: SelectedStatus,
                sortBy: isDueToday ? "nextreview" : SortBy,
                isAscending: isDueToday || IsAscending,
                isDueToday: isDueToday ? true : null,
                isWeak: isWeak ? true : null,
                page: CurrentPage,
                pageSize: PageSize,
                cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

            var filteredResults = results.AsEnumerable();
            if (SortBy == "pos")
            {
                filteredResults = IsAscending
                    ? filteredResults.OrderBy(w => w.PrimaryPartOfSpeech).ThenBy(w => w.Term)
                    : filteredResults.OrderByDescending(w => w.PrimaryPartOfSpeech).ThenBy(w => w.Term);
            }
            else if (SortBy == "multipos")
            {
                filteredResults = filteredResults
                    .OrderByDescending(w => w.AllPartsOfSpeech.Count)
                    .ThenBy(w => w.Term);
            }

            resultList = filteredResults.ToList();

            await _dispatcherQueue.EnsureOnUIThreadAsync();

            Words.Clear();
            foreach (var w in resultList)
            {
                Words.Add(w);
            }

            if (Words.Count > 0)
            {
                var matched = SelectedWord != null ? Words.FirstOrDefault(w => w.Id == SelectedWord.Id) : null;
                SelectedWord = matched ?? Words[0];
            }
            else
            {
                SelectedWord = null;
            }

            if (TotalWordCount == 0 && !string.IsNullOrWhiteSpace(SearchTerm))
            {
                try
                {
                    var smartRes = await _vocabService.SmartSearchWordsAsync(
                        search: SearchTerm,
                        level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                        pos: SelectedPartOfSpeech,
                        topicId: SelectedTopicId,
                        examType: SelectedExamType,
                        status: SelectedStatus,
                        page: 1,
                        pageSize: 1,
                        cancellationToken: ct);

                    if (smartRes.ClosestSuggestions.Count > 0)
                    {
                        ClosestSuggestions = smartRes.ClosestSuggestions;
                        HasClosestSuggestions = true;
                        TypoSuggestion = smartRes.ClosestSuggestions[0];
                        HasTypoSuggestion = true;
                    }
                    else
                    {
                        CheckTypoSuggestion();
                    }
                }
                catch
                {
                    CheckTypoSuggestion();
                }
            }
            else
            {
                ClosestSuggestions = Array.Empty<string>();
                HasClosestSuggestions = false;
                TypoSuggestion = null;
                HasTypoSuggestion = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancelled queries
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyViewModel] LoadWordsAsync error: {ex.Message}");
        }
        finally
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public async Task FirstPageAsync()
    {
        if (CurrentPage != 1)
        {
            CurrentPage = 1;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public async Task LastPageAsync()
    {
        if (CurrentPage != TotalPages)
        {
            CurrentPage = TotalPages;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public async Task ChangePageSizeAsync(int newSize)
    {
        if (newSize > 0 && PageSize != newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public void ResetFilters()
    {
        SearchTerm = string.Empty;
        SelectedLevel = null;
        SelectedPartOfSpeech = null;
        SelectedTopicId = null;
        SelectedExamType = null;
        SelectedStatus = null;
        SelectedQuickFilter = "All";
    }

    private bool _isSavingWord = false;

    [RelayCommand]
    public async Task SaveWordAsync(WordCreateOrUpdateDto dto)
    {
        if (_isSavingWord) return;
        _isSavingWord = true;
        try
        {
            WordDto saved;
            if (dto.Id == null)
            {
                saved = await _vocabService.CreateWordAsync(dto);
                await _dispatcherQueue.EnsureOnUIThreadAsync();
                StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_WordAdded"), saved.Term);
            }
            else
            {
                saved = await _vocabService.UpdateWordAsync(dto);
                await _dispatcherQueue.EnsureOnUIThreadAsync();
                StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_WordUpdated"), saved.Term);
            }

            await LoadWordsAsync();
            _ = LoadContextUnitsAsync();

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            var matched = Words.FirstOrDefault(w => w.Id == saved.Id);
            if (matched == null)
            {
                ResetFilters();
                await LoadWordsAsync();
                matched = Words.FirstOrDefault(w => w.Id == saved.Id);
            }
            SelectedWord = matched ?? saved;
        }
        finally
        {
            _isSavingWord = false;
        }
    }

    private bool _isCreatingTopic = false;

    [RelayCommand]
    public async Task<TopicDto?> CreateTopicAsync(TopicCreateOrUpdateDto dto)
    {
        if (_isCreatingTopic) return null;
        _isCreatingTopic = true;
        try
        {
            var created = await _vocabService.CreateTopicAsync(dto);
            await LoadFiltersAsync();
            await LoadContextUnitsAsync();
            StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_TopicCreated"), created.Name);
            return created;
        }
        finally
        {
            _isCreatingTopic = false;
        }
    }

    private bool _isDeletingWord = false;

    [RelayCommand]
    public async Task DeleteWordAsync(Guid id)
    {
        if (_isDeletingWord) return;
        _isDeletingWord = true;
        try
        {
            var word = Words.FirstOrDefault(w => w.Id == id);
            string term = word?.Term ?? string.Empty;

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (SelectedWord?.Id == id)
            {
                SelectedWord = null;
            }

            await _vocabService.DeleteWordAsync(id);

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (!string.IsNullOrEmpty(term))
            {
                StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_WordDeleted"), term);
            }

            await LoadWordsAsync();
            _ = LoadContextUnitsAsync();
        }
        finally
        {
            _isDeletingWord = false;
        }
    }

    [RelayCommand]
    public void ToggleSelectionMode()
    {
        IsSelectionMode = !IsSelectionMode;
        if (!IsSelectionMode)
        {
            SelectedWordIds.Clear();
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    [RelayCommand]
    public void SelectAllWords()
    {
        SelectedWordIds.Clear();
        foreach (var word in Words)
        {
            SelectedWordIds.Add(word.Id);
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedWordIds.Clear();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    public void ToggleWordSelection(Guid id)
    {
        if (SelectedWordIds.Contains(id))
        {
            SelectedWordIds.Remove(id);
        }
        else
        {
            SelectedWordIds.Add(id);
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    private bool _isDeletingWords = false;

    public async Task<int> DeleteSelectedWordsAsync(IEnumerable<Guid>? ids = null)
    {
        var targetIds = ids?.Distinct().ToList() ?? SelectedWordIds.ToList();
        if (targetIds.Count == 0 || _isDeletingWords) return 0;
        _isDeletingWords = true;
        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;

        try
        {
            if (SelectedWord != null && targetIds.Contains(SelectedWord.Id))
            {
                SelectedWord = null;
            }

            int count = await Task.Run(() => _vocabService.DeleteWordsAsync(targetIds));

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            SelectedWordIds.Clear();
            IsSelectionMode = false;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));

            await LoadWordsAsync();
            _ = LoadContextUnitsAsync();
            return count;
        }
        finally
        {
            _isDeletingWords = false;
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (IsLoading)
            {
                IsLoading = false;
            }
        }
    }

    private bool _isMarkingStatus = false;

    [RelayCommand]
    public async Task MarkWordStatusAsync(bool remembered)
    {
        if (SelectedWord == null || _isMarkingStatus) return;
        _isMarkingStatus = true;
        try
        {
            var wordId = SelectedWord.Id;
            var term = SelectedWord.Term;
            await _reviewService.MarkStatusQuickAsync(wordId, remembered);

            var newStatus = remembered ? LearningStatus.Remembered : LearningStatus.NotRemembered;
            var updatedWord = SelectedWord with { LearningStatus = newStatus };

            int idx = -1;
            for (int i = 0; i < Words.Count; i++)
            {
                if (Words[i].Id == wordId)
                {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0)
            {
                Words[idx] = updatedWord;
            }
            SelectedWord = updatedWord;
            _ = LoadContextUnitsAsync();

            StatusMessage = remembered
                ? string.Format(_localizationService.GetString("Vocab_Status_MarkedRemembered"), term)
                : string.Format(_localizationService.GetString("Vocab_Status_MarkedNotRemembered"), term);
        }
        finally
        {
            _isMarkingStatus = false;
        }
    }

    [RelayCommand]
    public async Task PlayPronunciationAsync(string? accent = null)
    {
        if (SelectedWord == null) return;
        var chosenAccent = !string.IsNullOrWhiteSpace(accent)
            ? accent
            : (_preferencesService.GetPreferences().PreferredAccent ?? "uk");
        await _audioPlayer.PlayPronunciationAsync(SelectedWord.Term, chosenAccent);
    }

    public async Task<string> ExportToJsonAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportToJsonAsync(wordIds, progress: progress);
    }

    public async Task<string> ExportToCsvAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportToCsvAsync(wordIds, progress: progress);
    }

    public async Task<string> ExportToTxtAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportToTxtAsync(wordIds, progress: progress);
    }

    public async Task<ImportResult> ImportSmartAsync(
        string content,
        string? fileNameHint = null,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null)
    {
        var result = await _exportImportService.ImportSmartAsync(content, fileNameHint, conflictResolution, progress: progress);
        await LoadWordsAsync();
        _ = LoadContextUnitsAsync();
        return result;
    }

    public async Task<ImportResult> ImportDataAsync(
        string content,
        string format,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null)
    {
        ImportResult result = format.ToLowerInvariant() switch
        {
            "json" => await _exportImportService.ImportFromJsonAsync(content, conflictResolution, progress: progress),
            "csv" => await _exportImportService.ImportFromCsvAsync(content, conflictResolution, progress: progress),
            _ => await _exportImportService.ImportFromTxtAsync(content, conflictResolution, progress: progress)
        };

        await LoadWordsAsync();
        _ = LoadContextUnitsAsync();
        return result;
    }

    public async Task<ImportResult> ImportDataAsync(string content, bool isJson, IProgress<ProgressReport>? progress = null)
    {
        return await ImportDataAsync(content, isJson ? "json" : "csv", ImportConflictResolution.Skip, progress);
    }

    public string GetSampleTemplate(string format)
    {
        return _exportImportService.GetSampleTemplate(format);
    }

    public void CheckTypoSuggestion()
    {
        var query = SearchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2 || Words.Count > 0)
        {
            TypoSuggestion = null;
            HasTypoSuggestion = false;
            ClosestSuggestions = Array.Empty<string>();
            HasClosestSuggestions = false;
            return;
        }

        if (_smartSearchService != null)
        {
            var candidates = _knownTerms.Count > 0 ? _knownTerms : ContextUnits.SelectMany(u => u.Words).Select(w => w.Term).Distinct();
            var closest = _smartSearchService.FindClosestWordSuggestions(query, candidates, maxSuggestions: 4);
            if (closest.Count > 0)
            {
                ClosestSuggestions = closest;
                HasClosestSuggestions = true;
                TypoSuggestion = closest[0];
                HasTypoSuggestion = true;
                return;
            }
        }

        var fallbackCandidates = _knownTerms.Count > 0 ? _knownTerms : ContextUnits.SelectMany(u => u.Words).Select(w => w.Term).Distinct();
        var suggestion = _fuzzySearchService.SuggestCorrection(query, fallbackCandidates);
        if (!string.IsNullOrEmpty(suggestion) && !string.Equals(suggestion, query, StringComparison.OrdinalIgnoreCase))
        {
            TypoSuggestion = suggestion;
            HasTypoSuggestion = true;
            ClosestSuggestions = new[] { suggestion };
            HasClosestSuggestions = true;
        }
        else
        {
            TypoSuggestion = null;
            HasTypoSuggestion = false;
            ClosestSuggestions = Array.Empty<string>();
            HasClosestSuggestions = false;
        }
    }

    [RelayCommand]
    public async Task ApplyTypoSuggestionAsync()
    {
        if (string.IsNullOrWhiteSpace(TypoSuggestion)) return;
        await ApplyClosestSuggestionAsync(TypoSuggestion);
    }

    [RelayCommand]
    public async Task ApplyClosestSuggestionAsync(string? suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion)) return;
        SearchTerm = suggestion;
        TypoSuggestion = null;
        HasTypoSuggestion = false;
        ClosestSuggestions = Array.Empty<string>();
        HasClosestSuggestions = false;
        CurrentPage = 1;
        await LoadWordsAsync();
        ApplyUnitFilter();
    }

    [RelayCommand]
    public async Task LoadWordSuggestionsAsync(WordDto? word = null)
    {
        var targetWord = word ?? SelectedWord;
        if (targetWord == null)
        {
            RelatedSuggestions = null;
            HasRelatedSuggestions = false;
            return;
        }

        _suggestionsCts?.Cancel();
        _suggestionsCts?.Dispose();
        _suggestionsCts = new CancellationTokenSource();
        var ct = _suggestionsCts.Token;

        try
        {
            IsLoadingSuggestions = true;
            var suggestions = await _vocabService.GetWordSuggestionsAsync(targetWord.Id, ct);
            if (ct.IsCancellationRequested) return;

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            RelatedSuggestions = suggestions;
            HasRelatedSuggestions = suggestions != null && suggestions.TotalSuggestionsCount > 0;
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyViewModel] LoadWordSuggestionsAsync error: {ex.Message}");
        }
        finally
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            IsLoadingSuggestions = false;
        }
    }

    [RelayCommand]
    public async Task SelectSuggestedWordAsync(WordSuggestionItemDto? suggestion)
    {
        if (suggestion == null) return;

        var existing = Words.FirstOrDefault(w => w.Id == suggestion.Word.Id);
        if (existing != null)
        {
            SelectedWord = existing;
            return;
        }

        var word = await _vocabService.GetWordByIdAsync(suggestion.Word.Id);
        if (word != null)
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (!Words.Any(w => w.Id == word.Id))
            {
                Words.Insert(0, word);
            }
            SelectedWord = word;
        }
    }
}
