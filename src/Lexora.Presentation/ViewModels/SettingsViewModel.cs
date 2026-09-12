using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Infrastructure.Services;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml;

namespace Lexora.Presentation.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _locService;
    private readonly IDatabaseBackupService _backupService;

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; } = 0; // 0 = System, 1 = Light, 2 = Dark

    [ObservableProperty]
    public partial string CurrentLanguage { get; set; }

    [ObservableProperty]
    public partial string BackupStatusMessage { get; set; } = string.Empty;

    public SettingsViewModel(
        IThemeService themeService,
        ILocalizationService locService,
        IDatabaseBackupService backupService)
    {
        _themeService = themeService;
        _locService = locService;
        _backupService = backupService;

        CurrentLanguage = _locService.CurrentLanguage;
        SelectedThemeIndex = _themeService.CurrentTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };
    }

    [RelayCommand]
    public void ChangeTheme(int themeIndex)
    {
        SelectedThemeIndex = themeIndex;
        var elementTheme = themeIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        _themeService.SetTheme(elementTheme);
    }

    [RelayCommand]
    public void ChangeLanguage(string langCode)
    {
        CurrentLanguage = langCode;
        _locService.SetLanguage(langCode);
    }

    [RelayCommand]
    public async Task CreateBackupAsync()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var backupPath = await _backupService.CreateBackupAsync(desktop);
            BackupStatusMessage = $"Backup created successfully at: {backupPath}";
        }
        catch (Exception ex)
        {
            BackupStatusMessage = $"Backup failed: {ex.Message}";
        }
    }
}
