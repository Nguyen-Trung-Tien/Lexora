using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;

    public ShellPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<ShellViewModel>();
        _navigationService = App.Current.Services.GetRequiredService<INavigationService>();
        _localizationService = App.Current.Services.GetRequiredService<ILocalizationService>();

        UpdateNavigationLabels();
        _localizationService.LanguageChanged += OnLanguageChanged;
        Unloaded += (s, e) => _localizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateNavigationLabels();
    }

    private async void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        _navigationService.Initialize(ContentFrame);
        ContentFrame.Navigated += (s, args) =>
        {
            ViewModel.IsOnReviewPage = ContentFrame.Content is ReviewPage or TestPage;
        };
        _navigationService.Navigate(typeof(DashboardPage));
        NavView.SelectedItem = NavItemDashboard;
        await ViewModel.CheckDueReviewsAsync();
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            _navigationService.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            switch (tag)
            {
                case "Dashboard":
                    _navigationService.Navigate(typeof(DashboardPage));
                    break;
                case "Vocabulary":
                    _navigationService.Navigate(typeof(VocabularyPage));
                    break;
                case "Review":
                    _navigationService.Navigate(typeof(ReviewPage));
                    break;
                case "Test":
                    _navigationService.Navigate(typeof(TestPage));
                    break;
                case "Grammar":
                    _navigationService.Navigate(typeof(GrammarPage));
                    break;
            }
        }
    }

    private void UpdateNavigationLabels()
    {
        NavItemDashboard.Content = _localizationService.GetString("Nav_Dashboard");
        NavItemVocabulary.Content = _localizationService.GetString("Nav_Vocabulary");
        NavItemReview.Content = _localizationService.GetString("Nav_Review");
        NavItemTest.Content = _localizationService.GetString("Nav_Test");
        NavItemGrammar.Content = _localizationService.GetString("Nav_Grammar");
        GlobalSearchBox.PlaceholderText = _localizationService.GetString("Search_Placeholder");
    }

    private void GlobalSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _navigationService.Navigate(typeof(VocabularyPage), args.QueryText);
        NavView.SelectedItem = NavItemVocabulary;
    }

    private void ReviewNowButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.Navigate(typeof(ReviewPage));
        NavView.SelectedItem = NavItemReview;
    }

    private void NavigateAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        switch (sender.Key)
        {
            case Windows.System.VirtualKey.Number1:
                _navigationService.Navigate(typeof(DashboardPage));
                NavView.SelectedItem = NavItemDashboard;
                args.Handled = true;
                break;
            case Windows.System.VirtualKey.Number2:
                _navigationService.Navigate(typeof(VocabularyPage));
                NavView.SelectedItem = NavItemVocabulary;
                args.Handled = true;
                break;
            case Windows.System.VirtualKey.Number3:
                _navigationService.Navigate(typeof(ReviewPage));
                NavView.SelectedItem = NavItemReview;
                args.Handled = true;
                break;
            case Windows.System.VirtualKey.Number4:
                _navigationService.Navigate(typeof(TestPage));
                NavView.SelectedItem = NavItemTest;
                args.Handled = true;
                break;
            case Windows.System.VirtualKey.Number5:
                _navigationService.Navigate(typeof(GrammarPage));
                NavView.SelectedItem = NavItemGrammar;
                args.Handled = true;
                break;
        }
    }

    private void SearchAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        GlobalSearchBox.Focus(FocusState.Programmatic);
        args.Handled = true;
    }
}
