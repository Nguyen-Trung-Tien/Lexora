using Lexora.Application.DTOs.MemoryTips;
using Lexora.Application.Interfaces.MemoryTips;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels.MemoryTips;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace Lexora.Presentation.Views.MemoryTips;

public sealed partial class MemoryTipsPage : Page, ILocalizableView, IThemeAwareView
{
    public MemoryTipsViewModel ViewModel { get; }
    public ILocalizationService Loc { get; }
    private readonly IMemoryTipService _tipService;
    private readonly IThemeService _themeService;
    private readonly EventHandler<ElementTheme> _themeChangedHandler;

    public MemoryTipsPage()
    {
        NavigationCacheMode = NavigationCacheMode.Required;

        ViewModel = App.Current.Services.GetRequiredService<MemoryTipsViewModel>();
        var scopeProvider = App.GetScopeProviderFor(ViewModel);
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        _tipService = scopeProvider.GetRequiredService<IMemoryTipService>();
        _themeService = App.Current.Services.GetRequiredService<IThemeService>();

        InitializeComponent();

        if (_themeService.CurrentTheme != ElementTheme.Default)
        {
            RequestedTheme = _themeService.CurrentTheme;
        }

        ActualThemeChanged += Page_ActualThemeChanged;
        _themeChangedHandler = (s, theme) => OnThemeChanged();
        _themeService.ThemeChanged += _themeChangedHandler;

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void Page_ActualThemeChanged(FrameworkElement sender, object args) => OnThemeChanged();

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged -= Page_ActualThemeChanged;
        _themeService.ThemeChanged -= _themeChangedHandler;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RegisterShortcuts();
        OnLanguageChanged();
        OnThemeChanged();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        App.Current.Services.GetService<IShortcutManager>()?.UnregisterPageShortcuts(typeof(MemoryTipsPage));
    }

    private void RegisterShortcuts()
    {
        var sm = App.Current.Services.GetService<IShortcutManager>();
        if (sm == null) return;

        sm.RegisterPageShortcuts(typeof(MemoryTipsPage), new[]
        {
            new ShortcutDefinition
            {
                Id = "MemoryTips.AddTip",
                Gesture = new ShortcutGesture(ShortcutKey.N, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(MemoryTipsPage),
                DescriptionKey = "MemoryTips_AddButton",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = true,
                AllowWhenDialogActive = false,
                ExecuteAsync = () =>
                {
                    BtnAddTip_Click(this, new RoutedEventArgs());
                    return Task.CompletedTask;
                }
            },
            new ShortcutDefinition
            {
                Id = "MemoryTips.FocusSearch",
                Gesture = new ShortcutGesture(ShortcutKey.F, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(MemoryTipsPage),
                DescriptionKey = "MemoryTips_SearchPlaceholder",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = true,
                AllowWhenDialogActive = false,
                ExecuteAsync = () =>
                {
                    SearchBox.Focus(FocusState.Programmatic);
                    return Task.CompletedTask;
                }
            }
        });
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyLocalization();
            OnThemeChanged();
            await ViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MemoryTipsPage] OnPageLoaded error: {ex.Message}");
        }
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

    public void OnThemeChanged()
    {
        void UpdateThemeUI()
        {
            if (_themeService.CurrentTheme != ElementTheme.Default)
            {
                RequestedTheme = _themeService.CurrentTheme;
            }
            UpdateViewModeButtons();
            UpdateFilterPillStyles();
            Bindings.Update();
        }

        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateThemeUI();
        }
        else
        {
            DispatcherQueue.TryEnqueue(UpdateThemeUI);
        }
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = Loc.GetString("MemoryTips_Title");
        PageSubtitleText.Text = Loc.GetString("MemoryTips_Subtitle");

        TextViewSplit.Text = Loc.GetString("MemoryTips_ViewSplit");
        TextViewGrid.Text = Loc.GetString("MemoryTips_ViewGrid");
        ToolTipService.SetToolTip(BtnViewSplit, Loc.GetString("MemoryTips_TooltipSplit"));
        ToolTipService.SetToolTip(BtnViewGrid, Loc.GetString("MemoryTips_TooltipGrid"));

        TextFilterAll.Text = Loc.GetString("MemoryTips_FilterAll");
        TextFilterPinned.Text = Loc.GetString("MemoryTips_FilterPinned");
        TextFilterTraps.Text = Loc.GetString("MemoryTips_FilterTraps");

        TextAddTip.Text = Loc.GetString("MemoryTips_AddButton");
        ToolTipService.SetToolTip(BtnRefresh, Loc.GetString("Action_Refresh"));
        SearchBox.PlaceholderText = Loc.GetString("MemoryTips_SearchPlaceholder");
        ToolTipService.SetToolTip(BtnResetFilters, Loc.GetString("MemoryTips_ResetFilters"));
        TextTipsCountSuffix.Text = Loc.GetString("MemoryTips_TipsCount");

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
        ToolTipService.SetToolTip(BtnDetailCopyHook, Loc.GetString("MemoryTips_CopyHook"));
        ToolTipService.SetToolTip(BtnDetailPin, Loc.GetString("MemoryTips_Pin"));
        ToolTipService.SetToolTip(BtnDetailEdit, Loc.GetString("MemoryTips_Edit"));
        ToolTipService.SetToolTip(BtnDetailDelete, Loc.GetString("MemoryTips_Delete"));

        Bindings.Update();
    }

    private void UpdateViewModeButtons()
    {
        var cardBg = ThemeResourceHelper.GetThemeBrush("CardBackgroundFillColorDefaultBrush");
        var cardStroke = ThemeResourceHelper.GetThemeBrush("CardStrokeColorDefaultBrush");
        var accentText = ThemeResourceHelper.GetThemeBrush("AccentTextFillColorPrimaryBrush");
        var secondaryText = ThemeResourceHelper.GetThemeBrush("TextFillColorSecondaryBrush");
        var transparent = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        void ApplySegment(Button? btn, bool isActive)
        {
            if (btn == null) return;
            btn.Style = null;
            if (isActive)
            {
                btn.Background = cardBg;
                btn.BorderBrush = cardStroke;
                btn.BorderThickness = new Thickness(1);
                btn.Foreground = accentText;
            }
            else
            {
                btn.Background = transparent;
                btn.BorderBrush = transparent;
                btn.BorderThickness = new Thickness(0);
                btn.Foreground = secondaryText;
            }
        }

        ApplySegment(BtnViewSplit, ViewModel.IsSplitViewMode);
        ApplySegment(BtnViewGrid, ViewModel.IsGridViewMode);
    }

    private void UpdateFilterPillStyles()
    {
        SetPillStyle(ChipFilterAll, ViewModel.ActiveQuickFilter == 0);
        SetPillStyle(ChipFilterPinned, ViewModel.ActiveQuickFilter == 1);
        SetPillStyle(ChipFilterTraps, ViewModel.ActiveQuickFilter == 2);
    }

    private void SetPillStyle(Button? btn, bool isActive)
    {
        if (btn == null) return;
        btn.Style = (Style)App.Current.Resources[isActive ? "AccentButtonStyle" : "DefaultButtonStyle"];
    }

    private void BtnViewSplit_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetSplitView();
        UpdateViewModeButtons();
    }

    private void BtnViewGrid_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetGridView();
        UpdateViewModeButtons();
    }

    private async void PillFilterAll_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.FilterAllAsync();
        UpdateFilterPillStyles();
    }

    private async void PillFilterPinned_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.FilterPinnedAsync();
        UpdateFilterPillStyles();
    }

    private async void PillFilterTraps_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.FilterTrapsAsync();
        UpdateFilterPillStyles();
    }

    private InfoBarSeverity GetInfoBarSeverity(bool isError) =>
        isError ? InfoBarSeverity.Error : InfoBarSeverity.Success;

    private void TipsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TipsListView.SelectedItem is MemoryTipDto selected)
        {
            ViewModel.SelectedTip = selected;
        }
    }

    private void OnDetailCopyHookClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTip != null)
        {
            CopyMnemonicHook(ViewModel.SelectedTip.MnemonicHook);
        }
    }

    private void OnCardCopyHookClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: MemoryTipDto tip })
        {
            CopyMnemonicHook(tip.MnemonicHook);
        }
    }

    private void CopyMnemonicHook(string? hook)
    {
        if (string.IsNullOrWhiteSpace(hook)) return;
        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(hook);
            Clipboard.SetContent(dataPackage);
            ViewModel.ShowStatus(Loc.GetString("MemoryTips_CopySuccess"), isError: false);
        }
        catch
        {
            // Clipboard access fallback
        }
    }

    private async void OnDetailPinClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTip != null)
        {
            await ViewModel.TogglePinAsync(ViewModel.SelectedTip);
        }
    }

    private async void OnDetailEditClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTip != null)
        {
            await OpenEditorDialogAsync(ViewModel.SelectedTip);
        }
    }

    private async void OnDetailDeleteClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTip != null)
        {
            await ConfirmAndDeleteTipAsync(ViewModel.SelectedTip);
        }
    }

    private async void BtnAddTip_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new MemoryTipEditorDialog(Loc);
        var result = await dialog.ShowAsyncSafe(this.XamlRoot);
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
            await OpenEditorDialogAsync(tip);
        }
    }

    private async Task OpenEditorDialogAsync(MemoryTipDto tip)
    {
        var dialog = new MemoryTipEditorDialog(Loc, tip);
        var result = await dialog.ShowAsyncSafe(this.XamlRoot);
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
            await ConfirmAndDeleteTipAsync(tip);
        }
    }

    private async Task ConfirmAndDeleteTipAsync(MemoryTipDto tip)
    {
        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("MemoryTips_DeleteConfirmTitle"),
            Content = Loc.GetString("MemoryTips_DeleteConfirmMessage"),
            PrimaryButtonText = Loc.GetString("Action_Delete"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
        };

        var confirmResult = await confirmDialog.ShowAsyncSafe(this.XamlRoot);
        if (confirmResult == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteTipAsync(tip);
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

    private async void BtnSmartImport_Click(object sender, RoutedEventArgs e)
    {
        var pasteBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 200,
            PlaceholderText = Loc.GetString("SmartImport_PastePlaceholder")
        };

        var browseBtn = new Button { Content = Loc.GetString("SmartImport_BrowseFile") };
        browseBtn.Click += async (_, _) =>
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".json");
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add(".tsv");
            if (App.Current.MainWindow is not null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }
            var file = await picker.PickSingleFileAsync();
            if (file is null) return;
            var props = await file.GetBasicPropertiesAsync();
            if (props.Size > 20 * 1024 * 1024) return;
            pasteBox.Text = await Windows.Storage.FileIO.ReadTextAsync(file);
        };

        var sampleBtn = new Button { Content = Loc.GetString("SmartImport_ViewSample") };
        sampleBtn.Click += (_, _) => pasteBox.Text = ViewModel.GetSampleImportTemplate();

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        toolbar.Children.Add(browseBtn);
        toolbar.Children.Add(sampleBtn);

        var panel = new StackPanel { Spacing = 10, MinWidth = 460 };
        panel.Children.Add(toolbar);
        panel.Children.Add(pasteBox);

        var dialog = new ContentDialog
        {
            Title = Loc.GetString("MemoryTips_SmartImport"),
            Content = panel,
            PrimaryButtonText = Loc.GetString("Action_Import"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsyncSafe(this.XamlRoot);
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(pasteBox.Text))
        {
            await ViewModel.ImportTipsSmartAsync(pasteBox.Text);
        }
    }

    private void OpenHelp_Click(object sender, RoutedEventArgs e)
    {
        App.Current.Services.GetService<INavigationService>()?.Navigate(typeof(Help.HelpPage), "MemoryTips");
    }
}
