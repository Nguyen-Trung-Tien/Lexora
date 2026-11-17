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
            RefreshUnitPagination(resetToFirstPage: true);
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
        RefreshUnitPagination(resetToFirstPage: true);
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

    private bool _isSavingWord = false;

    [ObservableProperty]
    public partial bool IsSubmitting { get; set; } = false;

    [RelayCommand]
    public async Task SaveWordAsync(WordCreateOrUpdateDto dto)
    {
        if (_isSavingWord || IsSubmitting) return;
        _isSavingWord = true;
        IsSubmitting = true;
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
            IsSubmitting = false;
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
            RefreshUnitPagination();
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
    public async Task PlayPronunciationAsync(string? accent = null)
    {
        if (SelectedWord == null) return;
        var chosenAccent = !string.IsNullOrWhiteSpace(accent)
            ? accent
            : (_preferencesService.GetPreferences().PreferredAccent ?? "uk");
        await _audioPlayer.PlayPronunciationAsync(SelectedWord.Term, chosenAccent);
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

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        GC.SuppressFinalize(this);
    }
}
