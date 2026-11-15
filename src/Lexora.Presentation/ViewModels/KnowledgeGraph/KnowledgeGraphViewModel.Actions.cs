using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Views;

namespace Lexora.Presentation.ViewModels;

public partial class KnowledgeGraphViewModel
{
    public async Task LoadGraphAsync(
        Guid wordId,
        bool addToHistory = true,
        CancellationToken cancellationToken = default)
    {
        if (wordId == Guid.Empty) return;

        _graphCts?.Cancel();
        _graphCts?.Dispose();
        _graphCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _graphCts.Token;

        if (addToHistory && CurrentCenterWordId.HasValue && CurrentCenterWordId.Value != wordId)
        {
            _historyStack.Push(CurrentCenterWordId.Value);
            OnPropertyChanged(nameof(HasHistory));
        }

        CurrentCenterWordId = wordId;
        IsGraphLoading = true;

        try
        {
            var graph = await _graphService.BuildGraphAsync(wordId, token);
            token.ThrowIfCancellationRequested();

            if (graph != null)
            {
                ActiveGraph = graph;
                SelectedNode = graph.CenterNode;
                ApplyCategoryFilter();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when superseded by another graph load
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] LoadGraphAsync error: {ex.Message}");
            _toastService?.ShowError(ex.Message, _localizationService.GetString("Common_Error"));
        }
        finally
        {
            IsGraphLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadGraphForTermAsync(string term, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term)) return;

        _graphCts?.Cancel();
        _graphCts?.Dispose();
        _graphCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _graphCts.Token;

        IsGraphLoading = true;
        try
        {
            var graph = await _graphService.BuildGraphForTermAsync(term.Trim(), token);
            token.ThrowIfCancellationRequested();

            if (graph != null)
            {
                if (CurrentCenterWordId.HasValue && CurrentCenterWordId.Value != graph.CenterNode.Id)
                {
                    _historyStack.Push(CurrentCenterWordId.Value);
                    OnPropertyChanged(nameof(HasHistory));
                }

                CurrentCenterWordId = graph.CenterNode.Id;
                ActiveGraph = graph;
                SelectedNode = graph.CenterNode;
                ApplyCategoryFilter();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when superseded
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] LoadGraphForTermAsync error: {ex.Message}");
        }
        finally
        {
            IsGraphLoading = false;
        }
    }

    [RelayCommand]
    public async Task NavigateBackAsync(CancellationToken cancellationToken = default)
    {
        if (_historyStack.Count > 0)
        {
            var previousId = _historyStack.Pop();
            OnPropertyChanged(nameof(HasHistory));
            await LoadGraphAsync(previousId, addToHistory: false, cancellationToken);
        }
    }

    [RelayCommand]
    public void SelectNode(GraphNodeDto node)
    {
        SelectedNode = node;
    }

    [RelayCommand]
    public async Task PivotToNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var node = ActiveGraph?.ConnectedNodes?.FirstOrDefault(n => n.Id == nodeId);
        await PivotToNodeInternalAsync(nodeId, node?.Term, cancellationToken);
    }

    public async Task PivotToNodeInternalAsync(
        Guid nodeId,
        string? term = null,
        CancellationToken cancellationToken = default)
    {
        if (IsSubmitting) return;
        IsSubmitting = true;

        try
        {
            if (nodeId != Guid.Empty && nodeId != CurrentCenterWordId)
            {
                var graph = await _graphService.BuildGraphAsync(nodeId, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (graph != null)
                {
                    if (CurrentCenterWordId.HasValue && CurrentCenterWordId.Value != graph.CenterNode.Id)
                    {
                        _historyStack.Push(CurrentCenterWordId.Value);
                        OnPropertyChanged(nameof(HasHistory));
                    }

                    CurrentCenterWordId = graph.CenterNode.Id;
                    ActiveGraph = graph;
                    SelectedNode = graph.CenterNode;
                    ApplyCategoryFilter();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                await LoadGraphForTermAsync(term, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public Task FilterByCategoryAsync(string category)
    {
        SelectedCategoryFilter = category;
        ApplyCategoryFilter();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task PlayCenterPronunciationAsync(string? accent = "us")
    {
        var term = ActiveGraph?.CenterNode?.Term;
        if (string.IsNullOrWhiteSpace(term) || _audioPlayerService == null) return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(term, accent ?? "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] PlayCenterPronunciationAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task PlaySelectedPronunciationAsync(string? accent = "us")
    {
        var term = SelectedNode?.Term ?? ActiveGraph?.CenterNode?.Term;
        if (string.IsNullOrWhiteSpace(term) || _audioPlayerService == null) return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(term, accent ?? "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] PlaySelectedPronunciationAsync error: {ex.Message}");
        }
    }

    public async Task PlayTermPronunciationAsync(string? term, string? accent = "us")
    {
        if (string.IsNullOrWhiteSpace(term) || _audioPlayerService == null) return;

        try
        {
            await _audioPlayerService.PlayPronunciationAsync(term, accent ?? "us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] PlayTermPronunciationAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public Task PlayPronunciationAsync(string? accent = "us")
        => PlaySelectedPronunciationAsync(accent);

    [RelayCommand]
    public void NavigateToVocabulary(Guid? wordId = null)
    {
        var targetId = wordId ?? SelectedNode?.Id ?? ActiveGraph.CenterNode.Id;
        if (targetId != Guid.Empty)
        {
            _navigationService.Navigate(typeof(VocabularyPage), targetId);
        }
        else
        {
            _navigationService.Navigate(typeof(VocabularyPage));
        }
    }

    [RelayCommand]
    public void NavigateToTest(Guid? wordId = null)
    {
        var targetId = wordId ?? SelectedNode?.Id ?? ActiveGraph.CenterNode.Id;
        if (targetId != Guid.Empty)
        {
            _navigationService.Navigate(typeof(TestPage), targetId);
        }
        else
        {
            _navigationService.Navigate(typeof(TestPage));
        }
    }

    [RelayCommand]
    public void ToggleViewMode()
    {
        IsVisualMapMode = !IsVisualMapMode;
    }

    private void ApplyCategoryFilter()
    {
        FilteredConnectedNodes.Clear();
        var nodes = SelectedCategoryFilter switch
        {
            "Synonym" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.Synonym),
            "Antonym" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.Antonym),
            "WordFamily" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.WordFamily),
            "Collocation" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.Collocation),
            "TopicCluster" => ActiveGraph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.TopicCluster),
            _ => ActiveGraph.ConnectedNodes.AsEnumerable()
        };

        foreach (var node in nodes)
        {
            FilteredConnectedNodes.Add(node);
        }
    }

    [RelayCommand]
    public void CopySelectedTerm()
    {
        var term = SelectedNode?.Term ?? ActiveGraph?.CenterNode?.Term;
        if (string.IsNullOrWhiteSpace(term)) return;

        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(term);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            _toastService?.ShowSuccess(string.Format(_localizationService.GetString("KnowledgeGraph_TermCopied"), term));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] CopySelectedTerm error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void CopySelectedMeaning()
    {
        var meaning = SelectedNode?.MeaningVi ?? ActiveGraph?.CenterNode?.MeaningVi;
        if (string.IsNullOrWhiteSpace(meaning)) return;

        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(meaning);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            _toastService?.ShowSuccess(string.Format(_localizationService.GetString("KnowledgeGraph_MeaningCopied"), meaning));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphViewModel] CopySelectedMeaning error: {ex.Message}");
        }
    }

    public async Task<string> ExportAsync(string format, CancellationToken cancellationToken = default)
    {
        var targetWordId = ActiveGraph?.CenterNode?.Id != Guid.Empty ? ActiveGraph?.CenterNode?.Id : null;
        return format.ToLowerInvariant() switch
        {
            "csv" => await _exportImportService.ExportToCsvAsync(targetWordId, cancellationToken),
            "md" or "markdown" => await _exportImportService.ExportToMarkdownMindmapAsync(targetWordId, cancellationToken),
            _ => await _exportImportService.ExportToJsonAsync(targetWordId, cancellationToken)
        };
    }

    public string GetSampleTemplate(string format) => _exportImportService.GetSampleTemplate(format);
}
