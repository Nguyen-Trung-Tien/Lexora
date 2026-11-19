using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Lexora.Presentation.Views.Roadmap;

public sealed partial class RoadmapPage
{
    private void RenderSerpentineCanvas()
    {
        if (RoadmapCanvas == null || TrackSplinePath == null || ActiveSplinePath == null) return;

        var existingDynamicNodes = RoadmapCanvas.Children
            .Where(c => c != TrackSplinePath && c != ActiveSplinePath)
            .ToList();
        foreach (var child in existingDynamicNodes)
        {
            RoadmapCanvas.Children.Remove(child);
        }

        var nodes = ViewModel.Milestones.SelectMany(m => m.Nodes).ToList();
        if (ViewModel.ActiveRoadmap == null || nodes.Count == 0)
        {
            TrackSplinePath.Data = null;
            ActiveSplinePath.Data = null;
            return;
        }

        const double nodeWidth = 228.0;
        const double nodeHeight = 76.0;
        const double halfWidth = nodeWidth / 2.0;
        const double halfHeight = nodeHeight / 2.0;

        double maxY = nodes.Max(n => n.CanvasY) + nodeHeight + 160.0;
        double maxX = nodes.Max(n => n.CanvasX) + nodeWidth + 80.0;
        RoadmapCanvas.Width = Math.Max(780.0, maxX);
        RoadmapCanvas.Height = Math.Max(440.0, maxY);

        var trackGeometry = new PathGeometry();
        var activeGeometry = new PathGeometry();

        if (nodes.Count > 1)
        {
            var firstCenter = new Windows.Foundation.Point(nodes[0].CanvasX + halfWidth, nodes[0].CanvasY + halfHeight);
            var trackFigure = new PathFigure { StartPoint = firstCenter, IsClosed = false };
            var activeFigure = new PathFigure { StartPoint = firstCenter, IsClosed = false };
            bool hasActiveSegments = false;

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var nodeA = nodes[i];
                var nodeB = nodes[i + 1];

                var p0 = new Windows.Foundation.Point(nodeA.CanvasX + halfWidth, nodeA.CanvasY + halfHeight);
                var p1 = new Windows.Foundation.Point(nodeB.CanvasX + halfWidth, nodeB.CanvasY + halfHeight);
                double midY = (p0.Y + p1.Y) / 2.0;
                var cp1 = new Windows.Foundation.Point(p0.X, midY);
                var cp2 = new Windows.Foundation.Point(p1.X, midY);

                trackFigure.Segments.Add(new BezierSegment
                {
                    Point1 = cp1,
                    Point2 = cp2,
                    Point3 = p1
                });

                if (nodeA.Status == NodeStatus.Mastered || nodeB.Status is NodeStatus.Mastered or NodeStatus.InProgress or NodeStatus.Available)
                {
                    activeFigure.Segments.Add(new BezierSegment
                    {
                        Point1 = cp1,
                        Point2 = cp2,
                        Point3 = p1
                    });
                    hasActiveSegments = true;
                }
            }

            trackGeometry.Figures.Add(trackFigure);
            if (hasActiveSegments)
            {
                activeGeometry.Figures.Add(activeFigure);
            }
        }

        TrackSplinePath.Data = trackGeometry;
        ActiveSplinePath.Data = activeGeometry;

        // Render milestone phase header pills above each milestone's first node
        foreach (var milestone in ViewModel.Milestones.OrderBy(m => m.OrderIndex))
        {
            var firstNode = milestone.Nodes.OrderBy(n => n.OrderIndex).FirstOrDefault();
            if (firstNode == null) continue;

            var phasePill = new Border
            {
                Padding = new Thickness(12, 5, 12, 5),
                CornerRadius = new CornerRadius(14),
                Background = GetThemeBrush(milestone.IsCompleted ? "SystemFillColorSuccessBrush" : "SubtleFillColorSecondaryBrush"),
                BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1)
            };

            var pillContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            pillContent.Children.Add(new TextBlock
            {
                Text = $"{Loc.GetString("Roadmap_Phase")} {milestone.OrderIndex + 1}",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = GetThemeBrush(milestone.IsCompleted ? "TextOnAccentFillColorPrimaryBrush" : "AccentTextFillColorPrimaryBrush")
            });
            pillContent.Children.Add(new TextBlock
            {
                Text = milestone.Title,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = GetThemeBrush(milestone.IsCompleted ? "TextOnAccentFillColorPrimaryBrush" : "TextFillColorPrimaryBrush"),
                MaxWidth = 360,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            pillContent.Children.Add(new TextBlock
            {
                Text = $"({milestone.CompletedNodesCount}/{milestone.TotalNodesCount})",
                FontSize = 11,
                Foreground = GetThemeBrush(milestone.IsCompleted ? "TextOnAccentFillColorPrimaryBrush" : "TextFillColorSecondaryBrush")
            });
            phasePill.Child = pillContent;

            Canvas.SetLeft(phasePill, 20);
            Canvas.SetTop(phasePill, Math.Max(8, firstNode.CanvasY - 40));
            RoadmapCanvas.Children.Add(phasePill);
        }

        foreach (var node in nodes)
        {
            bool isSelected = ViewModel.SelectedNode?.Id == node.Id;
            bool isLocked = node.Status == NodeStatus.Locked;
            string localizedType = Loc.GetString(node.TypeLocalizationKey);

            var markerBtn = new Button
            {
                Width = nodeWidth,
                Height = nodeHeight,
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new CornerRadius(12),
                Opacity = isLocked ? 0.65 : 1.0,
                Tag = node,
                Background = isSelected
                    ? GetThemeBrush("SubtleFillColorSecondaryBrush")
                    : GetThemeBrush("CardBackgroundFillColorDefaultBrush"),
                BorderBrush = isSelected || node.Status is NodeStatus.Available or NodeStatus.InProgress
                    ? GetThemeBrush("AccentFillColorDefaultBrush")
                    : (node.Status == NodeStatus.Mastered
                        ? GetThemeBrush("SystemFillColorSuccessBrush")
                        : GetThemeBrush("CardStrokeColorDefaultBrush")),
                BorderThickness = new Thickness(isSelected ? 2.25 : 1.25)
            };

            var contentGrid = new Grid { ColumnSpacing = 10 };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var badge = new Border
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(15),
                VerticalAlignment = VerticalAlignment.Center,
                Background = GetThemeBrush("SubtleFillColorSecondaryBrush"),
                Child = new FontIcon
                {
                    FontSize = 13,
                    Glyph = node.StatusGlyph,
                    Foreground = node.Status == NodeStatus.Mastered
                        ? GetThemeBrush("SystemFillColorSuccessBrush")
                        : (isLocked
                            ? GetThemeBrush("TextFillColorTertiaryBrush")
                            : GetThemeBrush("AccentTextFillColorPrimaryBrush"))
                }
            };
            Grid.SetColumn(badge, 0);
            contentGrid.Children.Add(badge);

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
            textStack.Children.Add(new TextBlock
            {
                Text = node.Title,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2
            });
            textStack.Children.Add(new TextBlock
            {
                Text = $"{localizedType} • {node.ProgressPercentageDisplay}",
                FontSize = 10,
                Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 1
            });
            Grid.SetColumn(textStack, 1);
            contentGrid.Children.Add(textStack);

            markerBtn.Content = contentGrid;
            ToolTipService.SetToolTip(markerBtn, $"{node.Title}\n{node.Description}\n{localizedType} • {node.ProgressPercentageDisplay}");
            markerBtn.Click += NodeCard_Click;

            Canvas.SetLeft(markerBtn, node.CanvasX);
            Canvas.SetTop(markerBtn, node.CanvasY);
            RoadmapCanvas.Children.Add(markerBtn);
        }
    }
}
