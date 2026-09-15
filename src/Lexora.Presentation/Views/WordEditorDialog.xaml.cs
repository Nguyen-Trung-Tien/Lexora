using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

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
    public WordCreateOrUpdateDto? ResultDto { get; private set; }

    private readonly Guid? _editingWordId;
    private readonly List<TopicDto> _allTopics;
    private readonly List<ExamTagDto> _allExamTags;

    public WordEditorDialog(
        WordDto? existingWord,
        IReadOnlyList<TopicDto> allTopics,
        IReadOnlyList<ExamTagDto> allExamTags)
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

        // Populate Combos
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
            defaultMeaning.Examples.Add(new EditableExample());
            Meanings.Add(defaultMeaning);
        }

        RefreshMeaningIndices();
        PrimaryButtonClick += WordEditorDialog_PrimaryButtonClick;
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
        newMeaning.Examples.Add(new EditableExample());
        Meanings.Add(newMeaning);
        RefreshMeaningIndices();
    }

    private void DeleteMeaningButton_Click(object sender, RoutedEventArgs e)
    {
        if (Meanings.Count > 1 && sender is Button btn && btn.DataContext is EditableMeaning m)
        {
            Meanings.Remove(m);
            RefreshMeaningIndices();
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

        if (Meanings.Count == 0 || string.IsNullOrWhiteSpace(Meanings[0].DefinitionVi))
        {
            DialogInfoBar.Message = Loc.GetString("WordEditor_ErrMeaningRequired");
            DialogInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        var pos = PosComboBox.SelectedItem is PartOfSpeech p ? p : PartOfSpeech.Noun;
        var level = LevelComboBox.SelectedItem is CefrLevel l ? l : CefrLevel.None;

        var meaningDtos = Meanings.Select(m => new WordMeaningCreateDto(
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

        var selectedTopicIds = TopicsListView.SelectedItems.Cast<TopicDto>().Select(t => t.Id).ToList();
        var selectedExamTagIds = ExamTagsListView.SelectedItems.Cast<ExamTagDto>().Select(et => et.Id).ToList();

        ResultDto = new WordCreateOrUpdateDto(
            _editingWordId,
            term,
            string.IsNullOrWhiteSpace(IpaUkTextBox.Text) ? null : IpaUkTextBox.Text.Trim(),
            string.IsNullOrWhiteSpace(IpaUsTextBox.Text) ? null : IpaUsTextBox.Text.Trim(),
            null,
            null,
            level,
            pos,
            string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
            meaningDtos,
            selectedTopicIds,
            selectedExamTagIds
        );
    }
}
