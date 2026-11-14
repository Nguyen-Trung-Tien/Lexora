using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class SettingsViewModel
{
    public IReleaseNotesService ReleaseNotesService => _releaseNotesService;

    public void UpdateCurrentVersionNotes()
    {
        _ = LoadCurrentVersionNotesAsync();
    }

    private async Task LoadCurrentVersionNotesAsync()
    {
        try
        {
            var note = await _releaseNotesService.GetCurrentVersionReleaseNoteAsync();
            if (note != null && !string.IsNullOrWhiteSpace(note.MarkdownContent))
            {
                RunOnUI(() => CurrentVersionNotes = note.MarkdownContent);
                return;
            }
        }
        catch
        {
            // fallback
        }

        var isVi = _locService.CurrentLanguage == "vi-VN";
        var fallback = isVi
            ? "• Lộ trình học tập tương tác (Interactive Roadmap Canvas): Hệ thống chặng học trực quan với đồ thị Bézier thông minh cho TOEIC, IELTS và VSTEP.\n• Thuật toán Spaced Repetition kép: Tích hợp SM-2 kinh điển và FSRS tiên tiến, tự động hiệu chuẩn Ease Factor.\n• Phòng Luyện Nghe & Phát Âm (Pronunciation Lab): Hỗ trợ giọng đọc chuẩn UK/US cùng hai tầng âm thanh (Cached Human Audio & Windows TTS).\n• Kiểm tra thích ứng (Adaptive Test Mode): Tự động cá nhân hóa bài thi và phân loại câu hỏi theo độ khó CEFR.\n• Tối ưu hóa giao diện Fluent 2: Hỗ trợ hoàn hảo chế độ Sáng (Light) và Tối (Dark), con trỏ chuột tương tác mượt mà."
            : "• Interactive Learning Roadmap: Dynamic visual skill tree with smooth Bézier splines for TOEIC, IELTS, and VSTEP preparation.\n• Dual Spaced Repetition Engines: Classic SM-2 and next-generation FSRS with dynamic Ease Factor calibration.\n• Pronunciation & Listening Lab: High-fidelity UK/US audio playback with cached human pronunciation and speech synthesis fallback.\n• Adaptive Testing & Quizzes: Tailored difficulty calibration matching real CEFR proficiencies.\n• Fluent 2 Visual Enhancements: Flawless Light & Dark mode rendering, high-contrast clarity, and unified interactive cursor pointers.";

        RunOnUI(() => CurrentVersionNotes = fallback);
    }

    public void RefreshUpdateTexts()
    {
        if (AvailableUpdate != null)
        {
            if (AvailableUpdate.FileSizeBytes > 0)
            {
                var sizeMb = (double)AvailableUpdate.FileSizeBytes / (1024 * 1024);
                AvailableUpdateFileSizeText = string.Format(_locService.GetString("Settings_SizeFormat"), sizeMb);
            }
            else
            {
                AvailableUpdateFileSizeText = string.Empty;
            }

            AvailableUpdatePublishDateText = $"{_locService.GetString("Settings_ReleaseDate")} {AvailableUpdate.PublishedAt.LocalDateTime:dd/MM/yyyy}";
        }

        if (UpdateAvailable)
        {
            UpdateStatusMessage = _locService.GetString("Settings_UpdateAvailable");
        }
        else if (UpdateReadyToInstall)
        {
            UpdateStatusMessage = _locService.GetString("Settings_UpdateDownloaded");
        }
        else if (IsCheckingUpdate)
        {
            UpdateStatusMessage = _locService.GetString("Settings_CheckingForUpdates");
        }
        else if (IsDownloadingUpdate)
        {
            UpdateStatusMessage = _locService.GetString("Settings_DownloadingUpdate");
        }
        else if (!string.IsNullOrEmpty(UpdateStatusMessage))
        {
            UpdateStatusMessage = _locService.GetString("Settings_UpdateUpToDate");
        }
    }

    [RelayCommand]
    public void ToggleCurrentVersionNotes()
    {
        IsShowingCurrentVersionNotes = !IsShowingCurrentVersionNotes;
    }

    [RelayCommand]
    public void ToggleExpandUpdateNotes()
    {
        IsExpandedUpdateNotes = !IsExpandedUpdateNotes;
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
                    IsShowingCurrentVersionNotes = false;
                    IsExpandedUpdateNotes = false;
                    RefreshUpdateTexts();

                    var template = _locService.GetString("Update_Notification_Body");
                    var msg = string.Format(template, updateInfo.NewVersion);
                    _toastService.ShowInfo(msg, _locService.GetString("Settings_UpdateAvailable"), durationMs: 6000);

                    UpdateFound?.Invoke(this, updateInfo);
                }
                else if (updateInfo != null)
                {
                    UpdateStatusMessage = _locService.GetString("Settings_UpdateUpToDate");
                    _toastService.ShowSuccess(_locService.GetString("Settings_UpdateUpToDate"), _locService.GetString("Settings_UpdateTitle"));
                }
                else
                {
                    UpdateStatusMessage = _locService.GetString("Settings_UpdateFailed");
                    _toastService.ShowError(_locService.GetString("Settings_UpdateFailed"), _locService.GetString("Settings_UpdateTitle"));
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
