using Lexora.Application.DTOs.MemoryTips;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views.MemoryTips;

public sealed partial class MemoryTipEditorDialog : ContentDialog, IThemeAwareView
{
    private readonly Guid? _editingId;

    public ILocalizationService Loc { get; }
    public CreateMemoryTipRequest? CreatedResult { get; private set; }
    public UpdateMemoryTipRequest? UpdatedResult { get; private set; }

    private record QuickTipTemplate(
        string Title,
        string MnemonicHook,
        string Explanation,
        string Example,
        string CommonTrap,
        TipCategory Category,
        MnemonicTechnique Technique,
        ExamTarget? ExamTarget,
        string Tags);

    public MemoryTipEditorDialog(
        ILocalizationService localizationService,
        MemoryTipDto? existing = null)
    {
        Loc = localizationService;
        InitializeComponent();

        ActualThemeChanged += (s, e) => OnThemeChanged();
        var themeService = App.Current?.Services?.GetService<IThemeService>();
        if (themeService != null && themeService.CurrentTheme != ElementTheme.Default)
        {
            RequestedTheme = themeService.CurrentTheme;
        }

        Title = existing is null
            ? Loc.GetString("MemoryTips_DialogAddTitle")
            : Loc.GetString("MemoryTips_DialogEditTitle");

        PrimaryButtonText = Loc.GetString("Action_Save");
        CloseButtonText = Loc.GetString("Action_Cancel");

        PopulateDropdowns();
        PopulateQuickTemplates();

        if (existing is not null)
        {
            _editingId = existing.Id;
            LoadExistingValues(existing);
        }
    }

    public void OnThemeChanged()
    {
        var themeService = App.Current?.Services?.GetService<IThemeService>();
        if (themeService != null && themeService.CurrentTheme != ElementTheme.Default)
        {
            RequestedTheme = themeService.CurrentTheme;
        }
        Bindings.Update();
    }

    private void PopulateDropdowns()
    {
        CategoryComboBox.Items.Clear();
        CategoryComboBox.Items.Add(Loc.GetString("TipCategory_Vocabulary"));
        CategoryComboBox.Items.Add(Loc.GetString("TipCategory_Grammar"));
        CategoryComboBox.Items.Add(Loc.GetString("TipCategory_Tenses"));
        CategoryComboBox.Items.Add(Loc.GetString("TipCategory_Expressions"));
        CategoryComboBox.Items.Add(Loc.GetString("TipCategory_ExamStrategy"));
        CategoryComboBox.Items.Add(Loc.GetString("TipCategory_General"));
        CategoryComboBox.SelectedIndex = 0;

        TechniqueComboBox.Items.Clear();
        TechniqueComboBox.Items.Add(Loc.GetString("MnemonicTechnique_Acrostic"));
        TechniqueComboBox.Items.Add(Loc.GetString("MnemonicTechnique_RhymeAndRhythm"));
        TechniqueComboBox.Items.Add(Loc.GetString("MnemonicTechnique_VisualImagery"));
        TechniqueComboBox.Items.Add(Loc.GetString("MnemonicTechnique_SoundAlike"));
        TechniqueComboBox.Items.Add(Loc.GetString("MnemonicTechnique_Storytelling"));
        TechniqueComboBox.Items.Add(Loc.GetString("MnemonicTechnique_Etymology"));
        TechniqueComboBox.Items.Add(Loc.GetString("MnemonicTechnique_EliminationRule"));
        TechniqueComboBox.Items.Add(Loc.GetString("MnemonicTechnique_Other"));
        TechniqueComboBox.SelectedIndex = 0;

        ExamTargetComboBox.Items.Clear();
        ExamTargetComboBox.Items.Add(Loc.GetString("MemoryTips_ExamAll"));
        ExamTargetComboBox.Items.Add("TOEIC");
        ExamTargetComboBox.Items.Add("IELTS");
        ExamTargetComboBox.Items.Add("VSTEP");
        ExamTargetComboBox.SelectedIndex = 0;
    }

    private void PopulateQuickTemplates()
    {
        var isVi = Loc.CurrentLanguage?.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ?? true;
        var templates = isVi
            ? new List<QuickTipTemplate>
            {
                new(
                    "Trật tự tính từ OpSASCOMP",
                    "Ông Sáu Ăn Súp Cua Ông Mập Phì (Op - S - A - S - C - O - M - P)",
                    "Opinion -> Size -> Age -> Shape -> Color -> Origin -> Material -> Purpose.",
                    "A lovely small old round black Vietnamese wooden dining table.",
                    "Đề thi TOEIC hay đổi chỗ Size và Color (ví dụ 'a black small car' là SAI).",
                    TipCategory.Grammar,
                    MnemonicTechnique.Acrostic,
                    ExamTarget.Toeic,
                    "Adjectives, Grammar, Word Order, TOEIC"),
                new(
                    "7 Liên từ kết hợp FANBOYS",
                    "FANBOYS: For - And - Nor - But - Or - Yet - So",
                    "Nối 2 mệnh đề độc lập. Trước FANBOYS bắt buộc phải có dấu phẩy (,).",
                    "I wanted to score 850+ in TOEIC, so I practiced daily with Lexora.",
                    "Lỗi Comma Splice: Nối 2 mệnh đề độc lập bằng dấu phẩy mà thiếu FANBOYS.",
                    TipCategory.Grammar,
                    MnemonicTechnique.Acrostic,
                    ExamTarget.Toeic,
                    "Conjunctions, Clauses, Grammar, FANBOYS"),
                new(
                    "Phân biệt SINCE vs FOR",
                    "SINCE là mốc (Point in time) — FOR là khoảng (Period of time)",
                    "'Since' đi với mốc thời gian cụ thể; 'For' đi với khoảng thời gian kéo dài.",
                    "She has lived here since 2020 / for 5 years.",
                    "Bẫy 'Since + 3 years' là sai hoàn toàn! Phải dùng 'For 3 years'.",
                    TipCategory.Tenses,
                    MnemonicTechnique.EliminationRule,
                    ExamTarget.Toeic,
                    "Tenses, Present Perfect, Since, For"),
                new(
                    "TOEIC Listening Part 1: Bẫy âm tương tự",
                    "Nghe giống từ khóa trong câu hỏi -> 90% LÀ BẪY (Similar Sound Distractor)!",
                    "Các đáp án lặp lại âm thanh tương tự từ khóa trong câu hỏi thường là bẫy đánh lạc hướng.",
                    "Câu hỏi có 'coffee', đáp án bẫy xuất hiện 'copy'; câu hỏi có 'department', đáp án bẫy có 'departure'.",
                    "Thí sinh có thói quen chọn đáp án có từ nghe quen tai nhất trong câu hỏi.",
                    TipCategory.ExamStrategy,
                    MnemonicTechnique.EliminationRule,
                    ExamTarget.Toeic,
                    "TOEIC, Listening, Strategy, Distractor")
            }
            : new List<QuickTipTemplate>
            {
                new(
                    "Adjective Order: OpSASCOMP",
                    "Op - S - A - S - C - O - M - P (Opinion, Size, Age, Shape, Color, Origin, Material, Purpose)",
                    "Royal Order of Adjectives: Opinion -> Size -> Age -> Shape -> Color -> Origin -> Material -> Purpose.",
                    "A lovely small old round black Vietnamese wooden dining table.",
                    "Exams often swap Size and Color (e.g., 'a black small car' is incorrect; use 'a small black car').",
                    TipCategory.Grammar,
                    MnemonicTechnique.Acrostic,
                    ExamTarget.Toeic,
                    "Adjectives, Grammar, Word Order, TOEIC"),
                new(
                    "Coordinating Conjunctions: FANBOYS",
                    "FANBOYS: For, And, Nor, But, Or, Yet, So",
                    "Connects two independent clauses. Always place a comma before the coordinating conjunction.",
                    "I wanted to score 850+ in TOEIC, so I practiced daily with Lexora.",
                    "Comma Splice Error: Joining two independent clauses with only a comma without FANBOYS.",
                    TipCategory.Grammar,
                    MnemonicTechnique.Acrostic,
                    ExamTarget.Toeic,
                    "Conjunctions, Clauses, Grammar, FANBOYS"),
                new(
                    "Distinguishing SINCE vs FOR",
                    "SINCE is a Starting point — FOR is a Full duration",
                    "'Since' denotes a specific point in time; 'For' denotes a duration or continuous period.",
                    "She has lived here since 2020 / for 5 years.",
                    "'Since 3 years' is completely wrong! Always use 'For 3 years'.",
                    TipCategory.Tenses,
                    MnemonicTechnique.EliminationRule,
                    ExamTarget.Toeic,
                    "Tenses, Present Perfect, Since, For"),
                new(
                    "TOEIC Listening Part 1: Similar Sound Trap",
                    "Sounds just like the prompt keyword -> 90% It's a Similar Sound Distractor!",
                    "Incorrect options often echo rhyming or similar-sounding words to distract test-takers.",
                    "Question mentions 'coffee', the distractor option uses 'copy'; question mentions 'walk', distractor uses 'work'.",
                    "Test-takers often impulsively pick whatever familiar word they recognize from the audio prompt.",
                    TipCategory.ExamStrategy,
                    MnemonicTechnique.EliminationRule,
                    ExamTarget.Toeic,
                    "TOEIC, Listening, Strategy, Distractor")
            };

        TemplateComboBox.ItemsSource = templates;
        TemplateComboBox.SelectedIndex = 0;
    }

    private void LoadExistingValues(MemoryTipDto existing)
    {
        TitleBox.Text = existing.Title;
        MnemonicHookBox.Text = existing.MnemonicHook;
        ExplanationBox.Text = existing.Explanation;
        ExampleBox.Text = existing.Example ?? string.Empty;
        CommonTrapBox.Text = existing.CommonTrap ?? string.Empty;
        TagsBox.Text = existing.Tags ?? string.Empty;
        IsPinnedCheckBox.IsChecked = existing.IsPinned;

        CategoryComboBox.SelectedIndex = (int)existing.Category;
        TechniqueComboBox.SelectedIndex = (int)existing.Technique;
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
        if (TemplateComboBox.SelectedItem is QuickTipTemplate template)
        {
            TitleBox.Text = template.Title;
            MnemonicHookBox.Text = template.MnemonicHook;
            ExplanationBox.Text = template.Explanation;
            ExampleBox.Text = template.Example;
            CommonTrapBox.Text = template.CommonTrap;
            TagsBox.Text = template.Tags;
            CategoryComboBox.SelectedIndex = (int)template.Category;
            TechniqueComboBox.SelectedIndex = (int)template.Technique;
            ExamTargetComboBox.SelectedIndex = template.ExamTarget switch
            {
                ExamTarget.Toeic => 1,
                ExamTarget.Ielts => 2,
                ExamTarget.Vstep => 3,
                _ => 0
            };
        }
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ValidationInfoBar.IsOpen = false;

        var title = TitleBox.Text?.Trim() ?? string.Empty;
        var hook = MnemonicHookBox.Text?.Trim() ?? string.Empty;
        var explanation = ExplanationBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowValidationError(Loc.GetString("MemoryTips_ValidationTitleRequired"));
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(hook))
        {
            ShowValidationError(Loc.GetString("MemoryTips_ValidationHookRequired"));
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(explanation))
        {
            ShowValidationError(Loc.GetString("MemoryTips_ValidationExplanationRequired"));
            args.Cancel = true;
            return;
        }

        var category = CategoryComboBox.SelectedIndex >= 0
            ? (TipCategory)CategoryComboBox.SelectedIndex
            : TipCategory.Vocabulary;

        var technique = TechniqueComboBox.SelectedIndex >= 0
            ? (MnemonicTechnique)TechniqueComboBox.SelectedIndex
            : MnemonicTechnique.Acrostic;

        ExamTarget? examTarget = ExamTargetComboBox.SelectedIndex switch
        {
            1 => ExamTarget.Toeic,
            2 => ExamTarget.Ielts,
            3 => ExamTarget.Vstep,
            _ => null
        };

        var example = string.IsNullOrWhiteSpace(ExampleBox.Text) ? null : ExampleBox.Text.Trim();
        var trap = string.IsNullOrWhiteSpace(CommonTrapBox.Text) ? null : CommonTrapBox.Text.Trim();
        var tags = string.IsNullOrWhiteSpace(TagsBox.Text) ? null : TagsBox.Text.Trim();
        var isPinned = IsPinnedCheckBox.IsChecked ?? false;

        if (_editingId.HasValue)
        {
            UpdatedResult = new UpdateMemoryTipRequest
            {
                Id = _editingId.Value,
                Title = title,
                MnemonicHook = hook,
                Explanation = explanation,
                Example = example,
                CommonTrap = trap,
                Category = category,
                Technique = technique,
                ExamTarget = examTarget,
                IsPinned = isPinned,
                Tags = tags
            };
        }
        else
        {
            CreatedResult = new CreateMemoryTipRequest
            {
                Title = title,
                MnemonicHook = hook,
                Explanation = explanation,
                Example = example,
                CommonTrap = trap,
                Category = category,
                Technique = technique,
                ExamTarget = examTarget,
                IsPinned = isPinned,
                Tags = tags
            };
        }
    }

    private void ShowValidationError(string message)
    {
        ValidationInfoBar.Message = message;
        ValidationInfoBar.IsOpen = true;
    }
}
