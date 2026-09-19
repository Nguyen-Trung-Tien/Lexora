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
        var progressDialog = new OperationProgressDialog
        {
            Title = Loc.GetString("Progress_BackupTitle"),
            XamlRoot = this.XamlRoot
        };
        progressDialog.SetDeterminate(10, Loc.GetString("Progress_BackupFlushing"), Loc.GetString("Progress_PleaseWait"));
        _ = progressDialog.ShowAsync();

        try
        {
            await Task.Delay(250);
            progressDialog.SetDeterminate(40, Loc.GetString("Progress_BackupCopying"), Loc.GetString("Progress_PleaseWait"));
            
            await ViewModel.CreateBackupAsync();
            
            progressDialog.SetDeterminate(85, Loc.GetString("Progress_BackupVerifying"), Loc.GetString("Progress_PleaseWait"));
            await Task.Delay(200);

            progressDialog.SetCompleted(
                Loc.GetString("Progress_CompletedTitle"),
                ViewModel.BackupStatusMessage,
                closeButtonText: Loc.GetString("Action_Close"));
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

        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".db");

            var mainWindow = App.Current.MainWindow;
            if (mainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                selectedFilePath = file.Path;
            }
        }
        catch
        {
            // Fallback to text path prompt if picker encounters WinRT uninitialized handle
        }

        if (string.IsNullOrEmpty(selectedFilePath))
        {
            var pathBox = new TextBox
            {
                PlaceholderText = @"C:\path\to\lexora_backup_xxxx.db",
                MaxWidth = 450,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var promptDialog = new ContentDialog
            {
                Title = Loc.GetString("Settings_DlgRestorePathTitle"),
                Content = pathBox,
                PrimaryButtonText = Loc.GetString("Settings_DlgRestorePathSelect"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var promptRes = await promptDialog.ShowAsync();
            if (promptRes == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(pathBox.Text))
            {
                selectedFilePath = pathBox.Text.Trim();
            }
        }

        if (!string.IsNullOrEmpty(selectedFilePath) && File.Exists(selectedFilePath))
        {
            await Task.Yield();
            var confirmDialog = new ContentDialog
            {
                Title = Loc.GetString("Settings_DlgRestoreConfirmTitle"),
                Content = string.Format(Loc.GetString("Settings_DlgRestoreConfirmMsg"), selectedFilePath),
                PrimaryButtonText = Loc.GetString("Settings_DlgRestoreConfirmBtn"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await Task.Yield();

                var progressDialog = new OperationProgressDialog
                {
                    Title = Loc.GetString("Progress_RestoreTitle"),
                    XamlRoot = this.XamlRoot
                };
                progressDialog.SetDeterminate(15, Loc.GetString("Progress_RestoreVerifying"), Loc.GetString("Progress_PleaseWait"));
                _ = progressDialog.ShowAsync();

                try
                {
                    await Task.Delay(250);
                    progressDialog.SetDeterminate(60, Loc.GetString("Progress_RestoreApplying"), Loc.GetString("Progress_PleaseWait"));

                    var restored = await ViewModel.RestoreBackupAsync(selectedFilePath);
                    if (restored)
                    {
                        progressDialog.SetCompleted(
                            Loc.GetString("Progress_CompletedTitle"),
                            Loc.GetString("Settings_DlgRestoreSuccessMsg"),
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
