using System.Collections.ObjectModel;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Lexora.Presentation.Views.Dashboard;
using Lexora.Presentation.Views.Grammar;
using Lexora.Presentation.Views.Help;
using Lexora.Presentation.Views.Homophones;
using Lexora.Presentation.Views.KnowledgeGraph;
using Lexora.Presentation.Views.LearningStorage;
using Lexora.Presentation.Views.Review;
using Lexora.Presentation.Views.Roadmap;
using Lexora.Presentation.Views.Settings;
using Lexora.Presentation.Views.Test;
using Lexora.Presentation.Views.Vocabulary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Lexora.Presentation.Views.Common;

public class CommandPaletteDisplayItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Glyph { get; set; } = string.Empty;
    public string? Badge { get; set; }
    public Action? ExecuteAction { get; set; }

    public bool HasBadge => !string.IsNullOrWhiteSpace(Badge);

    public CommandPaletteDisplayItem() { }

    public CommandPaletteDisplayItem(string title, string subtitle, string glyph, string? badge = null, Action? executeAction = null)
    {
        Title = title;
        Subtitle = subtitle;
        Glyph = glyph;
        Badge = badge;
        ExecuteAction = executeAction;
    }
}

public sealed partial class CommandPaletteOverlayControl : UserControl
{
    private readonly ObservableCollection<CommandPaletteDisplayItem> _items = new();
    private CancellationTokenSource? _searchCts;

    public bool IsOpen => Visibility == Visibility.Visible;

    public CommandPaletteOverlayControl()
    {
        InitializeComponent();
        ItemsListView.ItemsSource = _items;
        Loaded += (s, e) =>
        {
            UpdateLocalizedTexts();
        };
    }

    public void UpdateLocalizedTexts()
    {
        var loc = App.Current.Services.GetService<ILocalizationService>();
        if (loc == null) return;

        SearchBox.PlaceholderText = loc.GetString("Palette_Placeholder");
        FooterHintText.Text = loc.GetString("Palette_FooterHint");
        NoResultsText.Text = loc.GetString("Palette_NoResults");
    }

    public void Show()
    {
        UpdateLocalizedTexts();
        Visibility = Visibility.Visible;
        SearchBox.Text = string.Empty;
        PopulateDefaultCommands();

        DispatcherQueue.TryEnqueue(() =>
        {
            SearchBox.Focus(FocusState.Programmatic);
            if (_items.Count > 0)
            {
                ItemsListView.SelectedIndex = 0;
            }
        });
    }

    public void Hide()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;
        Visibility = Visibility.Collapsed;
    }

    private List<CommandPaletteDisplayItem> BuildDefaultCommands(
        ILocalizationService? loc,
        INavigationService? nav,
        IThemeService? theme = null)
    {
        return new List<CommandPaletteDisplayItem>
        {
            new(loc?.GetString("Nav_Dashboard") ?? "Dashboard", loc?.GetString("Search_SmartNav_Dashboard") ?? "", "\uE80F", "Ctrl+1", () => nav?.Navigate(typeof(DashboardPage))),
            new(loc?.GetString("Nav_Vocabulary") ?? "Vocabulary", loc?.GetString("Search_SmartNav_Vocab") ?? "", "\uE82D", "Ctrl+2", () => nav?.Navigate(typeof(VocabularyPage))),
            new(loc?.GetString("Nav_Homophones") ?? "Homophones", loc?.GetString("Search_SmartNav_Homophones") ?? "", "\uE768", "Ctrl+3", () => nav?.Navigate(typeof(HomophonesPage))),
            new(loc?.GetString("Nav_KnowledgeGraph") ?? "Knowledge Graph", loc?.GetString("Nav_KnowledgeGraph") ?? "", "\uE81E", "Ctrl+4", () => nav?.Navigate(typeof(KnowledgeGraphPage))),
            new(loc?.GetString("Nav_Grammar") ?? "Grammar", loc?.GetString("Search_SmartNav_Grammar") ?? "", "\uE8D2", "Ctrl+5", () => nav?.Navigate(typeof(GrammarPage))),
            new(loc?.GetString("Nav_Phrases") ?? "Conversation Phrases", loc?.GetString("Phrases_Subtitle") ?? "", "\uE8BD", "Ctrl+Shift+P", () => nav?.Navigate(typeof(Lexora.Presentation.Views.Phrases.PhrasesPage))),
            new(loc?.GetString("Nav_MemoryTips") ?? "Memory Tips", loc?.GetString("MemoryTips_Subtitle") ?? "", "\uE8D7", "Ctrl+Shift+M", () => nav?.Navigate(typeof(Lexora.Presentation.Views.MemoryTips.MemoryTipsPage))),
            new(loc?.GetString("Nav_Review") ?? "Review", loc?.GetString("Search_SmartNav_Review") ?? "", "\uE895", "Ctrl+6", () => nav?.Navigate(typeof(ReviewPage))),
            new(loc?.GetString("Nav_Test") ?? "Test", loc?.GetString("Search_SmartNav_Test") ?? "", "\uE765", "Ctrl+7", () => nav?.Navigate(typeof(TestPage))),
            new(loc?.GetString("Nav_LearningStorage") ?? "Learning Storage", loc?.GetString("Search_SmartNav_LearningStorage") ?? "", "\uE81C", "Ctrl+8", () => nav?.Navigate(typeof(LearningStoragePage))),
            new(loc?.GetString("Nav_Roadmap") ?? "Roadmap", loc?.GetString("Nav_Roadmap") ?? "", "\uE81C", "Ctrl+9", () => nav?.Navigate(typeof(RoadmapPage))),
            new(loc?.GetString("Dashboard_StartReview") ?? "Start Review", loc?.GetString("Search_SmartNav_Review") ?? "", "\uE895", "Ctrl+Shift+R", () => nav?.Navigate(typeof(ReviewPage))),
            new(loc?.GetString("Palette_DictationLab") ?? "Listening Lab", loc?.GetString("Nav_ListeningLab") ?? "", "\uE767", "Ctrl+Shift+D", OpenDictationLabDialog),
            new(loc?.GetString("Nav_Settings") ?? "Settings", loc?.GetString("Search_SmartNav_Settings") ?? "", "\uE713", null, () => nav?.Navigate(typeof(SettingsPage))),
            new(loc?.GetString("Nav_Help") ?? "Help", loc?.GetString("Nav_Help") ?? "", "\uE9CE", "F1", () => nav?.Navigate(typeof(HelpPage))),
            new(loc?.GetString("Nav_ThemeToggle") ?? "Toggle Theme", loc?.GetString("Nav_ThemeToggle") ?? "", "\uE708", "Ctrl+T", () => theme?.ToggleTheme())
        };
    }

    private void PopulateDefaultCommands()
    {
        _items.Clear();
        var loc = App.Current.Services.GetService<ILocalizationService>();
        var nav = App.Current.Services.GetService<INavigationService>();
        var theme = App.Current.Services.GetService<IThemeService>();

        if (loc == null || nav == null) return;

        foreach (var cmd in BuildDefaultCommands(loc, nav, theme))
        {
            _items.Add(cmd);
        }

        UpdateEmptyState();
    }

    public async void OpenDictationLabDialog()
    {
        try
        {
            using var scope = App.Current.Services.CreateScope();
            var labService = scope.ServiceProvider.GetRequiredService<IPronunciationListeningLabService>();
            var loc = App.Current.Services.GetRequiredService<ILocalizationService>();
            var audioService = App.Current.Services.GetService<IAudioPlayerService>();

            var dialog = new ListeningLabDialog(labService, loc, audioService)
            {
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsyncSafe(this.XamlRoot);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandPalette] OpenDictationLabDialog error: {ex.Message}");
        }
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        var query = SearchBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            PopulateDefaultCommands();
            return;
        }

        try
        {
            // Debounce 200ms
            await Task.Delay(200, ct);
            if (ct.IsCancellationRequested) return;

            var loc = App.Current.Services.GetService<ILocalizationService>();
            var nav = App.Current.Services.GetService<INavigationService>();

            var theme = App.Current.Services.GetService<IThemeService>();

            var results = new List<CommandPaletteDisplayItem>();

            // 1. Filter default commands
            var allCommands = BuildDefaultCommands(loc, nav, theme);

            foreach (var cmd in allCommands)
            {
                if (cmd.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    cmd.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(cmd);
                }
            }

            // 2. Query Vocabulary (up to 8 matches)
            using (var scope = App.Current.Services.CreateScope())
            {
                var vocabService = scope.ServiceProvider.GetRequiredService<IVocabularyService>();
                var words = await vocabService.SearchWordsAsync(query, pageSize: 8, cancellationToken: ct);

                if (ct.IsCancellationRequested) return;

                foreach (var w in words)
                {
                    var pos = !string.IsNullOrWhiteSpace(w.PartsOfSpeechDisplay) ? w.PartsOfSpeechDisplay : w.PrimaryPartOfSpeech.ToString().ToLowerInvariant();
                    var sub = string.IsNullOrWhiteSpace(w.PrimaryDefinition) ? pos : $"{pos} • {w.PrimaryDefinition}";
                    var badge = w.Level != Domain.Enums.CefrLevel.None ? w.Level.ToString() : null;

                    results.Add(new CommandPaletteDisplayItem(
                        w.Term,
                        sub,
                        "\uE8BD",
                        badge,
                        () => nav?.Navigate(typeof(VocabularyPage), w.Term)
                    ));
                }

                // 3. Query Grammar (up to 3 matches)
                var grammarService = scope.ServiceProvider.GetRequiredService<IGrammarService>();
                var grammarRules = await grammarService.SearchRulesAsync(query, cancellationToken: ct);

                if (ct.IsCancellationRequested) return;

                foreach (var rule in grammarRules.Take(3))
                {
                    var prefix = loc?.GetString("Search_GrammarRule_Prefix") ?? "[Grammar]";
                    var sub = !string.IsNullOrWhiteSpace(rule.Structure) ? $"{prefix} • {rule.Structure}" : prefix;

                    results.Add(new CommandPaletteDisplayItem(
                        rule.Title,
                        sub,
                        "\uE8D2",
                        rule.Level.ToString(),
                        () => nav?.Navigate(typeof(GrammarPage), rule.Title)
                    ));
                }
            }

            if (!ct.IsCancellationRequested)
            {
                _items.Clear();
                foreach (var item in results)
                {
                    _items.Add(item);
                }

                if (_items.Count > 0)
                {
                    ItemsListView.SelectedIndex = 0;
                }
                UpdateEmptyState();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected debounce cancellation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandPalette] Search error: {ex.Message}");
        }
    }

    private void UpdateEmptyState()
    {
        NoResultsPanel.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ItemsListView.Visibility = _items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            Hide();
        }
        else if (e.Key == VirtualKey.Down)
        {
            e.Handled = true;
            if (_items.Count > 0)
            {
                int nextIndex = (ItemsListView.SelectedIndex + 1) % _items.Count;
                ItemsListView.SelectedIndex = nextIndex;
                ItemsListView.ScrollIntoView(ItemsListView.SelectedItem);
            }
        }
        else if (e.Key == VirtualKey.Up)
        {
            e.Handled = true;
            if (_items.Count > 0)
            {
                int prevIndex = ItemsListView.SelectedIndex <= 0 ? _items.Count - 1 : ItemsListView.SelectedIndex - 1;
                ItemsListView.SelectedIndex = prevIndex;
                ItemsListView.ScrollIntoView(ItemsListView.SelectedItem);
            }
        }
        else if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            ExecuteCurrentSelection();
        }
    }

    private void ItemsListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            Hide();
        }
        else if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            ExecuteCurrentSelection();
        }
    }

    private void ItemsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CommandPaletteDisplayItem item)
        {
            Hide();
            item.ExecuteAction?.Invoke();
        }
    }

    private void ExecuteCurrentSelection()
    {
        if (ItemsListView.SelectedItem is CommandPaletteDisplayItem selected)
        {
            Hide();
            selected.ExecuteAction?.Invoke();
        }
    }

    private void RootOverlay_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Hide();
    }

    private void PaletteCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }
}
