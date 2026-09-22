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
    public partial double SpeechRate { get; set; } = 1.0;

    [ObservableProperty]
    public partial bool EnableTypingPractice { get; set; }

    [ObservableProperty]
    public partial string BackupStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DatabasePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DatabaseSizeFormatted { get; set; } = string.Empty;

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
        SpeechRate = prefs.SpeechRate;
        EnableTypingPractice = prefs.EnableTypingPractice;

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
    public async Task SetSpeechRateAsync(double rate)
    {
        SpeechRate = rate;
        _audioService.PlaybackRate = rate;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { SpeechRate = rate });
    }

    [RelayCommand]
    public async Task SetTypingPracticeAsync(bool enabled)
    {
        EnableTypingPractice = enabled;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { EnableTypingPractice = enabled });
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
                DatabaseSizeFormatted = _locService.GetString("Settings_DbNotCreatedYet");
            }

            TotalWordsCount = await _vocabService.CountWordsAsync();
            var grammarRules = await _grammarService.GetAllRulesAsync();
            TotalGrammarCount = grammarRules.Count;
        }
        catch (Exception ex)
        {
            BackupStatusMessage = string.Format(_locService.GetString("Settings_FailedLoadStatsFormat"), ex.Message);
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
        var current = _preferencesService.GetPreferences();
        _ = _preferencesService.SavePreferencesAsync(current with { LanguageCode = langCode });
    }

    [RelayCommand]
    public async Task<string?> CreateBackupAsync(string? targetFilePath = null)
    {
        try
        {
            string backupPath;
            if (!string.IsNullOrWhiteSpace(targetFilePath))
            {
                backupPath = await _backupService.CreateBackupToFileAsync(targetFilePath);
            }
            else
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
                {
                    desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lexora", "backups");
                }
                backupPath = await _backupService.CreateBackupAsync(desktop);
            }

            BackupStatusMessage = string.Format(_locService.GetString("Settings_BackupSuccess"), backupPath);
            await LoadDatabaseStatsAsync();
            return backupPath;
        }
        catch (Exception ex)
        {
            BackupStatusMessage = string.Format(_locService.GetString("Settings_BackupFailed"), ex.Message);
            return null;
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath)
    {
        try
        {
            await _backupService.RestoreBackupAsync(backupFilePath);
            BackupStatusMessage = string.Format(_locService.GetString("Settings_RestoreSuccessStatus"), Path.GetFileName(backupFilePath));
            await LoadDatabaseStatsAsync();
            return true;
        }
        catch (Exception ex)
        {
            BackupStatusMessage = string.Format(_locService.GetString("Settings_RestoreFailedStatus"), ex.Message);
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
