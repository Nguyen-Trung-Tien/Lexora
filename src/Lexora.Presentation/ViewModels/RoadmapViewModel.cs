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
    public partial StudyRoadmapDto? ActiveRoadmap { get; set; }

    [ObservableProperty]
    public partial bool HasActiveRoadmap { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAdvanceProgress))]
    public partial bool IsSubmittingAction { get; set; }

    public bool CanAdvanceProgress => !IsSubmittingAction && SelectedNode != null && SelectedNode.IsActionable;

    [ObservableProperty]
    public partial ExamTarget SelectedExamTarget { get; set; } = ExamTarget.Toeic;

    [ObservableProperty]
    public partial double StartingScore { get; set; } = 450;

    [ObservableProperty]
    public partial double TargetScore { get; set; } = 750;

    [ObservableProperty]
    public partial int DurationWeeks { get; set; } = 12;

    [ObservableProperty]
    public partial RoadmapNodeDto? SelectedNode { get; set; }

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
        try
        {
            IsLoading = true;
            ActiveRoadmap = await _roadmapService.GetActiveRoadmapAsync(cancellationToken);
            HasActiveRoadmap = ActiveRoadmap != null;

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
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task GenerateRoadmapAsync()
    {
        if (IsLoading || !_actionLock.Wait(0)) return;

        try
        {
            IsLoading = true;
            var request = new GenerateRoadmapRequestDto(
                string.Empty,
                SelectedExamTarget,
                StartingScore,
                TargetScore,
                DurationWeeks);

            ActiveRoadmap = await _roadmapService.GenerateAdaptiveRoadmapAsync(request);
            HasActiveRoadmap = ActiveRoadmap != null;

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

            IsNodeDetailOpen = false;
            SelectedNode = null;
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
    }

    [RelayCommand]
    public void SelectNode(RoadmapNodeDto node)
    {
        SelectedNode = node;
        IsNodeDetailOpen = node != null;
        OnPropertyChanged(nameof(CanAdvanceProgress));
    }

    [RelayCommand]
    public void CloseNodeDetail()
    {
        IsNodeDetailOpen = false;
        SelectedNode = null;
        OnPropertyChanged(nameof(CanAdvanceProgress));
    }

    [RelayCommand]
    public async Task AdvanceNodeProgressAsync(RoadmapNodeDto? node)
    {
        var target = node ?? SelectedNode;
        if (target == null || target.Status == NodeStatus.Locked || !_actionLock.Wait(0)) return;

        try
        {
            IsSubmittingAction = true;
            // Simulate learning items (e.g., 5 items completed with high retention EF 2.6)
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
    public void NavigateToModule(RoadmapNodeDto? node)
    {
        var target = node ?? SelectedNode;
        if (target == null) return;

        switch (target.Type)
        {
            case RoadmapNodeType.VocabularySet:
                _navService.Navigate(typeof(VocabularyPage));
                break;
            case RoadmapNodeType.GrammarPoint:
                _navService.Navigate(typeof(GrammarPage));
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

