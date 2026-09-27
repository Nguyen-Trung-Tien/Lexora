using Lexora.Application.DTOs;
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
            UpdateAccentButtons();
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateLanguageButtons();
            UpdateThemeButtons();
            UpdateAccentButtons();
            ViewModel.UpdateVoiceStatus();
            Bindings.Update();
        });
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.InitializeAsync();
            UpdateLanguageButtons();
            UpdateThemeButtons();
            UpdateAccentButtons();

            SpeechRateCombo.SelectedIndex = ViewModel.SpeechRate switch
            {
                <= 0.8 => 0,
                <= 1.1 => 1,
                <= 1.35 => 2,
                _ => 3
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] Loaded error: {ex.Message}");
        }
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

    private void UpdateAccentButtons()
    {
        bool isUk = ViewModel.PreferredAccent.Equals("uk", StringComparison.OrdinalIgnoreCase);
        var cardBg = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
        var cardStroke = GetThemeBrush("CardStrokeColorDefaultBrush");
        var accentText = GetThemeBrush("AccentTextFillColorPrimaryBrush");
        var subtleText = GetThemeBrush("TextFillColorSecondaryBrush");
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        void ApplyAccentBtn(Button btn, TextBlock text, FontIcon check, bool isActive)
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

        ApplyAccentBtn(AccentUkButton, TextAccentUk, CheckAccentUk, isUk);
        ApplyAccentBtn(AccentUsButton, TextAccentUs, CheckAccentUs, !isUk);
    }

    private async void AccentUkButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SetPreferredAccentAsync("uk");
        DispatcherQueue.TryEnqueue(UpdateAccentButtons);
    }

    private async void AccentUsButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SetPreferredAccentAsync("us");
        DispatcherQueue.TryEnqueue(UpdateAccentButtons);
    }

    private async void TestVoiceUk_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.TestVoiceAsync("uk");
    }

    private async void TestVoiceUs_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.TestVoiceAsync("us");
    }

    private async void OpenSpeechSettings_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenWindowsSpeechSettingsAsync();
    }

    private async void CopyDbPath_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(ViewModel.DatabasePath);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

            // Visual feedback: checkmark + success color — must be on UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                IconCopyDbPath.Glyph = "\uE73E";
                var bSuccess = GetThemeBrush("SystemFillColorSuccessBrush");
                if (bSuccess != null)
                {
                    IconCopyDbPath.Foreground = bSuccess;
                    TextCopyDbPath.Foreground = bSuccess;
                }
                TextCopyDbPath.Text = Loc.GetString("Action_Copied");
            });

            await Task.Delay(1800);

            // Restore — also must be on UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                IconCopyDbPath.Glyph = "\uE8C8";
                IconCopyDbPath.ClearValue(FontIcon.ForegroundProperty);
                TextCopyDbPath.Text = Loc.GetString("Action_Copy");
                TextCopyDbPath.ClearValue(TextBlock.ForegroundProperty);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] CopyDbPath error: {ex.Message}");
        }
    }

    private async void SpeechRateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (SpeechRateCombo?.SelectedItem is ComboBoxItem item &&
                item.Tag is string tagStr &&
                double.TryParse(tagStr, System.Globalization.CultureInfo.InvariantCulture, out var rate))
            {
                await ViewModel.SetSpeechRateAsync(rate);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] SpeechRate error: {ex.Message}");
        }
    }

    private async void TypingPracticeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is ToggleSwitch ts)
            {
                await ViewModel.SetTypingPracticeAsync(ts.IsOn);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] TypingPracticeToggle error: {ex.Message}");
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
        try
        {
            if (sender is ToggleSwitch ts)
            {
                await ViewModel.SetAutoPlayAudioAsync(ts.IsOn);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] AutoPlay error: {ex.Message}");
        }
    }

    private async void ReminderToggle_Toggled(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is ToggleSwitch ts)
            {
                await ViewModel.SetReviewReminderAsync(ts.IsOn);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] Reminder error: {ex.Message}");
        }
    }

    private async void RefreshStatsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.LoadDatabaseStatsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] RefreshStats error: {ex.Message}");
        }
    }

    private async void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        try
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
            _ = progressDialog.ShowAsyncSafe();

            try
            {
                await Task.Delay(200);
                await DispatcherQueue.EnsureOnUIThreadAsync();
                progressDialog.SetDeterminate(40, Loc.GetString("Progress_BackupCopying"), Loc.GetString("Progress_PleaseWait"));

                var backupPath = await ViewModel.CreateBackupAsync(targetFilePath);
                await DispatcherQueue.EnsureOnUIThreadAsync();

                progressDialog.SetDeterminate(85, Loc.GetString("Progress_BackupVerifying"), Loc.GetString("Progress_PleaseWait"));
                await Task.Delay(200);
                await DispatcherQueue.EnsureOnUIThreadAsync();

                if (!string.IsNullOrEmpty(backupPath))
                {
                    progressDialog.SetCompleted(
                        Loc.GetString("Settings_BackupSuccessTitle"),
                        ViewModel.BackupStatusMessage,
                        closeButtonText: Loc.GetString("Action_Close"));
                }
                else
                {
                    progressDialog.SetError(
                        Loc.GetString("Settings_BackupFailedTitle"),
                        ViewModel.BackupStatusMessage,
                        Loc.GetString("Action_Close"));
                }
            }
            catch (Exception ex)
            {
                await DispatcherQueue.EnsureOnUIThreadAsync();
                progressDialog.SetError(
                    Loc.GetString("Settings_BackupFailedTitle"),
                    ex.Message,
                    Loc.GetString("Action_Close"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] BackupButton error: {ex.Message}");
        }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        try
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

                var promptRes = await promptDialog.ShowAsyncSafe();
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
                        Title = Loc.GetString("Settings_RestoreFailedTitle"),
                        Content = string.Format(Loc.GetString("Settings_DlgRestoreInvalidFile"), metadata.ErrorMessage),
                        CloseButtonText = Loc.GetString("Action_Close"),
                        XamlRoot = this.XamlRoot,
                        RequestedTheme = currentTheme
                    };
                    await errorDialog.ShowAsyncSafe();
                    return;
                }

                // 2. Build high-fidelity Fluent 2 preview confirmation layout
                var previewContent = CreateRestorePreviewContent(metadata);

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

                var result = await confirmDialog.ShowAsyncSafe();
                if (result != ContentDialogResult.Primary)
                {
                    return;
                }

                await Task.Yield();

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
                _ = progressDialog.ShowAsyncSafe();

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
                    await DispatcherQueue.EnsureOnUIThreadAsync();

                    if (restored)
                    {
                        progressDialog.SetCompleted(
                            Loc.GetString("Settings_RestoreSuccessTitle"),
                            Loc.GetString("Settings_DlgRestoreSuccessMsg"),
                            imported: metadata.WordCount,
                            skipped: 0,
                            errors: 0,
                            closeButtonText: Loc.GetString("Action_Close"));
                    }
                    else
                    {
                        progressDialog.SetError(
                            Loc.GetString("Settings_RestoreFailedTitle"),
                            ViewModel.BackupStatusMessage,
                            Loc.GetString("Action_Close"));
                    }
                }
                catch (Exception ex)
                {
                    await DispatcherQueue.EnsureOnUIThreadAsync();
                    progressDialog.SetError(
                        Loc.GetString("Settings_RestoreFailedTitle"),
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
                        Title = Loc.GetString("Settings_RestoreFailedTitle"),
                        Content = ex.Message,
                        CloseButtonText = Loc.GetString("Action_Close"),
                        XamlRoot = this.XamlRoot,
                        RequestedTheme = fallbackTheme
                    };
                    await errDlg.ShowAsyncSafe();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] RestoreButton error: {ex.Message}");
        }
    }

    private async void TestUkVoice_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.TestVoiceAsync("uk");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] TestUkVoice error: {ex.Message}");
        }
    }

    private async void TestUsVoice_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.TestVoiceAsync("us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] TestUsVoice error: {ex.Message}");
        }
    }

    private UIElement CreateRestorePreviewContent(BackupMetadata metadata)
    {
        var panel = new StackPanel { Spacing = 14, MaxWidth = 460 };

        // File Header Card
        var fileCard = new Border
        {
            Background = ThemeResourceHelper.GetThemeBrush(this, "SubtleFillColorSecondaryBrush"),
            BorderBrush = ThemeResourceHelper.GetThemeBrush(this, "CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12)
        };

        var fileGrid = new Grid { ColumnSpacing = 12 };
        fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconBorder = new Border
        {
            Width = 38,
            Height = 38,
            CornerRadius = new CornerRadius(8),
            Background = ThemeResourceHelper.GetThemeBrush(this, "LayerFillColorDefaultBrush"),
            BorderBrush = ThemeResourceHelper.GetThemeBrush(this, "CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            Child = new FontIcon
            {
                Glyph = "\uE896",
                FontSize = 18,
                Foreground = ThemeResourceHelper.GetThemeBrush(this, "AccentTextFillColorPrimaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(iconBorder, 0);

        var fileInfoStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        fileInfoStack.Children.Add(new TextBlock
        {
            Text = metadata.FileName,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 14,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorPrimaryBrush")
        });
        fileInfoStack.Children.Add(new TextBlock
        {
            Text = $"{metadata.FileSizeFormatted} • {metadata.LastModified:g}",
            FontSize = 12,
            Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorSecondaryBrush")
        });
        Grid.SetColumn(fileInfoStack, 1);

        fileGrid.Children.Add(iconBorder);
        fileGrid.Children.Add(fileInfoStack);
        fileCard.Child = fileGrid;
        panel.Children.Add(fileCard);

        // Stats Comparison Grid
        var statsGrid = new Grid { ColumnSpacing = 10 };
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var currentBorder = new Border
        {
            Background = ThemeResourceHelper.GetThemeBrush(this, "SubtleFillColorSecondaryBrush"),
            BorderBrush = ThemeResourceHelper.GetThemeBrush(this, "CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10)
        };
        var currentStack = new StackPanel { Spacing = 2 };
        currentStack.Children.Add(new TextBlock
        {
            Text = Loc.GetString("Settings_DlgRestoreCurrentStats"),
            FontSize = 11,
            Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorSecondaryBrush")
        });
        currentStack.Children.Add(new TextBlock
        {
            Text = $"{ViewModel.TotalWordsCount} {Loc.GetString("Vocab_NavTab").ToLower()} • {ViewModel.TotalGrammarCount} {Loc.GetString("Grammar_NavTab").ToLower()}",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorPrimaryBrush")
        });
        currentBorder.Child = currentStack;
        Grid.SetColumn(currentBorder, 0);

        var backupBorder = new Border
        {
            Background = ThemeResourceHelper.GetThemeBrush(this, "SubtleFillColorSecondaryBrush"),
            BorderBrush = ThemeResourceHelper.GetThemeBrush(this, "CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10)
        };
        var backupStack = new StackPanel { Spacing = 2 };
        backupStack.Children.Add(new TextBlock
        {
            Text = Loc.GetString("Settings_DlgRestoreBackupStats"),
            FontSize = 11,
            Foreground = ThemeResourceHelper.GetThemeBrush(this, "AccentTextFillColorPrimaryBrush")
        });
        backupStack.Children.Add(new TextBlock
        {
            Text = $"{metadata.WordCount} {Loc.GetString("Vocab_NavTab").ToLower()} • {metadata.GrammarRuleCount} {Loc.GetString("Grammar_NavTab").ToLower()}",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = ThemeResourceHelper.GetThemeBrush(this, "AccentTextFillColorPrimaryBrush")
        });
        backupBorder.Child = backupStack;
        Grid.SetColumn(backupBorder, 1);

        statsGrid.Children.Add(currentBorder);
        statsGrid.Children.Add(backupBorder);
        panel.Children.Add(statsGrid);

        // Detailed Stats
        var detailsBorder = new Border
        {
            Background = ThemeResourceHelper.GetThemeBrush(this, "SubtleFillColorSecondaryBrush"),
            BorderBrush = ThemeResourceHelper.GetThemeBrush(this, "CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10),
            Child = new TextBlock
            {
                Text = string.Format(Loc.GetString("Settings_DlgRestoreStatsFormat"), metadata.WordCount, metadata.GrammarRuleCount, metadata.StudyLogCount),
                FontSize = 12,
                Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorSecondaryBrush"),
                TextWrapping = TextWrapping.Wrap
            }
        };
        panel.Children.Add(detailsBorder);

        // Warning Border
        var warningBorder = new Border
        {
            Background = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCautionBackgroundBrush"),
            BorderBrush = ThemeResourceHelper.GetThemeBrush(this, "SystemFillColorCautionBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10)
        };
        var warningStack = new StackPanel { Spacing = 4 };
        warningStack.Children.Add(new TextBlock
        {
            Text = Loc.GetString("Settings_DlgRestoreConfirmTitle"),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 12,
            Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorPrimaryBrush")
        });
        warningStack.Children.Add(new TextBlock
        {
            Text = Loc.GetString("Settings_DlgRestoreWarningNote"),
            FontSize = 11,
            Foreground = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap
        });
        warningBorder.Child = warningStack;
        panel.Children.Add(warningBorder);

        return panel;
    }
}
