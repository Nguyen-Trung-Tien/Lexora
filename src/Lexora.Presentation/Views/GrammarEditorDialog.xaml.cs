using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class GrammarEditorDialog : ContentDialog
{
    private Guid? _editingRuleId;
    public GrammarRuleCreateOrUpdateDto? ResultDto { get; private set; }
    public ObservableCollection<GrammarExampleEditorModel> ExampleItems { get; } = new();

    public GrammarEditorDialog()
    {
        InitializeComponent();
        ExamplesItemsControl.ItemsSource = ExampleItems;

        LevelComboBox.ItemsSource = Enum.GetValues<CefrLevel>();
        ExamComboBox.ItemsSource = Enum.GetValues<ExamType>();

        PrimaryButtonClick += GrammarEditorDialog_PrimaryButtonClick;
    }

    public void InitializeForCreate()
    {
        Title = "Add New Grammar Rule";
        _editingRuleId = null;

        TitleTextBox.Text = string.Empty;
        StructureTextBox.Text = string.Empty;
        LevelComboBox.SelectedItem = CefrLevel.B1;
        ExamComboBox.SelectedItem = ExamType.General;
        TopicTextBox.Text = string.Empty;
        ExplanationViTextBox.Text = string.Empty;
        ExplanationEnTextBox.Text = string.Empty;
        CommonMistakesTextBox.Text = string.Empty;

        ExampleItems.Clear();
        ExampleItems.Add(new GrammarExampleEditorModel());
    }

    public void InitializeForEdit(GrammarRuleDto rule)
    {
        Title = "Edit Grammar Rule";
        _editingRuleId = rule.Id;

        TitleTextBox.Text = rule.Title;
        StructureTextBox.Text = rule.Structure;
        LevelComboBox.SelectedItem = rule.Level;
        ExamComboBox.SelectedItem = rule.ExamTarget;
        TopicTextBox.Text = rule.Topic ?? string.Empty;
        ExplanationViTextBox.Text = rule.ExplanationVi;
        ExplanationEnTextBox.Text = rule.ExplanationEn ?? string.Empty;
        CommonMistakesTextBox.Text = rule.CommonMistakes ?? string.Empty;

        ExampleItems.Clear();
        foreach (var ex in rule.Examples)
        {
            ExampleItems.Add(new GrammarExampleEditorModel
            {
                SentenceEn = ex.SentenceEn,
                SentenceVi = ex.SentenceVi,
                Note = ex.Note ?? string.Empty
            });
        }

        if (ExampleItems.Count == 0)
        {
            ExampleItems.Add(new GrammarExampleEditorModel());
        }
    }

    private void AddExampleButton_Click(object sender, RoutedEventArgs e)
    {
        ExampleItems.Add(new GrammarExampleEditorModel());
    }

    private void RemoveExampleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is GrammarExampleEditorModel item)
        {
            ExampleItems.Remove(item);
        }
    }

    private void GrammarEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        string title = TitleTextBox.Text.Trim();
        string structure = StructureTextBox.Text.Trim();
        string explanationVi = ExplanationViTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(structure) || string.IsNullOrWhiteSpace(explanationVi))
        {
            args.Cancel = true;
            return;
        }

        var examples = ExampleItems
            .Where(e => !string.IsNullOrWhiteSpace(e.SentenceEn) && !string.IsNullOrWhiteSpace(e.SentenceVi))
            .Select(e => new GrammarExampleCreateDto(e.SentenceEn.Trim(), e.SentenceVi.Trim(), string.IsNullOrWhiteSpace(e.Note) ? null : e.Note.Trim()))
            .ToList();

        ResultDto = new GrammarRuleCreateOrUpdateDto(
            Id: _editingRuleId,
            Title: title,
            Structure: structure,
            ExplanationVi: explanationVi,
            ExplanationEn: string.IsNullOrWhiteSpace(ExplanationEnTextBox.Text) ? null : ExplanationEnTextBox.Text.Trim(),
            CommonMistakes: string.IsNullOrWhiteSpace(CommonMistakesTextBox.Text) ? null : CommonMistakesTextBox.Text.Trim(),
            Level: LevelComboBox.SelectedItem is CefrLevel cl ? cl : CefrLevel.None,
            ExamTarget: ExamComboBox.SelectedItem is ExamType et ? et : ExamType.General,
            Topic: string.IsNullOrWhiteSpace(TopicTextBox.Text) ? null : TopicTextBox.Text.Trim(),
            Examples: examples
        );
    }
}

public partial class GrammarExampleEditorModel : ObservableObject
{
    [ObservableProperty]
    public partial string SentenceEn { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SentenceVi { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;
}
