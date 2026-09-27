using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
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
        RegisterShortcuts();

        Unloaded += (s, e) =>
        {
            Loc.LanguageChanged -= OnLanguageChanged;
            _searchDebounceTimer?.Stop();
            var sm = App.Current.Services.GetService<IShortcutManager>();
            sm?.UnregisterPageShortcuts(typeof(VocabularyPage));
        };

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _searchDebounceTimer.Tick += async (s, e) =>
        {
            try
            {
                _searchDebounceTimer.Stop();
                await ViewModel.LoadWordsAsync();
                ViewModel.ApplyUnitFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VocabularyPage] SearchDebounce error: {ex.Message}");
            }
        };

        Loaded += async (s, e) =>
        {
            try
            {
                _isInitializing = true;
                await ViewModel.InitializeAsync();
                PopulateTopicFilterCombo();
                UpdateViewModeButtons();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VocabularyPage] Loaded error: {ex.Message}");
            }
            finally
            {
                _isInitializing = false;
            }
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
        var prevExamIdx = ExamFilterCombo.SelectedIndex >= 0 ? ExamFilterCombo.SelectedIndex : 0;
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

        // Populate Exam combo
        var exams = new List<object> { Loc.GetString("Vocab_AllExams") };
        exams.AddRange(Enum.GetValues<ExamType>().Cast<object>());
        ExamFilterCombo.ItemsSource = exams;
        ExamFilterCombo.SelectedIndex = prevExamIdx < exams.Count ? prevExamIdx : 0;

        // Populate Sort combo
        SortCombo.ItemsSource = new List<string>
        {
            Loc.GetString("Vocab_SortTermAsc"),
            Loc.GetString("Vocab_SortTermDesc"),
            Loc.GetString("Vocab_SortLevelAsc"),
            Loc.GetString("Vocab_SortLevelDesc"),
            Loc.GetString("Vocab_SortDateNewest"),
            Loc.GetString("Vocab_SortDateOldest"),
            Loc.GetString("Vocab_SortPosAsc"),
            Loc.GetString("Vocab_SortMultiPos")
        };
        SortCombo.SelectedIndex = prevSortIdx < 8 ? prevSortIdx : 0;
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

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string query && !string.IsNullOrWhiteSpace(query))
        {
            SearchBox.Text = query;
            ViewModel.SearchTerm = query;
            await ViewModel.LoadWordsAsync();
            ViewModel.ApplyUnitFilter();
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private async void TopicFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] TopicFilterCombo_SelectionChanged error: {ex.Message}");
        }
    }


    private async void LevelFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] LevelFilterCombo_SelectionChanged error: {ex.Message}");
        }
    }

    private async void PosFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] PosFilterCombo_SelectionChanged error: {ex.Message}");
        }
    }

    private async void StatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] StatusFilterCombo_SelectionChanged error: {ex.Message}");
        }
    }

    private async void ExamFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_isInitializing) return;
            if (ExamFilterCombo.SelectedItem is ExamType exam)
            {
                ViewModel.SelectedExamType = exam;
            }
            else
            {
                ViewModel.SelectedExamType = null;
            }
            await ViewModel.LoadWordsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] ExamFilterCombo_SelectionChanged error: {ex.Message}");
        }
    }

    private async void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
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
                case 6: // Part of speech
                    ViewModel.SortBy = "pos";
                    ViewModel.IsAscending = true;
                    break;
                case 7: // Multi-POS first
                    ViewModel.SortBy = "multipos";
                    ViewModel.IsAscending = false;
                    break;
            }
            await ViewModel.LoadWordsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] SortCombo_SelectionChanged error: {ex.Message}");
        }
    }

    private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        TopicFilterCombo.SelectedIndex = 0;
        LevelFilterCombo.SelectedIndex = 0;
        PosFilterCombo.SelectedIndex = 0;
        StatusFilterCombo.SelectedIndex = 0;
        ExamFilterCombo.SelectedIndex = 0;
        SortCombo.SelectedIndex = 0;
        UpdateQuickFilterStyles("All");
        _ = ViewModel.SelectQuickFilterAsync("All");
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
            WordsListView.SelectionMode = ListViewSelectionMode.Multiple;
            TextSelectMode.Text = Loc.GetString("Action_CancelSelection");
            IconSelectMode.Glyph = "\uE711";
            UpdateDeleteSelectedButtonText();
        }
        else
        {
            try
            {
                WordsListView.SelectedItems.Clear();
                WordsListView.SelectionMode = ListViewSelectionMode.Single;
            }
            catch { }
            TextSelectMode.Text = Loc.GetString("Action_SelectMultiple");
            IconSelectMode.Glyph = "\uE762";
            ViewModel.SelectedWordIds.Clear();
            UpdateDeleteSelectedButtonText();
        }
    }

    private DispatcherTimer? _notificationTimer;

    private void ShowNotification(InfoBarSeverity severity, string title, string message, int autoCloseMs = 4500)
    {
        if (App.Current?.Services?.GetService<IToastNotificationService>() is { } toastService)
        {
            var type = severity switch
            {
                InfoBarSeverity.Success => ToastType.Success,
                InfoBarSeverity.Error => ToastType.Error,
                InfoBarSeverity.Warning => ToastType.Warning,
                _ => ToastType.Information
            };
            toastService.Show(type, message, title, autoCloseMs);
            return;
        }

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

    private void WordsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ViewModel.IsSelectionMode)
        {
            return;
        }

        ViewModel.SelectedWordIds.Clear();
        foreach (var item in WordsListView.SelectedItems)
        {
            if (item is WordDto word)
            {
                ViewModel.SelectedWordIds.Add(word.Id);
            }
        }
        UpdateDeleteSelectedButtonText();
    }

    private void SelectAllWords_Click(object sender, RoutedEventArgs e)
    {
        WordsListView.SelectAll();
    }

    private void DeselectAllWords_Click(object sender, RoutedEventArgs e)
    {
        WordsListView.SelectedItems.Clear();
    }

    private void UpdateDeleteSelectedButtonText()
    {
        TextDeleteSelected.Text = string.Format(Loc.GetString("Action_DeleteSelected"), ViewModel.SelectedWordIds.Count);
        BtnDeleteSelectedWords.IsEnabled = ViewModel.SelectedWordIds.Count > 0;
    }

    private async void DeleteSelectedWords_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWordIds.Count == 0) return;

        var count = ViewModel.SelectedWordIds.Count;
        var theme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Vocab_ConfirmBulkDeleteTitle"),
            Content = string.Format(Loc.GetString("Vocab_ConfirmBulkDeleteMsg"), count),
            PrimaryButtonText = Loc.GetString("Action_Delete"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            RequestedTheme = theme
        };

        var result = await confirmDialog.ShowAsyncSafe();
        if (result == ContentDialogResult.Primary)
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();

            // 1. Snapshot the IDs to delete BEFORE exiting selection mode or clearing UI!
            var idsToDelete = ViewModel.SelectedWordIds.ToList();
            if (idsToDelete.Count == 0) return;

            // 2. Disable buttons and exit selection mode cleanly on UI thread
            BtnDeleteSelectedWords.IsEnabled = false;
            BtnSelectMode.IsEnabled = false;

            // Turn off IsSelectionMode first so WordsListView_SelectionChanged will not clear SelectedWordIds or fire side effects
            ViewModel.IsSelectionMode = false;
            try
            {
                WordsListView.SelectionMode = ListViewSelectionMode.Single;
                WordsListView.SelectedItems.Clear();
            }
            catch { }

            int deletedCount = 0;
            Exception? deleteError = null;
            try
            {
                deletedCount = await ViewModel.DeleteSelectedWordsAsync(idsToDelete);
            }
            catch (Exception ex)
            {
                deleteError = ex;
            }

            // 3. Ensure UI updates on UI thread
            await DispatcherQueue.EnsureOnUIThreadAsync();
            BtnSelectMode.IsEnabled = true;
            UpdateSelectionModeUI();

            if (deleteError != null)
            {
                ShowNotification(InfoBarSeverity.Error, Loc.GetString("Action_Delete"), deleteError.Message);
            }
            else
            {
                ShowNotification(InfoBarSeverity.Success, Loc.GetString("Action_Delete"), string.Format(Loc.GetString("Vocab_BulkDeleteSuccess"), deletedCount));
            }
        }
    }

    private async void QuickFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string filter)
        {
            try
            {
                await ViewModel.SelectQuickFilterAsync(filter);
                UpdateQuickFilterStyles(filter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VocabularyPage] QuickFilter error: {ex.Message}");
            }
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
        try
        {
            var soundFeedback = App.Current.Services.GetService<Lexora.Presentation.Services.ISoundFeedbackService>();
            soundFeedback?.PlaySoftTapSound();

            var xamlRoot = (sender as FrameworkElement)?.XamlRoot ?? this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot;
            var themeService = App.Current.Services.GetService<IThemeService>();
            var activeTheme = themeService?.CurrentTheme ?? ElementTheme.Default;

            var dialog = new WordEditorDialog(null, ViewModel.AvailableTopics, ViewModel.AvailableExamTags)
            {
                XamlRoot = xamlRoot,
                RequestedTheme = activeTheme
            };

            var result = await dialog.ShowAsyncSafe(xamlRoot);
            if ((result == ContentDialogResult.Primary || dialog.ResultDto != null) && dialog.ResultDto != null)
            {
                await ViewModel.SaveWordAsync(dialog.ResultDto);
                PopulateTopicFilterCombo();

                CardSaveSuccessStoryboard.Begin();
                soundFeedback?.PlayCardFlipSound();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] AddWord error: {ex.Message}");
            await ShowErrorDialogAsync(Loc.GetString("Vocab_AddWord"), ex.Message);
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

        try
        {
            // Smooth connected pre-launch card spring
            CardMorphStoryboard.Begin();
            var soundFeedback = App.Current.Services.GetService<Lexora.Presentation.Services.ISoundFeedbackService>();
            soundFeedback?.PlaySoftTapSound();

            var xamlRoot = (sender as FrameworkElement)?.XamlRoot ?? this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot;
            var themeService = App.Current.Services.GetService<IThemeService>();
            var activeTheme = themeService?.CurrentTheme ?? ElementTheme.Default;

            var dialog = new WordEditorDialog(ViewModel.SelectedWord, ViewModel.AvailableTopics, ViewModel.AvailableExamTags)
            {
                XamlRoot = xamlRoot,
                RequestedTheme = activeTheme
            };

            var result = await dialog.ShowAsyncSafe(xamlRoot);
            if ((result == ContentDialogResult.Primary || dialog.ResultDto != null) && dialog.ResultDto != null)
            {
                await ViewModel.SaveWordAsync(dialog.ResultDto);
                PopulateTopicFilterCombo();

                // Smooth return bounce + pleasant completion feedback
                CardSaveSuccessStoryboard.Begin();
                soundFeedback?.PlayCardFlipSound();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] EditWord error: {ex.Message}");
            await ShowErrorDialogAsync(Loc.GetString("Vocab_EditWord"), ex.Message);
        }
    }

    private async void DeleteWordButton_Click(object sender, RoutedEventArgs e)
    {
        var wordToDelete = ViewModel.SelectedWord;
        if (wordToDelete == null) return;
        var wordId = wordToDelete.Id;
        var wordTerm = wordToDelete.Term;

        try
        {
            var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            var confirmDialog = new ContentDialog
            {
                Title = Loc.GetString("Vocab_DeleteTitle"),
                Content = string.Format(Loc.GetString("Vocab_DeleteConfirm"), wordTerm),
                PrimaryButtonText = Loc.GetString("Action_Delete"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = activeTheme
            };

            var result = await confirmDialog.ShowAsyncSafe();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await DispatcherQueue.EnsureOnUIThreadAsync();
                    await ViewModel.DeleteWordAsync(wordId);
                    await DispatcherQueue.EnsureOnUIThreadAsync();
                    ShowNotification(InfoBarSeverity.Success, Loc.GetString("Action_Delete"), string.Format(Loc.GetString("Vocab_Status_WordDeleted"), wordTerm));
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
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] DeleteWord error: {ex.Message}");
        }
    }

    private async void PlayUkAudio_Click(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (BtnPlayUk != null) BtnPlayUk.IsEnabled = false;
            if (IconPlayUk != null) IconPlayUk.Glyph = "\uE995";
        });
        try
        {
            await ViewModel.PlayPronunciationAsync("uk");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] PlayUkAudio error: {ex.Message}");
        }
        finally
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (IconPlayUk != null) IconPlayUk.Glyph = "\uE768";
                if (BtnPlayUk != null) BtnPlayUk.IsEnabled = true;
            });
        }
    }

    private async void PlayUsAudio_Click(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (BtnPlayUs != null) BtnPlayUs.IsEnabled = false;
            if (IconPlayUs != null) IconPlayUs.Glyph = "\uE995";
        });
        try
        {
            await ViewModel.PlayPronunciationAsync("us");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] PlayUsAudio error: {ex.Message}");
        }
        finally
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (IconPlayUs != null) IconPlayUs.Glyph = "\uE768";
                if (BtnPlayUs != null) BtnPlayUs.IsEnabled = true;
            });
        }
    }

    private async void QuickListenButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.DataContext is WordDto w)
            {
                var audioService = App.Current.Services.GetRequiredService<IAudioPlayerService>();
                var prefsService = App.Current.Services.GetRequiredService<IUserPreferencesService>();
                var accent = prefsService.GetPreferences().PreferredAccent ?? "uk";
                await audioService.PlayPronunciationAsync(w.Term, accent);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] QuickListen error: {ex.Message}");
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
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ErrorMarkStatus"), ex.Message);
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
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ErrorMarkStatus"), ex.Message);
        }
        finally
        {
            if (sender is Button btn2) btn2.IsEnabled = true;
        }
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await PerformExportAsync("json", "JSON Vocabulary File", ".json", (p) => ViewModel.ExportToJsonAsync(progress: p));
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ExportFailed"), ex.Message);
        }
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await PerformExportAsync("csv", "CSV Vocabulary File", ".csv", (p) => ViewModel.ExportToCsvAsync(progress: p));
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ExportFailed"), ex.Message);
        }
    }

    private async void ExportTxt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await PerformExportAsync("txt", "TXT Vocabulary File", ".txt", (p) => ViewModel.ExportToTxtAsync(progress: p));
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ExportFailed"), ex.Message);
        }
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
        _ = progressDialog.ShowAsyncSafe();

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
            progressDialog.HideSafe();
            await Task.Yield();

            var defaultName = $"lexora_export_{DateTime.Now:yyyyMMdd_HHmmss}";
            await ExportFileAsync(content, defaultName, extension, fileTypeDesc);
        }
        catch (Exception ex)
        {
            progressDialog.HideSafe();
            await Task.Yield();
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ExportFailed"), ex.Message);
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
            await DispatcherQueue.EnsureOnUIThreadAsync();
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
            ShowNotification(InfoBarSeverity.Success, Loc.GetString("Vocab_ExportCompleteTitle"), string.Format(Loc.GetString("Vocab_ExportCompleteMsg"), ViewModel.Words.Count, Path.GetFileName(savedPath)));

            var exportCompleteTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            var dialog = new ContentDialog
            {
                Title = Loc.GetString("Vocab_ExportCompleteTitle"),
                Content = string.Format(Loc.GetString("Vocab_ExportCompleteMsg"), ViewModel.Words.Count, savedPath),
                PrimaryButtonText = Loc.GetString("Export_OpenFile"),
                SecondaryButtonText = Loc.GetString("Export_OpenFolder"),
                CloseButtonText = Loc.GetString("Action_Close"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                RequestedTheme = exportCompleteTheme
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

    private void ExportImportFlyout_Opening(object sender, object e)
    {
        var count = WordsListView?.SelectedItems?.Count ?? 0;
        if (ExportSelectedMenuItem != null)
        {
            ExportSelectedMenuItem.IsEnabled = count > 0;
            ExportSelectedMenuItem.Text = string.Format(Loc.GetString("Vocab_ExportSelected"), count);
        }
    }

    private async void ExportSelected_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedWords = WordsListView?.SelectedItems?.OfType<WordDto>().ToList();
            if (selectedWords == null || selectedWords.Count == 0) return;

            var selectedIds = selectedWords.Select(w => w.Id).ToList();
            await PerformExportAsync(
                "csv",
                "CSV Vocabulary File",
                ".csv",
                (progress) => ViewModel.ExportToCsvAsync(selectedIds, progress));
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ExportFailed"), ex.Message);
        }
    }

    private async void SmartImport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Ensure we are on the UI thread before constructing ContentDialog.
            // Task.Yield() is unsafe here — it can resume on a thread-pool thread
            // if no SynchronizationContext is installed, causing RPC_E_WRONG_THREAD.
            await DispatcherQueue.EnsureOnUIThreadAsync();

            var xamlRoot = App.Current.MainWindow?.Content?.XamlRoot ?? this.XamlRoot;

            var dialog = new SmartImportDialog(format => ViewModel.GetSampleTemplate(format))
            {
                XamlRoot = xamlRoot
            };

            var res = await dialog.ShowAsyncSafe(xamlRoot);
            if (dialog.WasCommitted)
            {
                await ViewModel.LoadWordsAsync();
                PopulateTopicFilterCombo();
                ShowNotification(InfoBarSeverity.Success, Loc.GetString("Progress_CompletedTitle"), Loc.GetString("SmartImport_CommitSuccess"));
            }
            else if (res == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.ContentToImport))
            {
                await ExecuteImportOperationAsync(dialog.ContentToImport, dialog.FileNameHint, dialog.ConflictResolution);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] SmartImport error: {ex}");
            // Log full details to crash.log so the exception can be diagnosed.
            Services.SafeDialogExtensions.LogSmartImportError("VocabularyPage", ex);
            ShowNotification(InfoBarSeverity.Error, Loc.GetString("Vocab_ImportFailed"), ex.Message);
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
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ImportFailed"), ex.Message);
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
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ImportFailed"), ex.Message);
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
            await ShowErrorDialogAsync(Loc.GetString("Vocab_ImportFailed"), ex.Message);
        }
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
        string? fileName = null;

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
            await DispatcherQueue.EnsureOnUIThreadAsync();
            if (file != null)
            {
                var basicProps = await file.GetBasicPropertiesAsync();
                if (basicProps.Size > 20 * 1024 * 1024)
                {
                    await ShowErrorDialogAsync(Loc.GetString("Vocab_ImportFailed"), Loc.GetString("SmartImport_FileTooLarge"));
                    return;
                }

                fileName = file.Name;
                contentToImport = await Windows.Storage.FileIO.ReadTextAsync(file);
            }
            else
            {
                return; // User cancelled file picker cleanly
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

        var importTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var progressDialog = new OperationProgressDialog
        {
            Title = Loc.GetString("Progress_ImportingTitle"),
            XamlRoot = this.XamlRoot,
            RequestedTheme = importTheme
        };
        progressDialog.SetDeterminate(0, Loc.GetString("Progress_ReadingFile"), Loc.GetString("Progress_PleaseWait"));
        _ = progressDialog.ShowAsyncSafe();

        var progress = new Progress<ProgressReport>(report =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                progressDialog.SetDeterminate(report.Percent, report.Message, report.StepDetail);
            });
        });

        try
        {
            var importResult = await ViewModel.ImportSmartAsync(contentToImport, fileNameHint, conflictResolution, progress);
            PopulateTopicFilterCombo();

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
                Loc.GetString("Vocab_ImportFailed"),
                ex.Message,
                Loc.GetString("Action_Close"));

            ShowNotification(InfoBarSeverity.Error, Loc.GetString("Vocab_ImportFailed"), ex.Message);
        }
    }



    private void RegisterShortcuts()
    {
        var sm = App.Current.Services.GetService<IShortcutManager>();
        if (sm == null) return;

        sm.RegisterPageShortcuts(typeof(VocabularyPage), new[]
        {
            new ShortcutDefinition
            {
                Id = "Vocab.AddWord",
                Gesture = new ShortcutGesture(ShortcutKey.N, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(VocabularyPage),
                DescriptionKey = "Vocab_TooltipAddWord",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = true,
                AllowWhenDialogActive = false,
                ExecuteAsync = () =>
                {
                    AddWordButton_Click(this, new RoutedEventArgs());
                    return Task.CompletedTask;
                }
            },
            new ShortcutDefinition
            {
                Id = "Vocab.EditWord",
                Gesture = new ShortcutGesture(ShortcutKey.E, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(VocabularyPage),
                DescriptionKey = "Vocab_TooltipEditWord",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.SelectedWord != null && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = () =>
                {
                    EditWordButton_Click(this, new RoutedEventArgs());
                    return Task.CompletedTask;
                }
            },
            new ShortcutDefinition
            {
                Id = "Vocab.DeleteWord",
                Gesture = new ShortcutGesture(ShortcutKey.Delete, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.D, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(VocabularyPage),
                DescriptionKey = "Vocab_TooltipDeleteWord",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.SelectedWord != null && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = () =>
                {
                    DeleteWordButton_Click(this, new RoutedEventArgs());
                    return Task.CompletedTask;
                }
            },
            new ShortcutDefinition
            {
                Id = "Vocab.Refresh",
                Gesture = new ShortcutGesture(ShortcutKey.F5, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.R, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(VocabularyPage),
                DescriptionKey = "Vocab_SearchPlaceholder",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => !ViewModel.IsLoading && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.LoadWordsAsync();
                }
            },
            new ShortcutDefinition
            {
                Id = "Vocab.SmartImport",
                Gesture = new ShortcutGesture(ShortcutKey.I, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(VocabularyPage),
                DescriptionKey = "Vocab_SmartImport",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = () =>
                {
                    SmartImport_Click(this, new RoutedEventArgs());
                    return Task.CompletedTask;
                }
            }
        });
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

    private async void TestUnit_Click(object sender, RoutedEventArgs e)
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
                await ShowErrorDialogAsync(Loc.GetString("Vocab_NoWordsInUnitTitle"), string.Format(Loc.GetString("Vocab_NoWordsInUnitTestMsg"), unit.Name));
            }
        }
    }

    private async void ReviewUnit_Click(object sender, RoutedEventArgs e)
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
                await ShowErrorDialogAsync(Loc.GetString("Vocab_NoWordsInUnitTitle"), string.Format(Loc.GetString("Vocab_NoWordsInUnitReviewMsg"), unit.Name));
            }
        }
    }

    private async void AddTopicButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            var dialog = new TopicEditorDialog
            {
                XamlRoot = this.XamlRoot,
                RequestedTheme = activeTheme
            };

            var result = await dialog.ShowAsyncSafe();
            if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
            {
                try
                {
                    var created = await ViewModel.CreateTopicAsync(dialog.ResultDto);
                    PopulateTopicFilterCombo();
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync(Loc.GetString("Vocab_CreateTopicFailed"), ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] AddTopic error: {ex.Message}");
        }
    }

    private async void AddWordToUnit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && (fe.Tag as ContextUnitDto ?? fe.DataContext as ContextUnitDto) is ContextUnitDto unit)
            {
                var xamlRoot = fe.XamlRoot ?? this.XamlRoot ?? App.Current.MainWindow?.Content?.XamlRoot;
                var themeService = App.Current.Services.GetService<IThemeService>();
                var activeTheme = themeService?.CurrentTheme ?? ElementTheme.Default;
                var targetTopicId = unit.Id == Guid.Empty ? (Guid?)null : unit.Id;

                var dialog = new WordEditorDialog(null, ViewModel.AvailableTopics, ViewModel.AvailableExamTags, preselectedTopicId: targetTopicId)
                {
                    XamlRoot = xamlRoot,
                    RequestedTheme = activeTheme
                };

                var soundFeedback = App.Current.Services.GetService<Lexora.Presentation.Services.ISoundFeedbackService>();
                soundFeedback?.PlaySoftTapSound();

                var result = await dialog.ShowAsyncSafe(xamlRoot);
                if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
                {
                    await ViewModel.SaveWordAsync(dialog.ResultDto);
                    PopulateTopicFilterCombo();

                    CardSaveSuccessStoryboard.Begin();
                    soundFeedback?.PlayCardFlipSound();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] AddWordToUnit error: {ex.Message}");
            await ShowErrorDialogAsync(Loc.GetString("Vocab_AddWord"), ex.Message);
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
            System.Diagnostics.Debug.WriteLine($"[VocabularyPage] ShowErrorDialog error: {ex.Message}");
        }
    }
}

