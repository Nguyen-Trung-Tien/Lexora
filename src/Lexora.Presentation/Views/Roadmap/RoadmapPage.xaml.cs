using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Lexora.Presentation.Views.Roadmap;

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
            else if (e.PropertyName == nameof(ViewModel.StatusMessage))
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    if (StatusInfoBar != null)
                    {
                        var msg = ViewModel.StatusMessage;
                        StatusInfoBar.IsOpen = !string.IsNullOrWhiteSpace(msg);
                        StatusInfoBar.Severity = (!string.IsNullOrWhiteSpace(msg) &&
                            (msg.Contains("error", StringComparison.OrdinalIgnoreCase)
                             || msg.Contains("lá»—i", StringComparison.OrdinalIgnoreCase)
                             || msg.Contains("fail", StringComparison.OrdinalIgnoreCase)
                             || msg.Contains("exception", StringComparison.OrdinalIgnoreCase)))
                            ? InfoBarSeverity.Error
                            : InfoBarSeverity.Informational;
                    }
                });
            }
        };
        ViewModel.PropertyChanged += _propChangedHandler;

        Loaded += Page_Loaded;
        SizeChanged += Page_SizeChanged;
        ActualThemeChanged += (s, e) => OnThemeChanged();
    }

    private void StatusInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        ViewModel.StatusMessage = string.Empty;
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
            double w = ActualWidth > 0 ? ActualWidth : (XamlRoot?.Size.Width ?? 1200);
            UpdateResponsiveLayout(w);
            Bindings.Update();
            UpdateTabButtonsState();
            RenderMilestoneTrack();

            DispatcherQueue.TryEnqueue(() =>
            {
                if (ActualWidth > 0)
                {
                    UpdateResponsiveLayout(ActualWidth);
                }
            });
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
            bool isHeaderCompact = width < 1240;
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
            bool isManageCompact = width < 1240;
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
            else if (width < 1240)
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
            var leftContainer = (FrameworkElement?)TrackWorkspaceLeftPanel;
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
        if (TrackViewTabButton == null || ManageViewTabButton == null) return;

        var accentBg = GetThemeBrush("AccentFillColorDefaultBrush");
        var accentFg = GetThemeBrush("TextOnAccentFillColorPrimaryBrush");
        var transparent = new SolidColorBrush(Colors.Transparent);
        var normalFg = GetThemeBrush("TextFillColorPrimaryBrush");

        // Tab 1: Track View
        TrackViewTabButton.Background = ViewModel.IsTrackView ? accentBg : transparent;
        TrackViewTabButton.Foreground = ViewModel.IsTrackView ? accentFg : normalFg;

        // Tab 2: Manage Goals View
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

    private void SwitchToManageView_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetViewMode(1);
        UpdateTabButtonsState();
        Bindings.Update();
    }

    private void RenderMilestoneTrack()
    {
        RenderSerpentineCanvas();
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
        try
        {
            if (sender is Button btn && btn.Tag is StudyRoadmapDto roadmap)
            {
                await ViewModel.SwitchRoadmapCommand.ExecuteAsync(roadmap.Id);
                UpdateTabButtonsState();
                Bindings.Update();
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = ex.Message;
        }
    }

    private async void DeleteCurrentRoadmap_Click(object sender, RoutedEventArgs e)
    {
        try
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
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = ex.Message;
        }
    }

    private async void ResetActiveRoadmapProgress_Click(object sender, RoutedEventArgs e)
    {
        try
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
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = ex.Message;
        }
    }

    private async void ResetRoadmapItem_Click(object sender, RoutedEventArgs e)
    {
        try
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
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = ex.Message;
        }
    }

    private async void ClearInactiveRoadmaps_Click(object sender, RoutedEventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            ViewModel.StatusMessage = ex.Message;
        }
    }

    private async void ClearAllRoadmaps_Click(object sender, RoutedEventArgs e)
    {
        try
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
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = ex.Message;
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

        var previewBanner = new Border
        {
            Padding = new Thickness(12, 8, 12, 8),
            CornerRadius = new CornerRadius(8),
            Background = GetThemeBrush("SubtleFillColorSecondaryBrush"),
            BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1)
        };
        var previewText = new TextBlock
        {
            Style = (Style)App.Current.Resources["CaptionTextBlockStyle"],
            Foreground = GetThemeBrush("AccentTextFillColorPrimaryBrush"),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        previewBanner.Child = previewText;
        contentPanel.Children.Add(previewBanner);

        void UpdatePacePreview()
        {
            if (targetCombo.SelectedItem is not ExamTarget t) return;
            double s = double.IsNaN(startScoreBox.Value) ? 0 : startScoreBox.Value;
            double g = double.IsNaN(targetScoreBox.Value) ? 0 : targetScoreBox.Value;
            int w = double.IsNaN(durationBox.Value) ? 12 : Math.Max(2, (int)durationBox.Value);
            var previewEngine = new Lexora.Application.Services.RoadmapEngine();
            var sample = previewEngine.GenerateAdaptiveRoadmap(string.Empty, t, s, g, w);
            int totalItems = sample.Milestones.SelectMany(m => m.Nodes).Sum(n => n.TargetItemCount);
            int daily = Math.Max(1, (int)Math.Ceiling((double)totalItems / (w * 7)));
            previewText.Text = string.Format(
                ViewModel.Loc.GetString("Roadmap_EstimatedPacePreview"),
                daily,
                w,
                sample.Milestones.Count);
        }

        UpdatePacePreview();
        targetCombo.SelectionChanged += (s, args) => UpdatePacePreview();
        startScoreBox.ValueChanged += (s, args) => UpdatePacePreview();
        targetScoreBox.ValueChanged += (s, args) => UpdatePacePreview();
        durationBox.ValueChanged += (s, args) => UpdatePacePreview();

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
