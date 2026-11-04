using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.Views;

namespace Lexora.Presentation.ViewModels;

public partial class RoadmapViewModel : ObservableObject, IDisposable
{
    private readonly IRoadmapService _roadmapService;
    private readonly INavigationService _navService;
    private readonly ILocalizationService _localizationService;
    private readonly EventHandler? _languageChangedHandler;
    private readonly SemaphoreSlim _actionLock = new(1, 1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NextActiveNode))]
    [NotifyPropertyChangedFor(nameof(HasNextActiveNode))]
    [NotifyPropertyChangedFor(nameof(HasActiveRoadmap))]
    [NotifyPropertyChangedFor(nameof(Milestones))]
    public partial StudyRoadmapDto? ActiveRoadmap { get; set; }

    public bool HasActiveRoadmap => ActiveRoadmap != null;

    public IReadOnlyList<MilestoneDto> Milestones => ActiveRoadmap?.Milestones ?? Array.Empty<MilestoneDto>();

    public ObservableCollection<StudyRoadmapDto> AllRoadmaps { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMapView))]
    [NotifyPropertyChangedFor(nameof(IsTimelineView))]
    public partial int SelectedViewMode { get; set; } = 0;

    public bool IsMapView => SelectedViewMode == 0;
    public bool IsTimelineView => SelectedViewMode == 1;

    [ObservableProperty]
    public partial double CanvasZoom { get; set; } = 1.0;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAdvanceProgress))]
    public partial bool IsSubmittingAction { get; set; }

    public bool CanAdvanceProgress => !IsSubmittingAction && SelectedNode != null && SelectedNode.CanStartPractice;

    public RoadmapNodeDto? NextActiveNode => ActiveRoadmap?.NextActiveNode;
    public bool HasNextActiveNode => NextActiveNode != null;

    [ObservableProperty]
    public partial ExamTarget SelectedExamTarget { get; set; } = ExamTarget.Toeic;

    [ObservableProperty]
    public partial double StartingScore { get; set; } = 450;

    [ObservableProperty]
    public partial double TargetScore { get; set; } = 750;

    [ObservableProperty]
    public partial int DurationWeeks { get; set; } = 12;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAdvanceProgress))]
    [NotifyPropertyChangedFor(nameof(SelectedNodeLinkedTopicDisplay))]
    public partial RoadmapNodeDto? SelectedNode { get; set; }

    public string SelectedNodeLinkedTopicDisplay => SelectedNode?.ReferenceDisplay ?? string.Empty;

    [ObservableProperty]
    public partial bool IsNodeDetailOpen { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public ObservableCollection<RoadmapNodeDto> VisualNodes { get; } = new();

    public ILocalizationService Loc => _localizationService;

    public IReadOnlyList<ExamTarget> ExamTargets { get; } =
    [
        ExamTarget.Toeic,
        ExamTarget.Ielts,
        ExamTarget.Vstep
    ];

    public RoadmapViewModel(
        IRoadmapService roadmapService,
        INavigationService navService,
        ILocalizationService localizationService)
    {
        _roadmapService = roadmapService ?? throw new ArgumentNullException(nameof(roadmapService));
        _navService = navService ?? throw new ArgumentNullException(nameof(navService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

        _languageChangedHandler = (s, e) => OnPropertyChanged(nameof(Loc));
        _localizationService.LanguageChanged += _languageChangedHandler;
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await LoadActiveRoadmapAsync(cancellationToken);
    }

    [RelayCommand]
    public async Task LoadActiveRoadmapAsync(CancellationToken cancellationToken = default)
    {
        await _actionLock.WaitAsync(cancellationToken);
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            // 1. Synchronize real progress from learning logs and words first
            var syncedRoadmap = await _roadmapService.SyncRoadmapProgressAsync(cancellationToken);
            ActiveRoadmap = syncedRoadmap ?? await _roadmapService.GetActiveRoadmapAsync(cancellationToken);

            var newNodes = new List<RoadmapNodeDto>();
            if (ActiveRoadmap != null)
            {
                foreach (var milestone in ActiveRoadmap.Milestones)
                {
                    foreach (var node in milestone.Nodes)
                    {
                        newNodes.Add(node);
                    }
                }
            }

            // In-place synchronization to prevent UI blanking or flickering on tab navigation
            if (VisualNodes.Count == newNodes.Count && newNodes.Count > 0)
            {
                for (int i = 0; i < newNodes.Count; i++)
                {
                    VisualNodes[i] = newNodes[i];
                }
            }
            else
            {
                VisualNodes.Clear();
                foreach (var node in newNodes)
                {
                    VisualNodes.Add(node);
                }
            }

            OnPropertyChanged(nameof(VisualNodes));
            OnPropertyChanged(nameof(NextActiveNode));
            OnPropertyChanged(nameof(HasNextActiveNode));
            OnPropertyChanged(nameof(Milestones));

            var all = await _roadmapService.GetAllRoadmapsAsync(cancellationToken);
            AllRoadmaps.Clear();
            foreach (var r in all)
            {
                AllRoadmaps.Add(r);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[RoadmapViewModel] LoadActiveRoadmapAsync error: {ex}");
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
    }

    [RelayCommand]
    public async Task SyncProgressAsync()
    {
        await _actionLock.WaitAsync();
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            var updated = await _roadmapService.SyncRoadmapProgressAsync();
            if (updated != null)
            {
                ActiveRoadmap = updated;
                var newNodes = new List<RoadmapNodeDto>();
                foreach (var milestone in ActiveRoadmap.Milestones)
                {
                    foreach (var node in milestone.Nodes)
                    {
                        newNodes.Add(node);
                    }
                }

                // In-place synchronization to prevent UI blanking or flickering
                if (VisualNodes.Count == newNodes.Count && newNodes.Count > 0)
                {
                    for (int i = 0; i < newNodes.Count; i++)
                    {
                        VisualNodes[i] = newNodes[i];
                    }
                }
                else
                {
                    VisualNodes.Clear();
                    foreach (var node in newNodes)
                    {
                        VisualNodes.Add(node);
                    }
                }

                OnPropertyChanged(nameof(VisualNodes));
                OnPropertyChanged(nameof(NextActiveNode));
                OnPropertyChanged(nameof(HasNextActiveNode));

                if (SelectedNode != null)
                {
                    SelectedNode = VisualNodes.FirstOrDefault(n => n.Id == SelectedNode.Id);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[RoadmapViewModel] SyncProgressAsync error: {ex}");
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
    }

    [RelayCommand]
    public async Task GenerateRoadmapAsync(CancellationToken cancellationToken = default)
    {
        await _actionLock.WaitAsync(cancellationToken);
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            var request = new GenerateRoadmapRequestDto(
                string.Empty,
                SelectedExamTarget,
                StartingScore,
                TargetScore,
                DurationWeeks);

            ActiveRoadmap = await _roadmapService.GenerateAdaptiveRoadmapAsync(request, cancellationToken);

            VisualNodes.Clear();
            if (ActiveRoadmap != null)
            {
                foreach (var milestone in ActiveRoadmap.Milestones)
                {
                    foreach (var node in milestone.Nodes)
                    {
                        VisualNodes.Add(node);
                    }
                }
            }
            OnPropertyChanged(nameof(VisualNodes));
            OnPropertyChanged(nameof(NextActiveNode));
            OnPropertyChanged(nameof(HasNextActiveNode));
            OnPropertyChanged(nameof(Milestones));

            var all = await _roadmapService.GetAllRoadmapsAsync(cancellationToken);
            AllRoadmaps.Clear();
            foreach (var r in all)
            {
                AllRoadmaps.Add(r);
            }

            IsNodeDetailOpen = false;
            SelectedNode = null;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[RoadmapViewModel] GenerateRoadmapAsync error: {ex}");
            App.LogCrash("RoadmapViewModel.GenerateRoadmapAsync", ex);
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
    }

    [RelayCommand]
    public async Task SwitchRoadmapAsync(Guid roadmapId)
    {
        await _actionLock.WaitAsync();
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            await _roadmapService.SetActiveRoadmapAsync(roadmapId);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
        await LoadActiveRoadmapAsync();
    }

    [RelayCommand]
    public async Task DeleteRoadmapAsync(Guid roadmapId)
    {
        await _actionLock.WaitAsync();
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            await _roadmapService.DeleteRoadmapAsync(roadmapId);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
        await LoadActiveRoadmapAsync();
    }

    [RelayCommand]
    public async Task QuickCreateRoadmapAsync(string preset)
    {
        (ExamTarget target, double start, double goal, int weeks) = preset switch
        {
            "TOEIC_750" => (ExamTarget.Toeic, 450.0, 750.0, 12),
            "IELTS_65" => (ExamTarget.Ielts, 5.0, 6.5, 16),
            "VSTEP_B2" => (ExamTarget.Vstep, 4.0, 6.5, 10),
            _ => (ExamTarget.Toeic, 450.0, 750.0, 12)
        };

        SelectedExamTarget = target;
        StartingScore = start;
        TargetScore = goal;
        DurationWeeks = weeks;

        await GenerateRoadmapAsync();
    }

    [RelayCommand]
    public void ZoomIn() => CanvasZoom = Math.Min(1.4, Math.Round(CanvasZoom + 0.15, 2));

    [RelayCommand]
    public void ZoomOut() => CanvasZoom = Math.Max(0.65, Math.Round(CanvasZoom - 0.15, 2));

    [RelayCommand]
    public void ResetZoom() => CanvasZoom = 1.0;

    [RelayCommand]
    public void SelectNode(RoadmapNodeDto node)
    {
        SelectedNode = node;
        IsNodeDetailOpen = node != null;
        OnPropertyChanged(nameof(CanAdvanceProgress));
        OnPropertyChanged(nameof(SelectedNodeLinkedTopicDisplay));
    }

    [RelayCommand]
    public void CloseNodeDetail()
    {
        IsNodeDetailOpen = false;
        SelectedNode = null;
        OnPropertyChanged(nameof(CanAdvanceProgress));
    }

    [RelayCommand]
    public async Task StartLearningNodeAsync(RoadmapNodeDto? node)
    {
        var target = node ?? SelectedNode ?? NextActiveNode;
        if (target == null || target.Status == NodeStatus.Locked) return;

        try
        {
            var context = await _roadmapService.GetNodeActionContextAsync(target.Id);

            switch (target.Type)
            {
                case RoadmapNodeType.VocabularySet:
                    if (context?.TopicId != null && !string.IsNullOrWhiteSpace(context.TopicName))
                    {
                        var topicDto = new TopicDto(context.TopicId.Value, context.TopicName, null, null, null);
                        _navService.Navigate(typeof(ReviewPage), topicDto);
                    }
                    else if (context != null && context.LinkedWordIds.Count > 0)
                    {
                        _navService.Navigate(typeof(ReviewPage), context.LinkedWordIds);
                    }
                    else
                    {
                        _navService.Navigate(typeof(ReviewPage));
                    }
                    break;

                case RoadmapNodeType.GrammarPoint:
                    string grammarQuery = context?.GrammarQuery ?? target.ReferenceName ?? target.Title;
                    _navService.Navigate(typeof(GrammarPage), grammarQuery);
                    break;

                case RoadmapNodeType.MockTest:
                case RoadmapNodeType.Checkpoint:
                    _navService.Navigate(typeof(TestPage));
                    break;

                default:
                    _navService.Navigate(typeof(VocabularyPage));
                    break;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task ViewInVocabularyAsync(RoadmapNodeDto? node)
    {
        var target = node ?? SelectedNode;
        if (target == null) return;

        var context = await _roadmapService.GetNodeActionContextAsync(target.Id);
        string query = context?.TopicName ?? target.ReferenceName ?? target.Title;
        _navService.Navigate(typeof(VocabularyPage), query);
    }

    [RelayCommand]
    public async Task AdvanceNodeProgressAsync(RoadmapNodeDto? node)
    {
        var target = node ?? SelectedNode;
        if (target == null || target.Status == NodeStatus.Locked || !_actionLock.Wait(0)) return;

        try
        {
            IsSubmittingAction = true;
            int batch = Math.Max(1, (int)Math.Ceiling(target.TargetItemCount * 0.25));
            await _roadmapService.UpdateNodeProgressAsync(target.Id, batch, 2.6);
            await LoadActiveRoadmapAsync();

            if (SelectedNode != null)
            {
                SelectedNode = VisualNodes.FirstOrDefault(n => n.Id == target.Id);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsSubmittingAction = false;
            _actionLock.Release();
            OnPropertyChanged(nameof(CanAdvanceProgress));
        }
    }

    [RelayCommand]
    public async Task MarkNodeCompletedAsync(RoadmapNodeDto? node)
    {
        var target = node ?? SelectedNode;
        if (target == null || target.Status == NodeStatus.Locked || !_actionLock.Wait(0)) return;

        try
        {
            IsSubmittingAction = true;
            int remaining = Math.Max(1, target.TargetItemCount - target.CompletedItemCount);
            await _roadmapService.UpdateNodeProgressAsync(target.Id, remaining, 2.8);
            await LoadActiveRoadmapAsync();

            if (SelectedNode != null)
            {
                SelectedNode = VisualNodes.FirstOrDefault(n => n.Id == target.Id);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsSubmittingAction = false;
            _actionLock.Release();
            OnPropertyChanged(nameof(CanAdvanceProgress));
        }
    }

    [RelayCommand]
    public void NavigateToModule(RoadmapNodeDto? node)
    {
        var target = node ?? SelectedNode;
        if (target == null) return;

        _ = StartLearningNodeAsync(target);
    }

    public void Dispose()
    {
        if (_languageChangedHandler != null)
        {
            _localizationService.LanguageChanged -= _languageChangedHandler;
        }
        _actionLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
