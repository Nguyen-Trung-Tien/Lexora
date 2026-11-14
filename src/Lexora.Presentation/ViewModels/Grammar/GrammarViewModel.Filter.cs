using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class GrammarViewModel
{
    private CancellationTokenSource? _loadCts;

    [RelayCommand]
    public async Task LoadTopicsAsync()
    {
        try
        {
            var topics = await _grammarService.GetAllTopicsAsync();
            await _dispatcherQueue.EnsureOnUIThreadAsync();
            Topics.Clear();
            foreach (var t in topics) Topics.Add(t);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarViewModel] LoadTopicsAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadRulesAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        await _dispatcherQueue.EnsureOnUIThreadAsync();
        IsLoading = true;
        try
        {
            var pagedResult = await _grammarService.GetPagedRulesAsync(
                page: CurrentPage,
                pageSize: PageSize,
                level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                examType: SelectedExamTarget == ExamType.General ? null : SelectedExamTarget,
                topic: SelectedTopic,
                search: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                cancellationToken: ct
            );

            if (ct.IsCancellationRequested) return;

            await _dispatcherQueue.EnsureOnUIThreadAsync();

            TotalRuleCount = pagedResult.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalRuleCount / PageSize));

            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
                pagedResult = await _grammarService.GetPagedRulesAsync(
                    page: CurrentPage,
                    pageSize: PageSize,
                    level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                    examType: SelectedExamTarget == ExamType.General ? null : SelectedExamTarget,
                    topic: SelectedTopic,
                    search: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                    cancellationToken: ct
                );
                if (ct.IsCancellationRequested) return;
                await _dispatcherQueue.EnsureOnUIThreadAsync();
                TotalRuleCount = pagedResult.TotalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalRuleCount / PageSize));
            }
            if (CurrentPage < 1) CurrentPage = 1;

            Rules.Clear();
            foreach (var r in pagedResult.Rules)
            {
                Rules.Add(r);
            }

            if (Rules.Count > 0)
            {
                if (SelectedRule == null || !Rules.Any(r => r.Id == SelectedRule.Id))
                {
                    SelectedRule = Rules[0];
                }
                ClosestSuggestions = Array.Empty<string>();
                HasClosestSuggestions = false;
                TypoSuggestion = null;
                HasTypoSuggestion = false;
            }
            else
            {
                SelectedRule = null;
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    try
                    {
                        var smartRes = await _grammarService.SmartSearchRulesAsync(
                            search: SearchQuery,
                            level: SelectedLevel == CefrLevel.None ? null : SelectedLevel,
                            examType: SelectedExamTarget == ExamType.General ? null : SelectedExamTarget,
                            topic: SelectedTopic,
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
                            ClosestSuggestions = Array.Empty<string>();
                            HasClosestSuggestions = false;
                            TypoSuggestion = null;
                            HasTypoSuggestion = false;
                        }
                    }
                    catch
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
        }
        catch (OperationCanceledException)
        {
            // Ignore cancelled queries
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarViewModel] LoadRulesAsync error: {ex.Message}");
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                await _dispatcherQueue.EnsureOnUIThreadAsync();
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public async Task FirstPageAsync()
    {
        if (CurrentPage != 1)
        {
            CurrentPage = 1;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public async Task LastPageAsync()
    {
        if (CurrentPage != TotalPages)
        {
            CurrentPage = TotalPages;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public async Task ChangePageSizeAsync(int newSize)
    {
        if (newSize > 0 && PageSize != newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadRulesAsync();
        }
    }

    [RelayCommand]
    public void ResetFilters()
    {
        SearchQuery = string.Empty;
        SelectedLevel = null;
        SelectedExamTarget = null;
        SelectedTopic = null;
        CurrentPage = 1;
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
        SearchQuery = suggestion;
        TypoSuggestion = null;
        HasTypoSuggestion = false;
        ClosestSuggestions = Array.Empty<string>();
        HasClosestSuggestions = false;
        CurrentPage = 1;
        await LoadRulesAsync();

        if (Rules.Count == 0 && (SelectedLevel != CefrLevel.None || SelectedExamTarget != ExamType.General || SelectedTopic != null))
        {
            SelectedLevel = CefrLevel.None;
            SelectedExamTarget = ExamType.General;
            SelectedTopic = null;
            CurrentPage = 1;
            await LoadRulesAsync();
        }
    }
}
