using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class PhrasesViewModel
{
    [ObservableProperty]
    public partial string SelectedCategory { get; set; } = "All";

    [ObservableProperty]
    public partial int SelectedCefrIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedRegisterIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedExamTargetIndex { get; set; }

    [ObservableProperty]
    public partial bool OnlyFavorites { get; set; }

    [ObservableProperty]
    public partial bool OnlyDueForReview { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    partial void OnSelectedCategoryChanged(string value)
    {
        if (_suppressFilterReload) return;
        _ = RefreshPhrasesAsync(resetPage: true);
    }

    partial void OnSelectedCefrIndexChanged(int value)
    {
        if (_suppressFilterReload) return;
        _ = RefreshPhrasesAsync(resetPage: true);
    }

    partial void OnSelectedRegisterIndexChanged(int value)
    {
        if (_suppressFilterReload) return;
        _ = RefreshPhrasesAsync(resetPage: true);
    }

    partial void OnSelectedExamTargetIndexChanged(int value)
    {
        if (_suppressFilterReload) return;
        _ = RefreshPhrasesAsync(resetPage: true);
    }

    partial void OnOnlyFavoritesChanged(bool value)
    {
        if (_suppressFilterReload) return;
        _ = RefreshPhrasesAsync(resetPage: true);
    }

    partial void OnOnlyDueForReviewChanged(bool value)
    {
        if (_suppressFilterReload) return;
        _ = RefreshPhrasesAsync(resetPage: true);
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (_suppressFilterReload) return;
        _ = DebounceSearchAsync();
    }

    private async Task DebounceSearchAsync()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;

        try
        {
            await Task.Delay(300, cts.Token);
            if (!cts.IsCancellationRequested)
            {
                await RefreshPhrasesAsync(resetPage: true);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    [RelayCommand]
    public async Task RefreshPhrasesAsync(bool resetPage = true)
    {
        if (_disposed) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        var cts = new CancellationTokenSource();
        _loadCts = cts;

        IsLoading = true;
        OnPropertyChanged(nameof(HasNoPhrases));

        try
        {
            var filter = BuildCurrentFilter();
            var items = await phraseService.GetPhrasesAsync(filter, cts.Token);
            if (cts.IsCancellationRequested) return;

            _allFilteredPhrases.Clear();
            _allFilteredPhrases.AddRange(items);

            if (resetPage)
            {
                CurrentPage = 1;
            }

            RefreshCurrentPageSlice();
            UpdateSummaryCounters();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorLoad")}: {ex.Message}", isError: true);
        }
        finally
        {
            if (ReferenceEquals(_loadCts, cts))
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoPhrases));
            }
        }
    }

    [RelayCommand]
    public async Task ResetFiltersAsync()
    {
        _suppressFilterReload = true;
        try
        {
            SelectedCategory = "All";
            SelectedCefrIndex = 0;
            SelectedRegisterIndex = 0;
            SelectedExamTargetIndex = 0;
            OnlyFavorites = false;
            OnlyDueForReview = false;
            SearchQuery = string.Empty;
        }
        finally
        {
            _suppressFilterReload = false;
        }

        await RefreshPhrasesAsync(resetPage: true);
    }

    private ConversationPhraseFilterDto BuildCurrentFilter()
    {
        CefrLevel? cefr = SelectedCefrIndex switch
        {
            1 => CefrLevel.A1,
            2 => CefrLevel.A2,
            3 => CefrLevel.B1,
            4 => CefrLevel.B2,
            5 => CefrLevel.C1,
            6 => CefrLevel.C2,
            _ => null
        };

        PhraseRegister? register = SelectedRegisterIndex switch
        {
            1 => PhraseRegister.Neutral,
            2 => PhraseRegister.Formal,
            3 => PhraseRegister.Casual,
            4 => PhraseRegister.Idiomatic,
            _ => null
        };

        ExamTarget? exam = SelectedExamTargetIndex switch
        {
            1 => ExamTarget.Toeic,
            2 => ExamTarget.Ielts,
            3 => ExamTarget.Vstep,
            _ => null
        };

        return new ConversationPhraseFilterDto
        {
            Category = string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase)
                ? null
                : SelectedCategory,
            CefrLevel = cefr,
            Register = register,
            ExamTarget = exam,
            OnlyFavorites = OnlyFavorites ? true : null,
            OnlyDueForReview = OnlyDueForReview ? true : null,
            SearchQuery = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim()
        };
    }
}
