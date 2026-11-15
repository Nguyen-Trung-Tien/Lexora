using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class KnowledgeGraphViewModel : ObservableObject, IDisposable
{
    private readonly IVocabularyKnowledgeGraphService _graphService;
    private readonly ILocalizationService _localizationService;
    private readonly INavigationService _navigationService;
    private readonly IKnowledgeGraphExportImportService _exportImportService;
    private readonly IAudioPlayerService? _audioPlayerService;
    private readonly IToastNotificationService? _toastService;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly EventHandler? _languageChangedHandler;

    private readonly Stack<Guid> _historyStack = new();
    private CancellationTokenSource? _graphCts;
    private CancellationTokenSource? _catalogCts;

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
    public partial bool IsSubmitting { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CefrLevel? SelectedLevelFilter { get; set; }

    [ObservableProperty]
    public partial string SelectedCategoryFilter { get; set; } = "All";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveGraph))]
    [NotifyPropertyChangedFor(nameof(HasMultipleMeaningsForCenterNode))]
    [NotifyPropertyChangedFor(nameof(AllTabText))]
    [NotifyPropertyChangedFor(nameof(SynonymsTabText))]
    [NotifyPropertyChangedFor(nameof(AntonymsTabText))]
    [NotifyPropertyChangedFor(nameof(FamilyTabText))]
    [NotifyPropertyChangedFor(nameof(CollocationsTabText))]
    [NotifyPropertyChangedFor(nameof(TopicTabText))]
    public partial VocabularyKnowledgeGraphDto ActiveGraph { get; set; } = VocabularyKnowledgeGraphDto.Empty;

    public bool HasActiveGraph => ActiveGraph != null && ActiveGraph.CenterNode.Id != Guid.Empty;
    public bool HasMultipleMeaningsForCenterNode => ActiveGraph?.CenterNode?.AllMeanings != null && ActiveGraph.CenterNode.AllMeanings.Count > 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedNode))]
    [NotifyPropertyChangedFor(nameof(HasMultipleMeaningsForSelectedNode))]
    public partial GraphNodeDto? SelectedNode { get; set; }

    public bool HasSelectedNode => SelectedNode != null;
    public bool HasMultipleMeaningsForSelectedNode => SelectedNode?.AllMeanings != null && SelectedNode.AllMeanings.Count > 1;

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

        _languageChangedHandler = (_, _) =>
        {
            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(UpdateLocalizedTexts);
            }
            else
            {
                UpdateLocalizedTexts();
            }
        };
        _localizationService.LanguageChanged += _languageChangedHandler;
    }

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

    public void Dispose()
    {
        if (_languageChangedHandler != null)
        {
            _localizationService.LanguageChanged -= _languageChangedHandler;
        }
        _graphCts?.Cancel();
        _graphCts?.Dispose();
        _graphCts = null;
        _catalogCts?.Cancel();
        _catalogCts?.Dispose();
        _catalogCts = null;
    }
}
