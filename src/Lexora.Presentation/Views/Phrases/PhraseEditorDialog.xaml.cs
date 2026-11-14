using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views.Phrases;

public sealed partial class PhraseEditorDialog : ContentDialog
{
    private readonly ILocalizationService _loc;
    private readonly ConversationPhraseDto? _existingPhrase;

    public ILocalizationService Loc => _loc;
    public CreateConversationPhraseDto? CreatedResult { get; private set; }
    public UpdateConversationPhraseDto? UpdatedResult { get; private set; }

    public PhraseEditorDialog(ILocalizationService loc, ConversationPhraseDto? existingPhrase = null)
    {
        _loc = loc;
        _existingPhrase = existingPhrase;

        InitializeComponent();

        Title = existingPhrase == null
            ? _loc.GetString("Phrases_CreateDialogTitle")
            : _loc.GetString("Phrases_EditDialogTitle");

        PrimaryButtonText = _loc.GetString("Action_Save");
        CloseButtonText = _loc.GetString("Action_Cancel");

        PrimaryButtonClick += OnPrimaryButtonClick;

        if (existingPhrase != null)
        {
            LoadExistingData(existingPhrase);
        }
    }

    private void LoadExistingData(ConversationPhraseDto p)
    {
        // Category
        CategoryComboBox.Text = p.Category;
        foreach (ComboBoxItem item in CategoryComboBox.Items)
        {
            if (string.Equals(item.Content?.ToString(), p.Category, StringComparison.OrdinalIgnoreCase))
            {
                CategoryComboBox.SelectedItem = item;
                break;
            }
        }

        TagTextBox.Text = p.ContextTag ?? string.Empty;
        PromptTextBox.Text = p.PromptSentence;
        PromptTransTextBox.Text = p.PromptTranslation;
        ResponseTextBox.Text = p.SampleResponse;
        ResponseTransTextBox.Text = p.ResponseTranslation;
        SuggestionsTextBox.Text = p.AdditionalSuggestions ?? string.Empty;

        // Level
        foreach (ComboBoxItem item in LevelComboBox.Items)
        {
            if (item.Tag?.ToString() == p.CefrLevel.ToString())
            {
                LevelComboBox.SelectedItem = item;
                break;
            }
        }
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var category = CategoryComboBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(category) && CategoryComboBox.SelectedItem is ComboBoxItem cbi)
        {
            category = cbi.Content?.ToString()?.Trim();
        }

        var prompt = PromptTextBox.Text?.Trim();
        var promptTrans = PromptTransTextBox.Text?.Trim();
        var response = ResponseTextBox.Text?.Trim();
        var responseTrans = ResponseTransTextBox.Text?.Trim();
        var suggestions = SuggestionsTextBox.Text?.Trim() ?? string.Empty;
        var tag = TagTextBox.Text?.Trim() ?? string.Empty;

        var selectedLevel = CefrLevel.B1;
        if (LevelComboBox.SelectedItem is ComboBoxItem levelItem &&
            Enum.TryParse<CefrLevel>(levelItem.Tag?.ToString(), out var parsedLevel))
        {
            selectedLevel = parsedLevel;
        }

        // Validation
        if (string.IsNullOrWhiteSpace(category))
        {
            ShowValidationError(_loc.GetString("Phrases_ValidationCategoryRequired"));
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            ShowValidationError(_loc.GetString("Phrases_ValidationPromptRequired"));
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(promptTrans))
        {
            ShowValidationError(_loc.GetString("Phrases_ValidationPromptTransRequired"));
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            ShowValidationError(_loc.GetString("Phrases_ValidationResponseRequired"));
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(responseTrans))
        {
            ShowValidationError(_loc.GetString("Phrases_ValidationResponseTransRequired"));
            args.Cancel = true;
            return;
        }

        DialogInfoBar.IsOpen = false;

        if (_existingPhrase == null)
        {
            CreatedResult = new CreateConversationPhraseDto
            {
                Category = category,
                ContextTag = tag,
                PromptSentence = prompt,
                PromptTranslation = promptTrans,
                SampleResponse = response,
                ResponseTranslation = responseTrans,
                AdditionalSuggestions = suggestions,
                CefrLevel = selectedLevel,
                IsFavorite = false
            };
        }
        else
        {
            UpdatedResult = new UpdateConversationPhraseDto
            {
                Id = _existingPhrase.Id,
                Category = category,
                ContextTag = tag,
                PromptSentence = prompt,
                PromptTranslation = promptTrans,
                SampleResponse = response,
                ResponseTranslation = responseTrans,
                AdditionalSuggestions = suggestions,
                CefrLevel = selectedLevel,
                IsFavorite = _existingPhrase.IsFavorite
            };
        }
    }

    private void ShowValidationError(string message)
    {
        DialogInfoBar.Message = message;
        DialogInfoBar.IsOpen = true;
    }

    private void AppendSnippet(string snippet)
    {
        if (string.IsNullOrWhiteSpace(SuggestionsTextBox.Text))
        {
            SuggestionsTextBox.Text = snippet;
        }
        else
        {
            SuggestionsTextBox.Text += "\n" + snippet;
        }
    }

    private void BtnInsertFormal_Click(object sender, RoutedEventArgs e)
    {
        AppendSnippet("• Formal Alternative: \"May I respectfully inquire about... ?\"\n(Cách diễn đạt trang trọng, lịch thiệp trong công sở/đối tác)");
    }

    private void BtnInsertCasual_Click(object sender, RoutedEventArgs e)
    {
        AppendSnippet("• Casual / Native phrasing: \"What's the latest on this? How's it going?\"\n(Cách nói thân mật hàng ngày, gần gũi)");
    }

    private void BtnInsertSynonyms_Click(object sender, RoutedEventArgs e)
    {
        AppendSnippet("• Synonyms & Vocabulary Expansion:\n  - Thay vì dùng từ quen thuộc, hãy thử các tính từ phong phú hơn (e.g. impressive, remarkable, solid).");
    }

    private void BtnInsertFollowUp_Click(object sender, RoutedEventArgs e)
    {
        AppendSnippet("• Follow-up Question to extend dialogue:\n  \"By the way, what were the key takeaways from your team?\"");
    }
}
