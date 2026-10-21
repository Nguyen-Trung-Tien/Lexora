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
    public partial int TotalCount { get; set; }

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

            Groups.Clear();
            foreach (var group in results)
            {
                Groups.Add(group);
            }

            TotalCount = Groups.Count;

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomophonesViewModel] LoadGroupsAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
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
                var existing = Groups.FirstOrDefault(g => g.Id == id);
                if (existing != null)
                {
                    Groups.Remove(existing);
                    TotalCount = Groups.Count;
                    if (SelectedGroup?.Id == id)
                    {
                        SelectedGroup = Groups.FirstOrDefault();
                    }
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
