using System.ComponentModel;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Storage.Pickers;

namespace Lexora.Presentation.Views;

public sealed partial class KnowledgeGraphPage : Page, ILocalizableView, IThemeAwareView
{
    public KnowledgeGraphViewModel ViewModel { get; }
    public ILocalizationService Loc { get; }

    public KnowledgeGraphPage()
        : this(
            App.Current.Services.GetRequiredService<KnowledgeGraphViewModel>(),
            App.Current.Services.GetRequiredService<ILocalizationService>())
    {
    }

    public KnowledgeGraphPage(
        KnowledgeGraphViewModel viewModel,
        ILocalizationService loc)
    {
        ViewModel = viewModel;
        Loc = loc;

        this.InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        Loaded += async (s, e) =>
        {
            if (!ViewModel.HasActiveGraph)
            {
                await ViewModel.InitializeAsync();
                RenderGraphCanvas();
            }
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid wordId && wordId != Guid.Empty)
        {
            await ViewModel.InitializeAsync(wordId);
            RenderGraphCanvas();
        }
        else if (e.Parameter is string term && !string.IsNullOrWhiteSpace(term))
        {
            await ViewModel.LoadGraphForTermAsync(term);
            RenderGraphCanvas();
        }
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var isCompact = e.NewSize.Width < 1200;
        var btnTextVisibility = isCompact ? Visibility.Collapsed : Visibility.Visible;

        if (BtnRecommendationsText != null) BtnRecommendationsText.Visibility = btnTextVisibility;
        if (BtnSmartImportText != null) BtnSmartImportText.Visibility = btnTextVisibility;
        if (BtnExportText != null) BtnExportText.Visibility = btnTextVisibility;
        if (BtnViewModeText != null) BtnViewModeText.Visibility = btnTextVisibility;

        if (HeaderSearchBox != null)
        {
            HeaderSearchBox.MaxWidth = e.NewSize.Width < 1000 ? 180 : 280;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.ActiveGraph) ||
            e.PropertyName == nameof(ViewModel.FilteredConnectedNodes) ||
            e.PropertyName == nameof(ViewModel.SelectedCategoryFilter) ||
            e.PropertyName == nameof(ViewModel.SelectedNode) ||
            e.PropertyName == nameof(ViewModel.HasSelectedNode) ||
            e.PropertyName == nameof(ViewModel.IsVisualMapMode))
        {
            Bindings.Update();
            RenderGraphCanvas();
        }
    }

    public void OnLanguageChanged()
    {
        void UpdateUI()
        {
            ViewModel.UpdateLocalizedTexts();
            Bindings.Update();
            RenderGraphCanvas();
        }

        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateUI();
        }
        else
        {
            DispatcherQueue.TryEnqueue(UpdateUI);
        }
    }

    public void OnThemeChanged()
    {
        void UpdateUI()
        {
            Bindings.Update();
            RenderGraphCanvas();
        }

        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateUI();
        }
        else
        {
            DispatcherQueue.TryEnqueue(UpdateUI);
        }
    }

    private void GraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderGraphCanvas();
    }

    private void RenderGraphCanvas()
    {
        if (!ViewModel.IsVisualMapMode || GraphCanvas == null) return;

        GraphCanvas.Children.Clear();

        double width = GraphCanvas.ActualWidth;
        double height = GraphCanvas.ActualHeight;

        if (width <= 0 || height <= 0 || !ViewModel.HasActiveGraph) return;

        double cx = width / 2.0;
        double cy = height / 2.0;

        var nodes = ViewModel.FilteredConnectedNodes.Count > 0
            ? ViewModel.FilteredConnectedNodes
            : ViewModel.ActiveGraph.ConnectedNodes;

        int count = nodes.Count;
        double baseRadius = Math.Clamp(Math.Min(width, height) / 2.0 - 55.0, 110.0, 190.0);
        double radius = Math.Clamp(baseRadius * ViewModel.CanvasScale, 60.0, 400.0);

        // 1. Draw Connecting Lines first so they sit under the nodes
        for (int i = 0; i < count; i++)
        {
            double angle = (2.0 * Math.PI * i / count) - (Math.PI / 2.0);
            double nx = cx + (radius * Math.Cos(angle));
            double ny = cy + (radius * Math.Sin(angle));

            var line = new Line
            {
                X1 = cx,
                Y1 = cy,
                X2 = nx,
                Y2 = ny,
                Stroke = GetRelationLineBrush(nodes[i].NodeType),
                StrokeThickness = 1.8
            };

            if (nodes[i].NodeType == KnowledgeGraphNodeType.Antonym)
            {
                line.StrokeDashArray = new DoubleCollection { 4, 3 };
            }

            GraphCanvas.Children.Add(line);
        }

        // 2. Draw Surrounding Orbit Nodes
        for (int i = 0; i < count; i++)
        {
            var node = nodes[i];
            double angle = (2.0 * Math.PI * i / count) - (Math.PI / 2.0);
            double nx = cx + (radius * Math.Cos(angle));
            double ny = cy + (radius * Math.Sin(angle));

            bool isSelected = ViewModel.SelectedNode != null &&
                              (ReferenceEquals(ViewModel.SelectedNode, node) || ViewModel.SelectedNode.Id == node.Id);

            var nodeBorder = new Border
            {
                Background = (Brush)App.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = isSelected
                    ? (Brush)App.Current.Resources["AccentFillColorDefaultBrush"]
                    : (Brush)App.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(isSelected ? 2.5 : 1.2),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 6, 10, 6),
                Tag = node
            };

            var nodePanel = new StackPanel { Spacing = 3 };
            var topPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

            var relBadge = new Border
            {
                Background = GetRelationBadgeBackground(node.NodeType),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 2, 5, 2)
            };
            var relText = new TextBlock
            {
                Text = GetShortRelLabel(node.NodeType),
                FontSize = 9.5,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = GetRelationTextForeground(node.NodeType)
            };
            relBadge.Child = relText;
            topPanel.Children.Add(relBadge);

            if (node.Level != CefrLevel.None)
            {
                var lvlBadge = new Border
                {
                    Background = (Brush)App.Current.Resources["SubtleFillColorSecondaryBrush"],
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 1, 4, 1)
                };
                var lvlText = new TextBlock
                {
                    Text = node.Level.ToString(),
                    FontSize = 9,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = (Brush)App.Current.Resources["TextFillColorSecondaryBrush"]
                };
                lvlBadge.Child = lvlText;
                topPanel.Children.Add(lvlBadge);
            }

            var termText = new TextBlock
            {
                Text = node.Term,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = (Brush)App.Current.Resources["TextFillColorPrimaryBrush"],
                TextWrapping = TextWrapping.NoWrap
            };

            nodePanel.Children.Add(topPanel);
            nodePanel.Children.Add(termText);
            nodeBorder.Child = nodePanel;

            // Interactive handlers
            nodeBorder.Tapped += (s, ev) =>
            {
                if (s is Border b && b.Tag is GraphNodeDto clickedNode)
                {
                    ViewModel.SelectNode(clickedNode);
                    RenderGraphCanvas();
                }
            };

            nodeBorder.DoubleTapped += async (s, ev) =>
            {
                if (s is Border b && b.Tag is GraphNodeDto clickedNode)
                {
                    await ViewModel.PivotToNodeInternalAsync(clickedNode.Id, clickedNode.Term);
                }
            };

            // Measure & arrange
            nodeBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double nw = Math.Max(nodeBorder.DesiredSize.Width + 10, 82);
            double nh = Math.Max(nodeBorder.DesiredSize.Height, 42);

            Canvas.SetLeft(nodeBorder, nx - (nw / 2.0));
            Canvas.SetTop(nodeBorder, ny - (nh / 2.0));

            GraphCanvas.Children.Add(nodeBorder);
        }

        // 3. Draw Central Core Concept Node with Click Selection & Dynamic Auto-Sizing
        bool isCenterSelected = ViewModel.SelectedNode != null &&
                                (ReferenceEquals(ViewModel.SelectedNode, ViewModel.ActiveGraph.CenterNode) ||
                                 ViewModel.SelectedNode.Id == ViewModel.ActiveGraph.CenterNode.Id);

        var centerBorder = new Border
        {
            Background = (Brush)App.Current.Resources["AccentFillColorDefaultBrush"],
            BorderBrush = isCenterSelected
                ? (Brush)App.Current.Resources["SystemFillColorCautionBrush"]
                : (Brush)App.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(isCenterSelected ? 3.5 : 1.5),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(22, 10, 22, 10),
            MinWidth = 145,
            MinHeight = 60,
            Tag = ViewModel.ActiveGraph.CenterNode
        };

        var centerPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2
        };

        var centerLabel = new TextBlock
        {
            Text = Loc.GetString("KnowledgeGraph_CoreConcept"),
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)App.Current.Resources["TextOnAccentFillColorPrimaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap
        };

        var centerTerm = new TextBlock
        {
            Text = ViewModel.ActiveGraph.CenterNode.Term,
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = (Brush)App.Current.Resources["TextOnAccentFillColorPrimaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap
        };

        centerPanel.Children.Add(centerLabel);
        centerPanel.Children.Add(centerTerm);
        centerBorder.Child = centerPanel;

        centerBorder.Tapped += (s, ev) =>
        {
            ViewModel.SelectNode(ViewModel.ActiveGraph.CenterNode);
            RenderGraphCanvas();
        };

        centerBorder.DoubleTapped += (s, ev) =>
        {
            ViewModel.NavigateToVocabulary(ViewModel.ActiveGraph.CenterNode.Id);
        };

        centerBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double cw = Math.Max(centerBorder.DesiredSize.Width + 14, 145);
        double ch = Math.Max(centerBorder.DesiredSize.Height, 60);

        Canvas.SetLeft(centerBorder, cx - (cw / 2.0));
        Canvas.SetTop(centerBorder, cy - (ch / 2.0));
        GraphCanvas.Children.Add(centerBorder);
    }

    private Brush GetRelationLineBrush(KnowledgeGraphNodeType type) => type switch
    {
        KnowledgeGraphNodeType.Synonym => (Brush)App.Current.Resources["SystemFillColorSuccessBrush"],
        KnowledgeGraphNodeType.Antonym => (Brush)App.Current.Resources["SystemFillColorCriticalBrush"],
        KnowledgeGraphNodeType.WordFamily => (Brush)App.Current.Resources["AccentFillColorDefaultBrush"],
        KnowledgeGraphNodeType.Collocation => (Brush)App.Current.Resources["SystemFillColorCautionBrush"],
        KnowledgeGraphNodeType.TopicCluster => (Brush)App.Current.Resources["AccentTextFillColorPrimaryBrush"],
        _ => (Brush)App.Current.Resources["TextFillColorSecondaryBrush"]
    };

    private Brush GetRelationBadgeBackground(KnowledgeGraphNodeType type) => type switch
    {
        KnowledgeGraphNodeType.Synonym => (Brush)App.Current.Resources["SystemFillColorSuccessBackgroundBrush"],
        KnowledgeGraphNodeType.Antonym => (Brush)App.Current.Resources["SystemFillColorCriticalBackgroundBrush"],
        KnowledgeGraphNodeType.Collocation => (Brush)App.Current.Resources["SystemFillColorCautionBackgroundBrush"],
        KnowledgeGraphNodeType.TopicCluster => (Brush)App.Current.Resources["SubtleFillColorSecondaryBrush"],
        KnowledgeGraphNodeType.WordFamily => (Brush)App.Current.Resources["SubtleFillColorSecondaryBrush"],
        _ => (Brush)App.Current.Resources["SubtleFillColorSecondaryBrush"]
    };

    private Brush GetRelationTextForeground(KnowledgeGraphNodeType type) => type switch
    {
        KnowledgeGraphNodeType.Synonym => (Brush)App.Current.Resources["SystemFillColorSuccessBrush"],
        KnowledgeGraphNodeType.Antonym => (Brush)App.Current.Resources["SystemFillColorCriticalBrush"],
        KnowledgeGraphNodeType.WordFamily => (Brush)App.Current.Resources["AccentTextFillColorPrimaryBrush"],
        KnowledgeGraphNodeType.Collocation => (Brush)App.Current.Resources["SystemFillColorCautionBrush"],
        KnowledgeGraphNodeType.TopicCluster => (Brush)App.Current.Resources["AccentTextFillColorPrimaryBrush"],
        _ => (Brush)App.Current.Resources["TextFillColorSecondaryBrush"]
    };

    private string GetShortRelLabel(KnowledgeGraphNodeType type) => type switch
    {
        KnowledgeGraphNodeType.Synonym => Loc.GetString("KnowledgeGraph_SynonymsTab"),
        KnowledgeGraphNodeType.Antonym => Loc.GetString("KnowledgeGraph_AntonymsTab"),
        KnowledgeGraphNodeType.WordFamily => Loc.GetString("KnowledgeGraph_FamilyTab"),
        KnowledgeGraphNodeType.Collocation => Loc.GetString("KnowledgeGraph_CollocationsTab"),
        KnowledgeGraphNodeType.TopicCluster => Loc.GetString("KnowledgeGraph_TopicTab"),
        _ => "Rel"
    };

    // Event Handlers
    private async void HeaderSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            await ViewModel.SearchSuggestionsAsync(sender.Text);
            sender.ItemsSource = ViewModel.SearchSuggestions;
        }
    }

    private async void HeaderSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is WordDto word)
        {
            sender.Text = word.Term;
            await ViewModel.LoadGraphAsync(word.Id);
        }
    }

    private async void HeaderSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is WordDto word)
        {
            await ViewModel.LoadGraphAsync(word.Id);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            await ViewModel.LoadGraphForTermAsync(args.QueryText);
        }
    }

    private async void CatalogSearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ViewModel.PerformSearchAsync(CatalogSearchTextBox.Text);
        }
    }

    private async void LevelFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;

        ResetLevelFilterButtons();
        btn.Style = (Style)App.Current.Resources["AccentButtonStyle"];

        CefrLevel? level = tag switch
        {
            "A1" => CefrLevel.A1,
            "A2" => CefrLevel.A2,
            "B1" => CefrLevel.B1,
            "B2" => CefrLevel.B2,
            "C1" => CefrLevel.C1,
            "C2" => CefrLevel.C2,
            _ => null
        };

        await ViewModel.FilterByLevelAsync(level);
    }

    private void ResetLevelFilterButtons()
    {
        var defaultStyle = (Style)App.Current.Resources["DefaultButtonStyle"];
        LevelFilterAllBtn.Style = defaultStyle;
        LevelFilterA1Btn.Style = defaultStyle;
        LevelFilterA2Btn.Style = defaultStyle;
        LevelFilterB1Btn.Style = defaultStyle;
        LevelFilterB2Btn.Style = defaultStyle;
        LevelFilterC1Btn.Style = defaultStyle;
        LevelFilterC2Btn.Style = defaultStyle;
    }

    private async void FilterTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;

        ResetFilterTabButtons();
        btn.Style = (Style)App.Current.Resources["AccentButtonStyle"];

        await ViewModel.FilterByCategoryAsync(tag);
        RenderGraphCanvas();
    }

    private void ResetFilterTabButtons()
    {
        var defaultStyle = (Style)App.Current.Resources["DefaultButtonStyle"];
        TabAllBtn.Style = defaultStyle;
        TabSynBtn.Style = defaultStyle;
        TabAntBtn.Style = defaultStyle;
        TabFamBtn.Style = defaultStyle;
        TabColBtn.Style = defaultStyle;
        TabTopBtn.Style = defaultStyle;
    }

    private async void ConceptItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid wordId)
        {
            await ViewModel.LoadGraphAsync(wordId);
        }
    }

    private async void RecommendItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid wordId)
        {
            BtnRecommendations.Flyout?.Hide();
            await ViewModel.LoadGraphAsync(wordId);
        }
    }

    private async void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PreviousPageAsync();
    }

    private async void NextPage_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NextPageAsync();
    }

    private async void BtnHistoryBack_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NavigateBackAsync();
    }

    private void BtnViewMode_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleViewMode();
        Bindings.Update();
        if (ViewModel.IsVisualMapMode)
        {
            RenderGraphCanvas();
        }
    }

    private async void PlayAudioUs_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlayCenterPronunciationAsync("us");
    }

    private async void PlayAudioUk_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlayCenterPronunciationAsync("uk");
    }

    private async void PlaySelectedAudio_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlaySelectedPronunciationAsync("us");
    }

    private async void PlayNodeItemAudio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn.Tag is string term)
            {
                await ViewModel.PlayTermPronunciationAsync(term, "us");
            }
            else if (btn.Tag is GraphNodeDto node)
            {
                await ViewModel.PlayTermPronunciationAsync(node.Term, "us");
            }
        }
    }

    private void CardItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is GraphNodeDto node)
        {
            ViewModel.SelectNode(node);
        }
    }

    private void ViewInVocab_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToVocabulary();
    }

    private async void PivotToNode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn.Tag is Guid targetId)
            {
                var term = ViewModel.ActiveGraph?.ConnectedNodes?.FirstOrDefault(n => n.Id == targetId)?.Term;
                await ViewModel.PivotToNodeInternalAsync(targetId, term);
            }
            else if (btn.Tag is GraphNodeDto node)
            {
                await ViewModel.PivotToNodeInternalAsync(node.Id, node.Term);
            }
        }
    }

    private async void PivotSelectedNode_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode != null)
        {
            await ViewModel.PivotToNodeInternalAsync(ViewModel.SelectedNode.Id, ViewModel.SelectedNode.Term);
        }
    }

    private void InspectNode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GraphNodeDto node)
        {
            ViewModel.SelectNode(node);
        }
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ZoomIn();
        RenderGraphCanvas();
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ZoomOut();
        RenderGraphCanvas();
    }

    private void ResetZoom_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetZoom();
        RenderGraphCanvas();
    }

    private void CopySelectedTerm_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopySelectedTerm();
    }

    private void ViewSelectedInVocab_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode != null)
        {
            ViewModel.NavigateToVocabulary(ViewModel.SelectedNode.Id);
        }
    }

    private async void BtnSmartImport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new KnowledgeGraphImportDialog
        {
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsyncSafe();
        if (dialog.WasCommitted)
        {
            await ViewModel.LoadCatalogPageAsync(ViewModel.CurrentPage);
            if (ViewModel.CurrentCenterWordId.HasValue)
            {
                await ViewModel.LoadGraphAsync(ViewModel.CurrentCenterWordId.Value, addToHistory: false);
            }
            RenderGraphCanvas();
        }
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e) => await ExportToFileAsync("json");
    private async void ExportCsv_Click(object sender, RoutedEventArgs e) => await ExportToFileAsync("csv");
    private async void ExportMarkdown_Click(object sender, RoutedEventArgs e) => await ExportToFileAsync("md");

    private async Task ExportToFileAsync(string format)
    {
        try
        {
            var content = await ViewModel.ExportAsync(format);
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"Lexora_SemanticGraph_{DateTime.Now:yyyyMMdd}"
            };

            var ext = format switch
            {
                "csv" => ".csv",
                "md" or "markdown" => ".md",
                _ => ".json"
            };
            picker.FileTypeChoices.Add($"Knowledge Graph Export ({ext})", [ext]);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, content);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KnowledgeGraphPage] ExportToFileAsync error: {ex.Message}");
        }
    }
}
