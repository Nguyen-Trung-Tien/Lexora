using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.Views;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class KnowledgeGraphViewModel : ObservableObject
{
    private readonly IVocabularyKnowledgeGraphService _graphService;
    private readonly ILocalizationService _localizationService;
    private readonly INavigationService _navigationService;
    private readonly IKnowledgeGraphExportImportService _exportImportService;
    private readonly IAudioPlayerService? _audioPlayerService;
    private readonly IToastNotificationService? _toastService;
    private readonly DispatcherQueue? _dispatcherQueue;

    private readonly Stack<Guid> _historyStack = new();

    public ObservableCollection<KnowledgeGraphConceptSummaryDto> PagedConcepts { get; } = [];
    public ObservableCollection<GraphNodeDto> FilteredConnectedNodes { get; } = [];
    public ObservableCollection<KnowledgeGraphRecommendationDto> Recommendations { get; } = [];
    public ObservableCollection<WordDto> SearchSuggestions { get; } = [];

    [ObservableProperty]
    public partial double CanvasScale { get; set; } = 1.0;

    [RelayCommand]
    public void ZoomIn() => CanvasScale = Math.Min(2.5, Math.Round(CanvasScale + 0.15, 2));

    [RelayCommand]
    public void ZoomOut() => CanvasScale = Math.Max(0.4, Math.Round(CanvasScale - 0.15, 2));

    [RelayCommand]
    public void ResetZoom() => CanvasScale = 1.0;

    [ObservableProperty]
    public partial bool IsGraphLoading { get; set; }

    [ObservableProperty]
    public partial bool IsCatalogLoading { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CefrLevel? SelectedLevelFilter { get; set; }

    [ObservableProperty]
    public partial string SelectedCategoryFilter { get; set; } = "All";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveGraph))]
    [NotifyPropertyChangedFor(nameof(AllTabText))]
    [NotifyPropertyChangedFor(nameof(SynonymsTabText))]
    [NotifyPropertyChangedFor(nameof(AntonymsTabText))]
    [NotifyPropertyChangedFor(nameof(FamilyTabText))]
    [NotifyPropertyChangedFor(nameof(CollocationsTabText))]
    [NotifyPropertyChangedFor(nameof(TopicTabText))]
    public partial VocabularyKnowledgeGraphDto ActiveGraph { get; set; } = VocabularyKnowledgeGraphDto.Empty;

    public bool HasActiveGraph => ActiveGraph != null && ActiveGraph.CenterNode.Id != Guid.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedNode))]
    public partial GraphNodeDto? SelectedNode { get; set; }

    public bool HasSelectedNode => SelectedNode != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHistory))]
    public partial Guid? CurrentCenterWordId { get; set; }

    public bool HasHistory => _historyStack.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ViewModeButtonText))]
    [NotifyPropertyChangedFor(nameof(ViewModeIconGlyph))]
    public partial bool IsVisualMapMode { get; set; } = true;

    public string ViewModeButtonText => IsVisualMapMode
        ? _localizationService.GetString("KnowledgeGraph_CardsListMode")
        : _localizationService.GetString("KnowledgeGraph_VisualGraphMode");

    public string ViewModeIconGlyph => IsVisualMapMode ? "\uE809" : "\uE81E";

    // Pagination
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageDisplayInfo))]
    [NotifyPropertyChangedFor(nameof(ItemsRangeDisplay))]
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
    public partial int PageSize { get; set; } = 8;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public string PageDisplayInfo => $"{CurrentPage} / {TotalPages}";

    public string ItemsRangeDisplay
    {
        get
        {
            if (TotalCount == 0) return _localizationService.GetString("KnowledgeGraph_PagingItemsFormat")
                .Replace("{0}", "0").Replace("{1}", "0").Replace("{2}", "0");
            int start = ((CurrentPage - 1) * PageSize) + 1;
            int end = Math.Min(CurrentPage * PageSize, TotalCount);
            return string.Format(_localizationService.GetString("KnowledgeGraph_PagingItemsFormat"), start, end, TotalCount);
        }
    }

    public string AllTabText => string.Format(_localizationService.GetString("KnowledgeGraph_AllTab"), ActiveGraph.ConnectedNodes.Count);
    public string SynonymsTabText => $"{_localizationService.GetString("KnowledgeGraph_SynonymsTab")} ({ActiveGraph.SynonymsCount})";
    public string AntonymsTabText => $"{_localizationService.GetString("KnowledgeGraph_AntonymsTab")} ({ActiveGraph.AntonymsCount})";
    public string FamilyTabText => $"{_localizationService.GetString("KnowledgeGraph_FamilyTab")} ({ActiveGraph.WordFamilyCount})";
    public string CollocationsTabText => $"{_localizationService.GetString("KnowledgeGraph_CollocationsTab")} ({ActiveGraph.CollocationsCount})";
    public string TopicTabText => $"{_localizationService.GetString("KnowledgeGraph_TopicTab")} ({ActiveGraph.TopicWordsCount})";

    public KnowledgeGraphViewModel(
        IVocabularyKnowledgeGraphService graphService,
        ILocalizationService localizationService,
        INavigationService navigationService,
        IKnowledgeGraphExportImportService exportImportService,
        IAudioPlayerService? audioPlayerService = null,
        IToastNotificationService? toastService = null)
    {
        _graphService = graphService;
        _localizationService = localizationService;
        _navigationService = navigationService;
        _exportImportService = exportImportService;
        _audioPlayerService = audioPlayerService;
        _toastService = toastService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public async Task InitializeAsync(Guid? initialWordId = null)
    {
        await LoadRecommendationsAsync();
        await LoadCatalogPageAsync(1);

        if (initialWordId.HasValue && initialWordId.Value != Guid.Empty)
        {
            await LoadGraphAsync(initialWordId.Value, addToHistory: false);
        }
        else if (PagedConcepts.Count > 0)
        {
            await LoadGraphAsync(PagedConcepts[0].Id, addToHistory: false);
        }
    }

    [RelayCommand]
    public async Task LoadCatalogPageAsync(int page)
    {
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

            var pagedResult = await _graphService.GetConceptsPagedAsync(query);
            TotalCount = pagedResult.TotalCount;

            PagedConcepts.Clear();
            foreach (var item in pagedResult.Items)
            {
                PagedConcepts.Add(item);
            }

            OnPropertyChanged(nameof(ItemsRangeDisplay));
            OnPropertyChanged(nameof(PageDisplayInfo));
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
    public async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            await LoadCatalogPageAsync(CurrentPage + 1);
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            await LoadCatalogPageAsync(CurrentPage - 1);
        }
    }

    [RelayCommand]
    public async Task LoadRecommendationsAsync()
    {
        try
        {
            var recs = await _graphService.GetSmartRecommendationsAsync(8);
            Recommendations.Clear();
            foreach (var r in recs)
            {
                Recommendations.Add(r);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] LoadRecommendationsAsync error: {ex.Message}");
        }
    }

    public async Task LoadGraphAsync(Guid wordId, bool addToHistory = true)
    {
        if (wordId == Guid.Empty) return;

        if (addToHistory && CurrentCenterWordId.HasValue && CurrentCenterWordId.Value != wordId)
        {
            _historyStack.Push(CurrentCenterWordId.Value);
            OnPropertyChanged(nameof(HasHistory));
        }

        CurrentCenterWordId = wordId;
        IsGraphLoading = true;

        try
        {
            var graph = await _graphService.BuildGraphAsync(wordId);
            if (graph != null)
            {
                ActiveGraph = graph;
                SelectedNode = graph.CenterNode;
                ApplyCategoryFilter();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] LoadGraphAsync error: {ex.Message}");
            _toastService?.ShowError(ex.Message, _localizationService.GetString("Common_Error"));
        }
        finally
        {
            IsGraphLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadGraphForTermAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return;

        IsGraphLoading = true;
        try
        {
            var graph = await _graphService.BuildGraphForTermAsync(term.Trim());
            if (graph != null)
            {
                if (CurrentCenterWordId.HasValue && CurrentCenterWordId.Value != graph.CenterNode.Id)
                {
                    _historyStack.Push(CurrentCenterWordId.Value);
                    OnPropertyChanged(nameof(HasHistory));
                }

                CurrentCenterWordId = graph.CenterNode.Id;
                ActiveGraph = graph;
                SelectedNode = graph.CenterNode;
                ApplyCategoryFilter();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] LoadGraphForTermAsync error: {ex.Message}");
        }
        finally
        {
            IsGraphLoading = false;
        }
    }

    [RelayCommand]
    public async Task NavigateBackAsync()
    {
        if (_historyStack.Count > 0)
        {
            var previousId = _historyStack.Pop();
            OnPropertyChanged(nameof(HasHistory));
            await LoadGraphAsync(previousId, addToHistory: false);
        }
    }

    [RelayCommand]
    public void SelectNode(GraphNodeDto node)
    {
        SelectedNode = node;
    }

    [RelayCommand]
    public async Task PivotToNodeAsync(Guid nodeId)
    {
        var node = ActiveGraph?.ConnectedNodes?.FirstOrDefault(n => n.Id == nodeId);
        await PivotToNodeInternalAsync(nodeId, node?.Term);
    }

    public async Task PivotToNodeInternalAsync(Guid nodeId, string? term = null)
    {
        if (nodeId != Guid.Empty && nodeId != CurrentCenterWordId)
        {
            var graph = await _graphService.BuildGraphAsync(nodeId);
            if (graph != null)
            {
                if (CurrentCenterWordId.HasValue && CurrentCenterWordId.Value != graph.CenterNode.Id)
                {
                    _historyStack.Push(CurrentCenterWordId.Value);
                    OnPropertyChanged(nameof(HasHistory));
                }

                CurrentCenterWordId = graph.CenterNode.Id;
                ActiveGraph = graph;
                SelectedNode = graph.CenterNode;
                ApplyCategoryFilter();
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            await LoadGraphForTermAsync(term);
        }
    }

    [RelayCommand]
    public async Task FilterByCategoryAsync(string category)
    {
        SelectedCategoryFilter = category;
        ApplyCategoryFilter();
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task FilterByLevelAsync(CefrLevel? level)
    {
        SelectedLevelFilter = level;
        await LoadCatalogPageAsync(1);
    }

    [RelayCommand]
    public async Task PerformSearchAsync(string text)
    {
        SearchQuery = text;
        await LoadCatalogPageAsync(1);
    }

    [RelayCommand]
    public async Task SearchSuggestionsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            SearchSuggestions.Clear();
            return;
        }

        try
        {
            var matches = await _graphService.SearchGraphNodesAsync(query.Trim(), limit: 10);
            SearchSuggestions.Clear();
            foreach (var m in matches)
            {
                SearchSuggestions.Add(m);
            }
        }
        catch
        {
            SearchSuggestions.Clear();
        }
    }

    [RelayCommand]
    public async Task PlayCenterPronunciationAsync(string? accent = "us")
    {
        var term = ActiveGraph?.CenterNode?.Term;
        if (string.IsNullOrWhiteSpace(term) || _audioPlayerService == null) return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(term, accent ?? "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] PlayCenterPronunciationAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task PlaySelectedPronunciationAsync(string? accent = "us")
    {
        var term = SelectedNode?.Term ?? ActiveGraph?.CenterNode?.Term;
        if (string.IsNullOrWhiteSpace(term) || _audioPlayerService == null) return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(term, accent ?? "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] PlaySelectedPronunciationAsync error: {ex.Message}");
        }
    }

    public async Task PlayTermPronunciationAsync(string? term, string? accent = "us")
    {
        if (string.IsNullOrWhiteSpace(term) || _audioPlayerService == null) return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(term, accent ?? "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] PlayTermPronunciationAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task PlayPronunciationAsync(string? accent = "us")
    {
        await PlaySelectedPronunciationAsync(accent);
    }

    [RelayCommand]
    public void NavigateToVocabulary(Guid? wordId = null)
    {
        var targetId = wordId ?? SelectedNode?.Id ?? ActiveGraph.CenterNode.Id;
        if (targetId != Guid.Empty)
        {
            _navigationService.Navigate(typeof(VocabularyPage), targetId);
        }
        else
        {
            _navigationService.Navigate(typeof(VocabularyPage));
        }
    }

    [RelayCommand]
    public void NavigateToTest(Guid? wordId = null)
    {
        var targetId = wordId ?? SelectedNode?.Id ?? ActiveGraph.CenterNode.Id;
        if (targetId != Guid.Empty)
        {
            _navigationService.Navigate(typeof(TestPage), targetId);
        }
        else
        {
            _navigationService.Navigate(typeof(TestPage));
        }
    }

    [RelayCommand]
    public void ToggleViewMode()
    {
        IsVisualMapMode = !IsVisualMapMode;
    }

    private void ApplyCategoryFilter()
    {
        FilteredConnectedNodes.Clear();
        var nodes = SelectedCategoryFilter switch
        {
            "Synonym" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.Synonym),
            "Antonym" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.Antonym),
            "WordFamily" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.WordFamily),
            "Collocation" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.Collocation),
            "TopicCluster" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.TopicCluster),
            _ => ActiveGraph.ConnectedNodes.AsEnumerable()
        };

        foreach (var node in nodes)
        {
            FilteredConnectedNodes.Add(node);
        }
    }

    [RelayCommand]
    public void CopySelectedTerm()
    {
        var term = SelectedNode?.Term ?? ActiveGraph?.CenterNode?.Term;
        if (string.IsNullOrWhiteSpace(term)) return;

        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(term);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            _toastService?.ShowSuccess(string.Format(_localizationService.GetString("KnowledgeGraph_TermCopied"), term));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] CopySelectedTerm error: {ex.Message}");
        }
    }

    public async Task<string> ExportAsync(string format)
    {
        var targetWordId = ActiveGraph?.CenterNode?.Id != Guid.Empty ? ActiveGraph?.CenterNode?.Id : null;
        return format.ToLowerInvariant() switch
        {
            "csv" => await _exportImportService.ExportToCsvAsync(targetWordId),
            "md" or "markdown" => await _exportImportService.ExportToMarkdownMindmapAsync(targetWordId),
            _ => await _exportImportService.ExportToJsonAsync(targetWordId)
        };
    }

    public string GetSampleTemplate(string format) => _exportImportService.GetSampleTemplate(format);

    public void UpdateLocalizedTexts()
    {
        OnPropertyChanged(nameof(ItemsRangeDisplay));
        OnPropertyChanged(nameof(PageDisplayInfo));
        OnPropertyChanged(nameof(AllTabText));
        OnPropertyChanged(nameof(SynonymsTabText));
        OnPropertyChanged(nameof(AntonymsTabText));
        OnPropertyChanged(nameof(FamilyTabText));
        OnPropertyChanged(nameof(CollocationsTabText));
        OnPropertyChanged(nameof(TopicTabText));
        OnPropertyChanged(nameof(ViewModeButtonText));
    }
}
