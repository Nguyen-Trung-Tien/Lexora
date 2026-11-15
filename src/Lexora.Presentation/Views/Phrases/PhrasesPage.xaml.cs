using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Lexora.Presentation.Views.Phrases;

/// <summary>
/// Presentation Page for Conversation Phrases featuring Responsive Master-Detail layout,
/// audio playback indicators, connected transitions, Roleplay SM-2/FSRS mode, and import/export.
/// </summary>
public sealed partial class PhrasesPage : Page
{
    private const ulong MaxImportBytes = 20UL * 1024 * 1024;
    private bool _hasLoadedOnce;

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
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
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
        if (ViewModel.IsSubmitting) return;

        var templates = await ViewModel.GetQuickTemplatesAsync(ViewModel.SelectedCategory);
        var dialog = new PhraseEditorDialog(Loc, ViewModel.EditorCategories.ToList(), templates);
        var result = await dialog.ShowAsyncSafe(this.XamlRoot);
        if (result == ContentDialogResult.Primary && dialog.CreatedResult is not null)
        {
            await ViewModel.CreatePhraseAsync(dialog.CreatedResult);
        }
    }

    private async void OnEditSelectedClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPhrase is null || ViewModel.IsSubmitting) return;

        var templates = await ViewModel.GetQuickTemplatesAsync(ViewModel.SelectedPhrase.Category);
        var dialog = new PhraseEditorDialog(Loc, ViewModel.EditorCategories.ToList(), templates, ViewModel.SelectedPhrase);
        var result = await dialog.ShowAsyncSafe(this.XamlRoot);
        if (result == ContentDialogResult.Primary && dialog.UpdatedResult is not null)
        {
            await ViewModel.UpdatePhraseAsync(dialog.UpdatedResult);
        }
    }

    private async void OnDeleteSelectedClick(object sender, RoutedEventArgs e)
    {
        var phrase = ViewModel.SelectedPhrase;
        if (phrase is null || ViewModel.IsSubmitting) return;

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
}
