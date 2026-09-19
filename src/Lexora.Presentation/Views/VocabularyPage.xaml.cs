using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;

namespace Lexora.Presentation.Views;

public sealed partial class VocabularyPage : Page
{
    public VocabularyViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }
    private bool _isInitializing = true;

    public VocabularyPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<VocabularyViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();

        _isInitializing = true;

        PopulateFilterCombos();

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
            await ViewModel.LoadWordsAsync();
            ViewModel.ApplyUnitFilter();
        };

        Loaded += async (s, e) =>
        {
            _isInitializing = true;
            await ViewModel.InitializeAsync();
            PopulateTopicFilterCombo();
            UpdateViewModeButtons();
            _isInitializing = false;
        };

        ActualThemeChanged += (s, e) => UpdateViewModeButtons();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var wasInit = _isInitializing;
            _isInitializing = true;
            PopulateFilterCombos();
            PopulateTopicFilterCombo();
            _isInitializing = wasInit;
            Bindings.Update();
        });
    }

    private void PopulateFilterCombos()
    {
        var prevLevelIdx = LevelFilterCombo.SelectedIndex >= 0 ? LevelFilterCombo.SelectedIndex : 0;
        var prevPosIdx = PosFilterCombo.SelectedIndex >= 0 ? PosFilterCombo.SelectedIndex : 0;
        var prevStatusIdx = StatusFilterCombo.SelectedIndex >= 0 ? StatusFilterCombo.SelectedIndex : 0;
        var prevSortIdx = SortCombo.SelectedIndex >= 0 ? SortCombo.SelectedIndex : 0;

        // Populate Level combo
        var levels = new List<object> { Loc.GetString("Vocab_AllLevels") };
        levels.AddRange(Enum.GetValues<CefrLevel>().Cast<object>());
        LevelFilterCombo.ItemsSource = levels;
        LevelFilterCombo.SelectedIndex = prevLevelIdx < levels.Count ? prevLevelIdx : 0;

        // Populate POS combo
        var posList = new List<object> { Loc.GetString("Vocab_AllPos") };
        posList.AddRange(Enum.GetValues<PartOfSpeech>().Cast<object>());
        PosFilterCombo.ItemsSource = posList;
        PosFilterCombo.SelectedIndex = prevPosIdx < posList.Count ? prevPosIdx : 0;

        // Populate Status combo
        var statuses = new List<object> { Loc.GetString("Vocab_AllStatus") };
        statuses.AddRange(Enum.GetValues<LearningStatus>().Cast<object>());
        StatusFilterCombo.ItemsSource = statuses;
        StatusFilterCombo.SelectedIndex = prevStatusIdx < statuses.Count ? prevStatusIdx : 0;

        // Populate Sort combo
        SortCombo.ItemsSource = new List<string>
        {
            Loc.GetString("Vocab_SortTermAsc"),
            Loc.GetString("Vocab_SortTermDesc"),
            Loc.GetString("Vocab_SortLevelAsc"),
            Loc.GetString("Vocab_SortLevelDesc"),
            Loc.GetString("Vocab_SortDateNewest"),
            Loc.GetString("Vocab_SortDateOldest")
        };
        SortCombo.SelectedIndex = prevSortIdx < 6 ? prevSortIdx : 0;
    }

    private void PopulateTopicFilterCombo()
    {
        var currentSelectedId = ViewModel.SelectedTopicId;
        TopicFilterCombo.SelectionChanged -= TopicFilterCombo_SelectionChanged;
        TopicFilterCombo.Items.Clear();

        var allItem = new ComboBoxItem
        {
            Content = Loc.GetString("Vocab_AllTopics"),
            Tag = null
        };
        ToolTipService.SetToolTip(allItem, Loc.GetString("Vocab_AllTopics"));
        TopicFilterCombo.Items.Add(allItem);

        int selectedIndex = 0;
        int idx = 1;
        foreach (var topic in ViewModel.AvailableTopics)
        {
            var item = new ComboBoxItem
            {
                Content = topic.Name,
                Tag = topic.Id
            };
            ToolTipService.SetToolTip(item, topic.Name);
            TopicFilterCombo.Items.Add(item);

            if (currentSelectedId.HasValue && topic.Id == currentSelectedId.Value)
            {
                selectedIndex = idx;
            }
            idx++;
        }

        TopicFilterCombo.SelectedIndex = selectedIndex;
        if (TopicFilterCombo.SelectedItem is ComboBoxItem initialCbi && initialCbi.Content != null)
        {
            ToolTipService.SetToolTip(TopicFilterCombo, initialCbi.Content.ToString());
        }
        TopicFilterCombo.SelectionChanged += TopicFilterCombo_SelectionChanged;
    }

    private readonly DispatcherTimer _searchDebounceTimer;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string query && !string.IsNullOrWhiteSpace(query))
        {
            ViewModel.SearchTerm = query;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private async void TopicFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (TopicFilterCombo.SelectedItem is ComboBoxItem cbi)
        {
            ToolTipService.SetToolTip(TopicFilterCombo, cbi.Content?.ToString());
            ViewModel.SelectedTopicId = cbi.Tag as Guid?;
        }
        else
        {
            ViewModel.SelectedTopicId = null;
            ToolTipService.SetToolTip(TopicFilterCombo, null);
        }
        await ViewModel.LoadWordsAsync();
    }


    private async void LevelFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (LevelFilterCombo.SelectedItem is CefrLevel level)
        {
            ViewModel.SelectedLevel = level;
        }
        else
        {
            ViewModel.SelectedLevel = null;
        }
        await ViewModel.LoadWordsAsync();
    }

    private async void PosFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (PosFilterCombo.SelectedItem is PartOfSpeech pos)
        {
            ViewModel.SelectedPartOfSpeech = pos;
        }
        else
        {
            ViewModel.SelectedPartOfSpeech = null;
        }
        await ViewModel.LoadWordsAsync();
    }

    private async void StatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (StatusFilterCombo.SelectedItem is LearningStatus status)
        {
            ViewModel.SelectedStatus = status;
        }
        else
        {
            ViewModel.SelectedStatus = null;
        }
        await ViewModel.LoadWordsAsync();
    }

    private async void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        switch (SortCombo.SelectedIndex)
        {
            case 0: // Term A-Z
                ViewModel.SortBy = "term";
                ViewModel.IsAscending = true;
                break;
            case 1: // Term Z-A
                ViewModel.SortBy = "term";
                ViewModel.IsAscending = false;
                break;
            case 2: // Level low-high
                ViewModel.SortBy = "level";
                ViewModel.IsAscending = true;
                break;
            case 3: // Level high-low
                ViewModel.SortBy = "level";
                ViewModel.IsAscending = false;
                break;
            case 4: // Date newest
                ViewModel.SortBy = "createdat";
                ViewModel.IsAscending = false;
                break;
            case 5: // Date oldest
                ViewModel.SortBy = "createdat";
                ViewModel.IsAscending = true;
                break;
        }
        await ViewModel.LoadWordsAsync();
    }

    private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        TopicFilterCombo.SelectedIndex = 0;
        LevelFilterCombo.SelectedIndex = 0;
        PosFilterCombo.SelectedIndex = 0;
        StatusFilterCombo.SelectedIndex = 0;
        SortCombo.SelectedIndex = 0;
        UpdateQuickFilterStyles("All");
        _ = ViewModel.SelectQuickFilterAsync("All");
    }

    private async void QuickFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string filter)
        {
            await ViewModel.SelectQuickFilterAsync(filter);
            UpdateQuickFilterStyles(filter);
        }
    }

    private void UpdateQuickFilterStyles(string activeFilter)
    {
        SetChipStyle(ChipFilterAll, activeFilter == "All");
        SetChipStyle(ChipFilterDueToday, activeFilter == "DueToday");
        SetChipStyle(ChipFilterWeak, activeFilter == "Weak");
        SetChipStyle(ChipFilterLearning, activeFilter == "Learning");
        SetChipStyle(ChipFilterMastered, activeFilter == "Mastered");
        SetChipStyle(ChipFilterNew, activeFilter == "New");
    }

    private void SetChipStyle(Button? btn, bool isActive)
    {
        if (btn == null) return;
        if (isActive)
        {
            btn.Style = (Style)App.Current.Resources["AccentButtonStyle"];
        }
        else
        {
            btn.Style = (Style)App.Current.Resources["DefaultButtonStyle"];
        }
    }

    private async void AddWordButton_Click(object sender, RoutedEventArgs e)
    {
        var soundFeedback = App.Current.Services.GetService<Lexora.Presentation.Services.ISoundFeedbackService>();
        soundFeedback?.PlaySoftTapSound();

        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var dialog = new WordEditorDialog(null, ViewModel.AvailableTopics, ViewModel.AvailableExamTags)
        {
            XamlRoot = this.XamlRoot,
            RequestedTheme = activeTheme
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
        {
            await ViewModel.SaveWordAsync(dialog.ResultDto);
            PopulateTopicFilterCombo();

            CardSaveSuccessStoryboard.Begin();
            soundFeedback?.PlayCardFlipSound();
        }
    }

    private async void CopyWordButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWord == null) return;

        try
        {
            var textToCopy = ViewModel.SelectedWord.Term;
            if (!string.IsNullOrWhiteSpace(ViewModel.SelectedWord.PrimaryDefinition))
            {
                textToCopy += $" - {ViewModel.SelectedWord.PrimaryDefinition}";
            }

            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(textToCopy);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

            // Visual feedback: checkmark + "Đã sao chép!"
            IconCopyWord.Glyph = "\uE73E";
            if (App.Current.Resources.TryGetValue("SystemFillColorSuccessBrush", out var successBrush) && successBrush is Microsoft.UI.Xaml.Media.Brush bSuccess)
            {
                IconCopyWord.Foreground = bSuccess;
                TextCopyWord.Foreground = bSuccess;
            }
            TextCopyWord.Text = Loc.GetString("Action_Copied");

            await Task.Delay(1800);

            IconCopyWord.Glyph = "\uE8C8";
            IconCopyWord.ClearValue(FontIcon.ForegroundProperty);
            TextCopyWord.Text = Loc.GetString("Action_Copy");
            TextCopyWord.ClearValue(TextBlock.ForegroundProperty);
        }
        catch
        {
            // Clipboard fallback
        }
    }

    private async void EditWordButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWord == null) return;

        // Smooth connected pre-launch card spring
        CardMorphStoryboard.Begin();
        var soundFeedback = App.Current.Services.GetService<Lexora.Presentation.Services.ISoundFeedbackService>();
        soundFeedback?.PlaySoftTapSound();

        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var dialog = new WordEditorDialog(ViewModel.SelectedWord, ViewModel.AvailableTopics, ViewModel.AvailableExamTags)
        {
            XamlRoot = this.XamlRoot,
            RequestedTheme = activeTheme
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
        {
            await ViewModel.SaveWordAsync(dialog.ResultDto);
            PopulateTopicFilterCombo();

            // Smooth return bounce + pleasant completion feedback
            CardSaveSuccessStoryboard.Begin();
            soundFeedback?.PlayCardFlipSound();
        }
    }

    private async void DeleteWordButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWord == null) return;

        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Vocab_DeleteTitle"),
            Content = string.Format(Loc.GetString("Vocab_DeleteConfirm"), ViewModel.SelectedWord.Term),
            PrimaryButtonText = Loc.GetString("Action_Delete"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteWordAsync(ViewModel.SelectedWord.Id);
        }
    }

    private async void PlayUkAudio_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlayPronunciationAsync("uk");
    }

    private async void PlayUsAudio_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlayPronunciationAsync("us");
    }

    private async void QuickListenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is WordDto w)
        {
            var audioService = App.Current.Services.GetRequiredService<Services.IAudioPlayerService>();
            await audioService.PlayPronunciationAsync(w.Term, "uk");
        }
    }

    private async void RememberedButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWord == null) return;
        try
        {
            if (sender is Button btn) btn.IsEnabled = false;
            await ViewModel.MarkWordStatusAsync(remembered: true);
        }
        catch (Exception ex)
        {
            ShowErrorDialog(Loc.GetString("Vocab_ErrorMarkStatus"), ex.Message);
        }
        finally
        {
            if (sender is Button btn2) btn2.IsEnabled = true;
        }
    }

    private async void NotRememberedButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWord == null) return;
        try
        {
            if (sender is Button btn) btn.IsEnabled = false;
            await ViewModel.MarkWordStatusAsync(remembered: false);
        }
        catch (Exception ex)
        {
            ShowErrorDialog(Loc.GetString("Vocab_ErrorMarkStatus"), ex.Message);
        }
        finally
        {
            if (sender is Button btn2) btn2.IsEnabled = true;
        }
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        await PerformExportAsync("json", "JSON Vocabulary File", ".json", (p) => ViewModel.ExportToJsonAsync(p));
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        await PerformExportAsync("csv", "CSV Vocabulary File", ".csv", (p) => ViewModel.ExportToCsvAsync(p));
    }

    private async void ExportTxt_Click(object sender, RoutedEventArgs e)
    {
        await PerformExportAsync("txt", "TXT Vocabulary File", ".txt", (p) => ViewModel.ExportToTxtAsync(p));
    }

    private async Task PerformExportAsync(string format, string fileTypeDesc, string extension, Func<IProgress<ProgressReport>, Task<string>> exportFunc)
    {
        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
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
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    progressDialog.SetDeterminate(report.Percent, report.Message, report.StepDetail);
                });
            });

            var content = await exportFunc(progress);
            progressDialog.Hide();
            await Task.Yield();

            var defaultName = $"lexora_export_{DateTime.Now:yyyyMMdd_HHmmss}";
            await ExportFileAsync(content, defaultName, extension, fileTypeDesc);
        }
        catch (Exception ex)
        {
            progressDialog.Hide();
            await Task.Yield();
            ShowErrorDialog(Loc.GetString("Vocab_ExportFailed"), ex.Message);
        }
    }

    private async Task ExportFileAsync(string content, string defaultName, string extension, string fileTypeDescription)
    {
        string? savedPath = null;
        try
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            savePicker.FileTypeChoices.Add(fileTypeDescription, new List<string> { extension });
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
            else
            {
                // User cancelled
                return;
            }
        }
        catch
        {
            // Fallback: save to Desktop if picker initialization fails
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            savedPath = Path.Combine(desktop, $"{defaultName}{extension}");
            await File.WriteAllTextAsync(savedPath, content);
        }

        if (!string.IsNullOrEmpty(savedPath))
        {
            var exportCompleteTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            var dialog = new ContentDialog
            {
                Title = Loc.GetString("Vocab_ExportCompleteTitle"),
                Content = string.Format(Loc.GetString("Vocab_ExportCompleteMsg"), ViewModel.Words.Count, savedPath),
                CloseButtonText = Loc.GetString("Action_Close"),
                XamlRoot = this.XamlRoot,
                RequestedTheme = exportCompleteTheme
            };
            await dialog.ShowAsync();
        }
    }

    private async void ImportJson_Click(object sender, RoutedEventArgs e)
    {
        await ShowImportDialogAsync("json");
    }

    private async void ImportCsv_Click(object sender, RoutedEventArgs e)
    {
        await ShowImportDialogAsync("csv");
    }

    private async void ImportTxt_Click(object sender, RoutedEventArgs e)
    {
        await ShowImportDialogAsync("txt");
    }

    private async Task ShowImportDialogAsync(string format)
    {
        string fileExt = format.ToLowerInvariant() switch
        {
            "json" => ".json",
            "csv" => ".csv",
            _ => ".txt"
        };
        string filterName = format.ToLowerInvariant() switch
        {
            "json" => "JSON Files",
            "csv" => "CSV Files",
            _ => "TXT Files"
        };
        string? contentToImport = null;

        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(fileExt);

            var mainWindow = App.Current.MainWindow;
            if (mainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                contentToImport = await Windows.Storage.FileIO.ReadTextAsync(file);
            }
        }
        catch
        {
            // Fallback to text prompt
        }

        if (string.IsNullOrEmpty(contentToImport))
        {
            var placeholder = format.ToLowerInvariant() switch
            {
                "json" => Loc.GetString("Vocab_ImportPastePlaceholderJson"),
                "csv" => Loc.GetString("Vocab_ImportPastePlaceholderCsv"),
                _ => Loc.GetString("Vocab_ImportPastePlaceholderTxt")
            };

            var dialogTitle = format.ToLowerInvariant() switch
            {
                "json" => Loc.GetString("Vocab_ImportJsonTitle"),
                "csv" => Loc.GetString("Vocab_ImportCsvTitle"),
                _ => Loc.GetString("Vocab_ImportTxtTitle")
            };

            var textBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 250,
                PlaceholderText = placeholder
            };

            var dialog = new ContentDialog
            {
                Title = dialogTitle,
                Content = textBox,
                PrimaryButtonText = Loc.GetString("Vocab_ImportExport"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var res = await dialog.ShowAsync();
            if (res == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                contentToImport = textBox.Text;
            }
        }

        if (!string.IsNullOrWhiteSpace(contentToImport))
        {
            await Task.Yield();

            var importTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            var progressDialog = new OperationProgressDialog
            {
                Title = Loc.GetString("Progress_ImportingTitle"),
                XamlRoot = this.XamlRoot,
                RequestedTheme = importTheme
            };
            progressDialog.SetDeterminate(0, Loc.GetString("Progress_ReadingFile"), Loc.GetString("Progress_PleaseWait"));
            _ = progressDialog.ShowAsync();

            var progress = new Progress<ProgressReport>(report =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    progressDialog.SetDeterminate(report.Percent, report.Message, report.StepDetail);
                });
            });

            try
            {
                var importResult = await ViewModel.ImportDataAsync(contentToImport, format, progress);
                PopulateTopicFilterCombo();

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
                    Loc.GetString("Vocab_ImportFailed"),
                    ex.Message,
                    Loc.GetString("Action_Close"));
            }
        }
    }

    private async void ShowErrorDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = Loc.GetString("Action_Close"),
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void AddWordAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        AddWordButton_Click(this, new RoutedEventArgs());
        args.Handled = true;
    }

    private void EditWordAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.SelectedWord != null)
        {
            EditWordButton_Click(this, new RoutedEventArgs());
            args.Handled = true;
        }
    }

    private void DeleteWordAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.SelectedWord != null)
        {
            DeleteWordButton_Click(this, new RoutedEventArgs());
            args.Handled = true;
        }
    }

    private void TestThisWordButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWord != null)
        {
            var navService = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.INavigationService>();
            navService.Navigate(typeof(TestPage), new[] { ViewModel.SelectedWord.Id });
        }
    }

    private void ReviewThisWordButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWord != null)
        {
            var navService = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.INavigationService>();
            navService.Navigate(typeof(ReviewPage), new[] { ViewModel.SelectedWord.Id });
        }
    }

    private void BtnViewList_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SwitchToListView();
        UpdateViewModeButtons();
    }

    private void BtnViewUnits_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SwitchToUnitView();
        UpdateViewModeButtons();
    }

    private Microsoft.UI.Xaml.Media.Brush? GetThemeBrush(string key)
    {
        return Services.ThemeResourceHelper.GetThemeBrush(this, key);
    }

    private void UpdateViewModeButtons()
    {
        var cardBg = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
        var cardStroke = GetThemeBrush("CardStrokeColorDefaultBrush");
        var accentText = GetThemeBrush("AccentTextFillColorPrimaryBrush");
        var secondaryText = GetThemeBrush("TextFillColorSecondaryBrush");
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        void ApplySegment(Button btn, bool isActive)
        {
            btn.Style = null;
            if (isActive)
            {
                if (cardBg != null) btn.Background = cardBg;
                if (cardStroke != null) btn.BorderBrush = cardStroke;
                btn.BorderThickness = new Thickness(1);
                if (accentText != null) btn.Foreground = accentText;
            }
            else
            {
                btn.Background = transparent;
                btn.BorderThickness = new Thickness(0);
                if (secondaryText != null) btn.Foreground = secondaryText;
            }
        }

        ApplySegment(BtnViewList, !ViewModel.IsUnitViewMode);
        ApplySegment(BtnViewUnits, ViewModel.IsUnitViewMode);
    }

    private async void ViewUnitWords_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && (fe.Tag as ContextUnitDto ?? fe.DataContext as ContextUnitDto) is ContextUnitDto unit)
        {
            await ViewModel.FilterByUnitAsync(unit);

            // Sync TopicFilterCombo selection
            if (unit.Id == Guid.Empty)
            {
                TopicFilterCombo.SelectedIndex = 0;
            }
            else
            {
                for (int i = 0; i < TopicFilterCombo.Items.Count; i++)
                {
                    if (TopicFilterCombo.Items[i] is ComboBoxItem cbi && cbi.Tag is Guid gid && gid == unit.Id)
                    {
                        TopicFilterCombo.SelectedIndex = i;
                        break;
                    }
                }
            }

            UpdateViewModeButtons();
        }
    }

    private void TestUnit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && (fe.Tag as ContextUnitDto ?? fe.DataContext as ContextUnitDto) is ContextUnitDto unit)
        {
            if (unit.Words.Count > 0)
            {
                var navService = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.INavigationService>();
                navService.Navigate(typeof(TestPage), unit.Words.Select(w => w.Id).ToList());
            }
            else
            {
                ShowErrorDialog(Loc.GetString("Vocab_NoWordsInUnitTitle"), string.Format(Loc.GetString("Vocab_NoWordsInUnitTestMsg"), unit.Name));
            }
        }
    }

    private void ReviewUnit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && (fe.Tag as ContextUnitDto ?? fe.DataContext as ContextUnitDto) is ContextUnitDto unit)
        {
            if (unit.Words.Count > 0)
            {
                var navService = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.INavigationService>();
                navService.Navigate(typeof(ReviewPage), unit);
            }
            else
            {
                ShowErrorDialog(Loc.GetString("Vocab_NoWordsInUnitTitle"), string.Format(Loc.GetString("Vocab_NoWordsInUnitReviewMsg"), unit.Name));
            }
        }
    }

    private async void AddTopicButton_Click(object sender, RoutedEventArgs e)
    {
        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var dialog = new TopicEditorDialog
        {
            XamlRoot = this.XamlRoot,
            RequestedTheme = activeTheme
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
        {
            try
            {
                var created = await ViewModel.CreateTopicAsync(dialog.ResultDto);
                PopulateTopicFilterCombo();
            }
            catch (Exception ex)
            {
                ShowErrorDialog(Loc.GetString("Vocab_CreateTopicFailed"), ex.Message);
            }
        }
    }

    private async void AddWordToUnit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && (fe.Tag as ContextUnitDto ?? fe.DataContext as ContextUnitDto) is ContextUnitDto unit)
        {
            var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            var targetTopicId = unit.Id == Guid.Empty ? (Guid?)null : unit.Id;
            var dialog = new WordEditorDialog(null, ViewModel.AvailableTopics, ViewModel.AvailableExamTags, preselectedTopicId: targetTopicId)
            {
                XamlRoot = this.XamlRoot,
                RequestedTheme = activeTheme
            };

            var soundFeedback = App.Current.Services.GetService<Lexora.Presentation.Services.ISoundFeedbackService>();
            soundFeedback?.PlaySoftTapSound();

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
            {
                await ViewModel.SaveWordAsync(dialog.ResultDto);
                PopulateTopicFilterCombo();

                CardSaveSuccessStoryboard.Begin();
                soundFeedback?.PlayCardFlipSound();
            }
        }
    }
}

