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
                DispatcherQueue?.TryEnqueue(RenderRoadmapGraph);
            }
        };
        ViewModel.PropertyChanged += _propChangedHandler;

        Loaded += Page_Loaded;
        Unloaded += Page_Unloaded;
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= _propChangedHandler;
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
            await ViewModel.InitializeAsync();
            RenderRoadmapGraph();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapPage] OnNavigatedTo error: {ex.Message}");
        }
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.InitializeAsync();
            RenderRoadmapGraph();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapPage] Page_Loaded error: {ex.Message}");
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
            if (RoadmapCanvas == null || ViewModel?.VisualNodes == null || ViewModel.VisualNodes.Count == 0)
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

            var nodes = ViewModel.VisualNodes.OrderBy(n => n.OrderIndex).ToList();
            if (nodes.Count == 0) return;

            // 2. Adjust Canvas Height dynamically based on bottom-most node
            double maxY = nodes.Max(n => n.CanvasY) + 200;
            RoadmapCanvas.Height = Math.Max(700, maxY);

            // 3. Construct Smooth Bézier Spline Geometry
            var trackGeometry = new PathGeometry();
            var trackFigure = new PathFigure
            {
                StartPoint = new Point(nodes[0].CanvasX + 36, nodes[0].CanvasY + 36),
                IsClosed = false
            };

            var activeGeometry = new PathGeometry();
            var activeFigure = new PathFigure
            {
                StartPoint = new Point(nodes[0].CanvasX + 36, nodes[0].CanvasY + 36),
                IsClosed = false
            };

            bool stillInActivePrefix = true;

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var p0 = new Point(nodes[i].CanvasX + 36, nodes[i].CanvasY + 36);
                var p1 = new Point(nodes[i + 1].CanvasX + 36, nodes[i + 1].CanvasY + 36);

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
            Width = 72,
            Height = 72,
            CornerRadius = new CornerRadius(36),
            BorderThickness = new Thickness(node.Status == NodeStatus.Available ? 3 : 2),
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
                root.Background = GetThemeBrush("AccentFillColorDefaultBrush");
                root.BorderBrush = GetThemeBrush("AccentTextFillColorPrimaryBrush");
                break;
            case NodeStatus.Available:
                root.Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
                root.BorderBrush = GetThemeBrush("AccentFillColorDefaultBrush");
                break;
            case NodeStatus.Locked:
            default:
                root.Background = GetThemeBrush("SubtleFillColorSecondaryBrush");
                root.BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush");
                root.Opacity = 0.65;
                break;
        }

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2
        };

        var icon = new FontIcon
        {
            FontSize = 20,
            Glyph = GetGlyphForNode(node)
        };

        if (node.Status == NodeStatus.InProgress)
        {
            icon.Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush");
        }
        else if (node.Status == NodeStatus.Mastered)
        {
            icon.Foreground = GetThemeBrush("SystemFillColorSuccessBrush");
        }
        else if (node.Status == NodeStatus.Available)
        {
            icon.Foreground = GetThemeBrush("AccentTextFillColorPrimaryBrush");
        }
        else
        {
            icon.Foreground = GetThemeBrush("TextFillColorTertiaryBrush");
        }

        stack.Children.Add(icon);

        var titleBlock = new TextBlock
        {
            Text = $"{node.ProgressPercentage:F0}%",
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (node.Status == NodeStatus.InProgress)
        {
            titleBlock.Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush");
        }
        else
        {
            titleBlock.Foreground = GetThemeBrush("TextFillColorSecondaryBrush");
        }

        stack.Children.Add(titleBlock);
        root.Child = stack;

        // Interaction
        Lexora.Presentation.Helpers.CursorHelper.SetCursor(root, "Hand");
        root.PointerPressed += (s, e) =>
        {
            if (s is Border b && b.Tag is RoadmapNodeDto clickedNode)
            {
                ViewModel.SelectNode(clickedNode);
            }
        };

        ToolTipService.SetToolTip(root, $"{node.Title} ({node.Status})");
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
            Width = 360
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
            ViewModel.StartingScore = startScoreBox.Value;
            ViewModel.TargetScore = targetScoreBox.Value;
            ViewModel.DurationWeeks = (int)Math.Max(2, durationBox.Value);

            await Task.Yield(); // Sequential dialog safety invariant
            await ViewModel.GenerateRoadmapCommand.ExecuteAsync(null);
            RenderRoadmapGraph();
        }
    }
}
