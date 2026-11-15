using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class KnowledgeGraphViewModel
{
    public async Task InitializeAsync(Guid? initialWordId = null, CancellationToken cancellationToken = default)
    {
        await LoadRecommendationsAsync(cancellationToken);
        await LoadCatalogPageAsync(1, cancellationToken);

        if (initialWordId.HasValue && initialWordId.Value != Guid.Empty)
        {
            await LoadGraphAsync(initialWordId.Value, addToHistory: false, cancellationToken);
        }
        else if (PagedConcepts.Count > 0)
        {
            await LoadGraphAsync(PagedConcepts[0].Id, addToHistory: false, cancellationToken);
        }
    }

    [RelayCommand]
    public async Task LoadCatalogPageAsync(int page, CancellationToken cancellationToken = default)
    {
        _catalogCts?.Cancel();
        _catalogCts?.Dispose();
        _catalogCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _catalogCts.Token;

        IsCatalogLoading = true;
        try
        {
            CurrentPage = Math.Max(1, page);
            var query = new KnowledgeGraphQueryDto(
                SearchTerm: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                Level: SelectedLevelFilter,
                Page: CurrentPage,
                PageSize: PageSize
            );

            var pagedResult = await _graphService.GetConceptsPagedAsync(query, token);
            token.ThrowIfCancellationRequested();

            TotalCount = pagedResult.TotalCount;

            PagedConcepts.Clear();
            foreach (var item in pagedResult.Items)
            {
                PagedConcepts.Add(item);
            }

            OnPropertyChanged(nameof(ItemsRangeDisplay));
            OnPropertyChanged(nameof(PageDisplayInfo));
        }
        catch (OperationCanceledException)
        {
            // Expected when superseded by a newer search/filter request
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] LoadCatalogPageAsync error: {ex.Message}");
        }
        finally
        {
            IsCatalogLoading = false;
        }
    }

    [RelayCommand]
    public async Task NextPageAsync(CancellationToken cancellationToken = default)
    {
        if (HasNextPage)
        {
            await LoadCatalogPageAsync(CurrentPage + 1, cancellationToken);
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync(CancellationToken cancellationToken = default)
    {
        if (HasPreviousPage)
        {
            await LoadCatalogPageAsync(CurrentPage - 1, cancellationToken);
        }
    }

    [RelayCommand]
    public async Task LoadRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var recs = await _graphService.GetSmartRecommendationsAsync(8, cancellationToken);
            Recommendations.Clear();
            foreach (var r in recs)
            {
                Recommendations.Add(r);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] LoadRecommendationsAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task FilterByLevelAsync(CefrLevel? level, CancellationToken cancellationToken = default)
    {
        SelectedLevelFilter = level;
        await LoadCatalogPageAsync(1, cancellationToken);
    }

    [RelayCommand]
    public async Task PerformSearchAsync(string text, CancellationToken cancellationToken = default)
    {
        SearchQuery = text;
        await LoadCatalogPageAsync(1, cancellationToken);
    }

    [RelayCommand]
    public async Task SearchSuggestionsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            SearchSuggestions.Clear();
            return;
        }

        try
        {
            var matches = await _graphService.SearchGraphNodesAsync(query.Trim(), limit: 10, cancellationToken);
            SearchSuggestions.Clear();
            foreach (var m in matches)
            {
                SearchSuggestions.Add(m);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch
        {
            SearchSuggestions.Clear();
        }
    }
}
