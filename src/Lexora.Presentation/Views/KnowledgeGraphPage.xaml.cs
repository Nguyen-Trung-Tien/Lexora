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

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.ActiveGraph) ||
            e.PropertyName == nameof(ViewModel.FilteredConnectedNodes) ||
            e.PropertyName == nameof(ViewModel.SelectedCategoryFilter) ||
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
        double radius = Math.Clamp(Math.Min(width, height) / 2.0 - 55.0, 110.0, 190.0);

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
                Stroke = (Brush)App.Current.Resources["CardStrokeColorDefaultBrush"],
                StrokeThickness = 1.2
            };

            if (nodes[i].NodeType == KnowledgeGraphNodeType.Antonym)
            {
                line.StrokeDashArray = new DoubleCollection { 3, 2 };
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

            var nodeBorder = new Border
            {
                Background = (Brush)App.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = ReferenceEquals(ViewModel.SelectedNode, node)
                    ? (Brush)App.Current.Resources["AccentFillColorDefaultBrush"]
                    : (Brush)App.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(ReferenceEquals(ViewModel.SelectedNode, node) ? 2 : 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 4, 8, 4),
                Tag = node
            };

            var nodePanel = new StackPanel { Spacing = 2 };
            var topPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };

            var relBadge = new Border
            {
                Background = (Brush)App.Current.Resources["SubtleFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 1, 4, 1)
            };
            var relText = new TextBlock
            {
                Text = GetShortRelLabel(node.NodeType),
                FontSize = 9,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)App.Current.Resources["AccentTextFillColorPrimaryBrush"]
            };
            relBadge.Child = relText;
            topPanel.Children.Add(relBadge);

            var termText = new TextBlock
            {
                Text = node.Term,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
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
                    await ViewModel.PivotToNodeAsync(clickedNode.Id);
                }
            };

            // Measure & arrange
            nodeBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double nw = Math.Max(nodeBorder.DesiredSize.Width, 70);
            double nh = Math.Max(nodeBorder.DesiredSize.Height, 36);

            Canvas.SetLeft(nodeBorder, nx - (nw / 2.0));
            Canvas.SetTop(nodeBorder, ny - (nh / 2.0));

            GraphCanvas.Children.Add(nodeBorder);
        }

        // 3. Draw Central Core Concept Node
        var centerBorder = new Border
        {
            Background = (Brush)App.Current.Resources["AccentFillColorDefaultBrush"],
            BorderBrush = (Brush)App.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 8, 14, 8),
            Width = 120,
            Height = 56
        };

        var centerPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 1
        };

        var centerLabel = new TextBlock
        {
            Text = Loc.GetString("KnowledgeGraph_CoreConcept"),
            FontSize = 9,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = (Brush)App.Current.Resources["TextOnAccentFillColorPrimaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var centerTerm = new TextBlock
        {
            Text = ViewModel.ActiveGraph.CenterNode.Term,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = (Brush)App.Current.Resources["TextOnAccentFillColorPrimaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center
        };

        centerPanel.Children.Add(centerLabel);
        centerPanel.Children.Add(centerTerm);
        centerBorder.Child = centerPanel;

        Canvas.SetLeft(centerBorder, cx - 60);
        Canvas.SetTop(centerBorder, cy - 28);
        GraphCanvas.Children.Add(centerBorder);
    }

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
        await ViewModel.PlayPronunciationAsync("us");
    }

    private async void PlayAudioUk_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlayPronunciationAsync("uk");
    }

    private async void PlaySelectedAudio_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlayPronunciationAsync("us");
    }

    private void ViewInVocab_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToVocabulary();
    }

    private async void PivotToNode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid targetId)
        {
            await ViewModel.PivotToNodeAsync(targetId);
        }
    }

    private async void PivotSelectedNode_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode != null)
        {
            await ViewModel.PivotToNodeAsync(ViewModel.SelectedNode.Id);
        }
    }

    private void InspectNode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GraphNodeDto node)
        {
            ViewModel.SelectNode(node);
        }
    }
}
