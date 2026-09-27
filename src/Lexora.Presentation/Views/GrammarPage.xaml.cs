using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;

namespace Lexora.Presentation.Views;

public sealed partial class GrammarPage : Page
{
    public GrammarViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }

    public GrammarPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<GrammarViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();

        Loc.LanguageChanged += OnLanguageChanged;
        Unloaded += (s, e) =>
        {
            Loc.LanguageChanged -= OnLanguageChanged;
            _searchDebounceTimer?.Stop();
        };

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _searchDebounceTimer.Tick += async (s, e) =>
        {
            _searchDebounceTimer.Stop();
            try
            {
                await ViewModel.LoadRulesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GrammarPage] Debounce load error: {ex.Message}");
            }
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            Bindings.Update();
            UpdateSelectionModeUI();
        });
    }

    private readonly DispatcherTimer _searchDebounceTimer;

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        try
        {
            await ViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarPage] OnNavigatedTo error: {ex.Message}");
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private async void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            await ViewModel.LoadRulesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarPage] FilterChanged error: {ex.Message}");
        }
    }

    private void BtnSelectMode_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSelectionMode();
        UpdateSelectionModeUI();
    }

    private void UpdateSelectionModeUI()
    {
        if (ViewModel.IsSelectionMode)
        {
            BtnSelectMode.Style = (Style)App.Current.Resources["AccentButtonStyle"];
            IconSelectMode.Glyph = "\uE711";
            TextSelectMode.Text = Loc.GetString("Action_CancelSelection");
            RulesListView.SelectionMode = ListViewSelectionMode.Multiple;
            UpdateDeleteSelectedButtonText();
        }
        else
        {
            BtnSelectMode.Style = (Style)App.Current.Resources["DefaultButtonStyle"];
            IconSelectMode.Glyph = "\uE762";
            TextSelectMode.Text = Loc.GetString("Action_SelectMultiple");
            try
            {
                RulesListView.SelectedItems.Clear();
                RulesListView.SelectionMode = ListViewSelectionMode.Single;
            }
            catch { }
            ViewModel.ClearSelection();
        }
    }

    private DispatcherTimer? _notificationTimer;

    private void ShowNotification(InfoBarSeverity severity, string title, string message, int autoCloseMs = 5000)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _notificationTimer?.Stop();
            PageNotificationInfoBar.Severity = severity;
            PageNotificationInfoBar.Title = title;
            PageNotificationInfoBar.Message = message;
            PageNotificationInfoBar.IsOpen = true;

            if (autoCloseMs > 0)
            {
                _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(autoCloseMs) };
                _notificationTimer.Tick += (s, e) =>
                {
                    _notificationTimer.Stop();
                    PageNotificationInfoBar.IsOpen = false;
                };
                _notificationTimer.Start();
            }
        });
    }

    private void UpdateDeleteSelectedButtonText()
    {
        TextDeleteSelected.Text = string.Format(Loc.GetString("Action_DeleteSelected"), ViewModel.SelectedRuleIds.Count);
        BtnDeleteSelectedRules.IsEnabled = ViewModel.SelectedRuleIds.Count > 0;
    }

    private void RulesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ViewModel.IsSelectionMode) return;

        ViewModel.SelectedRuleIds.Clear();
        foreach (var item in RulesListView.SelectedItems)
        {
            if (item is GrammarRuleDto rule)
            {
                ViewModel.SelectedRuleIds.Add(rule.Id);
            }
        }
        UpdateDeleteSelectedButtonText();
    }

    private void SelectAllRules_Click(object sender, RoutedEventArgs e)
    {
        RulesListView.SelectAll();
    }

    private void DeselectAllRules_Click(object sender, RoutedEventArgs e)
    {
        RulesListView.SelectedItems.Clear();
        ViewModel.ClearSelection();
        UpdateDeleteSelectedButtonText();
    }

    private async void DeleteSelectedRules_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRuleIds.Count == 0) return;

        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var count = ViewModel.SelectedRuleIds.Count;
        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Grammar_ConfirmBulkDeleteTitle"),
            Content = string.Format(Loc.GetString("Grammar_ConfirmBulkDeleteMsg"), count),
            PrimaryButtonText = Loc.GetString("Action_Delete"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
            RequestedTheme = activeTheme
        };

        var result = await confirmDialog.ShowAsyncSafe();
        if (result == ContentDialogResult.Primary)
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();

            // 1. Snapshot the IDs to delete BEFORE resetting selection mode or modifying UI!
            var idsToDelete = ViewModel.SelectedRuleIds.ToList();
            if (idsToDelete.Count == 0) return;

            // 2. Disable buttons and exit selection mode cleanly on UI thread
            BtnDeleteSelectedRules.IsEnabled = false;
            BtnSelectMode.IsEnabled = false;

            // Turn off IsSelectionMode first so RulesListView_SelectionChanged will not clear SelectedRuleIds or fire side effects
            ViewModel.IsSelectionMode = false;
            try
            {
                RulesListView.SelectionMode = ListViewSelectionMode.Single;
                RulesListView.SelectedItems.Clear();
            }
            catch { }

            int deletedCount = 0;
            Exception? deleteError = null;
            try
            {
                deletedCount = await ViewModel.DeleteSelectedRulesAsync(idsToDelete);
            }
            catch (Exception ex)
            {
                deleteError = ex;
            }

            // 3. Re-enable selection button & refresh UI on UI thread
            await DispatcherQueue.EnsureOnUIThreadAsync();
            BtnSelectMode.IsEnabled = true;
            UpdateSelectionModeUI();

            if (deleteError != null)
            {
                ShowNotification(InfoBarSeverity.Error, Loc.GetString("Action_Delete"), deleteError.Message);
            }
            else
            {
                ShowNotification(InfoBarSeverity.Success, Loc.GetString("Action_Delete"), string.Format(Loc.GetString("Grammar_BulkDeleteSuccess"), deletedCount));
            }
        }
    }

    private async void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var xamlRoot = (sender as FrameworkElement)?.XamlRoot ?? this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot;
            var themeService = App.Current.Services.GetService<IThemeService>();
            var activeTheme = themeService?.CurrentTheme ?? ElementTheme.Default;

            var dialog = new GrammarEditorDialog
            {
                XamlRoot = xamlRoot,
                RequestedTheme = activeTheme
            };
            dialog.InitializeForCreate();

            var result = await dialog.ShowAsyncSafe(xamlRoot);
            if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
            {
                await ViewModel.AddRuleAsync(dialog.ResultDto);
                ShowNotification(InfoBarSeverity.Success, Loc.GetString("Action_Add"), Loc.GetString("Grammar_Status_RuleAdded"));
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Action_Add"), ex.Message);
        }
    }

    private async void EditRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRule == null) return;

        try
        {
            var xamlRoot = (sender as FrameworkElement)?.XamlRoot ?? this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot;
            var themeService = App.Current.Services.GetService<IThemeService>();
            var activeTheme = themeService?.CurrentTheme ?? ElementTheme.Default;

            var dialog = new GrammarEditorDialog
            {
                XamlRoot = xamlRoot,
                RequestedTheme = activeTheme
            };
            dialog.InitializeForEdit(ViewModel.SelectedRule);

            var result = await dialog.ShowAsyncSafe(xamlRoot);
            if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
            {
                await ViewModel.UpdateRuleAsync(dialog.ResultDto);
                ShowNotification(InfoBarSeverity.Success, Loc.GetString("Action_Edit"), Loc.GetString("Grammar_Status_RuleUpdated"));
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Action_Edit"), ex.Message);
        }
    }

    private async void DeleteRuleButton_Click(object sender, RoutedEventArgs e)
    {
        var ruleToDelete = ViewModel.SelectedRule;
        if (ruleToDelete == null) return;
        var ruleId = ruleToDelete.Id;
        var ruleTitle = ruleToDelete.Title;

        try
        {
            var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            var confirmDialog = new ContentDialog
            {
                Title = Loc.GetString("Grammar_DeleteTitle"),
                Content = string.Format(Loc.GetString("Grammar_DeleteConfirm"), ruleTitle),
                PrimaryButtonText = Loc.GetString("Action_Delete"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot,
                RequestedTheme = activeTheme
            };

            var result = await confirmDialog.ShowAsyncSafe();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await DispatcherQueue.EnsureOnUIThreadAsync();
                    await ViewModel.DeleteRuleAsync(ruleId);
                    await DispatcherQueue.EnsureOnUIThreadAsync();
                    ShowNotification(InfoBarSeverity.Success, Loc.GetString("Action_Delete"), string.Format(Loc.GetString("Grammar_Status_RuleDeleted"), ruleTitle));
                }
                catch (Exception ex)
                {
                    await DispatcherQueue.EnsureOnUIThreadAsync();
                    ShowNotification(InfoBarSeverity.Error, Loc.GetString("Action_Delete"), ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarPage] DeleteRule error: {ex.Message}");
        }
    }

    private async void PracticeButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartPracticeAsync();
    }

    private void PracticeOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string opt)
        {
            ViewModel.SelectPracticeOption(opt);
        }
    }

    private void NextPracticeQuestion_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NextPracticeQuestion();
    }

    private void ExitPracticeButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExitPractice();
    }

    public InfoBarSeverity GetFeedbackSeverity(bool isCorrect)
    {
        return isCorrect ? InfoBarSeverity.Success : InfoBarSeverity.Error;
    }

    public string GetFeedbackTitle(bool isCorrect)
    {
        return isCorrect 
            ? (Loc.CurrentLanguage == "vi-VN" ? "Chính xác! 🎉" : "Correct! 🎉")
            : (Loc.CurrentLanguage == "vi-VN" ? "Chưa chính xác!" : "Incorrect!");
    }

    public Brush GetQuestionReviewBrush(bool? isCorrect)
    {
        string brushKey = isCorrect == true ? "SystemFillColorSuccessBrush" : "SystemFillColorCriticalBrush";
        return Services.ThemeResourceHelper.GetThemeBrush(this, brushKey) ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    public string GetQuestionReviewGlyph(bool? isCorrect)
    {
        return isCorrect == true ? "\uE73E" : "\uE711";
    }

    public string GetQuestionReviewStatusText(bool? isCorrect)
    {
        if (isCorrect == true)
        {
            return Loc.CurrentLanguage == "vi-VN" ? "Chính xác" : "Correct";
        }
        return Loc.CurrentLanguage == "vi-VN" ? "Chưa đúng" : "Incorrect";
    }

    private void AddRuleAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        AddRuleButton_Click(this, new RoutedEventArgs());
        args.Handled = true;
    }

    private void EditRuleAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.SelectedRule != null)
        {
            EditRuleButton_Click(this, new RoutedEventArgs());
            args.Handled = true;
        }
    }

    private void DeleteRuleAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.SelectedRule != null)
        {
            DeleteRuleButton_Click(this, new RoutedEventArgs());
            args.Handled = true;
        }
    }

    private void PracticeAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        PracticeButton_Click(this, new RoutedEventArgs());
        args.Handled = true;
    }

    private async void CopyStructureButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRule == null || string.IsNullOrWhiteSpace(ViewModel.SelectedRule.Structure)) return;

        try
        {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(ViewModel.SelectedRule.Structure);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

            // Visual feedback: checkmark + "Đã sao chép!"
            IconCopyStructure.Glyph = "\uE73E";
            if (App.Current.Resources.TryGetValue("SystemFillColorSuccessBrush", out var successBrush) && successBrush is Microsoft.UI.Xaml.Media.Brush bSuccess)
            {
                IconCopyStructure.Foreground = bSuccess;
                TextCopyStructure.Foreground = bSuccess;
            }
            TextCopyStructure.Text = Loc.GetString("Action_Copied");

            await Task.Delay(1800);

            IconCopyStructure.Glyph = "\uE8C8";
            IconCopyStructure.ClearValue(FontIcon.ForegroundProperty);
            TextCopyStructure.Text = Loc.GetString("Action_Copy");
            TextCopyStructure.ClearValue(TextBlock.ForegroundProperty);
        }
        catch
        {
            // Clipboard fallback
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Export Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await PerformExportAsync("json", "Grammar JSON File", ".json",
                   progress => ViewModel.ExportToJsonAsync(null, progress));
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Grammar_ExportFailed"), ex.Message);
        }
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await PerformExportAsync("csv", "Grammar CSV File", ".csv",
                   progress => ViewModel.ExportToCsvAsync(null, progress));
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Grammar_ExportFailed"), ex.Message);
        }
    }

    private async void ExportTxt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await PerformExportAsync("txt", "Grammar TXT/Markdown File", ".txt",
                   progress => ViewModel.ExportToTxtAsync(null, progress));
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Grammar_ExportFailed"), ex.Message);
        }
    }

    private void ExportImportFlyout_Opening(object sender, object e)
    {
        var count = RulesListView?.SelectedItems?.Count ?? 0;
        if (ExportSelectedMenuItem != null)
        {
            ExportSelectedMenuItem.IsEnabled = count > 0;
            ExportSelectedMenuItem.Text = string.Format(Loc.GetString("Grammar_ExportSelected"), count);
        }
    }

    private async void ExportSelected_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedRules = RulesListView.SelectedItems.OfType<GrammarRuleDto>().ToList();
            if (selectedRules.Count == 0) return;

            var selectedIds = selectedRules.Select(r => r.Id).ToList();
            await PerformExportAsync("csv", "Grammar CSV File", ".csv",
                progress => ViewModel.ExportToCsvAsync(selectedIds, progress));
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Grammar_ExportFailed"), ex.Message);
        }
    }

    private async Task PerformExportAsync(
        string format, string fileTypeDesc, string extension,
        Func<IProgress<ProgressReport>, Task<string>> exportFunc)
    {
        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark
            ? ElementTheme.Dark : ElementTheme.Light;

        var progressDialog = new OperationProgressDialog
        {
            Title = Loc.GetString("Progress_ExportingTitle"),
            XamlRoot = this.XamlRoot,
            RequestedTheme = activeTheme
        };
        progressDialog.SetIndeterminate(Loc.GetString("Progress_PreparingExport"), Loc.GetString("Progress_PleaseWait"));
        _ = progressDialog.ShowAsyncSafe();

        try
        {
            var progress = new Progress<ProgressReport>(report =>
                DispatcherQueue.TryEnqueue(() =>
                    progressDialog.SetDeterminate(report.Percent, report.Message, report.StepDetail)));

            var content = await exportFunc(progress);
            progressDialog.HideSafe();
            await Task.Yield();

            var defaultName = $"lexora_grammar_{DateTime.Now:yyyyMMdd_HHmmss}";
            await SaveExportFileAsync(content, defaultName, extension, fileTypeDesc);
        }
        catch (Exception ex)
        {
            progressDialog.HideSafe();
            await Task.Yield();
            await ShowErrorDialogAsync(Loc.GetString("Grammar_ExportFailed"), ex.Message);
        }
    }

    private async Task SaveExportFileAsync(
        string content, string defaultName, string extension, string fileTypeDescription)
    {
        string? savedPath = null;
        try
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            savePicker.FileTypeChoices.Add(fileTypeDescription, [extension]);
            savePicker.SuggestedFileName = defaultName;

            var mainWindow = App.Current.MainWindow;
            if (mainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            }

            var file = await savePicker.PickSaveFileAsync();
            await DispatcherQueue.EnsureOnUIThreadAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, content);
                savedPath = file.Path;
            }
            else return; // User cancelled
        }
        catch
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            savedPath = Path.Combine(desktop, $"{defaultName}{extension}");
            await File.WriteAllTextAsync(savedPath, content);
        }

        if (!string.IsNullOrEmpty(savedPath))
        {
            ShowNotification(InfoBarSeverity.Success, Loc.GetString("Grammar_ExportCompleteTitle"), string.Format(Loc.GetString("Grammar_ExportCompleteMsg"), ViewModel.Rules.Count, Path.GetFileName(savedPath)));

            var exportTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark
                ? ElementTheme.Dark : ElementTheme.Light;
            var dialog = new ContentDialog
            {
                Title = Loc.GetString("Grammar_ExportCompleteTitle"),
                Content = string.Format(Loc.GetString("Grammar_ExportCompleteMsg"),
                    ViewModel.Rules.Count, savedPath),
                PrimaryButtonText = Loc.GetString("Export_OpenFile"),
                SecondaryButtonText = Loc.GetString("Export_OpenFolder"),
                CloseButtonText = Loc.GetString("Action_Close"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                RequestedTheme = exportTheme
            };

            var res = await dialog.ShowAsyncSafe();
            if (res == ContentDialogResult.Primary && File.Exists(savedPath))
            {
                try
                {
                    var cleanPath = Path.GetFullPath(savedPath);
                    var ext = Path.GetExtension(cleanPath).ToLowerInvariant();
                    if (ext is ".csv" or ".json" or ".txt" or ".db" or ".tsv")
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(cleanPath) { UseShellExecute = true });
                    }
                }
                catch
                {
                    // Fallback
                }
            }
            else if (res == ContentDialogResult.Secondary && File.Exists(savedPath))
            {
                try
                {
                    var cleanPath = Path.GetFullPath(savedPath);
                    var psi = new System.Diagnostics.ProcessStartInfo("explorer.exe");
                    psi.ArgumentList.Add($"/select,{cleanPath}");
                    psi.UseShellExecute = false;
                    System.Diagnostics.Process.Start(psi);
                }
                catch
                {
                    // Explorer launch fallback
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Import Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private async void SmartImport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Ensure we are on the UI thread before constructing ContentDialog.
            await DispatcherQueue.EnsureOnUIThreadAsync();

            var xamlRoot = App.Current.MainWindow?.Content?.XamlRoot ?? this.XamlRoot;

            var dialog = new SmartImportDialog(format => ViewModel.GetSampleTemplate(format), isGrammarMode: true)
            {
                XamlRoot = xamlRoot
            };

            var res = await dialog.ShowAsyncSafe(xamlRoot);
            if (dialog.WasCommitted)
            {
                await ViewModel.LoadRulesAsync();
                await ViewModel.LoadTopicsAsync();
                ShowNotification(InfoBarSeverity.Success, Loc.GetString("Progress_CompletedTitle"), Loc.GetString("SmartImport_CommitSuccess"));
            }
            else if (res == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.ContentToImport))
            {
                await ExecuteImportOperationAsync(dialog.ContentToImport, dialog.FileNameHint, dialog.ConflictResolution);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarPage] SmartImport error: {ex}");
            Services.SafeDialogExtensions.LogSmartImportError("GrammarPage", ex);
            ShowNotification(InfoBarSeverity.Error, Loc.GetString("Grammar_ImportFailed"), ex.Message);
        }
    }

    private async void ImportJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ShowImportDialogAsync("json");
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Grammar_ImportFailed"), ex.Message);
        }
    }

    private async void ImportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ShowImportDialogAsync("csv");
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Grammar_ImportFailed"), ex.Message);
        }
    }

    private async void ImportTxt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ShowImportDialogAsync("txt");
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Grammar_ImportFailed"), ex.Message);
        }
    }

    private async Task ShowImportDialogAsync(string format)
    {
        string filterExt = format switch
        {
            "json" => ".json",
            "csv" => ".csv",
            _ => ".txt"
        };
        string? contentToImport = null;
        string? fileName = null;

        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(filterExt);

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
                var basicProps = await file.GetBasicPropertiesAsync();
                if (basicProps.Size > 20 * 1024 * 1024)
                {
                    await ShowErrorDialogAsync(Loc.GetString("Grammar_ImportFailed"), Loc.GetString("SmartImport_FileTooLarge"));
                    return;
                }

                fileName = file.Name;
                contentToImport = await Windows.Storage.FileIO.ReadTextAsync(file);
            }
            else
            {
                return; // User cancelled cleanly
            }
        }
        catch
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(contentToImport))
        {
            await ExecuteImportOperationAsync(contentToImport, fileName, ImportConflictResolution.Skip);
        }
    }

    private async Task ExecuteImportOperationAsync(
        string contentToImport,
        string? fileNameHint,
        ImportConflictResolution conflictResolution)
    {
        await Task.Yield();

        var importTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark
            ? ElementTheme.Dark : ElementTheme.Light;

        var progressDialog = new OperationProgressDialog
        {
            Title = Loc.GetString("Progress_ImportingTitle"),
            XamlRoot = this.XamlRoot,
            RequestedTheme = importTheme
        };
        progressDialog.SetDeterminate(0, Loc.GetString("Progress_ReadingFile"), Loc.GetString("Progress_PleaseWait"));
        _ = progressDialog.ShowAsyncSafe();

        var progress = new Progress<ProgressReport>(report =>
            DispatcherQueue.TryEnqueue(() =>
                progressDialog.SetDeterminate(report.Percent, report.Message, report.StepDetail)));

        try
        {
            var importResult = await ViewModel.ImportSmartAsync(contentToImport, fileNameHint, conflictResolution, progress);

            string summary;
            if (importResult.TotalUpdated > 0)
            {
                summary = string.Format(
                    Loc.GetString("Progress_SummaryFormatWithUpdated"),
                    importResult.TotalImported,
                    importResult.TotalUpdated,
                    importResult.TotalSkipped,
                    importResult.Errors.Count);
            }
            else
            {
                summary = string.Format(
                    Loc.GetString("Progress_SummaryFormat"),
                    importResult.TotalImported,
                    importResult.TotalSkipped,
                    importResult.Errors.Count);
            }

            progressDialog.SetCompleted(
                Loc.GetString("Progress_CompletedTitle"),
                summary,
                importResult.TotalImported + importResult.TotalUpdated,
                importResult.TotalSkipped,
                importResult.Errors.Count,
                Loc.GetString("Action_Close"));

            ShowNotification(
                importResult.Errors.Count > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success,
                Loc.GetString("Progress_CompletedTitle"),
                summary);
        }
        catch (Exception ex)
        {
            progressDialog.SetError(
                Loc.GetString("Grammar_ImportFailed"),
                ex.Message,
                Loc.GetString("Action_Close"));

            ShowNotification(InfoBarSeverity.Error, Loc.GetString("Grammar_ImportFailed"), ex.Message);
        }
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        try
        {
            var xamlRoot = this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot;
            if (xamlRoot == null) return;
            var themeService = App.Current.Services.GetService<IThemeService>();
            var errTheme = themeService?.CurrentTheme ?? ElementTheme.Default;
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = Loc.GetString("Action_Close"),
                XamlRoot = xamlRoot,
                RequestedTheme = errTheme
            };
            await dialog.ShowAsyncSafe(xamlRoot);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GrammarPage] ShowErrorDialog error: {ex.Message}");
        }
    }
}
