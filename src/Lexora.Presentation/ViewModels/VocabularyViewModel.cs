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

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial string SearchTerm { get; set; } = string.Empty;

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
    public partial string SortBy { get; set; } = "term";

    [ObservableProperty]
    public partial bool IsAscending { get; set; } = true;

    [ObservableProperty]
    public partial WordDto? SelectedWord { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsUnitViewMode { get; set; } = false;

    public ObservableCollection<WordDto> Words { get; } = new();
    public ObservableCollection<TopicDto> AvailableTopics { get; } = new();
    public ObservableCollection<ExamTagDto> AvailableExamTags { get; } = new();
    public ObservableCollection<ContextUnitDto> ContextUnits { get; } = new();
    public ObservableCollection<ContextUnitDto> FilteredContextUnits { get; } = new();

    public VocabularyViewModel(
        IVocabularyService vocabService,
        IReviewService reviewService,
        IExportImportService exportImportService,
        IAudioPlayerService audioPlayer)
    {
        _vocabService = vocabService;
        _reviewService = reviewService;
        _exportImportService = exportImportService;
        _audioPlayer = audioPlayer;
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
                u.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (u.Description != null && u.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                u.Words.Any(w => w.Term.Contains(query, StringComparison.OrdinalIgnoreCase)));

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
                sortBy: SortBy,
                isAscending: IsAscending,
                pageSize: 200,
                cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

            Words.Clear();
            foreach (var w in results)
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
            StatusMessage = $"Added '{created.Term}' successfully.";
        }
        else
        {
            var updated = await _vocabService.UpdateWordAsync(dto);
            StatusMessage = $"Updated '{updated.Term}' successfully.";
        }

        await LoadWordsAsync();
        _ = LoadContextUnitsAsync();
        SelectedWord = Words.FirstOrDefault(w => w.Term.Equals(dto.Term, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    public async Task DeleteWordAsync(Guid id)
    {
        var word = Words.FirstOrDefault(w => w.Id == id);
        if (word != null)
        {
            await _vocabService.DeleteWordAsync(id);
            StatusMessage = $"Deleted '{word.Term}'.";
            await LoadWordsAsync();
            _ = LoadContextUnitsAsync();
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
                ? $"✓ Đã đánh dấu '{term}' là Nhớ rồi."
                : $"✕ Đã đánh dấu '{term}' là Chưa nhớ.";
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

    public async Task<string> ExportToJsonAsync()
    {
        return await _exportImportService.ExportToJsonAsync();
    }

    public async Task<string> ExportToCsvAsync()
    {
        return await _exportImportService.ExportToCsvAsync();
    }

    public async Task<ImportResult> ImportDataAsync(string content, bool isJson)
    {
        ImportResult result;
        if (isJson)
        {
            result = await _exportImportService.ImportFromJsonAsync(content);
        }
        else
        {
            result = await _exportImportService.ImportFromCsvAsync(content);
        }

        await LoadWordsAsync();
        _ = LoadContextUnitsAsync();
        return result;
    }
}
