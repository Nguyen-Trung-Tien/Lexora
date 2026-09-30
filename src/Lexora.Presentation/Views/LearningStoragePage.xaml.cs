using System.Threading;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class LearningStoragePage : Page
{
    public LearningStorageViewModel ViewModel { get; }
    public Services.ILocalizationService Loc { get; }

    private readonly INavigationService _navigationService;
    private readonly IVocabularyService _vocabService;
    private readonly IGrammarService _grammarService;
    private readonly ILearningStorageService _storageService;
    private readonly IToastNotificationService? _toastService;
    private CancellationTokenSource? _searchCts;

    public LearningStoragePage()
    {
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
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private async void TabVocab_Click(object sender, RoutedEventArgs e)
    {
        TabVocabBtn.IsChecked = true;
        TabGrammarBtn.IsChecked = false;
        await ViewModel.SwitchTabAsync(0);
    }

    private async void TabGrammar_Click(object sender, RoutedEventArgs e)
    {
        TabVocabBtn.IsChecked = false;
        TabGrammarBtn.IsChecked = true;
        await ViewModel.SwitchTabAsync(1);
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
            var audio = item.Word?.AudioUsPath ?? item.Word?.AudioUkPath;
            await ViewModel.PlayAudioAsync(audio);
        }
    }

    private async void CardMasteredBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LearningStorageItemDto item)
        {
            await ViewModel.MarkAsMasteredAsync(item);
            _toastService?.ShowSuccess(Loc.GetString("Storage_Toast_Mastered"));
        }
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

            var dialog = new ContentDialog
            {
                Title = Loc.GetString("Storage_Dialog_EditNoteTitle"),
                Content = noteBox,
                PrimaryButtonText = Loc.GetString("Action_Save"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var res = await dialog.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                await ViewModel.SaveNoteAsync((item.Id, noteBox.Text));
                _toastService?.ShowSuccess(Loc.GetString("Storage_Toast_StatusUpdated"));
            }
        }
    }

    private async void MenuRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is LearningStorageItemDto item)
        {
            var dialog = new ContentDialog
            {
                Title = Loc.GetString("Storage_Dialog_ConfirmRemoveTitle"),
                Content = Loc.GetString("Storage_Dialog_ConfirmRemoveMessage"),
                PrimaryButtonText = Loc.GetString("Action_Delete"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var res = await dialog.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                await ViewModel.RemoveItemAsync(item);
                _toastService?.ShowSuccess(Loc.GetString("Storage_Toast_Removed"));
            }
        }
    }

    private async void OpenAddDialog_Click(object sender, RoutedEventArgs e)
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

    private async Task ShowAddWordDialogAsync()
    {
        var searchBox = new TextBox
        {
            PlaceholderText = Loc.GetString("Storage_Dialog_AddVocabPlaceholder"),
            Margin = new Thickness(0, 0, 0, 10),
            CornerRadius = new CornerRadius(8)
        };

        var wordTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Grid Padding=""6,8"" ColumnDefinitions=""Auto,*,Auto"" ColumnSpacing=""10"">
                    <Border Width=""30"" Height=""30"" CornerRadius=""6"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" VerticalAlignment=""Center"">
                        <FontIcon Glyph=""&#xE82D;"" FontSize=""13"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" />
                    </Border>
                    <StackPanel Grid.Column=""1"" VerticalAlignment=""Center"" Spacing=""2"">
                        <TextBlock Text=""{Binding Title}"" FontWeight=""SemiBold"" FontSize=""14"" Foreground=""{ThemeResource TextFillColorPrimaryBrush}"" />
                        <TextBlock Text=""{Binding Subtitle}"" Foreground=""{ThemeResource TextFillColorSecondaryBrush}"" FontSize=""12"" TextTrimming=""CharacterEllipsis"" />
                    </StackPanel>
                    <Border Grid.Column=""2"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" CornerRadius=""4"" Padding=""6,2"" VerticalAlignment=""Center"">
                        <TextBlock Text=""{Binding Badge}"" FontSize=""10"" Foreground=""{ThemeResource TextFillColorTertiaryBrush}"" />
                    </Border>
                </Grid>
            </DataTemplate>");

        var listView = new ListView
        {
            MaxHeight = 280,
            SelectionMode = ListViewSelectionMode.Single,
            ItemTemplate = wordTemplate
        };

        var allWords = await _vocabService.SearchWordsAsync(pageSize: 50);
        listView.ItemsSource = allWords.Select(w => new StorageCandidateItem
        {
            Id = w.Id,
            Title = w.Term,
            Subtitle = w.Meanings.FirstOrDefault()?.DefinitionVi ?? w.PhoneticUs ?? string.Empty,
            Badge = w.Level.ToString()
        }).ToList();

        searchBox.TextChanged += async (s, e) =>
        {
            var q = searchBox.Text;
            var words = await _vocabService.SearchWordsAsync(search: q, pageSize: 50);
            listView.ItemsSource = words.Select(w => new StorageCandidateItem
            {
                Id = w.Id,
                Title = w.Term,
                Subtitle = w.Meanings.FirstOrDefault()?.DefinitionVi ?? w.PhoneticUs ?? string.Empty,
                Badge = w.Level.ToString()
            }).ToList();
        };

        var noteBox = new TextBox
        {
            PlaceholderText = Loc.GetString("Storage_Dialog_AddNotePlaceholder"),
            Margin = new Thickness(0, 10, 0, 0),
            CornerRadius = new CornerRadius(8)
        };

        var panel = new StackPanel { Spacing = 6, MinWidth = 440 };
        panel.Children.Add(searchBox);
        panel.Children.Add(listView);
        panel.Children.Add(noteBox);

        var dialog = new ContentDialog
        {
            Title = Loc.GetString("Storage_Dialog_AddTitle"),
            Content = panel,
            PrimaryButtonText = Loc.GetString("Action_Add"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            XamlRoot = this.XamlRoot
        };

        listView.SelectionChanged += (s, e) =>
        {
            dialog.IsPrimaryButtonEnabled = listView.SelectedItem != null;
        };

        var res = await dialog.ShowAsync();
        if (res == ContentDialogResult.Primary && listView.SelectedItem is StorageCandidateItem selected)
        {
            await _storageService.MoveWordToStorageAsync(selected.Id, StorageAdditionReason.Manual, noteBox.Text);
            _toastService?.ShowSuccess(Loc.GetString("Storage_Toast_Added"));
            await ViewModel.InitializeAsync();
        }
    }

    private async Task ShowAddGrammarDialogAsync()
    {
        var searchBox = new TextBox
        {
            PlaceholderText = Loc.GetString("Storage_Dialog_AddGrammarPlaceholder"),
            Margin = new Thickness(0, 0, 0, 10),
            CornerRadius = new CornerRadius(8)
        };

        var grammarTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Grid Padding=""6,8"" ColumnDefinitions=""Auto,*,Auto"" ColumnSpacing=""10"">
                    <Border Width=""30"" Height=""30"" CornerRadius=""6"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" VerticalAlignment=""Center"">
                        <FontIcon Glyph=""&#xE8D2;"" FontSize=""13"" Foreground=""{ThemeResource AccentTextFillColorPrimaryBrush}"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" />
                    </Border>
                    <StackPanel Grid.Column=""1"" VerticalAlignment=""Center"" Spacing=""2"">
                        <TextBlock Text=""{Binding Title}"" FontWeight=""SemiBold"" FontSize=""14"" Foreground=""{ThemeResource TextFillColorPrimaryBrush}"" />
                        <TextBlock Text=""{Binding Subtitle}"" Foreground=""{ThemeResource TextFillColorSecondaryBrush}"" FontSize=""12"" TextTrimming=""CharacterEllipsis"" />
                    </StackPanel>
                    <Border Grid.Column=""2"" Background=""{ThemeResource SubtleFillColorSecondaryBrush}"" CornerRadius=""4"" Padding=""6,2"" VerticalAlignment=""Center"">
                        <TextBlock Text=""{Binding Badge}"" FontSize=""10"" Foreground=""{ThemeResource TextFillColorTertiaryBrush}"" />
                    </Border>
                </Grid>
            </DataTemplate>");

        var listView = new ListView
        {
            MaxHeight = 280,
            SelectionMode = ListViewSelectionMode.Single,
            ItemTemplate = grammarTemplate
        };

        var rules = await _grammarService.GetAllRulesAsync();
        var candidateRules = rules.Select(r => new StorageCandidateItem
        {
            Id = r.Id,
            Title = r.Title,
            Subtitle = $"{r.Structure} • {r.ExplanationVi}",
            Badge = r.Level.ToString()
        }).ToList();
        listView.ItemsSource = candidateRules;

        searchBox.TextChanged += (s, e) =>
        {
            var q = searchBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(q))
            {
                listView.ItemsSource = candidateRules;
            }
            else
            {
                listView.ItemsSource = candidateRules
                    .Where(r => r.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                r.Subtitle.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        };

        var noteBox = new TextBox
        {
            PlaceholderText = Loc.GetString("Storage_Dialog_AddNotePlaceholder"),
            Margin = new Thickness(0, 10, 0, 0),
            CornerRadius = new CornerRadius(8)
        };

        var panel = new StackPanel { Spacing = 6, MinWidth = 440 };
        panel.Children.Add(searchBox);
        panel.Children.Add(listView);
        panel.Children.Add(noteBox);

        var dialog = new ContentDialog
        {
            Title = Loc.GetString("Storage_Dialog_AddTitle"),
            Content = panel,
            PrimaryButtonText = Loc.GetString("Action_Add"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            XamlRoot = this.XamlRoot
        };

        listView.SelectionChanged += (s, e) =>
        {
            dialog.IsPrimaryButtonEnabled = listView.SelectedItem != null;
        };

        var res = await dialog.ShowAsync();
        if (res == ContentDialogResult.Primary && listView.SelectedItem is StorageCandidateItem selected)
        {
            await _storageService.MoveGrammarToStorageAsync(selected.Id, StorageAdditionReason.Manual, noteBox.Text);
            _toastService?.ShowSuccess(Loc.GetString("Storage_Toast_Added"));
            await ViewModel.InitializeAsync();
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

public class StorageCandidateItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;

    public override string ToString() => string.IsNullOrWhiteSpace(Subtitle) ? Title : $"{Title} — {Subtitle}";
}
