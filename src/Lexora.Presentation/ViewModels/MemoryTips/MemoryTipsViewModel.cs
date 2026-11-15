using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs.MemoryTips;
using Lexora.Application.Interfaces.MemoryTips;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels.MemoryTips;

/// <summary>
/// ViewModel managing memory tips, cognitive mnemonic hooks, and exam traps.
/// Implements partial class architecture with C# 13 partial properties,
/// IsSubmitting concurrency protection, and reactive Fluent 2 bindings.
/// </summary>
public partial class MemoryTipsViewModel(
    IMemoryTipService tipService,
    ILocalizationService localizationService) : ObservableObject, IDisposable
{
    private readonly IMemoryTipService _tipService = tipService;
    private readonly ILocalizationService _localizationService = localizationService;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchDebounceCts;
    private bool _disposed;

    public ILocalizationService Loc => _localizationService;

    public ObservableCollection<MemoryTipDto> Tips { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTip))]
    public partial MemoryTipDto? SelectedTip { get; set; }

    [ObservableProperty]
    public partial bool IsSplitViewMode { get; set; } = true;

    [ObservableProperty]
    public partial bool IsGridViewMode { get; set; } = false;

    [ObservableProperty]
    public partial int ActiveQuickFilter { get; set; } = 0; // 0: All, 1: Pinned, 2: Traps

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoTips))]
    [NotifyPropertyChangedFor(nameof(HasTips))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSubmitting { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsErrorStatus { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial int PinnedCount { get; set; }

    [ObservableProperty]
    public partial int TrapCount { get; set; }

    public bool HasSelectedTip => SelectedTip != null;
    public bool HasTips => Tips.Count > 0;
    public bool HasNoTips => !IsLoading && Tips.Count == 0;
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadTipsAsync();
    }

    [RelayCommand]
    public void SetSplitView()
    {
        IsSplitViewMode = true;
        IsGridViewMode = false;
    }

    [RelayCommand]
    public void SetGridView()
    {
        IsSplitViewMode = false;
        IsGridViewMode = true;
    }

    [RelayCommand]
    public async Task FilterAllAsync()
    {
        ActiveQuickFilter = 0;
        OnlyPinnedFilter = false;
        SelectedCategoryIndex = 0;
        SelectedTechniqueIndex = 0;
        SelectedExamTargetIndex = 0;
        CurrentPage = 1;
        await LoadTipsAsync();
    }

    [RelayCommand]
    public async Task FilterPinnedAsync()
    {
        ActiveQuickFilter = 1;
        OnlyPinnedFilter = true;
        SelectedCategoryIndex = 0;
        CurrentPage = 1;
        await LoadTipsAsync();
    }

    [RelayCommand]
    public async Task FilterTrapsAsync()
    {
        ActiveQuickFilter = 2;
        OnlyPinnedFilter = false;
        SelectedCategoryIndex = 5; // TipCategory.ExamStrategy
        CurrentPage = 1;
        await LoadTipsAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadTipsAsync();
    }

    [RelayCommand]
    public async Task TogglePinAsync(MemoryTipDto? targetTip)
    {
        var tip = targetTip ?? SelectedTip;
        if (tip == null || IsSubmitting) return;

        IsSubmitting = true;
        try
        {
            await _tipService.TogglePinAsync(tip.Id);
            await LoadTipsAsync();
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message, isError: true);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public async Task DeleteTipAsync(MemoryTipDto? targetTip)
    {
        var tip = targetTip ?? SelectedTip;
        if (tip == null || IsSubmitting) return;

        IsSubmitting = true;
        try
        {
            var success = await _tipService.DeleteAsync(tip.Id);
            if (success)
            {
                if (SelectedTip?.Id == tip.Id)
                {
                    SelectedTip = null;
                }
                await LoadTipsAsync();
                ShowStatus(_localizationService.GetString("MemoryTips_DeleteSuccess"), isError: false);
            }
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message, isError: true);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public async Task OnTipCreatedOrUpdatedAsync(MemoryTipDto updatedOrNew)
    {
        SelectedTip = updatedOrNew;
        await LoadTipsAsync();
    }

    public async Task RefreshStatsAsync()
    {
        try
        {
            var stats = await _tipService.GetStatisticsAsync();
            TotalCount = stats.TotalCount;
            PinnedCount = stats.PinnedCount;
            TrapCount = stats.TrapCount;
        }
        catch
        {
            // Ignore background stats failure
        }
    }

    public void ShowStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsErrorStatus = isError;
    }

    public void ClearStatus()
    {
        StatusMessage = string.Empty;
        IsErrorStatus = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
