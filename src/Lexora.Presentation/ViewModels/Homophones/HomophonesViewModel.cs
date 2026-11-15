using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class HomophonesViewModel : ObservableObject, IDisposable
{
    private readonly IHomophoneService _homophoneService;
    private readonly IHomophoneExportImportService _exportImportService;
    private readonly IAudioPlayerService? _audioPlayerService;
    private readonly ILocalizationService _localizationService;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly EventHandler? _languageChangedHandler;

    private readonly List<HomophoneGroupDto> _allFilteredGroups = [];
    private CancellationTokenSource? _loadCts;

    public ObservableCollection<HomophoneGroupDto> Groups { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyState))]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSubmitting { get; set; }

    public bool IsEmptyState => !IsLoading && Groups.Count == 0;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial HomophoneType? SelectedTypeFilter { get; set; }

    [ObservableProperty]
    public partial CefrLevel? SelectedLevelFilter { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedGroup))]
    public partial HomophoneGroupDto? SelectedGroup { get; set; }

    public bool HasSelectedGroup => SelectedGroup != null;

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
    public partial int PageSize { get; set; } = 7;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    public string PageDisplayInfo => $"{CurrentPage} / {TotalPages}";

    public string ItemsRangeDisplay
    {
        get
        {
            if (TotalCount == 0) return _localizationService.GetString("Homophones_ItemsRangeEmpty");
            int start = ((CurrentPage - 1) * PageSize) + 1;
            int end = Math.Min(CurrentPage * PageSize, TotalCount);
            return string.Format(_localizationService.GetString("Homophones_ItemsRangeFormat"), start, end, TotalCount);
        }
    }

    public HomophonesViewModel(
        IHomophoneService homophoneService,
        IHomophoneExportImportService exportImportService,
        ILocalizationService localizationService,
        IAudioPlayerService? audioPlayerService = null)
    {
        _homophoneService = homophoneService;
        _exportImportService = exportImportService;
        _localizationService = localizationService;
        _audioPlayerService = audioPlayerService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _languageChangedHandler = (_, _) =>
        {
            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() => OnPropertyChanged(nameof(ItemsRangeDisplay)));
            }
            else
            {
                OnPropertyChanged(nameof(ItemsRangeDisplay));
            }
        };
        _localizationService.LanguageChanged += _languageChangedHandler;
    }

    [RelayCommand]
    public async Task LoadGroupsAsync(CancellationToken cancellationToken = default)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _loadCts.Token;

        IsLoading = true;
        try
        {
            var results = await _homophoneService.GetAllGroupsAsync(
                typeFilter: SelectedTypeFilter,
                levelFilter: SelectedLevelFilter,
                searchTerm: SearchQuery,
                cancellationToken: token);

            token.ThrowIfCancellationRequested();

            _allFilteredGroups.Clear();
            _allFilteredGroups.AddRange(results);
            TotalCount = _allFilteredGroups.Count;

            if (CurrentPage > TotalPages)
            {
                CurrentPage = 1;
            }

            RefreshCurrentPageSlice();
        }
        catch (OperationCanceledException)
        {
            // Expected when superseded by a newer search/filter request
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomophonesViewModel] LoadGroupsAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshCurrentPageSlice()
    {
        Groups.Clear();
        var pageItems = _allFilteredGroups
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var group in pageItems)
        {
            Groups.Add(group);
        }

        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(PageDisplayInfo));
        OnPropertyChanged(nameof(ItemsRangeDisplay));

        if (SelectedGroup != null)
        {
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == SelectedGroup.Id) ?? Groups.FirstOrDefault();
        }
        else
        {
            SelectedGroup = Groups.FirstOrDefault();
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        _ = LoadGroupsAsync();
    }

    partial void OnSelectedTypeFilterChanged(HomophoneType? value)
    {
        _ = LoadGroupsAsync();
    }

    partial void OnSelectedLevelFilterChanged(CefrLevel? value)
    {
        _ = LoadGroupsAsync();
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
    }
}
