using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class PhrasesViewModel : ObservableObject, IDisposable
{
    private readonly IConversationPhraseService _phraseService;
    private readonly ILocalizationService _localizationService;
    private readonly IAudioPlayerService? _audioPlayerService;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly EventHandler? _languageChangedHandler;

    private readonly List<ConversationPhraseDto> _allFilteredPhrases = [];
    private CancellationTokenSource? _loadCts;

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

        _languageChangedHandler = (_, _) =>
        {
            void RefreshTexts()
            {
                OnPropertyChanged(nameof(ItemsRangeDisplay));
                OnPropertyChanged(nameof(CountDisplayInfo));
            }

            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(RefreshTexts);
            }
            else
            {
                RefreshTexts();
            }
        };
        _localizationService.LanguageChanged += _languageChangedHandler;
    }

    [RelayCommand]
    public async Task LoadDataAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        try
        {
            await LoadCategoriesAsync(cancellationToken);
            await RefreshPhrasesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
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

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cats = await _phraseService.GetCategoriesAsync(cancellationToken);
            Categories.Clear();
            Categories.Add(_localizationService.GetString("Phrases_FilterAllCategories"));
            foreach (var c in cats)
            {
                Categories.Add(c);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] LoadCategories error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task RefreshPhrasesAsync(CancellationToken cancellationToken = default)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _loadCts.Token;

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

            var list = await _phraseService.GetPhrasesAsync(filter, token);
            token.ThrowIfCancellationRequested();

            _allFilteredPhrases.Clear();
            _allFilteredPhrases.AddRange(list);

            TotalCount = _allFilteredPhrases.Count;
            CurrentPage = 1;
            RefreshCurrentPageSlice();
        }
        catch (OperationCanceledException)
        {
            // Expected when superseded by a newer search/filter request
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
        var slice = _allFilteredPhrases
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        while (Phrases.Count > slice.Count)
        {
            Phrases.RemoveAt(Phrases.Count - 1);
        }

        for (int i = 0; i < slice.Count; i++)
        {
            if (i < Phrases.Count)
            {
                if (Phrases[i].Id != slice[i].Id)
                {
                    Phrases[i] = slice[i];
                }
            }
            else
            {
                Phrases.Add(slice[i]);
            }
        }

        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(PageDisplayInfo));
        OnPropertyChanged(nameof(ItemsRangeDisplay));
        OnPropertyChanged(nameof(IsEmptyState));
    }

    public void Dispose()
    {
        if (_languageChangedHandler != null)
        {
            _localizationService.LanguageChanged -= _languageChangedHandler;
        }
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        _allFilteredPhrases.Clear();
        Phrases.Clear();
        Categories.Clear();
    }
}
