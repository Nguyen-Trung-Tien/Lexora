using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Lexora.Presentation.Views.Phrases;

/// <summary>
/// Presentation Page for Conversation Phrases featuring Responsive Master-Detail layout,
/// audio playback indicators, connected transitions, Roleplay SM-2/FSRS mode, and import/export.
/// </summary>
public sealed partial class PhrasesPage : Page, ILocalizableView, IThemeAwareView
{
    private const ulong MaxImportBytes = 20UL * 1024 * 1024;
    private bool _hasLoadedOnce;
    private bool _isOpeningDialog;

    public PhrasesViewModel ViewModel { get; }
    public ILocalizationService Loc { get; }

    public PhrasesPage()
        : this(
            App.Current.Services.GetRequiredService<PhrasesViewModel>(),
            App.Current.Services.GetRequiredService<ILocalizationService>())
    {
    }

    public PhrasesPage(
        PhrasesViewModel viewModel,
        ILocalizationService localizationService)
    {
        ViewModel = viewModel;
        Loc = localizationService;
        InitializeComponent();
        DataContext = ViewModel;

        UpdateFilterPillStyles();

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(PhrasesViewModel.QuickFilterIndex)
                or nameof(PhrasesViewModel.OnlyFavorites)
                or nameof(PhrasesViewModel.OnlyDueForReview))
            {
                UpdateFilterPillStyles();
            }
        };
    }

    public void OnLanguageChanged()
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            Bindings.Update();
            UpdateFilterPillStyles();
        }
        else
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Bindings.Update();
                UpdateFilterPillStyles();
            });
        }
    }

    public void OnThemeChanged()
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateFilterPillStyles();
        }
        else
        {
            DispatcherQueue.TryEnqueue(UpdateFilterPillStyles);
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        try
        {
            if (!_hasLoadedOnce)
            {
                _hasLoadedOnce = true;
                await ViewModel.LoadDataAsync();
            }
            else
            {
                await ViewModel.RefreshPhrasesAsync(resetPage: false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesPage] OnNavigatedTo error: {ex.Message}");
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.StopAudio();
    }

    private void OnPhraseListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedPhrase is null || MasterListView.SelectedItem is null)
        {
            return;
        }

        try
        {
            var container = MasterListView.ContainerFromItem(MasterListView.SelectedItem) as UIElement;
            if (container is not null && DetailCardRoot.Visibility == Visibility.Visible)
            {
                var anim = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhraseDetailCard", container);
                anim.TryStart(DetailCardRoot);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesPage] ConnectedAnimation skipped: {ex.Message}");
        }
    }

    private async void OnAddPhraseClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsSubmitting || _isOpeningDialog) return;
        _isOpeningDialog = true;
        try
        {
            var templates = await ViewModel.GetQuickTemplatesAsync(ViewModel.SelectedCategory);
            var dialog = new PhraseEditorDialog(Loc, ViewModel.EditorCategories.ToList(), templates);
            var result = await dialog.ShowAsyncSafe(this.XamlRoot);
            if (result == ContentDialogResult.Primary && dialog.CreatedResult is not null)
            {
                await ViewModel.CreatePhraseAsync(dialog.CreatedResult);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesPage] OnAddPhraseClick error: {ex.Message}");
        }
        finally
        {
            _isOpeningDialog = false;
        }
    }

    private async void OnEditSelectedClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPhrase is null || ViewModel.IsSubmitting || _isOpeningDialog) return;
        _isOpeningDialog = true;
        try
        {
            var templates = await ViewModel.GetQuickTemplatesAsync(ViewModel.SelectedPhrase.Category);
            var dialog = new PhraseEditorDialog(Loc, ViewModel.EditorCategories.ToList(), templates, ViewModel.SelectedPhrase);
            var result = await dialog.ShowAsyncSafe(this.XamlRoot);
            if (result == ContentDialogResult.Primary && dialog.UpdatedResult is not null)
            {
                await ViewModel.UpdatePhraseAsync(dialog.UpdatedResult);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesPage] OnEditSelectedClick error: {ex.Message}");
        }
        finally
        {
            _isOpeningDialog = false;
        }
    }

    private async void OnDeleteSelectedClick(object sender, RoutedEventArgs e)
    {
        var phrase = ViewModel.SelectedPhrase;
        if (phrase is null || ViewModel.IsSubmitting || _isOpeningDialog) return;
        _isOpeningDialog = true;
        try
        {
            var confirm = new ContentDialog
            {
                Title = Loc.GetString("Phrases_DeleteConfirmTitle"),
                Content = string.Format(Loc.GetString("Phrases_DeleteConfirmMsg"), phrase.PromptSentence),
                PrimaryButtonText = Loc.GetString("Action_Delete"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirm.ShowAsyncSafe(this.XamlRoot);
            if (result == ContentDialogResult.Primary)
            {
                await Task.Yield();
                await ViewModel.DeletePhraseAsync(phrase);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesPage] OnDeleteSelectedClick error: {ex.Message}");
        }
        finally
        {
            _isOpeningDialog = false;
        }
    }

    private async void OnStartRoleplayClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (MasterDetailWorkspace.Visibility == Visibility.Visible && ViewModel.HasSelectedPhrase)
            {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PhraseToRoleplay", DetailCardRoot);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhrasesPage] Roleplay ConnectedAnimation skipped: {ex.Message}");
        }

        await ViewModel.StartRoleplayCommand.ExecuteAsync(null);
        await Task.Yield();

        try
        {
            var anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("PhraseToRoleplay");
            if (anim is not null && ViewModel.IsRoleplayMode)
            {
                anim.TryStart(RoleplayWorkspace);
            }
        }
        catch
        {
            // Ignore when animation could not attach on initial transition
        }
    }

    private void OnFilterAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectFilterAll();
        UpdateFilterPillStyles();
    }

    private void OnFilterFavoritesClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectFilterFavorites();
        UpdateFilterPillStyles();
    }

    private void OnFilterDueClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectFilterDue();
        UpdateFilterPillStyles();
    }

    private void UpdateFilterPillStyles()
    {
        var accentStyle = (Style)App.Current.Resources["AccentButtonStyle"];
        var defaultStyle = (Style)App.Current.Resources["DefaultButtonStyle"];

        BtnFilterAll.Style = ViewModel.QuickFilterIndex == 0 ? accentStyle : defaultStyle;
        BtnFilterFavorites.Style = ViewModel.QuickFilterIndex == 1 ? accentStyle : defaultStyle;
        BtnFilterDue.Style = ViewModel.QuickFilterIndex == 2 ? accentStyle : defaultStyle;
    }

    private void DetailScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is ScrollViewer sv && sv.ActualWidth > 0 && DetailCardRoot is not null)
        {
            double availableWidth = sv.ViewportWidth > 0 ? sv.ViewportWidth : (sv.ActualWidth - 16);
            DetailCardRoot.MaxWidth = Math.Max(0, availableWidth);
        }
    }

    private async void OnDetailPlayFullConversationClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPhrase is not null)
        {
            await ViewModel.PlayFullConversationCommand.ExecuteAsync(ViewModel.SelectedPhrase);
        }
    }

    private async void OnDetailToggleFavoriteClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPhrase is not null)
        {
            await ViewModel.ToggleFavoriteAsync(ViewModel.SelectedPhrase);
        }
    }

    private void OnStopAudioClick(object sender, RoutedEventArgs e)
    {
        ViewModel.StopAudio();
    }

    private async void OnCardPlayPromptClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ConversationPhraseDto dto)
        {
            await ViewModel.PlayPromptAudioCommand.ExecuteAsync(dto);
        }
    }

    private async void OnCardToggleFavoriteClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ConversationPhraseDto dto)
        {
            await ViewModel.ToggleFavoriteCommand.ExecuteAsync(dto);
        }
    }

    private async void OnDetailPlayPromptClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPhrase is not null)
        {
            await ViewModel.PlayPromptAudioCommand.ExecuteAsync(ViewModel.SelectedPhrase);
        }
    }

    private async void OnDetailPlayResponseClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPhrase is not null)
        {
            await ViewModel.PlayResponseAudioCommand.ExecuteAsync(ViewModel.SelectedPhrase);
        }
    }

    private void OnCopyPromptClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyTextCommand.Execute(ViewModel.SelectedPhrase?.PromptSentence);
    }

    private void OnCopyResponseClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyTextCommand.Execute(ViewModel.SelectedPhrase?.SampleResponse);
    }

    private async void OnEnqueueSelectedReviewClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPhrase is not null)
        {
            await ViewModel.EnqueuePhraseForReviewCommand.ExecuteAsync(ViewModel.SelectedPhrase);
        }
    }

    private void OnCopyTemplateClick(object sender, RoutedEventArgs e)
    {
        var jsonSample = ViewModel.GetSampleTemplate("json");
        ViewModel.CopyTextCommand.Execute(jsonSample);
    }

    private async void OnSmartImportClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var pasteBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 200,
                PlaceholderText = Loc.GetString("SmartImport_PastePlaceholder")
            };

            var conflictCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                SelectedIndex = 0
            };
            conflictCombo.Items.Add(Loc.GetString("Phrases_ImportSkipDuplicates"));
            conflictCombo.Items.Add(Loc.GetString("Phrases_ImportOverwriteDuplicates"));

            var browseBtn = new Button
            {
                Content = Loc.GetString("SmartImport_BrowseFile")
            };
            string? selectedFileName = null;
            browseBtn.Click += async (_, _) =>
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".json");
                picker.FileTypeFilter.Add(".csv");
                picker.FileTypeFilter.Add(".txt");
                picker.FileTypeFilter.Add(".tsv");
                InitializePickerWindow(picker);
                var file = await picker.PickSingleFileAsync();
                if (file is null) return;
                var props = await file.GetBasicPropertiesAsync();
                if (props.Size > MaxImportBytes)
                {
                    return;
                }
                selectedFileName = file.Name;
                pasteBox.Text = await FileIO.ReadTextAsync(file);
            };

            var sampleBtn = new Button
            {
                Content = Loc.GetString("SmartImport_ViewSample")
            };
            sampleBtn.Click += (_, _) =>
            {
                pasteBox.Text = ViewModel.GetSampleTemplate("json");
            };

            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            toolbar.Children.Add(browseBtn);
            toolbar.Children.Add(sampleBtn);

            var panel = new StackPanel { Spacing = 10, MinWidth = 460 };
            panel.Children.Add(toolbar);
            panel.Children.Add(pasteBox);
            panel.Children.Add(conflictCombo);

            var dialog = new ContentDialog
            {
                Title = Loc.GetString("Phrases_SmartImport"),
                Content = panel,
                PrimaryButtonText = Loc.GetString("Action_Import"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsyncSafe(this.XamlRoot);
            if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(pasteBox.Text))
            {
                return;
            }

            var resolution = conflictCombo.SelectedIndex == 1
                ? ImportConflictResolution.Overwrite
                : ImportConflictResolution.Skip;

            await ViewModel.ImportPhrasesContentAsync(pasteBox.Text, selectedFileName, resolution);
        }
        catch (Exception ex)
        {
            await ShowInfoDialogAsync(Loc.GetString("Phrases_ErrorImport"), ex.Message);
        }
    }

    private async void OnImportFileClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".json");
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add(".tsv");

            InitializePickerWindow(picker);

            var file = await picker.PickSingleFileAsync();
            await DispatcherQueue.EnsureOnUIThreadAsync();
            if (file is null) return;

            var props = await file.GetBasicPropertiesAsync();
            if (props.Size > MaxImportBytes)
            {
                await ShowInfoDialogAsync(Loc.GetString("Phrases_ErrorImport"), Loc.GetString("Phrases_ImportTooLarge"));
                return;
            }

            var conflictDialog = new ContentDialog
            {
                Title = Loc.GetString("Phrases_ImportConflictTitle"),
                Content = Loc.GetString("Phrases_ImportConflictMsg"),
                PrimaryButtonText = Loc.GetString("Phrases_ImportSkipDuplicates"),
                SecondaryButtonText = Loc.GetString("Phrases_ImportOverwriteDuplicates"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Primary
            };

            var choice = await conflictDialog.ShowAsyncSafe(this.XamlRoot);
            if (choice == ContentDialogResult.None) return;

            var resolution = choice == ContentDialogResult.Secondary
                ? ImportConflictResolution.Overwrite
                : ImportConflictResolution.Skip;

            await Task.Yield();
            var content = await FileIO.ReadTextAsync(file);
            await ViewModel.ImportPhrasesContentAsync(content, file.Name, resolution);
        }
        catch (Exception ex)
        {
            await ShowInfoDialogAsync(Loc.GetString("Phrases_ErrorImport"), ex.Message);
        }
    }

    private void OnExportJsonClick(object sender, RoutedEventArgs e)
        => _ = ExportWithPickerAsync(DataFormat.Json, ".json", "JSON (*.json)", "lexora-phrases");

    private void OnExportCsvClick(object sender, RoutedEventArgs e)
        => _ = ExportWithPickerAsync(DataFormat.Csv, ".csv", "CSV (*.csv)", "lexora-phrases");

    private void OnExportTxtClick(object sender, RoutedEventArgs e)
        => _ = ExportWithPickerAsync(DataFormat.Txt, ".txt", "Tab-delimited Text (*.txt)", "lexora-phrases");

    private async Task ExportWithPickerAsync(DataFormat format, string extension, string description, string defaultName)
    {
        try
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = defaultName
            };
            savePicker.FileTypeChoices.Add(description, [extension]);
            InitializePickerWindow(savePicker);

            var file = await savePicker.PickSaveFileAsync();
            await DispatcherQueue.EnsureOnUIThreadAsync();
            if (file is null) return;

            var dir = Path.GetDirectoryName(file.Path) ?? Path.GetTempPath();
            var exportedPath = await ViewModel.ExportPhrasesToFileAsync(file.Path, dir, format);
            await ShowInfoDialogAsync(
                Loc.GetString("Phrases_ExportSuccessTitle"),
                string.Format(Loc.GetString("Phrases_ExportSuccessMsg"), exportedPath));
        }
        catch (Exception ex)
        {
            await ShowInfoDialogAsync(Loc.GetString("Phrases_ErrorExport"), ex.Message);
        }
    }

    private async Task ShowInfoDialogAsync(string title, string message)
    {
        await Task.Yield();
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = Loc.GetString("Action_Close")
        };
        await dialog.ShowAsyncSafe(this.XamlRoot);
    }

    private static void InitializePickerWindow(object picker)
    {
        var mainWindow = App.Current.MainWindow;
        if (mainWindow is not null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }
    }

    private void OpenHelp_Click(object sender, RoutedEventArgs e)
    {
        App.Current.Services.GetService<INavigationService>()?.Navigate(typeof(Help.HelpPage), "Phrases");
    }
}
