using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Domain.Common;
using Lexora.Presentation.Services;
using Lexora.Presentation.Views;
using Lexora.Presentation.Views.Roadmap;

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
    IReadOnlyList<string> ShortcutKeysKeys);

public partial class HelpViewModel : ObservableObject, IDisposable
{
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;
    private readonly EventHandler _languageChangedHandler;

    private readonly List<HelpGuideDefinition> _definitions = new();
    private readonly List<HelpGuideDisplayItem> _allDisplayItems = new();

    [ObservableProperty]
    public partial bool IsSubmitting { get; set; }

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

        _languageChangedHandler = (s, e) =>
        {
            RebuildDisplayItems();
            ApplyFilter();
        };
        _localizationService.LanguageChanged += _languageChangedHandler;

        InitializeDefinitions();
        RebuildDisplayItems();
        ApplyFilter();
    }

    public void Dispose()
    {
        _localizationService.LanguageChanged -= _languageChangedHandler;
        GC.SuppressFinalize(this);
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
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_Ctrl1",
                "Help_Shortcut_CtrlK",
                "Help_Shortcut_F1"
            }
        ));

        // 2. Vocabulary & Topic Units
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
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_Ctrl2",
                "Help_Shortcut_CtrlN",
                "Help_Shortcut_Ctrl5"
            }
        ));

        // 3. Spaced Repetition (SM-2 & FSRS Deep-Dive)
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
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_Ctrl6",
                "Help_Shortcut_SpaceFlip",
                "Help_Shortcut_RatingAgain",
                "Help_Shortcut_RatingHard",
                "Help_Shortcut_RatingGood",
                "Help_Shortcut_RatingEasy",
                "Help_Shortcut_CtrlZ"
            }
        ));

        // 4. Learning Roadmap & Goal Tracking
        _definitions.Add(new HelpGuideDefinition(
            Id: "learning-roadmap",
            Category: "Roadmap",
            TitleKey: "Help_Guide_Roadmap_Title",
            SubtitleKey: "Help_Guide_Roadmap_Subtitle",
            IconGlyph: "\uE81D",
            SummaryKey: "Help_Guide_Roadmap_Summary",
            ContentKey: "Help_Guide_Roadmap_Content",
            FormulaKey: "Help_Guide_Roadmap_Formula",
            ActionTargetTag: "Roadmap",
            ActionLabelKey: "Help_Action_GoRoadmap",
            BadgeKey: "Help_Badge_Roadmap",
            TipsListKeys: new[]
            {
                "Help_Tip_Roadmap_1",
                "Help_Tip_Roadmap_2",
                "Help_Tip_Roadmap_3"
            },
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_Ctrl9",
                "Help_Shortcut_RoadmapInspect"
            }
        ));

        // 5. Knowledge Graph & Semantic Associations
        _definitions.Add(new HelpGuideDefinition(
            Id: "knowledge-graph",
            Category: "KnowledgeGraph",
            TitleKey: "Help_Guide_KnowledgeGraph_Title",
            SubtitleKey: "Help_Guide_KnowledgeGraph_Subtitle",
            IconGlyph: "\uE9CE",
            SummaryKey: "Help_Guide_KnowledgeGraph_Summary",
            ContentKey: "Help_Guide_KnowledgeGraph_Content",
            FormulaKey: null,
            ActionTargetTag: "KnowledgeGraph",
            ActionLabelKey: "Help_Action_GoKnowledgeGraph",
            BadgeKey: "Help_Badge_Graph",
            TipsListKeys: new[]
            {
                "Help_Tip_KnowledgeGraph_1",
                "Help_Tip_KnowledgeGraph_2",
                "Help_Tip_KnowledgeGraph_3"
            },
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_Ctrl4",
                "Help_Shortcut_GraphInspect",
                "Help_Shortcut_GraphPivot"
            }
        ));

        // 6. Grammar & Pattern Suggester
        _definitions.Add(new HelpGuideDefinition(
            Id: "grammar-patterns",
            Category: "Vocabulary",
            TitleKey: "Help_Guide_Grammar_Title",
            SubtitleKey: "Help_Guide_Grammar_Subtitle",
            IconGlyph: "\uE8D3",
            SummaryKey: "Help_Guide_Grammar_Summary",
            ContentKey: "Help_Guide_Grammar_Content",
            FormulaKey: null,
            ActionTargetTag: "Grammar",
            ActionLabelKey: "Help_Action_GoGrammar",
            BadgeKey: "Help_Badge_Grammar",
            TipsListKeys: new[]
            {
                "Help_Tip_Grammar_1",
                "Help_Tip_Grammar_2",
                "Help_Tip_Grammar_3"
            },
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_Ctrl5"
            }
        ));

        // 7. Learning Storage & Personal Notebooks
        _definitions.Add(new HelpGuideDefinition(
            Id: "learning-storage",
            Category: "KnowledgeGraph",
            TitleKey: "Help_Guide_Storage_Title",
            SubtitleKey: "Help_Guide_Storage_Subtitle",
            IconGlyph: "\uE74E",
            SummaryKey: "Help_Guide_Storage_Summary",
            ContentKey: "Help_Guide_Storage_Content",
            FormulaKey: null,
            ActionTargetTag: "LearningStorage",
            ActionLabelKey: "Help_Action_GoStorage",
            BadgeKey: "Help_Badge_Storage",
            TipsListKeys: new[]
            {
                "Help_Tip_Storage_1",
                "Help_Tip_Storage_2",
                "Help_Tip_Storage_3"
            },
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_Ctrl8"
            }
        ));

        // 8. Homophones, Pronunciation & Listening Lab
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
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_Ctrl3",
                "Help_Shortcut_AltU",
                "Help_Shortcut_AltA"
            }
        ));

        // 9. Adaptive Testing & Practice Mode (CAT)
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
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_Ctrl7",
                "Help_Shortcut_TestKeys",
                "Help_Shortcut_TestEnter"
            }
        ));

        // 10. Database Backup, Restore & Personalization
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
            ShortcutKeysKeys: new[]
            {
                "Help_Shortcut_CtrlT",
                "Help_Shortcut_F1"
            }
        ));

        TotalGuidesCount = _definitions.Count;
    }

    public void RebuildDisplayItems()
    {
        _allDisplayItems.Clear();

        foreach (var def in _definitions)
        {
            var tips = def.TipsListKeys.Select(k => _localizationService.GetString(k)).ToList();
            var shortcuts = def.ShortcutKeysKeys.Select(k => _localizationService.GetString(k)).ToList();
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
                ShortcutKeys: shortcuts,
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
            "Roadmap" => typeof(RoadmapPage),
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
        var rawQuery = SearchQuery?.Trim() ?? string.Empty;
        var normQuery = SearchTextHelper.Normalize(rawQuery);
        var category = SelectedCategory;

        var results = _allDisplayItems.Where(g =>
        {
            // Category check
            if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(g.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Search query check with diacritic-insensitive intelligence
            if (!string.IsNullOrWhiteSpace(normQuery))
            {
                var match = SearchTextHelper.Normalize(g.Id).Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                            SearchTextHelper.Normalize(g.Title).Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                            SearchTextHelper.Normalize(g.Subtitle).Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                            SearchTextHelper.Normalize(g.Summary).Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                            SearchTextHelper.Normalize(g.Content).Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
                            (g.Formula != null && SearchTextHelper.Normalize(g.Formula).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
                            g.TipsList.Any(t => SearchTextHelper.Normalize(t).Contains(normQuery, StringComparison.OrdinalIgnoreCase)) ||
                            g.ShortcutKeys.Any(s => SearchTextHelper.Normalize(s).Contains(normQuery, StringComparison.OrdinalIgnoreCase));

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
