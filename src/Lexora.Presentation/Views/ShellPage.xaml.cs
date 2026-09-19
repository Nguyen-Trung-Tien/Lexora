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
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateNavigationLabels();
            ViewModel.UpdateLocalizedTexts();
            Bindings.Update();
        });
    }

    private async void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        _navigationService.Initialize(ContentFrame);
        ContentFrame.Navigated += (s, args) =>
        {
            switch (ContentFrame.Content)
            {
                case DashboardPage:
                    NavView.SelectedItem = NavItemDashboard;
                    break;
                case VocabularyPage:
                    NavView.SelectedItem = NavItemVocabulary;
                    break;
                case ReviewPage:
                    NavView.SelectedItem = NavItemReview;
                    break;
                case TestPage:
                    NavView.SelectedItem = NavItemTest;
                    break;
                case GrammarPage:
                    NavView.SelectedItem = NavItemGrammar;
                    break;
                case SettingsPage:
                    NavView.SelectedItem = NavItemSettings;
                    break;
            }
        };
        UpdateNavigationLabels();
        _navigationService.Navigate(typeof(DashboardPage));
        await ViewModel.CheckDueReviewsAsync();
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked || args.InvokedItemContainer == NavItemSettings)
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
                case "Settings":
                    _navigationService.Navigate(typeof(SettingsPage));
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
        NavItemSettings.Content = _localizationService.GetString("Nav_Settings");
        GlobalSearchBox.PlaceholderText = _localizationService.GetString("Search_Placeholder");

        ToolTipService.SetToolTip(NavItemDashboard, _localizationService.GetString("Shell_TooltipDashboard"));
        ToolTipService.SetToolTip(NavItemVocabulary, _localizationService.GetString("Shell_TooltipVocabulary"));
        ToolTipService.SetToolTip(NavItemReview, _localizationService.GetString("Shell_TooltipReview"));
        ToolTipService.SetToolTip(NavItemTest, _localizationService.GetString("Shell_TooltipTest"));
        ToolTipService.SetToolTip(NavItemGrammar, _localizationService.GetString("Shell_TooltipGrammar"));
        ToolTipService.SetToolTip(NavItemSettings, _localizationService.GetString("Shell_TooltipSettings"));
        ToolTipService.SetToolTip(GlobalSearchBox, _localizationService.GetString("Shell_SearchTooltip"));
    }

    private async void GlobalSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        var query = sender.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            sender.ItemsSource = null;
            return;
        }

        var suggestions = new List<SmartSearchSuggestion>();
        var lower = query.ToLowerInvariant();

        // 1. Quick Navigation Targets matching
        var navTargets = new[]
        {
            (Key: "dashboard", Page: "Dashboard", Title: _localizationService.GetString("Nav_Dashboard"), Subtitle: _localizationService.GetString("Search_SmartNav_Dashboard"), Glyph: "\uE80F"),
            (Key: "vocabulary", Page: "Vocabulary", Title: _localizationService.GetString("Nav_Vocabulary"), Subtitle: _localizationService.GetString("Search_SmartNav_Vocab"), Glyph: "\uE82D"),
            (Key: "review", Page: "Review", Title: _localizationService.GetString("Nav_Review"), Subtitle: _localizationService.GetString("Search_SmartNav_Review"), Glyph: "\uE895"),
            (Key: "test", Page: "Test", Title: _localizationService.GetString("Nav_Test"), Subtitle: _localizationService.GetString("Search_SmartNav_Test"), Glyph: "\uE765"),
            (Key: "grammar", Page: "Grammar", Title: _localizationService.GetString("Nav_Grammar"), Subtitle: _localizationService.GetString("Search_SmartNav_Grammar"), Glyph: "\uE8D2"),
            (Key: "settings", Page: "Settings", Title: _localizationService.GetString("Nav_Settings"), Subtitle: _localizationService.GetString("Search_SmartNav_Settings"), Glyph: "\uE713")
        };

        foreach (var nav in navTargets)
        {
            if (nav.Key.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
                nav.Title.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
                nav.Subtitle.Contains(lower, StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(new SmartSearchSuggestion
                {
                    Title = nav.Title,
                    Subtitle = nav.Subtitle,
                    Glyph = nav.Glyph,
                    TargetPage = nav.Page
                });
            }
        }

        // 2. Vocabulary search matches (up to 5 words)
        try
        {
            using var scope = App.Current.Services.CreateScope();
            var vocabService = scope.ServiceProvider.GetRequiredService<Lexora.Application.Interfaces.IVocabularyService>();
            var matchingWords = await vocabService.SearchWordsAsync(query, pageSize: 5);

            foreach (var word in matchingWords)
            {
                suggestions.Add(new SmartSearchSuggestion
                {
                    Title = word.Term,
                    Subtitle = string.IsNullOrWhiteSpace(word.PrimaryDefinition) ? word.PrimaryPartOfSpeech.ToString() : $"{word.PrimaryPartOfSpeech} • {word.PrimaryDefinition}",
                    Glyph = "\uE8BD",
                    SearchTerm = word.Term
                });
            }
        }
        catch
        {
            // Suppress search preview errors
        }

        sender.ItemsSource = suggestions;
    }

    private void GlobalSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SmartSearchSuggestion item)
        {
            sender.Text = item.SearchTerm ?? item.Title;
        }
    }

    private void GlobalSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is SmartSearchSuggestion chosen)
        {
            if (!string.IsNullOrEmpty(chosen.TargetPage))
            {
                switch (chosen.TargetPage)
                {
                    case "Dashboard":
                        _navigationService.Navigate(typeof(DashboardPage));
                        NavView.SelectedItem = NavItemDashboard;
                        break;
                    case "Vocabulary":
                        _navigationService.Navigate(typeof(VocabularyPage));
                        NavView.SelectedItem = NavItemVocabulary;
                        break;
                    case "Review":
                        _navigationService.Navigate(typeof(ReviewPage));
                        NavView.SelectedItem = NavItemReview;
                        break;
                    case "Test":
                        _navigationService.Navigate(typeof(TestPage));
                        NavView.SelectedItem = NavItemTest;
                        break;
                    case "Grammar":
                        _navigationService.Navigate(typeof(GrammarPage));
                        NavView.SelectedItem = NavItemGrammar;
                        break;
                    case "Settings":
                        _navigationService.Navigate(typeof(SettingsPage));
                        NavView.SelectedItem = NavItemSettings;
                        break;
                }
                return;
            }

            if (!string.IsNullOrEmpty(chosen.SearchTerm))
            {
                _navigationService.Navigate(typeof(VocabularyPage), chosen.SearchTerm);
                NavView.SelectedItem = NavItemVocabulary;
                return;
            }
        }

        var query = args.QueryText?.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            _navigationService.Navigate(typeof(VocabularyPage), query);
            NavView.SelectedItem = NavItemVocabulary;
        }
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

public sealed class SmartSearchSuggestion
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Glyph { get; init; }
    public string? TargetPage { get; init; }
    public string? SearchTerm { get; init; }

    public override string ToString() => Title;
}
