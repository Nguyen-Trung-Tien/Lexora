using Lexora.Application.DTOs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Lexora.Presentation.Controls;

/// <summary>
/// Lightweight, theme-aware Fluent 2 vector bar chart for 14-day FSRS workload simulation.
/// Uses 100% ThemeResource brushes with zero hardcoded hex colors.
/// </summary>
public sealed class VectorChartControl : Grid
{
    private readonly Grid _barsGrid;

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IReadOnlyList<DailyWorkloadPointDto>),
        typeof(VectorChartControl),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public IReadOnlyList<DailyWorkloadPointDto>? ItemsSource
    {
        get => (IReadOnlyList<DailyWorkloadPointDto>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public VectorChartControl()
    {
        _barsGrid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ColumnSpacing = 6,
            MinHeight = 110
        };
        Children.Add(_barsGrid);
        ActualThemeChanged += (_, _) => RenderBars();
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VectorChartControl ctrl)
        {
            ctrl.RenderBars();
        }
    }

    private void RenderBars()
    {
        _barsGrid.Children.Clear();
        _barsGrid.ColumnDefinitions.Clear();

        var items = ItemsSource;
        if (items == null || items.Count == 0)
        {
            return;
        }

        int maxCount = Math.Max(1, items.Max(x => x.DueCount));
        var accentBrush = GetThemeBrush("AccentFillColorDefaultBrush");
        var peakBrush = GetThemeBrush("SystemFillColorAttentionBrush") ?? accentBrush;
        var mutedBrush = GetThemeBrush("ControlFillColorSecondaryBrush");
        var secondaryTextBrush = GetThemeBrush("TextFillColorSecondaryBrush");
        var primaryTextBrush = GetThemeBrush("TextFillColorPrimaryBrush");

        for (int i = 0; i < items.Count; i++)
        {
            var point = items[i];
            _barsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var colStack = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                },
                VerticalAlignment = VerticalAlignment.Stretch
            };

            double ratio = point.DueCount <= 0 ? 0.04 : Math.Clamp((double)point.DueCount / maxCount, 0.08, 1.0);
            double barHeight = Math.Round(76.0 * ratio, 1);

            var barContainer = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Spacing = 3
            };

            if (point.DueCount > 0)
            {
                barContainer.Children.Add(new TextBlock
                {
                    Text = point.DueCount.ToString(),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = point.IsPeak ? primaryTextBrush : secondaryTextBrush
                });
            }

            var bar = new Rectangle
            {
                Height = barHeight,
                RadiusX = 4,
                RadiusY = 4,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Fill = point.DueCount == 0 ? mutedBrush : (point.IsPeak ? peakBrush : accentBrush)
            };
            barContainer.Children.Add(bar);

            Grid.SetRow(barContainer, 0);
            colStack.Children.Add(barContainer);

            var labelText = new TextBlock
            {
                Text = i == 0 || i == items.Count - 1 || i % 3 == 0 ? point.Label : string.Empty,
                FontSize = 10,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = secondaryTextBrush
            };
            Grid.SetRow(labelText, 1);
            colStack.Children.Add(labelText);

            ToolTipService.SetToolTip(colStack, $"{point.Label}: {point.DueCount} cards");
            Grid.SetColumn(colStack, i);
            _barsGrid.Children.Add(colStack);
        }
    }

    private Brush? GetThemeBrush(string key)
    {
        if (Microsoft.UI.Xaml.Application.Current?.Resources.TryGetValue(key, out var val) == true && val is Brush b)
        {
            return b;
        }
        return null;
    }
}
