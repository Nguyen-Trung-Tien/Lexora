using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.Views;

public sealed partial class SmartImportDialog : ContentDialog
{
    private readonly Func<string, string>? _sampleProvider;
    private readonly ILocalizationService _loc;

    public string? ContentToImport { get; private set; }
    public string? FileNameHint { get; private set; }
    public ImportConflictResolution ConflictResolution => RadioConflictOverwrite.IsChecked == true
        ? ImportConflictResolution.Overwrite
        : ImportConflictResolution.Skip;
    public DataFormat DetectedFormat { get; private set; } = DataFormat.Unknown;

    public SmartImportDialog(Func<string, string>? sampleProvider = null)
    {
        InitializeComponent();
        _sampleProvider = sampleProvider;
        _loc = App.Current.Services.GetRequiredService<ILocalizationService>();

        // Apply theme from current main window
        var rootTheme = (App.Current.MainWindow?.Content as FrameworkElement)?.ActualTheme;
        if (rootTheme.HasValue)
        {
            RequestedTheme = rootTheme.Value;
        }

        ApplyLocalization();
        UpdateMode(true);
        UpdateDetectionBadge();

        PrimaryButtonClick += SmartImportDialog_PrimaryButtonClick;
    }

    private void ApplyLocalization()
    {
        Title = _loc.GetString("SmartImport_Title");
        SubtitleText.Text = _loc.GetString("SmartImport_Subtitle");
        TextModeFile.Text = _loc.GetString("SmartImport_TabFile");
        TextModePaste.Text = _loc.GetString("SmartImport_TabPaste");
        TextBrowseFile.Text = _loc.GetString("SmartImport_BrowseFile");
        TextSelectedFile.Text = string.Format(_loc.GetString("SmartImport_SelectedFile"), "—");
        InputTextBox.PlaceholderText = _loc.GetString("SmartImport_PastePlaceholder");
        TextPasteClipboard.Text = _loc.GetString("SmartImport_PasteFromClipboard");
        TextViewSample.Text = _loc.GetString("SmartImport_ViewSample");
        TextConflictTitle.Text = _loc.GetString("SmartImport_ConflictTitle");
        RadioConflictSkip.Content = _loc.GetString("SmartImport_ConflictSkip");
        RadioConflictOverwrite.Content = _loc.GetString("SmartImport_ConflictOverwrite");

        PrimaryButtonText = _loc.GetString("SmartImport_BtnImport");
        CloseButtonText = _loc.GetString("Action_Cancel");
    }

    private void UpdateMode(bool isFileMode)
    {
        if (isFileMode)
        {
            PanelFile.Visibility = Visibility.Visible;
            PanelPaste.Visibility = Visibility.Collapsed;
            BtnModeFile.Style = App.Current.Resources["AccentButtonStyle"] as Style;
            BtnModePaste.Style = App.Current.Resources["DefaultButtonStyle"] as Style;
        }
        else
        {
            PanelFile.Visibility = Visibility.Collapsed;
            PanelPaste.Visibility = Visibility.Visible;
            BtnModeFile.Style = App.Current.Resources["DefaultButtonStyle"] as Style;
            BtnModePaste.Style = App.Current.Resources["AccentButtonStyle"] as Style;
        }
        EvaluateContent();
    }

    private void BtnModeFile_Click(object sender, RoutedEventArgs e) => UpdateMode(true);

    private void BtnModePaste_Click(object sender, RoutedEventArgs e) => UpdateMode(false);

    private async void BtnBrowseFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".json");
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add(".tsv");

            var mainWindow = App.Current.MainWindow;
            if (mainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                FileNameHint = file.Name;
                ContentToImport = await Windows.Storage.FileIO.ReadTextAsync(file);
                TextSelectedFile.Text = string.Format(_loc.GetString("SmartImport_SelectedFile"), file.Name);
                EvaluateContent();
            }
        }
        catch (Exception ex)
        {
            TextSelectedFile.Text = $"Lỗi đọc file: {ex.Message}";
        }
    }

    private async void BtnPasteClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dataPackage = Clipboard.GetContent();
            if (dataPackage != null && dataPackage.Contains(StandardDataFormats.Text))
            {
                var text = await dataPackage.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    InputTextBox.Text = text;
                }
            }
        }
        catch
        {
            // Clipboard access fallback
        }
    }

    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PanelPaste.Visibility == Visibility.Visible)
        {
            ContentToImport = InputTextBox.Text;
            FileNameHint = null;
            EvaluateContent();
        }
    }

    private void EvaluateContent()
    {
        var raw = ContentToImport;
        DetectedFormat = SmartContentDetector.DetectFormat(raw, FileNameHint);
        UpdateDetectionBadge();
    }

    private void UpdateDetectionBadge()
    {
        var accentBrush = ThemeResourceHelper.GetThemeBrush("AccentTextFillColorPrimaryBrush") ?? new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
        var successBrush = ThemeResourceHelper.GetThemeBrush("SystemFillColorSuccessBrush") ?? new SolidColorBrush(Microsoft.UI.Colors.ForestGreen);
        var cautionBrush = ThemeResourceHelper.GetThemeBrush("SystemFillColorCautionBrush") ?? new SolidColorBrush(Microsoft.UI.Colors.Orange);

        switch (DetectedFormat)
        {
            case DataFormat.Json:
                IconFormatBadge.Glyph = "\uE73E"; // Checkmark
                IconFormatBadge.Foreground = successBrush;
                TextFormatBadge.Text = string.Format(_loc.GetString("SmartImport_FormatDetected"), "JSON");
                break;
            case DataFormat.Csv:
                IconFormatBadge.Glyph = "\uE73E"; // Checkmark
                IconFormatBadge.Foreground = successBrush;
                TextFormatBadge.Text = string.Format(_loc.GetString("SmartImport_FormatDetected"), "CSV");
                break;
            case DataFormat.Txt:
                IconFormatBadge.Glyph = "\uE73E"; // Checkmark
                IconFormatBadge.Foreground = accentBrush;
                TextFormatBadge.Text = string.Format(_loc.GetString("SmartImport_FormatDetected"), "TXT / TSV / Anki / Quizlet");
                break;
            default:
                IconFormatBadge.Glyph = "\uE946"; // Info
                IconFormatBadge.Foreground = cautionBrush;
                TextFormatBadge.Text = _loc.GetString("SmartImport_FormatUnknown");
                break;
        }
    }

    private void MenuItemSampleJson_Click(object sender, RoutedEventArgs e) => LoadSample("json");
    private void MenuItemSampleCsv_Click(object sender, RoutedEventArgs e) => LoadSample("csv");
    private void MenuItemSampleTxt_Click(object sender, RoutedEventArgs e) => LoadSample("txt");

    private void LoadSample(string format)
    {
        if (_sampleProvider != null)
        {
            var sample = _sampleProvider(format);
            UpdateMode(false);
            InputTextBox.Text = sample;
        }
    }

    private void SmartImportDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (PanelPaste.Visibility == Visibility.Visible)
        {
            ContentToImport = InputTextBox.Text;
        }

        if (string.IsNullOrWhiteSpace(ContentToImport))
        {
            args.Cancel = true;
            TextFormatBadge.Text = _loc.GetString("SmartImport_NoContent");
            IconFormatBadge.Glyph = "\uEA39"; // Error cross
            var criticalBrush = ThemeResourceHelper.GetThemeBrush("SystemFillColorCriticalBrush") ?? new SolidColorBrush(Microsoft.UI.Colors.Red);
            IconFormatBadge.Foreground = criticalBrush;
        }
    }
}
