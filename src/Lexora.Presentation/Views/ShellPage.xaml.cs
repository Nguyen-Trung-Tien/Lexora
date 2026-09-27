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

        if (App.Current.Services.GetService<IShortcutManager>() is ShortcutManager sm)
        {
            sm.GlobalSearchRequested += OnGlobalSearchRequested;
        }

        Unloaded += (s, e) =>
        {
            _localizationService.LanguageChanged -= OnLanguageChanged;
            if (App.Current.Services.GetService<IShortcutManager>() is ShortcutManager smClean)
            {
                smClean.GlobalSearchRequested -= OnGlobalSearchRequested;
            }
            ViewModel.Dispose();
        };
    }

    private void OnGlobalSearchRequested(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            GlobalSearchBox.Focus(FocusState.Programmatic);
        });
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

    private bool _isNavInitialized;

    private async void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_isNavInitialized)
            {
                _isNavInitialized = true;
                _navigationService.Initialize(ContentFrame);

                NavView.BackRequested += (s, args) =>
                {
                    if (_navigationService.CanGoBack)
                    {
                        _navigationService.GoBack();
                    }
                };

                ContentFrame.Navigated += (s, args) =>
                {
                    NavView.IsBackEnabled = _navigationService.CanGoBack;
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

                _navigationService.Navigate(typeof(DashboardPage));
            }

            UpdateNavigationLabels();
            await ViewModel.CheckDueReviewsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellPage] NavView_Loaded error: {ex.Message}");
        }
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

    private CancellationTokenSource? _searchCts;

    private async void GlobalSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        var query = sender.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            sender.ItemsSource = null;
            return;
        }

        try
        {
            // Debounce delay to prevent flooding SQLite/EF Core on every keystroke
            await Task.Delay(250, ct);
        }
        catch (OperationCanceledException)
        {
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
            var matchingWords = await vocabService.SearchWordsAsync(query, pageSize: 5, cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

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
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            // Suppress search preview errors
        }

        if (!ct.IsCancellationRequested)
        {
            sender.ItemsSource = suggestions;
        }
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
