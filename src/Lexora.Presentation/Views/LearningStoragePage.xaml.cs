using System.Collections.ObjectModel;
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

public sealed partial class LearningStoragePage : Page, ILocalizableView, IThemeAwareView
{
    public LearningStorageViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }

    private readonly INavigationService _navigationService;
    private readonly IVocabularyService _vocabService;
    private readonly IGrammarService _grammarService;
    private readonly ILearningStorageService _storageService;
    private readonly ISmartSearchService _smartSearchService;
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
        _smartSearchService = App.Current.Services.GetRequiredService<ISmartSearchService>();
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
        SizeChanged += (s, e) => UpdateResponsiveLayout(e.NewSize.Width);
        ActualThemeChanged += (s, e) => OnThemeChanged();
    }

    public void OnLanguageChanged()
    {
        void UpdateUI()
        {
            ViewModel.UpdateLanguageState();
            UpdateSegmentedButtons();
            UpdateSelectionModeUI();
            PopulateStatusFilterComboBox();
            if (StorageSearchBox != null)
            {
                StorageSearchBox.PlaceholderText = Loc.GetString("Storage_Search_Placeholder");
            }
            if (StorageListView != null)
            {
                var prevItems = StorageListView.ItemsSource;
                StorageListView.ItemsSource = null;
                StorageListView.ItemsSource = prevItems;
            }
            Bindings.Update();
        }

        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateUI();
        }
        else
        {
            DispatcherQueue.TryEnqueue(UpdateUI);
        }
    }

    public void OnThemeChanged()
    {
        void UpdateThemeUI()
        {
            UpdateSegmentedButtons();
            UpdateSelectionModeUI();
            Bindings.Update();
        }

        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateThemeUI();
        }
        else
        {
            DispatcherQueue.TryEnqueue(UpdateThemeUI);
        }
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        OnLanguageChanged();
        OnThemeChanged();
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
        PopulateStatusFilterComboBox();
        UpdateSegmentedButtons();
        UpdateSelectionModeUI();
        UpdateResponsiveLayout(ActualWidth);
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (StorageToolbarGrid == null || TabSwitcherBorder == null || StorageSearchBox == null || StorageActionsScrollViewer == null) return;

        if (width < 960)
        {
            if (StorageToolbarGrid.RowDefinitions.Count == 0)
            {
                StorageToolbarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                StorageToolbarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                StorageToolbarGrid.RowSpacing = 8;
            }

            Grid.SetRow(TabSwitcherBorder, 0);
            Grid.SetColumn(TabSwitcherBorder, 0);
            Grid.SetRow(StorageSearchBox, 0);
            Grid.SetColumn(StorageSearchBox, 1);
            Grid.SetColumnSpan(StorageSearchBox, 1);

            Grid.SetRow(StorageActionsScrollViewer, 1);
            Grid.SetColumn(StorageActionsScrollViewer, 0);
            Grid.SetColumnSpan(StorageActionsScrollViewer, 3);
        }
        else
        {
            if (StorageToolbarGrid.RowDefinitions.Count > 0)
            {
                StorageToolbarGrid.RowDefinitions.Clear();
                StorageToolbarGrid.RowSpacing = 0;
            }

            Grid.SetRow(TabSwitcherBorder, 0);
            Grid.SetColumn(TabSwitcherBorder, 0);
            Grid.SetRow(StorageSearchBox, 0);
            Grid.SetColumn(StorageSearchBox, 1);
            Grid.SetColumnSpan(StorageSearchBox, 1);

            Grid.SetRow(StorageActionsScrollViewer, 0);
            Grid.SetColumn(StorageActionsScrollViewer, 2);
            Grid.SetColumnSpan(StorageActionsScrollViewer, 1);
        }

        if (TopHeaderGrid != null && TopActionsPanel != null)
        {
            if (width < 760)
            {
                if (TopHeaderGrid.RowDefinitions.Count == 0)
                {
                    TopHeaderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    TopHeaderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    TopHeaderGrid.RowSpacing = 10;
                }

                Grid.SetColumn(TopActionsPanel, 0);
                Grid.SetRow(TopActionsPanel, 1);
                Grid.SetColumnSpan(TopActionsPanel, 2);
            }
            else
            {
                if (TopHeaderGrid.RowDefinitions.Count > 0)
                {
                    TopHeaderGrid.RowDefinitions.Clear();
                    TopHeaderGrid.RowSpacing = 0;
                }

                Grid.SetColumn(TopActionsPanel, 1);
                Grid.SetRow(TopActionsPanel, 0);
                Grid.SetColumnSpan(TopActionsPanel, 1);
            }
        }
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

    private void PopulateStatusFilterComboBox()
    {
        if (StatusFilterComboBox == null) return;
        var selectedTag = (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "All";
        var filters = new (string Tag, string Key)[]
        {
            ("All", "Storage_FilterAll"),
            ("Weak", "Storage_FilterWeak"),
            ("Learning", "Storage_FilterLearning"),
            ("New", "Storage_FilterNew"),
            ("Mastered", "Storage_FilterMastered"),
            ("DueToday", "Storage_FilterDueToday")
        };

        StatusFilterComboBox.SelectionChanged -= FilterComboBox_SelectionChanged;
        try
        {
            StatusFilterComboBox.Items.Clear();
            int selectedIndex = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                var item = new ComboBoxItem
                {
                    Tag = filters[i].Tag,
                    Content = Loc.GetString(filters[i].Key)
                };
                StatusFilterComboBox.Items.Add(item);
                if (filters[i].Tag == selectedTag) selectedIndex = i;
            }
            StatusFilterComboBox.SelectedIndex = selectedIndex;
        }
        finally
        {
            StatusFilterComboBox.SelectionChanged += FilterComboBox_SelectionChanged;
        }
    }

    private void UpdateSegmentedButtons()
    {
        if (TextVocabTab != null) TextVocabTab.Text = Loc.GetString("Vocab_NavTab");
        if (TextGrammarTab != null) TextGrammarTab.Text = Loc.GetString("Grammar_NavTab");

        var cardBg = ThemeResourceHelper.GetThemeBrush(this, "CardBackgroundFillColorDefaultBrush");
        var cardStroke = ThemeResourceHelper.GetThemeBrush(this, "CardStrokeColorDefaultBrush");
        var accentText = ThemeResourceHelper.GetThemeBrush(this, "AccentTextFillColorPrimaryBrush");
        var secondaryText = ThemeResourceHelper.GetThemeBrush(this, "TextFillColorSecondaryBrush");
        var layerBg = ThemeResourceHelper.GetThemeBrush(this, "LayerFillColorDefaultBrush");
        var subtleBg = ThemeResourceHelper.GetThemeBrush(this, "SubtleFillColorSecondaryBrush");
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        void ApplySegment(Button? btn, Border? badge, bool isActive)
        {
            if (btn == null) return;
            if (isActive)
            {
                if (cardBg != null) btn.Background = cardBg;
                if (cardStroke != null) btn.BorderBrush = cardStroke;
                btn.BorderThickness = new Thickness(1);
                if (accentText != null) btn.Foreground = accentText;
                if (badge != null && layerBg != null) badge.Background = layerBg;
            }
            else
            {
                btn.Background = transparent;
                btn.BorderThickness = new Thickness(0);
                if (secondaryText != null) btn.Foreground = secondaryText;
                if (badge != null && subtleBg != null) badge.Background = subtleBg;
            }
        }

        ApplySegment(BtnVocabTab, BadgeVocab, ViewModel.IsVocabTabSelected);
        ApplySegment(BtnGrammarTab, BadgeGrammar, ViewModel.IsGrammarTabSelected);
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
            Margin = new Thickness(0, 0, 0, 6),
            CornerRadius = new CornerRadius(8)
        };

        var wordTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Grid Padding=""6,6"" ColumnDefinitions=""Auto,Auto,*,Auto,Auto"" ColumnSpacing=""10"">
                    <CheckBox IsChecked=""{Binding IsSelected, Mode=TwoWay}"" VerticalAlignment=""Center"" Margin=""0,0,2,0"" MinWidth=""28"" />
                    <Border Grid.Column=""1"" Width=""32"" Height=""32"" CornerRadius=""6"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" VerticalAlignment=""Center"">
                        <FontIcon Glyph=""&#xE82D;"" FontSize=""13"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" />
                    </Border>
                    <StackPanel Grid.Column=""2"" VerticalAlignment=""Center"" Spacing=""2"">
                        <StackPanel Orientation=""Horizontal"" Spacing=""6"">
                            <TextBlock Text=""{Binding Title}"" FontWeight=""SemiBold"" FontSize=""14"" Foreground=""{ThemeResource TextFillColorPrimaryBrush}"" VerticalAlignment=""Center"" />
                            <Border Background=""{ThemeResource AccentFillColorDefaultBrush}"" CornerRadius=""4"" Padding=""5,1"" Visibility=""{Binding MatchReasonVisibility}"" VerticalAlignment=""Center"">
                                <TextBlock Text=""{Binding MatchReason}"" FontSize=""9.5"" Foreground=""{ThemeResource TextOnAccentFillColorPrimaryBrush}"" FontWeight=""SemiBold"" />
                            </Border>
                        </StackPanel>
                        <TextBlock Text=""{Binding Subtitle}"" Foreground=""{ThemeResource TextFillColorSecondaryBrush}"" FontSize=""12"" TextTrimming=""CharacterEllipsis"" />
                    </StackPanel>
                    <Border Grid.Column=""3"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" CornerRadius=""4"" Padding=""6,2"" VerticalAlignment=""Center"">
                        <TextBlock Text=""{Binding Badge}"" FontSize=""10"" Foreground=""{ThemeResource TextFillColorTertiaryBrush}"" />
                    </Border>
                    <Button x:Name=""QuickAddButton"" Grid.Column=""4"" Style=""{ThemeResource SubtleButtonStyle}"" CornerRadius=""6"" Padding=""8,4"" VerticalAlignment=""Center"">
                        <StackPanel Orientation=""Horizontal"" Spacing=""4"">
                            <FontIcon Glyph=""&#xE710;"" FontSize=""11"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" />
                            <TextBlock Text=""" + Loc.GetString("Storage_Dialog_QuickAdd") + @""" FontSize=""11"" FontWeight=""SemiBold"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" />
                        </StackPanel>
                    </Button>
                </Grid>
            </DataTemplate>");

        var candidateCollection = new ObservableCollection<StorageCandidateItem>();

        var listView = new ListView
        {
            MaxHeight = 320,
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = true,
            ItemTemplate = wordTemplate,
            ItemsSource = candidateCollection
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

        // Filter / Suggestion Chips
        string activeFilterChip = "All";
        var chipButtons = new List<Button>();
        var filterChipsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        Button CreateVocabChip(string tag, string text, bool isDefault = false)
        {
            var chip = new Button
            {
                Content = text,
                Tag = tag,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10, 3, 10, 3),
                FontSize = 11.5
            };
            chip.Style = isDefault
                ? (Style)App.Current.Resources["AccentButtonStyle"]
                : (Style)App.Current.Resources["SubtleButtonStyle"];
            chipButtons.Add(chip);
            return chip;
        }

        var chipAll = CreateVocabChip("All", Loc.GetString("Storage_Dialog_FilterAll"), true);
        var chipWeak = CreateVocabChip("Weak", Loc.GetString("Storage_Dialog_FilterWeak"));
        var chipExam = CreateVocabChip("Exam", Loc.GetString("Storage_Dialog_FilterExam"));
        var chipA1A2 = CreateVocabChip("A1A2", Loc.GetString("Storage_Dialog_FilterA1A2"));
        var chipB1B2 = CreateVocabChip("B1B2", Loc.GetString("Storage_Dialog_FilterB1B2"));
        var chipC1C2 = CreateVocabChip("C1C2", Loc.GetString("Storage_Dialog_FilterC1C2"));

        filterChipsPanel.Children.Add(chipAll);
        filterChipsPanel.Children.Add(chipWeak);
        filterChipsPanel.Children.Add(chipExam);
        filterChipsPanel.Children.Add(chipA1A2);
        filterChipsPanel.Children.Add(chipB1B2);
        filterChipsPanel.Children.Add(chipC1C2);

        var chipScrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollMode = ScrollMode.Enabled,
            VerticalScrollMode = ScrollMode.Disabled,
            Content = filterChipsPanel,
            Margin = new Thickness(0, 0, 0, 6)
        };

        // Did-You-Mean Bar
        var didYouMeanPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 6),
            Visibility = Visibility.Collapsed
        };
        var didYouMeanIcon = new FontIcon
        {
            Glyph = "\uE7BE",
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["AccentTextFillColorPrimaryBrush"],
            VerticalAlignment = VerticalAlignment.Center
        };
        var didYouMeanLabel = new TextBlock
        {
            Text = Loc.GetString("Storage_Dialog_DidYouMean"),
            FontSize = 11.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["TextFillColorSecondaryBrush"],
            VerticalAlignment = VerticalAlignment.Center
        };
        var didYouMeanButtonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        didYouMeanPanel.Children.Add(didYouMeanIcon);
        didYouMeanPanel.Children.Add(didYouMeanLabel);
        didYouMeanPanel.Children.Add(didYouMeanButtonsPanel);

        var panel = new StackPanel { Spacing = 4, MinWidth = 480, MaxWidth = 560 };
        panel.Children.Add(searchBox);
        panel.Children.Add(chipScrollViewer);
        panel.Children.Add(didYouMeanPanel);
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
            txtSelectedCount.Text = string.Format(
                "{0} • " + Loc.GetString("Storage_Dialog_SelectedCount"),
                string.Format(Loc.GetString("Storage_Dialog_AvailableCount"), candidateCollection.Count),
                selectedCandidateIds.Count);

            dialog.IsPrimaryButtonEnabled = selectedCandidateIds.Count > 0;
            dialog.PrimaryButtonText = selectedCandidateIds.Count > 0
                ? string.Format(Loc.GetString("Storage_Dialog_AddMultipleBtn"), selectedCandidateIds.Count)
                : Loc.GetString("Action_Add");
        }

        async Task ExecuteVocabQueryAsync(string? queryText, CancellationToken token = default)
        {
            var q = queryText?.Trim() ?? string.Empty;
            var normQ = Lexora.Domain.Common.SearchTextHelper.Normalize(q);

            IReadOnlyList<WordDto> fetchedWords;

            if (string.IsNullOrWhiteSpace(q))
            {
                fetchedWords = activeFilterChip switch
                {
                    "Weak" => await _vocabService.SearchWordsAsync(isWeak: true, pageSize: 250, cancellationToken: token),
                    "Exam" => await _vocabService.SearchWordsAsync(examType: ExamType.Ielts, pageSize: 200, cancellationToken: token),
                    "A1A2" => await _vocabService.SearchWordsAsync(level: CefrLevel.A1, pageSize: 200, cancellationToken: token),
                    "B1B2" => await _vocabService.SearchWordsAsync(level: CefrLevel.B1, pageSize: 200, cancellationToken: token),
                    "C1C2" => await _vocabService.SearchWordsAsync(level: CefrLevel.C1, pageSize: 200, cancellationToken: token),
                    _ => await _vocabService.SearchWordsAsync(pageSize: 250, cancellationToken: token)
                };
            }
            else
            {
                fetchedWords = await _vocabService.SearchWordsAsync(search: q, pageSize: 300, cancellationToken: token);
            }

            await DispatcherQueue.EnsureOnUIThreadAsync();
            if (token.IsCancellationRequested) return;

            var candidates = fetchedWords.Where(w => !storedWordIds.Contains(w.Id)).ToList();

            List<StorageCandidateItem> rankedList;

            if (!string.IsNullOrWhiteSpace(normQ))
            {
                rankedList = candidates
                    .Select(w =>
                    {
                        var normTerm = Lexora.Domain.Common.SearchTextHelper.Normalize(w.Term);
                        double score = 0;
                        string matchReason = string.Empty;

                        if (string.Equals(normTerm, normQ, StringComparison.OrdinalIgnoreCase))
                        {
                            score = 1000.0;
                            matchReason = Loc.GetString("Storage_Dialog_MatchExact");
                        }
                        else if (normTerm.StartsWith(normQ, StringComparison.OrdinalIgnoreCase))
                        {
                            score = 800.0 - Math.Min(100, (normTerm.Length - normQ.Length) * 2);
                            matchReason = Loc.GetString("Storage_Dialog_MatchPrefix");
                        }
                        else if (IsWordBoundaryMatch(normTerm, normQ))
                        {
                            score = 650.0;
                            matchReason = Loc.GetString("Storage_Dialog_MatchPrefix");
                        }
                        else if (normTerm.Contains(normQ, StringComparison.OrdinalIgnoreCase))
                        {
                            score = 500.0;
                        }
                        else
                        {
                            if (w.Meanings != null)
                            {
                                foreach (var m in w.Meanings)
                                {
                                    var normVi = Lexora.Domain.Common.SearchTextHelper.Normalize(m.DefinitionVi);
                                    var normEn = Lexora.Domain.Common.SearchTextHelper.Normalize(m.DefinitionEn);
                                    if (normVi.Equals(normQ, StringComparison.OrdinalIgnoreCase) || normEn.Equals(normQ, StringComparison.OrdinalIgnoreCase))
                                    {
                                        score = Math.Max(score, 450.0);
                                        matchReason = Loc.GetString("Storage_Dialog_MatchMeaning");
                                        break;
                                    }
                                    if (normVi.StartsWith(normQ, StringComparison.OrdinalIgnoreCase) || normEn.StartsWith(normQ, StringComparison.OrdinalIgnoreCase))
                                    {
                                        score = Math.Max(score, 400.0);
                                        matchReason = Loc.GetString("Storage_Dialog_MatchMeaning");
                                        break;
                                    }
                                    if (normVi.Contains(normQ, StringComparison.OrdinalIgnoreCase) || normEn.Contains(normQ, StringComparison.OrdinalIgnoreCase))
                                    {
                                        score = Math.Max(score, 300.0);
                                        matchReason = Loc.GetString("Storage_Dialog_MatchMeaning");
                                        break;
                                    }
                                }
                            }
                        }

                        return new StorageCandidateItem
                        {
                            Id = w.Id,
                            Title = w.Term,
                            Subtitle = w.Meanings?.FirstOrDefault()?.DefinitionVi ?? w.PhoneticUs ?? string.Empty,
                            Badge = w.Level.ToString(),
                            MatchReason = matchReason,
                            RelevanceScore = score,
                            IsSelected = selectedCandidateIds.Contains(w.Id)
                        };
                    })
                    .OrderByDescending(item => item.RelevanceScore)
                    .ThenBy(item => item.Title)
                    .ToList();
            }
            else
            {
                rankedList = candidates
                    .Select(w => new StorageCandidateItem
                    {
                        Id = w.Id,
                        Title = w.Term,
                        Subtitle = w.Meanings?.FirstOrDefault()?.DefinitionVi ?? w.PhoneticUs ?? string.Empty,
                        Badge = w.Level.ToString(),
                        IsSelected = selectedCandidateIds.Contains(w.Id)
                    })
                    .OrderBy(item => item.Title)
                    .ToList();
            }

            candidateCollection.Clear();
            foreach (var item in rankedList)
            {
                item.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(StorageCandidateItem.IsSelected))
                    {
                        if (item.IsSelected) selectedCandidateIds.Add(item.Id);
                        else selectedCandidateIds.Remove(item.Id);
                        UpdateSelectionUI();
                    }
                };
                candidateCollection.Add(item);
            }

            UpdateSelectionUI();

            txtEmpty.Text = string.IsNullOrWhiteSpace(searchBox.Text)
                ? Loc.GetString("Storage_Dialog_NoItemsAvailable")
                : Loc.GetString("Storage_Dialog_AllItemsAlreadyAdded");
            txtEmpty.Visibility = candidateCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (candidateCollection.Count == 0 && !string.IsNullOrWhiteSpace(q))
            {
                var suggestions = await _vocabService.GetClosestSuggestionsAsync(q, 3, token);
                await DispatcherQueue.EnsureOnUIThreadAsync();
                if (suggestions != null && suggestions.Count > 0)
                {
                    didYouMeanButtonsPanel.Children.Clear();
                    foreach (var sug in suggestions)
                    {
                        var sugBtn = new Button
                        {
                            Content = sug,
                            Style = (Style)App.Current.Resources["SubtleButtonStyle"],
                            Padding = new Thickness(8, 2, 8, 2),
                            FontSize = 11,
                            CornerRadius = new CornerRadius(10)
                        };
                        sugBtn.Click += (s, e) =>
                        {
                            searchBox.Text = sug;
                            searchBox.SelectionStart = sug.Length;
                        };
                        didYouMeanButtonsPanel.Children.Add(sugBtn);
                    }
                    didYouMeanPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    didYouMeanPanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                didYouMeanPanel.Visibility = Visibility.Collapsed;
            }
        }

        foreach (var chip in chipButtons)
        {
            chip.Click += async (s, e) =>
            {
                activeFilterChip = (string)chip.Tag;
                foreach (var b in chipButtons)
                {
                    b.Style = (b == chip)
                        ? (Style)App.Current.Resources["AccentButtonStyle"]
                        : (Style)App.Current.Resources["SubtleButtonStyle"];
                }
                await ExecuteVocabQueryAsync(searchBox.Text);
            };
        }

        async void OnVocabQuickAddClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StorageCandidateItem candidate)
            {
                if (candidate.IsAdding) return;
                candidate.IsAdding = true;
                btn.IsEnabled = false;

                try
                {
                    int added = await ViewModel.AddWordsToStorageAsync(new[] { candidate.Id }, string.IsNullOrWhiteSpace(noteBox.Text) ? null : noteBox.Text);
                    if (added > 0)
                    {
                        storedWordIds.Add(candidate.Id);
                        selectedCandidateIds.Remove(candidate.Id);
                        candidateCollection.Remove(candidate);
                        UpdateSelectionUI();
                        txtEmpty.Visibility = candidateCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                        _toastService?.ShowSuccess(string.Format(Loc.GetString("Storage_Dialog_AddedItemToast"), candidate.Title), Loc.GetString("Nav_LearningStorage"));
                    }
                    else
                    {
                        _toastService?.ShowWarning(Loc.GetString("Storage_Toast_AlreadyInStorage"), Loc.GetString("Nav_LearningStorage"));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] VocabQuickAdd error: {ex.Message}");
                    _toastService?.ShowError(ex.Message, Loc.GetString("Common_Error"));
                    btn.IsEnabled = true;
                    candidate.IsAdding = false;
                }
            }
        }

        listView.ContainerContentChanging += (sender, args) =>
        {
            if (args.ItemContainer?.ContentTemplateRoot is Grid root)
            {
                var quickAddBtn = root.FindName("QuickAddButton") as Button;
                if (quickAddBtn != null)
                {
                    ToolTipService.SetToolTip(quickAddBtn, Loc.GetString("Storage_Dialog_QuickAddTooltip"));
                    quickAddBtn.Click -= OnVocabQuickAddClick;
                    quickAddBtn.Click += OnVocabQuickAddClick;
                }
            }
        };

        listView.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is StorageCandidateItem candidate)
            {
                candidate.IsSelected = !candidate.IsSelected;
            }
        };

        btnSelectAll.Click += (s, e) =>
        {
            foreach (var item in candidateCollection)
            {
                item.IsSelected = true;
                selectedCandidateIds.Add(item.Id);
            }
            UpdateSelectionUI();
        };

        btnDeselectAll.Click += (s, e) =>
        {
            foreach (var item in candidateCollection)
            {
                item.IsSelected = false;
            }
            selectedCandidateIds.Clear();
            UpdateSelectionUI();
        };

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
                await ExecuteVocabQueryAsync(searchBox.Text, token);
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

        await ExecuteVocabQueryAsync(string.Empty);

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
            Margin = new Thickness(0, 0, 0, 6),
            CornerRadius = new CornerRadius(8)
        };

        var grammarTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Grid Padding=""6,6"" ColumnDefinitions=""Auto,Auto,*,Auto,Auto"" ColumnSpacing=""10"">
                    <CheckBox IsChecked=""{Binding IsSelected, Mode=TwoWay}"" VerticalAlignment=""Center"" Margin=""0,0,2,0"" MinWidth=""28"" />
                    <Border Grid.Column=""1"" Width=""32"" Height=""32"" CornerRadius=""6"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" VerticalAlignment=""Center"">
                        <FontIcon Glyph=""&#xE8D2;"" FontSize=""13"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" />
                    </Border>
                    <StackPanel Grid.Column=""2"" VerticalAlignment=""Center"" Spacing=""2"">
                        <StackPanel Orientation=""Horizontal"" Spacing=""6"">
                            <TextBlock Text=""{Binding Title}"" FontWeight=""SemiBold"" FontSize=""14"" Foreground=""{ThemeResource TextFillColorPrimaryBrush}"" VerticalAlignment=""Center"" />
                            <Border Background=""{ThemeResource AccentFillColorDefaultBrush}"" CornerRadius=""4"" Padding=""5,1"" Visibility=""{Binding MatchReasonVisibility}"" VerticalAlignment=""Center"">
                                <TextBlock Text=""{Binding MatchReason}"" FontSize=""9.5"" Foreground=""{ThemeResource TextOnAccentFillColorPrimaryBrush}"" FontWeight=""SemiBold"" />
                            </Border>
                        </StackPanel>
                        <TextBlock Text=""{Binding Subtitle}"" Foreground=""{ThemeResource TextFillColorSecondaryBrush}"" FontSize=""12"" TextTrimming=""CharacterEllipsis"" />
                    </StackPanel>
                    <Border Grid.Column=""3"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" CornerRadius=""4"" Padding=""6,2"" VerticalAlignment=""Center"">
                        <TextBlock Text=""{Binding Badge}"" FontSize=""10"" Foreground=""{ThemeResource TextFillColorTertiaryBrush}"" />
                    </Border>
                    <Button x:Name=""QuickAddButton"" Grid.Column=""4"" Style=""{ThemeResource SubtleButtonStyle}"" CornerRadius=""6"" Padding=""8,4"" VerticalAlignment=""Center"">
                        <StackPanel Orientation=""Horizontal"" Spacing=""4"">
                            <FontIcon Glyph=""&#xE710;"" FontSize=""11"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" />
                            <TextBlock Text=""" + Loc.GetString("Storage_Dialog_QuickAdd") + @""" FontSize=""11"" FontWeight=""SemiBold"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" />
                        </StackPanel>
                    </Button>
                </Grid>
            </DataTemplate>");

        var grammarCollection = new ObservableCollection<StorageCandidateItem>();

        var listView = new ListView
        {
            MaxHeight = 320,
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = true,
            ItemTemplate = grammarTemplate,
            ItemsSource = grammarCollection
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

        // Filter / Suggestion Chips
        string activeGrammarChip = "All";
        var chipButtons = new List<Button>();
        var filterChipsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        Button CreateGrammarChip(string tag, string text, bool isDefault = false)
        {
            var chip = new Button
            {
                Content = text,
                Tag = tag,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10, 3, 10, 3),
                FontSize = 11.5
            };
            chip.Style = isDefault
                ? (Style)App.Current.Resources["AccentButtonStyle"]
                : (Style)App.Current.Resources["SubtleButtonStyle"];
            chipButtons.Add(chip);
            return chip;
        }

        var chipAll = CreateGrammarChip("All", Loc.GetString("Storage_Dialog_FilterAll"), true);
        var chipCore = CreateGrammarChip("Core", Loc.GetString("Storage_Dialog_FilterCore"));
        var chipA1A2 = CreateGrammarChip("A1A2", Loc.GetString("Storage_Dialog_FilterA1A2"));
        var chipB1B2 = CreateGrammarChip("B1B2", Loc.GetString("Storage_Dialog_FilterB1B2"));
        var chipC1C2 = CreateGrammarChip("C1C2", Loc.GetString("Storage_Dialog_FilterC1C2"));

        filterChipsPanel.Children.Add(chipAll);
        filterChipsPanel.Children.Add(chipCore);
        filterChipsPanel.Children.Add(chipA1A2);
        filterChipsPanel.Children.Add(chipB1B2);
        filterChipsPanel.Children.Add(chipC1C2);

        var chipScrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollMode = ScrollMode.Enabled,
            VerticalScrollMode = ScrollMode.Disabled,
            Content = filterChipsPanel,
            Margin = new Thickness(0, 0, 0, 6)
        };

        // Did-You-Mean Bar
        var didYouMeanPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 6),
            Visibility = Visibility.Collapsed
        };
        var didYouMeanIcon = new FontIcon
        {
            Glyph = "\uE7BE",
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["AccentTextFillColorPrimaryBrush"],
            VerticalAlignment = VerticalAlignment.Center
        };
        var didYouMeanLabel = new TextBlock
        {
            Text = Loc.GetString("Storage_Dialog_DidYouMean"),
            FontSize = 11.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["TextFillColorSecondaryBrush"],
            VerticalAlignment = VerticalAlignment.Center
        };
        var didYouMeanButtonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        didYouMeanPanel.Children.Add(didYouMeanIcon);
        didYouMeanPanel.Children.Add(didYouMeanLabel);
        didYouMeanPanel.Children.Add(didYouMeanButtonsPanel);

        var panel = new StackPanel { Spacing = 4, MinWidth = 480, MaxWidth = 560 };
        panel.Children.Add(searchBox);
        panel.Children.Add(chipScrollViewer);
        panel.Children.Add(didYouMeanPanel);
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

        void UpdateGrammarSelectionUI()
        {
            txtSelectedCount.Text = string.Format(
                "{0} • " + Loc.GetString("Storage_Dialog_SelectedCount"),
                string.Format(Loc.GetString("Storage_Dialog_AvailableCount"), grammarCollection.Count),
                selectedCandidateIds.Count);

            dialog.IsPrimaryButtonEnabled = selectedCandidateIds.Count > 0;
            dialog.PrimaryButtonText = selectedCandidateIds.Count > 0
                ? string.Format(Loc.GetString("Storage_Dialog_AddMultipleBtn"), selectedCandidateIds.Count)
                : Loc.GetString("Action_Add");
        }

        var allRules = await _grammarService.GetAllRulesAsync();
        await DispatcherQueue.EnsureOnUIThreadAsync();
        var candidateRules = allRules.Where(r => !storedGrammarIds.Contains(r.Id)).ToList();

        void ExecuteGrammarQuery(string? queryText)
        {
            var q = queryText?.Trim() ?? string.Empty;
            var normQ = Lexora.Domain.Common.SearchTextHelper.Normalize(q);

            var pool = candidateRules.Where(r => !storedGrammarIds.Contains(r.Id));

            if (string.IsNullOrWhiteSpace(normQ))
            {
                pool = activeGrammarChip switch
                {
                    "Core" => pool.Where(r => IsCoreGrammarRule(r)),
                    "A1A2" => pool.Where(r => r.Level == CefrLevel.A1 || r.Level == CefrLevel.A2),
                    "B1B2" => pool.Where(r => r.Level == CefrLevel.B1 || r.Level == CefrLevel.B2),
                    "C1C2" => pool.Where(r => r.Level == CefrLevel.C1 || r.Level == CefrLevel.C2),
                    _ => pool
                };
            }

            var poolList = pool.ToList();
            List<StorageCandidateItem> rankedList;

            if (!string.IsNullOrWhiteSpace(normQ))
            {
                rankedList = poolList
                    .Select(r =>
                    {
                        var normTitle = Lexora.Domain.Common.SearchTextHelper.Normalize(r.Title);
                        var normStruct = Lexora.Domain.Common.SearchTextHelper.Normalize(r.Structure);
                        var normExpl = Lexora.Domain.Common.SearchTextHelper.Normalize(r.ExplanationVi);
                        var normTopic = Lexora.Domain.Common.SearchTextHelper.Normalize(r.Topic);

                        double score = 0;
                        string matchReason = string.Empty;

                        if (string.Equals(normTitle, normQ, StringComparison.OrdinalIgnoreCase))
                        {
                            score = 1000.0;
                            matchReason = Loc.GetString("Storage_Dialog_MatchExact");
                        }
                        else if (normTitle.StartsWith(normQ, StringComparison.OrdinalIgnoreCase))
                        {
                            score = 800.0 - Math.Min(100, (normTitle.Length - normQ.Length) * 2);
                            matchReason = Loc.GetString("Storage_Dialog_MatchPrefix");
                        }
                        else if (normStruct.Contains(normQ, StringComparison.OrdinalIgnoreCase))
                        {
                            score = 650.0;
                            matchReason = Loc.GetString("Storage_Dialog_MatchStructure");
                        }
                        else if (normTitle.Contains(normQ, StringComparison.OrdinalIgnoreCase))
                        {
                            score = 550.0;
                        }
                        else if (normExpl.Contains(normQ, StringComparison.OrdinalIgnoreCase))
                        {
                            score = 450.0;
                            matchReason = Loc.GetString("Storage_Dialog_MatchMeaning");
                        }
                        else if (!string.IsNullOrEmpty(normTopic) && normTopic.Contains(normQ, StringComparison.OrdinalIgnoreCase))
                        {
                            score = 350.0;
                        }

                        return new StorageCandidateItem
                        {
                            Id = r.Id,
                            Title = r.Title,
                            Subtitle = $"{r.Structure} • {r.ExplanationVi}",
                            Badge = r.Level.ToString(),
                            MatchReason = matchReason,
                            RelevanceScore = score,
                            IsSelected = selectedCandidateIds.Contains(r.Id)
                        };
                    })
                    .Where(item => item.RelevanceScore > 0)
                    .OrderByDescending(item => item.RelevanceScore)
                    .ThenBy(item => item.Title)
                    .ToList();
            }
            else
            {
                rankedList = poolList
                    .Select(r => new StorageCandidateItem
                    {
                        Id = r.Id,
                        Title = r.Title,
                        Subtitle = $"{r.Structure} • {r.ExplanationVi}",
                        Badge = r.Level.ToString(),
                        IsSelected = selectedCandidateIds.Contains(r.Id)
                    })
                    .OrderBy(item => item.Title)
                    .ToList();
            }

            grammarCollection.Clear();
            foreach (var item in rankedList)
            {
                item.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(StorageCandidateItem.IsSelected))
                    {
                        if (item.IsSelected) selectedCandidateIds.Add(item.Id);
                        else selectedCandidateIds.Remove(item.Id);
                        UpdateGrammarSelectionUI();
                    }
                };
                grammarCollection.Add(item);
            }

            UpdateGrammarSelectionUI();

            txtEmpty.Text = string.IsNullOrWhiteSpace(searchBox.Text)
                ? Loc.GetString("Storage_Dialog_NoItemsAvailable")
                : Loc.GetString("Storage_Dialog_AllItemsAlreadyAdded");
            txtEmpty.Visibility = grammarCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (grammarCollection.Count == 0 && !string.IsNullOrWhiteSpace(q) && _smartSearchService != null)
            {
                var suggestions = _smartSearchService.FindClosestGrammarRuleSuggestions(q, candidateRules, maxSuggestions: 3);
                if (suggestions != null && suggestions.Count > 0)
                {
                    didYouMeanButtonsPanel.Children.Clear();
                    foreach (var sug in suggestions)
                    {
                        var sugBtn = new Button
                        {
                            Content = sug,
                            Style = (Style)App.Current.Resources["SubtleButtonStyle"],
                            Padding = new Thickness(8, 2, 8, 2),
                            FontSize = 11,
                            CornerRadius = new CornerRadius(10)
                        };
                        sugBtn.Click += (s, e) =>
                        {
                            searchBox.Text = sug;
                            searchBox.SelectionStart = sug.Length;
                        };
                        didYouMeanButtonsPanel.Children.Add(sugBtn);
                    }
                    didYouMeanPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    didYouMeanPanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                didYouMeanPanel.Visibility = Visibility.Collapsed;
            }
        }

        foreach (var chip in chipButtons)
        {
            chip.Click += (s, e) =>
            {
                activeGrammarChip = (string)chip.Tag;
                foreach (var b in chipButtons)
                {
                    b.Style = (b == chip)
                        ? (Style)App.Current.Resources["AccentButtonStyle"]
                        : (Style)App.Current.Resources["SubtleButtonStyle"];
                }
                ExecuteGrammarQuery(searchBox.Text);
            };
        }

        async void OnGrammarQuickAddClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StorageCandidateItem candidate)
            {
                if (candidate.IsAdding) return;
                candidate.IsAdding = true;
                btn.IsEnabled = false;

                try
                {
                    int added = await ViewModel.AddGrammarsToStorageAsync(new[] { candidate.Id }, string.IsNullOrWhiteSpace(noteBox.Text) ? null : noteBox.Text);
                    if (added > 0)
                    {
                        storedGrammarIds.Add(candidate.Id);
                        selectedCandidateIds.Remove(candidate.Id);
                        grammarCollection.Remove(candidate);
                        UpdateGrammarSelectionUI();
                        txtEmpty.Visibility = grammarCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                        _toastService?.ShowSuccess(string.Format(Loc.GetString("Storage_Dialog_AddedItemToast"), candidate.Title), Loc.GetString("Nav_LearningStorage"));
                    }
                    else
                    {
                        _toastService?.ShowWarning(Loc.GetString("Storage_Toast_AlreadyInStorage"), Loc.GetString("Nav_LearningStorage"));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LearningStoragePage] GrammarQuickAdd error: {ex.Message}");
                    _toastService?.ShowError(ex.Message, Loc.GetString("Common_Error"));
                    btn.IsEnabled = true;
                    candidate.IsAdding = false;
                }
            }
        }

        listView.ContainerContentChanging += (sender, args) =>
        {
            if (args.ItemContainer?.ContentTemplateRoot is Grid root)
            {
                var quickAddBtn = root.FindName("QuickAddButton") as Button;
                if (quickAddBtn != null)
                {
                    ToolTipService.SetToolTip(quickAddBtn, Loc.GetString("Storage_Dialog_QuickAddTooltip"));
                    quickAddBtn.Click -= OnGrammarQuickAddClick;
                    quickAddBtn.Click += OnGrammarQuickAddClick;
                }
            }
        };

        listView.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is StorageCandidateItem candidate)
            {
                candidate.IsSelected = !candidate.IsSelected;
            }
        };

        btnSelectAll.Click += (s, e) =>
        {
            foreach (var item in grammarCollection)
            {
                item.IsSelected = true;
                selectedCandidateIds.Add(item.Id);
            }
            UpdateGrammarSelectionUI();
        };

        btnDeselectAll.Click += (s, e) =>
        {
            foreach (var item in grammarCollection)
            {
                item.IsSelected = false;
            }
            selectedCandidateIds.Clear();
            UpdateGrammarSelectionUI();
        };

        searchBox.TextChanged += (s, e) =>
        {
            ExecuteGrammarQuery(searchBox.Text);
        };

        ExecuteGrammarQuery(string.Empty);

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

    private static bool IsWordBoundaryMatch(string text, string query)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query)) return false;
        int idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;
        bool leftBoundary = idx == 0 || !char.IsLetterOrDigit(text[idx - 1]);
        int endIdx = idx + query.Length;
        bool rightBoundary = endIdx == text.Length || !char.IsLetterOrDigit(text[endIdx]);
        return leftBoundary && rightBoundary;
    }

    private static bool IsCoreGrammarRule(GrammarRuleDto r)
    {
        var title = r.Title.ToLowerInvariant();
        var topic = (r.Topic ?? string.Empty).ToLowerInvariant();
        return title.Contains("tense") || title.Contains("thì") ||
               title.Contains("present") || title.Contains("past") || title.Contains("perfect") ||
               title.Contains("passive") || title.Contains("bị động") ||
               title.Contains("condition") || title.Contains("điều kiện") ||
               title.Contains("relative") || title.Contains("quan hệ") ||
               title.Contains("modal") || title.Contains("inversion") ||
               topic.Contains("tense") || topic.Contains("clause") || topic.Contains("passive");
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

    private void StorageHelpButton_Click(object sender, RoutedEventArgs e)
    {
        StorageHelpTeachingTip.IsOpen = true;
    }

    private void OpenFullGuide_Click(object sender, RoutedEventArgs e)
    {
        StorageHelpTeachingTip.IsOpen = false;
        var tag = (sender as FrameworkElement)?.Tag?.ToString() ?? "Vocabulary";
        _navigationService.Navigate(typeof(HelpPage), tag);
    }
}

public partial class StorageCandidateItem : ObservableObject
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string MatchReason { get; set; } = string.Empty;
    public bool HasMatchReason => !string.IsNullOrEmpty(MatchReason);
    public Visibility MatchReasonVisibility => HasMatchReason ? Visibility.Visible : Visibility.Collapsed;
    public bool IsRecommended { get; set; } = false;
    public double RelevanceScore { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = false;

    [ObservableProperty]
    public partial bool IsAdding { get; set; } = false;

    public override string ToString() => string.IsNullOrWhiteSpace(Subtitle) ? Title : $"{Title} — {Subtitle}";
}
