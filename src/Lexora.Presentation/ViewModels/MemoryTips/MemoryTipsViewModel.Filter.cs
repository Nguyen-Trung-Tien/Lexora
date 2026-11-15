using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels.MemoryTips;

public partial class MemoryTipsViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    public partial int SelectedCategoryIndex { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    public partial int SelectedTechniqueIndex { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    public partial int SelectedExamTargetIndex { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    public partial bool OnlyPinnedFilter { get; set; } = false;

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchQuery) ||
        SelectedCategoryIndex > 0 ||
        SelectedTechniqueIndex > 0 ||
        SelectedExamTargetIndex > 0 ||
        OnlyPinnedFilter;

    // Pagination State
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
    public partial int FilteredTotalCount { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemRangeDisplayInfo))]
    public partial int PageSize { get; set; } = 10;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public string PageDisplayInfo => $"{CurrentPage} / {Math.Max(1, TotalPages)}";
    public string ItemRangeDisplayInfo => FilteredTotalCount > 0
        ? $"{Math.Min((CurrentPage - 1) * PageSize + 1, FilteredTotalCount)}-{Math.Min(CurrentPage * PageSize, FilteredTotalCount)} / {FilteredTotalCount}"
        : "0 / 0";

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, token);
                if (!token.IsCancellationRequested)
                {
                    await LoadTipsAsync(token);
                }
            }
            catch (OperationCanceledException)
            {
                // Debounce cancelled by newer keypress
            }
        }, token);
    }

    partial void OnSelectedCategoryIndexChanged(int value)
    {
        CurrentPage = 1;
        _ = LoadTipsAsync();
    }

    partial void OnSelectedTechniqueIndexChanged(int value)
    {
        CurrentPage = 1;
        _ = LoadTipsAsync();
    }

    partial void OnSelectedExamTargetIndexChanged(int value)
    {
        CurrentPage = 1;
        _ = LoadTipsAsync();
    }

    partial void OnOnlyPinnedFilterChanged(bool value)
    {
        CurrentPage = 1;
        _ = LoadTipsAsync();
    }

    [RelayCommand]
    public async Task ResetFiltersAsync()
    {
        ActiveQuickFilter = 0;
        SearchQuery = string.Empty;
        SelectedCategoryIndex = 0;
        SelectedTechniqueIndex = 0;
        SelectedExamTargetIndex = 0;
        OnlyPinnedFilter = false;
        CurrentPage = 1;
        await LoadTipsAsync();
    }

    [RelayCommand]
    public async Task FirstPageAsync()
    {
        if (CurrentPage != 1)
        {
            CurrentPage = 1;
            await LoadTipsAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadTipsAsync();
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadTipsAsync();
        }
    }

    [RelayCommand]
    public async Task LastPageAsync()
    {
        if (CurrentPage != TotalPages)
        {
            CurrentPage = TotalPages;
            await LoadTipsAsync();
        }
    }

    [RelayCommand]
    public async Task ChangePageSizeAsync(int newSize)
    {
        if (newSize > 0 && PageSize != newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadTipsAsync();
        }
    }

    public async Task LoadTipsAsync(CancellationToken cancellationToken = default)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _loadCts.Token;

        IsLoading = true;
        try
        {
            TipCategory? category = SelectedCategoryIndex > 0
                ? (TipCategory)(SelectedCategoryIndex - 1)
                : null;

            MnemonicTechnique? technique = SelectedTechniqueIndex > 0
                ? (MnemonicTechnique)(SelectedTechniqueIndex - 1)
                : null;

            ExamTarget? examTarget = SelectedExamTargetIndex switch
            {
                1 => ExamTarget.Toeic,
                2 => ExamTarget.Ielts,
                3 => ExamTarget.Vstep,
                _ => null
            };

            bool? onlyPinned = OnlyPinnedFilter ? true : null;
            string? search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();

            var pagedResult = await _tipService.GetPagedTipsAsync(
                CurrentPage,
                PageSize,
                category,
                technique,
                examTarget,
                onlyPinned,
                search,
                token);

            if (!token.IsCancellationRequested)
            {
                Tips.Clear();
                foreach (var item in pagedResult.Items)
                {
                    Tips.Add(item);
                }

                FilteredTotalCount = pagedResult.TotalCount;
                TotalPages = pagedResult.TotalPages;
                CurrentPage = pagedResult.Page;

                if (SelectedTip == null || !Tips.Any(t => t.Id == SelectedTip.Id))
                {
                    SelectedTip = Tips.FirstOrDefault();
                }

                OnPropertyChanged(nameof(HasTips));
                OnPropertyChanged(nameof(HasNoTips));
                await RefreshStatsAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Request superseded
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message, isError: true);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
