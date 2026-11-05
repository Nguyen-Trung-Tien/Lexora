using Lexora.Application.DTOs;
using Lexora.Presentation.Helpers;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class UpdateDetailsDialog : ContentDialog
{
    private readonly UpdateInfo _updateInfo;
    private readonly ILocalizationService _loc;
    private readonly Action? _onDownloadRequested;
    private readonly Action? _onInstallRequested;
    private readonly bool _isReadyToInstall;

    public UpdateDetailsDialog(
        UpdateInfo updateInfo,
        ILocalizationService loc,
        Action? onDownloadRequested = null,
        Action? onInstallRequested = null,
        bool isReadyToInstall = false)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);
        ArgumentNullException.ThrowIfNull(loc);

        _updateInfo = updateInfo;
        _loc = loc;
        _onDownloadRequested = onDownloadRequested;
        _onInstallRequested = onInstallRequested;
        _isReadyToInstall = isReadyToInstall;

        InitializeComponent();

        // Theme synchronization to guarantee 100% fidelity on Dark & Light modes
        if (App.Current?.Services?.GetService<IThemeService>() is { } themeService && themeService.CurrentTheme != ElementTheme.Default)
        {
            RequestedTheme = themeService.CurrentTheme;
        }
        else if (App.Current?.MainWindow?.Content is FrameworkElement rootElem && rootElem.ActualTheme != ElementTheme.Default)
        {
            RequestedTheme = rootElem.ActualTheme;
        }

        Title = _loc.GetString("Settings_UpdateDialogTitle");
        CloseButtonText = _loc.GetString("Settings_RemindLater");

        if (_isReadyToInstall)
        {
            PrimaryButtonText = _loc.GetString("Settings_InstallUpdate");
        }
        else
        {
            PrimaryButtonText = _loc.GetString("Settings_DownloadUpdate");
        }

        PopulateUI();

        ActualThemeChanged += (s, e) => RenderNotes();
        PrimaryButtonClick += UpdateDetailsDialog_PrimaryButtonClick;
    }

    private void PopulateUI()
    {
        VersionBadgeText.Text = $"v{_updateInfo.NewVersion}";
        ReleaseTitleText.Text = string.IsNullOrWhiteSpace(_updateInfo.ReleaseTitle)
            ? $"Lexora v{_updateInfo.NewVersion}"
            : _updateInfo.ReleaseTitle;

        var template = _loc.GetString("Settings_CurrentVsNewVersion");
        VersionTransitionText.Text = string.Format(template, _updateInfo.CurrentVersion, _updateInfo.NewVersion);

        if (_updateInfo.FileSizeBytes > 0)
        {
            var sizeMb = (double)_updateInfo.FileSizeBytes / (1024 * 1024);
            DownloadSizeText.Text = string.Format(_loc.GetString("Settings_SizeFormat"), sizeMb);
            DownloadSizeText.Visibility = Visibility.Visible;
        }
        else
        {
            DownloadSizeText.Visibility = Visibility.Collapsed;
        }

        PublishDateText.Text = $"{_loc.GetString("Settings_ReleaseDate")} {_updateInfo.PublishedAt.LocalDateTime:dd/MM/yyyy}";
        ReleaseNotesLabel.Text = _loc.GetString("Settings_ReleaseNotes");
        AutoBackupNoteText.Text = _loc.GetString("Settings_AutoBackupNote");

        RenderNotes();
    }

    private void RenderNotes()
    {
        ReleaseNotesRenderer.RenderMarkdown(_updateInfo.ReleaseNotes, NotesContainer, this);
    }

    private void UpdateDetailsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_isReadyToInstall)
        {
            _onInstallRequested?.Invoke();
        }
        else
        {
            _onDownloadRequested?.Invoke();
        }
    }
}
