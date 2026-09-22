using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class VocabularyViewModel : ObservableObject
{
    private readonly IVocabularyService _vocabService;
    private readonly IReviewService _reviewService;
    private readonly IExportImportService _exportImportService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly IFuzzySearchService _fuzzySearchService;
    private readonly ILocalizationService _localizationService;
    private readonly List<string> _knownTerms = new();

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial string SearchTerm { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? TypoSuggestion { get; set; }

    [ObservableProperty]
    public partial bool HasTypoSuggestion { get; set; }

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
        ILocalizationService localizationService)
    {
        _vocabService = vocabService;
        _reviewService = reviewService;
        _exportImportService = exportImportService;
        _audioPlayer = audioPlayer;
        _fuzzySearchService = fuzzySearchService;
        _localizationService = localizationService;
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
        var topics = await _vocabService.GetAllTopicsAsync();
        AvailableTopics.Clear();
        foreach (var t in topics)
        {
            AvailableTopics.Add(t);
        }

        var tags = await _vocabService.GetAllExamTagsAsync();
        AvailableExamTags.Clear();
        foreach (var et in tags)
        {
            AvailableExamTags.Add(et);
        }
    }

    [RelayCommand]
    public async Task SelectQuickFilterAsync(string filter)
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

    private CancellationTokenSource? _loadCts;

    [RelayCommand]
    public async Task LoadWordsAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        IsLoading = true;
        try
        {
            var results = await _vocabService.SearchWordsAsync(
                search: string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                pos: SelectedPartOfSpeech,
                topicId: SelectedTopicId,
                examType: SelectedExamType,
                status: SelectedStatus,
                sortBy: SelectedQuickFilter == "DueToday" ? "nextreview" : SortBy,
                isAscending: SelectedQuickFilter == "DueToday" || IsAscending,
                pageSize: 200,
                cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

            var filteredResults = results.AsEnumerable();
            if (SelectedQuickFilter == "DueToday")
            {
                filteredResults = filteredResults.Where(w => w.IsDueNow);
            }
            else if (SelectedQuickFilter == "Weak")
            {
                filteredResults = filteredResults.Where(w => w.Lapses > 0 || w.LearningStatus == LearningStatus.NotRemembered);
            }

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

            Words.Clear();
            foreach (var w in filteredResults)
            {
                Words.Add(w);
            }

            if (Words.Count > 0 && (SelectedWord == null || !Words.Any(w => w.Id == SelectedWord.Id)))
            {
                SelectedWord = Words[0];
            }
            else if (Words.Count == 0)
            {
                SelectedWord = null;
            }

            // Update known terms cache
            foreach (var w in Words)
            {
                if (!_knownTerms.Contains(w.Term, StringComparer.OrdinalIgnoreCase))
                {
                    _knownTerms.Add(w.Term);
                }
            }

            CheckTypoSuggestion();
        }
        catch (OperationCanceledException)
        {
            // Ignore cancelled queries
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    public async Task SaveWordAsync(WordCreateOrUpdateDto dto)
    {
        if (dto.Id == null)
        {
            var created = await _vocabService.CreateWordAsync(dto);
            StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_WordAdded"), created.Term);
        }
        else
        {
            var updated = await _vocabService.UpdateWordAsync(dto);
            StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_WordUpdated"), updated.Term);
        }

        await LoadWordsAsync();
        _ = LoadContextUnitsAsync();
        SelectedWord = Words.FirstOrDefault(w => w.Term.Equals(dto.Term, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    public async Task<TopicDto> CreateTopicAsync(TopicCreateOrUpdateDto dto)
    {
        var created = await _vocabService.CreateTopicAsync(dto);
        await LoadFiltersAsync();
        await LoadContextUnitsAsync();
        StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_TopicCreated"), created.Name);
        return created;
    }


    [RelayCommand]
    public async Task DeleteWordAsync(Guid id)
    {
        var word = Words.FirstOrDefault(w => w.Id == id);
        if (word != null)
        {
            await _vocabService.DeleteWordAsync(id);
            StatusMessage = string.Format(_localizationService.GetString("Vocab_Status_WordDeleted"), word.Term);
            await LoadWordsAsync();
            _ = LoadContextUnitsAsync();
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

    public async Task<int> DeleteSelectedWordsAsync()
    {
        if (SelectedWordIds.Count == 0) return 0;

        var idsToDelete = SelectedWordIds.ToList();
        int count = await _vocabService.DeleteWordsAsync(idsToDelete);
        SelectedWordIds.Clear();
        IsSelectionMode = false;
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));

        await LoadWordsAsync();
        _ = LoadContextUnitsAsync();
        return count;
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
    public async Task PlayPronunciationAsync(string accent)
    {
        if (SelectedWord == null) return;
        await _audioPlayer.PlayPronunciationAsync(SelectedWord.Term, accent);
    }

    public async Task<string> ExportToJsonAsync(IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportToJsonAsync(progress: progress);
    }

    public async Task<string> ExportToCsvAsync(IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportToCsvAsync(progress: progress);
    }

    public async Task<string> ExportToTxtAsync(IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportToTxtAsync(progress: progress);
    }

    public async Task<ImportResult> ImportDataAsync(string content, string format, IProgress<ProgressReport>? progress = null)
    {
        ImportResult result = format.ToLowerInvariant() switch
        {
            "json" => await _exportImportService.ImportFromJsonAsync(content, progress: progress),
            "csv" => await _exportImportService.ImportFromCsvAsync(content, progress: progress),
            _ => await _exportImportService.ImportFromTxtAsync(content, progress: progress)
        };

        await LoadWordsAsync();
        _ = LoadContextUnitsAsync();
        return result;
    }

    public async Task<ImportResult> ImportDataAsync(string content, bool isJson, IProgress<ProgressReport>? progress = null)
    {
        return await ImportDataAsync(content, isJson ? "json" : "csv", progress);
    }

    public void CheckTypoSuggestion()
    {
        var query = SearchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3 || Words.Count > 0)
        {
            TypoSuggestion = null;
            HasTypoSuggestion = false;
            return;
        }

        var candidates = _knownTerms.Count > 0 ? _knownTerms : ContextUnits.SelectMany(u => u.Words).Select(w => w.Term).Distinct();
        var suggestion = _fuzzySearchService.SuggestCorrection(query, candidates);
        if (!string.IsNullOrEmpty(suggestion) && !string.Equals(suggestion, query, StringComparison.OrdinalIgnoreCase))
        {
            TypoSuggestion = suggestion;
            HasTypoSuggestion = true;
        }
        else
        {
            TypoSuggestion = null;
            HasTypoSuggestion = false;
        }
    }

    [RelayCommand]
    public async Task ApplyTypoSuggestionAsync()
    {
        if (string.IsNullOrWhiteSpace(TypoSuggestion)) return;
        SearchTerm = TypoSuggestion;
        TypoSuggestion = null;
        HasTypoSuggestion = false;
        await LoadWordsAsync();
        ApplyUnitFilter();
    }
}
