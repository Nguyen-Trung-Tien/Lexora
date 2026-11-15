using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Windows.ApplicationModel.DataTransfer;

namespace Lexora.Presentation.ViewModels;

public partial class PhrasesViewModel
{
    public Task<IReadOnlyList<PhraseSmartSuggestionDto>> GetQuickTemplatesAsync(
        string? category = null,
        CancellationToken cancellationToken = default)
        => phraseService.GetQuickTemplatesAsync(category, cancellationToken);

    public async Task<ConversationPhraseDto?> CreatePhraseAsync(CreateConversationPhraseDto dto)
    {
        if (IsSubmitting) return null;
        IsSubmitting = true;

        try
        {
            var created = await phraseService.CreateAsync(dto);
            await LoadCategoriesAsync();
            await RefreshPhrasesAsync(resetPage: true);

            var match = Phrases.FirstOrDefault(p => p.Id == created.Id);
            if (match is not null)
            {
                SelectedPhrase = match;
            }

            SetStatus(localizationService.GetString("Phrases_SaveSuccess"), isError: false);
            return created;
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorSave")}: {ex.Message}", isError: true);
            return null;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public async Task<ConversationPhraseDto?> UpdatePhraseAsync(UpdateConversationPhraseDto dto)
    {
        if (IsSubmitting) return null;
        IsSubmitting = true;

        try
        {
            var updated = await phraseService.UpdateAsync(dto);
            SyncItemInCollections(updated);
            await LoadCategoriesAsync();
            RefreshCurrentPageSlice();
            UpdateSummaryCounters();

            if (SelectedPhrase?.Id == updated.Id)
            {
                SelectedPhrase.UpdateFrom(updated);
            }

            SetStatus(localizationService.GetString("Phrases_SaveSuccess"), isError: false);
            return updated;
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorSave")}: {ex.Message}", isError: true);
            return null;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync(ConversationPhraseDto? phrase)
    {
        if (phrase is null || IsSubmitting) return;
        IsSubmitting = true;

        try
        {
            var newState = await phraseService.ToggleFavoriteAsync(phrase.Id);
            phrase.IsFavorite = newState;

            if (OnlyFavorites && !newState)
            {
                await RefreshPhrasesAsync(resetPage: false);
            }
            else
            {
                UpdateSummaryCounters();
            }
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorSave")}: {ex.Message}", isError: true);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public async Task DeletePhraseAsync(ConversationPhraseDto? phrase)
    {
        if (phrase is null || IsSubmitting) return;
        IsSubmitting = true;

        try
        {
            if (phrase.IsAnyAudioPlaying)
            {
                StopAudio();
            }

            await phraseService.DeleteAsync(phrase.Id);
            _allFilteredPhrases.RemoveAll(p => p.Id == phrase.Id);
            Phrases.Remove(phrase);

            if (SelectedPhrase?.Id == phrase.Id)
            {
                SelectedPhrase = Phrases.FirstOrDefault();
            }

            RefreshCurrentPageSlice();
            UpdateSummaryCounters();
            SetStatus(localizationService.GetString("Phrases_DeleteSuccess"), isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorDelete")}: {ex.Message}", isError: true);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public void CopyText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
            SetStatus(localizationService.GetString("Phrases_Copied"), isError: false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesViewModel] CopyText failed: {ex.Message}");
        }
    }

    private void SyncItemInCollections(ConversationPhraseDto updated)
    {
        var backingMatch = _allFilteredPhrases.FirstOrDefault(p => p.Id == updated.Id);
        backingMatch?.UpdateFrom(updated);

        var pageMatch = Phrases.FirstOrDefault(p => p.Id == updated.Id);
        pageMatch?.UpdateFrom(updated);

        if (SelectedPhrase?.Id == updated.Id)
        {
            SelectedPhrase.UpdateFrom(updated);
        }
    }
}
