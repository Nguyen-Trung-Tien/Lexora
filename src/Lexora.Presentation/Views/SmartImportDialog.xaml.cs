using System.Collections.ObjectModel;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.Views;

public class SmartImportPreviewItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public int RowNumber { get; set; }
    public string Term { get; set; } = string.Empty;
    public string? PartOfSpeech { get; set; }
    public bool HasPartOfSpeech => !string.IsNullOrWhiteSpace(PartOfSpeech);
    public string Meaning { get; set; } = string.Empty;
    public string? Pronunciation { get; set; }
    public string? ExampleSentence { get; set; }
    public string? ExampleTranslation { get; set; }
    public string? TopicName { get; set; }
    public string? CefrLevel { get; set; }
    public string? ExamType { get; set; }

    public bool IsDuplicate { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }

    public string StatusText { get; set; } = string.Empty;
    public Brush? StatusBackgroundBrush { get; set; }
    public Brush? StatusBorderBrush { get; set; }
    public Brush? StatusForegroundBrush { get; set; }

    public SmartImportItemDto OriginalDto { get; set; } = null!;
}

public sealed partial class SmartImportDialog : ContentDialog
{
    private readonly IServiceScope? _scope;
    private readonly Func<string, string>? _sampleProvider;
    private readonly bool _isGrammarMode;
    private readonly ILocalizationService _loc;
    private readonly IExportImportService _exportImportService;
    private readonly IGrammarExportImportService? _grammarExportImportService;

    private readonly ObservableCollection<SmartImportPreviewItem> _previewItems = new();
    private SmartImportAnalysisResultDto? _analysisResult;

    public string? ContentToImport { get; private set; }
    public string? FileNameHint { get; private set; }
    public ImportConflictResolution ConflictResolution => RadioConflictOverwrite.IsChecked == true
        ? ImportConflictResolution.Overwrite
        : ImportConflictResolution.Skip;
    public DataFormat DetectedFormat { get; private set; } = DataFormat.Unknown;

    public bool WasCommitted { get; private set; }
    public SmartImportCommitResultDto? CommitResult { get; private set; }

    public SmartImportDialog(Func<string, string>? sampleProvider = null, bool isGrammarMode = false)
    {
        InitializeComponent();
        _sampleProvider = sampleProvider;
        _isGrammarMode = isGrammarMode;

        try
        {
            _scope = App.Current.Services.CreateScope();
            _loc = _scope.ServiceProvider.GetRequiredService<ILocalizationService>();
            _exportImportService = _scope.ServiceProvider.GetRequiredService<IExportImportService>();
            _grammarExportImportService = _scope.ServiceProvider.GetService<IGrammarExportImportService>();
        }
        catch
        {
            _loc = App.Current.Services.GetRequiredService<ILocalizationService>();
            _exportImportService = App.Current.Services.GetRequiredService<IExportImportService>();
            _grammarExportImportService = App.Current.Services.GetService<IGrammarExportImportService>();
        }

        Unloaded += (s, e) => _scope?.Dispose();

        // Sync theme from the main window so the dialog renders correctly in both Light and Dark mode.
        var rootTheme = (App.Current.MainWindow?.Content as FrameworkElement)?.ActualTheme;
        if (rootTheme.HasValue && rootTheme.Value != ElementTheme.Default)
        {
            RequestedTheme = rootTheme.Value;
        }

        CloseButtonText = _loc.GetString("Action_Cancel");

        // Defer visual-tree-dependent initialisation to the Loaded event so that
        // ThemeResourceHelper can resolve ActualTheme from the live visual tree
        // and named elements are guaranteed to exist.
        Loaded += OnDialogLoaded;
    }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnDialogLoaded;
        try
        {
            ApplyLocalization();
            UpdateMode(true);
            UpdateDetectionBadge();
            GoToStep(1);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SmartImportDialog] OnDialogLoaded error: {ex.Message}");
        }
    }

    private static Style? GetResourceStyle(string key)
    {
        if (App.Current.Resources.TryGetValue(key, out var res) && res is Style style)
        {
            return style;
        }
        foreach (var md in App.Current.Resources.MergedDictionaries)
        {
            if (md.TryGetValue(key, out var mdRes) && mdRes is Style mdStyle)
            {
                return mdStyle;
            }
        }
        return null;
    }

    private void ApplyLocalization()
    {
        if (_isGrammarMode)
        {
            Title = _loc.GetString("Grammar_SmartImport_Title");
            SubtitleText.Text = _loc.GetString("Grammar_SmartImport_Subtitle");
            TextAnalyzeNext.Text = _loc.GetString("Grammar_SmartImport_AnalyzeBtn");
        }
        else
        {
            Title = _loc.GetString("SmartImport_Title");
            SubtitleText.Text = _loc.GetString("SmartImport_Subtitle");
            TextAnalyzeNext.Text = _loc.GetString("SmartImport_AnalyzeBtn");
        }

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

        Step1Text.Text = _loc.GetString("SmartImport_StepSource");
        Step2Text.Text = _loc.GetString("SmartImport_StepPreview");
        Step3Text.Text = _loc.GetString("SmartImport_StepSummary");
        TextBackToStep1.Text = _loc.GetString("SmartImport_BackBtn");
        ChkSelectAll.Content = _loc.GetString("SmartImport_SelectAll");
        TextDone.Text = _loc.GetString("SmartImport_DoneBtn");
        BtnClearPaste.Content = _loc.GetString("SmartImport_ClearPaste");
        TextDropHint.Text = _loc.GetString("SmartImport_DragDropHint");
    }

    private void GoToStep(int step)
    {
        var accentBrush = ThemeResourceHelper.GetThemeBrush(this, "AccentFillColorDefaultBrush");
        var layerBrush = ThemeResourceHelper.GetThemeBrush(this, "LayerFillColorDefaultBrush");
        var accentText = ThemeResourceHelper.GetThemeBrush(this, "AccentTextFillColorPrimaryBrush");
        var subtleText = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorSecondaryBrush");
        var onAccentText = ThemeResourceHelper.GetThemeBrush(this, "TextOnAccentFillColorPrimaryBrush");

        PanelStep1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        PanelStep2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        PanelStep3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

        // Step 1 Indicator
        Step1NumberBorder.Background = step >= 1 ? accentBrush : layerBrush;
        Step1Text.Foreground = step >= 1 ? accentText : subtleText;

        // Step 2 Indicator
        Step2NumberBorder.Background = step >= 2 ? accentBrush : layerBrush;
        Step2NumberText.Foreground = step >= 2 ? onAccentText : subtleText;
        Step2Text.Foreground = step >= 2 ? accentText : subtleText;

        // Step 3 Indicator
        Step3NumberBorder.Background = step == 3 ? accentBrush : layerBrush;
        Step3NumberText.Foreground = step == 3 ? onAccentText : subtleText;
        Step3Text.Foreground = step == 3 ? accentText : subtleText;
    }

    private void UpdateMode(bool isFileMode)
    {
        var accentStyle = GetResourceStyle("AccentButtonStyle") ?? GetResourceStyle("LexoraAccentButtonStyle");
        var defaultStyle = GetResourceStyle("DefaultButtonStyle");

        if (isFileMode)
        {
            PanelFile.Visibility = Visibility.Visible;
            PanelPaste.Visibility = Visibility.Collapsed;
            if (accentStyle != null) BtnModeFile.Style = accentStyle;
            if (defaultStyle != null) BtnModePaste.Style = defaultStyle;
        }
        else
        {
            PanelFile.Visibility = Visibility.Collapsed;
            PanelPaste.Visibility = Visibility.Visible;
            if (defaultStyle != null) BtnModeFile.Style = defaultStyle;
            if (accentStyle != null) BtnModePaste.Style = accentStyle;
        }
        EvaluateContent();
    }

    private void BtnModeFile_Click(object sender, RoutedEventArgs e) => UpdateMode(true);
    private void BtnModePaste_Click(object sender, RoutedEventArgs e) => UpdateMode(false);

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".json", ".csv", ".tsv", ".txt", ".tab", ".md"
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB ceiling per AGENTS.md

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = _loc.CurrentLanguage == "vi-VN" ? "Thả tệp để nhập thông minh" : "Drop file to smart import";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
            DropZoneBorder.BorderBrush = ThemeResourceHelper.GetThemeBrush("AccentFillColorDefaultBrush") 
                ?? (Brush)App.Current.Resources["AccentFillColorDefaultBrush"];
        }
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        DropZoneBorder.BorderBrush = ThemeResourceHelper.GetThemeBrush("CardStrokeColorDefaultBrush")
            ?? (Brush)App.Current.Resources["CardStrokeColorDefaultBrush"];
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        DropZoneBorder.BorderBrush = ThemeResourceHelper.GetThemeBrush("CardStrokeColorDefaultBrush")
            ?? (Brush)App.Current.Resources["CardStrokeColorDefaultBrush"];

        try
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                await DispatcherQueue.EnsureOnUIThreadAsync();
                if (items.Count > 0 && items[0] is StorageFile file)
                {
                    await LoadSelectedFileAsync(file.Path, file.Name, file);
                }
            }
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            TextSelectedFile.Text = string.Format(_loc.GetString("Common_ErrorPrefix"), ex.Message);
            TextSelectedFile.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
        }
    }

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
            picker.FileTypeFilter.Add(".tab");

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
                await LoadSelectedFileAsync(file.Path, file.Name, file);
            }
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            TextSelectedFile.Text = string.Format(_loc.GetString("Common_ErrorPrefix"), ex.Message);
            TextSelectedFile.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
        }
    }

    private async Task LoadSelectedFileAsync(string? filePath, string fileName, StorageFile? storageFile = null)
    {
        await DispatcherQueue.EnsureOnUIThreadAsync();

        try
        {
            var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                TextSelectedFile.Text = string.Format(
                    _loc.GetString("SmartImport_UnsupportedFileType"),
                    string.IsNullOrEmpty(ext) ? "(no extension)" : ext);
                TextSelectedFile.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
                ContentToImport = string.Empty;
                FileNameHint = null;
                EvaluateContent();
                BtnAnalyzeNext.IsEnabled = false;
                return;
            }

            // 1. Check file size if path exists
            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                if (fileInfo.Length > MaxFileSizeBytes)
                {
                    TextSelectedFile.Text = _loc.GetString("SmartImport_FileTooLarge");
                    TextSelectedFile.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
                    ContentToImport = string.Empty;
                    FileNameHint = null;
                    EvaluateContent();
                    BtnAnalyzeNext.IsEnabled = false;
                    return;
                }
            }

            // 2. Read file content safely
            string content;
            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                try
                {
                    content = await System.IO.File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);
                }
                catch
                {
                    using var stream = System.IO.File.OpenRead(filePath);
                    using var reader = new System.IO.StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                    content = await reader.ReadToEndAsync();
                }
            }
            else if (storageFile != null)
            {
                content = await Windows.Storage.FileIO.ReadTextAsync(storageFile);
            }
            else
            {
                throw new System.IO.FileNotFoundException("Could not locate file path.");
            }

            await DispatcherQueue.EnsureOnUIThreadAsync();

            // 3. Check for binary characters (null bytes)
            if (content.Contains('\0'))
            {
                TextSelectedFile.Text = _loc.GetString("SmartImport_BinaryFileError");
                TextSelectedFile.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
                ContentToImport = string.Empty;
                FileNameHint = null;
                EvaluateContent();
                BtnAnalyzeNext.IsEnabled = false;
                return;
            }

            // 4. Success: bind content
            FileNameHint = fileName;
            ContentToImport = content;
            TextSelectedFile.Text = string.Format(_loc.GetString("SmartImport_SelectedFile"), fileName);
            TextSelectedFile.Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorSecondaryBrush");
            EvaluateContent();
            BtnAnalyzeNext.IsEnabled = !string.IsNullOrWhiteSpace(ContentToImport) && DetectedFormat != DataFormat.Unknown;
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            TextSelectedFile.Text = string.Format(_loc.GetString("Common_FileReadError"), ex.Message);
            TextSelectedFile.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
            ContentToImport = string.Empty;
            FileNameHint = null;
            EvaluateContent();
            BtnAnalyzeNext.IsEnabled = false;
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
        }
    }

    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PanelPaste.Visibility == Visibility.Visible)
        {
            ContentToImport = InputTextBox.Text;
            FileNameHint = null;
            UpdatePasteStats();
            EvaluateContent();
        }
    }

    private void UpdatePasteStats()
    {
        var text = InputTextBox.Text ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            TextPasteStats.Text = string.Empty;
            BtnClearPaste.Visibility = Visibility.Collapsed;
            return;
        }

        BtnClearPaste.Visibility = Visibility.Visible;
        int lineCount = text.Split('\n').Length;
        int charCount = text.Length;
        TextPasteStats.Text = string.Format(_loc.GetString("SmartImport_LineCount"), lineCount, charCount);
    }

    private void BtnClearPaste_Click(object sender, RoutedEventArgs e)
    {
        InputTextBox.Text = string.Empty;
        UpdatePasteStats();
        EvaluateContent();
    }

    private void EvaluateContent()
    {
        var raw = ContentToImport;
        DetectedFormat = SmartContentDetector.DetectFormat(raw, FileNameHint);
        UpdateDetectionBadge();
    }

    private void UpdateDetectionBadge()
    {
        var accentBrush = ThemeResourceHelper.GetThemeBrush(this, "AccentTextFillColorPrimaryBrush");
        var successBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorSuccessBrush");
        var cautionBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCautionBrush");

        switch (DetectedFormat)
        {
            case DataFormat.Json:
                IconFormatBadge.Glyph = "\uE73E";
                IconFormatBadge.Foreground = successBrush;
                TextFormatBadge.Text = string.Format(_loc.GetString("SmartImport_FormatDetected"), "JSON");
                break;
            case DataFormat.Csv:
                IconFormatBadge.Glyph = "\uE73E";
                IconFormatBadge.Foreground = successBrush;
                TextFormatBadge.Text = string.Format(_loc.GetString("SmartImport_FormatDetected"), "CSV");
                break;
            case DataFormat.Txt:
                IconFormatBadge.Glyph = "\uE73E";
                IconFormatBadge.Foreground = accentBrush;
                TextFormatBadge.Text = string.Format(_loc.GetString("SmartImport_FormatDetected"), "TXT / TSV / Anki / Quizlet");
                break;
            default:
                IconFormatBadge.Glyph = "\uE946";
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
            UpdatePasteStats();
        }
    }

    private async void BtnAnalyzeNext_Click(object sender, RoutedEventArgs e)
    {
        if (PanelPaste.Visibility == Visibility.Visible)
        {
            ContentToImport = InputTextBox.Text;
        }

        if (string.IsNullOrWhiteSpace(ContentToImport))
        {
            TextFormatBadge.Text = _loc.GetString("SmartImport_NoContent");
            IconFormatBadge.Glyph = "\uEA39";
            IconFormatBadge.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
            return;
        }

        if (DetectedFormat == DataFormat.Unknown)
        {
            TextFormatBadge.Text = _loc.GetString("SmartImport_FormatUnknown");
            IconFormatBadge.Glyph = "\uEA39";
            IconFormatBadge.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
            return;
        }

        if (_isGrammarMode)
        {
            if (_grammarExportImportService == null)
            {
                TextFormatBadge.Text = "Grammar export/import service not available.";
                return;
            }

            BtnAnalyzeNext.IsEnabled = false;
            GoToStep(3);
            PanelImportProgress.Visibility = Visibility.Visible;
            PanelImportReport.Visibility = Visibility.Collapsed;
            CloseButtonText = string.Empty;

            var progress = new Progress<ProgressReport>(report =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    BarImportProgress.Value = report.Percent;
                    TextImportProgressMessage.Text = report.Message;
                });
            });

            try
            {
                var importResult = await _grammarExportImportService.ImportSmartAsync(
                    ContentToImport,
                    FileNameHint,
                    ConflictResolution,
                    CancellationToken.None,
                    progress);

                await DispatcherQueue.EnsureOnUIThreadAsync();

                WasCommitted = true;
                PanelImportProgress.Visibility = Visibility.Collapsed;
                PanelImportReport.Visibility = Visibility.Visible;

                TextReportAdded.Text = string.Format(_loc.GetString("SmartImport_ImportedCount"), importResult.TotalImported);
                TextReportUpdated.Text = string.Format(_loc.GetString("SmartImport_UpdatedCount"), importResult.TotalUpdated);
                TextReportSkipped.Text = string.Format(_loc.GetString("SmartImport_SkippedCount"), importResult.TotalSkipped);
                TextReportErrors.Text = string.Format(_loc.GetString("SmartImport_ErrorCount"), importResult.Errors.Count);
            }
            catch (Exception ex)
            {
                await DispatcherQueue.EnsureOnUIThreadAsync();
                PanelImportProgress.Visibility = Visibility.Collapsed;
                PanelImportReport.Visibility = Visibility.Visible;
                TextReportTitle.Text = $"Lỗi: {ex.Message}";
            }
            finally
            {
                CloseButtonText = _loc.GetString("Action_Cancel");
                BtnAnalyzeNext.IsEnabled = true;
            }
            return;
        }

        BtnAnalyzeNext.IsEnabled = false;
        try
        {
            _analysisResult = await _exportImportService.AnalyzeSmartImportAsync(ContentToImport, FileNameHint);
            await DispatcherQueue.EnsureOnUIThreadAsync();

            // Populate Step 2 Metrics
            TextMetricTotal.Text = _analysisResult.TotalParsed.ToString();
            TextMetricValid.Text = _analysisResult.ValidCount.ToString();
            TextMetricDuplicates.Text = _analysisResult.DuplicateCount.ToString();
            TextMetricErrors.Text = _analysisResult.InvalidCount.ToString();

            // Populate Preview List
            var successBg = ThemeResourceHelper.GetThemeBrush("SubtleFillColorSecondaryBrush");
            var successBorder = ThemeResourceHelper.GetThemeBrush("SystemFillColorSuccessBrush");
            var successFg = ThemeResourceHelper.GetThemeBrush("SystemFillColorSuccessBrush");

            var cautionBorder = ThemeResourceHelper.GetThemeBrush("SystemFillColorCautionBrush");
            var cautionFg = ThemeResourceHelper.GetThemeBrush("SystemFillColorCautionBrush");

            var criticalBorder = ThemeResourceHelper.GetThemeBrush("SystemFillColorCriticalBrush");
            var criticalFg = ThemeResourceHelper.GetThemeBrush("SystemFillColorCriticalBrush");

            _previewItems.Clear();
            foreach (var item in _analysisResult.PreviewItems)
            {
                var preview = new SmartImportPreviewItem
                {
                    RowNumber = item.RowIndex,
                    Term = item.Term,
                    PartOfSpeech = item.PartOfSpeech.ToString(),
                    Meaning = item.DefinitionVi,
                    Pronunciation = item.PhoneticUk ?? item.PhoneticUs,
                    ExampleSentence = item.Examples,
                    TopicName = item.Topics,
                    CefrLevel = item.Level.ToString(),
                    IsDuplicate = item.IsDuplicate,
                    IsValid = item.IsValid,
                    ValidationMessage = item.ValidationMessage,
                    IsSelected = item.IsValid,
                    OriginalDto = item,
                    StatusBackgroundBrush = successBg
                };

                if (!item.IsValid)
                {
                    preview.StatusText = _loc.GetString("SmartImport_StatusInvalid");
                    preview.StatusBorderBrush = criticalBorder;
                    preview.StatusForegroundBrush = criticalFg;
                }
                else if (item.IsDuplicate)
                {
                    preview.StatusText = _loc.GetString("SmartImport_StatusDuplicate");
                    preview.StatusBorderBrush = cautionBorder;
                    preview.StatusForegroundBrush = cautionFg;
                }
                else
                {
                    preview.StatusText = _loc.GetString("SmartImport_StatusValid");
                    preview.StatusBorderBrush = successBorder;
                    preview.StatusForegroundBrush = successFg;
                }

                preview.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(SmartImportPreviewItem.IsSelected))
                    {
                        UpdateSelectionCount();
                    }
                };

                _previewItems.Add(preview);
            }

            ItemsPreviewList.ItemsSource = _previewItems;
            UpdateSelectionCount();
            GoToStep(2);
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            TextFormatBadge.Text = string.Format(_loc.GetString("Common_ParseError"), ex.Message);
            IconFormatBadge.Glyph = "\uEA39";
            IconFormatBadge.Foreground = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCriticalBrush");
        }
        finally
        {
            BtnAnalyzeNext.IsEnabled = true;
        }
    }

    private void UpdateSelectionCount()
    {
        int selected = _previewItems.Count(x => x.IsSelected);
        TextSelectedCounter.Text = $"{selected} / {_previewItems.Count}";
        TextCommitImport.Text = string.Format(_loc.GetString("SmartImport_CommitBtn"), selected);
        BtnCommitImport.IsEnabled = selected > 0;
    }

    private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
    {
        bool isChecked = ChkSelectAll.IsChecked == true;
        foreach (var item in _previewItems)
        {
            item.IsSelected = isChecked;
        }
        UpdateSelectionCount();
    }

    private void BtnBackToStep1_Click(object sender, RoutedEventArgs e)
    {
        GoToStep(1);
    }

    private async void BtnCommitImport_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = _previewItems.Where(x => x.IsSelected).Select(x => x.OriginalDto).ToList();
        if (selectedItems.Count == 0) return;

        GoToStep(3);
        PanelImportProgress.Visibility = Visibility.Visible;
        PanelImportReport.Visibility = Visibility.Collapsed;
        CloseButtonText = string.Empty; // Prevent closing while importing

        var progress = new Progress<ProgressReport>(report =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                BarImportProgress.Value = report.Percent;
                TextImportProgressMessage.Text = report.Message;
            });
        });

        try
        {
            var request = new SmartImportCommitRequestDto(
                Items: selectedItems,
                ConflictResolution: ConflictResolution
            );

            CommitResult = await _exportImportService.CommitSmartImportAsync(request, CancellationToken.None, progress);
            await DispatcherQueue.EnsureOnUIThreadAsync();
            WasCommitted = true;

            // Show summary report
            PanelImportProgress.Visibility = Visibility.Collapsed;
            PanelImportReport.Visibility = Visibility.Visible;

            TextReportAdded.Text = string.Format(_loc.GetString("SmartImport_ImportedCount"), CommitResult.TotalImported);
            TextReportUpdated.Text = string.Format(_loc.GetString("SmartImport_UpdatedCount"), CommitResult.TotalUpdated);
            TextReportSkipped.Text = string.Format(_loc.GetString("SmartImport_SkippedCount"), CommitResult.TotalSkipped);
            TextReportErrors.Text = string.Format(_loc.GetString("SmartImport_ErrorCount"), CommitResult.TotalErrors);
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            PanelImportProgress.Visibility = Visibility.Collapsed;
            PanelImportReport.Visibility = Visibility.Visible;
            TextReportTitle.Text = $"Lỗi: {ex.Message}";
        }
        finally
        {
            CloseButtonText = _loc.GetString("Action_Cancel");
        }
    }

    private void BtnDone_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
