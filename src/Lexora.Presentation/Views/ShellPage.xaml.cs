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
        ViewModel = App.Current.Services.GetRequiredService<ShellViewModel>();
        _navigationService = App.Current.Services.GetRequiredService<INavigationService>();
        _localizationService = App.Current.Services.GetRequiredService<ILocalizationService>();

        InitializeComponent();

        // Ensure navigation service is bound to ContentFrame immediately
        _navigationService.Initialize(ContentFrame);

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
        void UpdateUI()
        {
            UpdateNavigationLabels();
            ViewModel.UpdateLocalizedTexts();
            Bindings.Update();
            if (ContentFrame.Content is ILocalizableView localizableView)
            {
                localizableView.OnLanguageChanged();
            }
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

                    if (_navigationService.HasPendingNavigation)
                    {
                        return;
                    }

                    NavigationViewItem? targetItem = ContentFrame.Content switch
                    {
                        DashboardPage => NavItemDashboard,
                        VocabularyPage => NavItemVocabulary,
                        ReviewPage => NavItemReview,
                        TestPage => NavItemTest,
                        GrammarPage => NavItemGrammar,
                        LearningStoragePage => NavItemLearningStorage,
                        SettingsPage => NavItemSettings,
                        _ => null
                    };

                    if (targetItem != null && !ReferenceEquals(NavView.SelectedItem, targetItem))
                    {
                        NavView.SelectedItem = targetItem;
                    }
                };

                _navigationService.Navigate(typeof(DashboardPage));
            }

            UpdateNavigationLabels();
            await ViewModel.CheckDueReviewsAsync();

            _ = Task.Run(async () =>
            {
                await Task.Delay(2500);
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await ViewModel.CheckForUpdatesAsync();
                });
            });
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
            if (_navigationService.CurrentPageType == typeof(SettingsPage))
            {
                return;
            }
            _navigationService.Navigate(typeof(SettingsPage));
            return;
        }

        var item = args.InvokedItemContainer as NavigationViewItem
            ?? sender.MenuItems.OfType<NavigationViewItem>()
                .Concat(sender.FooterMenuItems.OfType<NavigationViewItem>())
                .FirstOrDefault(i =>
                    Equals(i.Content, args.InvokedItem) ||
                    Equals(i.Tag, args.InvokedItem) ||
                    string.Equals(i.Content?.ToString(), args.InvokedItem?.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(i.Tag?.ToString(), args.InvokedItem?.ToString(), StringComparison.OrdinalIgnoreCase));

        if (item != null)
        {
            NavigateToItem(item);
        }
    }

    private void NavigateToItem(NavigationViewItem item)
    {
        var tag = item.Tag?.ToString();
        var targetType = tag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "Vocabulary" => typeof(VocabularyPage),
            "Review" => typeof(ReviewPage),
            "Test" => typeof(TestPage),
            "Grammar" => typeof(GrammarPage),
            "LearningStorage" => typeof(LearningStoragePage),
            "Settings" => typeof(SettingsPage),
            _ => null
        };

        if (targetType != null)
        {
            if (_navigationService.CurrentPageType == targetType)
            {
                return;
            }
            _navigationService.Navigate(targetType);
        }
    }

    private void UpdateNavigationLabels()
    {
        NavItemDashboard.Content = _localizationService.GetString("Nav_Dashboard");
        NavItemVocabulary.Content = _localizationService.GetString("Nav_Vocabulary");
        NavItemReview.Content = _localizationService.GetString("Nav_Review");
        NavItemTest.Content = _localizationService.GetString("Nav_Test");
        NavItemGrammar.Content = _localizationService.GetString("Nav_Grammar");
        NavItemLearningStorage.Content = _localizationService.GetString("Nav_LearningStorage");
        NavItemSettings.Content = _localizationService.GetString("Nav_Settings");
        GlobalSearchBox.PlaceholderText = _localizationService.GetString("Search_Placeholder");

        ToolTipService.SetToolTip(NavItemDashboard, _localizationService.GetString("Shell_TooltipDashboard"));
        ToolTipService.SetToolTip(NavItemVocabulary, _localizationService.GetString("Shell_TooltipVocabulary"));
        ToolTipService.SetToolTip(NavItemReview, _localizationService.GetString("Shell_TooltipReview"));
        ToolTipService.SetToolTip(NavItemTest, _localizationService.GetString("Shell_TooltipTest"));
        ToolTipService.SetToolTip(NavItemGrammar, _localizationService.GetString("Shell_TooltipGrammar"));
        ToolTipService.SetToolTip(NavItemLearningStorage, _localizationService.GetString("Shell_TooltipLearningStorage"));
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
            (Key: "storage", Page: "LearningStorage", Title: _localizationService.GetString("Nav_LearningStorage"), Subtitle: _localizationService.GetString("Search_SmartNav_LearningStorage"), Glyph: "\uE7B8"),
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

        // 2. Vocabulary search matches (up to 8 words)
        try
        {
            using var scope = App.Current.Services.CreateScope();
            var vocabService = scope.ServiceProvider.GetRequiredService<Lexora.Application.Interfaces.IVocabularyService>();
            var matchingWords = await vocabService.SearchWordsAsync(query, pageSize: 8, cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

            foreach (var word in matchingWords)
            {
                var posLabel = !string.IsNullOrWhiteSpace(word.PartsOfSpeechDisplay)
                    ? word.PartsOfSpeechDisplay
                    : LocalizationHelper.FormatPartOfSpeech(word.PrimaryPartOfSpeech);

                suggestions.Add(new SmartSearchSuggestion
                {
                    Title = word.Term,
                    Subtitle = string.IsNullOrWhiteSpace(word.PrimaryDefinition) ? posLabel : $"{posLabel} • {word.PrimaryDefinition}",
                    Glyph = "\uE8BD",
                    SearchTerm = word.Term,
                    TargetType = "Word"
                });
            }

            // 3. Grammar search matches (up to 4 rules)
            var grammarService = scope.ServiceProvider.GetRequiredService<Lexora.Application.Interfaces.IGrammarService>();
            var matchingRules = await grammarService.SearchRulesAsync(query, cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

            var prefix = _localizationService.GetString("Search_GrammarRule_Prefix");
            foreach (var rule in matchingRules.Take(4))
            {
                var sub = !string.IsNullOrWhiteSpace(rule.Structure)
                    ? $"{prefix} • {rule.Structure}"
                    : (!string.IsNullOrWhiteSpace(rule.ExplanationVi) ? $"{prefix} • {rule.ExplanationVi}" : prefix);

                suggestions.Add(new SmartSearchSuggestion
                {
                    Title = rule.Title,
                    Subtitle = sub,
                    Glyph = "\uE8D2",
                    SearchTerm = rule.Title,
                    TargetType = "Grammar"
                });
            }

            // 4. Closest suggestions fallback if no direct matches found
            if (suggestions.Count == 0 && query.Length >= 2)
            {
                var smartWords = await vocabService.SmartSearchWordsAsync(search: query, pageSize: 1, cancellationToken: ct);
                var closestLabel = _localizationService.GetString("Search_ClosestSuggestions");
                foreach (var s in smartWords.ClosestSuggestions.Take(3))
                {
                    suggestions.Add(new SmartSearchSuggestion
                    {
                        Title = s,
                        Subtitle = closestLabel,
                        Glyph = "\uE721",
                        SearchTerm = s,
                        TargetType = "Word"
                    });
                }

                if (suggestions.Count == 0)
                {
                    var smartGrammar = await grammarService.SmartSearchRulesAsync(search: query, pageSize: 1, cancellationToken: ct);
                    foreach (var s in smartGrammar.ClosestSuggestions.Take(3))
                    {
                        suggestions.Add(new SmartSearchSuggestion
                        {
                            Title = s,
                            Subtitle = $"{closestLabel} • {prefix}",
                            Glyph = "\uE8D2",
                            SearchTerm = s,
                            TargetType = "Grammar"
                        });
                    }
                }
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
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!ct.IsCancellationRequested)
                {
                    sender.ItemsSource = suggestions;
                }
            });
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

            if (chosen.TargetType == "Grammar")
            {
                _navigationService.Navigate(typeof(GrammarPage), chosen.SearchTerm ?? chosen.Title);
                NavView.SelectedItem = NavItemGrammar;
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
