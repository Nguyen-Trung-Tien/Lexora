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

    public ObservableCollection<WordDto> Words { get; } = new();
    public ObservableCollection<TopicDto> AvailableTopics { get; } = new();
    public ObservableCollection<ExamTagDto> AvailableExamTags { get; } = new();

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
    public async Task LoadWordsAsync()
    {
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
                pageSize: 200);

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
        finally
        {
            IsLoading = false;
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
        }
    }

    [RelayCommand]
    public async Task MarkWordStatusAsync(bool remembered)
    {
        if (SelectedWord == null) return;

        await _reviewService.MarkStatusQuickAsync(SelectedWord.Id, remembered);
        var currentId = SelectedWord.Id;
        await LoadWordsAsync();
        SelectedWord = Words.FirstOrDefault(w => w.Id == currentId);
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
        return result;
    }
}
