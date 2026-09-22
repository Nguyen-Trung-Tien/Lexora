using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace Lexora.Presentation.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<SettingsViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();

        Loc.LanguageChanged += OnLanguageChanged;
        Unloaded += (s, e) => Loc.LanguageChanged -= OnLanguageChanged;
        ActualThemeChanged += (s, e) =>
        {
            UpdateLanguageButtons();
            UpdateThemeButtons();
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateLanguageButtons();
            UpdateThemeButtons();
            Bindings.Update();
        });
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        UpdateLanguageButtons();
        UpdateThemeButtons();

        SpeechRateCombo.SelectedIndex = ViewModel.SpeechRate switch
        {
            <= 0.8 => 0,
            <= 1.1 => 1,
            <= 1.35 => 2,
            _ => 3
        };
    }

    private Microsoft.UI.Xaml.Media.Brush? GetThemeBrush(string key)
    {
        return Services.ThemeResourceHelper.GetThemeBrush(this, key);
    }

    private void UpdateLanguageButtons()
    {
        bool isVi = Loc.CurrentLanguage == "vi-VN";
        var cardBg = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
        var cardStroke = GetThemeBrush("CardStrokeColorDefaultBrush");
        var accentText = GetThemeBrush("AccentTextFillColorPrimaryBrush");
        var subtleText = GetThemeBrush("TextFillColorSecondaryBrush");
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        void ApplyLangBtn(Button btn, TextBlock text, FontIcon check, bool isActive)
        {
            btn.Style = null;
            if (isActive)
            {
                if (cardBg != null) btn.Background = cardBg;
                if (cardStroke != null) btn.BorderBrush = cardStroke;
                btn.BorderThickness = new Thickness(1);
                if (accentText != null) text.Foreground = accentText;
                btn.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                check.Visibility = Visibility.Visible;
            }
            else
            {
                btn.Background = transparent;
                btn.BorderThickness = new Thickness(0);
                if (subtleText != null) text.Foreground = subtleText;
                btn.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                check.Visibility = Visibility.Collapsed;
            }
        }

        ApplyLangBtn(LangViButton, TextLangVi, CheckLangVi, isVi);
        ApplyLangBtn(LangEnButton, TextLangEn, CheckLangEn, !isVi);
    }

    private async void CopyDbPath_Click(object sender, RoutedEventArgs e)
    {
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(ViewModel.DatabasePath);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

        // Visual feedback: checkmark + "Đã sao chép!"
        IconCopyDbPath.Glyph = "\uE73E";
        if (App.Current.Resources.TryGetValue("SystemFillColorSuccessBrush", out var successBrush) && successBrush is Microsoft.UI.Xaml.Media.Brush bSuccess)
        {
            IconCopyDbPath.Foreground = bSuccess;
            TextCopyDbPath.Foreground = bSuccess;
        }
        TextCopyDbPath.Text = Loc.GetString("Action_Copied");

        await Task.Delay(1800);

        IconCopyDbPath.Glyph = "\uE8C8";
        IconCopyDbPath.ClearValue(FontIcon.ForegroundProperty);
        TextCopyDbPath.Text = Loc.GetString("Action_Copy");
        TextCopyDbPath.ClearValue(TextBlock.ForegroundProperty);
    }

    private async void SpeechRateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SpeechRateCombo?.SelectedItem is ComboBoxItem item &&
            item.Tag is string tagStr &&
            double.TryParse(tagStr, System.Globalization.CultureInfo.InvariantCulture, out var rate))
        {
            await ViewModel.SetSpeechRateAsync(rate);
        }
    }

    private async void TypingPracticeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.SetTypingPracticeAsync(ts.IsOn);
        }
    }

    private void ThemeSystemButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeTheme(0);
        UpdateThemeButtons();
    }

    private void ThemeLightButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeTheme(1);
        UpdateThemeButtons();
    }

    private void ThemeDarkButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeTheme(2);
        UpdateThemeButtons();
    }

    private void UpdateThemeButtons()
    {
        int index = ViewModel.SelectedThemeIndex;
        var selectedBorder = GetThemeBrush("LexoraThemeCardSelectedBorderBrush") ?? GetThemeBrush("AccentFillColorDefaultBrush");
        var defaultBorder = GetThemeBrush("CardStrokeColorDefaultBrush");
        var cardBg = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
        var subtleBg = GetThemeBrush("SubtleFillColorSecondaryBrush");

        void ApplyThemeBtn(Button btn, Border check, bool isSelected)
        {
            if (isSelected)
            {
                if (selectedBorder != null) btn.BorderBrush = selectedBorder;
                if (cardBg != null) btn.Background = cardBg;
                btn.BorderThickness = new Thickness(2);
                check.Visibility = Visibility.Visible;
            }
            else
            {
                if (defaultBorder != null) btn.BorderBrush = defaultBorder;
                if (subtleBg != null) btn.Background = subtleBg;
                btn.BorderThickness = new Thickness(1);
                check.Visibility = Visibility.Collapsed;
            }
        }

        if (ThemeSystemButton != null && ThemeSystemCheck != null) ApplyThemeBtn(ThemeSystemButton, ThemeSystemCheck, index == 0);
        if (ThemeLightButton != null && ThemeLightCheck != null) ApplyThemeBtn(ThemeLightButton, ThemeLightCheck, index == 1);
        if (ThemeDarkButton != null && ThemeDarkCheck != null) ApplyThemeBtn(ThemeDarkButton, ThemeDarkCheck, index == 2);
    }

    private void LangViButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeLanguage("vi-VN");
        UpdateLanguageButtons();
    }

    private void LangEnButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeLanguage("en-US");
        UpdateLanguageButtons();
    }

    private async void AutoPlayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.SetAutoPlayAudioAsync(ts.IsOn);
        }
    }

    private async void ReminderToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.SetReviewReminderAsync(ts.IsOn);
        }
    }

    private async void RefreshStatsButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDatabaseStatsAsync();
    }

    private async void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        string? targetFilePath = null;
        try
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            savePicker.FileTypeChoices.Add("SQLite Database Backup (*.db)", new List<string> { ".db" });
            savePicker.SuggestedFileName = $"lexora_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";

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
                targetFilePath = file.Path;
            }
            else
            {
                // User explicitly cancelled save dialog
                return;
            }
        }
        catch
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            // Picker fallback: targetFilePath remains null -> ViewModel defaults to Desktop / AppData
        }

        await DispatcherQueue.EnsureOnUIThreadAsync();
        if (this.XamlRoot == null) return;

        var backupTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var progressDialog = new OperationProgressDialog
        {
            Title = Loc.GetString("Progress_BackupTitle"),
            XamlRoot = this.XamlRoot,
            RequestedTheme = backupTheme
        };
        progressDialog.SetDeterminate(10, Loc.GetString("Progress_BackupFlushing"), Loc.GetString("Progress_PleaseWait"));
        _ = progressDialog.ShowAsync();

        try
        {
            await Task.Delay(200);
            progressDialog.SetDeterminate(40, Loc.GetString("Progress_BackupCopying"), Loc.GetString("Progress_PleaseWait"));

            var backupPath = await ViewModel.CreateBackupAsync(targetFilePath);

            progressDialog.SetDeterminate(85, Loc.GetString("Progress_BackupVerifying"), Loc.GetString("Progress_PleaseWait"));
            await Task.Delay(200);

            if (!string.IsNullOrEmpty(backupPath))
            {
                progressDialog.SetCompleted(
                    Loc.GetString("Progress_CompletedTitle"),
                    ViewModel.BackupStatusMessage,
                    closeButtonText: Loc.GetString("Action_Close"));
            }
            else
            {
                progressDialog.SetError(
                    Loc.GetString("Settings_BackupFailed"),
                    ViewModel.BackupStatusMessage,
                    Loc.GetString("Action_Close"));
            }
        }
        catch (Exception ex)
        {
            progressDialog.SetError(
                Loc.GetString("Settings_BackupFailed"),
                ex.Message,
                Loc.GetString("Action_Close"));
        }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        string? selectedFilePath = null;
        bool pickerCancelled = false;

        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".db");
            picker.FileTypeFilter.Add(".sqlite");
            picker.FileTypeFilter.Add(".sqlite3");

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
                selectedFilePath = file.Path;
            }
            else
            {
                pickerCancelled = true;
            }
        }
        catch
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            // Fallback to text path prompt if picker encounters WinRT uninitialized handle
        }

        if (pickerCancelled)
        {
            return;
        }

        await DispatcherQueue.EnsureOnUIThreadAsync();
        if (this.XamlRoot == null) return;

        if (string.IsNullOrEmpty(selectedFilePath))
        {
            var pathBox = new TextBox
            {
                PlaceholderText = @"C:\path\to\lexora_backup_xxxx.db",
                MaxWidth = 450,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var pathPromptTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            var promptDialog = new ContentDialog
            {
                Title = Loc.GetString("Settings_DlgRestorePathTitle"),
                Content = pathBox,
                PrimaryButtonText = Loc.GetString("Settings_DlgRestorePathSelect"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                RequestedTheme = pathPromptTheme
            };

            var promptRes = await promptDialog.ShowAsync();
            if (promptRes == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(pathBox.Text))
            {
                selectedFilePath = pathBox.Text.Trim();
            }
            else
            {
                return;
            }
        }

        if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
        {
            return;
        }

        try
        {
            // 1. Inspect backup file metadata and structural integrity
            var metadata = await ViewModel.GetBackupMetadataAsync(selectedFilePath);

            await DispatcherQueue.EnsureOnUIThreadAsync();
            if (this.XamlRoot == null) return;

            var currentTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;

            if (!metadata.IsValid)
            {
                var errorDialog = new ContentDialog
                {
                    Title = Loc.GetString("Settings_BackupFailed"),
                    Content = string.Format(Loc.GetString("Settings_DlgRestoreInvalidFile"), metadata.ErrorMessage),
                    CloseButtonText = Loc.GetString("Action_Close"),
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = currentTheme
                };
                await errorDialog.ShowAsync();
                return;
            }

            // 2. Build high-fidelity Fluent 2 preview confirmation layout
            var previewXaml = $@"
<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
            Spacing=""14"" MaxWidth=""460"">
    <Border Background=""{{ThemeResource SubtleFillColorSecondaryBrush}}""
            BorderBrush=""{{ThemeResource CardStrokeColorDefaultBrush}}""
            BorderThickness=""1"" CornerRadius=""10"" Padding=""14,12"">
        <Grid ColumnDefinitions=""Auto,*"" ColumnSpacing=""12"">
            <Border Width=""38"" Height=""38"" CornerRadius=""8""
                    Background=""{{ThemeResource LayerFillColorDefaultBrush}}""
                    BorderBrush=""{{ThemeResource CardStrokeColorDefaultBrush}}""
                    BorderThickness=""1"">
                <FontIcon Glyph=""&#xE896;"" FontSize=""18"" Foreground=""{{ThemeResource AccentTextFillColorPrimaryBrush}}""
                          HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
            </Border>
            <StackPanel Grid.Column=""1"" Spacing=""2"" VerticalAlignment=""Center"">
                <TextBlock Text=""{System.Security.SecurityElement.Escape(metadata.FileName)}"" FontWeight=""SemiBold"" FontSize=""14""
                           TextTrimming=""CharacterEllipsis"" Foreground=""{{ThemeResource TextFillColorPrimaryBrush}}""/>
                <TextBlock Text=""{System.Security.SecurityElement.Escape(metadata.FileSizeFormatted)} • {metadata.LastModified:g}""
                           FontSize=""12"" Foreground=""{{ThemeResource TextFillColorSecondaryBrush}}""/>
            </StackPanel>
        </Grid>
    </Border>

    <Grid ColumnDefinitions=""*,*"" ColumnSpacing=""10"">
        <Border Background=""{{ThemeResource SubtleFillColorSecondaryBrush}}""
                BorderBrush=""{{ThemeResource CardStrokeColorDefaultBrush}}""
                BorderThickness=""1"" CornerRadius=""10"" Padding=""12,10"">
            <StackPanel Spacing=""2"">
                <TextBlock Text=""{System.Security.SecurityElement.Escape(Loc.GetString("Settings_DlgRestoreCurrentStats"))}""
                           FontSize=""11"" Foreground=""{{ThemeResource TextFillColorSecondaryBrush}}""/>
                <TextBlock Text=""{ViewModel.TotalWordsCount} {System.Security.SecurityElement.Escape(Loc.GetString("Vocab_NavTab").ToLower())} • {ViewModel.TotalGrammarCount} {System.Security.SecurityElement.Escape(Loc.GetString("Grammar_NavTab").ToLower())}""
                           FontSize=""13"" FontWeight=""Bold"" Foreground=""{{ThemeResource TextFillColorPrimaryBrush}}""/>
            </StackPanel>
        </Border>

        <Border Grid.Column=""1"" Background=""{{ThemeResource SubtleFillColorSecondaryBrush}}""
                BorderBrush=""{{ThemeResource CardStrokeColorDefaultBrush}}""
                BorderThickness=""1"" CornerRadius=""10"" Padding=""12,10"">
            <StackPanel Spacing=""2"">
                <TextBlock Text=""{System.Security.SecurityElement.Escape(Loc.GetString("Settings_DlgRestoreBackupStats"))}""
                           FontSize=""11"" Foreground=""{{ThemeResource AccentTextFillColorPrimaryBrush}}""/>
                <TextBlock Text=""{metadata.WordCount} {System.Security.SecurityElement.Escape(Loc.GetString("Vocab_NavTab").ToLower())} • {metadata.GrammarRuleCount} {System.Security.SecurityElement.Escape(Loc.GetString("Grammar_NavTab").ToLower())}""
                           FontSize=""13"" FontWeight=""Bold"" Foreground=""{{ThemeResource AccentTextFillColorPrimaryBrush}}""/>
            </StackPanel>
        </Border>
    </Grid>

    <Border Background=""{{ThemeResource SubtleFillColorSecondaryBrush}}""
            BorderBrush=""{{ThemeResource CardStrokeColorDefaultBrush}}""
            BorderThickness=""1"" CornerRadius=""10"" Padding=""12,10"">
        <TextBlock Text=""{System.Security.SecurityElement.Escape(string.Format(Loc.GetString("Settings_DlgRestoreStatsFormat"), metadata.WordCount, metadata.GrammarRuleCount, metadata.StudyLogCount))}""
                   FontSize=""12"" Foreground=""{{ThemeResource TextFillColorSecondaryBrush}}"" TextWrapping=""Wrap""/>
    </Border>

    <Border Background=""{{ThemeResource SystemFillColorCautionBackgroundBrush}}""
            BorderBrush=""{{ThemeResource SystemFillColorCautionBrush}}""
            BorderThickness=""1"" CornerRadius=""10"" Padding=""12,10"">
        <StackPanel Spacing=""4"">
            <TextBlock Text=""{System.Security.SecurityElement.Escape(Loc.GetString("Settings_DlgRestoreConfirmTitle"))}""
                       FontWeight=""SemiBold"" FontSize=""12"" Foreground=""{{ThemeResource TextFillColorPrimaryBrush}}""/>
            <TextBlock Text=""{System.Security.SecurityElement.Escape(Loc.GetString("Settings_DlgRestoreWarningNote"))}""
                       FontSize=""11"" Foreground=""{{ThemeResource TextFillColorSecondaryBrush}}"" TextWrapping=""Wrap""/>
        </StackPanel>
    </Border>
</StackPanel>";

            var previewContent = (UIElement)Microsoft.UI.Xaml.Markup.XamlReader.Load(previewXaml);

            var confirmDialog = new ContentDialog
            {
                Title = Loc.GetString("Settings_DlgRestorePreviewInfo"),
                Content = previewContent,
                PrimaryButtonText = Loc.GetString("Settings_DlgRestoreConfirmBtn"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = currentTheme
            };

            var closedTcs = new TaskCompletionSource();
            confirmDialog.Closed += (s, args) => closedTcs.TrySetResult();

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            // Wait for confirmDialog to completely close and detach from XamlRoot
            await closedTcs.Task;
            await Task.Delay(150);

            await DispatcherQueue.EnsureOnUIThreadAsync();
            if (this.XamlRoot == null) return;

            var restoreTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            var progressDialog = new OperationProgressDialog
            {
                Title = Loc.GetString("Progress_RestoreTitle"),
                XamlRoot = this.XamlRoot,
                RequestedTheme = restoreTheme
            };
            progressDialog.SetDeterminate(10, Loc.GetString("Progress_RestoreVerifying"), metadata.FileName);
            _ = progressDialog.ShowAsync();

            var progressReporter = new Progress<(int Percent, string StepMessage, string? Detail)>(report =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    progressDialog.SetDeterminate(report.Percent, report.StepMessage, report.Detail);
                });
            });

            try
            {
                var restored = await ViewModel.RestoreBackupAsync(selectedFilePath, progressReporter);
                if (restored)
                {
                    progressDialog.SetCompleted(
                        Loc.GetString("Progress_CompletedTitle"),
                        Loc.GetString("Settings_DlgRestoreSuccessMsg"),
                        imported: metadata.WordCount,
                        skipped: 0,
                        errors: 0,
                        closeButtonText: Loc.GetString("Action_Close"));
                }
                else
                {
                    progressDialog.SetError(
                        Loc.GetString("Settings_BackupFailed"),
                        ViewModel.BackupStatusMessage,
                        Loc.GetString("Action_Close"));
                }
            }
            catch (Exception ex)
            {
                progressDialog.SetError(
                    Loc.GetString("Settings_BackupFailed"),
                    ex.Message,
                    Loc.GetString("Action_Close"));
            }
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            if (this.XamlRoot != null)
            {
                var fallbackTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
                var errDlg = new ContentDialog
                {
                    Title = Loc.GetString("Settings_BackupFailed"),
                    Content = ex.Message,
                    CloseButtonText = Loc.GetString("Action_Close"),
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = fallbackTheme
                };
                await errDlg.ShowAsync();
            }
        }
    }

    private async void TestUkVoice_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.TestVoiceAsync("uk");
    }

    private async void TestUsVoice_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.TestVoiceAsync("us");
    }
}
