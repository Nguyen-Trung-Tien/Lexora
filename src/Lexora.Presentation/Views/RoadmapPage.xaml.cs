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

    public RoadmapPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Current.Services.GetRequiredService<RoadmapViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();
        DataContext = ViewModel;

        InitializeComponent();

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
                });
            }
        };
        ViewModel.PropertyChanged += _propChangedHandler;

        Loaded += Page_Loaded;
        SizeChanged += Page_SizeChanged;
        ActualThemeChanged += (s, e) => OnThemeChanged();
    }

    public void OnLanguageChanged()
    {
        Bindings.Update();
        RenderRoadmapGraph();
    }

    public void OnThemeChanged()
    {
        RenderRoadmapGraph();
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
                // Instant render from cached in-memory state
                RenderRoadmapGraph();
                // Silent background sync so user never experiences UI lag or blank screens
                _ = ViewModel.SyncProgressAsync();
            }

            Bindings.Update();
            RenderRoadmapGraph();
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

    private Brush GetThemeBrush(string key)
    {
        return ThemeResourceHelper.GetThemeBrush(this, key)
            ?? new SolidColorBrush(Colors.Transparent);
    }

    private void RenderRoadmapGraph()
    {
        try
        {
            if (RoadmapCanvas == null || ViewModel?.VisualNodes == null)
            {
                return;
            }

            var nodes = ViewModel.VisualNodes.OrderBy(n => n.OrderIndex).ToList();
            if (nodes.Count == 0)
            {
                return;
            }

            // 1. Remove previously dynamically added node controls (keep the 2 track paths)
            var childrenToRemove = RoadmapCanvas.Children
                .Where(c => c != TrackSplinePath && c != ActiveSplinePath)
                .ToList();

            foreach (var child in childrenToRemove)
            {
                RoadmapCanvas.Children.Remove(child);
            }

            // 2. Adjust Canvas Height dynamically based on bottom-most node
            double maxY = nodes.Max(n => n.CanvasY) + 200;
            RoadmapCanvas.Height = Math.Max(700, maxY);

            // 3. Construct Smooth Bézier Spline Geometry
            var trackGeometry = new PathGeometry();
            var trackFigure = new PathFigure
            {
                StartPoint = new Point(nodes[0].CanvasX + 75, nodes[0].CanvasY + 34),
                IsClosed = false
            };

            var activeGeometry = new PathGeometry();
            var activeFigure = new PathFigure
            {
                StartPoint = new Point(nodes[0].CanvasX + 75, nodes[0].CanvasY + 34),
                IsClosed = false
            };

            bool stillInActivePrefix = true;

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var p0 = new Point(nodes[i].CanvasX + 75, nodes[i].CanvasY + 34);
                var p1 = new Point(nodes[i + 1].CanvasX + 75, nodes[i + 1].CanvasY + 34);

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
                    if (nodes[i + 1].Status == NodeStatus.Mastered || nodes[i + 1].Status == NodeStatus.InProgress)
                    {
                        activeFigure.Segments.Add(segment);
                    }
                    else
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

            // 4. Render Node Visual Cards along the Spline
            foreach (var node in nodes)
            {
                var nodeElement = CreateNodeVisual(node);
                Canvas.SetLeft(nodeElement, node.CanvasX);
                Canvas.SetTop(nodeElement, node.CanvasY);
                RoadmapCanvas.Children.Add(nodeElement);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapPage] RenderRoadmapGraph error: {ex.Message}");
        }
    }

    private FrameworkElement CreateNodeVisual(RoadmapNodeDto node)
    {
        var root = new Border
        {
            Width = 150,
            Height = 68,
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(node.Status == NodeStatus.Available ? 2 : 1),
            Padding = new Thickness(8, 6, 8, 6),
            Tag = node
        };

        // ThemeResource based status styling
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
                root.BorderBrush = GetThemeBrush("AccentTextFillColorPrimaryBrush");
                break;
            case NodeStatus.Locked:
            default:
                root.Background = GetThemeBrush("SubtleFillColorSecondaryBrush");
                root.BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush");
                root.Opacity = 0.65;
                break;
        }

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(32) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 8
        };

        // Icon badge
        var iconBadge = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(15),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (node.Status == NodeStatus.Mastered)
        {
            iconBadge.Background = GetThemeBrush("SystemFillColorSuccessBrush");
        }
        else if (node.Status == NodeStatus.InProgress)
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

        if (node.Status == NodeStatus.Mastered || node.Status == NodeStatus.InProgress)
        {
            icon.Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush");
        }
        else if (node.Status == NodeStatus.Available)
        {
            icon.Foreground = GetThemeBrush("AccentTextFillColorPrimaryBrush");
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
            Text = node.ReferenceDisplay,
            FontSize = 9,
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1
        };
        textStack.Children.Add(subBlock);

        var progressBar = new ProgressBar
        {
            Maximum = 100,
            Value = node.ProgressPercentage,
            Height = 3,
            Margin = new Thickness(0, 2, 0, 0)
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
                    // Ignore connected animation preparation errors on rapid clicks
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
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var targetScoreBox = new NumberBox
        {
            Value = ViewModel.TargetScore,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var durationBox = new NumberBox
        {
            Value = ViewModel.DurationWeeks,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var contentPanel = new StackPanel
        {
            Spacing = 16,
            MinWidth = 320,
            MaxWidth = 440,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        contentPanel.Children.Add(new TextBlock { Text = ViewModel.Loc.GetString("Roadmap_ExamTarget"), Style = (Style)App.Current.Resources["CaptionTextBlockStyle"] });
        contentPanel.Children.Add(targetCombo);

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
                RenderRoadmapGraph();
            });
        }
    }
}
