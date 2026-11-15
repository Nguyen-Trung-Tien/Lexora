using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views.Phrases;

/// <summary>
/// ContentDialog for creating or editing a ConversationPhrase with scenario,
/// formality register, exam target, variations, and Vietnamese learner notes.
/// </summary>
public sealed partial class PhraseEditorDialog : ContentDialog
{
    private readonly Guid? _editingId;

    public ILocalizationService Loc { get; }
    public CreateConversationPhraseDto? CreatedResult { get; private set; }
    public UpdateConversationPhraseDto? UpdatedResult { get; private set; }

    public PhraseEditorDialog(
        ILocalizationService localizationService,
        IReadOnlyList<string>? availableCategories = null,
        IReadOnlyList<PhraseSmartSuggestionDto>? quickTemplates = null,
        ConversationPhraseDto? existing = null)
    {
        Loc = localizationService;
        InitializeComponent();

        Title = existing is null
            ? Loc.GetString("Phrases_AddTitle")
            : Loc.GetString("Phrases_EditTitle");
        PrimaryButtonText = Loc.GetString("Action_Save");
        CloseButtonText = Loc.GetString("Action_Cancel");

        PopulateCategories(availableCategories);
        if (quickTemplates is not null && quickTemplates.Count > 0)
        {
            TemplateComboBox.ItemsSource = quickTemplates;
            TemplateComboBox.SelectedIndex = 0;
        }

        if (existing is not null)
        {
            _editingId = existing.Id;
            LoadExistingValues(existing);
        }
        else if (CategoryComboBox.Items.Count > 0)
        {
            CategoryComboBox.SelectedIndex = 0;
        }
    }

    private void PopulateCategories(IReadOnlyList<string>? availableCategories)
    {
        var items = (availableCategories ?? [])
            .Concat(["Workplace & Office", "Business & TOEIC", "Academic & IELTS Speaking", "Travel & Airport", "Daily Life & Social", "VSTEP Practical Situations"])
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var item in items)
        {
            CategoryComboBox.Items.Add(item);
        }
    }

    private void LoadExistingValues(ConversationPhraseDto existing)
    {
        CategoryComboBox.Text = existing.Category;
        ContextTagBox.Text = existing.ContextTag;
        PromptSentenceBox.Text = existing.PromptSentence;
        PromptTranslationBox.Text = existing.PromptTranslation;
        SampleResponseBox.Text = existing.SampleResponse;
        ResponseTranslationBox.Text = existing.ResponseTranslation;
        KeyPhrasesBox.Text = existing.KeyPhrases;
        FormalVariationBox.Text = existing.FormalVariation;
        CasualVariationBox.Text = existing.CasualVariation;
        CommonMistakesBox.Text = existing.CommonMistakesVi;
        AdditionalSuggestionsBox.Text = existing.AdditionalSuggestions;
        IsFavoriteCheckBox.IsChecked = existing.IsFavorite;

        CefrComboBox.SelectedIndex = existing.CefrLevel switch
        {
            CefrLevel.A1 => 0,
            CefrLevel.A2 => 1,
            CefrLevel.B1 => 2,
            CefrLevel.B2 => 3,
            CefrLevel.C1 => 4,
            CefrLevel.C2 => 5,
            _ => 2
        };

        RegisterComboBox.SelectedIndex = existing.Register switch
        {
            PhraseRegister.Neutral => 0,
            PhraseRegister.Formal => 1,
            PhraseRegister.Casual => 2,
            PhraseRegister.Idiomatic => 3,
            _ => 0
        };

        ExamTargetComboBox.SelectedIndex = existing.ExamTarget switch
        {
            ExamTarget.Toeic => 1,
            ExamTarget.Ielts => 2,
            ExamTarget.Vstep => 3,
            _ => 0
        };
    }

    private void OnApplyTemplateClick(object sender, RoutedEventArgs e)
    {
        if (TemplateComboBox.SelectedItem is not PhraseSmartSuggestionDto template)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(template.CategoryTag) &&
            !template.CategoryTag.Equals("Formal", StringComparison.OrdinalIgnoreCase) &&
            !template.CategoryTag.Equals("Casual", StringComparison.OrdinalIgnoreCase))
        {
            CategoryComboBox.Text = template.CategoryTag;
        }
        else if (string.IsNullOrWhiteSpace(CategoryComboBox.Text))
        {
            CategoryComboBox.Text = template.Register == PhraseRegister.Formal
                ? "Workplace & Office"
                : "Daily Life & Social";
        }

        if (string.IsNullOrWhiteSpace(SampleResponseBox.Text))
        {
            SampleResponseBox.Text = template.TextToAppend;
        }
        else if (string.IsNullOrWhiteSpace(PromptSentenceBox.Text))
        {
            PromptSentenceBox.Text = template.TextToAppend;
        }

        KeyPhrasesBox.Text = string.IsNullOrWhiteSpace(KeyPhrasesBox.Text)
            ? template.KeyPhrases
            : KeyPhrasesBox.Text;
        FormalVariationBox.Text = string.IsNullOrWhiteSpace(FormalVariationBox.Text)
            ? template.FormalVariation
            : FormalVariationBox.Text;
        CasualVariationBox.Text = string.IsNullOrWhiteSpace(CasualVariationBox.Text)
            ? template.CasualVariation
            : CasualVariationBox.Text;
        CommonMistakesBox.Text = string.IsNullOrWhiteSpace(CommonMistakesBox.Text)
            ? template.CommonMistakesVi
            : CommonMistakesBox.Text;

        RegisterComboBox.SelectedIndex = (int)template.Register;
        ExamTargetComboBox.SelectedIndex = template.ExamTarget switch
        {
            ExamTarget.Toeic => 1,
            ExamTarget.Ielts => 2,
            ExamTarget.Vstep => 3,
            _ => 0
        };
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ValidationInfoBar.IsOpen = false;

        var prompt = PromptSentenceBox.Text?.Trim() ?? string.Empty;
        var promptTrans = PromptTranslationBox.Text?.Trim() ?? string.Empty;
        var response = SampleResponseBox.Text?.Trim() ?? string.Empty;
        var responseTrans = ResponseTranslationBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(prompt) ||
            string.IsNullOrWhiteSpace(promptTrans) ||
            string.IsNullOrWhiteSpace(response) ||
            string.IsNullOrWhiteSpace(responseTrans))
        {
            ValidationInfoBar.Title = Loc.GetString("Phrases_ValidationRequiredTitle");
            ValidationInfoBar.Message = Loc.GetString("Phrases_ValidationRequiredMsg");
            ValidationInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        var category = string.IsNullOrWhiteSpace(CategoryComboBox.Text)
            ? (CategoryComboBox.SelectedItem as string ?? "Daily Life")
            : CategoryComboBox.Text.Trim();

        var level = CefrComboBox.SelectedIndex switch
        {
            0 => CefrLevel.A1,
            1 => CefrLevel.A2,
            2 => CefrLevel.B1,
            3 => CefrLevel.B2,
            4 => CefrLevel.C1,
            5 => CefrLevel.C2,
            _ => CefrLevel.B1
        };

        var register = RegisterComboBox.SelectedIndex switch
        {
            1 => PhraseRegister.Formal,
            2 => PhraseRegister.Casual,
            3 => PhraseRegister.Idiomatic,
            _ => PhraseRegister.Neutral
        };

        ExamTarget? examTarget = ExamTargetComboBox.SelectedIndex switch
        {
            1 => ExamTarget.Toeic,
            2 => ExamTarget.Ielts,
            3 => ExamTarget.Vstep,
            _ => null
        };

        if (_editingId.HasValue)
        {
            UpdatedResult = new UpdateConversationPhraseDto
            {
                Id = _editingId.Value,
                Category = category,
                ContextTag = ContextTagBox.Text?.Trim() ?? string.Empty,
                PromptSentence = prompt,
                PromptTranslation = promptTrans,
                SampleResponse = response,
                ResponseTranslation = responseTrans,
                AdditionalSuggestions = AdditionalSuggestionsBox.Text?.Trim(),
                CefrLevel = level,
                Register = register,
                ExamTarget = examTarget,
                KeyPhrases = KeyPhrasesBox.Text?.Trim(),
                FormalVariation = FormalVariationBox.Text?.Trim(),
                CasualVariation = CasualVariationBox.Text?.Trim(),
                CommonMistakesVi = CommonMistakesBox.Text?.Trim(),
                IsFavorite = IsFavoriteCheckBox.IsChecked == true
            };
        }
        else
        {
            CreatedResult = new CreateConversationPhraseDto
            {
                Category = category,
                ContextTag = ContextTagBox.Text?.Trim() ?? string.Empty,
                PromptSentence = prompt,
                PromptTranslation = promptTrans,
                SampleResponse = response,
                ResponseTranslation = responseTrans,
                AdditionalSuggestions = AdditionalSuggestionsBox.Text?.Trim(),
                CefrLevel = level,
                Register = register,
                ExamTarget = examTarget,
                KeyPhrases = KeyPhrasesBox.Text?.Trim(),
                FormalVariation = FormalVariationBox.Text?.Trim(),
                CasualVariation = CasualVariationBox.Text?.Trim(),
                CommonMistakesVi = CommonMistakesBox.Text?.Trim(),
                IsFavorite = IsFavoriteCheckBox.IsChecked == true
            };
        }
    }
}
