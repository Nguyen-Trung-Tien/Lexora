using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;

namespace Lexora.Presentation.ViewModels;

public partial class SettingsViewModel
{
    private bool _isCreatingBackup = false;
    private bool _isRestoringBackup = false;

    [RelayCommand]
    public async Task<string?> CreateBackupAsync(string? targetFilePath = null)
    {
        if (_isCreatingBackup || IsSubmitting) return null;
        _isCreatingBackup = true;
        IsSubmitting = true;
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
            IsSubmitting = false;
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
        if (_isRestoringBackup || IsSubmitting) return false;
        _isRestoringBackup = true;
        IsSubmitting = true;
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
            IsSubmitting = false;
        }
    }
}
