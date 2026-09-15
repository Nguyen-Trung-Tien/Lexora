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
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        UpdateLanguageButtons();

        SpeechRateCombo.SelectedIndex = ViewModel.SpeechRate switch
        {
            <= 0.8 => 0,
            <= 1.1 => 1,
            <= 1.35 => 2,
            _ => 3
        };
    }

    private void UpdateLanguageButtons()
    {
        bool isVi = Loc.CurrentLanguage == "vi-VN";
        if (App.Current.Resources.TryGetValue(isVi ? "AccentButtonStyle" : "DefaultButtonStyle", out var viStyle) && viStyle is Style vs)
        {
            LangViButton.Style = vs;
        }
        if (App.Current.Resources.TryGetValue(!isVi ? "AccentButtonStyle" : "DefaultButtonStyle", out var enStyle) && enStyle is Style es)
        {
            LangEnButton.Style = es;
        }
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

    private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is RadioButtons rb)
        {
            ViewModel.ChangeTheme(rb.SelectedIndex);
        }
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
        await ViewModel.CreateBackupAsync();
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
                Title = "Specify Backup File Path",
                Content = pathBox,
                PrimaryButtonText = "Select",
                CloseButtonText = "Cancel",
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
                Title = "Restore Database Confirmation",
                Content = $"Are you sure you want to restore database from:\n{selectedFilePath}\n\nWARNING: All current local data will be replaced by the backup copy!",
                PrimaryButtonText = "Restore Now",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var restored = await ViewModel.RestoreBackupAsync(selectedFilePath);
                if (restored)
                {
                    await Task.Yield();
                    var doneDialog = new ContentDialog
                    {
                        Title = "Restore Completed",
                        Content = "Database has been restored successfully. It is recommended to restart the application to reload all active caches.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await doneDialog.ShowAsync();
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
