using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class LearningStorageViewModel
{
    private bool _isDeletingItems = false;
    private bool _isAddingItems = false;

    public async Task<int> AddWordsToStorageAsync(
        IEnumerable<Guid> wordIds,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var targetIds = wordIds.Distinct().Where(id => id != Guid.Empty).ToList();
        if (targetIds.Count == 0 || _isAddingItems || _isDeletingItems) return 0;
        _isAddingItems = true;

        if (_dispatcherQueue != null)
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
        }
        IsLoading = true;

        try
        {
            int addedCount = await _storageService.MoveWordsToStorageAsync(
                targetIds, StorageAdditionReason.Manual, note, cancellationToken);

            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }

            // Immediate UI update: switch to Vocabulary tab and refresh list & metrics
            SelectedTab = 0;
            CurrentPage = 1;
            await RefreshAllAsync();

            return addedCount;
        }
        finally
        {
            _isAddingItems = false;
            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }
            IsLoading = false;
        }
    }

    public async Task<int> AddGrammarsToStorageAsync(
        IEnumerable<Guid> grammarRuleIds,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var targetIds = grammarRuleIds.Distinct().Where(id => id != Guid.Empty).ToList();
        if (targetIds.Count == 0 || _isAddingItems || _isDeletingItems) return 0;
        _isAddingItems = true;

        if (_dispatcherQueue != null)
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
        }
        IsLoading = true;

        try
        {
            int addedCount = await _storageService.MoveGrammarsToStorageAsync(
                targetIds, StorageAdditionReason.Manual, note, cancellationToken);

            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }

            // Immediate UI update: switch to Grammar tab and refresh list & metrics
            SelectedTab = 1;
            CurrentPage = 1;
            await RefreshAllAsync();

            return addedCount;
        }
        finally
        {
            _isAddingItems = false;
            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }
            IsLoading = false;
        }
    }

    public async Task<int> DeleteSelectedItemsAsync(
        IEnumerable<Guid>? ids = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        var targetIds = ids?.Distinct().ToList() ?? SelectedItemIds.ToList();
        if (targetIds.Count == 0 || _isDeletingItems || _isAddingItems) return 0;
        _isDeletingItems = true;

        if (_dispatcherQueue != null)
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
        }
        IsLoading = true;

        try
        {
            int count = await Task.Run(
                () => _storageService.RemoveRangeFromStorageAsync(targetIds, cancellationToken, progress),
                cancellationToken);

            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }

            // Immediate UI update: remove deleted items from in-memory collection immediately
            var deletedSet = new HashSet<Guid>(targetIds);
            for (int i = StorageItems.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(StorageItems[i].Id))
                {
                    StorageItems.RemoveAt(i);
                }
            }

            TotalCount = Math.Max(0, TotalCount - count);
            if (SelectedItem != null && deletedSet.Contains(SelectedItem.Id))
            {
                SelectedItem = StorageItems.FirstOrDefault();
            }

            SelectedItemIds.Clear();
            IsSelectionMode = false;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsEmptyState));

            // Reload statistics to sync badges and metrics
            await LoadStatisticsAsync();

            // Handle page overflow if the current page was emptied
            if (StorageItems.Count == 0 && CurrentPage > 1)
            {
                CurrentPage--;
                await LoadItemsAsync();
            }
            else if (StorageItems.Count == 0 && TotalCount > 0)
            {
                await LoadItemsAsync();
            }

            return count;
        }
        finally
        {
            _isDeletingItems = false;
            if (_dispatcherQueue != null)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
            }
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task MarkAsMasteredAsync(LearningStorageItemDto? item)
    {
        if (item == null) return;
        try
        {
            await _storageService.MarkAsMasteredAsync(item.Id);
            _soundFeedback?.PlaySuccessSound();
            await LoadStatisticsAsync();
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] MarkAsMastered error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task RemoveItemAsync(LearningStorageItemDto? item)
    {
        if (item == null) return;
        try
        {
            await _storageService.RemoveFromStorageAsync(item.Id);
            StorageItems.Remove(item);
            TotalCount = Math.Max(0, TotalCount - 1);
            await LoadStatisticsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] RemoveItem error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task UpdateStatusAsync((Guid Id, StorageLearningStatus Status) param)
    {
        try
        {
            await _storageService.UpdateStatusAsync(param.Id, param.Status);
            await LoadStatisticsAsync();
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] UpdateStatus error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveNoteAsync((Guid Id, string Note) param)
    {
        try
        {
            await _storageService.UpdateNoteAsync(param.Id, param.Note);
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] SaveNote error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task PlayAudioAsync(string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || _audioPlayer == null) return;
        try
        {
            var accent = _preferencesService?.GetPreferences().PreferredAccent ?? "us";
            await _audioPlayer.PlayPronunciationAsync(term, accent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStorageViewModel] PlayAudio error: {ex.Message}");
        }
    }
}
