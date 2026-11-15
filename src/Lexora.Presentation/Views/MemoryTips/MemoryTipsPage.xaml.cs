using Lexora.Application.DTOs.MemoryTips;
using Lexora.Application.Interfaces.MemoryTips;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels.MemoryTips;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views.MemoryTips;

public sealed partial class MemoryTipsPage : Page, ILocalizableView
{
    public MemoryTipsViewModel ViewModel { get; }
    public ILocalizationService Loc { get; }
    private readonly IMemoryTipService _tipService;

    public MemoryTipsPage()
    {
        NavigationCacheMode = NavigationCacheMode.Required;

        ViewModel = App.Current.Services.GetRequiredService<MemoryTipsViewModel>();
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        _tipService = App.Current.Services.GetRequiredService<IMemoryTipService>();

        InitializeComponent();

        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalization();
        await ViewModel.InitializeAsync();
    }

    public void OnLanguageChanged()
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            ApplyLocalization();
        }
        else
        {
            DispatcherQueue.TryEnqueue(ApplyLocalization);
        }
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = Loc.GetString("MemoryTips_Title");
        PageSubtitleText.Text = Loc.GetString("MemoryTips_Subtitle");
        StatTotalLabel.Text = Loc.GetString("MemoryTips_StatTotal");
        StatPinnedLabel.Text = Loc.GetString("MemoryTips_StatPinned");
        StatTrapsLabel.Text = Loc.GetString("MemoryTips_StatTraps");
        TextAddTip.Text = Loc.GetString("MemoryTips_AddButton");
        ToolTipService.SetToolTip(BtnRefresh, Loc.GetString("Action_Refresh"));
        SearchBox.PlaceholderText = Loc.GetString("MemoryTips_SearchPlaceholder");
        TextOnlyPinned.Text = Loc.GetString("MemoryTips_OnlyPinned");
        ToolTipService.SetToolTip(BtnResetFilters, Loc.GetString("MemoryTips_ResetFilters"));
        EmptyTitleText.Text = Loc.GetString("MemoryTips_EmptyTitle");
        EmptySubtitleText.Text = Loc.GetString("MemoryTips_EmptySubtitle");

        ComboCatAll.Content = Loc.GetString("MemoryTips_CategoryAll");
        ComboCatVocab.Content = Loc.GetString("TipCategory_Vocabulary");
        ComboCatGrammar.Content = Loc.GetString("TipCategory_Grammar");
        ComboCatTense.Content = Loc.GetString("TipCategory_Tenses");
        ComboCatExpr.Content = Loc.GetString("TipCategory_Expressions");
        ComboCatExam.Content = Loc.GetString("TipCategory_ExamStrategy");
        ComboCatPronounce.Content = Loc.GetString("TipCategory_General");

        ComboTechAll.Content = Loc.GetString("MemoryTips_TechniqueAll");
        ComboTechAcrostic.Content = Loc.GetString("MnemonicTechnique_Acrostic");
        ComboTechRhyme.Content = Loc.GetString("MnemonicTechnique_RhymeAndRhythm");
        ComboTechVisual.Content = Loc.GetString("MnemonicTechnique_VisualImagery");
        ComboTechStory.Content = Loc.GetString("MnemonicTechnique_Storytelling");
        ComboTechKeyword.Content = Loc.GetString("MnemonicTechnique_SoundAlike");
        ComboTechContrast.Content = Loc.GetString("MnemonicTechnique_Etymology");
        ComboTechChunking.Content = Loc.GetString("MnemonicTechnique_EliminationRule");
        ComboTechOther.Content = Loc.GetString("MnemonicTechnique_Other");

        ComboExamAll.Content = Loc.GetString("MemoryTips_ExamAll");

        TextPageSizeLabel.Text = Loc.GetString("MemoryTips_PageSize");
        TextPageSizeSuffix.Text = Loc.GetString("MemoryTips_PageSizeSuffix");
        ToolTipService.SetToolTip(BtnFirstPage, Loc.GetString("MemoryTips_FirstPage"));
        ToolTipService.SetToolTip(BtnPrevPage, Loc.GetString("MemoryTips_PrevPage"));
        ToolTipService.SetToolTip(BtnNextPage, Loc.GetString("MemoryTips_NextPage"));
        ToolTipService.SetToolTip(BtnLastPage, Loc.GetString("MemoryTips_LastPage"));

        Bindings.Update();
    }

    private InfoBarSeverity GetInfoBarSeverity(bool isError) =>
        isError ? InfoBarSeverity.Error : InfoBarSeverity.Success;

    private async void BtnAddTip_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new MemoryTipEditorDialog(Loc)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.CreatedResult is not null)
        {
            try
            {
                var created = await _tipService.CreateAsync(dialog.CreatedResult);
                await ViewModel.OnTipCreatedOrUpdatedAsync(created);
                ViewModel.ShowStatus(Loc.GetString("MemoryTips_AddSuccess"), isError: false);
            }
            catch (Exception ex)
            {
                ViewModel.ShowStatus(ex.Message, isError: true);
            }
        }
    }

    private async void OnItemEditClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: MemoryTipDto tip })
        {
            var dialog = new MemoryTipEditorDialog(Loc, tip)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.UpdatedResult is not null)
            {
                try
                {
                    var updated = await _tipService.UpdateAsync(dialog.UpdatedResult);
                    await ViewModel.OnTipCreatedOrUpdatedAsync(updated);
                    ViewModel.ShowStatus(Loc.GetString("MemoryTips_UpdateSuccess"), isError: false);
                }
                catch (Exception ex)
                {
                    ViewModel.ShowStatus(ex.Message, isError: true);
                }
            }
        }
    }

    private async void OnItemPinClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: MemoryTipDto tip })
        {
            await ViewModel.TogglePinAsync(tip);
        }
    }

    private async void OnItemDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: MemoryTipDto tip })
        {
            var confirmDialog = new ContentDialog
            {
                Title = Loc.GetString("MemoryTips_DeleteConfirmTitle"),
                Content = Loc.GetString("MemoryTips_DeleteConfirmMessage"),
                PrimaryButtonText = Loc.GetString("Action_Delete"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var confirmResult = await confirmDialog.ShowAsync();
            if (confirmResult == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteTipAsync(tip);
                ViewModel.ShowStatus(Loc.GetString("MemoryTips_DeleteSuccess"), isError: false);
            }
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private async void PageSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var size))
        {
            await ViewModel.ChangePageSizeAsync(size);
        }
    }
}
