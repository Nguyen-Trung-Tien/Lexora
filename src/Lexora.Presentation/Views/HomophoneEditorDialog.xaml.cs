using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class HomophoneEditorDialog : ContentDialog
{
    private readonly ILocalizationService _loc;
    private readonly HomophoneGroupDto? _existingGroup;

    public CreateHomophoneGroupDto? CreatedGroupResult { get; private set; }
    public UpdateHomophoneGroupDto? UpdatedGroupResult { get; private set; }

    public HomophoneEditorDialog(ILocalizationService loc, HomophoneGroupDto? existingGroup = null)
    {
        _loc = loc;
        _existingGroup = existingGroup;

        InitializeComponent();

        Title = existingGroup == null
            ? _loc.GetString("Homophones_CreateDialogTitle")
            : _loc.GetString("Homophones_EditDialogTitle");

        PrimaryButtonText = _loc.GetString("Action_Save");
        CloseButtonText = _loc.GetString("Action_Cancel");

        PrimaryButtonClick += OnPrimaryButtonClick;

        if (existingGroup != null)
        {
            LoadExistingData(existingGroup);
        }
    }

    private void LoadExistingData(HomophoneGroupDto g)
    {
        TitleTextBox.Text = g.Title;
        IpaTextBox.Text = g.PhoneticIpa;
        TipTextBox.Text = g.DifferentiationTip ?? string.Empty;

        // Select Type
        foreach (ComboBoxItem item in TypeComboBox.Items)
        {
            if (item.Tag?.ToString() == g.Type.ToString())
            {
                TypeComboBox.SelectedItem = item;
                break;
            }
        }

        // Select Level
        foreach (ComboBoxItem item in LevelComboBox.Items)
        {
            if (item.Tag?.ToString() == g.DifficultyLevel.ToString())
            {
                LevelComboBox.SelectedItem = item;
                break;
            }
        }

        // Select Exam Tag
        foreach (ComboBoxItem item in ExamComboBox.Items)
        {
            if (item.Tag?.ToString() == g.ExamTag.ToString())
            {
                ExamComboBox.SelectedItem = item;
                break;
            }
        }

        // Word 1
        if (g.Items.Count > 0)
        {
            var w1 = g.Items[0];
            Word1TermBox.Text = w1.Term;
            Word1MeaningBox.Text = w1.MeaningVi;
            Word1ExampleBox.Text = w1.ExampleSentence ?? string.Empty;
            Word1ExampleTransBox.Text = w1.ExampleTranslation ?? string.Empty;
            SelectPos(Word1PosBox, w1.PartOfSpeech);
        }

        // Word 2
        if (g.Items.Count > 1)
        {
            var w2 = g.Items[1];
            Word2TermBox.Text = w2.Term;
            Word2MeaningBox.Text = w2.MeaningVi;
            Word2ExampleBox.Text = w2.ExampleSentence ?? string.Empty;
            Word2ExampleTransBox.Text = w2.ExampleTranslation ?? string.Empty;
            SelectPos(Word2PosBox, w2.PartOfSpeech);
        }
    }

    private static void SelectPos(ComboBox cb, PartOfSpeech pos)
    {
        foreach (ComboBoxItem item in cb.Items)
        {
            if (item.Tag?.ToString() == pos.ToString())
            {
                cb.SelectedItem = item;
                break;
            }
        }
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var title = TitleTextBox.Text?.Trim() ?? string.Empty;
        var ipa = IpaTextBox.Text?.Trim() ?? string.Empty;
        var tip = TipTextBox.Text?.Trim();

        var word1Term = Word1TermBox.Text?.Trim() ?? string.Empty;
        var word1Meaning = Word1MeaningBox.Text?.Trim() ?? string.Empty;

        var word2Term = Word2TermBox.Text?.Trim() ?? string.Empty;
        var word2Meaning = Word2MeaningBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowError(_loc.GetString("Homophones_ValidationTitleRequired"));
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(word1Term) || string.IsNullOrWhiteSpace(word1Meaning) ||
            string.IsNullOrWhiteSpace(word2Term) || string.IsNullOrWhiteSpace(word2Meaning))
        {
            ShowError(_loc.GetString("Homophones_ValidationWordsRequired"));
            args.Cancel = true;
            return;
        }

        var type = Enum.TryParse<HomophoneType>((TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var t)
            ? t : HomophoneType.ExactHomophone;

        var level = Enum.TryParse<CefrLevel>((LevelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var lvl)
            ? lvl : CefrLevel.None;

        var exam = Enum.TryParse<ExamType>((ExamComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var ex)
            ? ex : ExamType.General;

        var pos1 = Enum.TryParse<PartOfSpeech>((Word1PosBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var p1)
            ? p1 : PartOfSpeech.Noun;

        var pos2 = Enum.TryParse<PartOfSpeech>((Word2PosBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var p2)
            ? p2 : PartOfSpeech.Verb;

        if (_existingGroup == null)
        {
            CreatedGroupResult = new CreateHomophoneGroupDto
            {
                Title = title,
                PhoneticIpa = ipa,
                Type = type,
                DifficultyLevel = level,
                ExamTag = exam,
                DifferentiationTip = tip,
                Items =
                [
                    new CreateHomophoneItemDto
                    {
                        Term = word1Term,
                        PartOfSpeech = pos1,
                        MeaningVi = word1Meaning,
                        ExampleSentence = Word1ExampleBox.Text?.Trim(),
                        ExampleTranslation = Word1ExampleTransBox.Text?.Trim()
                    },
                    new CreateHomophoneItemDto
                    {
                        Term = word2Term,
                        PartOfSpeech = pos2,
                        MeaningVi = word2Meaning,
                        ExampleSentence = Word2ExampleBox.Text?.Trim(),
                        ExampleTranslation = Word2ExampleTransBox.Text?.Trim()
                    }
                ]
            };
        }
        else
        {
            UpdatedGroupResult = new UpdateHomophoneGroupDto
            {
                Id = _existingGroup.Id,
                Title = title,
                PhoneticIpa = ipa,
                Type = type,
                DifficultyLevel = level,
                ExamTag = exam,
                DifferentiationTip = tip,
                Items =
                [
                    new UpdateHomophoneItemDto
                    {
                        Term = word1Term,
                        PartOfSpeech = pos1,
                        MeaningVi = word1Meaning,
                        ExampleSentence = Word1ExampleBox.Text?.Trim(),
                        ExampleTranslation = Word1ExampleTransBox.Text?.Trim()
                    },
                    new UpdateHomophoneItemDto
                    {
                        Term = word2Term,
                        PartOfSpeech = pos2,
                        MeaningVi = word2Meaning,
                        ExampleSentence = Word2ExampleBox.Text?.Trim(),
                        ExampleTranslation = Word2ExampleTransBox.Text?.Trim()
                    }
                ]
            };
        }
    }

    private void ShowError(string message)
    {
        DialogInfoBar.Message = message;
        DialogInfoBar.IsOpen = true;
    }
}
