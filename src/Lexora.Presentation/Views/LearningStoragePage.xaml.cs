using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class LearningStoragePage : Page, ILocalizableView
{
    public LearningStorageViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }

    private readonly INavigationService _navigationService;
    private readonly IVocabularyService _vocabService;
    private readonly IGrammarService _grammarService;
    private readonly ILearningStorageService _storageService;
    private readonly IToastNotificationService? _toastService;
    private CancellationTokenSource? _searchCts;

    public LearningStoragePage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Current.Services.GetRequiredService<LearningStorageViewModel>();
        Loc = App.Current.Services.GetRequiredService<Services.ILocalizationService>();
        _navigationService = App.Current.Services.GetRequiredService<INavigationService>();
        _vocabService = App.Current.Services.GetRequiredService<IVocabularyService>();
        _grammarService = App.Current.Services.GetRequiredService<IGrammarService>();
        _storageService = App.Current.Services.GetRequiredService<ILearningStorageService>();
        _toastService = App.Current.Services.GetService<IToastNotificationService>();
        ViewModel.NoPracticeQuestionsAvailable += () =>
        {
            _toastService?.ShowWarning(Loc.GetString("Storage_Practice_NoQuestions"));
        };
        ViewModel.PracticeFailed += (msg) =>
        {
            _toastService?.ShowWarning(msg);
        };
        InitializeComponent();
    }

    public void OnLanguageChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.UpdateLanguageState();
            UpdateSegmentedButtons();
            UpdateSelectionModeUI();
            Bindings.Update();
        });
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        OnLanguageChanged();
        try
        {
            await ViewModel.InitializeAsync();
            UpdateSegmentedButtons();
            UpdateSelectionModeUI();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] OnNavigatedTo error: {ex.Message}");
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateSegmentedButtons();
        UpdateSelectionModeUI();
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        UpdateSegmentedButtons();
        UpdateSelectionModeUI();
    }

    private async void TabVocab_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SwitchTabAsync(0);
        UpdateSegmentedButtons();
        UpdateSelectionModeUI();
    }

    private async void TabGrammar_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SwitchTabAsync(1);
        UpdateSegmentedButtons();
        UpdateSelectionModeUI();
    }

    private void UpdateSegmentedButtons()
    {
        var cardBg = ThemeResourceHelper.GetThemeBrush(this, "CardBackgroundFillColorDefaultBrush");
        var cardStroke = ThemeResourceHelper.GetThemeBrush(this, "CardStrokeColorDefaultBrush");
        var accentText = ThemeResourceHelper.GetThemeBrush(this, "AccentTextFillColorPrimaryBrush");
        var secondaryText = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorSecondaryBrush");
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        void ApplySegment(Button? btn, bool isActive)
        {
            if (btn == null) return;
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

        ApplySegment(BtnVocabTab, ViewModel.IsVocabTabSelected);
        ApplySegment(BtnGrammarTab, ViewModel.IsGrammarTabSelected);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, token);
                if (token.IsCancellationRequested) return;

                DispatcherQueue.TryEnqueue(async () =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        await ViewModel.SearchAsync(StorageSearchBox.Text);
                    }
                });
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private async void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null) return;
        if (StatusFilterComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            await ViewModel.ApplyFilterAsync(tag);
        }
    }

    private async void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PreviousPageAsync();
    }

    private async void NextPage_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NextPageAsync();
    }

    private async void CardAudioBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LearningStorageItemDto item)
        {
            var term = item.Word?.Term ?? item.Title;
            if (!string.IsNullOrWhiteSpace(term))
            {
                await ViewModel.PlayAudioAsync(term);
            }
        }
    }

    private async void CardMasteredBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LearningStorageItemDto item)
        {
            try
            {
                await ViewModel.MarkAsMasteredAsync(item);
                _toastService?.ShowSuccess(Loc.GetString("Storage_Toast_Mastered"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] CardMastered error: {ex.Message}");
                _toastService?.ShowError(ex.Message, Loc.GetString("Common_Error"));
            }
        }
    }

    private void CardMenuFlyout_Opening(object? sender, object e)
    {
        if (sender is MenuFlyout flyout)
        {
            var item = (flyout.Target as FrameworkElement)?.Tag as LearningStorageItemDto;
            if (flyout.Items.Count >= 5)
            {
                if (flyout.Items[0] is MenuFlyoutItem item0) { item0.Text = Loc.GetString("Storage_FilterWeak"); item0.Tag = item; }
                if (flyout.Items[1] is MenuFlyoutItem item1) { item1.Text = Loc.GetString("Storage_FilterLearning"); item1.Tag = item; }
                if (flyout.Items[2] is MenuFlyoutItem item2) { item2.Text = Loc.GetString("Storage_BtnEditNote"); item2.Tag = item; }
                if (flyout.Items[4] is MenuFlyoutItem item4) { item4.Text = Loc.GetString("Storage_BtnRemove"); item4.Tag = item; }
            }
        }
    }

    private ElementTheme GetActiveTheme()
    {
        var themeService = App.Current?.Services?.GetService<IThemeService>();
        if (themeService != null && themeService.CurrentTheme != ElementTheme.Default)
        {
            return themeService.CurrentTheme;
        }

        if ((this.XamlRoot?.Content as FrameworkElement)?.ActualTheme is { } actual && actual != ElementTheme.Default)
        {
            return actual;
        }

        return ElementTheme.Default;
    }

    private async void MenuMarkWeak_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is LearningStorageItemDto item)
        {
            await ViewModel.UpdateStatusAsync((item.Id, StorageLearningStatus.Weak));
            _toastService?.ShowInfo(Loc.GetString("Storage_Toast_StatusUpdated"));
        }
    }

    private async void MenuMarkLearning_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is LearningStorageItemDto item)
        {
            await ViewModel.UpdateStatusAsync((item.Id, StorageLearningStatus.Learning));
            _toastService?.ShowInfo(Loc.GetString("Storage_Toast_StatusUpdated"));
        }
    }

    private async void MenuEditNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is LearningStorageItemDto item)
        {
            var noteBox = new TextBox
            {
                Text = item.Note ?? string.Empty,
                PlaceholderText = Loc.GetString("Storage_Dialog_AddNotePlaceholder"),
                AcceptsReturn = true,
                Height = 100,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var activeTheme = GetActiveTheme();

            var dialog = new ContentDialog
            {
                Title = Loc.GetString("Storage_Dialog_EditNoteTitle"),
                Content = noteBox,
                PrimaryButtonText = Loc.GetString("Action_Save"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                RequestedTheme = activeTheme
            };

            string? savedNoteText = null;
            dialog.PrimaryButtonClick += (s, args) =>
            {
                savedNoteText = noteBox.Text;
            };

            var res = await dialog.ShowAsyncSafe(this.XamlRoot);
            await DispatcherQueue.EnsureOnUIThreadAsync();

            if (res == ContentDialogResult.Primary && savedNoteText != null)
            {
                try
                {
                    await ViewModel.SaveNoteAsync((item.Id, savedNoteText));
                    _toastService?.ShowSuccess(Loc.GetString("Storage_Toast_StatusUpdated"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] SaveNote error: {ex.Message}");
                    _toastService?.ShowError(ex.Message, Loc.GetString("Common_Error"));
                }
            }
        }
    }

    private async void MenuRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is LearningStorageItemDto item)
        {
            var activeTheme = GetActiveTheme();

            var dialog = new ContentDialog
            {
                Title = Loc.GetString("Storage_Dialog_ConfirmRemoveTitle"),
                Content = Loc.GetString("Storage_Dialog_ConfirmRemoveMessage"),
                PrimaryButtonText = Loc.GetString("Action_Delete"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = activeTheme
            };

            var res = await dialog.ShowAsyncSafe(this.XamlRoot);
            if (res == ContentDialogResult.Primary)
            {
                try
                {
                    await ViewModel.RemoveItemAsync(item);
                    _toastService?.ShowSuccess(Loc.GetString("Storage_Toast_Removed"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] RemoveItem error: {ex.Message}");
                    _toastService?.ShowError(ex.Message, Loc.GetString("Common_Error"));
                }
            }
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
            TextSelectMode.Text = Loc.GetString("Action_CancelSelection");
            IconSelectMode.Glyph = "\uE711";
            StorageListView.SelectionMode = ListViewSelectionMode.Multiple;
            UpdateDeleteSelectedButtonText();
        }
        else
        {
            BtnSelectMode.Style = (Style)App.Current.Resources["DefaultButtonStyle"];
            TextSelectMode.Text = Loc.GetString("Action_SelectMultiple");
            IconSelectMode.Glyph = "\uE762";
            try
            {
                StorageListView.SelectedItems.Clear();
                StorageListView.SelectionMode = ListViewSelectionMode.None;
            }
            catch { }
            ViewModel.ClearSelection();
            UpdateDeleteSelectedButtonText();
        }
    }

    private void StorageListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ViewModel.IsSelectionMode)
        {
            try
            {
                StorageListView.SelectedItems.Clear();
            }
            catch { }
            return;
        }

        ViewModel.SelectedItemIds.Clear();
        foreach (var item in StorageListView.SelectedItems)
        {
            if (item is LearningStorageItemDto storageItem)
            {
                ViewModel.SelectedItemIds.Add(storageItem.Id);
            }
        }
        ViewModel.NotifySelectionChanged();
        UpdateDeleteSelectedButtonText();
    }

    private void SelectAllItems_Click(object sender, RoutedEventArgs e)
    {
        StorageListView.SelectAll();
    }

    private void DeselectAllItems_Click(object sender, RoutedEventArgs e)
    {
        StorageListView.SelectedItems.Clear();
    }

    private void UpdateDeleteSelectedButtonText()
    {
        TextDeleteSelected.Text = string.Format(Loc.GetString("Action_DeleteSelected"), ViewModel.SelectedItemIds.Count);
        BtnDeleteSelectedItems.IsEnabled = ViewModel.SelectedItemIds.Count > 0;
    }

    private async void DeleteSelectedItems_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedItemIds.Count == 0) return;
        if (ViewModel.IsLoading || ViewModel.IsOperating)
        {
            _toastService?.ShowWarning(Loc.GetString("Storage_Toast_OperationInProgress"), Loc.GetString("Nav_LearningStorage"));
            return;
        }

        var count = ViewModel.SelectedItemIds.Count;
        var activeTheme = GetActiveTheme();
        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Storage_ConfirmBulkDeleteTitle"),
            Content = string.Format(Loc.GetString("Storage_ConfirmBulkDeleteMsg"), count),
            PrimaryButtonText = Loc.GetString("Action_Delete"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            RequestedTheme = activeTheme
        };

        var result = await confirmDialog.ShowAsyncSafe();
        if (result != ContentDialogResult.Primary) return;

        await DispatcherQueue.EnsureOnUIThreadAsync();

        // 1. Snapshot the IDs to delete BEFORE resetting selection mode or modifying UI!
        var idsToDelete = ViewModel.SelectedItemIds.ToList();
        if (idsToDelete.Count == 0) return;

        // 2. Disable buttons and exit selection mode cleanly on UI thread FIRST!
        BtnDeleteSelectedItems.IsEnabled = false;
        BtnSelectMode.IsEnabled = false;

        // Turn off IsSelectionMode first so StorageListView_SelectionChanged will not clear SelectedItemIds or corrupt SelectionModel
        ViewModel.IsSelectionMode = false;
        try
        {
            StorageListView.SelectionMode = ListViewSelectionMode.None;
            StorageListView.SelectedItems.Clear();
        }
        catch { }

        // 3. Perform deletion with graceful error handling and without secondary dialog conflicts
        int deletedCount = 0;
        Exception? deleteError = null;

        try
        {
            deletedCount = await ViewModel.DeleteSelectedItemsAsync(idsToDelete);
        }
        catch (Exception ex)
        {
            deleteError = ex;
        }

        // 4. Re-enable selection mode button and refresh UI
        await DispatcherQueue.EnsureOnUIThreadAsync();
        BtnSelectMode.IsEnabled = true;
        UpdateSelectionModeUI();

        // 5. Toast notification
        if (deleteError != null)
        {
            _toastService?.ShowError(deleteError.Message, Loc.GetString("Common_Error"));
        }
        else
        {
            _toastService?.ShowSuccess(string.Format(Loc.GetString("Storage_BulkDeleteSuccessMsg"), deletedCount), Loc.GetString("Action_Delete"));
        }
    }

    private async void OpenAddDialog_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsLoading || ViewModel.IsOperating)
        {
            _toastService?.ShowWarning(Loc.GetString("Storage_Toast_OperationInProgress"), Loc.GetString("Nav_LearningStorage"));
            return;
        }

        try
        {
            if (ViewModel.IsVocabTabSelected)
            {
                await ShowAddWordDialogAsync();
            }
            else
            {
                await ShowAddGrammarDialogAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] OpenAddDialog error: {ex.Message}");
            _toastService?.ShowError(ex.Message, Loc.GetString("Common_Error"));
        }
    }

    private async Task ShowAddWordDialogAsync()
    {
        var storedWordIds = (await _storageService.GetStoredWordIdsAsync()).ToHashSet();

        var searchBox = new TextBox
        {
            PlaceholderText = Loc.GetString("Storage_Dialog_AddVocabPlaceholder"),
            Margin = new Thickness(0, 0, 0, 8),
            CornerRadius = new CornerRadius(8)
        };

        var wordTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Grid Padding=""6,6"" ColumnDefinitions=""Auto,Auto,*,Auto"" ColumnSpacing=""10"">
                    <CheckBox IsChecked=""{Binding IsSelected, Mode=TwoWay}"" VerticalAlignment=""Center"" Margin=""0,0,2,0"" MinWidth=""28"" />
                    <Border Grid.Column=""1"" Width=""30"" Height=""30"" CornerRadius=""6"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" VerticalAlignment=""Center"">
                        <FontIcon Glyph=""&#xE82D;"" FontSize=""13"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" />
                    </Border>
                    <StackPanel Grid.Column=""2"" VerticalAlignment=""Center"" Spacing=""2"">
                        <TextBlock Text=""{Binding Title}"" FontWeight=""SemiBold"" FontSize=""14"" Foreground=""{ThemeResource TextFillColorPrimaryBrush}"" />
                        <TextBlock Text=""{Binding Subtitle}"" Foreground=""{ThemeResource TextFillColorSecondaryBrush}"" FontSize=""12"" TextTrimming=""CharacterEllipsis"" />
                    </StackPanel>
                    <Border Grid.Column=""3"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" CornerRadius=""4"" Padding=""6,2"" VerticalAlignment=""Center"">
                        <TextBlock Text=""{Binding Badge}"" FontSize=""10"" Foreground=""{ThemeResource TextFillColorTertiaryBrush}"" />
                    </Border>
                </Grid>
            </DataTemplate>");

        var listView = new ListView
        {
            MaxHeight = 300,
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = true,
            ItemTemplate = wordTemplate
        };

        var selectedCandidateIds = new HashSet<Guid>();

        var btnSelectAll = new Button
        {
            Content = Loc.GetString("Storage_Dialog_SelectAll"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4, 10, 4),
            FontSize = 12
        };

        var btnDeselectAll = new Button
        {
            Content = Loc.GetString("Storage_Dialog_DeselectAll"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4, 10, 4),
            FontSize = 12
        };

        var txtSelectedCount = new TextBlock
        {
            Text = string.Format(Loc.GetString("Storage_Dialog_SelectedCount"), 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var actionsBar = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 8,
            Margin = new Thickness(0, 0, 0, 8)
        };
        actionsBar.Children.Add(btnSelectAll);
        Grid.SetColumn(btnSelectAll, 0);
        actionsBar.Children.Add(btnDeselectAll);
        Grid.SetColumn(btnDeselectAll, 1);
        actionsBar.Children.Add(txtSelectedCount);
        Grid.SetColumn(txtSelectedCount, 2);

        var txtEmpty = new TextBlock
        {
            Text = Loc.GetString("Storage_Dialog_AllItemsAlreadyAdded"),
            FontSize = 12,
            FontStyle = Windows.UI.Text.FontStyle.Italic,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["TextFillColorSecondaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 12),
            Visibility = Visibility.Collapsed
        };

        var noteBox = new TextBox
        {
            PlaceholderText = Loc.GetString("Storage_Dialog_AddNotePlaceholder"),
            Margin = new Thickness(0, 10, 0, 0),
            CornerRadius = new CornerRadius(8)
        };

        var panel = new StackPanel { Spacing = 6, MinWidth = 460 };
        panel.Children.Add(searchBox);
        panel.Children.Add(actionsBar);
        panel.Children.Add(txtEmpty);
        panel.Children.Add(listView);
        panel.Children.Add(noteBox);

        var activeTheme = GetActiveTheme();

        var dialog = new ContentDialog
        {
            Title = Loc.GetString("Storage_Dialog_AddMultipleVocabTitle"),
            Content = panel,
            PrimaryButtonText = Loc.GetString("Action_Add"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            XamlRoot = this.XamlRoot,
            RequestedTheme = activeTheme
        };

        void UpdateSelectionUI()
        {
            txtSelectedCount.Text = string.Format(Loc.GetString("Storage_Dialog_SelectedCount"), selectedCandidateIds.Count);
            dialog.IsPrimaryButtonEnabled = selectedCandidateIds.Count > 0;
            dialog.PrimaryButtonText = selectedCandidateIds.Count > 0
                ? string.Format(Loc.GetString("Storage_Dialog_AddMultipleBtn"), selectedCandidateIds.Count)
                : Loc.GetString("Action_Add");
        }

        void BindCandidates(IEnumerable<WordDto> words)
        {
            var list = words
                .Where(w => !storedWordIds.Contains(w.Id))
                .Select(w =>
                {
                    var item = new StorageCandidateItem
                    {
                        Id = w.Id,
                        Title = w.Term,
                        Subtitle = w.Meanings.FirstOrDefault()?.DefinitionVi ?? w.PhoneticUs ?? string.Empty,
                        Badge = w.Level.ToString(),
                        IsSelected = selectedCandidateIds.Contains(w.Id)
                    };
                    item.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(StorageCandidateItem.IsSelected))
                        {
                            if (item.IsSelected) selectedCandidateIds.Add(item.Id);
                            else selectedCandidateIds.Remove(item.Id);
                            UpdateSelectionUI();
                        }
                    };
                    return item;
                }).ToList();

            listView.ItemsSource = list;
            txtEmpty.Text = string.IsNullOrWhiteSpace(searchBox.Text)
                ? Loc.GetString("Storage_Dialog_NoItemsAvailable")
                : Loc.GetString("Storage_Dialog_AllItemsAlreadyAdded");
            txtEmpty.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        listView.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is StorageCandidateItem candidate)
            {
                candidate.IsSelected = !candidate.IsSelected;
            }
        };

        btnSelectAll.Click += (s, e) =>
        {
            if (listView.ItemsSource is List<StorageCandidateItem> items)
            {
                foreach (var item in items)
                {
                    item.IsSelected = true;
                    selectedCandidateIds.Add(item.Id);
                }
                UpdateSelectionUI();
            }
        };

        btnDeselectAll.Click += (s, e) =>
        {
            if (listView.ItemsSource is List<StorageCandidateItem> items)
            {
                foreach (var item in items)
                {
                    item.IsSelected = false;
                }
            }
            selectedCandidateIds.Clear();
            UpdateSelectionUI();
        };

        var initialWords = await _vocabService.SearchWordsAsync(pageSize: 150);
        await DispatcherQueue.EnsureOnUIThreadAsync();
        BindCandidates(initialWords);

        CancellationTokenSource? dialogSearchCts = null;
        DispatcherTimer? dialogDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        dialogDebounceTimer.Tick += async (s, e) =>
        {
            dialogDebounceTimer.Stop();
            dialogSearchCts?.Cancel();
            dialogSearchCts?.Dispose();
            dialogSearchCts = new CancellationTokenSource();
            var token = dialogSearchCts.Token;

            try
            {
                var q = searchBox.Text;
                var words = await _vocabService.SearchWordsAsync(search: q, pageSize: 150, cancellationToken: token);
                await DispatcherQueue.EnsureOnUIThreadAsync();
                if (!token.IsCancellationRequested)
                {
                    BindCandidates(words);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] Candidate search error: {ex.Message}");
            }
        };

        searchBox.TextChanged += (s, e) =>
        {
            dialogDebounceTimer.Stop();
            dialogDebounceTimer.Start();
        };

        string? noteText = null;
        dialog.PrimaryButtonClick += (s, args) =>
        {
            dialogDebounceTimer.Stop();
            dialogSearchCts?.Cancel();
            noteText = noteBox.Text;
        };

        var res = await dialog.ShowAsyncSafe(this.XamlRoot);
        dialogDebounceTimer.Stop();
        dialogSearchCts?.Cancel();
        dialogSearchCts?.Dispose();
        await DispatcherQueue.EnsureOnUIThreadAsync();

        if (res == ContentDialogResult.Primary && selectedCandidateIds.Count > 0)
        {
            var idsToAdd = selectedCandidateIds.ToList();
            try
            {
                int addedCount = await ViewModel.AddWordsToStorageAsync(idsToAdd, string.IsNullOrWhiteSpace(noteText) ? null : noteText);
                if (addedCount > 0)
                {
                    _toastService?.ShowSuccess(string.Format(Loc.GetString("Storage_Toast_BulkAddedVocab"), addedCount), Loc.GetString("Nav_LearningStorage"));
                }
                else
                {
                    _toastService?.ShowWarning(Loc.GetString("Storage_Toast_AlreadyInStorage"), Loc.GetString("Nav_LearningStorage"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] MoveWordsToStorage error: {ex.Message}");
                _toastService?.ShowError(ex.Message, Loc.GetString("Common_Error"));
            }
        }
    }

    private async Task ShowAddGrammarDialogAsync()
    {
        var storedGrammarIds = (await _storageService.GetStoredGrammarRuleIdsAsync()).ToHashSet();

        var searchBox = new TextBox
        {
            PlaceholderText = Loc.GetString("Storage_Dialog_AddGrammarPlaceholder"),
            Margin = new Thickness(0, 0, 0, 8),
            CornerRadius = new CornerRadius(8)
        };

        var grammarTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Grid Padding=""6,6"" ColumnDefinitions=""Auto,Auto,*,Auto"" ColumnSpacing=""10"">
                    <CheckBox IsChecked=""{Binding IsSelected, Mode=TwoWay}"" VerticalAlignment=""Center"" Margin=""0,0,2,0"" MinWidth=""28"" />
                    <Border Grid.Column=""1"" Width=""30"" Height=""30"" CornerRadius=""6"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" VerticalAlignment=""Center"">
                        <FontIcon Glyph=""&#xE8D2;"" FontSize=""13"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" />
                    </Border>
                    <StackPanel Grid.Column=""2"" VerticalAlignment=""Center"" Spacing=""2"">
                        <TextBlock Text=""{Binding Title}"" FontWeight=""SemiBold"" FontSize=""14"" Foreground=""{ThemeResource TextFillColorPrimaryBrush}"" />
                        <TextBlock Text=""{Binding Subtitle}"" Foreground=""{ThemeResource TextFillColorSecondaryBrush}"" FontSize=""12"" TextTrimming=""CharacterEllipsis"" />
                    </StackPanel>
                    <Border Grid.Column=""3"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" CornerRadius=""4"" Padding=""6,2"" VerticalAlignment=""Center"">
                        <TextBlock Text=""{Binding Badge}"" FontSize=""10"" Foreground=""{ThemeResource TextFillColorTertiaryBrush}"" />
                    </Border>
                </Grid>
            </DataTemplate>");

        var listView = new ListView
        {
            MaxHeight = 300,
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = true,
            ItemTemplate = grammarTemplate
        };

        var selectedCandidateIds = new HashSet<Guid>();

        var btnSelectAll = new Button
        {
            Content = Loc.GetString("Storage_Dialog_SelectAll"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4, 10, 4),
            FontSize = 12
        };

        var btnDeselectAll = new Button
        {
            Content = Loc.GetString("Storage_Dialog_DeselectAll"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4, 10, 4),
            FontSize = 12
        };

        var txtSelectedCount = new TextBlock
        {
            Text = string.Format(Loc.GetString("Storage_Dialog_SelectedCount"), 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var actionsBar = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 8,
            Margin = new Thickness(0, 0, 0, 8)
        };
        actionsBar.Children.Add(btnSelectAll);
        Grid.SetColumn(btnSelectAll, 0);
        actionsBar.Children.Add(btnDeselectAll);
        Grid.SetColumn(btnDeselectAll, 1);
        actionsBar.Children.Add(txtSelectedCount);
        Grid.SetColumn(txtSelectedCount, 2);

        var txtEmpty = new TextBlock
        {
            Text = Loc.GetString("Storage_Dialog_AllItemsAlreadyAdded"),
            FontSize = 12,
            FontStyle = Windows.UI.Text.FontStyle.Italic,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["TextFillColorSecondaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 12),
            Visibility = Visibility.Collapsed
        };

        var noteBox = new TextBox
        {
            PlaceholderText = Loc.GetString("Storage_Dialog_AddNotePlaceholder"),
            Margin = new Thickness(0, 10, 0, 0),
            CornerRadius = new CornerRadius(8)
        };

        var panel = new StackPanel { Spacing = 6, MinWidth = 460 };
        panel.Children.Add(searchBox);
        panel.Children.Add(actionsBar);
        panel.Children.Add(txtEmpty);
        panel.Children.Add(listView);
        panel.Children.Add(noteBox);

        var activeGrammarTheme = GetActiveTheme();

        var dialog = new ContentDialog
        {
            Title = Loc.GetString("Storage_Dialog_AddMultipleGrammarTitle"),
            Content = panel,
            PrimaryButtonText = Loc.GetString("Action_Add"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            XamlRoot = this.XamlRoot,
            RequestedTheme = activeGrammarTheme
        };

        void UpdateSelectionUI()
        {
            txtSelectedCount.Text = string.Format(Loc.GetString("Storage_Dialog_SelectedCount"), selectedCandidateIds.Count);
            dialog.IsPrimaryButtonEnabled = selectedCandidateIds.Count > 0;
            dialog.PrimaryButtonText = selectedCandidateIds.Count > 0
                ? string.Format(Loc.GetString("Storage_Dialog_AddMultipleBtn"), selectedCandidateIds.Count)
                : Loc.GetString("Action_Add");
        }

        var allRules = await _grammarService.GetAllRulesAsync();
        await DispatcherQueue.EnsureOnUIThreadAsync();
        var candidateRules = allRules
            .Where(r => !storedGrammarIds.Contains(r.Id))
            .Select(r =>
            {
                var item = new StorageCandidateItem
                {
                    Id = r.Id,
                    Title = r.Title,
                    Subtitle = $"{r.Structure} • {r.ExplanationVi}",
                    Badge = r.Level.ToString(),
                    IsSelected = false
                };
                item.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(StorageCandidateItem.IsSelected))
                    {
                        if (item.IsSelected) selectedCandidateIds.Add(item.Id);
                        else selectedCandidateIds.Remove(item.Id);
                        UpdateSelectionUI();
                    }
                };
                return item;
            }).ToList();

        void UpdateGrammarListView(IEnumerable<StorageCandidateItem> items)
        {
            var list = items.ToList();
            listView.ItemsSource = list;
            txtEmpty.Text = string.IsNullOrWhiteSpace(searchBox.Text)
                ? Loc.GetString("Storage_Dialog_NoItemsAvailable")
                : Loc.GetString("Storage_Dialog_AllItemsAlreadyAdded");
            txtEmpty.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        UpdateGrammarListView(candidateRules);

        listView.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is StorageCandidateItem candidate)
            {
                candidate.IsSelected = !candidate.IsSelected;
            }
        };

        btnSelectAll.Click += (s, e) =>
        {
            if (listView.ItemsSource is IEnumerable<StorageCandidateItem> currentVisible)
            {
                foreach (var item in currentVisible)
                {
                    item.IsSelected = true;
                    selectedCandidateIds.Add(item.Id);
                }
                UpdateSelectionUI();
            }
        };

        btnDeselectAll.Click += (s, e) =>
        {
            foreach (var item in candidateRules)
            {
                item.IsSelected = false;
            }
            selectedCandidateIds.Clear();
            UpdateSelectionUI();
        };

        searchBox.TextChanged += (s, e) =>
        {
            var q = searchBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(q))
            {
                UpdateGrammarListView(candidateRules);
            }
            else
            {
                var filtered = candidateRules
                    .Where(r => r.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                r.Subtitle.Contains(q, StringComparison.OrdinalIgnoreCase));
                UpdateGrammarListView(filtered);
            }
        };

        string? noteText = null;
        dialog.PrimaryButtonClick += (s, args) =>
        {
            noteText = noteBox.Text;
        };

        var res = await dialog.ShowAsyncSafe(this.XamlRoot);
        await DispatcherQueue.EnsureOnUIThreadAsync();

        if (res == ContentDialogResult.Primary && selectedCandidateIds.Count > 0)
        {
            var idsToAdd = selectedCandidateIds.ToList();
            try
            {
                int addedCount = await ViewModel.AddGrammarsToStorageAsync(idsToAdd, string.IsNullOrWhiteSpace(noteText) ? null : noteText);
                if (addedCount > 0)
                {
                    _toastService?.ShowSuccess(string.Format(Loc.GetString("Storage_Toast_BulkAddedGrammar"), addedCount), Loc.GetString("Nav_LearningStorage"));
                }
                else
                {
                    _toastService?.ShowWarning(Loc.GetString("Storage_Toast_AlreadyInStorage"), Loc.GetString("Nav_LearningStorage"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] MoveGrammarToStorage error: {ex.Message}");
                _toastService?.ShowError(ex.Message, Loc.GetString("Common_Error"));
            }
        }
    }

    private async void PracticeBtn_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartPracticeAsync();
    }

    private async void TestBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Generates test focusing on Storage weak items
            var testSession = await _storageService.GenerateStorageTestAsync(questionCount: 15, prioritizeWeak: true);
            _navigationService.Navigate(typeof(TestPage), testSession);
        }
        catch (InvalidOperationException)
        {
            _toastService?.ShowWarning(Loc.GetString("Storage_Test_NoQuestions"));
        }
        catch (Exception ex)
        {
            _toastService?.ShowWarning(ex.Message);
        }
    }

    private void PracticeOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string answer)
        {
            ViewModel.SelectPracticeAnswer(answer);
        }
    }

    private void SubmitPractice_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SubmitPracticeAnswer();
    }

    private async void NextPractice_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NextPracticeQuestionAsync();
    }

    private void ClosePractice_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClosePractice();
    }
}

public partial class StorageCandidateItem : ObservableObject
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = false;

    public override string ToString() => string.IsNullOrWhiteSpace(Subtitle) ? Title : $"{Title} — {Subtitle}";
}
