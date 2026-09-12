using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<SettingsViewModel>();
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
    }

    private void LangEnButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeLanguage("en-US");
    }

    private async void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateBackupAsync();
    }
}
