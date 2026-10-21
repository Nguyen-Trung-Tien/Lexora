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

    private readonly List<HomophoneGroupDto> _allFilteredGroups = [];

    public ObservableCollection<HomophoneGroupDto> Groups { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyState))]
    public partial bool IsLoading { get; set; } = true;

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
            if (TotalCount == 0) return "0 nhóm";
            int start = ((CurrentPage - 1) * PageSize) + 1;
            int end = Math.Min(CurrentPage * PageSize, TotalCount);
            return $"Hiển thị {start} - {end} trong {TotalCount} nhóm";
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
    }

    [RelayCommand]
    public async Task LoadGroupsAsync()
    {
        IsLoading = true;
        try
        {
            var results = await _homophoneService.GetAllGroupsAsync(
                typeFilter: SelectedTypeFilter,
                levelFilter: SelectedLevelFilter,
                searchTerm: SearchQuery);

            _allFilteredGroups.Clear();
            _allFilteredGroups.AddRange(results);
            TotalCount = _allFilteredGroups.Count;

            if (CurrentPage > TotalPages)
            {
                CurrentPage = 1;
            }

            RefreshCurrentPageSlice();
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

        // Maintain or auto-select first item
        if (SelectedGroup != null)
        {
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == SelectedGroup.Id) ?? Groups.FirstOrDefault();
        }
        else
        {
            SelectedGroup = Groups.FirstOrDefault();
        }
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
        if (target == CurrentPage && Groups.Count > 0) return;
        CurrentPage = target;
        RefreshCurrentPageSlice();
    }

    public void SelectGroupById(Guid id)
    {
        var found = _allFilteredGroups.FirstOrDefault(g => g.Id == id);
        if (found != null)
        {
            int index = _allFilteredGroups.IndexOf(found);
            int targetPage = (index / PageSize) + 1;
            if (targetPage != CurrentPage)
            {
                CurrentPage = targetPage;
                RefreshCurrentPageSlice();
            }
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == id) ?? found;
        }
    }

    public async Task<IReadOnlyList<SmartSearchSuggestion>> GetSearchSuggestionsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SmartSearchSuggestion>();
        }

        var clean = query.Trim().ToLowerInvariant();
        var cleanIpa = clean.Trim('/', '[', ']', ' ');
        var suggestions = new List<SmartSearchSuggestion>();

        var all = _allFilteredGroups.Count > 0 ? _allFilteredGroups : await _homophoneService.GetAllGroupsAsync();

        foreach (var g in all)
        {
            bool titleMatch = g.Title.Contains(clean, StringComparison.OrdinalIgnoreCase);
            bool ipaMatch = !string.IsNullOrEmpty(g.PhoneticIpa) &&
                            (g.PhoneticIpa.Contains(clean, StringComparison.OrdinalIgnoreCase) ||
                             (!string.IsNullOrEmpty(cleanIpa) && g.PhoneticIpa.Contains(cleanIpa, StringComparison.OrdinalIgnoreCase)));
            bool itemMatch = g.Items.Any(i => i.Term.Contains(clean, StringComparison.OrdinalIgnoreCase) ||
                                              i.MeaningVi.Contains(clean, StringComparison.OrdinalIgnoreCase));

            if (titleMatch || ipaMatch || itemMatch)
            {
                string typeLabel = g.Type switch
                {
                    HomophoneType.ExactHomophone => "Đồng âm hoàn toàn",
                    HomophoneType.NearHomophone => "Gần đồng âm",
                    HomophoneType.MinimalPair => "Cặp âm tối thiểu",
                    _ => "Đồng âm"
                };

                string termsDisplay = g.Items.Count > 0
                    ? string.Join(" / ", g.Items.Select(i => i.Term))
                    : g.Title;

                string subtitle = !string.IsNullOrEmpty(g.PhoneticIpa)
                    ? $"{g.PhoneticIpa} • {typeLabel}"
                    : typeLabel;

                suggestions.Add(new SmartSearchSuggestion
                {
                    Title = termsDisplay,
                    Subtitle = subtitle,
                    Glyph = "\uE8D6",
                    SearchTerm = termsDisplay,
                    TargetType = g.Id.ToString()
                });

                if (suggestions.Count >= 6) break;
            }
        }

        // Fuzzy fallback if few results
        if (suggestions.Count < 3)
        {
            foreach (var g in all)
            {
                foreach (var item in g.Items)
                {
                    if (item.Term.StartsWith(clean, StringComparison.OrdinalIgnoreCase) &&
                        !suggestions.Any(s => s.TargetType == g.Id.ToString()))
                    {
                        suggestions.Add(new SmartSearchSuggestion
                        {
                            Title = $"{item.Term} ({g.Title})",
                            Subtitle = $"Gợi ý: {g.PhoneticIpa}",
                            Glyph = "\uE946",
                            SearchTerm = item.Term,
                            TargetType = g.Id.ToString()
                        });

                        if (suggestions.Count >= 6) break;
                    }
                }
                if (suggestions.Count >= 6) break;
            }
        }

        return suggestions;
    }

    [RelayCommand]
    public async Task PlayAudioAsync(string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || _audioPlayerService == null) return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(term.Trim());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomophonesViewModel] PlayAudioAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteGroupAsync(Guid id)
    {
        try
        {
            var success = await _homophoneService.DeleteGroupAsync(id);
            if (success)
            {
                var existing = _allFilteredGroups.FirstOrDefault(g => g.Id == id);
                if (existing != null)
                {
                    _allFilteredGroups.Remove(existing);
                    TotalCount = _allFilteredGroups.Count;
                    if (CurrentPage > TotalPages)
                    {
                        CurrentPage = Math.Max(1, TotalPages);
                    }
                    RefreshCurrentPageSlice();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomophonesViewModel] DeleteGroupAsync error: {ex.Message}");
        }
    }

    public async Task<HomophoneGroupDto> SaveGroupAsync(CreateHomophoneGroupDto createDto)
    {
        var result = await _homophoneService.CreateGroupAsync(createDto);
        await LoadGroupsAsync();
        SelectedGroup = Groups.FirstOrDefault(g => g.Id == result.Id) ?? result;
        return result;
    }

    public async Task<HomophoneGroupDto> UpdateGroupAsync(UpdateHomophoneGroupDto updateDto)
    {
        var result = await _homophoneService.UpdateGroupAsync(updateDto);
        await LoadGroupsAsync();
        SelectedGroup = Groups.FirstOrDefault(g => g.Id == result.Id) ?? result;
        return result;
    }

    public async Task<string> ExportAsync(string format)
    {
        return (format.ToLowerInvariant()) switch
        {
            "csv" => await _exportImportService.ExportToCsvAsync(),
            "txt" => await _exportImportService.ExportToTxtAsync(),
            _ => await _exportImportService.ExportToJsonAsync()
        };
    }

    public string GetSampleTemplate(string format)
    {
        return _exportImportService.GetSampleTemplate(format);
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
        // Cleanup if needed
    }
}
