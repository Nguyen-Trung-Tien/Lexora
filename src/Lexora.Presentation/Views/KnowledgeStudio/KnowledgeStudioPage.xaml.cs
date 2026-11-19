using Lexora.Presentation.Services;
using Lexora.Presentation.Views.Grammar;
using Lexora.Presentation.Views.Homophones;
using Lexora.Presentation.Views.KnowledgeGraph;
using Lexora.Presentation.Views.MemoryTips;
using Lexora.Presentation.Views.Phrases;
using Lexora.Presentation.Views.Vocabulary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views.KnowledgeStudio;

public sealed partial class KnowledgeStudioPage : Page, ILocalizableView, IThemeAwareView
{
    private readonly ILocalizationService _localizationService;
    private string _activeTab = "Vocabulary";
    private int _previousTabIndex = 0;
    private object? _pendingParam;

    public KnowledgeStudioPage()
    {
        _localizationService = App.Current.Services.GetRequiredService<ILocalizationService>();
        InitializeComponent();
        UpdateLocalizedLabels();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string tabTag && !string.IsNullOrWhiteSpace(tabTag))
        {
            SelectTab(tabTag);
            return;
        }

        if (StudioContentFrame.Content == null)
        {
            SelectTab("Vocabulary");
        }
        else if (StudioContentFrame.CurrentSourcePageType is { } activePageType)
        {
            App.Current.Services.GetService<IShortcutManager>()?.SetCurrentPage(activePageType);
        }
    }

    public void SelectTab(string tabTag, object? parameter = null)
    {
        _activeTab = tabTag;
        _pendingParam = parameter;

        var targetItem = tabTag switch
        {
            "KnowledgeGraph" => TabKnowledgeGraph,
            "Homophones" => TabHomophones,
            "Grammar" => TabGrammar,
            "Phrases" => TabPhrases,
            "MemoryTips" => TabMemoryTips,
            _ => TabVocabulary
        };

        if (!ReferenceEquals(StudioSelectorBar.SelectedItem, targetItem))
        {
            StudioSelectorBar.SelectedItem = targetItem;
        }
        else
        {
            NavigateToTab(tabTag, parameter);
        }
    }

    private void StudioSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem is SelectorBarItem item && item.Tag is string tag)
        {
            var param = string.Equals(tag, _activeTab, StringComparison.OrdinalIgnoreCase) ? _pendingParam : null;
            _pendingParam = null;
            _activeTab = tag;
            NavigateToTab(tag, param);
        }
    }

    private void NavigateToTab(string tag, object? parameter)
    {
        (Type targetPage, int newIndex) = tag switch
        {
            "KnowledgeGraph" => (typeof(KnowledgeGraphPage), 1),
            "Homophones" => (typeof(HomophonesPage), 2),
            "Grammar" => (typeof(GrammarPage), 3),
            "Phrases" => (typeof(PhrasesPage), 4),
            "MemoryTips" => (typeof(MemoryTipsPage), 5),
            _ => (typeof(VocabularyPage), 0)
        };

        if (StudioContentFrame.CurrentSourcePageType == targetPage && parameter == null)
        {
            App.Current.Services.GetService<IShortcutManager>()?.SetCurrentPage(targetPage);
            return;
        }

        var effect = newIndex >= _previousTabIndex
            ? SlideNavigationTransitionEffect.FromRight
            : SlideNavigationTransitionEffect.FromLeft;
        _previousTabIndex = newIndex;

        StudioContentFrame.Navigate(targetPage, parameter, new SlideNavigationTransitionInfo { Effect = effect });
        App.Current.Services.GetService<IShortcutManager>()?.SetCurrentPage(targetPage);
    }

    public void OnLanguageChanged()
    {
        UpdateLocalizedLabels();
        if (StudioContentFrame.Content is ILocalizableView child)
        {
            child.OnLanguageChanged();
        }
    }

    public void OnThemeChanged()
    {
        if (StudioContentFrame.Content is IThemeAwareView child)
        {
            child.OnThemeChanged();
        }
    }

    private void UpdateLocalizedLabels()
    {
        TabVocabulary.Text = _localizationService.GetString("Nav_Vocabulary");
        TabKnowledgeGraph.Text = _localizationService.GetString("Nav_KnowledgeGraph");
        TabHomophones.Text = _localizationService.GetString("Nav_Homophones");
        TabGrammar.Text = _localizationService.GetString("Nav_Grammar");
        TabPhrases.Text = _localizationService.GetString("Nav_Phrases");
        TabMemoryTips.Text = _localizationService.GetString("Nav_MemoryTips");
    }
}
