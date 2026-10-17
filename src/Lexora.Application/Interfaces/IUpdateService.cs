using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

/// <summary>
/// Service contract for checking, downloading, and applying production software updates.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Returns the currently running application version.
    /// </summary>
    Version GetCurrentVersion();

    /// <summary>
    /// Checks the release channel (e.g. GitHub Releases) for newly published versions.
    /// </summary>
    /// <param name="isManualCheck">True if invoked directly by user action, false if background periodic check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<UpdateInfo?> CheckForUpdatesAsync(bool isManualCheck = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the update installer executable to a secure local temp directory,
    /// tracking progress and optionally verifying cryptographic checksums.
    /// </summary>
    /// <param name="update">The target release metadata.</param>
    /// <param name="progress">Progress reporter for UI rendering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verified local file path to the downloaded installer.</returns>
    Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely creates a database snapshot backup, clears database connection pools,
    /// launches the installer in unattended/silent mode, and shuts down the application.
    /// </summary>
    /// <param name="installerFilePath">Local path to the installer executable.</param>
    /// <param name="silent">Whether to run the installer with silent flags.</param>
    Task LaunchInstallerAndExitAsync(string installerFilePath, bool silent = true);
}
