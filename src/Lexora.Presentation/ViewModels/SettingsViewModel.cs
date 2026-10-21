using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Lexora.Presentation.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _locService;
    private readonly IDatabaseBackupService _backupService;
    private readonly IVocabularyService _vocabService;
    private readonly IGrammarService _grammarService;
    private readonly IAudioPlayerService _audioService;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IUpdateService _updateService;
    private readonly IToastNotificationService _toastService;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly EventHandler? _languageChangedHandler;

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; } = 0; // 0 = System, 1 = Light, 2 = Dark

    [ObservableProperty]
    public partial string SpacedRepetitionAlgorithm { get; set; } = "SM2"; // "SM2" or "FSRS"

    [ObservableProperty]
    public partial int SelectedAlgorithmIndex { get; set; } = 0; // 0 = SM-2, 1 = FSRS v5

    [ObservableProperty]
    public partial double TargetRetention { get; set; } = 0.90; // 0.85, 0.90, 0.95

    [ObservableProperty]
    public partial int SelectedRetentionIndex { get; set; } = 1; // 0 = 85%, 1 = 90%, 2 = 95%

    [ObservableProperty]
    public partial string CurrentLanguage { get; set; }

    [ObservableProperty]
    public partial string CurrentAppVersion { get; set; } = "1.3.0";

    [ObservableProperty]
    public partial bool IsCheckingUpdate { get; set; }

    [ObservableProperty]
    public partial bool IsDownloadingUpdate { get; set; }

    [ObservableProperty]
    public partial bool UpdateAvailable { get; set; }

    [ObservableProperty]
    public partial UpdateInfo? AvailableUpdate { get; set; }

    [ObservableProperty]
    public partial string UpdateStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double DownloadProgressPercentage { get; set; }

    [ObservableProperty]
    public partial string DownloadProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool UpdateReadyToInstall { get; set; }

    [ObservableProperty]
    public partial string DownloadedInstallerPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool AutoPlayAudio { get; set; }

    [ObservableProperty]
    public partial bool EnableReviewReminder { get; set; } = true;

    [ObservableProperty]
    public partial double SpeechRate { get; set; } = 1.0;

    [ObservableProperty]
    public partial bool EnableTypingPractice { get; set; }

    [ObservableProperty]
    public partial bool AutoCheckUpdates { get; set; } = true;

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
    public partial string PreferredAccent { get; set; } = "uk";

    [ObservableProperty]
    public partial bool HasUkVoice { get; set; }

    [ObservableProperty]
    public partial bool HasUsVoice { get; set; }

    [ObservableProperty]
    public partial string UkVoiceDisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UsVoiceDisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UkVoiceStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UsVoiceStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoadingStats { get; set; }

    public SettingsViewModel(
        IThemeService themeService,
        ILocalizationService locService,
        IDatabaseBackupService backupService,
        IVocabularyService vocabService,
        IGrammarService grammarService,
        IAudioPlayerService audioService,
        IUserPreferencesService preferencesService,
        IUpdateService updateService,
        IToastNotificationService toastService)
    {
        _themeService = themeService;
        _locService = locService;
        _backupService = backupService;
        _vocabService = vocabService;
        _grammarService = grammarService;
        _audioService = audioService;
        _preferencesService = preferencesService;
        _updateService = updateService;
        _toastService = toastService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? App.Current?.UIDispatcherQueue
            ?? App.Current?.MainWindow?.DispatcherQueue;

        CurrentLanguage = _locService.CurrentLanguage;
        var prefs = _preferencesService.GetPreferences();
        SelectedThemeIndex = prefs.ThemeIndex;
        _themeService.ThemeChanged += (s, theme) =>
        {
            var idx = theme switch
            {
                ElementTheme.Light => 1,
                ElementTheme.Dark => 2,
                _ => 0
            };
            RunOnUI(() => SelectedThemeIndex = idx);
        };
        AutoPlayAudio = prefs.AutoPlayAudio;
        EnableReviewReminder = prefs.EnableReviewReminder;
        SpeechRate = prefs.SpeechRate;
        EnableTypingPractice = prefs.EnableTypingPractice;
        AutoCheckUpdates = prefs.AutoCheckUpdates;
        PreferredAccent = string.IsNullOrWhiteSpace(prefs.PreferredAccent) ? "uk" : prefs.PreferredAccent;

        SpacedRepetitionAlgorithm = string.Equals(prefs.SpacedRepetitionAlgorithm, "FSRS", StringComparison.OrdinalIgnoreCase) ? "FSRS" : "SM2";
        SelectedAlgorithmIndex = SpacedRepetitionAlgorithm == "FSRS" ? 1 : 0;
        TargetRetention = prefs.TargetRetention > 0 ? prefs.TargetRetention : 0.90;
        SelectedRetentionIndex = TargetRetention switch
        {
            <= 0.86 => 0,
            >= 0.94 => 2,
            _ => 1
        };

        DatabasePath = _backupService.DatabasePath;

        var curVer = _updateService.GetCurrentVersion();
        CurrentAppVersion = $"{curVer.Major}.{curVer.Minor}.{curVer.Build}";

        if (_updateService.LatestDiscoveredUpdate != null && _updateService.LatestDiscoveredUpdate.HasUpdate)
        {
            AvailableUpdate = _updateService.LatestDiscoveredUpdate;
            UpdateAvailable = true;
            UpdateStatusMessage = _locService.GetString("Settings_UpdateAvailable");
        }

        UpdateVoiceStatus();

        _languageChangedHandler = (s, e) =>
        {
            RunOnUI(UpdateLanguageState);
        };
        _locService.LanguageChanged += _languageChangedHandler;
    }

    public void Dispose()
    {
        if (_languageChangedHandler != null)
        {
            _locService.LanguageChanged -= _languageChangedHandler;
        }
        GC.SuppressFinalize(this);
    }

    public void UpdateLanguageState()
    {
        CurrentLanguage = _locService.CurrentLanguage;
        UpdateVoiceStatus();

        if (UpdateReadyToInstall)
        {
            UpdateStatusMessage = _locService.GetString("Settings_UpdateDownloaded");
        }
        else if (IsDownloadingUpdate)
        {
            UpdateStatusMessage = _locService.GetString("Settings_DownloadingUpdate");
        }
        else if (IsCheckingUpdate)
        {
            UpdateStatusMessage = _locService.GetString("Settings_CheckingForUpdates");
        }
        else if (UpdateAvailable && AvailableUpdate != null)
        {
            UpdateStatusMessage = _locService.GetString("Settings_UpdateAvailable");
        }
        else if (!string.IsNullOrEmpty(UpdateStatusMessage))
        {
            UpdateStatusMessage = _locService.GetString("Settings_UpdateUpToDate");
        }

        if (!File.Exists(DatabasePath))
        {
            DatabaseSizeFormatted = _locService.GetString("Settings_DbNotCreatedYet");
        }
    }

    public void UpdateVoiceStatus()
    {
        var resUk = _audioService.ResolveVoice("uk");
        HasUkVoice = resUk.IsExactMatch;
        UkVoiceDisplayName = resUk.Voice?.DisplayName ?? "N/A";
        UkVoiceStatus = HasUkVoice
            ? string.Format(_locService.GetString("Settings_Voice_Installed"), UkVoiceDisplayName)
            : string.Format(_locService.GetString("Settings_Voice_FallbackWarning"), UkVoiceDisplayName);

        var resUs = _audioService.ResolveVoice("us");
        HasUsVoice = resUs.IsExactMatch;
        UsVoiceDisplayName = resUs.Voice?.DisplayName ?? "N/A";
        UsVoiceStatus = HasUsVoice
            ? string.Format(_locService.GetString("Settings_Voice_Installed"), UsVoiceDisplayName)
            : string.Format(_locService.GetString("Settings_Voice_FallbackWarning"), UsVoiceDisplayName);
    }

    [RelayCommand]
    public async Task SetPreferredAccentAsync(string accent)
    {
        if (string.IsNullOrWhiteSpace(accent)) return;
        var normalized = accent.Trim().ToLowerInvariant();
        if (normalized != "uk" && normalized != "us") normalized = "uk";

        PreferredAccent = normalized;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { PreferredAccent = normalized });
    }

    [RelayCommand]
    public async Task OpenWindowsSpeechSettingsAsync()
    {
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:speech"));
        }
        catch
        {
        }
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
    public async Task SetAutoCheckUpdatesAsync(bool enabled)
    {
        AutoCheckUpdates = enabled;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { AutoCheckUpdates = enabled });
    }

    partial void OnAutoCheckUpdatesChanged(bool value)
    {
        var current = _preferencesService.GetPreferences();
        if (current.AutoCheckUpdates != value)
        {
            _ = _preferencesService.SavePreferencesAsync(current with { AutoCheckUpdates = value });
        }
    }

    private void RunOnUI(Action action)
    {
        var dq = _dispatcherQueue 
            ?? App.Current?.UIDispatcherQueue 
            ?? App.Current?.MainWindow?.DispatcherQueue;

        if (dq == null || dq.HasThreadAccess)
        {
            action();
        }
        else
        {
            dq.TryEnqueue(() => action());
        }
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadDatabaseStatsAsync();
    }

    [RelayCommand]
    public async Task LoadDatabaseStatsAsync()
    {
        RunOnUI(() => IsLoadingStats = true);
        try
        {
            DatabasePath = _backupService.DatabasePath;

            string formatted = _locService.GetString("Settings_DbNotCreatedYet");
            if (File.Exists(DatabasePath))
            {
                var fileInfo = new FileInfo(DatabasePath);
                var bytes = fileInfo.Length;
                formatted = bytes switch
                {
                    < 1024 => $"{bytes} B",
                    < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
                    _ => $"{bytes / (1024.0 * 1024.0):F2} MB"
                };
            }

            var wordCount = await _vocabService.CountWordsAsync();
            var grammarRules = await _grammarService.GetAllRulesAsync();

            RunOnUI(() =>
            {
                DatabaseSizeFormatted = formatted;
                TotalWordsCount = wordCount;
                TotalGrammarCount = grammarRules.Count;
            });
        }
        catch (Exception ex)
        {
            RunOnUI(() =>
            {
                BackupStatusMessage = string.Format(_locService.GetString("Settings_FailedLoadStatsFormat"), ex.Message);
            });
        }
        finally
        {
            RunOnUI(() => IsLoadingStats = false);
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
        UpdateLanguageState();
        var current = _preferencesService.GetPreferences();
        _ = _preferencesService.SavePreferencesAsync(current with { LanguageCode = langCode });
    }

    [RelayCommand]
    public async Task SetAlgorithmAsync(string algorithm)
    {
        SpacedRepetitionAlgorithm = string.Equals(algorithm, "FSRS", StringComparison.OrdinalIgnoreCase) ? "FSRS" : "SM2";
        SelectedAlgorithmIndex = SpacedRepetitionAlgorithm == "FSRS" ? 1 : 0;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { SpacedRepetitionAlgorithm = SpacedRepetitionAlgorithm });
    }

    [RelayCommand]
    public async Task SetTargetRetentionAsync(double retentionRate)
    {
        TargetRetention = retentionRate;
        SelectedRetentionIndex = retentionRate switch
        {
            <= 0.86 => 0,
            >= 0.94 => 2,
            _ => 1
        };
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { TargetRetention = TargetRetention });
    }

    private bool _isCreatingBackup = false;
    private bool _isRestoringBackup = false;

    [RelayCommand]
    public async Task<string?> CreateBackupAsync(string? targetFilePath = null)
    {
        if (_isCreatingBackup) return null;
        _isCreatingBackup = true;
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

            RunOnUI(() =>
            {
                BackupStatusMessage = string.Format(_locService.GetString("Settings_BackupSuccess"), backupPath);
            });
            await LoadDatabaseStatsAsync();
            return backupPath;
        }
        catch (Exception ex)
        {
            RunOnUI(() =>
            {
                BackupStatusMessage = string.Format(_locService.GetString("Settings_BackupFailed"), ex.Message);
            });
            return null;
        }
        finally
        {
            _isCreatingBackup = false;
        }
    }

    public async Task<BackupMetadata> GetBackupMetadataAsync(string backupFilePath)
    {
        return await _backupService.GetBackupMetadataAsync(backupFilePath);
    }

    public async Task<bool> RestoreBackupAsync(
        string backupFilePath,
        IProgress<(int Percent, string StepMessage, string? Detail)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_isRestoringBackup) return false;
        _isRestoringBackup = true;
        try
        {
            await _backupService.RestoreBackupAsync(backupFilePath, progress, cancellationToken);
            RunOnUI(() =>
            {
                BackupStatusMessage = string.Format(_locService.GetString("Settings_RestoreSuccessStatus"), Path.GetFileName(backupFilePath));
            });
            await LoadDatabaseStatsAsync();
            return true;
        }
        catch (OperationCanceledException)
        {
            RunOnUI(() =>
            {
                BackupStatusMessage = _locService.GetString("Common_OperationCancelled") ?? "Operation cancelled.";
            });
            return false;
        }
        catch (Exception ex)
        {
            RunOnUI(() =>
            {
                BackupStatusMessage = string.Format(_locService.GetString("Settings_RestoreFailedStatus"), ex.Message);
            });
            return false;
        }
        finally
        {
            _isRestoringBackup = false;
        }
    }

    [RelayCommand]
    public async Task TestVoiceAsync(string accent)
    {
        if (string.IsNullOrWhiteSpace(TestVoiceTerm))
            TestVoiceTerm = "Welcome to Lexora English Learning";

        await _audioService.PlayPronunciationAsync(TestVoiceTerm, accent);
    }

    [RelayCommand]
    public async Task CheckForUpdatesAsync()
    {
        if (IsCheckingUpdate || IsDownloadingUpdate) return;
        RunOnUI(() =>
        {
            IsCheckingUpdate = true;
            UpdateStatusMessage = _locService.GetString("Settings_CheckingForUpdates");
            UpdateAvailable = false;
            AvailableUpdate = null;
            UpdateReadyToInstall = false;
        });

        try
        {
            var updateInfo = await _updateService.CheckForUpdatesAsync(isManualCheck: true);
            RunOnUI(() =>
            {
                if (updateInfo != null && updateInfo.HasUpdate)
                {
                    AvailableUpdate = updateInfo;
                    UpdateAvailable = true;
                    UpdateStatusMessage = _locService.GetString("Settings_UpdateAvailable");

                    var template = _locService.GetString("Update_Notification_Body");
                    var msg = string.Format(template, updateInfo.NewVersion);
                    _toastService.ShowInfo(msg, _locService.GetString("Settings_UpdateAvailable"), durationMs: 6000);
                }
                else
                {
                    UpdateStatusMessage = _locService.GetString("Settings_UpdateUpToDate");
                    _toastService.ShowSuccess(_locService.GetString("Settings_UpdateUpToDate"), _locService.GetString("Settings_UpdateTitle"));
                }
            });
        }
        catch (Exception ex)
        {
            RunOnUI(() =>
            {
                UpdateStatusMessage = $"{_locService.GetString("Settings_UpdateFailed")}: {ex.Message}";
                _toastService.ShowError($"{_locService.GetString("Settings_UpdateFailed")}: {ex.Message}", _locService.GetString("Settings_UpdateTitle"));
            });
        }
        finally
        {
            RunOnUI(() => IsCheckingUpdate = false);
        }
    }

    [RelayCommand]
    public async Task DownloadUpdateAsync()
    {
        if (AvailableUpdate == null || IsDownloadingUpdate) return;
        RunOnUI(() =>
        {
            IsDownloadingUpdate = true;
            UpdateReadyToInstall = false;
            DownloadProgressPercentage = 0;
            DownloadProgressText = "0%";
            UpdateStatusMessage = _locService.GetString("Settings_DownloadingUpdate");
        });

        var progress = new Progress<UpdateDownloadProgress>(p =>
        {
            RunOnUI(() =>
            {
                DownloadProgressPercentage = p.ProgressPercentage;
                var mbReceived = (double)p.BytesReceived / (1024 * 1024);
                var mbTotal = (double)p.TotalBytesToReceive / (1024 * 1024);
                DownloadProgressText = $"{mbReceived:F1} MB / {mbTotal:F1} MB ({p.ProgressPercentage:F0}%)";
            });
        });

        try
        {
            var installerPath = await _updateService.DownloadUpdateAsync(AvailableUpdate, progress);
            RunOnUI(() =>
            {
                DownloadedInstallerPath = installerPath;
                UpdateReadyToInstall = true;
                UpdateStatusMessage = _locService.GetString("Settings_UpdateDownloaded");
                _toastService.ShowSuccess(_locService.GetString("Settings_UpdateDownloaded"), _locService.GetString("Settings_UpdateTitle"));
            });
        }
        catch (Exception ex)
        {
            RunOnUI(() =>
            {
                UpdateStatusMessage = $"{_locService.GetString("Settings_UpdateFailed")}: {ex.Message}";
                _toastService.ShowError($"{_locService.GetString("Settings_UpdateFailed")}: {ex.Message}", _locService.GetString("Settings_UpdateTitle"));
            });
        }
        finally
        {
            RunOnUI(() => IsDownloadingUpdate = false);
        }
    }

    [RelayCommand]
    public async Task InstallUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(DownloadedInstallerPath) || !File.Exists(DownloadedInstallerPath))
            return;

        try
        {
            await _updateService.LaunchInstallerAndExitAsync(DownloadedInstallerPath, silent: true);
        }
        catch (Exception ex)
        {
            RunOnUI(() =>
            {
                UpdateStatusMessage = $"{_locService.GetString("Settings_UpdateFailed")}: {ex.Message}";
            });
        }
    }
}
