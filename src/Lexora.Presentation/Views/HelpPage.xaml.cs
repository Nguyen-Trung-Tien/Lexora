using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;

namespace Lexora.Presentation.Views;

public sealed partial class HelpPage : Page, ILocalizableView, IThemeAwareView
{
    public HelpViewModel ViewModel { get; }
    public ILocalizationService Loc { get; }

    private readonly INavigationService _navigationService;

    private readonly System.ComponentModel.PropertyChangedEventHandler _propChangedHandler;
    private readonly TypedEventHandler<FrameworkElement, object> _themeChangedHandler;

    public HelpPage()
    {
        NavigationCacheMode = NavigationCacheMode.Required;
        ViewModel = App.Current.Services.GetRequiredService<HelpViewModel>();
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        _navigationService = App.Current.Services.GetRequiredService<INavigationService>();

        InitializeComponent();

        _themeChangedHandler = (s, e) => OnThemeChanged();
        ActualThemeChanged += _themeChangedHandler;

        _propChangedHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.SelectedCategory))
            {
                DispatcherQueue.TryEnqueue(UpdateFilterPillsState);
            }
        };
        ViewModel.PropertyChanged += _propChangedHandler;

        Unloaded += (s, e) =>
        {
            ActualThemeChanged -= _themeChangedHandler;
            ViewModel.PropertyChanged -= _propChangedHandler;
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string targetCategory && !string.IsNullOrWhiteSpace(targetCategory))
        {
            ViewModel.SelectCategory(targetCategory);
            UpdateFilterPillsState();
        }
        else
        {
            UpdateFilterPillsState();
        }
    }

    private void HelpPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateFilterPillsState();
    }

    public void OnLanguageChanged()
    {
        ViewModel.RebuildDisplayItems();
        ViewModel.ApplyFilter();
        UpdateFilterPillsState();
    }

    public void OnThemeChanged()
    {
        UpdateFilterPillsState();
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string category)
        {
            ViewModel.SelectCategory(category);
            UpdateFilterPillsState();
        }
    }

    private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetFilter();
        UpdateFilterPillsState();
    }

    private void GuideActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string targetTag)
        {
            ViewModel.NavigateToTarget(targetTag);
        }
    }

    private void UpdateFilterPillsState()
    {
        var activeCategory = ViewModel.SelectedCategory;

        SetPillState(FilterBtnAll, string.Equals(activeCategory, "All", StringComparison.OrdinalIgnoreCase));
        SetPillState(FilterBtnGettingStarted, string.Equals(activeCategory, "GettingStarted", StringComparison.OrdinalIgnoreCase));
        SetPillState(FilterBtnVocabulary, string.Equals(activeCategory, "Vocabulary", StringComparison.OrdinalIgnoreCase));
        SetPillState(FilterBtnSpacedRepetition, string.Equals(activeCategory, "SpacedRepetition", StringComparison.OrdinalIgnoreCase));
        SetPillState(FilterBtnListeningLab, string.Equals(activeCategory, "ListeningLab", StringComparison.OrdinalIgnoreCase));
        SetPillState(FilterBtnTestMode, string.Equals(activeCategory, "TestMode", StringComparison.OrdinalIgnoreCase));
        SetPillState(FilterBtnBackupSettings, string.Equals(activeCategory, "BackupAndSettings", StringComparison.OrdinalIgnoreCase));
    }

    private static void SetPillState(Button button, bool isActive)
    {
        if (isActive)
        {
            button.Style = App.Current.Resources.TryGetValue("AccentButtonStyle", out var accentStyle)
                ? (Style)accentStyle
                : null;
        }
        else
        {
            button.Style = App.Current.Resources.TryGetValue("DefaultButtonStyle", out var defaultStyle)
                ? (Style)defaultStyle
                : null;
        }
    }
}
