using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class KnowledgeGraphDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly IVocabularyKnowledgeGraphService _graphService;
    private readonly IAudioPlayerService? _audioService;
    public ILocalizationService Loc { get; }

    private Guid _currentWordId;
    private VocabularyKnowledgeGraphDto _graph = VocabularyKnowledgeGraphDto.Empty;
    private bool _isLoading;
    private string _currentFilterTag = "All";

    public VocabularyKnowledgeGraphDto Graph
    {
        get => _graph;
        set
        {
            _graph = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AllTabText));
            ApplyCurrentFilter();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string AllTabText => string.Format(Loc.GetString("KnowledgeGraph_AllTab"), Graph.ConnectedNodes.Count);

    public ObservableCollection<GraphNodeDto> FilteredNodes { get; } = [];

    public KnowledgeGraphDialog(
        Guid initialWordId,
        IVocabularyKnowledgeGraphService graphService,
        ILocalizationService loc,
        IAudioPlayerService? audioService = null)
    {
        _currentWordId = initialWordId;
        _graphService = graphService;
        Loc = loc;
        _audioService = audioService;

        this.InitializeComponent();

        Loaded += async (s, e) =>
        {
            await LoadGraphForWordAsync(_currentWordId);
        };
    }

    public async Task LoadGraphForWordAsync(Guid wordId)
    {
        _currentWordId = wordId;
        IsLoading = true;
        try
        {
            var graph = await _graphService.BuildGraphAsync(wordId);
            if (graph != null)
            {
                Graph = graph;
            }
        }
        catch
        {
            // Fallback gracefully on error
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyCurrentFilter()
    {
        FilteredNodes.Clear();
        var nodes = _currentFilterTag switch
        {
            "Synonym" => Graph.ConnectedNodes.Where(n => n.RelationshipLabel.Equals("Synonym", StringComparison.OrdinalIgnoreCase)),
            "Antonym" => Graph.ConnectedNodes.Where(n => n.RelationshipLabel.Equals("Antonym", StringComparison.OrdinalIgnoreCase)),
            "WordFamily" => Graph.ConnectedNodes.Where(n => n.RelationshipLabel.Equals("Family", StringComparison.OrdinalIgnoreCase) || n.RelationshipLabel.Equals("Derivative", StringComparison.OrdinalIgnoreCase)),
            "Collocation" => Graph.ConnectedNodes.Where(n => n.RelationshipLabel.Equals("Collocation", StringComparison.OrdinalIgnoreCase)),
            _ => Graph.ConnectedNodes.AsEnumerable()
        };

        foreach (var node in nodes)
        {
            FilteredNodes.Add(node);
        }
    }

    private void FilterTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;

        _currentFilterTag = tag;

        // Reset visual styles
        TabAllBtn.Style = (Style)App.Current.Resources["DefaultButtonStyle"];
        TabSynBtn.Style = (Style)App.Current.Resources["DefaultButtonStyle"];
        TabAntBtn.Style = (Style)App.Current.Resources["DefaultButtonStyle"];
        TabFamBtn.Style = (Style)App.Current.Resources["DefaultButtonStyle"];
        TabColBtn.Style = (Style)App.Current.Resources["DefaultButtonStyle"];

        btn.Style = (Style)App.Current.Resources["AccentButtonStyle"];

        ApplyCurrentFilter();
    }

    private async void PivotToNode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid targetWordId)
        {
            await LoadGraphForWordAsync(targetWordId);
        }
    }

    private async void PlayCenterAudio_Click(object sender, RoutedEventArgs e)
    {
        if (_audioService != null && !string.IsNullOrWhiteSpace(Graph.CenterNode.Term))
        {
            try
            {
                await _audioService.PlayPronunciationAsync(Graph.CenterNode.Term, "uk");
            }
            catch
            {
                // Ignored
            }
        }
    }

    private async void GraphSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && !string.IsNullOrWhiteSpace(sender.Text))
        {
            var matches = await _graphService.SearchGraphNodesAsync(sender.Text.Trim());
            sender.ItemsSource = matches;
        }
        else if (string.IsNullOrWhiteSpace(sender.Text))
        {
            sender.ItemsSource = null;
        }
    }

    private async void GraphSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is WordDto chosenWord)
        {
            sender.Text = chosenWord.Term;
            await LoadGraphForWordAsync(chosenWord.Id);
        }
        else if (args.SelectedItem is GraphNodeDto chosenNode)
        {
            sender.Text = chosenNode.Term;
            await LoadGraphForWordAsync(chosenNode.Id);
        }
    }

    private async void GraphSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is WordDto chosenWord)
        {
            await LoadGraphForWordAsync(chosenWord.Id);
        }
        else if (args.ChosenSuggestion is GraphNodeDto chosenNode)
        {
            await LoadGraphForWordAsync(chosenNode.Id);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            var matches = await _graphService.SearchGraphNodesAsync(args.QueryText.Trim());
            var first = matches.FirstOrDefault();
            if (first != null)
            {
                await LoadGraphForWordAsync(first.Id);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
