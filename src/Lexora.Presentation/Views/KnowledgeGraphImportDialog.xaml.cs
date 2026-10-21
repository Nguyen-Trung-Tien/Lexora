using Windows.Storage.Pickers;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class KnowledgeGraphImportDialog : ContentDialog
{
    private readonly IServiceScope? _scope;
    private readonly IKnowledgeGraphExportImportService _exportImportService;

    private int _currentStep = 1;
    private string _importContent = string.Empty;
    private string _selectedFormat = "json";
    private KnowledgeGraphImportAnalysisResultDto? _analysisResult;

    public bool WasCommitted { get; private set; }

    public ILocalizationService Loc { get; }

    public KnowledgeGraphImportDialog()
    {
        try
        {
            _scope = App.Current.Services.CreateScope();
            _exportImportService = _scope.ServiceProvider.GetRequiredService<IKnowledgeGraphExportImportService>();
            Loc = _scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        }
        catch
        {
            _exportImportService = App.Current.Services.GetRequiredService<IKnowledgeGraphExportImportService>();
            Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        }

        InitializeComponent();

        Unloaded += (s, e) => _scope?.Dispose();

        // Sync theme with main window
        var rootTheme = (App.Current.MainWindow?.Content as FrameworkElement)?.ActualTheme;
        if (rootTheme.HasValue && rootTheme.Value != ElementTheme.Default)
        {
            RequestedTheme = rootTheme.Value;
        }

        Title = Loc.GetString("KnowledgeGraph_Import_Title");
        PrimaryButtonText = Loc.GetString("KnowledgeGraph_Import_BtnNext");
        CloseButtonText = Loc.GetString("KnowledgeGraph_Import_BtnCancel");
        DefaultButton = ContentDialogButton.Primary;

        PrimaryButtonClick += KnowledgeGraphImportDialog_PrimaryButtonClick;
        SecondaryButtonClick += KnowledgeGraphImportDialog_SecondaryButtonClick;
    }

    private void BtnModeFile_Click(object sender, RoutedEventArgs e)
    {
        PanelFileMode.Visibility = Visibility.Visible;
        PanelPasteMode.Visibility = Visibility.Collapsed;
        if (App.Current.Resources.TryGetValue("AccentButtonStyle", out var accent))
            BtnModeFile.Style = (Style)accent;
        if (App.Current.Resources.TryGetValue("DefaultButtonStyle", out var def))
            BtnModePaste.Style = (Style)def;
    }

    private void BtnModePaste_Click(object sender, RoutedEventArgs e)
    {
        PanelFileMode.Visibility = Visibility.Collapsed;
        PanelPasteMode.Visibility = Visibility.Visible;
        if (App.Current.Resources.TryGetValue("AccentButtonStyle", out var accent))
            BtnModePaste.Style = (Style)accent;
        if (App.Current.Resources.TryGetValue("DefaultButtonStyle", out var def))
            BtnModeFile.Style = (Style)def;
    }

    private async void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".json");
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".txt");

            var mainWindow = App.Current.MainWindow;
            if (mainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            await DispatcherQueue.EnsureOnUIThreadAsync();
            if (file != null)
            {
                _importContent = await Windows.Storage.FileIO.ReadTextAsync(file);
                SelectedFileNameText.Text = file.Name;
                var ext = file.FileType.TrimStart('.').ToLowerInvariant();
                _selectedFormat = (ext == "csv" || ext == "txt") ? "csv" : "json";
            }
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            Step1InfoBar.IsOpen = true;
            Step1InfoBar.Message = $"Lỗi đọc tệp: {ex.Message}";
        }
    }

    private void FormatRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, RadioFormatCsv))
        {
            _selectedFormat = "csv";
        }
        else
        {
            _selectedFormat = "json";
        }
    }

    private void PasteSample_Click(object sender, RoutedEventArgs e)
    {
        var sample = _exportImportService.GetSampleTemplate(_selectedFormat);
        InputTextBox.Text = sample;
    }

    private async void KnowledgeGraphImportDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            if (_currentStep == 1)
            {
                args.Cancel = true;

                var content = PanelFileMode.Visibility == Visibility.Visible
                    ? _importContent
                    : InputTextBox.Text;

                if (string.IsNullOrWhiteSpace(content))
                {
                    Step1InfoBar.IsOpen = true;
                    Step1InfoBar.Message = Loc.GetString("KnowledgeGraph_Import_ErrEmpty");
                    return;
                }

                Step1InfoBar.IsOpen = false;
                IsPrimaryButtonEnabled = false;

                try
                {
                    _analysisResult = await _exportImportService.AnalyzeImportAsync(content, _selectedFormat);

                    if (_analysisResult.Items.Count == 0)
                    {
                        Step1InfoBar.IsOpen = true;
                        Step1InfoBar.Message = Loc.GetString("KnowledgeGraph_Import_ErrNoValid");
                        IsPrimaryButtonEnabled = true;
                        return;
                    }

                    // Transition to Step 2
                    _currentStep = 2;
                    PanelStep1.Visibility = Visibility.Collapsed;
                    PanelStep2.Visibility = Visibility.Visible;

                    Step1NumberBorder.Background = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBrush");
                    Step2NumberBorder.Background = ThemeResourceHelper.GetThemeBrush(this, "AccentFillColorDefaultBrush");
                    Step2NumberText.Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextOnAccentFillColorPrimaryBrush");

                    TextSummaryTotal.Text = _analysisResult.TotalCount.ToString();
                    TextSummaryValid.Text = _analysisResult.ValidCount.ToString();
                    TextSummaryMissing.Text = _analysisResult.MissingWordsCount.ToString();

                    PreviewListView.ItemsSource = _analysisResult.Items;

                    PrimaryButtonText = Loc.GetString("KnowledgeGraph_Import_BtnImport");
                    SecondaryButtonText = Loc.GetString("KnowledgeGraph_Import_BtnBack");
                    CloseButtonText = Loc.GetString("KnowledgeGraph_Import_BtnCancel");
                }
                catch (Exception ex)
                {
                    Step1InfoBar.IsOpen = true;
                    Step1InfoBar.Message = ex.Message;
                }
                finally
                {
                    IsPrimaryButtonEnabled = true;
                }
            }
            else if (_currentStep == 2)
            {
                args.Cancel = true;
                if (_analysisResult == null) return;

                IsPrimaryButtonEnabled = false;
                IsSecondaryButtonEnabled = false;

                try
                {
                    bool autoCreate = CheckAutoCreateMissing.IsChecked == true;
                    var commitResult = await _exportImportService.CommitImportAsync(_analysisResult, autoCreate);

                    if (commitResult.Success)
                    {
                        WasCommitted = true;
                        _currentStep = 3;

                        PanelStep2.Visibility = Visibility.Collapsed;
                        PanelStep3.Visibility = Visibility.Visible;

                        Step2NumberBorder.Background = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBrush");
                        Step3NumberBorder.Background = ThemeResourceHelper.GetThemeBrush(this, "AccentFillColorDefaultBrush");
                        Step3NumberText.Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextOnAccentFillColorPrimaryBrush");

                        ResultImportedCount.Text = commitResult.ImportedRelationsCount.ToString();
                        ResultUpdatedWordsCount.Text = commitResult.UpdatedWordsCount.ToString();
                        ResultCreatedWordsCount.Text = commitResult.CreatedWordsCount.ToString();

                        PrimaryButtonText = string.Empty;
                        SecondaryButtonText = string.Empty;
                        CloseButtonText = Loc.GetString("KnowledgeGraph_Import_BtnFinish");
                        IsPrimaryButtonEnabled = false;
                    }
                    else
                    {
                        Step2InfoBar.IsOpen = true;
                        Step2InfoBar.Severity = InfoBarSeverity.Error;
                        Step2InfoBar.Message = string.Join("; ", commitResult.Errors);
                    }
                }
                catch (Exception ex)
                {
                    Step2InfoBar.IsOpen = true;
                    Step2InfoBar.Severity = InfoBarSeverity.Error;
                    Step2InfoBar.Message = $"Lỗi ghi dữ liệu: {ex.Message}";
                }
                finally
                {
                    IsPrimaryButtonEnabled = true;
                    IsSecondaryButtonEnabled = true;
                }
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void KnowledgeGraphImportDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_currentStep == 2)
        {
            args.Cancel = true;
            _currentStep = 1;

            PanelStep2.Visibility = Visibility.Collapsed;
            PanelStep1.Visibility = Visibility.Visible;

            Step1NumberBorder.Background = ThemeResourceHelper.GetThemeBrush(this, "AccentFillColorDefaultBrush");
            Step2NumberBorder.Background = ThemeResourceHelper.GetThemeBrush(this, "LayerFillColorDefaultBrush");
            Step2NumberText.Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorSecondaryBrush");

            PrimaryButtonText = Loc.GetString("KnowledgeGraph_Import_BtnNext");
            SecondaryButtonText = string.Empty;
            CloseButtonText = Loc.GetString("KnowledgeGraph_Import_BtnCancel");
        }
    }
}
