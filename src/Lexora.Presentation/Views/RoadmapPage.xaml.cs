using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Lexora.Presentation.Views;

public sealed partial class RoadmapPage : Page, ILocalizableView, IThemeAwareView
{
    public RoadmapViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }
    private readonly System.ComponentModel.PropertyChangedEventHandler _propChangedHandler;
    private readonly ScaleTransform _canvasScaleTransform = new();
    private readonly Dictionary<Guid, Border> _nodeVisuals = new();

    public RoadmapPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Current.Services.GetRequiredService<RoadmapViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();
        DataContext = ViewModel;

        InitializeComponent();

        if (RoadmapCanvas != null)
        {
            RoadmapCanvas.RenderTransform = _canvasScaleTransform;
        }

        _propChangedHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.ActiveRoadmap) 
                || e.PropertyName == nameof(ViewModel.HasActiveRoadmap)
                || e.PropertyName == nameof(ViewModel.VisualNodes))
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    Bindings.Update();
                    RenderRoadmapGraph();
                    UpdateTabButtonsState();
                });
            }
            else if (e.PropertyName == nameof(ViewModel.CanvasZoom))
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    _canvasScaleTransform.ScaleX = ViewModel.CanvasZoom;
                    _canvasScaleTransform.ScaleY = ViewModel.CanvasZoom;
                });
            }
            else if (e.PropertyName == nameof(ViewModel.SelectedViewMode))
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    UpdateTabButtonsState();
                    Bindings.Update();
                });
            }
            else if (e.PropertyName == nameof(ViewModel.SelectedNode))
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    UpdateNodeSelectionVisuals();
                    Bindings.Update();
                });
            }
        };
        ViewModel.PropertyChanged += _propChangedHandler;

        Loaded += Page_Loaded;
        Unloaded += (s, e) => ViewModel.PropertyChanged -= _propChangedHandler;
        SizeChanged += Page_SizeChanged;
        ActualThemeChanged += (s, e) => OnThemeChanged();
    }

    public void OnLanguageChanged()
    {
        Bindings.Update();
        RenderRoadmapGraph();
        UpdateTabButtonsState();
    }

    public void OnThemeChanged()
    {
        RenderRoadmapGraph();
        UpdateTabButtonsState();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        try
        {
            OnLanguageChanged();
            OnThemeChanged();

            if (!ViewModel.HasActiveRoadmap || ViewModel.VisualNodes.Count == 0)
            {
                await ViewModel.InitializeAsync();
            }
            else
            {
                RenderRoadmapGraph();
                await ViewModel.SyncProgressAsync();
            }

            Bindings.Update();
            RenderRoadmapGraph();
            UpdateTabButtonsState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapPage] OnNavigatedTo error: {ex.Message}");
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateResponsiveLayout(ActualWidth);
            Bindings.Update();
            RenderRoadmapGraph();
            UpdateTabButtonsState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapPage] Page_Loaded error: {ex.Message}");
        }
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateResponsiveLayout(e.NewSize.Width);
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0 || OverviewCardsGrid == null || RoadmapWorkspaceGrid == null) return;

        bool isCompact = width < 1050;

        // 1. Overview Cards: 4-columns on wide screens, 2x2 grid on compact screens
        OverviewCardsGrid.ColumnDefinitions[2].Width = isCompact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        OverviewCardsGrid.ColumnDefinitions[3].Width = isCompact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);

        Grid.SetRow(OverviewCard1, 0);
        Grid.SetColumn(OverviewCard1, 0);

        Grid.SetRow(OverviewCard2, 0);
        Grid.SetColumn(OverviewCard2, 1);

        Grid.SetRow(OverviewCard3, isCompact ? 1 : 0);
        Grid.SetColumn(OverviewCard3, isCompact ? 0 : 2);

        Grid.SetRow(OverviewCard4, isCompact ? 1 : 0);
        Grid.SetColumn(OverviewCard4, isCompact ? 1 : 3);

        // 2. Canvas + Inspector Workspace: Side-by-side on wide screens, stacked vertically on compact screens
        if (isCompact)
        {
            RoadmapWorkspaceGrid.ColumnDefinitions[1].Width = new GridLength(0);
            Grid.SetRow(CanvasHostBorder, 0);
            Grid.SetColumn(CanvasHostBorder, 0);

            Grid.SetRow(InspectorHostBorder, 1);
            Grid.SetColumn(InspectorHostBorder, 0);
            InspectorHostBorder.Margin = new Thickness(0, 16, 0, 0);
        }
        else
        {
            RoadmapWorkspaceGrid.ColumnDefinitions[1].Width = new GridLength(380);
            Grid.SetRow(CanvasHostBorder, 0);
            Grid.SetColumn(CanvasHostBorder, 0);

            Grid.SetRow(InspectorHostBorder, 0);
            Grid.SetColumn(InspectorHostBorder, 1);
            InspectorHostBorder.Margin = new Thickness(16, 0, 0, 0);
        }
    }

    private void UpdateTabButtonsState()
    {
        if (MapViewTabButton == null || TimelineViewTabButton == null) return;

        if (ViewModel.IsMapView)
        {
            MapViewTabButton.Background = GetThemeBrush("AccentFillColorDefaultBrush");
            MapViewTabButton.Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush");
            TimelineViewTabButton.Background = new SolidColorBrush(Colors.Transparent);
            TimelineViewTabButton.Foreground = GetThemeBrush("TextFillColorPrimaryBrush");
        }
        else
        {
            TimelineViewTabButton.Background = GetThemeBrush("AccentFillColorDefaultBrush");
            TimelineViewTabButton.Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush");
            MapViewTabButton.Background = new SolidColorBrush(Colors.Transparent);
            MapViewTabButton.Foreground = GetThemeBrush("TextFillColorPrimaryBrush");
        }
    }

    private Brush GetThemeBrush(string key)
    {
        return ThemeResourceHelper.GetThemeBrush(this, key)
            ?? new SolidColorBrush(Colors.Transparent);
    }

    private void SwitchToMapView_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedViewMode = 0;
        UpdateTabButtonsState();
        RenderRoadmapGraph();
    }

    private void SwitchToTimelineView_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedViewMode = 1;
        UpdateTabButtonsState();
    }

    private void CenterOnActiveNode_Click(object sender, RoutedEventArgs e)
    {
        var targetNode = ViewModel.NextActiveNode ?? ViewModel.SelectedNode;
        if (targetNode != null && CanvasScrollViewer != null)
        {
            double targetY = Math.Max(0, (targetNode.CanvasY * ViewModel.CanvasZoom) - 180);
            CanvasScrollViewer.ChangeView(null, targetY, null);
        }
    }

    private async void SwitchRoadmapItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is StudyRoadmapDto roadmap)
        {
            RoadmapsFlyout?.Hide();
            await ViewModel.SwitchRoadmapCommand.ExecuteAsync(roadmap.Id);
        }
    }

    private async void DeleteCurrentRoadmap_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveRoadmap == null) return;

        RoadmapsFlyout?.Hide();

        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Roadmap_DeleteConfirmTitle"),
            Content = Loc.GetString("Roadmap_DeleteConfirmMessage"),
            PrimaryButtonText = Loc.GetString("Roadmap_DeleteRoadmap"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            CornerRadius = new CornerRadius(14),
            XamlRoot = this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot
        };

        var result = await confirmDialog.ShowAsyncSafe(this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot);
        if (result == ContentDialogResult.Primary)
        {
            await Task.Yield();
            await ViewModel.DeleteRoadmapCommand.ExecuteAsync(ViewModel.ActiveRoadmap.Id);
        }
    }

    private void TimelineNodeSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.Tag is RoadmapNodeDto node)
        {
            ViewModel.SelectNode(node);
        }
    }

    private void TimelineNodeLearn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.Tag is RoadmapNodeDto node)
        {
            ViewModel.StartLearningNodeCommand.Execute(node);
        }
    }

    private void UpdateNodeSelectionVisuals()
    {
        var selectedId = ViewModel.SelectedNode?.Id;
        foreach (var (nodeId, border) in _nodeVisuals)
        {
            if (border.Tag is RoadmapNodeDto node)
            {
                bool isSelected = nodeId == selectedId;
                border.BorderThickness = new Thickness(isSelected ? 3.0 : (node.Status == NodeStatus.Available ? 2.5 : 1.5));
                if (isSelected)
                {
                    border.BorderBrush = GetThemeBrush("AccentFillColorDefaultBrush");
                }
                else
                {
                    border.BorderBrush = node.Status switch
                    {
                        NodeStatus.Mastered => GetThemeBrush("SystemFillColorSuccessBrush"),
                        NodeStatus.InProgress => GetThemeBrush("AccentFillColorDefaultBrush"),
                        NodeStatus.Available => GetThemeBrush("AccentFillColorDefaultBrush"),
                        _ => GetThemeBrush("CardStrokeColorDefaultBrush")
                    };
                }
            }
        }
    }

    private void RenderRoadmapGraph()
    {
        try
        {
            if (RoadmapCanvas == null || ViewModel?.VisualNodes == null)
            {
                return;
            }

            var nodes = ViewModel.VisualNodes.OrderBy(n => n.CanvasY).ToList();
            if (nodes.Count == 0)
            {
                return;
            }

            _nodeVisuals.Clear();

            // 1. Remove previously dynamically added node controls (keep the 2 track paths)
            var childrenToRemove = RoadmapCanvas.Children
                .Where(c => c != TrackSplinePath && c != ActiveSplinePath)
                .ToList();

            foreach (var child in childrenToRemove)
            {
                RoadmapCanvas.Children.Remove(child);
            }

            // 2. Calculate dynamic available width & horizontal centering
            double containerWidth = CanvasScrollViewer?.ActualWidth > 0 ? CanvasScrollViewer.ActualWidth : 900;
            double availableWidth = Math.Max(760, containerWidth);
            RoadmapCanvas.Width = availableWidth;

            double centerX = availableWidth / 2.0;
            double offsetX = centerX - 320.0;

            // 3. Adjust Canvas Height dynamically based on bottom-most node
            double maxY = nodes.Max(n => n.CanvasY) + 240;
            RoadmapCanvas.Height = Math.Max(700, maxY);

            // 4. Construct Smooth Bézier Spline Geometry
            const double nodeHalfW = 84.0;
            const double nodeHalfH = 38.0;

            var trackGeometry = new PathGeometry();
            var trackFigure = new PathFigure
            {
                StartPoint = new Point(nodes[0].CanvasX + offsetX + nodeHalfW, nodes[0].CanvasY + nodeHalfH),
                IsClosed = false
            };

            var activeGeometry = new PathGeometry();
            var activeFigure = new PathFigure
            {
                StartPoint = new Point(nodes[0].CanvasX + offsetX + nodeHalfW, nodes[0].CanvasY + nodeHalfH),
                IsClosed = false
            };

            bool stillInActivePrefix = true;

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var p0 = new Point(nodes[i].CanvasX + offsetX + nodeHalfW, nodes[i].CanvasY + nodeHalfH);
                var p1 = new Point(nodes[i + 1].CanvasX + offsetX + nodeHalfW, nodes[i + 1].CanvasY + nodeHalfH);

                double midY = (p0.Y + p1.Y) / 2.0;
                var cp1 = new Point(p0.X, midY);
                var cp2 = new Point(p1.X, midY);

                var segment = new BezierSegment
                {
                    Point1 = cp1,
                    Point2 = cp2,
                    Point3 = p1
                };
                trackFigure.Segments.Add(segment);

                if (stillInActivePrefix)
                {
                    if (nodes[i].Status == NodeStatus.Mastered ||
                        nodes[i + 1].Status == NodeStatus.Mastered ||
                        nodes[i + 1].Status == NodeStatus.InProgress)
                    {
                        activeFigure.Segments.Add(segment);
                    }
                    if (nodes[i + 1].Status == NodeStatus.Locked)
                    {
                        stillInActivePrefix = false;
                    }
                }
            }

            trackGeometry.Figures.Add(trackFigure);
            if (TrackSplinePath != null)
            {
                TrackSplinePath.Data = trackGeometry;
            }

            if (ActiveSplinePath != null)
            {
                if (activeFigure.Segments.Count > 0)
                {
                    activeGeometry.Figures.Add(activeFigure);
                    ActiveSplinePath.Data = activeGeometry;
                    ActiveSplinePath.Visibility = Visibility.Visible;
                }
                else
                {
                    ActiveSplinePath.Visibility = Visibility.Collapsed;
                }
            }

            // 5. Render Milestone Phase Dividers across the canvas
            if (ViewModel.ActiveRoadmap?.Milestones != null)
            {
                foreach (var milestone in ViewModel.ActiveRoadmap.Milestones)
                {
                    var firstNode = milestone.Nodes.OrderBy(n => n.CanvasY).FirstOrDefault();
                    if (firstNode != null)
                    {
                        var phaseBanner = CreateMilestoneBannerVisual(milestone, availableWidth);
                        Canvas.SetLeft(phaseBanner, 24);
                        Canvas.SetTop(phaseBanner, Math.Max(10, firstNode.CanvasY - 55));
                        RoadmapCanvas.Children.Add(phaseBanner);
                    }
                }
            }

            // 6. Render Node Visual Cards along the Spline
            foreach (var node in nodes)
            {
                var nodeElement = CreateNodeVisual(node);
                Canvas.SetLeft(nodeElement, node.CanvasX + offsetX);
                Canvas.SetTop(nodeElement, node.CanvasY);
                RoadmapCanvas.Children.Add(nodeElement);
                _nodeVisuals[node.Id] = (Border)nodeElement;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapPage] RenderRoadmapGraph error: {ex.Message}");
        }
    }

    private FrameworkElement CreateMilestoneBannerVisual(MilestoneDto milestone, double canvasWidth)
    {
        var container = new Grid
        {
            Width = Math.Max(400, canvasWidth - 48),
            Height = 44
        };

        var line = new Line
        {
            X1 = 20,
            Y1 = 22,
            X2 = container.Width - 20,
            Y2 = 22,
            Stroke = GetThemeBrush("CardStrokeColorDefaultBrush"),
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 4, 3 }
        };
        container.Children.Add(line);

        var badge = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(16),
            Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush"),
            BorderBrush = milestone.IsCompleted ? GetThemeBrush("SystemFillColorSuccessBrush") : GetThemeBrush("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(milestone.IsCompleted ? 1.5 : 1),
            Padding = new Thickness(16, 6, 16, 6)
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        var icon = new FontIcon
        {
            FontSize = 13,
            Glyph = milestone.IsCompleted ? "\uE73E" : "\uE8D2",
            Foreground = milestone.IsCompleted
                ? GetThemeBrush("SystemFillColorSuccessBrush")
                : GetThemeBrush("AccentTextFillColorPrimaryBrush")
        };
        stack.Children.Add(icon);

        var title = new TextBlock
        {
            Text = milestone.Title.ToUpperInvariant(),
            FontSize = 11.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = GetThemeBrush("TextFillColorPrimaryBrush")
        };
        stack.Children.Add(title);

        var countText = new TextBlock
        {
            Text = $"({milestone.CompletedNodesCount}/{milestone.TotalNodesCount})",
            FontSize = 11,
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush")
        };
        stack.Children.Add(countText);

        badge.Child = stack;
        container.Children.Add(badge);
        return container;
    }

    private FrameworkElement CreateNodeVisual(RoadmapNodeDto node)
    {
        bool isSelected = ViewModel.SelectedNode?.Id == node.Id;

        var root = new Border
        {
            Width = 168,
            Height = 76,
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(isSelected ? 3.0 : (node.Status == NodeStatus.Available ? 2.5 : 1.5)),
            Padding = new Thickness(10, 8, 10, 8),
            Tag = node
        };

        // ThemeResource based status styling
        if (isSelected)
        {
            root.Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
            root.BorderBrush = GetThemeBrush("AccentFillColorDefaultBrush");
        }
        else
        {
            switch (node.Status)
            {
                case NodeStatus.Mastered:
                    root.Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
                    root.BorderBrush = GetThemeBrush("SystemFillColorSuccessBrush");
                    break;
                case NodeStatus.InProgress:
                    root.Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
                    root.BorderBrush = GetThemeBrush("AccentFillColorDefaultBrush");
                    break;
                case NodeStatus.Available:
                    root.Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
                    root.BorderBrush = GetThemeBrush("AccentFillColorDefaultBrush");
                    break;
                case NodeStatus.Locked:
                default:
                    root.Background = GetThemeBrush("SubtleFillColorSecondaryBrush");
                    root.BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush");
                    root.Opacity = 0.60;
                    break;
            }
        }

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(34) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 10
        };

        // Icon badge
        var iconBadge = new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(16),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (node.Status == NodeStatus.Mastered)
        {
            iconBadge.Background = GetThemeBrush("SystemFillColorSuccessBrush");
        }
        else if (node.Status == NodeStatus.InProgress || node.Status == NodeStatus.Available)
        {
            iconBadge.Background = GetThemeBrush("AccentFillColorDefaultBrush");
        }
        else
        {
            iconBadge.Background = GetThemeBrush("SubtleFillColorSecondaryBrush");
        }

        var icon = new FontIcon
        {
            FontSize = 13,
            Glyph = GetGlyphForNode(node),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (node.Status == NodeStatus.Mastered || node.Status == NodeStatus.InProgress || node.Status == NodeStatus.Available)
        {
            icon.Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush");
        }
        else
        {
            icon.Foreground = GetThemeBrush("TextFillColorTertiaryBrush");
        }

        iconBadge.Child = icon;
        Grid.SetColumn(iconBadge, 0);
        grid.Children.Add(iconBadge);

        // Text & Progress stack
        var textStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2
        };

        var titleBlock = new TextBlock
        {
            Text = node.Title,
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
            Foreground = GetThemeBrush("TextFillColorPrimaryBrush")
        };
        textStack.Children.Add(titleBlock);

        var subBlock = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(node.ReferenceDisplay) ? node.Type.ToString() : node.ReferenceDisplay,
            FontSize = 9.5,
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1
        };
        textStack.Children.Add(subBlock);

        var progressBar = new ProgressBar
        {
            Maximum = 100,
            Value = node.ProgressPercentage,
            Height = 3.5,
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 3, 0, 0)
        };
        textStack.Children.Add(progressBar);

        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        root.Child = grid;

        // Interaction
        Lexora.Presentation.Helpers.CursorHelper.SetCursor(root, "Hand");
        root.PointerPressed += (s, e) =>
        {
            if (s is Border b && b.Tag is RoadmapNodeDto clickedNode)
            {
                try
                {
                    var animService = ConnectedAnimationService.GetForCurrentView();
                    animService.PrepareToAnimate("nodeTransition", b);
                }
                catch
                {
                    // Ignore connected animation preparation errors
                }

                ViewModel.SelectNode(clickedNode);

                if (InspectorTitle != null)
                {
                    try
                    {
                        var anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("nodeTransition");
                        anim?.TryStart(InspectorTitle);
                    }
                    catch
                    {
                        // Ignore connected animation start errors
                    }
                }
            }
        };

        ToolTipService.SetToolTip(root, $"{node.Title}\n{node.ReferenceDisplay} ({node.Status} - {node.ProgressPercentage:F0}%)");
        return root;
    }

    private static string GetGlyphForNode(RoadmapNodeDto node)
    {
        if (node.Status == NodeStatus.Locked) return "\uE72E"; // Lock
        if (node.Status == NodeStatus.Mastered) return "\uE73E"; // Checkmark

        return node.Type switch
        {
            RoadmapNodeType.VocabularySet => "\uE82D",
            RoadmapNodeType.GrammarPoint => "\uE8D2",
            RoadmapNodeType.MockTest => "\uE765",
            RoadmapNodeType.Checkpoint => "\uE9D2",
            _ => "\uE768"
        };
    }

    private async void OpenCreateRoadmapDialog_Click(object sender, RoutedEventArgs e)
    {
        var targetCombo = new ComboBox
        {
            ItemsSource = ViewModel.ExamTargets,
            SelectedItem = ViewModel.SelectedExamTarget,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var startScoreBox = new NumberBox
        {
            Value = ViewModel.StartingScore,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SmallChange = 10,
            Minimum = 0
        };

        var targetScoreBox = new NumberBox
        {
            Value = ViewModel.TargetScore,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SmallChange = 10,
            Minimum = 0
        };

        var durationBox = new NumberBox
        {
            Value = ViewModel.DurationWeeks,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SmallChange = 1,
            Minimum = 2,
            Maximum = 52
        };

        // Score Presets Container
        var presetsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(0, 4, 0, 0)
        };

        void PopulatePresets(ExamTarget target)
        {
            presetsPanel.Children.Clear();
            if (target == ExamTarget.Toeic)
            {
                startScoreBox.SmallChange = 10;
                startScoreBox.Maximum = 990;
                targetScoreBox.SmallChange = 10;
                targetScoreBox.Maximum = 990;

                double[] toeicScores = [550, 650, 750, 850, 900];
                foreach (var sc in toeicScores)
                {
                    var btn = new Button
                    {
                        Content = $"{sc:F0}",
                        Padding = new Thickness(8, 4, 8, 4),
                        Style = (Style)App.Current.Resources["DefaultButtonStyle"]
                    };
                    btn.Click += (s, args) =>
                    {
                        targetScoreBox.Value = sc;
                        startScoreBox.Value = Math.Max(0, sc - 300);
                    };
                    presetsPanel.Children.Add(btn);
                }
            }
            else if (target == ExamTarget.Ielts)
            {
                startScoreBox.SmallChange = 0.5;
                startScoreBox.Maximum = 9.0;
                targetScoreBox.SmallChange = 0.5;
                targetScoreBox.Maximum = 9.0;

                double[] ieltsScores = [5.5, 6.0, 6.5, 7.0, 7.5, 8.0];
                foreach (var sc in ieltsScores)
                {
                    var btn = new Button
                    {
                        Content = $"{sc:F1}",
                        Padding = new Thickness(8, 4, 8, 4),
                        Style = (Style)App.Current.Resources["DefaultButtonStyle"]
                    };
                    btn.Click += (s, args) =>
                    {
                        targetScoreBox.Value = sc;
                        startScoreBox.Value = Math.Max(0, sc - 1.5);
                    };
                    presetsPanel.Children.Add(btn);
                }
            }
            else // Vstep
            {
                startScoreBox.SmallChange = 0.5;
                startScoreBox.Maximum = 10.0;
                targetScoreBox.SmallChange = 0.5;
                targetScoreBox.Maximum = 10.0;

                (string label, double sc, double st)[] vstepScores = [("B1 (4.0)", 4.0, 2.0), ("B2 (6.0)", 6.0, 4.0), ("C1 (8.0)", 8.0, 6.0)];
                foreach (var (label, sc, st) in vstepScores)
                {
                    var btn = new Button
                    {
                        Content = label,
                        Padding = new Thickness(8, 4, 8, 4),
                        Style = (Style)App.Current.Resources["DefaultButtonStyle"]
                    };
                    btn.Click += (s, args) =>
                    {
                        targetScoreBox.Value = sc;
                        startScoreBox.Value = st;
                    };
                    presetsPanel.Children.Add(btn);
                }
            }
        }

        PopulatePresets((ExamTarget)targetCombo.SelectedItem);
        targetCombo.SelectionChanged += (s, args) =>
        {
            if (targetCombo.SelectedItem is ExamTarget newTarget)
            {
                if (newTarget == ExamTarget.Toeic)
                {
                    startScoreBox.Value = 450;
                    targetScoreBox.Value = 750;
                    durationBox.Value = 12;
                }
                else if (newTarget == ExamTarget.Ielts)
                {
                    startScoreBox.Value = 5.0;
                    targetScoreBox.Value = 6.5;
                    durationBox.Value = 16;
                }
                else
                {
                    startScoreBox.Value = 4.0;
                    targetScoreBox.Value = 6.0;
                    durationBox.Value = 10;
                }
                PopulatePresets(newTarget);
            }
        };

        var contentPanel = new StackPanel
        {
            Spacing = 14,
            MinWidth = 360,
            MaxWidth = 460,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        contentPanel.Children.Add(new TextBlock { Text = ViewModel.Loc.GetString("Roadmap_ExamTarget"), Style = (Style)App.Current.Resources["CaptionTextBlockStyle"] });
        contentPanel.Children.Add(targetCombo);

        contentPanel.Children.Add(new TextBlock { Text = ViewModel.Loc.GetString("Roadmap_QuickStart"), Style = (Style)App.Current.Resources["CaptionTextBlockStyle"] });
        contentPanel.Children.Add(presetsPanel);

        contentPanel.Children.Add(new TextBlock { Text = ViewModel.Loc.GetString("Roadmap_CurrentScore"), Style = (Style)App.Current.Resources["CaptionTextBlockStyle"] });
        contentPanel.Children.Add(startScoreBox);

        contentPanel.Children.Add(new TextBlock { Text = ViewModel.Loc.GetString("Roadmap_TargetScore"), Style = (Style)App.Current.Resources["CaptionTextBlockStyle"] });
        contentPanel.Children.Add(targetScoreBox);

        contentPanel.Children.Add(new TextBlock { Text = ViewModel.Loc.GetString("Roadmap_DurationWeeks"), Style = (Style)App.Current.Resources["CaptionTextBlockStyle"] });
        contentPanel.Children.Add(durationBox);

        var dialog = new ContentDialog
        {
            Title = ViewModel.Loc.GetString("Roadmap_CreateGoal"),
            PrimaryButtonText = ViewModel.Loc.GetString("Roadmap_GenerateButton"),
            CloseButtonText = ViewModel.Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            CornerRadius = new CornerRadius(16),
            Content = contentPanel,
            XamlRoot = this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot
        };

        var result = await dialog.ShowAsyncSafe(this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot);
        if (result == ContentDialogResult.Primary)
        {
            if (targetCombo.SelectedItem is ExamTarget target)
            {
                ViewModel.SelectedExamTarget = target;
            }
            if (!double.IsNaN(startScoreBox.Value) && startScoreBox.Value >= 0)
            {
                ViewModel.StartingScore = startScoreBox.Value;
            }
            if (!double.IsNaN(targetScoreBox.Value) && targetScoreBox.Value > 0)
            {
                ViewModel.TargetScore = targetScoreBox.Value;
            }
            if (!double.IsNaN(durationBox.Value) && durationBox.Value >= 2)
            {
                ViewModel.DurationWeeks = (int)durationBox.Value;
            }

            await Task.Yield(); // Sequential dialog safety invariant
            await ViewModel.GenerateRoadmapCommand.ExecuteAsync(null);
            
            DispatcherQueue?.TryEnqueue(() =>
            {
                Bindings.Update();
                RenderRoadmapGraph();
                UpdateTabButtonsState();
            });
        }
    }
}
