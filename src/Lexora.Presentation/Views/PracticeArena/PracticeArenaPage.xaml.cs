using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Lexora.Presentation.Views.Review;
using Lexora.Presentation.Views.Test;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views.PracticeArena;

public sealed partial class PracticeArenaPage : Page, ILocalizableView, IThemeAwareView
{
    private readonly ILocalizationService _localizationService;
    private string _activeTab = "Review";
    private int _previousTabIndex = 0;
    private object? _pendingParam;

    public PracticeArenaPage()
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

        if (e.Parameter != null)
        {
            SelectTab("Review", e.Parameter);
            return;
        }

        if (ArenaContentFrame.Content == null)
        {
            SelectTab("Review");
        }
        else if (ArenaContentFrame.CurrentSourcePageType is { } activePageType)
        {
            App.Current.Services.GetService<IShortcutManager>()?.SetCurrentPage(activePageType);
        }
    }

    public void SelectTab(string tabTag, object? parameter = null)
    {
        _activeTab = tabTag;
        _pendingParam = parameter;

        var targetItem = string.Equals(tabTag, "Test", StringComparison.OrdinalIgnoreCase)
            ? TabTest
            : TabReview;

        if (!ReferenceEquals(ArenaSelectorBar.SelectedItem, targetItem))
        {
            ArenaSelectorBar.SelectedItem = targetItem;
        }
        else
        {
            NavigateToTab(tabTag, parameter);
        }
    }

    private void ArenaSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
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
        bool isTest = string.Equals(tag, "Test", StringComparison.OrdinalIgnoreCase);
        Type targetPage = isTest ? typeof(TestPage) : typeof(ReviewPage);
        int newIndex = isTest ? 1 : 0;

        if (ArenaContentFrame.CurrentSourcePageType == targetPage && parameter == null)
        {
            App.Current.Services.GetService<IShortcutManager>()?.SetCurrentPage(targetPage);
            return;
        }

        var effect = newIndex >= _previousTabIndex
            ? SlideNavigationTransitionEffect.FromRight
            : SlideNavigationTransitionEffect.FromLeft;
        _previousTabIndex = newIndex;

        ArenaContentFrame.Navigate(targetPage, parameter, new SlideNavigationTransitionInfo { Effect = effect });
        App.Current.Services.GetService<IShortcutManager>()?.SetCurrentPage(targetPage);
    }

    private async void OpenListeningLab_Click(object sender, RoutedEventArgs e)
    {
        using var scope = App.Current.Services.CreateScope();
        var labService = scope.ServiceProvider.GetRequiredService<IPronunciationListeningLabService>();
        var audioService = scope.ServiceProvider.GetService<IAudioPlayerService>();
        var dialog = new ListeningLabDialog(labService, _localizationService, audioService);
        await dialog.ShowAsyncSafe(this.XamlRoot);
    }

    public void OnLanguageChanged()
    {
        UpdateLocalizedLabels();
        if (ArenaContentFrame.Content is ILocalizableView child)
        {
            child.OnLanguageChanged();
        }
    }

    public void OnThemeChanged()
    {
        if (ArenaContentFrame.Content is IThemeAwareView child)
        {
            child.OnThemeChanged();
        }
    }

    private void UpdateLocalizedLabels()
    {
        TabReview.Text = _localizationService.GetString("Nav_Review");
        TabTest.Text = _localizationService.GetString("Nav_Test");
        ListeningLabLabel.Text = _localizationService.GetString("Dashboard_ListeningLabAction");
    }
}
