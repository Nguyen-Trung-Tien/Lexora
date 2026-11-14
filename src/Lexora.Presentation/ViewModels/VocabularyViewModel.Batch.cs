using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class VocabularyViewModel
{
    [RelayCommand]
    public void ToggleSelectionMode()
    {
        IsSelectionMode = !IsSelectionMode;
        if (!IsSelectionMode)
        {
            SelectedWordIds.Clear();
            SelectedContextUnitIds.Clear();
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedContextUnitCount));
        OnPropertyChanged(nameof(HasContextUnitSelection));
    }

    [RelayCommand]
    public void SelectAllContextUnits()
    {
        SelectedContextUnitIds.Clear();
        foreach (var unit in FilteredContextUnits)
        {
            if (unit.Id != Guid.Empty)
            {
                SelectedContextUnitIds.Add(unit.Id);
            }
        }
        OnPropertyChanged(nameof(SelectedContextUnitCount));
        OnPropertyChanged(nameof(HasContextUnitSelection));
    }

    [RelayCommand]
    public void DeselectAllContextUnits()
    {
        SelectedContextUnitIds.Clear();
        OnPropertyChanged(nameof(SelectedContextUnitCount));
        OnPropertyChanged(nameof(HasContextUnitSelection));
    }

    public void ToggleContextUnitSelection(Guid id)
    {
        if (id == Guid.Empty) return;
        if (SelectedContextUnitIds.Contains(id))
        {
            SelectedContextUnitIds.Remove(id);
        }
        else
        {
            SelectedContextUnitIds.Add(id);
        }
        OnPropertyChanged(nameof(SelectedContextUnitCount));
        OnPropertyChanged(nameof(HasContextUnitSelection));
    }

    public bool IsContextUnitSelected(Guid id) => SelectedContextUnitIds.Contains(id);

    [RelayCommand]
    public void SelectAllWords()
    {
        SelectedWordIds.Clear();
        foreach (var word in Words)
        {
            SelectedWordIds.Add(word.Id);
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedWordIds.Clear();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    public void ToggleWordSelection(Guid id)
    {
        if (SelectedWordIds.Contains(id))
        {
            SelectedWordIds.Remove(id);
        }
        else
        {
            SelectedWordIds.Add(id);
        }
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    private bool _isDeletingWords = false;

    public async Task<int> DeleteSelectedWordsAsync(
        IEnumerable<Guid>? ids = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        var targetIds = ids?.Distinct().ToList() ?? SelectedWordIds.ToList();
        if (targetIds.Count == 0 || _isDeletingWords) return 0;
        _isDeletingWords = true;
        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;
        StatusMessage = _localizationService.GetString("Vocab_BulkDeleteProgress");

        try
        {
            int count = await Task.Run(
                () => _vocabService.DeleteWordsAsync(targetIds, cancellationToken, progress),
                cancellationToken);

            await _dispatcherQueue.EnsureOnUIThreadAsync();

            var deletedSet = new HashSet<Guid>(targetIds);
            for (int i = Words.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(Words[i].Id))
                {
                    Words.RemoveAt(i);
                }
            }

            TotalWordCount = Math.Max(0, TotalWordCount - count);
            if (SelectedWord != null && deletedSet.Contains(SelectedWord.Id))
            {
                SelectedWord = Words.FirstOrDefault();
            }

            SelectedWordIds.Clear();
            IsSelectionMode = false;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));

            if (Words.Count == 0 && CurrentPage > 1)
            {
                CurrentPage--;
                await LoadWordsAsync();
            }

            await LoadContextUnitsAsync();
            return count;
        }
        finally
        {
            _isDeletingWords = false;
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (IsLoading)
            {
                IsLoading = false;
            }
        }
    }

    private bool _isDeletingUnits = false;

    public async Task<int> DeleteSelectedContextUnitsAsync(
        IEnumerable<Guid>? ids = null,
        CancellationToken cancellationToken = default)
    {
        var targetIds = ids?.Where(id => id != Guid.Empty).Distinct().ToList()
            ?? SelectedContextUnitIds.Where(id => id != Guid.Empty).ToList();
        if (targetIds.Count == 0 || _isDeletingUnits) return 0;

        _isDeletingUnits = true;
        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;
        StatusMessage = _localizationService.GetString("Vocab_BulkDeleteTopicsProgress");

        try
        {
            int count = await _vocabService.DeleteTopicsAsync(targetIds, cancellationToken);

            await _dispatcherQueue.EnsureOnUIThreadAsync();

            var deletedSet = new HashSet<Guid>(targetIds);

            // Immediate removal from in-memory collections for instant UI update
            for (int i = ContextUnits.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(ContextUnits[i].Id))
                {
                    ContextUnits.RemoveAt(i);
                }
            }

            for (int i = FilteredContextUnits.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(FilteredContextUnits[i].Id))
                {
                    FilteredContextUnits.RemoveAt(i);
                }
            }

            for (int i = AvailableTopics.Count - 1; i >= 0; i--)
            {
                if (deletedSet.Contains(AvailableTopics[i].Id))
                {
                    AvailableTopics.RemoveAt(i);
                }
            }

            // If the filtered topic was deleted, reset filter immediately
            if (SelectedTopicId.HasValue && deletedSet.Contains(SelectedTopicId.Value))
            {
                SelectedTopicId = null;
                await LoadWordsAsync();
            }

            SelectedContextUnitIds.Clear();
            IsSelectionMode = false;
            OnPropertyChanged(nameof(SelectedContextUnitCount));
            OnPropertyChanged(nameof(HasContextUnitSelection));

            await LoadFiltersAsync();
            await LoadContextUnitsAsync();

            StatusMessage = string.Format(_localizationService.GetString("Vocab_BulkDeleteTopicsSuccess"), count);
            return count;
        }
        finally
        {
            _isDeletingUnits = false;
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            if (IsLoading)
            {
                IsLoading = false;
            }
        }
    }

    private bool _isMarkingStatus = false;

    [RelayCommand]
    public async Task MarkWordStatusAsync(bool remembered)
    {
        if (SelectedWord == null || _isMarkingStatus) return;
        _isMarkingStatus = true;
        try
        {
            var wordId = SelectedWord.Id;
            var term = SelectedWord.Term;
            await _reviewService.MarkStatusQuickAsync(wordId, remembered);

            var newStatus = remembered ? LearningStatus.Remembered : LearningStatus.NotRemembered;
            var updatedWord = SelectedWord with { LearningStatus = newStatus };

            int idx = -1;
            for (int i = 0; i < Words.Count; i++)
            {
                if (Words[i].Id == wordId)
                {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0)
            {
                Words[idx] = updatedWord;
            }
            SelectedWord = updatedWord;
            _ = LoadContextUnitsAsync();

            StatusMessage = remembered
                ? string.Format(_localizationService.GetString("Vocab_Status_MarkedRemembered"), term)
                : string.Format(_localizationService.GetString("Vocab_Status_MarkedNotRemembered"), term);
        }
        finally
        {
            _isMarkingStatus = false;
        }
    }
}
