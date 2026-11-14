using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class VocabularyViewModel
{
    private CancellationTokenSource? _loadCts;

    [RelayCommand]
    public async Task LoadFiltersAsync()
    {
        try
        {
            var topics = await _vocabService.GetAllTopicsAsync();
            var tags = await _vocabService.GetAllExamTagsAsync();
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            AvailableTopics.Clear();
            foreach (var t in topics)
            {
                AvailableTopics.Add(t);
            }

            AvailableExamTags.Clear();
            foreach (var et in tags)
            {
                AvailableExamTags.Add(et);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyViewModel] LoadFiltersAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SelectQuickFilterAsync(string filter)
    {
        try
        {
            SelectedQuickFilter = filter;
            switch (filter)
            {
                case "All":
                case "DueToday":
                case "Weak":
                    SelectedStatus = null;
                    break;
                case "Learning":
                    SelectedStatus = LearningStatus.Learning;
                    break;
                case "Mastered":
                    SelectedStatus = LearningStatus.Mastered;
                    break;
                case "New":
                    SelectedStatus = LearningStatus.New;
                    break;
            }

            await LoadWordsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyViewModel] SelectQuickFilter error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadWordsAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;
        try
        {
            List<WordDto> resultList;

            bool isDueToday = SelectedQuickFilter == "DueToday";
            bool isWeak = SelectedQuickFilter == "Weak";

            var total = await _vocabService.CountWordsAsync(
                search: string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                pos: SelectedPartOfSpeech,
                topicId: SelectedTopicId,
                examType: SelectedExamType,
                status: SelectedStatus,
                isDueToday: isDueToday ? true : null,
                isWeak: isWeak ? true : null,
                cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

            TotalWordCount = total;
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalWordCount / PageSize));

            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var results = await _vocabService.SearchWordsAsync(
                search: string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                pos: SelectedPartOfSpeech,
                topicId: SelectedTopicId,
                examType: SelectedExamType,
                status: SelectedStatus,
                sortBy: isDueToday ? "nextreview" : SortBy,
                isAscending: isDueToday || IsAscending,
                isDueToday: isDueToday ? true : null,
                isWeak: isWeak ? true : null,
                page: CurrentPage,
                pageSize: PageSize,
                cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

            var filteredResults = results.AsEnumerable();
            if (SortBy == "pos")
            {
                filteredResults = IsAscending
                    ? filteredResults.OrderBy(w => w.PrimaryPartOfSpeech).ThenBy(w => w.Term)
                    : filteredResults.OrderByDescending(w => w.PrimaryPartOfSpeech).ThenBy(w => w.Term);
            }
            else if (SortBy == "multipos")
            {
                filteredResults = filteredResults
                    .OrderByDescending(w => w.AllPartsOfSpeech.Count)
                    .ThenBy(w => w.Term);
            }

            resultList = filteredResults.ToList();

            await _dispatcherQueue.EnsureOnUIThreadAsync();

            Words.Clear();
            foreach (var w in resultList)
            {
                Words.Add(w);
                if (!_knownTerms.Contains(w.Term, StringComparer.OrdinalIgnoreCase))
                {
                    _knownTerms.Add(w.Term);
                }
            }

            if (Words.Count > 0)
            {
                var matched = SelectedWord != null ? Words.FirstOrDefault(w => w.Id == SelectedWord.Id) : null;
                SelectedWord = matched ?? Words[0];
            }
            else
            {
                SelectedWord = null;
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var normSearch = Lexora.Domain.Common.SearchTextHelper.Normalize(SearchTerm);
                bool hasDirectTermOrDefMatch = Words.Any(w =>
                    Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).StartsWith(normSearch, StringComparison.OrdinalIgnoreCase) ||
                    Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).Equals(normSearch, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(w.PrimaryDefinition) && Lexora.Domain.Common.SearchTextHelper.Normalize(w.PrimaryDefinition).Contains(normSearch, StringComparison.OrdinalIgnoreCase)));

                if (TotalWordCount == 0 || !hasDirectTermOrDefMatch)
                {
                    try
                    {
                        var smartRes = await _vocabService.SmartSearchWordsAsync(
                            search: SearchTerm,
                            level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                            pos: SelectedPartOfSpeech,
                            topicId: SelectedTopicId,
                            examType: SelectedExamType,
                            status: SelectedStatus,
                            page: 1,
                            pageSize: 1,
                            cancellationToken: ct);

                        if (smartRes.ClosestSuggestions.Count > 0)
                        {
                            ClosestSuggestions = smartRes.ClosestSuggestions;
                            HasClosestSuggestions = true;
                            TypoSuggestion = smartRes.ClosestSuggestions[0];
                            HasTypoSuggestion = true;
                        }
                        else
                        {
                            CheckTypoSuggestion();
                        }
                    }
                    catch
                    {
                        CheckTypoSuggestion();
                    }
                }
                else
                {
                    ClosestSuggestions = Array.Empty<string>();
                    HasClosestSuggestions = false;
                    TypoSuggestion = null;
                    HasTypoSuggestion = false;
                }
            }
            else
            {
                ClosestSuggestions = Array.Empty<string>();
                HasClosestSuggestions = false;
                TypoSuggestion = null;
                HasTypoSuggestion = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancelled queries
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyViewModel] LoadWordsAsync error: {ex.Message}");
        }
        finally
        {
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public async Task FirstPageAsync()
    {
        if (CurrentPage != 1)
        {
            CurrentPage = 1;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public async Task LastPageAsync()
    {
        if (CurrentPage != TotalPages)
        {
            CurrentPage = TotalPages;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public async Task ChangePageSizeAsync(int newSize)
    {
        if (newSize > 0 && PageSize != newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadWordsAsync();
        }
    }

    [RelayCommand]
    public void ResetFilters()
    {
        SearchTerm = string.Empty;
        SelectedLevel = null;
        SelectedPartOfSpeech = null;
        SelectedTopicId = null;
        SelectedExamType = null;
        SelectedStatus = null;
        SelectedQuickFilter = "All";
    }

    public async Task CheckTypoSuggestionAsync(CancellationToken cancellationToken = default)
    {
        var query = SearchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2 || Words.Count > 0)
        {
            TypoSuggestion = null;
            HasTypoSuggestion = false;
            ClosestSuggestions = Array.Empty<string>();
            HasClosestSuggestions = false;
            return;
        }

        var closest = await GetClosestSuggestionsAsync(query, maxSuggestions: 4, cancellationToken);
        if (closest.Count > 0)
        {
            ClosestSuggestions = closest;
            HasClosestSuggestions = true;
            TypoSuggestion = closest[0];
            HasTypoSuggestion = true;
        }
        else
        {
            TypoSuggestion = null;
            HasTypoSuggestion = false;
            ClosestSuggestions = Array.Empty<string>();
            HasClosestSuggestions = false;
        }
    }

    public void CheckTypoSuggestion() => _ = CheckTypoSuggestionAsync();

    public async Task<IReadOnlyList<string>> GetClosestSuggestionsAsync(string query, int maxSuggestions = 4, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return Array.Empty<string>();

        try
        {
            var suggestions = await _vocabService.GetClosestSuggestionsAsync(query, maxSuggestions, cancellationToken);
            if (suggestions.Count > 0) return suggestions;
        }
        catch
        {
            // Fallback to in-memory candidates
        }

        var fallbackCandidates = _knownTerms.Count > 0 ? _knownTerms : ContextUnits.SelectMany(u => u.Words).Select(w => w.Term).Distinct();
        var suggestion = _fuzzySearchService.SuggestCorrection(query, fallbackCandidates);
        return !string.IsNullOrEmpty(suggestion) && !string.Equals(suggestion, query, StringComparison.OrdinalIgnoreCase)
            ? new[] { suggestion }
            : Array.Empty<string>();
    }

    public IReadOnlyList<string> GetClosestSuggestions(string query, int maxSuggestions = 4)
    {
        return GetClosestSuggestionsAsync(query, maxSuggestions).GetAwaiter().GetResult();
    }

    public async Task<IReadOnlyList<WordDto>> SearchWordSuggestionsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<WordDto>();
        var results = await _vocabService.SearchWordsAsync(search: query, pageSize: 12, cancellationToken: cancellationToken);
        var normQuery = Lexora.Domain.Common.SearchTextHelper.Normalize(query);

        return results
            .OrderByDescending(w => Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).Equals(normQuery, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(w => Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term).StartsWith(normQuery, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(w => w.Meanings != null && w.Meanings.Any(m => Lexora.Domain.Common.SearchTextHelper.Normalize(m.DefinitionVi).StartsWith(normQuery, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(w => w.Term.Length)
            .Take(6)
            .ToList();
    }

    [RelayCommand]
    public async Task ApplyTypoSuggestionAsync()
    {
        if (string.IsNullOrWhiteSpace(TypoSuggestion)) return;
        await ApplyClosestSuggestionAsync(TypoSuggestion);
    }

    [RelayCommand]
    public async Task ApplyClosestSuggestionAsync(string? suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion)) return;
        SearchTerm = suggestion;
        TypoSuggestion = null;
        HasTypoSuggestion = false;
        ClosestSuggestions = Array.Empty<string>();
        HasClosestSuggestions = false;
        CurrentPage = 1;
        await LoadWordsAsync();
        ApplyUnitFilter();

        if (TotalWordCount == 0 && (SelectedLevel != CefrLevel.None || SelectedPartOfSpeech != null || SelectedTopicId != null || SelectedExamType != null || SelectedStatus != null || SelectedQuickFilter != "All"))
        {
            SelectedLevel = CefrLevel.None;
            SelectedPartOfSpeech = null;
            SelectedTopicId = null;
            SelectedExamType = null;
            SelectedStatus = null;
            SelectedQuickFilter = "All";
            CurrentPage = 1;
            await LoadWordsAsync();
            ApplyUnitFilter();
        }

        // Auto-select the word in the list so details pane updates immediately
        if (Words.Count > 0)
        {
            var match = Words.FirstOrDefault(w => w.Term.Equals(suggestion, StringComparison.OrdinalIgnoreCase))
                ?? Words.FirstOrDefault(w => w.Term.StartsWith(suggestion, StringComparison.OrdinalIgnoreCase))
                ?? Words[0];
            SelectedWord = match;
        }
    }

    [RelayCommand]
    public async Task ApplySearchTermAsync(string term)
    {
        await ApplyClosestSuggestionAsync(term);
    }
}
