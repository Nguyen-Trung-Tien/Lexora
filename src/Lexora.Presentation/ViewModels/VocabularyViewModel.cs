using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class VocabularyViewModel : ObservableObject, IDisposable
{
    private readonly IVocabularyService _vocabService;
    private readonly IReviewService _reviewService;
    private readonly IExportImportService _exportImportService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly IFuzzySearchService _fuzzySearchService;
    private readonly ISmartSearchService? _smartSearchService;
    private readonly ILocalizationService _localizationService;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IHomophoneService? _homophoneService;
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
    public partial HomophoneGroupDto? RelatedHomophoneGroup { get; set; }

    [ObservableProperty]
    public partial bool HasRelatedHomophoneGroup { get; set; }

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

    public ObservableCollection<Guid> SelectedContextUnitIds { get; } = new();

    public int SelectedContextUnitCount => SelectedContextUnitIds.Count;
    public bool HasContextUnitSelection => SelectedContextUnitIds.Count > 0;

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
        ISmartSearchService? smartSearchService = null,
        IHomophoneService? homophoneService = null)
    {
        _vocabService = vocabService;
        _reviewService = reviewService;
        _exportImportService = exportImportService;
        _audioPlayer = audioPlayer;
        _fuzzySearchService = fuzzySearchService;
        _localizationService = localizationService;
        _preferencesService = preferencesService;
        _smartSearchService = smartSearchService;
        _homophoneService = homophoneService;
        // Must be captured on the UI thread (constructor is always called from UI thread).
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? App.Current?.UIDispatcherQueue
            ?? App.Current?.MainWindow?.DispatcherQueue;
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
        if (string.IsNullOrEmpty(query))
        {
            foreach (var unit in ContextUnits)
            {
                FilteredContextUnits.Add(unit);
            }
            return;
        }

        var normQuery = Lexora.Domain.Common.SearchTextHelper.Normalize(query);
        var list = ContextUnits.Where(u =>
            (!string.IsNullOrEmpty(u.Name) && Lexora.Domain.Common.SearchTextHelper.Normalize(u.Name).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(u.Description) && Lexora.Domain.Common.SearchTextHelper.Normalize(u.Description).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
            (u.Words != null && u.Words.Any(w =>
                (!string.IsNullOrEmpty(w.Term) && Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(w.PrimaryDefinition) && Lexora.Domain.Common.SearchTextHelper.Normalize(w.PrimaryDefinition).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
                (w.Meanings != null && w.Meanings.Any(m =>
                    (!string.IsNullOrEmpty(m.DefinitionVi) && Lexora.Domain.Common.SearchTextHelper.Normalize(m.DefinitionVi).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(m.DefinitionEn) && Lexora.Domain.Common.SearchTextHelper.Normalize(m.DefinitionEn).Contains(normQuery, StringComparison.OrdinalIgnoreCase)))))));

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
                if (!_knownTerms.Contains(w.Term, StringComparer.OrdinalIgnoreCase))
                {
                    _knownTerms.Add(w.Term);
                }
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

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var normSearch = Lexora.Domain.Common.SearchTextHelper.Normalize(SearchTerm);
                bool hasDirectTermOrDefMatch = Words.Any(w =>
                    Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).StartsWith(normSearch, StringComparison.OrdinalIgnoreCase) ||
                    Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).Equals(normSearch, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(w.PrimaryDefinition) && Lexora.Domain.Common.SearchTextHelper.Normalize(w.PrimaryDefinition).Contains(normSearch, StringComparison.OrdinalIgnoreCase)));

                if (TotalWordCount == 0 || !hasDirectTermOrDefMatch)
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
                Words.Insert(0, saved);
                TotalWordCount++;
                SelectedWord = saved;
                StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_WordAdded"), saved.Term);
            }
            else
            {
                saved = await _vocabService.UpdateWordAsync(dto);
                await _dispatcherQueue.EnsureOnUIThreadAsync();
                int idx = -1;
                for (int i = 0; i < Words.Count; i++)
                {
                    if (Words[i].Id == saved.Id)
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx >= 0)
                {
                    Words[idx] = saved;
                }
                else
                {
                    await LoadWordsAsync();
                }
                SelectedWord = saved;
                StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_WordUpdated"), saved.Term);
            }

            _ = LoadContextUnitsAsync();
            _ = LoadFiltersAsync();
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

    public async Task<TopicDto?> UpdateTopicAsync(Guid id, TopicCreateOrUpdateDto dto)
    {
        try
        {
            var updated = await _vocabService.UpdateTopicAsync(id, dto);
            await LoadFiltersAsync();
            await LoadContextUnitsAsync();
            StatusMessage = string.Format(_localizationService.GetString("Vocab_TopicUpdated"), updated.Name);
            return updated;
        }
        catch
        {
            throw;
        }
    }

    public async Task DeleteTopicAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty) return;

            var topic = AvailableTopics.FirstOrDefault(t => t.Id == id);
            string topicName = topic?.Name ?? string.Empty;

            await _vocabService.DeleteTopicAsync(id);

            await _dispatcherQueue.EnsureOnUIThreadAsync();

            for (int i = ContextUnits.Count - 1; i >= 0; i--)
            {
                if (ContextUnits[i].Id == id) ContextUnits.RemoveAt(i);
            }
            for (int i = FilteredContextUnits.Count - 1; i >= 0; i--)
            {
                if (FilteredContextUnits[i].Id == id) FilteredContextUnits.RemoveAt(i);
            }
            for (int i = AvailableTopics.Count - 1; i >= 0; i--)
            {
                if (AvailableTopics[i].Id == id) AvailableTopics.RemoveAt(i);
            }

            if (SelectedTopicId.HasValue && SelectedTopicId.Value == id)
            {
                SelectedTopicId = null;
                await LoadWordsAsync();
            }

            SelectedContextUnitIds.Remove(id);
            OnPropertyChanged(nameof(SelectedContextUnitCount));
            OnPropertyChanged(nameof(HasContextUnitSelection));

            await LoadFiltersAsync();
            await LoadContextUnitsAsync();

            if (!string.IsNullOrEmpty(topicName))
            {
                StatusMessage = string.Format(_localizationService.GetString("Vocab_TopicDeleted"), topicName);
            }
        }
        catch
        {
            throw;
        }
    }

    private bool _isDeletingUnits = false;

    public async Task<int> DeleteSelectedContextUnitsAsync(
        IEnumerable<Guid>? ids = null,
        CancellationToken cancellationToken = default)
    {
        var targetIds = ids?.Where(id => id != Guid.Empty).Distinct().ToList()
            ?? SelectedContextUnitIds.Where(id => id != Guid.Empty).ToList();
        if (targetIds.Count == 0 || _isDeletingUnits) return 0;

        _isDeletingUnits = true;
        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;
        StatusMessage = _localizationService.GetString("Vocab_BulkDeleteTopicsProgress");

        try
        {
            int count = await _vocabService.DeleteTopicsAsync(targetIds, cancellationToken);

            await _dispatcherQueue.EnsureOnUIThreadAsync();

            var deletedSet = new HashSet<Guid>(targetIds);

            // Immediate removal from in-memory collections for instant UI update
            for (int i = ContextUnits.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(ContextUnits[i].Id))
                {
                    ContextUnits.RemoveAt(i);
                }
            }

            for (int i = FilteredContextUnits.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(FilteredContextUnits[i].Id))
                {
                    FilteredContextUnits.RemoveAt(i);
                }
            }

            for (int i = AvailableTopics.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(AvailableTopics[i].Id))
                {
                    AvailableTopics.RemoveAt(i);
                }
            }

            // If the filtered topic was deleted, reset filter immediately
            if (SelectedTopicId.HasValue && deletedSet.Contains(SelectedTopicId.Value))
            {
                SelectedTopicId = null;
                await LoadWordsAsync();
            }

            SelectedContextUnitIds.Clear();
            IsSelectionMode = false;
            OnPropertyChanged(nameof(SelectedContextUnitCount));
            OnPropertyChanged(nameof(HasContextUnitSelection));

            await LoadFiltersAsync();
            await LoadContextUnitsAsync();

            StatusMessage = string.Format(_localizationService.GetString("Vocab_BulkDeleteTopicsSuccess"), count);
            return count;
        }
        finally
        {
            _isDeletingUnits = false;
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (IsLoading)
            {
                IsLoading = false;
            }
        }
    }

    public async Task AssignWordsToTopicAsync(Guid topicId, IEnumerable<Guid> wordIds)
    {
        try
        {
            var idList = wordIds.Distinct().ToList();
            if (idList.Count == 0) return;

            await _vocabService.AssignWordsToTopicAsync(topicId, idList);
            await LoadWordsAsync();
            await LoadContextUnitsAsync();
        }
        catch
        {
            throw;
        }
    }

    public async Task UnassignWordsFromTopicAsync(Guid topicId, IEnumerable<Guid> wordIds)
    {
        try
        {
            var idList = wordIds.Distinct().ToList();
            if (idList.Count == 0) return;

            await _vocabService.UnassignWordsFromTopicAsync(topicId, idList);
            await LoadWordsAsync();
            await LoadContextUnitsAsync();
        }
        catch
        {
            throw;
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
            if (word != null)
            {
                Words.Remove(word);
                TotalWordCount = Math.Max(0, TotalWordCount - 1);
            }
            if (SelectedWord?.Id == id)
            {
                SelectedWord = Words.FirstOrDefault();
            }

            await _vocabService.DeleteWordAsync(id);

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (!string.IsNullOrEmpty(term))
            {
                StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_WordDeleted"), term);
            }

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
            SelectedContextUnitIds.Clear();
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedContextUnitCount));
        OnPropertyChanged(nameof(HasContextUnitSelection));
    }

    [RelayCommand]
    public void SelectAllContextUnits()
    {
        SelectedContextUnitIds.Clear();
        foreach (var unit in FilteredContextUnits)
        {
            if (unit.Id != Guid.Empty)
            {
                SelectedContextUnitIds.Add(unit.Id);
            }
        }
        OnPropertyChanged(nameof(SelectedContextUnitCount));
        OnPropertyChanged(nameof(HasContextUnitSelection));
    }

    [RelayCommand]
    public void DeselectAllContextUnits()
    {
        SelectedContextUnitIds.Clear();
        OnPropertyChanged(nameof(SelectedContextUnitCount));
        OnPropertyChanged(nameof(HasContextUnitSelection));
    }

    public void ToggleContextUnitSelection(Guid id)
    {
        if (id == Guid.Empty) return;
        if (SelectedContextUnitIds.Contains(id))
        {
            SelectedContextUnitIds.Remove(id);
        }
        else
        {
            SelectedContextUnitIds.Add(id);
        }
        OnPropertyChanged(nameof(SelectedContextUnitCount));
        OnPropertyChanged(nameof(HasContextUnitSelection));
    }

    public bool IsContextUnitSelected(Guid id) => SelectedContextUnitIds.Contains(id);

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

    public async Task<int> DeleteSelectedWordsAsync(
        IEnumerable<Guid>? ids = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        var targetIds = ids?.Distinct().ToList() ?? SelectedWordIds.ToList();
        if (targetIds.Count == 0 || _isDeletingWords) return 0;
        _isDeletingWords = true;
        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;
        StatusMessage = _localizationService.GetString("Vocab_BulkDeleteProgress");

        try
        {
            int count = await Task.Run(
                () => _vocabService.DeleteWordsAsync(targetIds, cancellationToken, progress),
                cancellationToken);

            await _dispatcherQueue.EnsureOnUIThreadAsync();

            var deletedSet = new HashSet<Guid>(targetIds);
            for (int i = Words.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(Words[i].Id))
                {
                    Words.RemoveAt(i);
                }
            }

            TotalWordCount = Math.Max(0, TotalWordCount - count);
            if (SelectedWord != null && deletedSet.Contains(SelectedWord.Id))
            {
                SelectedWord = Words.FirstOrDefault();
            }

            SelectedWordIds.Clear();
            IsSelectionMode = false;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));

            if (Words.Count == 0 && CurrentPage > 1)
            {
                CurrentPage--;
                await LoadWordsAsync();
            }

            await LoadContextUnitsAsync();
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

    public async Task<string> ExportVocabularyOnlyJsonAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportVocabularyOnlyJsonAsync(wordIds, progress: progress);
    }

    public async Task<string> ExportVocabularyOnlyCsvAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportVocabularyOnlyCsvAsync(wordIds, progress: progress);
    }

    public async Task<string> ExportVocabularyOnlyTxtAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportVocabularyOnlyTxtAsync(wordIds, progress: progress);
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

    public async Task CheckTypoSuggestionAsync(CancellationToken cancellationToken = default)
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

        var closest = await GetClosestSuggestionsAsync(query, maxSuggestions: 4, cancellationToken);
        if (closest.Count > 0)
        {
            ClosestSuggestions = closest;
            HasClosestSuggestions = true;
            TypoSuggestion = closest[0];
            HasTypoSuggestion = true;
        }
        else
        {
            TypoSuggestion = null;
            HasTypoSuggestion = false;
            ClosestSuggestions = Array.Empty<string>();
            HasClosestSuggestions = false;
        }
    }

    public void CheckTypoSuggestion() => _ = CheckTypoSuggestionAsync();

    public async Task<IReadOnlyList<string>> GetClosestSuggestionsAsync(string query, int maxSuggestions = 4, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return Array.Empty<string>();

        try
        {
            var suggestions = await _vocabService.GetClosestSuggestionsAsync(query, maxSuggestions, cancellationToken);
            if (suggestions.Count > 0) return suggestions;
        }
        catch
        {
            // Fallback to in-memory candidates
        }

        var fallbackCandidates = _knownTerms.Count > 0 ? _knownTerms : ContextUnits.SelectMany(u => u.Words).Select(w => w.Term).Distinct();
        var suggestion = _fuzzySearchService.SuggestCorrection(query, fallbackCandidates);
        return !string.IsNullOrEmpty(suggestion) && !string.Equals(suggestion, query, StringComparison.OrdinalIgnoreCase)
            ? new[] { suggestion }
            : Array.Empty<string>();
    }

    public IReadOnlyList<string> GetClosestSuggestions(string query, int maxSuggestions = 4)
    {
        return GetClosestSuggestionsAsync(query, maxSuggestions).GetAwaiter().GetResult();
    }

    public async Task<IReadOnlyList<WordDto>> SearchWordSuggestionsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<WordDto>();
        var results = await _vocabService.SearchWordsAsync(search: query, pageSize: 12, cancellationToken: cancellationToken);
        var normQuery = Lexora.Domain.Common.SearchTextHelper.Normalize(query);

        return results
            .OrderByDescending(w => Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).Equals(normQuery, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(w => Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(w => w.Meanings != null && w.Meanings.Any(m => Lexora.Domain.Common.SearchTextHelper.Normalize(m.DefinitionVi).StartsWith(normQuery, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(w => w.Term.Length)
            .Take(6)
            .ToList();
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

        if (TotalWordCount == 0 && (SelectedLevel != CefrLevel.None || SelectedPartOfSpeech != null || SelectedTopicId != null || SelectedExamType != null || SelectedStatus != null || SelectedQuickFilter != "All"))
        {
            SelectedLevel = CefrLevel.None;
            SelectedPartOfSpeech = null;
            SelectedTopicId = null;
            SelectedExamType = null;
            SelectedStatus = null;
            SelectedQuickFilter = "All";
            CurrentPage = 1;
            await LoadWordsAsync();
            ApplyUnitFilter();
        }

        // Auto-select the word in the list so details pane updates immediately
        if (Words.Count > 0)
        {
            var match = Words.FirstOrDefault(w => w.Term.Equals(suggestion, StringComparison.OrdinalIgnoreCase))
                ?? Words.FirstOrDefault(w => w.Term.StartsWith(suggestion, StringComparison.OrdinalIgnoreCase))
                ?? Words[0];
            SelectedWord = match;
        }
    }

    [RelayCommand]
    public async Task ApplySearchTermAsync(string term)
    {
        await ApplyClosestSuggestionAsync(term);
    }

    public async Task SelectWordByIdAsync(Guid wordId)
    {
        if (wordId == Guid.Empty) return;

        if (Words.Count == 0)
        {
            await LoadWordsAsync();
        }

        var matched = Words.FirstOrDefault(w => w.Id == wordId);
        if (matched != null)
        {
            SelectedWord = matched;
        }
        else
        {
            var word = await _vocabService.GetWordByIdAsync(wordId);
            if (word != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
                Words.Insert(0, word);
                SelectedWord = word;
            }
        }
    }

    [RelayCommand]
    public async Task LoadWordSuggestionsAsync(WordDto? word = null)
    {
        var targetWord = word ?? SelectedWord;
        if (targetWord == null)
        {
            RelatedSuggestions = null;
            HasRelatedSuggestions = false;
            RelatedHomophoneGroup = null;
            HasRelatedHomophoneGroup = false;
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

            HomophoneGroupDto? matchedHomophone = null;
            if (_homophoneService != null && !string.IsNullOrWhiteSpace(targetWord.Term))
            {
                var groups = await _homophoneService.SearchAsync(targetWord.Term, ct);
                matchedHomophone = groups.FirstOrDefault(g => g.Items.Any(i => i.Term.Equals(targetWord.Term, StringComparison.OrdinalIgnoreCase)));
            }
            if (ct.IsCancellationRequested) return;

            await _dispatcherQueue.EnsureOnUIThreadAsync();
            RelatedSuggestions = suggestions;
            HasRelatedSuggestions = suggestions != null && suggestions.TotalSuggestionsCount > 0;
            RelatedHomophoneGroup = matchedHomophone;
            HasRelatedHomophoneGroup = matchedHomophone != null;
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

    public void Dispose()
    {
        _suggestionsCts?.Cancel();
        _suggestionsCts?.Dispose();
        _suggestionsCts = null;
        GC.SuppressFinalize(this);
    }
}
