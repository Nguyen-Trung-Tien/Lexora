using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class GrammarEditorDialog : ContentDialog
{
    private Guid? _editingRuleId;
    public ILocalizationService Loc { get; }
    public GrammarRuleCreateOrUpdateDto? ResultDto { get; private set; }
    public ObservableCollection<GrammarExampleEditorModel> ExampleItems { get; } = new();

    public GrammarEditorDialog()
    {
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();

        InitializeComponent();

        if (App.Current?.Services?.GetService<IThemeService>() is { } themeService && themeService.CurrentTheme != ElementTheme.Default)
        {
            RequestedTheme = themeService.CurrentTheme;
        }
        else if (App.Current?.MainWindow?.Content is FrameworkElement rootElem && rootElem.ActualTheme != ElementTheme.Default)
        {
            RequestedTheme = rootElem.ActualTheme;
        }

        PrimaryButtonText = Loc.GetString("GrammarEditor_Save");
        CloseButtonText = Loc.GetString("GrammarEditor_Cancel");
        DialogInfoBar.Title = Loc.GetString("GrammarEditor_ValidationTitle");

        ExamplesItemsControl.ItemsSource = ExampleItems;

        LevelComboBox.ItemsSource = Enum.GetValues<CefrLevel>();
        ExamComboBox.ItemsSource = Enum.GetValues<ExamType>();

        PrimaryButtonClick += GrammarEditorDialog_PrimaryButtonClick;

        KeyDown += (s, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.S && (ShortcutManager.GetCurrentModifiers() & ShortcutModifiers.Control) == ShortcutModifiers.Control)
            {
                e.Handled = true;
                if (TrySaveInternal())
                {
                    this.Hide();
                }
            }
        };
    }

    public void InitializeForCreate()
    {
        Title = Loc.GetString("GrammarEditor_TitleAdd");
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
        RefreshExampleIndices();
    }

    public void InitializeForEdit(GrammarRuleDto rule)
    {
        Title = string.Format(Loc.GetString("GrammarEditor_TitleEdit"), rule.Title);
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

        RefreshExampleIndices();
    }

    private void RefreshExampleIndices()
    {
        for (int i = 0; i < ExampleItems.Count; i++)
        {
            ExampleItems[i].Index = i + 1;
            ExampleItems[i].CanRemove = ExampleItems.Count > 1;
        }
    }

    private void AddExampleButton_Click(object sender, RoutedEventArgs e)
    {
        ExampleItems.Add(new GrammarExampleEditorModel());
        RefreshExampleIndices();
    }

    private void RemoveExampleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ExampleItems.Count > 1 && sender is Button btn && btn.DataContext is GrammarExampleEditorModel item)
        {
            ExampleItems.Remove(item);
            RefreshExampleIndices();
        }
    }

    private bool TrySaveInternal()
    {
        DialogInfoBar.IsOpen = false;

        string title = TitleTextBox.Text.Trim();
        string structure = StructureTextBox.Text.Trim();
        string explanationVi = ExplanationViTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            DialogInfoBar.Message = Loc.GetString("GrammarEditor_ErrTitleRequired");
            DialogInfoBar.IsOpen = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(structure))
        {
            DialogInfoBar.Message = Loc.GetString("GrammarEditor_ErrStructureRequired");
            DialogInfoBar.IsOpen = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(explanationVi))
        {
            DialogInfoBar.Message = Loc.GetString("GrammarEditor_ErrExplanationViRequired");
            DialogInfoBar.IsOpen = true;
            return false;
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

        return true;
    }

    private void GrammarEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!TrySaveInternal())
        {
            args.Cancel = true;
        }
    }
}

public partial class GrammarExampleEditorModel : ObservableObject
{
    private static ILocalizationService? Localization => App.Current?.Services?.GetService<ILocalizationService>();

    [ObservableProperty]
    public partial int Index { get; set; } = 1;

    public string ExampleBadge => string.Format(Localization?.GetString("GrammarEditor_ExampleBadge") ?? "Ví dụ #{0}", Index);

    [ObservableProperty]
    public partial bool CanRemove { get; set; } = true;

    [ObservableProperty]
    public partial string SentenceEn { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SentenceVi { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    public string HeaderSentenceEn => Localization?.GetString("GrammarEditor_HeaderSentenceEn") ?? "English Sentence *";
    public string PlaceholderSentenceEn => Localization?.GetString("GrammarEditor_PlaceholderSentenceEn") ?? "e.g. I have been working here for three years.";
    public string HeaderSentenceVi => Localization?.GetString("GrammarEditor_HeaderSentenceVi") ?? "Vietnamese Translation *";
    public string PlaceholderSentenceVi => Localization?.GetString("GrammarEditor_PlaceholderSentenceVi") ?? "Bản dịch tiếng Việt...";
    public string HeaderNote => Localization?.GetString("GrammarEditor_HeaderNote") ?? "Grammar Note";
    public string PlaceholderNote => Localization?.GetString("GrammarEditor_PlaceholderNote") ?? "Ghi chú ngữ pháp bổ sung...";
    public string TooltipRemoveExample => Localization?.GetString("GrammarEditor_TooltipRemoveExample") ?? "Remove this example";

    partial void OnIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ExampleBadge));
    }
}
