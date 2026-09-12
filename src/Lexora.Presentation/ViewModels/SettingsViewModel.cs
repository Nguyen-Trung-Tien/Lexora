using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.Interfaces;
using Lexora.Infrastructure.Services;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml;

namespace Lexora.Presentation.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _locService;
    private readonly IDatabaseBackupService _backupService;
    private readonly IVocabularyService _vocabService;
    private readonly IGrammarService _grammarService;
    private readonly IAudioPlayerService _audioService;
    private readonly IUserPreferencesService _preferencesService;

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; } = 0; // 0 = System, 1 = Light, 2 = Dark

    [ObservableProperty]
    public partial string CurrentLanguage { get; set; }

    [ObservableProperty]
    public partial bool AutoPlayAudio { get; set; }

    [ObservableProperty]
    public partial bool EnableReviewReminder { get; set; } = true;

    [ObservableProperty]
    public partial string BackupStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DatabasePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DatabaseSizeFormatted { get; set; } = "Calculating...";

    [ObservableProperty]
    public partial int TotalWordsCount { get; set; }

    [ObservableProperty]
    public partial int TotalGrammarCount { get; set; }

    [ObservableProperty]
    public partial string TestVoiceTerm { get; set; } = "Welcome to Lexora English Learning";

    [ObservableProperty]
    public partial bool IsLoadingStats { get; set; }

    public SettingsViewModel(
        IThemeService themeService,
        ILocalizationService locService,
        IDatabaseBackupService backupService,
        IVocabularyService vocabService,
        IGrammarService grammarService,
        IAudioPlayerService audioService,
        IUserPreferencesService preferencesService)
    {
        _themeService = themeService;
        _locService = locService;
        _backupService = backupService;
        _vocabService = vocabService;
        _grammarService = grammarService;
        _audioService = audioService;
        _preferencesService = preferencesService;

        CurrentLanguage = _locService.CurrentLanguage;
        var prefs = _preferencesService.GetPreferences();
        SelectedThemeIndex = prefs.ThemeIndex;
        AutoPlayAudio = prefs.AutoPlayAudio;
        EnableReviewReminder = prefs.EnableReviewReminder;

        DatabasePath = _backupService.DatabasePath;
    }

    [RelayCommand]
    public async Task SetAutoPlayAudioAsync(bool enabled)
    {
        AutoPlayAudio = enabled;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { AutoPlayAudio = enabled });
    }

    [RelayCommand]
    public async Task SetReviewReminderAsync(bool enabled)
    {
        EnableReviewReminder = enabled;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { EnableReviewReminder = enabled });
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadDatabaseStatsAsync();
    }

    [RelayCommand]
    public async Task LoadDatabaseStatsAsync()
    {
        IsLoadingStats = true;
        try
        {
            DatabasePath = _backupService.DatabasePath;

            if (File.Exists(DatabasePath))
            {
                var fileInfo = new FileInfo(DatabasePath);
                var bytes = fileInfo.Length;
                DatabaseSizeFormatted = bytes switch
                {
                    < 1024 => $"{bytes} B",
                    < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
                    _ => $"{bytes / (1024.0 * 1024.0):F2} MB"
                };
            }
            else
            {
                DatabaseSizeFormatted = "Not created yet";
            }

            TotalWordsCount = await _vocabService.CountWordsAsync();
            var grammarRules = await _grammarService.GetAllRulesAsync();
            TotalGrammarCount = grammarRules.Count;
        }
        catch (Exception ex)
        {
            BackupStatusMessage = $"Failed to load stats: {ex.Message}";
        }
        finally
        {
            IsLoadingStats = false;
        }
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
            await LoadDatabaseStatsAsync();
        }
        catch (Exception ex)
        {
            BackupStatusMessage = $"Backup failed: {ex.Message}";
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath)
    {
        try
        {
            await _backupService.RestoreBackupAsync(backupFilePath);
            BackupStatusMessage = $"Database restored successfully from: {Path.GetFileName(backupFilePath)}";
            await LoadDatabaseStatsAsync();
            return true;
        }
        catch (Exception ex)
        {
            BackupStatusMessage = $"Restore failed: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    public async Task TestVoiceAsync(string accent)
    {
        if (string.IsNullOrWhiteSpace(TestVoiceTerm))
            TestVoiceTerm = "Welcome to Lexora English Learning";

        await _audioService.PlayPronunciationAsync(TestVoiceTerm, accent);
    }
}
