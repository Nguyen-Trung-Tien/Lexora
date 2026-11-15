using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

/// <summary>
/// ViewModel for Conversation Phrases supporting responsive Master-Detail browsing,
/// accent/rate audio playback indicators, interactive Roleplay SM-2/FSRS practice,
/// and secure import/export.
/// </summary>
public partial class PhrasesViewModel(
    IConversationPhraseService phraseService,
    IAudioPlayerService audioService,
    ILocalizationService localizationService) : ObservableObject, IDisposable
{
    private const int PageSize = 20;

    private readonly List<ConversationPhraseDto> _allFilteredPhrases = [];
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchDebounceCts;
    private CancellationTokenSource? _audioIndicatorCts;
    private int _audioRequestToken;
    private bool _suppressFilterReload;
    private bool _disposed;

    public ILocalizationService Loc => localizationService;

    public ObservableCollection<ConversationPhraseDto> Phrases { get; } = [];
    public ObservableCollection<string> FilterCategories { get; } = ["All"];
    public ObservableCollection<string> EditorCategories { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPhrase))]
    public partial ConversationPhraseDto? SelectedPhrase { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPhrases))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSubmitting { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsErrorStatus { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedAccentCode))]
    public partial int SelectedAccentIndex { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedPlaybackRate))]
    public partial int SelectedRateIndex { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalPhraseCount { get; set; }

    [ObservableProperty]
    public partial int FavoritePhraseCount { get; set; }

    [ObservableProperty]
    public partial int DuePhraseCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageSummaryText))]
    public partial int TotalFilteredCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageSummaryText))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageSummaryText))]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial bool HasPreviousPage { get; set; }

    [ObservableProperty]
    public partial bool HasNextPage { get; set; }

    public bool HasSelectedPhrase => SelectedPhrase is not null;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool HasNoPhrases => Phrases.Count == 0 && !IsLoading;
    public string SelectedAccentCode => SelectedAccentIndex == 1 ? "uk" : "us";
    public double SelectedPlaybackRate => SelectedRateIndex switch
    {
        0 => 0.75,
        2 => 1.25,
        3 => 1.5,
        _ => 1.0
    };
    public string PageSummaryText => TotalFilteredCount == 0
        ? "0 / 0"
        : $"{CurrentPage} / {TotalPages} ({TotalFilteredCount})";

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (_disposed) return;

        await LoadCategoriesAsync();
        await RefreshPhrasesAsync(resetPage: true);
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await phraseService.GetCategoriesAsync();
            var previousFilter = SelectedCategory;

            _suppressFilterReload = true;
            try
            {
                FilterCategories.Clear();
                FilterCategories.Add("All");
                EditorCategories.Clear();

                foreach (var cat in categories)
                {
                    FilterCategories.Add(cat);
                    EditorCategories.Add(cat);
                }

                SelectedCategory = FilterCategories.Contains(previousFilter)
                    ? previousFilter
                    : "All";
            }
            finally
            {
                _suppressFilterReload = false;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorLoad")}: {ex.Message}", isError: true);
        }
    }

    [RelayCommand]
    public async Task PlayPromptAudioAsync(ConversationPhraseDto? phrase)
    {
        if (phrase is null || string.IsNullOrWhiteSpace(phrase.PromptSentence))
        {
            return;
        }

        await ToggleOrPlayAudioAsync(phrase, isPrompt: true, phrase.PromptSentence);
    }

    [RelayCommand]
    public async Task PlayResponseAudioAsync(ConversationPhraseDto? phrase)
    {
        if (phrase is null || string.IsNullOrWhiteSpace(phrase.SampleResponse))
        {
            return;
        }

        await ToggleOrPlayAudioAsync(phrase, isPrompt: false, phrase.SampleResponse);
    }

    [RelayCommand]
    public void StopAudio()
    {
        Interlocked.Increment(ref _audioRequestToken);
        CancelAudioIndicatorCts();
        ClearAllPlayingIndicators();
        audioService.Stop();
    }

    [RelayCommand]
    public void NextPage()
    {
        if (!HasNextPage) return;
        CurrentPage++;
        RefreshCurrentPageSlice();
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (!HasPreviousPage) return;
        CurrentPage--;
        RefreshCurrentPageSlice();
    }

    private async Task ToggleOrPlayAudioAsync(ConversationPhraseDto phrase, bool isPrompt, string text)
    {
        bool alreadyPlayingSame = isPrompt ? phrase.IsPlayingPrompt : phrase.IsPlayingResponse;
        if (alreadyPlayingSame)
        {
            StopAudio();
            return;
        }

        int token = Interlocked.Increment(ref _audioRequestToken);
        CancelAudioIndicatorCts();
        ClearAllPlayingIndicators();

        var cts = new CancellationTokenSource();
        _audioIndicatorCts = cts;

        if (isPrompt)
        {
            phrase.IsPlayingPrompt = true;
        }
        else
        {
            phrase.IsPlayingResponse = true;
        }

        try
        {
            await audioService.PlayPronunciationAsync(text, SelectedAccentCode, SelectedPlaybackRate);
            int estimatedMs = Math.Clamp((int)(text.Length * 62 / SelectedPlaybackRate) + 650, 1100, 15000);
            await Task.Delay(estimatedMs, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorAudio")}: {ex.Message}", isError: true);
        }
        finally
        {
            if (token == Volatile.Read(ref _audioRequestToken))
            {
                phrase.IsPlayingPrompt = false;
                phrase.IsPlayingResponse = false;
            }
        }
    }

    private void ClearAllPlayingIndicators()
    {
        foreach (var item in _allFilteredPhrases)
        {
            item.IsPlayingPrompt = false;
            item.IsPlayingResponse = false;
        }

        foreach (var item in Phrases)
        {
            item.IsPlayingPrompt = false;
            item.IsPlayingResponse = false;
        }

        if (SelectedPhrase is not null)
        {
            SelectedPhrase.IsPlayingPrompt = false;
            SelectedPhrase.IsPlayingResponse = false;
        }

        if (CurrentRoleplayCard?.Phrase is not null)
        {
            CurrentRoleplayCard.Phrase.IsPlayingPrompt = false;
            CurrentRoleplayCard.Phrase.IsPlayingResponse = false;
        }
    }

    private void CancelAudioIndicatorCts()
    {
        try
        {
            _audioIndicatorCts?.Cancel();
            _audioIndicatorCts?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _audioIndicatorCts = null;
        }
    }

    private void RefreshCurrentPageSlice()
    {
        TotalFilteredCount = _allFilteredPhrases.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalFilteredCount / (double)PageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);
        HasPreviousPage = CurrentPage > 1;
        HasNextPage = CurrentPage < TotalPages;

        var slice = _allFilteredPhrases
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        for (int i = 0; i < slice.Count; i++)
        {
            var target = slice[i];
            if (i < Phrases.Count)
            {
                if (Phrases[i].Id == target.Id)
                {
                    Phrases[i].UpdateFrom(target);
                }
                else
                {
                    Phrases[i] = target;
                }
            }
            else
            {
                Phrases.Add(target);
            }
        }

        while (Phrases.Count > slice.Count)
        {
            Phrases.RemoveAt(Phrases.Count - 1);
        }

        if (SelectedPhrase is null || !Phrases.Any(p => p.Id == SelectedPhrase.Id))
        {
            SelectedPhrase = Phrases.FirstOrDefault();
        }
        else
        {
            var match = Phrases.First(p => p.Id == SelectedPhrase.Id);
            if (!ReferenceEquals(SelectedPhrase, match))
            {
                SelectedPhrase = match;
            }
        }

        OnPropertyChanged(nameof(HasNoPhrases));
        OnPropertyChanged(nameof(PageSummaryText));
    }

    private void UpdateSummaryCounters()
    {
        TotalPhraseCount = _allFilteredPhrases.Count;
        FavoritePhraseCount = _allFilteredPhrases.Count(p => p.IsFavorite);
        DuePhraseCount = _allFilteredPhrases.Count(p => p.IsDueForReview);
    }

    private void SetStatus(string message, bool isError = false)
    {
        IsErrorStatus = isError;
        StatusMessage = message;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        CancelAudioIndicatorCts();
        audioService.Stop();
        GC.SuppressFinalize(this);
    }
}
