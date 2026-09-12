using System.Collections.ObjectModel;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class WordEditorDialog : ContentDialog
{
    public class EditableMeaning
    {
        public PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Noun;
        public string DefinitionVi { get; set; } = string.Empty;
        public string? DefinitionEn { get; set; }
        public string? Explanation { get; set; }
        public ObservableCollection<EditableExample> Examples { get; } = new();
    }

    public class EditableExample
    {
        public string SentenceEn { get; set; } = string.Empty;
        public string SentenceVi { get; set; } = string.Empty;
    }

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
        InitializeComponent();

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

        MeaningsItemsControl.ItemsSource = Meanings;

        if (existingWord != null)
        {
            Title = $"Edit: {existingWord.Term}";
            TermTextBox.Text = existingWord.Term;
            IpaUkTextBox.Text = existingWord.PhoneticUk ?? "";
            IpaUsTextBox.Text = existingWord.PhoneticUs ?? "";
            NotesTextBox.Text = existingWord.Notes ?? "";

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
            Title = "Add New Word";
            // Default 1 meaning
            Meanings.Add(new EditableMeaning());
        }

        PrimaryButtonClick += WordEditorDialog_PrimaryButtonClick;
    }

    private void AddMeaningButton_Click(object sender, RoutedEventArgs e)
    {
        Meanings.Add(new EditableMeaning());
    }

    private void DeleteMeaningButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is EditableMeaning m)
        {
            Meanings.Remove(m);
        }
    }

    private void AddExampleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is EditableMeaning m)
        {
            m.Examples.Add(new EditableExample());
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
                    break;
                }
            }
        }
    }

    private void WordEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorMessageTextBlock.Visibility = Visibility.Collapsed;

        var term = TermTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            ErrorMessageTextBlock.Text = "Please enter a word or term.";
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        if (Meanings.Count == 0 || string.IsNullOrWhiteSpace(Meanings[0].DefinitionVi))
        {
            ErrorMessageTextBlock.Text = "Please provide at least one Vietnamese definition.";
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
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
