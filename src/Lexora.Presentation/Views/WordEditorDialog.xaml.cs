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

public partial class PosChipItem : ObservableObject
{
    public PartOfSpeech PartOfSpeech { get; }
    public string DisplayName { get; }
    public string ShortAbbr { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public PosChipItem(PartOfSpeech pos, string displayName, string shortAbbr, bool isSelected = false)
    {
        PartOfSpeech = pos;
        DisplayName = displayName;
        ShortAbbr = shortAbbr;
        IsSelected = isSelected;
    }
}

public sealed partial class WordEditorDialog : ContentDialog
{
    public partial class EditableMeaning : ObservableObject
    {
        private static ILocalizationService? Localization => App.Current?.Services?.GetService<ILocalizationService>();

        [ObservableProperty]
        public partial int Index { get; set; } = 1;

        public string MeaningBadge => string.Format(Localization?.GetString("WordEditor_MeaningBadge") ?? "Tầng nghĩa #{0}", Index);

        [ObservableProperty]
        public partial PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Noun;

        public event EventHandler? PartOfSpeechChanged;

        partial void OnPartOfSpeechChanged(PartOfSpeech value)
        {
            PartOfSpeechChanged?.Invoke(this, EventArgs.Empty);
        }

        [ObservableProperty]
        public partial string DefinitionVi { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? DefinitionEn { get; set; }

        [ObservableProperty]
        public partial string? Explanation { get; set; }

        [ObservableProperty]
        public partial bool CanDelete { get; set; } = true;

        public IReadOnlyList<PartOfSpeech> AvailablePartsOfSpeech { get; } = Enum.GetValues<PartOfSpeech>();

        public ObservableCollection<EditableExample> Examples { get; } = new();

        public string HeaderMeaningPos => Localization?.GetString("WordEditor_HeaderMeaningPos") ?? "Từ loại tầng nghĩa";
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

    public ILocalizationService Loc { get; }
    public ObservableCollection<EditableMeaning> Meanings { get; } = new();
    public ObservableCollection<PosChipItem> PosChips { get; } = new();
    public WordCreateOrUpdateDto? ResultDto { get; private set; }

    private readonly Guid? _editingWordId;
    private readonly string? _existingAudioUkPath;
    private readonly string? _existingAudioUsPath;
    private readonly List<TopicDto> _allTopics;
    private readonly List<ExamTagDto> _allExamTags;

    public WordEditorDialog(
        WordDto? existingWord,
        IReadOnlyList<TopicDto> allTopics,
        IReadOnlyList<ExamTagDto> allExamTags,
        Guid? preselectedTopicId = null)
    {
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();

        InitializeComponent();
        Loaded += (s, e) => DialogEntranceStoryboard.Begin();

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

        // Initialize POS Chips & Combos
        InitializePosChips();
        PosChipsRepeater.ItemsSource = PosChips;

        PosComboBox.ItemsSource = Enum.GetValues<PartOfSpeech>();
        PosComboBox.SelectedItem = existingWord?.PrimaryPartOfSpeech ?? PartOfSpeech.Noun;

        LevelComboBox.ItemsSource = Enum.GetValues<CefrLevel>();
        LevelComboBox.SelectedItem = existingWord?.Level ?? CefrLevel.B1;

        TopicsListView.ItemsSource = _allTopics;
        ExamTagsListView.ItemsSource = _allExamTags;

        EmptyTopicsTextBlock.Visibility = _allTopics.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyExamTagsTextBlock.Visibility = _allExamTags.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        MeaningsItemsControl.ItemsSource = Meanings;

        if (existingWord != null)
        {
            Title = string.Format(Loc.GetString("WordEditor_TitleEdit"), existingWord.Term);
            TermTextBox.Text = existingWord.Term;
            IpaUkTextBox.Text = existingWord.PhoneticUk ?? string.Empty;
            IpaUsTextBox.Text = existingWord.PhoneticUs ?? string.Empty;
            NotesTextBox.Text = existingWord.Notes ?? string.Empty;

            foreach (var m in existingWord.Meanings)
            {
                var editableMeaning = new EditableMeaning
                {
                    PartOfSpeech = m.PartOfSpeech,
                    DefinitionVi = m.DefinitionVi,
                    DefinitionEn = m.DefinitionEn,
                    Explanation = m.Explanation
                };
                editableMeaning.PartOfSpeechChanged += Meaning_PartOfSpeechChanged;

                foreach (var ex in m.Examples)
                {
                    editableMeaning.Examples.Add(new EditableExample
                    {
                        SentenceEn = ex.SentenceEn,
                        SentenceVi = ex.SentenceVi
                    });
                }

                RefreshExampleIndices(editableMeaning);
                Meanings.Add(editableMeaning);
            }

            if (Meanings.Count == 0)
            {
                var defaultMeaning = new EditableMeaning { PartOfSpeech = existingWord.PrimaryPartOfSpeech };
                defaultMeaning.PartOfSpeechChanged += Meaning_PartOfSpeechChanged;
                defaultMeaning.Examples.Add(new EditableExample());
                Meanings.Add(defaultMeaning);
            }

            // Sync all POS from existing word
            foreach (var pos in existingWord.AllPartsOfSpeech)
            {
                var chip = PosChips.FirstOrDefault(c => c.PartOfSpeech == pos);
                if (chip != null) chip.IsSelected = true;
            }
            var primaryChip = PosChips.FirstOrDefault(c => c.PartOfSpeech == existingWord.PrimaryPartOfSpeech);
            if (primaryChip != null) primaryChip.IsSelected = true;

            PosComboBox.SelectedItem = existingWord.PrimaryPartOfSpeech;

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
            var defaultMeaning = new EditableMeaning { PartOfSpeech = PartOfSpeech.Noun };
            defaultMeaning.PartOfSpeechChanged += Meaning_PartOfSpeechChanged;
            defaultMeaning.Examples.Add(new EditableExample());
            Meanings.Add(defaultMeaning);

            var defaultChip = PosChips.FirstOrDefault(c => c.PartOfSpeech == PartOfSpeech.Noun);
            if (defaultChip != null) defaultChip.IsSelected = true;
            PosComboBox.SelectedItem = PartOfSpeech.Noun;

            if (preselectedTopicId.HasValue)
            {
                var preselected = _allTopics.FirstOrDefault(t => t.Id == preselectedTopicId.Value);
                if (preselected != null)
                {
                    TopicsListView.SelectedItems.Add(preselected);
                }
            }
        }

        RefreshMeaningIndices();
        PrimaryButtonClick += WordEditorDialog_PrimaryButtonClick;
    }

    private void InitializePosChips()
    {
        PosChips.Clear();
        PosChips.Add(new PosChipItem(PartOfSpeech.Noun, Loc.GetString("WordEditor_PosNoun"), "n."));
        PosChips.Add(new PosChipItem(PartOfSpeech.Verb, Loc.GetString("WordEditor_PosVerb"), "v."));
        PosChips.Add(new PosChipItem(PartOfSpeech.Adjective, Loc.GetString("WordEditor_PosAdj"), "adj."));
        PosChips.Add(new PosChipItem(PartOfSpeech.Adverb, Loc.GetString("WordEditor_PosAdv"), "adv."));
        PosChips.Add(new PosChipItem(PartOfSpeech.PhrasalVerb, Loc.GetString("WordEditor_PosPhrasalVerb"), "phr. v."));
        PosChips.Add(new PosChipItem(PartOfSpeech.Idiom, Loc.GetString("WordEditor_PosIdiom"), "idiom"));
        PosChips.Add(new PosChipItem(PartOfSpeech.NounPhrase, Loc.GetString("WordEditor_PosNounPhrase"), "n. phr."));
        PosChips.Add(new PosChipItem(PartOfSpeech.Preposition, Loc.GetString("WordEditor_PosPreposition"), "prep."));
        PosChips.Add(new PosChipItem(PartOfSpeech.Conjunction, Loc.GetString("WordEditor_PosConjunction"), "conj."));
        PosChips.Add(new PosChipItem(PartOfSpeech.Interjection, Loc.GetString("WordEditor_PosInterjection"), "interj."));
    }

    private void Meaning_PartOfSpeechChanged(object? sender, EventArgs e)
    {
        SyncChipsFromMeanings();
    }

    private void SyncChipsFromMeanings()
    {
        var posInMeanings = Meanings.Select(m => m.PartOfSpeech).ToHashSet();
        foreach (var chip in PosChips)
        {
            if (posInMeanings.Contains(chip.PartOfSpeech))
            {
                chip.IsSelected = true;
            }
        }
    }

    private void PosChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb && tb.DataContext is PosChipItem chip)
        {
            bool isChecked = tb.IsChecked == true;
            chip.IsSelected = isChecked;

            if (isChecked)
            {
                var existingMeaning = Meanings.FirstOrDefault(m => m.PartOfSpeech == chip.PartOfSpeech);
                if (existingMeaning == null)
                {
                    if (Meanings.Count == 1 && string.IsNullOrWhiteSpace(Meanings[0].DefinitionVi))
                    {
                        var firstPosChip = PosChips.FirstOrDefault(c => c.PartOfSpeech == Meanings[0].PartOfSpeech);
                        if (firstPosChip != null && !firstPosChip.IsSelected)
                        {
                            Meanings[0].PartOfSpeech = chip.PartOfSpeech;
                        }
                        else
                        {
                            var newMeaning = new EditableMeaning { PartOfSpeech = chip.PartOfSpeech };
                            newMeaning.PartOfSpeechChanged += Meaning_PartOfSpeechChanged;
                            newMeaning.Examples.Add(new EditableExample());
                            Meanings.Add(newMeaning);
                        }
                    }
                    else
                    {
                        var newMeaning = new EditableMeaning { PartOfSpeech = chip.PartOfSpeech };
                        newMeaning.PartOfSpeechChanged += Meaning_PartOfSpeechChanged;
                        newMeaning.Examples.Add(new EditableExample());
                        Meanings.Add(newMeaning);
                    }
                    RefreshMeaningIndices();
                }

                if (PosComboBox.SelectedItem == null)
                {
                    PosComboBox.SelectedItem = chip.PartOfSpeech;
                }
            }
            else
            {
                if (!PosChips.Any(c => c.IsSelected))
                {
                    chip.IsSelected = true;
                    tb.IsChecked = true;
                    return;
                }

                if (PosComboBox.SelectedItem is PartOfSpeech primary && primary == chip.PartOfSpeech)
                {
                    var nextSelected = PosChips.FirstOrDefault(c => c.IsSelected);
                    if (nextSelected != null)
                    {
                        PosComboBox.SelectedItem = nextSelected.PartOfSpeech;
                    }
                }

                var emptyMeaning = Meanings.FirstOrDefault(m => m.PartOfSpeech == chip.PartOfSpeech && string.IsNullOrWhiteSpace(m.DefinitionVi));
                if (emptyMeaning != null && Meanings.Count > 1)
                {
                    emptyMeaning.PartOfSpeechChanged -= Meaning_PartOfSpeechChanged;
                    Meanings.Remove(emptyMeaning);
                    RefreshMeaningIndices();
                }
            }
        }
    }

    private void PosComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PosComboBox.SelectedItem is PartOfSpeech p)
        {
            var chip = PosChips.FirstOrDefault(c => c.PartOfSpeech == p);
            if (chip != null && !chip.IsSelected)
            {
                chip.IsSelected = true;
            }
        }
    }


    private void RefreshMeaningIndices()
    {
        for (int i = 0; i < Meanings.Count; i++)
        {
            Meanings[i].Index = i + 1;
            Meanings[i].CanDelete = Meanings.Count > 1;
        }
    }

    private static void RefreshExampleIndices(EditableMeaning meaning)
    {
        for (int i = 0; i < meaning.Examples.Count; i++)
        {
            meaning.Examples[i].Index = i + 1;
        }
    }

    private void AddMeaningButton_Click(object sender, RoutedEventArgs e)
    {
        var defaultPos = PosComboBox.SelectedItem is PartOfSpeech p ? p : PartOfSpeech.Noun;
        var newMeaning = new EditableMeaning { PartOfSpeech = defaultPos };
        newMeaning.PartOfSpeechChanged += Meaning_PartOfSpeechChanged;
        newMeaning.Examples.Add(new EditableExample());
        Meanings.Add(newMeaning);
        RefreshMeaningIndices();
        SyncChipsFromMeanings();
    }

    private void DeleteMeaningButton_Click(object sender, RoutedEventArgs e)
    {
        if (Meanings.Count > 1 && sender is Button btn && btn.DataContext is EditableMeaning m)
        {
            m.PartOfSpeechChanged -= Meaning_PartOfSpeechChanged;
            Meanings.Remove(m);
            RefreshMeaningIndices();
            SyncChipsFromMeanings();
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
            foreach (var m in Meanings)
            {
                if (m.Examples.Contains(ex))
                {
                    m.Examples.Remove(ex);
                    RefreshExampleIndices(m);
                    break;
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

        var term = TermTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            DialogInfoBar.Message = Loc.GetString("WordEditor_ErrTermRequired");
            DialogInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        var validMeanings = Meanings.Where(m => !string.IsNullOrWhiteSpace(m.DefinitionVi)).ToList();
        if (validMeanings.Count == 0)
        {
            DialogInfoBar.Message = Loc.GetString("WordEditor_ErrMeaningRequired");
            DialogInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        var pos = PosComboBox.SelectedItem is PartOfSpeech p ? p : (PosChips.FirstOrDefault(c => c.IsSelected)?.PartOfSpeech ?? PartOfSpeech.Noun);
        var level = LevelComboBox.SelectedItem is CefrLevel l ? l : CefrLevel.None;

        var meaningDtos = validMeanings.Select(m => new WordMeaningCreateDto(
            m.PartOfSpeech,
            m.DefinitionVi.Trim(),
            string.IsNullOrWhiteSpace(m.DefinitionEn) ? null : m.DefinitionEn.Trim(),
            string.IsNullOrWhiteSpace(m.Explanation) ? null : m.Explanation.Trim(),
            m.Examples.Where(ex => !string.IsNullOrWhiteSpace(ex.SentenceEn)).Select(ex => new ExampleSentenceCreateDto(
                ex.SentenceEn.Trim(),
                ex.SentenceVi.Trim(),
                null
            )).ToList()
        )).ToList();

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
            pos,
            string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
            meaningDtos,
            selectedTopics,
            selectedExamTags
        );
    }
}
