using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
                || e.PropertyName == nameof(ViewModel.SelectedViewMode))
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    Bindings.Update();
                    UpdateTabButtonsState();
                    RenderMilestoneTrack();
                    RenderTimelineView();
                });
            }
            else if (e.PropertyName == nameof(ViewModel.SelectedNode))
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    Bindings.Update();
                    RenderMilestoneTrack();
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
        UpdateTabButtonsState();
    }

    public void OnThemeChanged()
    {
        UpdateTabButtonsState();
        RenderMilestoneTrack();
        RenderTimelineView();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        try
        {
            ViewModel.PropertyChanged -= _propChangedHandler;
            ViewModel.PropertyChanged += _propChangedHandler;

            OnLanguageChanged();
            OnThemeChanged();

            if (!ViewModel.HasActiveRoadmap)
            {
                await ViewModel.InitializeAsync();
            }
            else
            {
                await ViewModel.SyncProgressAsync();
            }

            Bindings.Update();
            UpdateTabButtonsState();
            RenderMilestoneTrack();
            RenderTimelineView();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapPage] OnNavigatedTo error: {ex.Message}");
            App.LogCrash("RoadmapPage.OnNavigatedTo", ex);
        }
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.PropertyChanged -= _propChangedHandler;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateResponsiveLayout(ActualWidth);
            Bindings.Update();
            UpdateTabButtonsState();
            RenderMilestoneTrack();
            RenderTimelineView();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapPage] Page_Loaded error: {ex.Message}");
            App.LogCrash("RoadmapPage.Page_Loaded", ex);
        }
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateResponsiveLayout(e.NewSize.Width);
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0) return;

        // 1. Header Responsive Layout
        if (PageHeaderGrid != null && HeaderTitleArea != null && HeaderActionsScroller != null)
        {
            bool isHeaderCompact = width < 1180;
            if (isHeaderCompact)
            {
                PageHeaderGrid.ColumnDefinitions[1].Width = new GridLength(0);
                Grid.SetRow(HeaderTitleArea, 0);
                Grid.SetColumn(HeaderTitleArea, 0);
                Grid.SetColumnSpan(HeaderTitleArea, 2);

                Grid.SetRow(HeaderActionsScroller, 1);
                Grid.SetColumn(HeaderActionsScroller, 0);
                Grid.SetColumnSpan(HeaderActionsScroller, 2);
                HeaderActionsScroller.HorizontalAlignment = HorizontalAlignment.Left;
                HeaderActionsScroller.Margin = new Thickness(0, 12, 0, 0);
            }
            else
            {
                PageHeaderGrid.ColumnDefinitions[1].Width = GridLength.Auto;
                Grid.SetRow(HeaderTitleArea, 0);
                Grid.SetColumn(HeaderTitleArea, 0);
                Grid.SetColumnSpan(HeaderTitleArea, 1);

                Grid.SetRow(HeaderActionsScroller, 0);
                Grid.SetColumn(HeaderActionsScroller, 1);
                Grid.SetColumnSpan(HeaderActionsScroller, 1);
                HeaderActionsScroller.HorizontalAlignment = HorizontalAlignment.Right;
                HeaderActionsScroller.Margin = new Thickness(0);
            }
        }

        // 2. Manage Roadmaps Header Responsive Layout
        if (ManageHeaderGrid != null && ManageTitleArea != null && ManageActionsScroller != null)
        {
            bool isManageCompact = width < 1180;
            if (isManageCompact)
            {
                ManageHeaderGrid.ColumnDefinitions[1].Width = new GridLength(0);
                Grid.SetRow(ManageTitleArea, 0);
                Grid.SetColumn(ManageTitleArea, 0);
                Grid.SetColumnSpan(ManageTitleArea, 2);

                Grid.SetRow(ManageActionsScroller, 1);
                Grid.SetColumn(ManageActionsScroller, 0);
                Grid.SetColumnSpan(ManageActionsScroller, 2);
                ManageActionsScroller.HorizontalAlignment = HorizontalAlignment.Left;
                ManageActionsScroller.Margin = new Thickness(0, 10, 0, 0);
            }
            else
            {
                ManageHeaderGrid.ColumnDefinitions[1].Width = GridLength.Auto;
                Grid.SetRow(ManageTitleArea, 0);
                Grid.SetColumn(ManageTitleArea, 0);
                Grid.SetColumnSpan(ManageTitleArea, 1);

                Grid.SetRow(ManageActionsScroller, 0);
                Grid.SetColumn(ManageActionsScroller, 1);
                Grid.SetColumnSpan(ManageActionsScroller, 1);
                ManageActionsScroller.HorizontalAlignment = HorizontalAlignment.Right;
                ManageActionsScroller.Margin = new Thickness(0);
            }
        }

        // 3. Empty State Presets Responsive Layout
        if (PresetsGrid != null && PresetCard1 != null && PresetCard2 != null && PresetCard3 != null)
        {
            if (width < 960)
            {
                // 1-Column Layout
                PresetsGrid.ColumnDefinitions[1].Width = new GridLength(0);
                PresetsGrid.ColumnDefinitions[2].Width = new GridLength(0);

                Grid.SetRow(PresetCard1, 0);
                Grid.SetColumn(PresetCard1, 0);
                Grid.SetColumnSpan(PresetCard1, 1);

                Grid.SetRow(PresetCard2, 1);
                Grid.SetColumn(PresetCard2, 0);
                Grid.SetColumnSpan(PresetCard2, 1);

                Grid.SetRow(PresetCard3, 2);
                Grid.SetColumn(PresetCard3, 0);
                Grid.SetColumnSpan(PresetCard3, 1);
            }
            else if (width < 1200)
            {
                // 2-Column Layout
                PresetsGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                PresetsGrid.ColumnDefinitions[2].Width = new GridLength(0);

                Grid.SetRow(PresetCard1, 0);
                Grid.SetColumn(PresetCard1, 0);
                Grid.SetColumnSpan(PresetCard1, 1);

                Grid.SetRow(PresetCard2, 0);
                Grid.SetColumn(PresetCard2, 1);
                Grid.SetColumnSpan(PresetCard2, 1);

                Grid.SetRow(PresetCard3, 1);
                Grid.SetColumn(PresetCard3, 0);
                Grid.SetColumnSpan(PresetCard3, 2);
            }
            else
            {
                // 3-Column Layout
                PresetsGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                PresetsGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);

                Grid.SetRow(PresetCard1, 0);
                Grid.SetColumn(PresetCard1, 0);
                Grid.SetColumnSpan(PresetCard1, 1);

                Grid.SetRow(PresetCard2, 0);
                Grid.SetColumn(PresetCard2, 1);
                Grid.SetColumnSpan(PresetCard2, 1);

                Grid.SetRow(PresetCard3, 0);
                Grid.SetColumn(PresetCard3, 2);
                Grid.SetColumnSpan(PresetCard3, 1);
            }
        }

        // 4. Overview Cards: 4-columns on wide screens, 2x2 grid on compact screens
        if (OverviewCardsGrid != null && OverviewCard1 != null && OverviewCard2 != null && OverviewCard3 != null && OverviewCard4 != null)
        {
            bool isCompact = width < 1050;
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
        }

        // 5. Track List + Inspector Workspace: Side-by-side on wide screens, stacked vertically on compact screens
        if (RoadmapWorkspaceGrid != null && InspectorHostBorder != null)
        {
            bool isCompact = width < 1050;
            var leftContainer = (FrameworkElement?)TrackWorkspaceLeftPanel ?? TrackListContainer;
            if (isCompact)
            {
                RoadmapWorkspaceGrid.ColumnDefinitions[1].Width = new GridLength(0);
                if (leftContainer != null)
                {
                    Grid.SetRow(leftContainer, 0);
                    Grid.SetColumn(leftContainer, 0);
                    leftContainer.Margin = new Thickness(0);
                }

                Grid.SetRow(InspectorHostBorder, 1);
                Grid.SetColumn(InspectorHostBorder, 0);
                InspectorHostBorder.Margin = new Thickness(0, 16, 0, 0);
            }
            else
            {
                RoadmapWorkspaceGrid.ColumnDefinitions[1].Width = new GridLength(380);
                if (leftContainer != null)
                {
                    Grid.SetRow(leftContainer, 0);
                    Grid.SetColumn(leftContainer, 0);
                    leftContainer.Margin = new Thickness(0, 0, 16, 0);
                }

                Grid.SetRow(InspectorHostBorder, 0);
                Grid.SetColumn(InspectorHostBorder, 1);
                InspectorHostBorder.Margin = new Thickness(0);
            }
        }
    }

    private void UpdateTabButtonsState()
    {
        if (TrackViewTabButton == null || TimelineViewTabButton == null || ManageViewTabButton == null) return;

        var accentBg = GetThemeBrush("AccentFillColorDefaultBrush");
        var accentFg = GetThemeBrush("TextOnAccentFillColorPrimaryBrush");
        var transparent = new SolidColorBrush(Colors.Transparent);
        var normalFg = GetThemeBrush("TextFillColorPrimaryBrush");

        // Tab 1: Track View
        TrackViewTabButton.Background = ViewModel.IsTrackView ? accentBg : transparent;
        TrackViewTabButton.Foreground = ViewModel.IsTrackView ? accentFg : normalFg;

        // Tab 2: Timeline View
        TimelineViewTabButton.Background = ViewModel.IsTimelineView ? accentBg : transparent;
        TimelineViewTabButton.Foreground = ViewModel.IsTimelineView ? accentFg : normalFg;

        // Tab 3: Manage Goals View
        ManageViewTabButton.Background = ViewModel.IsManageView ? accentBg : transparent;
        ManageViewTabButton.Foreground = ViewModel.IsManageView ? accentFg : normalFg;
    }

    private Brush GetThemeBrush(string key)
    {
        return ThemeResourceHelper.GetThemeBrush(this, key)
            ?? new SolidColorBrush(Colors.Transparent);
    }

    private void SwitchToTrackView_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetViewMode(0);
        UpdateTabButtonsState();
        Bindings.Update();
    }

    private void SwitchToTimelineView_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetViewMode(1);
        UpdateTabButtonsState();
        Bindings.Update();
    }

    private void SwitchToManageView_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetViewMode(2);
        UpdateTabButtonsState();
        Bindings.Update();
    }

    private void RenderMilestoneTrack()
    {
        RenderSerpentineCanvas();

        if (TrackListContainer == null) return;
        TrackListContainer.Children.Clear();

        if (ViewModel.ActiveRoadmap == null || ViewModel.Milestones.Count == 0) return;

        foreach (var milestone in ViewModel.Milestones)
        {
            var milestoneCard = new Border
            {
                Margin = new Thickness(0, 0, 0, 16),
                Padding = new Thickness(20),
                Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush"),
                BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12)
            };

            var cardStack = new StackPanel { Spacing = 14 };

            // Milestone Header Grid
            var headerGrid = new Grid { ColumnSpacing = 12 };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Spacing = 4 };
            var phaseRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

            var phaseBorder = new Border
            {
                Padding = new Thickness(6, 2, 6, 2),
                CornerRadius = new CornerRadius(4),
                Background = GetThemeBrush("AccentFillColorDefaultBrush")
            };
            phaseBorder.Child = new TextBlock
            {
                Text = milestone.PhaseDisplay,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush")
            };
            phaseRow.Children.Add(phaseBorder);

            phaseRow.Children.Add(new TextBlock
            {
                Text = milestone.Title,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Style = (Style)App.Current.Resources["SubtitleTextBlockStyle"]
            });
            titleStack.Children.Add(phaseRow);

            if (!string.IsNullOrWhiteSpace(milestone.Description))
            {
                titleStack.Children.Add(new TextBlock
                {
                    Text = milestone.Description,
                    Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
                    Style = (Style)App.Current.Resources["CaptionTextBlockStyle"],
                    TextWrapping = TextWrapping.Wrap
                });
            }
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            // Right header stats
            var statsStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Spacing = 2 };
            statsStack.Children.Add(new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 13,
                Text = milestone.NodesSummaryDisplay
            });
            if (!string.IsNullOrWhiteSpace(milestone.TargetDateDisplay))
            {
                statsStack.Children.Add(new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    FontSize = 11,
                    Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
                    Text = milestone.TargetDateDisplay
                });
            }
            Grid.SetColumn(statsStack, 1);
            headerGrid.Children.Add(statsStack);

            cardStack.Children.Add(headerGrid);

            // Progress bar
            cardStack.Children.Add(new ProgressBar
            {
                Height = 4,
                Maximum = 100,
                Value = milestone.ProgressPercentage
            });

            // Nodes Container
            var nodesPanel = new StackPanel { Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
            foreach (var node in milestone.Nodes)
            {
                var isSelected = ViewModel.SelectedNode?.Id == node.Id;
                var nodeBtn = CreateNodeButton(node, isSelected);
                nodesPanel.Children.Add(nodeBtn);
            }
            cardStack.Children.Add(nodesPanel);

            milestoneCard.Child = cardStack;
            TrackListContainer.Children.Add(milestoneCard);
        }
    }

    private Button CreateNodeButton(RoadmapNodeDto node, bool isSelected)
    {
        var btn = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 3, 0, 3),
            Padding = new Thickness(14, 12, 14, 12),
            CornerRadius = new CornerRadius(8),
            Tag = node,
            Style = (Style)App.Current.Resources["SubtleButtonStyle"]
        };

        if (isSelected)
        {
            btn.Background = GetThemeBrush("SubtleFillColorSecondaryBrush");
            btn.BorderBrush = GetThemeBrush("AccentFillColorDefaultBrush");
            btn.BorderThickness = new Thickness(1.5);
        }

        btn.Click += NodeCard_Click;

        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Status badge icon
        var iconBorder = new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(16),
            VerticalAlignment = VerticalAlignment.Center,
            Background = GetThemeBrush("SubtleFillColorSecondaryBrush")
        };
        iconBorder.Child = new FontIcon
        {
            FontSize = 13,
            Foreground = node.Status == NodeStatus.Mastered 
                ? GetThemeBrush("SystemFillColorSuccessBrush") 
                : GetThemeBrush("AccentTextFillColorPrimaryBrush"),
            Glyph = node.StatusGlyph
        };
        Grid.SetColumn(iconBorder, 0);
        grid.Children.Add(iconBorder);

        // Center: Title + Type badge + Reference + Progress bar
        var centerStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 3 };
        var topRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        topRow.Children.Add(new TextBlock
        {
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Text = node.Title,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var typeBadge = new Border
        {
            Padding = new Thickness(6, 1, 6, 1),
            CornerRadius = new CornerRadius(4),
            Background = GetThemeBrush("SubtleFillColorSecondaryBrush")
        };
        typeBadge.Child = new TextBlock
        {
            FontSize = 10,
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
            Text = node.TypeDisplay
        };
        topRow.Children.Add(typeBadge);
        centerStack.Children.Add(topRow);

        centerStack.Children.Add(new TextBlock
        {
            FontSize = 11,
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
            Text = node.ReferenceDisplay,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        centerStack.Children.Add(new ProgressBar
        {
            Height = 3,
            Maximum = 100,
            Value = node.ProgressPercentage
        });

        Grid.SetColumn(centerStack, 1);
        grid.Children.Add(centerStack);

        // Right: Progress % + Count
        var rightStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
        rightStack.Children.Add(new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 12,
            Text = node.ProgressPercentageDisplay
        });
        rightStack.Children.Add(new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            FontSize = 11,
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
            Text = node.ItemCountDisplay
        });
        Grid.SetColumn(rightStack, 2);
        grid.Children.Add(rightStack);

        btn.Content = grid;
        return btn;
    }

    private void RenderTimelineView()
    {
        if (TimelineListContainer == null) return;
        TimelineListContainer.Children.Clear();

        if (ViewModel.ActiveRoadmap == null || ViewModel.Milestones.Count == 0) return;

        foreach (var milestone in ViewModel.Milestones)
        {
            var border = new Border
            {
                Margin = new Thickness(0, 0, 0, 16),
                Padding = new Thickness(18),
                Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush"),
                BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12)
            };

            var stack = new StackPanel { Spacing = 12 };

            var header = new Grid { ColumnSpacing = 8 };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var phasePill = new Border
            {
                Padding = new Thickness(6, 2, 6, 2),
                CornerRadius = new CornerRadius(4),
                Background = GetThemeBrush("AccentFillColorDefaultBrush")
            };
            phasePill.Child = new TextBlock
            {
                Text = milestone.PhaseDisplay,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush")
            };
            titleRow.Children.Add(phasePill);

            titleRow.Children.Add(new TextBlock
            {
                Text = milestone.Title,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Style = (Style)App.Current.Resources["BodyStrongTextBlockStyle"]
            });
            Grid.SetColumn(titleRow, 0);
            header.Children.Add(titleRow);

            if (!string.IsNullOrWhiteSpace(milestone.TargetDateDisplay))
            {
                var dateText = new TextBlock
                {
                    Text = milestone.TargetDateDisplay,
                    FontSize = 11,
                    Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(dateText, 1);
                header.Children.Add(dateText);
            }
            stack.Children.Add(header);

            // Nodes timeline items
            foreach (var node in milestone.Nodes)
            {
                var rowBorder = new Border
                {
                    Margin = new Thickness(0, 3, 0, 3),
                    Padding = new Thickness(12, 10, 12, 10),
                    Background = GetThemeBrush("SubtleFillColorSecondaryBrush"),
                    BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8)
                };

                var rowGrid = new Grid { ColumnSpacing = 12 };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = new FontIcon
                {
                    FontSize = 14,
                    Foreground = node.Status == NodeStatus.Mastered 
                        ? GetThemeBrush("SystemFillColorSuccessBrush") 
                        : GetThemeBrush("AccentTextFillColorPrimaryBrush"),
                    Glyph = node.StatusGlyph
                };
                Grid.SetColumn(icon, 0);
                rowGrid.Children.Add(icon);

                var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
                titleStack.Children.Add(new TextBlock { Text = node.Title, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                titleStack.Children.Add(new TextBlock
                {
                    Text = node.ReferenceDisplay,
                    FontSize = 11,
                    Foreground = GetThemeBrush("TextFillColorSecondaryBrush")
                });
                Grid.SetColumn(titleStack, 1);
                rowGrid.Children.Add(titleStack);

                var progress = new ProgressBar
                {
                    Width = 80,
                    Maximum = 100,
                    Value = node.ProgressPercentage,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(progress, 2);
                rowGrid.Children.Add(progress);

                var selectBtn = new Button
                {
                    Padding = new Thickness(8, 6, 8, 6),
                    CornerRadius = new CornerRadius(8),
                    Tag = node,
                    Style = (Style)App.Current.Resources["SubtleButtonStyle"]
                };
                ToolTipService.SetToolTip(selectBtn, Loc.GetString("Roadmap_ActionLearn"));
                selectBtn.Content = new FontIcon { Glyph = "\uE76C", FontSize = 12 };
                selectBtn.Click += TimelineNodeSelect_Click;
                Grid.SetColumn(selectBtn, 3);
                rowGrid.Children.Add(selectBtn);

                rowBorder.Child = rowGrid;
                stack.Children.Add(rowBorder);
            }

            border.Child = stack;
            TimelineListContainer.Children.Add(border);
        }
    }

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

        const double nodeWidth = 220.0;
        const double nodeHeight = 72.0;
        const double halfWidth = nodeWidth / 2.0;
        const double halfHeight = nodeHeight / 2.0;

        double maxY = nodes.Max(n => n.CanvasY) + nodeHeight + 60.0;
        double maxX = nodes.Max(n => n.CanvasX) + nodeWidth + 60.0;
        RoadmapCanvas.Width = Math.Max(780.0, maxX);
        RoadmapCanvas.Height = Math.Max(380.0, maxY);

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

        foreach (var node in nodes)
        {
            bool isSelected = ViewModel.SelectedNode?.Id == node.Id;
            var markerBtn = new Button
            {
                Width = nodeWidth,
                Height = nodeHeight,
                Padding = new Thickness(10, 6, 10, 6),
                CornerRadius = new CornerRadius(12),
                Tag = node,
                Background = isSelected
                    ? GetThemeBrush("SubtleFillColorSecondaryBrush")
                    : GetThemeBrush("CardBackgroundFillColorDefaultBrush"),
                BorderBrush = isSelected || node.Status is NodeStatus.Available or NodeStatus.InProgress
                    ? GetThemeBrush("AccentFillColorDefaultBrush")
                    : (node.Status == NodeStatus.Mastered
                        ? GetThemeBrush("SystemFillColorSuccessBrush")
                        : GetThemeBrush("CardStrokeColorDefaultBrush")),
                BorderThickness = new Thickness(isSelected ? 2.0 : 1.25)
            };

            var contentGrid = new Grid { ColumnSpacing = 8 };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var badge = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                VerticalAlignment = VerticalAlignment.Center,
                Background = GetThemeBrush("SubtleFillColorSecondaryBrush"),
                Child = new FontIcon
                {
                    FontSize = 12,
                    Glyph = node.StatusGlyph,
                    Foreground = node.Status == NodeStatus.Mastered
                        ? GetThemeBrush("SystemFillColorSuccessBrush")
                        : GetThemeBrush("AccentTextFillColorPrimaryBrush")
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
                Text = $"{node.TypeDisplay} • {node.ProgressPercentageDisplay}",
                FontSize = 10,
                Foreground = GetThemeBrush("TextFillColorSecondaryBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 1
            });
            Grid.SetColumn(textStack, 1);
            contentGrid.Children.Add(textStack);

            markerBtn.Content = contentGrid;
            ToolTipService.SetToolTip(markerBtn, $"{node.Title}\n{node.Description}\n{node.TypeDisplay} • {node.ProgressPercentageDisplay}");
            markerBtn.Click += NodeCard_Click;

            Canvas.SetLeft(markerBtn, node.CanvasX);
            Canvas.SetTop(markerBtn, node.CanvasY);
            RoadmapCanvas.Children.Add(markerBtn);
        }
    }

    private void NodeCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is RoadmapNodeDto node)
        {
            try
            {
                var animService = Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService.GetForCurrentView();
                animService.PrepareToAnimate("nodeTransition", btn);
                ViewModel.SelectNode(node);
                Bindings.Update();
                var anim = animService.GetAnimation("nodeTransition");
                if (anim != null && InspectorTitle != null)
                {
                    anim.TryStart(InspectorTitle);
                }
            }
            catch
            {
                ViewModel.SelectNode(node);
                Bindings.Update();
            }

            RenderMilestoneTrack();
        }
    }

    private void TimelineNodeSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.Tag is RoadmapNodeDto node)
        {
            ViewModel.SelectNode(node);
            ViewModel.SetViewMode(0); // Switch to Track view to see detail
            UpdateTabButtonsState();
            Bindings.Update();
            RenderMilestoneTrack();
        }
    }

    private void BtnSwitchRoadmap_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            ToolTipService.SetToolTip(btn, Loc.GetString("Roadmap_SelectGoal"));
            if (btn.Content is StackPanel sp && sp.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock tb)
            {
                tb.Text = Loc.GetString("Roadmap_BtnSelect");
            }
        }
    }

    private void BtnResetRoadmap_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            ToolTipService.SetToolTip(btn, Loc.GetString("Roadmap_ResetProgress"));
        }
    }

    private void BtnDeleteRoadmap_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            ToolTipService.SetToolTip(btn, Loc.GetString("Action_Delete"));
        }
    }

    private async void SwitchRoadmapItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is StudyRoadmapDto roadmap)
        {
            await ViewModel.SwitchRoadmapCommand.ExecuteAsync(roadmap.Id);
            UpdateTabButtonsState();
            Bindings.Update();
        }
    }

    private async void DeleteCurrentRoadmap_Click(object sender, RoutedEventArgs e)
    {
        var targetRoadmap = (sender as FrameworkElement)?.Tag as StudyRoadmapDto ?? ViewModel.ActiveRoadmap;
        if (targetRoadmap == null) return;

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
            await ViewModel.DeleteRoadmapCommand.ExecuteAsync(targetRoadmap.Id);
            UpdateTabButtonsState();
            Bindings.Update();
            RenderMilestoneTrack();
            RenderTimelineView();
        }
    }

    private async void ResetActiveRoadmapProgress_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveRoadmap == null) return;

        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Roadmap_ResetConfirmTitle"),
            Content = Loc.GetString("Roadmap_ResetConfirmMessage"),
            PrimaryButtonText = Loc.GetString("Roadmap_ResetProgress"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            CornerRadius = new CornerRadius(14),
            XamlRoot = this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot
        };

        var result = await confirmDialog.ShowAsyncSafe(this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot);
        if (result == ContentDialogResult.Primary)
        {
            await Task.Yield();
            await ViewModel.ResetRoadmapProgressCommand.ExecuteAsync(ViewModel.ActiveRoadmap.Id);
            Bindings.Update();
            RenderMilestoneTrack();
            RenderTimelineView();
        }
    }

    private async void ResetRoadmapItem_Click(object sender, RoutedEventArgs e)
    {
        var targetRoadmap = (sender as FrameworkElement)?.Tag as StudyRoadmapDto ?? ViewModel.ActiveRoadmap;
        if (targetRoadmap == null) return;

        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Roadmap_ResetConfirmTitle"),
            Content = Loc.GetString("Roadmap_ResetConfirmMessage"),
            PrimaryButtonText = Loc.GetString("Roadmap_ResetProgress"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            CornerRadius = new CornerRadius(14),
            XamlRoot = this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot
        };

        var result = await confirmDialog.ShowAsyncSafe(this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot);
        if (result == ContentDialogResult.Primary)
        {
            await Task.Yield();
            await ViewModel.ResetRoadmapProgressCommand.ExecuteAsync(targetRoadmap.Id);
            Bindings.Update();
            RenderMilestoneTrack();
            RenderTimelineView();
        }
    }

    private async void ClearInactiveRoadmaps_Click(object sender, RoutedEventArgs e)
    {
        var inactivesCount = ViewModel.AllRoadmaps.Count(r => !r.IsActive);
        if (inactivesCount == 0) return;

        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Roadmap_ClearInactiveConfirmTitle"),
            Content = Loc.GetString("Roadmap_ClearInactiveConfirmMessage"),
            PrimaryButtonText = Loc.GetString("Roadmap_ClearInactive"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            CornerRadius = new CornerRadius(14),
            XamlRoot = this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot
        };

        var result = await confirmDialog.ShowAsyncSafe(this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot);
        if (result == ContentDialogResult.Primary)
        {
            await Task.Yield();
            await ViewModel.ClearInactiveRoadmapsCommand.ExecuteAsync(null);
            Bindings.Update();
        }
    }

    private async void ClearAllRoadmaps_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.AllRoadmaps.Count == 0) return;

        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Roadmap_ClearAllConfirmTitle"),
            Content = Loc.GetString("Roadmap_ClearAllConfirmMessage"),
            PrimaryButtonText = Loc.GetString("Roadmap_ClearAll"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            CornerRadius = new CornerRadius(14),
            XamlRoot = this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot
        };

        var result = await confirmDialog.ShowAsyncSafe(this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot);
        if (result == ContentDialogResult.Primary)
        {
            await Task.Yield();
            await ViewModel.ClearAllRoadmapsCommand.ExecuteAsync(null);
            Bindings.Update();
            UpdateTabButtonsState();
            RenderMilestoneTrack();
            RenderTimelineView();
        }
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
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(10, 4, 10, 4),
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
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(10, 4, 10, 4),
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
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(10, 4, 10, 4),
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
                UpdateTabButtonsState();
            });
        }
    }
}
