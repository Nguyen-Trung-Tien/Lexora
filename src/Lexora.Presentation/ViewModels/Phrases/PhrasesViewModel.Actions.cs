using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Windows.ApplicationModel.DataTransfer;

namespace Lexora.Presentation.ViewModels;

public partial class PhrasesViewModel
{
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
        if (target == CurrentPage && Phrases.Count > 0) return;
        CurrentPage = target;
        RefreshCurrentPageSlice();
    }

    [RelayCommand]
    public void ChangePageSize(int newSize)
    {
        if (newSize <= 0 || newSize == PageSize) return;
        PageSize = newSize;
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync(ConversationPhraseDto? phrase, CancellationToken cancellationToken = default)
    {
        if (phrase == null || IsSubmitting) return;

        IsSubmitting = true;
        try
        {
            var newStatus = await _phraseService.ToggleFavoriteAsync(phrase.Id, cancellationToken);
            phrase.IsFavorite = newStatus;

            if (OnlyFavorites && !newStatus)
            {
                _allFilteredPhrases.Remove(phrase);
                TotalCount = _allFilteredPhrases.Count;
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = Math.Max(1, TotalPages);
                }
                RefreshCurrentPageSlice();
            }
            else
            {
                var match = _allFilteredPhrases.FirstOrDefault(p => p.Id == phrase.Id);
                if (match != null && match != phrase)
                {
                    match.IsFavorite = newStatus;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] ToggleFavorite error: {ex.Message}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public async Task DeletePhraseAsync(ConversationPhraseDto? phrase, CancellationToken cancellationToken = default)
    {
        if (phrase == null || IsSubmitting) return;

        IsSubmitting = true;
        try
        {
            await _phraseService.DeleteAsync(phrase.Id, cancellationToken);
            _allFilteredPhrases.Remove(phrase);
            TotalCount = _allFilteredPhrases.Count;
            if (CurrentPage > TotalPages)
            {
                CurrentPage = Math.Max(1, TotalPages);
            }
            RefreshCurrentPageSlice();
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] DeletePhrase error: {ex.Message}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public async Task PlayPromptAudioAsync(ConversationPhraseDto? phrase)
    {
        if (phrase == null || string.IsNullOrWhiteSpace(phrase.PromptSentence) || _audioPlayerService == null)
            return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(phrase.PromptSentence, "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] PlayPromptAudio error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task PlayResponseAudioAsync(ConversationPhraseDto? phrase)
    {
        if (phrase == null || string.IsNullOrWhiteSpace(phrase.SampleResponse) || _audioPlayerService == null)
            return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(phrase.SampleResponse, "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] PlayResponseAudio error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void CopyText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] CopyText error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
    {
        SearchQuery = string.Empty;
        SelectedCategory = null;
        SelectedLevel = null;
        OnlyFavorites = false;
        await RefreshPhrasesAsync(cancellationToken);
    }

    public void OnPhraseSaved(ConversationPhraseDto savedPhrase, bool isNew)
    {
        if (isNew)
        {
            _allFilteredPhrases.Insert(0, savedPhrase);
            TotalCount = _allFilteredPhrases.Count;
            CurrentPage = 1;
            RefreshCurrentPageSlice();
            if (!string.IsNullOrEmpty(savedPhrase.Category) && !Categories.Contains(savedPhrase.Category))
            {
                Categories.Add(savedPhrase.Category);
            }
        }
        else
        {
            var existing = _allFilteredPhrases.FirstOrDefault(p => p.Id == savedPhrase.Id);
            if (existing != null)
            {
                existing.Category = savedPhrase.Category;
                existing.ContextTag = savedPhrase.ContextTag;
                existing.PromptSentence = savedPhrase.PromptSentence;
                existing.PromptTranslation = savedPhrase.PromptTranslation;
                existing.SampleResponse = savedPhrase.SampleResponse;
                existing.ResponseTranslation = savedPhrase.ResponseTranslation;
                existing.AdditionalSuggestions = savedPhrase.AdditionalSuggestions;
                existing.CefrLevel = savedPhrase.CefrLevel;
                existing.IsFavorite = savedPhrase.IsFavorite;
                existing.UpdatedAt = savedPhrase.UpdatedAt;
            }
        }
    }
}
