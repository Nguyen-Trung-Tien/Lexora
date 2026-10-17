using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace Lexora.Presentation.Views;

public sealed partial class DownloadCenterPage : Page, ILocalizableView
{
    public DownloadCenterViewModel ViewModel { get; }
    public ILocalizationService Loc => ViewModel.Loc;

    public DownloadCenterPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<DownloadCenterViewModel>();
        InitializeComponent();

        Loaded += async (s, e) =>
        {
            await ViewModel.InitializeAsync();
        };

        Unloaded += (s, e) =>
        {
            ViewModel.Dispose();
        };
    }

    public void OnLanguageChanged()
    {
        Bindings.Update();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshCatalogAsync();
    }

    private async void TabVocab_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedTab = 0;
        await ViewModel.LoadCatalogAsync(forceRefresh: false);
    }

    private async void TabGrammar_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedTab = 1;
        await ViewModel.LoadCatalogAsync(forceRefresh: false);
    }

    private void TabDownloads_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedTab = 2;
    }

    private void TabShare_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedTab = 3;
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        await ViewModel.LoadCatalogAsync(forceRefresh: false);
    }

    private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await ViewModel.LoadCatalogAsync(forceRefresh: false);
    }

    private async void DownloadPackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CommunityPackageItemViewModel item)
        {
            await ViewModel.DownloadAndInstallPackageAsync(item);
        }
    }

    private async void PackageDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CommunityPackageMetadataDto dto)
        {
            var dialog = new ContentDialog
            {
                Title = dto.Name,
                CloseButtonText = Loc.GetString("Action_Close"),
                XamlRoot = this.XamlRoot
            };

            var stack = new StackPanel { Spacing = 12, MaxWidth = 480 };

            stack.Children.Add(new TextBlock
            {
                Text = dto.Description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["TextFillColorPrimaryBrush"]
            });

            var metaGrid = new Grid { ColumnSpacing = 16, RowSpacing = 8 };
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            void AddRow(int row, string label, string val)
            {
                metaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var lblBlock = new TextBlock { Text = label, Foreground = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["TextFillColorSecondaryBrush"], FontSize = 12 };
                var valBlock = new TextBlock { Text = val, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 12, TextWrapping = TextWrapping.Wrap };
                Grid.SetRow(lblBlock, row);
                Grid.SetColumn(lblBlock, 0);
                Grid.SetRow(valBlock, row);
                Grid.SetColumn(valBlock, 1);
                metaGrid.Children.Add(lblBlock);
                metaGrid.Children.Add(valBlock);
            }

            AddRow(0, Loc.GetString("DownloadCenter_PackageCategory"), dto.Category);
            AddRow(1, Loc.GetString("DownloadCenter_FilterLevel"), dto.Level);
            AddRow(2, Loc.GetString("DownloadCenter_FilterExam"), dto.TargetExam);
            AddRow(3, Loc.GetString("DownloadCenter_ItemsCount"), dto.ItemCount.ToString());
            AddRow(4, Loc.GetString("DownloadCenter_PackageAuthor"), dto.Author);
            AddRow(5, Loc.GetString("DownloadCenter_Version"), dto.Version);
            AddRow(6, Loc.GetString("DownloadCenter_License"), dto.License);
            if (!string.IsNullOrWhiteSpace(dto.ChecksumSha256))
            {
                AddRow(7, "SHA256", dto.ChecksumSha256);
            }

            stack.Children.Add(metaGrid);
            dialog.Content = stack;

            await dialog.ShowAsync();
        }
    }

    private async void RemoveInstalled_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string packageId)
        {
            var dialog = new ContentDialog
            {
                Title = Loc.GetString("DownloadCenter_Uninstall"),
                Content = string.Format(Loc.GetString("DownloadCenter_UninstallConfirm"), packageId),
                PrimaryButtonText = Loc.GetString("Action_Delete"),
                CloseButtonText = Loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var res = await dialog.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                await ViewModel.RemoveInstalledRecordAsync(packageId);
            }
        }
    }

    private async void PickImportFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".lexpkg");
            picker.FileTypeFilter.Add(".json");

            var mainWindow = App.Current.MainWindow;
            if (mainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var props = await file.GetBasicPropertiesAsync();
                if (props.Size > 20 * 1024 * 1024)
                {
                    await ShowErrorDialogAsync("Tệp quá lớn", "Kích thước tệp vượt quá hạn mức tối đa 20MB.");
                    return;
                }

                var text = await Windows.Storage.FileIO.ReadTextAsync(file);
                var resolution = ViewModel.SelectedConflictResolutionIndex == 1
                    ? ImportConflictResolution.Overwrite
                    : ImportConflictResolution.Skip;

                var result = await ViewModel.ImportFromRawJsonAsync(text, resolution);

                var successMsg = string.Format(
                    Loc.GetString("DownloadCenter_InstallSuccess"),
                    result.TotalImported,
                    result.TotalUpdated,
                    result.TotalSkipped);

                var successDialog = new ContentDialog
                {
                    Title = Loc.GetString("Toast_SuccessTitle"),
                    Content = successMsg,
                    CloseButtonText = Loc.GetString("Action_Close"),
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Toast_ErrorTitle"), ex.Message);
        }
    }

    private async void ImportUrl_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ImportFromDirectUrlAsync();
    }

    private async void ExportPackage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var json = await ViewModel.GenerateExportPackageJsonAsync();

            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            savePicker.FileTypeChoices.Add("Lexora Community Package (*.lexpkg)", new List<string> { ".lexpkg" });
            savePicker.FileTypeChoices.Add("JSON Package (*.json)", new List<string> { ".json" });
            savePicker.SuggestedFileName = string.IsNullOrWhiteSpace(ViewModel.ExportName)
                ? $"lexora_package_{DateTime.Now:yyyyMMdd}"
                : ViewModel.ExportName.Trim().ToLowerInvariant().Replace(' ', '-');

            var mainWindow = App.Current.MainWindow;
            if (mainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            }

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, json);

                var successMsg = string.Format(Loc.GetString("DownloadCenter_ExportSuccess"), 0, file.Path);
                var dialog = new ContentDialog
                {
                    Title = Loc.GetString("Toast_SuccessTitle"),
                    Content = successMsg,
                    CloseButtonText = Loc.GetString("Action_Close"),
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(Loc.GetString("Toast_ErrorTitle"), ex.Message);
        }
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        await Task.Yield();
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = Loc.GetString("Action_Close"),
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
