using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Lexora.Presentation.Views;



public partial class EditableExample : ObservableObject
{
    private static ILocalizationService? Localization => App.Current?.Services?.GetService<ILocalizationService>();

    [ObservableProperty]
    public partial int Index { get; set; } = 1;

    public string ExampleBadge => string.Format(Localization?.GetString("GrammarEditor_ExampleBadge") ?? "Ví dụ #{0}", Index);

    [ObservableProperty]
    public partial string SentenceEn { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SentenceVi { get; set; } = string.Empty;

    public string PlaceholderExampleEn => Localization?.GetString("WordEditor_PlaceholderExampleEn") ?? "Câu ví dụ tiếng Anh...";
    public string PlaceholderExampleVi => Localization?.GetString("WordEditor_PlaceholderExampleVi") ?? "Bản dịch nghĩa tiếng Việt...";
    public string TooltipDeleteExample => Localization?.GetString("WordEditor_TooltipDeleteExample") ?? "Xóa câu ví dụ này";

    partial void OnIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ExampleBadge));
    }
}

public partial class EditableMeaning : ObservableObject
{
    private static ILocalizationService? Localization => App.Current?.Services?.GetService<ILocalizationService>();

    [ObservableProperty]
    public partial int Index { get; set; } = 1;

    public string MeaningBadge => string.Format(Localization?.GetString("WordEditor_MeaningBadge") ?? "Tầng nghĩa #{0}", Index);

    [ObservableProperty]
    public partial string DefinitionVi { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? DefinitionEn { get; set; }

    [ObservableProperty]
    public partial string? Explanation { get; set; }

    [ObservableProperty]
    public partial bool CanDelete { get; set; } = true;

    public ObservableCollection<EditableExample> Examples { get; } = new();

    public string HeaderDefinitionVi => Localization?.GetString("WordEditor_HeaderDefinitionVi") ?? "Định nghĩa Tiếng Việt *";
    public string PlaceholderDefinitionVi => Localization?.GetString("WordEditor_PlaceholderDefinitionVi") ?? "Định nghĩa tiếng Việt rõ ràng, súc tích...";
    public string HeaderDefinitionEn => Localization?.GetString("WordEditor_HeaderDefinitionEn") ?? "Định nghĩa Tiếng Anh";
    public string PlaceholderDefinitionEn => Localization?.GetString("WordEditor_PlaceholderDefinitionEn") ?? "Định nghĩa tiếng Anh (Oxford/Cambridge)...";
    public string HeaderExplanation => Localization?.GetString("WordEditor_HeaderExplanation") ?? "Ngữ cảnh / Sắc thái sử dụng";
    public string PlaceholderExplanation => Localization?.GetString("WordEditor_PlaceholderExplanation") ?? "Khi nào nên dùng, văn phong trang trọng hay thân mật...";
    public string SectionExamples => Localization?.GetString("WordEditor_SectionExamples") ?? "Câu ví dụ minh họa";
    public string BtnAddExample => Localization?.GetString("WordEditor_BtnAddExample") ?? "Thêm ví dụ";
    public string TooltipDeleteMeaning => Localization?.GetString("WordEditor_TooltipDeleteMeaning") ?? "Xóa tầng nghĩa này";

    partial void OnIndexChanged(int value)
    {
        OnPropertyChanged(nameof(MeaningBadge));
    }
}

public partial class EditablePartOfSpeechGroup : ObservableObject
{
    private static ILocalizationService? Localization => App.Current?.Services?.GetService<ILocalizationService>();

    [ObservableProperty]
    public partial PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Noun;

    partial void OnPartOfSpeechChanged(PartOfSpeech value)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(ShortAbbr));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(BtnAddMeaningLabel));
    }

    [ObservableProperty]
    public partial bool IsPrimary { get; set; } = false;

    partial void OnIsPrimaryChanged(bool value)
    {
        OnPropertyChanged(nameof(PrimaryBadgeVisibility));
        OnPropertyChanged(nameof(SetPrimaryButtonVisibility));
    }

    public Visibility PrimaryBadgeVisibility => IsPrimary ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SetPrimaryButtonVisibility => !IsPrimary ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    public partial bool CanDelete { get; set; } = true;

    public string DisplayName => LocalizationHelper.FormatPartOfSpeech(PartOfSpeech);
    public string ShortAbbr => WordDto.GetAbbreviation(PartOfSpeech);
    public string HeaderTitle => $"{ShortAbbr} • {DisplayName}";

    public string BtnAddMeaningLabel => string.Format(Localization?.GetString("WordEditor_BtnAddMeaningForPos") ?? "Thêm tầng nghĩa cho {0}", DisplayName);

    public string TooltipDeletePos => Localization?.GetString("WordEditor_TooltipDeletePos") ?? "Xóa từ loại này cùng tất cả tầng nghĩa";
    public string ConfirmDeletePrompt => Localization?.GetString("WordEditor_ConfirmDeletePosPrompt") ?? "Xóa từ loại này cùng tất cả tầng nghĩa đã nhập?";
    public string ActionDeleteText => Localization?.GetString("Action_Delete") ?? "Xóa";
    public string ActionCancelText => Localization?.GetString("Action_Cancel") ?? "Hủy";

    public string PrimaryPosBadge => Localization?.GetString("WordEditor_PrimaryPosBadge") ?? "★ Từ loại chính";
    public string SetAsPrimaryText => Localization?.GetString("WordEditor_SetAsPrimary") ?? "Đặt làm chính";
    public string TooltipSetAsPrimary => Localization?.GetString("WordEditor_TooltipSetAsPrimary") ?? "Đặt từ loại này làm từ loại chính của từ vựng";

    public ObservableCollection<EditableMeaning> Meanings { get; } = new();

    public IReadOnlyList<PartOfSpeech> AvailablePartsOfSpeech { get; } = Enum.GetValues<PartOfSpeech>();
}

public sealed partial class WordEditorDialog : ContentDialog
{
    public ILocalizationService Loc { get; }
    public ObservableCollection<EditablePartOfSpeechGroup> PosGroups { get; } = new();
    public WordCreateOrUpdateDto? ResultDto { get; private set; }

    private readonly Guid? _editingWordId;
    private readonly string? _existingAudioUkPath;
    private readonly string? _existingAudioUsPath;
    private readonly List<TopicDto> _allTopics;
    private readonly List<ExamTagDto> _allExamTags;
    private bool _isHandlingPosSelectionChange;

    public WordEditorDialog(
        WordDto? existingWord,
        IReadOnlyList<TopicDto> allTopics,
        IReadOnlyList<ExamTagDto> allExamTags,
        Guid? preselectedTopicId = null)
    {
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();

        InitializeComponent();

        if (App.Current?.Services?.GetService<IThemeService>() is { } themeService)
        {
            RequestedTheme = themeService.CurrentTheme;
        }

        PrimaryButtonText = Loc.GetString("WordEditor_Save");
        CloseButtonText = Loc.GetString("WordEditor_Cancel");
        DialogInfoBar.Title = Loc.GetString("WordEditor_ValidationTitle");

        _allTopics = allTopics.ToList();
        _allExamTags = allExamTags.ToList();
        _editingWordId = existingWord?.Id;
        _existingAudioUkPath = existingWord?.AudioUkPath;
        _existingAudioUsPath = existingWord?.AudioUsPath;

        LevelComboBox.ItemsSource = Enum.GetValues<CefrLevel>();
        LevelComboBox.SelectedItem = existingWord?.Level ?? CefrLevel.B1;

        TopicsListView.ItemsSource = _allTopics;
        ExamTagsListView.ItemsSource = _allExamTags;

        EmptyTopicsTextBlock.Visibility = _allTopics.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyExamTagsTextBlock.Visibility = _allExamTags.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (existingWord != null)
        {
            Title = string.Format(Loc.GetString("WordEditor_TitleEdit"), existingWord.Term);
            TermTextBox.Text = existingWord.Term;
            IpaUkTextBox.Text = existingWord.PhoneticUk ?? string.Empty;
            IpaUsTextBox.Text = existingWord.PhoneticUs ?? string.Empty;
            NotesTextBox.Text = existingWord.Notes ?? string.Empty;

            // Group meanings by PartOfSpeech
            var groupedMeanings = existingWord.Meanings
                .GroupBy(m => m.PartOfSpeech)
                .ToList();

            foreach (var group in groupedMeanings)
            {
                var posGroup = new EditablePartOfSpeechGroup
                {
                    PartOfSpeech = group.Key,
                    IsPrimary = (group.Key == existingWord.PrimaryPartOfSpeech)
                };

                foreach (var m in group)
                {
                    var editableMeaning = new EditableMeaning
                    {
                        DefinitionVi = m.DefinitionVi,
                        DefinitionEn = m.DefinitionEn,
                        Explanation = m.Explanation
                    };

                    foreach (var ex in m.Examples)
                    {
                        editableMeaning.Examples.Add(new EditableExample
                        {
                            SentenceEn = ex.SentenceEn,
                            SentenceVi = ex.SentenceVi
                        });
                    }

                    RefreshExampleIndices(editableMeaning);
                    posGroup.Meanings.Add(editableMeaning);
                }

                RefreshMeaningIndices(posGroup);
                PosGroups.Add(posGroup);
            }

            // Fallback: If word has no meanings yet, initialize with its primary POS
            if (PosGroups.Count == 0)
            {
                var defaultGroup = new EditablePartOfSpeechGroup
                {
                    PartOfSpeech = existingWord.PrimaryPartOfSpeech,
                    IsPrimary = true
                };
                var defaultMeaning = new EditableMeaning();
                defaultMeaning.Examples.Add(new EditableExample());
                defaultGroup.Meanings.Add(defaultMeaning);
                PosGroups.Add(defaultGroup);
            }

            // Ensure at least one group is primary
            if (!PosGroups.Any(g => g.IsPrimary))
            {
                var primaryMatch = PosGroups.FirstOrDefault(g => g.PartOfSpeech == existingWord.PrimaryPartOfSpeech);
                if (primaryMatch != null)
                {
                    primaryMatch.IsPrimary = true;
                }
                else
                {
                    PosGroups[0].IsPrimary = true;
                }
            }

            // Select topics
            foreach (var t in _allTopics)
            {
                if (existingWord.Topics.Contains(t.Name))
                {
                    TopicsListView.SelectedItems.Add(t);
                }
            }

            // Select exam tags
            foreach (var et in _allExamTags)
            {
                if (existingWord.ExamTags.Contains(et.Name))
                {
                    ExamTagsListView.SelectedItems.Add(et);
                }
            }
        }
        else
        {
            Title = Loc.GetString("WordEditor_TitleAdd");

            // Smart Default: 1 POS group (Noun) with 1 empty meaning and 1 empty example
            var defaultGroup = new EditablePartOfSpeechGroup
            {
                PartOfSpeech = PartOfSpeech.Noun,
                IsPrimary = true
            };
            var defaultMeaning = new EditableMeaning();
            defaultMeaning.Examples.Add(new EditableExample());
            defaultGroup.Meanings.Add(defaultMeaning);
            RefreshMeaningIndices(defaultGroup);
            PosGroups.Add(defaultGroup);

            if (preselectedTopicId.HasValue)
            {
                var preselected = _allTopics.FirstOrDefault(t => t.Id == preselectedTopicId.Value);
                if (preselected != null)
                {
                    TopicsListView.SelectedItems.Add(preselected);
                }
            }
        }

        RefreshPosGroupState();

        PrimaryButtonClick += WordEditorDialog_PrimaryButtonClick;

        // Auto-focus the Term input field when dialog loads
        Loaded += (s, e) =>
        {
            TermTextBox.Focus(FocusState.Programmatic);
            TermTextBox.SelectAll();
        };
    }

    private void AddPosMenuFlyout_Opening(object? sender, object e)
    {
        AddPosMenuFlyout.Items.Clear();
        var usedPos = PosGroups.Select(g => g.PartOfSpeech).ToHashSet();

        foreach (var pos in Enum.GetValues<PartOfSpeech>())
        {
            var item = new MenuFlyoutItem
            {
                Text = $"{WordDto.GetAbbreviation(pos)} • {LocalizationHelper.FormatPartOfSpeech(pos)}",
                Tag = pos,
                IsEnabled = !usedPos.Contains(pos)
            };
            item.Click += (s, args) =>
            {
                if (s is MenuFlyoutItem mfi && mfi.Tag is PartOfSpeech p)
                {
                    AddPartOfSpeechGroup(p);
                }
            };
            AddPosMenuFlyout.Items.Add(item);
        }
    }

    private void RefreshPosGroupState()
    {
        for (int i = 0; i < PosGroups.Count; i++)
        {
            PosGroups[i].CanDelete = PosGroups.Count > 1;
            RefreshMeaningIndices(PosGroups[i]);
        }
    }

    private static void RefreshMeaningIndices(EditablePartOfSpeechGroup group)
    {
        for (int i = 0; i < group.Meanings.Count; i++)
        {
            group.Meanings[i].Index = i + 1;
            group.Meanings[i].CanDelete = group.Meanings.Count > 1;
        }
    }

    private static void RefreshExampleIndices(EditableMeaning meaning)
    {
        for (int i = 0; i < meaning.Examples.Count; i++)
        {
            meaning.Examples[i].Index = i + 1;
        }
    }

    private void AddPartOfSpeechGroup(PartOfSpeech pos)
    {
        if (PosGroups.Any(g => g.PartOfSpeech == pos))
        {
            DialogInfoBar.Message = Loc.GetString("WordEditor_ErrDuplicatePos");
            DialogInfoBar.IsOpen = true;
            return;
        }

        DialogInfoBar.IsOpen = false;

        var newGroup = new EditablePartOfSpeechGroup
        {
            PartOfSpeech = pos,
            IsPrimary = PosGroups.Count == 0
        };

        var defaultMeaning = new EditableMeaning();
        defaultMeaning.Examples.Add(new EditableExample());
        RefreshExampleIndices(defaultMeaning);
        newGroup.Meanings.Add(defaultMeaning);
        RefreshMeaningIndices(newGroup);

        PosGroups.Add(newGroup);
        RefreshPosGroupState();
    }

    private void PosGroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isHandlingPosSelectionChange) return;

        if (sender is ComboBox cb && cb.DataContext is EditablePartOfSpeechGroup group && cb.SelectedItem is PartOfSpeech newPos)
        {
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is PartOfSpeech oldPos && oldPos != newPos)
            {
                if (PosGroups.Any(g => g != group && g.PartOfSpeech == newPos))
                {
                    DialogInfoBar.Message = Loc.GetString("WordEditor_ErrDuplicatePos");
                    DialogInfoBar.IsOpen = true;
                    _isHandlingPosSelectionChange = true;
                    try
                    {
                        cb.SelectedItem = oldPos;
                    }
                    finally
                    {
                        _isHandlingPosSelectionChange = false;
                    }
                    return;
                }

                DialogInfoBar.IsOpen = false;
                group.PartOfSpeech = newPos;
            }
        }
    }

    private void SetPrimaryPosButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is EditablePartOfSpeechGroup group)
        {
            foreach (var g in PosGroups)
            {
                g.IsPrimary = (g == group);
            }
        }
    }

    private void DeletePosGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (PosGroups.Count <= 1) return;

        if (sender is Button btn && btn.DataContext is EditablePartOfSpeechGroup group)
        {
            PosGroups.Remove(group);

            if (group.IsPrimary && PosGroups.Count > 0)
            {
                PosGroups[0].IsPrimary = true;
            }

            RefreshPosGroupState();
        }
    }

    private void AddMeaningToPosButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is EditablePartOfSpeechGroup group)
        {
            var newMeaning = new EditableMeaning();
            newMeaning.Examples.Add(new EditableExample());
            RefreshExampleIndices(newMeaning);
            group.Meanings.Add(newMeaning);
            RefreshMeaningIndices(group);
        }
    }

    private void DeleteMeaningButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is EditableMeaning m)
        {
            var group = PosGroups.FirstOrDefault(g => g.Meanings.Contains(m));
            if (group != null && group.Meanings.Count > 1)
            {
                group.Meanings.Remove(m);
                RefreshMeaningIndices(group);
            }
        }
    }

    private void AddExampleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is EditableMeaning m)
        {
            m.Examples.Add(new EditableExample());
            RefreshExampleIndices(m);
        }
    }

    private void DeleteExampleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is EditableExample ex)
        {
            foreach (var group in PosGroups)
            {
                foreach (var m in group.Meanings)
                {
                    if (m.Examples.Contains(ex))
                    {
                        m.Examples.Remove(ex);
                        RefreshExampleIndices(m);
                        return;
                    }
                }
            }
        }
    }

    private void NewTopicNameTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            CreateTopicQuickButton_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private async void CreateTopicQuickButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NewTopicNameTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            NewTopicNameTextBox.Focus(FocusState.Programmatic);
            return;
        }

        var vocabService = App.Current.Services.GetService<Lexora.Application.Interfaces.IVocabularyService>();
        if (vocabService == null) return;

        try
        {
            var desc = NewTopicDescTextBox.Text?.Trim();
            var newTopic = await vocabService.CreateTopicAsync(new TopicCreateOrUpdateDto(
                Name: name,
                Description: string.IsNullOrWhiteSpace(desc) ? null : desc,
                IconGlyph: "\uE8EC",
                ColorHex: "#0078D4"
            ));

            var selectedTopicIds = TopicsListView.SelectedItems.OfType<TopicDto>().Select(t => t.Id).ToHashSet();
            _allTopics.Add(newTopic);
            TopicsListView.ItemsSource = null;
            TopicsListView.ItemsSource = _allTopics;
            foreach (var t in _allTopics)
            {
                if (selectedTopicIds.Contains(t.Id) || t.Id == newTopic.Id)
                {
                    TopicsListView.SelectedItems.Add(t);
                }
            }
            EmptyTopicsTextBlock.Visibility = Visibility.Collapsed;

            NewTopicNameTextBox.Text = string.Empty;
            NewTopicDescTextBox.Text = string.Empty;
            AddTopicFlyout.Hide();
        }
        catch (Exception ex)
        {
            DialogInfoBar.Message = ex.Message;
            DialogInfoBar.IsOpen = true;
        }
    }

    private void WordEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        DialogInfoBar.IsOpen = false;

        var term = TermTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(term))
        {
            DialogInfoBar.Message = Loc.GetString("WordEditor_ErrTermRequired");
            DialogInfoBar.IsOpen = true;
            TermTextBox.Focus(FocusState.Programmatic);
            args.Cancel = true;
            return;
        }

        if (PosGroups.Count == 0)
        {
            DialogInfoBar.Message = Loc.GetString("WordEditor_ErrMeaningRequired");
            DialogInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        // Validate each POS group and collect meanings
        var meaningDtos = new List<WordMeaningCreateDto>();
        foreach (var group in PosGroups)
        {
            var validMeaningsForGroup = group.Meanings
                .Where(m => !string.IsNullOrWhiteSpace(m.DefinitionVi))
                .ToList();

            if (validMeaningsForGroup.Count == 0)
            {
                DialogInfoBar.Message = string.Format(Loc.GetString("WordEditor_ErrPosNeedsMeaning"), group.DisplayName);
                DialogInfoBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            foreach (var m in validMeaningsForGroup)
            {
                var exampleDtos = m.Examples
                    .Where(ex => !string.IsNullOrWhiteSpace(ex.SentenceEn))
                    .Select(ex => new ExampleSentenceCreateDto(
                        SentenceEn: ex.SentenceEn.Trim(),
                        SentenceVi: ex.SentenceVi?.Trim() ?? string.Empty,
                        AudioPath: null
                    ))
                    .ToList();

                meaningDtos.Add(new WordMeaningCreateDto(
                    PartOfSpeech: group.PartOfSpeech,
                    DefinitionVi: m.DefinitionVi.Trim(),
                    DefinitionEn: string.IsNullOrWhiteSpace(m.DefinitionEn) ? null : m.DefinitionEn.Trim(),
                    Explanation: string.IsNullOrWhiteSpace(m.Explanation) ? null : m.Explanation.Trim(),
                    Examples: exampleDtos
                ));
            }
        }

        if (meaningDtos.Count == 0)
        {
            DialogInfoBar.Message = Loc.GetString("WordEditor_ErrMeaningRequired");
            DialogInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        var primaryGroup = PosGroups.FirstOrDefault(g => g.IsPrimary) ?? PosGroups[0];
        var primaryPos = primaryGroup.PartOfSpeech;
        var level = LevelComboBox.SelectedItem is CefrLevel l ? l : CefrLevel.None;

        var selectedTopics = TopicsListView.SelectedItems.OfType<TopicDto>().Select(t => t.Id).ToList();
        var selectedExamTags = ExamTagsListView.SelectedItems.OfType<ExamTagDto>().Select(et => et.Id).ToList();

        ResultDto = new WordCreateOrUpdateDto(
            _editingWordId,
            term,
            string.IsNullOrWhiteSpace(IpaUkTextBox.Text) ? null : IpaUkTextBox.Text.Trim(),
            string.IsNullOrWhiteSpace(IpaUsTextBox.Text) ? null : IpaUsTextBox.Text.Trim(),
            _existingAudioUkPath,
            _existingAudioUsPath,
            level,
            primaryPos,
            string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
            meaningDtos,
            selectedTopics,
            selectedExamTags
        );
    }
}
