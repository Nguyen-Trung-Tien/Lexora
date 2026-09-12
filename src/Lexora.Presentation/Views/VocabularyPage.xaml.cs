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

    public VocabularyPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<VocabularyViewModel>();

        // Populate Level combo
        var levels = new List<object> { "All Levels" };
        levels.AddRange(Enum.GetValues<CefrLevel>().Cast<object>());
        LevelFilterCombo.ItemsSource = levels;
        LevelFilterCombo.SelectedIndex = 0;

        // Populate POS combo
        var posList = new List<object> { "All Parts of Speech" };
        posList.AddRange(Enum.GetValues<PartOfSpeech>().Cast<object>());
        PosFilterCombo.ItemsSource = posList;
        PosFilterCombo.SelectedIndex = 0;

        // Populate Status combo
        var statuses = new List<object> { "All Statuses" };
        statuses.AddRange(Enum.GetValues<LearningStatus>().Cast<object>());
        StatusFilterCombo.ItemsSource = statuses;
        StatusFilterCombo.SelectedIndex = 0;

        // Populate Sort combo
        SortCombo.ItemsSource = new List<string> { "Term (A-Z)", "Term (Z-A)", "CEFR Level (A1-C2)", "CEFR Level (C2-A1)", "Date Added (Newest)", "Date Added (Oldest)" };
        SortCombo.SelectedIndex = 0;

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _searchDebounceTimer.Tick += async (s, e) =>
        {
            _searchDebounceTimer.Stop();
            await ViewModel.LoadWordsAsync();
        };

        Loaded += async (s, e) =>
        {
            await ViewModel.InitializeAsync();
        };
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

    private async void LevelFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
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
        LevelFilterCombo.SelectedIndex = 0;
        PosFilterCombo.SelectedIndex = 0;
        StatusFilterCombo.SelectedIndex = 0;
        SortCombo.SelectedIndex = 0;
    }

    private async void AddWordButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WordEditorDialog(null, ViewModel.AvailableTopics, ViewModel.AvailableExamTags)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
        {
            await ViewModel.SaveWordAsync(dialog.ResultDto);
        }
    }

    private async void EditWordButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWord == null) return;

        var dialog = new WordEditorDialog(ViewModel.SelectedWord, ViewModel.AvailableTopics, ViewModel.AvailableExamTags)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
        {
            await ViewModel.SaveWordAsync(dialog.ResultDto);
        }
    }

    private async void DeleteWordButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedWord == null) return;

        var confirmDialog = new ContentDialog
        {
            Title = "Delete Word",
            Content = $"Are you sure you want to permanently delete '{ViewModel.SelectedWord.Term}'? All associated review progress will also be deleted.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
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
            ViewModel.StatusMessage = $"✓ Đã đánh dấu '{ViewModel.SelectedWord?.Term ?? string.Empty}' là Nhớ rồi.";
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Lỗi đánh dấu từ", ex.Message);
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
            ViewModel.StatusMessage = $"✕ Đã đánh dấu '{ViewModel.SelectedWord?.Term ?? string.Empty}' là Chưa nhớ.";
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Lỗi đánh dấu từ", ex.Message);
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
            var json = await ViewModel.ExportToJsonAsync();
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var filePath = Path.Combine(desktop, $"lexora_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            await File.WriteAllTextAsync(filePath, json);

            var dialog = new ContentDialog
            {
                Title = "Export Complete",
                Content = $"Vocabulary exported successfully to:\n{filePath}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Export Failed", ex.Message);
        }
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var csv = await ViewModel.ExportToCsvAsync();
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var filePath = Path.Combine(desktop, $"lexora_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            await File.WriteAllTextAsync(filePath, csv);

            var dialog = new ContentDialog
            {
                Title = "Export Complete",
                Content = $"Vocabulary exported successfully to:\n{filePath}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Export Failed", ex.Message);
        }
    }

    private async void ImportJson_Click(object sender, RoutedEventArgs e)
    {
        await ShowImportTextInputDialog(isJson: true);
    }

    private async void ImportCsv_Click(object sender, RoutedEventArgs e)
    {
        await ShowImportTextInputDialog(isJson: false);
    }

    private async Task ShowImportTextInputDialog(bool isJson)
    {
        var textBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 250,
            PlaceholderText = isJson ? "Paste JSON array of vocabulary here..." : "Paste CSV content (with header line) here..."
        };

        var dialog = new ContentDialog
        {
            Title = isJson ? "Import Vocabulary (JSON)" : "Import Vocabulary (CSV)",
            Content = textBox,
            PrimaryButtonText = "Import Now",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var res = await dialog.ShowAsync();
        if (res == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            var importResult = await ViewModel.ImportDataAsync(textBox.Text, isJson);
            await Task.Yield();
            var summaryDialog = new ContentDialog
            {
                Title = "Import Results",
                Content = $"Imported: {importResult.TotalImported} words\nSkipped: {importResult.TotalSkipped} words\nErrors: {importResult.Errors.Count}",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };
            await summaryDialog.ShowAsync();
        }
    }

    private async void ShowErrorDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
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
}
