using Lexora.Application.DTOs;
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
        Unloaded += (s, e) => Loc.LanguageChanged -= OnLanguageChanged;

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _searchDebounceTimer.Tick += async (s, e) =>
        {
            _searchDebounceTimer.Stop();
            await ViewModel.LoadRulesAsync();
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
        await ViewModel.InitializeAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        _ = ViewModel.LoadRulesAsync();
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
            RulesListView.SelectionMode = ListViewSelectionMode.Single;
            RulesListView.SelectedItems.Clear();
            ViewModel.ClearSelection();
        }
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

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await Task.Yield();
            await ViewModel.DeleteSelectedRulesAsync();
            RulesListView.SelectedItems.Clear();
            UpdateSelectionModeUI();
        }
    }

    private async void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var dialog = new GrammarEditorDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = activeTheme
        };
        dialog.InitializeForCreate();

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
        {
            await ViewModel.AddRuleAsync(dialog.ResultDto);
        }
    }

    private async void EditRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRule == null) return;

        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var dialog = new GrammarEditorDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = activeTheme
        };
        dialog.InitializeForEdit(ViewModel.SelectedRule);

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
        {
            await ViewModel.UpdateRuleAsync(dialog.ResultDto);
        }
    }

    private async void DeleteRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRule == null) return;

        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Grammar_DeleteTitle"),
            Content = string.Format(Loc.GetString("Grammar_DeleteConfirm"), ViewModel.SelectedRule.Title),
            PrimaryButtonText = Loc.GetString("Action_Delete"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteRuleAsync(ViewModel.SelectedRule.Id);
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
        => await PerformExportAsync("json", "Grammar JSON File", ".json",
               progress => ViewModel.ExportToJsonAsync(progress));

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
        => await PerformExportAsync("csv", "Grammar CSV File", ".csv",
               progress => ViewModel.ExportToCsvAsync(progress));

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
        _ = progressDialog.ShowAsync();

        try
        {
            var progress = new Progress<ProgressReport>(report =>
                DispatcherQueue.TryEnqueue(() =>
                    progressDialog.SetDeterminate(report.Percent, report.Message, report.StepDetail)));

            var content = await exportFunc(progress);
            progressDialog.Hide();
            await Task.Yield();

            var defaultName = $"lexora_grammar_{DateTime.Now:yyyyMMdd_HHmmss}";
            await SaveExportFileAsync(content, defaultName, extension, fileTypeDesc);
        }
        catch (Exception ex)
        {
            progressDialog.Hide();
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
            var exportTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark
                ? ElementTheme.Dark : ElementTheme.Light;
            var dialog = new ContentDialog
            {
                Title = Loc.GetString("Grammar_ExportCompleteTitle"),
                Content = string.Format(Loc.GetString("Grammar_ExportCompleteMsg"),
                    ViewModel.Rules.Count, savedPath),
                CloseButtonText = Loc.GetString("Action_Close"),
                XamlRoot = this.XamlRoot,
                RequestedTheme = exportTheme
            };
            await dialog.ShowAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Import Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private async void ImportJson_Click(object sender, RoutedEventArgs e)
        => await ShowImportDialogAsync("json");

    private async void ImportCsv_Click(object sender, RoutedEventArgs e)
        => await ShowImportDialogAsync("csv");

    private async Task ShowImportDialogAsync(string format)
    {
        string dialogTitle = format == "json"
            ? Loc.GetString("Grammar_ImportJsonTitle")
            : Loc.GetString("Grammar_ImportCsvTitle");

        string placeholder = format == "json"
            ? Loc.GetString("Grammar_ImportPastePlaceholderJson")
            : Loc.GetString("Grammar_ImportPastePlaceholderCsv");

        string? contentToImport = null;

        // Try file picker first
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(format == "json" ? ".json" : ".csv");

            var mainWindow = App.Current.MainWindow;
            if (mainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
                contentToImport = await Windows.Storage.FileIO.ReadTextAsync(file);
        }
        catch
        {
            // Fallback to paste dialog
        }

        if (contentToImport == null)
        {
            var importThemePrompt = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark
                ? ElementTheme.Dark : ElementTheme.Light;

            var textBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 200,
                PlaceholderText = placeholder
            };

            var pasteDialog = new ContentDialog
            {
                Title = dialogTitle,
                Content = textBox,
                PrimaryButtonText = Loc.GetString("Vocab_ImportExport"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                RequestedTheme = importThemePrompt
            };

            var res = await pasteDialog.ShowAsync();
            if (res == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
                contentToImport = textBox.Text;
        }

        if (!string.IsNullOrWhiteSpace(contentToImport))
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
            _ = progressDialog.ShowAsync();

            var progress = new Progress<ProgressReport>(report =>
                DispatcherQueue.TryEnqueue(() =>
                    progressDialog.SetDeterminate(report.Percent, report.Message, report.StepDetail)));

            try
            {
                var importResult = await ViewModel.ImportDataAsync(contentToImport, format, progress);
                await ViewModel.RefreshAfterImportAsync();

                var summary = string.Format(
                    Loc.GetString("Progress_SummaryFormat"),
                    importResult.TotalImported,
                    importResult.TotalSkipped,
                    importResult.Errors.Count);

                progressDialog.SetCompleted(
                    Loc.GetString("Progress_CompletedTitle"),
                    summary,
                    importResult.TotalImported,
                    importResult.TotalSkipped,
                    importResult.Errors.Count,
                    Loc.GetString("Action_Close"));
            }
            catch (Exception ex)
            {
                progressDialog.SetError(
                    Loc.GetString("Grammar_ImportFailed"),
                    ex.Message,
                    Loc.GetString("Action_Close"));
            }
        }
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var errTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark
            ? ElementTheme.Dark : ElementTheme.Light;
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = Loc.GetString("Action_Close"),
            XamlRoot = this.XamlRoot,
            RequestedTheme = errTheme
        };
        await dialog.ShowAsync();
    }
}
