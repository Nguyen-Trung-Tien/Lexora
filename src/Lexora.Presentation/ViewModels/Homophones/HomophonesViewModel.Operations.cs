using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class HomophonesViewModel
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

    public async Task<IReadOnlyList<SmartSearchSuggestion>> GetSearchSuggestionsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SmartSearchSuggestion>();
        }

        var clean = query.Trim().ToLowerInvariant();
        var cleanIpa = clean.Trim('/', '[', ']', ' ');
        var suggestions = new List<SmartSearchSuggestion>();

        var all = _allFilteredGroups.Count > 0
            ? _allFilteredGroups
            : await _homophoneService.GetAllGroupsAsync(cancellationToken: cancellationToken);

        foreach (var g in all)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                    HomophoneType.ExactHomophone => _localizationService.GetString("Homophones_TypeExact"),
                    HomophoneType.NearHomophone => _localizationService.GetString("Homophones_TypeNear"),
                    HomophoneType.MinimalPair => _localizationService.GetString("Homophones_TypePair"),
                    _ => _localizationService.GetString("Homophones_SuggestionGroupBadge")
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
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var item in g.Items)
                {
                    if (item.Term.StartsWith(clean, StringComparison.OrdinalIgnoreCase) &&
                        !suggestions.Any(s => s.TargetType == g.Id.ToString()))
                    {
                        suggestions.Add(new SmartSearchSuggestion
                        {
                            Title = $"{item.Term} ({g.Title})",
                            Subtitle = string.Format(_localizationService.GetString("Homophones_SuggestionHintFormat"), g.PhoneticIpa),
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
    public async Task PlayAudioAsync(string? term, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term) || _audioPlayerService == null) return;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _audioPlayerService.PlayPronunciationAsync(term.Trim());
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomophonesViewModel] PlayAudioAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (IsSubmitting) return;
        IsSubmitting = true;
        try
        {
            var success = await _homophoneService.DeleteGroupAsync(id, cancellationToken);
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
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomophonesViewModel] DeleteGroupAsync error: {ex.Message}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public async Task<HomophoneGroupDto?> SaveGroupAsync(
        CreateHomophoneGroupDto createDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        if (IsSubmitting) return null;
        IsSubmitting = true;
        try
        {
            var result = await _homophoneService.CreateGroupAsync(createDto, cancellationToken);
            await LoadGroupsAsync(cancellationToken);
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == result.Id) ?? result;
            return result;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public async Task<HomophoneGroupDto?> UpdateGroupAsync(
        UpdateHomophoneGroupDto updateDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);
        if (IsSubmitting) return null;
        IsSubmitting = true;
        try
        {
            var result = await _homophoneService.UpdateGroupAsync(updateDto, cancellationToken);
            await LoadGroupsAsync(cancellationToken);
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == result.Id) ?? result;
            return result;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public async Task<string> ExportAsync(string format, CancellationToken cancellationToken = default)
    {
        return format.ToLowerInvariant() switch
        {
            "csv" => await _exportImportService.ExportToCsvAsync(cancellationToken: cancellationToken),
            "txt" => await _exportImportService.ExportToTxtAsync(cancellationToken: cancellationToken),
            _ => await _exportImportService.ExportToJsonAsync(cancellationToken: cancellationToken)
        };
    }

    public string GetSampleTemplate(string format)
    {
        return _exportImportService.GetSampleTemplate(format);
    }
}
