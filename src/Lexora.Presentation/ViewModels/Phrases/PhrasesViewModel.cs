using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;

namespace Lexora.Presentation.ViewModels;

public partial class PhrasesViewModel : ObservableObject, IDisposable
{
    private readonly IConversationPhraseService _phraseService;
    private readonly ILocalizationService _localizationService;
    private readonly IAudioPlayerService? _audioPlayerService;
    private readonly DispatcherQueue? _dispatcherQueue;

    private readonly List<ConversationPhraseDto> _allFilteredPhrases = [];

    public ObservableCollection<ConversationPhraseDto> Phrases { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyState))]
    public partial bool IsLoading { get; set; } = true;

    public bool IsEmptyState => !IsLoading && _allFilteredPhrases.Count == 0;

    [ObservableProperty]
    public partial bool IsSubmitting { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial CefrLevel? SelectedLevel { get; set; }

    [ObservableProperty]
    public partial bool OnlyFavorites { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPhrase))]
    public partial ConversationPhraseDto? SelectedPhrase { get; set; }

    public bool HasSelectedPhrase => SelectedPhrase != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    [NotifyPropertyChangedFor(nameof(ItemsRangeDisplay))]
    [NotifyPropertyChangedFor(nameof(CountDisplayInfo))]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    [NotifyPropertyChangedFor(nameof(ItemsRangeDisplay))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    [NotifyPropertyChangedFor(nameof(ItemsRangeDisplay))]
    public partial int PageSize { get; set; } = 5;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    public string PageDisplayInfo => $"{CurrentPage} / {TotalPages}";

    public string ItemsRangeDisplay
    {
        get
        {
            if (TotalCount == 0) return string.Empty;
            int start = ((CurrentPage - 1) * PageSize) + 1;
            int end = Math.Min(CurrentPage * PageSize, TotalCount);
            return string.Format(_localizationService.GetString("Phrases_PageSummary"), start, end, TotalCount);
        }
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        RefreshCurrentPageSlice();
    }

    public string CountDisplayInfo => string.Format(_localizationService.GetString("Phrases_TotalCount"), TotalCount);

    public PhrasesViewModel(
        IConversationPhraseService phraseService,
        ILocalizationService localizationService,
        IAudioPlayerService? audioPlayerService = null)
    {
        _phraseService = phraseService;
        _localizationService = localizationService;
        _audioPlayerService = audioPlayerService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await LoadCategoriesAsync();
            await RefreshPhrasesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] LoadData error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var cats = await _phraseService.GetCategoriesAsync();
            Categories.Clear();
            Categories.Add(_localizationService.GetString("Phrases_FilterAllCategories"));
            foreach (var c in cats)
            {
                Categories.Add(c);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] LoadCategories error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task RefreshPhrasesAsync()
    {
        IsLoading = true;
        try
        {
            var cat = SelectedCategory;
            if (string.Equals(cat, _localizationService.GetString("Phrases_FilterAllCategories"), StringComparison.OrdinalIgnoreCase))
            {
                cat = null;
            }

            var filter = new ConversationPhraseFilterDto
            {
                Category = cat,
                CefrLevel = SelectedLevel,
                OnlyFavorites = OnlyFavorites ? true : null,
                SearchQuery = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim()
            };

            var list = await _phraseService.GetPhrasesAsync(filter);

            _allFilteredPhrases.Clear();
            _allFilteredPhrases.AddRange(list);

            TotalCount = _allFilteredPhrases.Count;
            CurrentPage = 1;
            RefreshCurrentPageSlice();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] RefreshPhrases error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshCurrentPageSlice()
    {
        Phrases.Clear();
        var slice = _allFilteredPhrases
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in slice)
        {
            Phrases.Add(item);
        }

        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(PageDisplayInfo));
        OnPropertyChanged(nameof(ItemsRangeDisplay));
        OnPropertyChanged(nameof(IsEmptyState));
    }

    [RelayCommand]
    public void FirstPage() => GoToPage(1);

    [RelayCommand]
    public void PreviousPage() => GoToPage(CurrentPage - 1);

    [RelayCommand]
    public void NextPage() => GoToPage(CurrentPage + 1);

    [RelayCommand]
    public void LastPage() => GoToPage(TotalPages);

    public void GoToPage(int page)
    {
        int target = Math.Clamp(page, 1, TotalPages);
        if (target == CurrentPage && Phrases.Count > 0) return;
        CurrentPage = target;
        RefreshCurrentPageSlice();
    }

    [RelayCommand]
    public void ChangePageSize(int newSize)
    {
        if (newSize <= 0 || newSize == PageSize) return;
        PageSize = newSize;
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync(ConversationPhraseDto? phrase)
    {
        if (phrase == null || IsSubmitting) return;

        IsSubmitting = true;
        try
        {
            var newStatus = await _phraseService.ToggleFavoriteAsync(phrase.Id);
            phrase.IsFavorite = newStatus;

            // If filtering only favorites and user unfavorited, remove from list
            if (OnlyFavorites && !newStatus)
            {
                _allFilteredPhrases.Remove(phrase);
                TotalCount = _allFilteredPhrases.Count;
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = Math.Max(1, TotalPages);
                }
                RefreshCurrentPageSlice();
            }
            else
            {
                var match = _allFilteredPhrases.FirstOrDefault(p => p.Id == phrase.Id);
                if (match != null) match.IsFavorite = newStatus;

                var index = Phrases.IndexOf(phrase);
                if (index >= 0)
                {
                    Phrases[index] = phrase;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] ToggleFavorite error: {ex.Message}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public async Task DeletePhraseAsync(ConversationPhraseDto? phrase)
    {
        if (phrase == null || IsSubmitting) return;

        IsSubmitting = true;
        try
        {
            await _phraseService.DeleteAsync(phrase.Id);
            _allFilteredPhrases.Remove(phrase);
            TotalCount = _allFilteredPhrases.Count;
            if (CurrentPage > TotalPages)
            {
                CurrentPage = Math.Max(1, TotalPages);
            }
            RefreshCurrentPageSlice();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] DeletePhrase error: {ex.Message}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public async Task PlayPromptAudioAsync(ConversationPhraseDto? phrase)
    {
        if (phrase == null || string.IsNullOrWhiteSpace(phrase.PromptSentence) || _audioPlayerService == null)
            return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(phrase.PromptSentence, "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] PlayPromptAudio error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task PlayResponseAudioAsync(ConversationPhraseDto? phrase)
    {
        if (phrase == null || string.IsNullOrWhiteSpace(phrase.SampleResponse) || _audioPlayerService == null)
            return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(phrase.SampleResponse, "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] PlayResponseAudio error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void CopyText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] CopyText error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ClearFiltersAsync()
    {
        SearchQuery = string.Empty;
        SelectedCategory = null;
        SelectedLevel = null;
        OnlyFavorites = false;
        await RefreshPhrasesAsync();
    }

    public void OnPhraseSaved(ConversationPhraseDto savedPhrase, bool isNew)
    {
        if (isNew)
        {
            _allFilteredPhrases.Insert(0, savedPhrase);
            TotalCount = _allFilteredPhrases.Count;
            CurrentPage = 1;
            RefreshCurrentPageSlice();
            if (!string.IsNullOrEmpty(savedPhrase.Category) && !Categories.Contains(savedPhrase.Category))
            {
                Categories.Add(savedPhrase.Category);
            }
        }
        else
        {
            var existing = _allFilteredPhrases.FirstOrDefault(p => p.Id == savedPhrase.Id);
            if (existing != null)
            {
                var idx = _allFilteredPhrases.IndexOf(existing);
                _allFilteredPhrases[idx] = savedPhrase;
            }
            var pageIdx = Phrases.IndexOf(existing ?? savedPhrase);
            if (pageIdx >= 0)
            {
                Phrases[pageIdx] = savedPhrase;
            }
        }
    }

    public void Dispose()
    {
        _allFilteredPhrases.Clear();
        Phrases.Clear();
        Categories.Clear();
    }
}
