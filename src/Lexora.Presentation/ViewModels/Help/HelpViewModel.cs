using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Presentation.Services;
using Lexora.Presentation.Views;

namespace Lexora.Presentation.ViewModels;

public sealed record HelpGuideDisplayItem(
    string Id,
    string Category,
    string Title,
    string Subtitle,
    string IconGlyph,
    string Summary,
    string Content,
    string? Formula,
    string? ActionTargetTag,
    string? ActionLabel,
    string? Badge,
    IReadOnlyList<string> TipsList,
    IReadOnlyList<string> ShortcutKeys,
    string FormulaHeader = "",
    string KeyPointsHeader = "",
    string ShortcutsHeader = "");

internal sealed record HelpGuideDefinition(
    string Id,
    string Category,
    string TitleKey,
    string SubtitleKey,
    string IconGlyph,
    string SummaryKey,
    string ContentKey,
    string? FormulaKey,
    string? ActionTargetTag,
    string? ActionLabelKey,
    string? BadgeKey,
    IReadOnlyList<string> TipsListKeys,
    IReadOnlyList<string> ShortcutKeys);

public partial class HelpViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;

    private readonly List<HelpGuideDefinition> _definitions = new();
    private readonly List<HelpGuideDisplayItem> _allDisplayItems = new();

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedCategory { get; set; } = "All";

    [ObservableProperty]
    public partial ObservableCollection<HelpGuideDisplayItem> FilteredGuides { get; set; } = new();

    [ObservableProperty]
    public partial int TotalGuidesCount { get; set; }

    public HelpViewModel(INavigationService navigationService, ILocalizationService localizationService)
    {
        _navigationService = navigationService;
        _localizationService = localizationService;

        _localizationService.LanguageChanged += (s, e) =>
        {
            RebuildDisplayItems();
            ApplyFilter();
        };

        InitializeDefinitions();
        RebuildDisplayItems();
        ApplyFilter();
    }

    private void InitializeDefinitions()
    {
        _definitions.Clear();

        // 1. Getting Started & Dashboard Hub
        _definitions.Add(new HelpGuideDefinition(
            Id: "getting-started",
            Category: "GettingStarted",
            TitleKey: "Help_Guide_GettingStarted_Title",
            SubtitleKey: "Help_Guide_GettingStarted_Subtitle",
            IconGlyph: "\uE80F",
            SummaryKey: "Help_Guide_GettingStarted_Summary",
            ContentKey: "Help_Guide_GettingStarted_Content",
            FormulaKey: null,
            ActionTargetTag: "Dashboard",
            ActionLabelKey: "Help_Action_GoDashboard",
            BadgeKey: "Help_Badge_Core",
            TipsListKeys: new[]
            {
                "Help_Tip_GettingStarted_1",
                "Help_Tip_GettingStarted_2",
                "Help_Tip_GettingStarted_3"
            },
            ShortcutKeys: new[] { "Ctrl+1: Dashboard", "Ctrl+K: Smart Search", "F1: Help Hub" }
        ));

        // 2. Vocabulary & Grammar Management
        _definitions.Add(new HelpGuideDefinition(
            Id: "vocabulary-management",
            Category: "Vocabulary",
            TitleKey: "Help_Guide_Vocab_Title",
            SubtitleKey: "Help_Guide_Vocab_Subtitle",
            IconGlyph: "\uE8D2",
            SummaryKey: "Help_Guide_Vocab_Summary",
            ContentKey: "Help_Guide_Vocab_Content",
            FormulaKey: null,
            ActionTargetTag: "Vocabulary",
            ActionLabelKey: "Help_Action_GoVocabulary",
            BadgeKey: "Help_Badge_Essential",
            TipsListKeys: new[]
            {
                "Help_Tip_Vocab_1",
                "Help_Tip_Vocab_2",
                "Help_Tip_Vocab_3",
                "Help_Tip_Vocab_4"
            },
            ShortcutKeys: new[] { "Ctrl+2: Vocabulary Hub", "Ctrl+N: Add Word", "Ctrl+5: Grammar" }
        ));

        // 3. Spaced Repetition (SM-2 & FSRS Engine)
        _definitions.Add(new HelpGuideDefinition(
            Id: "spaced-repetition",
            Category: "SpacedRepetition",
            TitleKey: "Help_Guide_SpacedRepetition_Title",
            SubtitleKey: "Help_Guide_SpacedRepetition_Subtitle",
            IconGlyph: "\uE81C",
            SummaryKey: "Help_Guide_SpacedRepetition_Summary",
            ContentKey: "Help_Guide_SpacedRepetition_Content",
            FormulaKey: "Help_Guide_SpacedRepetition_Formula",
            ActionTargetTag: "Review",
            ActionLabelKey: "Help_Action_GoReview",
            BadgeKey: "Help_Badge_Algorithm",
            TipsListKeys: new[]
            {
                "Help_Tip_SpacedRepetition_Again",
                "Help_Tip_SpacedRepetition_Hard",
                "Help_Tip_SpacedRepetition_Good",
                "Help_Tip_SpacedRepetition_Easy"
            },
            ShortcutKeys: new[] { "Ctrl+6: Review", "1: Again", "2: Hard", "3: Good", "4: Easy", "Space: Flip Card", "Ctrl+Z: Undo" }
        ));

        // 4. Homophones & Pronunciation Listening Lab
        _definitions.Add(new HelpGuideDefinition(
            Id: "listening-lab-homophones",
            Category: "ListeningLab",
            TitleKey: "Help_Guide_ListeningLab_Title",
            SubtitleKey: "Help_Guide_ListeningLab_Subtitle",
            IconGlyph: "\uE767",
            SummaryKey: "Help_Guide_ListeningLab_Summary",
            ContentKey: "Help_Guide_ListeningLab_Content",
            FormulaKey: null,
            ActionTargetTag: "Homophones",
            ActionLabelKey: "Help_Action_GoHomophones",
            BadgeKey: "Help_Badge_Pronunciation",
            TipsListKeys: new[]
            {
                "Help_Tip_ListeningLab_1",
                "Help_Tip_ListeningLab_2",
                "Help_Tip_ListeningLab_3"
            },
            ShortcutKeys: new[] { "Ctrl+3: Homophones", "Alt+U: UK Accent", "Alt+A: US Accent" }
        ));

        // 5. Adaptive Testing & Practice Mode (CAT)
        _definitions.Add(new HelpGuideDefinition(
            Id: "test-and-cat",
            Category: "TestMode",
            TitleKey: "Help_Guide_Test_Title",
            SubtitleKey: "Help_Guide_Test_Subtitle",
            IconGlyph: "\uE7BE",
            SummaryKey: "Help_Guide_Test_Summary",
            ContentKey: "Help_Guide_Test_Content",
            FormulaKey: null,
            ActionTargetTag: "Test",
            ActionLabelKey: "Help_Action_GoTest",
            BadgeKey: "Help_Badge_ExamPrep",
            TipsListKeys: new[]
            {
                "Help_Tip_Test_1",
                "Help_Tip_Test_2",
                "Help_Tip_Test_3"
            },
            ShortcutKeys: new[] { "Ctrl+7: Test Hub", "1-4: Select Answer", "Enter: Submit" }
        ));

        // 6. Database Backup, Restore & Personalization
        _definitions.Add(new HelpGuideDefinition(
            Id: "backup-and-settings",
            Category: "BackupAndSettings",
            TitleKey: "Help_Guide_Settings_Title",
            SubtitleKey: "Help_Guide_Settings_Subtitle",
            IconGlyph: "\uE713",
            SummaryKey: "Help_Guide_Settings_Summary",
            ContentKey: "Help_Guide_Settings_Content",
            FormulaKey: null,
            ActionTargetTag: "Settings",
            ActionLabelKey: "Help_Action_GoSettings",
            BadgeKey: "Help_Badge_Safety",
            TipsListKeys: new[]
            {
                "Help_Tip_Settings_1",
                "Help_Tip_Settings_2",
                "Help_Tip_Settings_3"
            },
            ShortcutKeys: new[] { "Ctrl+T: Toggle Theme", "F1: Open Help" }
        ));

        TotalGuidesCount = _definitions.Count;
    }

    public void RebuildDisplayItems()
    {
        _allDisplayItems.Clear();

        foreach (var def in _definitions)
        {
            var tips = def.TipsListKeys.Select(k => _localizationService.GetString(k)).ToList();
            var item = new HelpGuideDisplayItem(
                Id: def.Id,
                Category: def.Category,
                Title: _localizationService.GetString(def.TitleKey),
                Subtitle: _localizationService.GetString(def.SubtitleKey),
                IconGlyph: def.IconGlyph,
                Summary: _localizationService.GetString(def.SummaryKey),
                Content: _localizationService.GetString(def.ContentKey),
                Formula: def.FormulaKey != null ? _localizationService.GetString(def.FormulaKey) : null,
                ActionTargetTag: def.ActionTargetTag,
                ActionLabel: def.ActionLabelKey != null ? _localizationService.GetString(def.ActionLabelKey) : null,
                Badge: def.BadgeKey != null ? _localizationService.GetString(def.BadgeKey) : null,
                TipsList: tips,
                ShortcutKeys: def.ShortcutKeys,
                FormulaHeader: _localizationService.GetString("Help_AlgorithmFormulaTitle"),
                KeyPointsHeader: _localizationService.GetString("Help_KeyPointsTitle"),
                ShortcutsHeader: _localizationService.GetString("Help_ShortcutsTitle")
            );
            _allDisplayItems.Add(item);
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ApplyFilter();
    }

    public void SelectCategory(string category)
    {
        SelectedCategory = category;
    }

    [RelayCommand]
    public void ResetFilter()
    {
        SearchQuery = string.Empty;
        SelectedCategory = "All";
    }

    [RelayCommand]
    public void NavigateToTarget(string? targetTag)
    {
        if (string.IsNullOrWhiteSpace(targetTag)) return;

        Type? targetType = targetTag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "Vocabulary" => typeof(VocabularyPage),
            "Homophones" => typeof(HomophonesPage),
            "KnowledgeGraph" => typeof(KnowledgeGraphPage),
            "Review" => typeof(ReviewPage),
            "Test" => typeof(TestPage),
            "Grammar" => typeof(GrammarPage),
            "LearningStorage" => typeof(LearningStoragePage),
            "Settings" => typeof(SettingsPage),
            _ => null
        };

        if (targetType != null)
        {
            _navigationService.Navigate(targetType);
        }
    }

    public void ApplyFilter()
    {
        var query = SearchQuery?.Trim() ?? string.Empty;
        var category = SelectedCategory;

        var results = _allDisplayItems.Where(g =>
        {
            // Category check
            if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(g.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Search query check
            if (!string.IsNullOrWhiteSpace(query))
            {
                var match = g.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            g.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            g.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            g.Summary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            g.Content.Contains(query, StringComparison.OrdinalIgnoreCase);

                if (!match) return false;
            }

            return true;
        }).ToList();

        FilteredGuides.Clear();
        foreach (var item in results)
        {
            FilteredGuides.Add(item);
        }
    }
}
