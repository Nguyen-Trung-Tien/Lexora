using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Services;

/// <summary>
/// Production-grade update service managing release discovery, asset verification,
/// automated pre-update database snapshots, and seamless Inno Setup execution.
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly IDatabaseBackupService _backupService;
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _repoOwner;
    private readonly string _repoName;

    public const string DefaultRepoOwner = "Nguyen-Trung-Tien";
    public const string DefaultRepoName = "Lexora";

    public UpdateService(
        IDatabaseBackupService backupService,
        ILogger<UpdateService> logger,
        HttpClient? httpClient = null,
        string? repoOwner = null,
        string? repoName = null)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _repoOwner = string.IsNullOrWhiteSpace(repoOwner) ? DefaultRepoOwner : repoOwner.Trim();
        _repoName = string.IsNullOrWhiteSpace(repoName) ? DefaultRepoName : repoName.Trim();
    }

    /// <inheritdoc/>
    public UpdateInfo? LatestDiscoveredUpdate { get; private set; }

    /// <inheritdoc/>
    public Version GetCurrentVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? typeof(UpdateService).Assembly;
        return asm.GetName().Version ?? new Version(1, 0, 0, 0);
    }

    /// <inheritdoc/>
    public async Task<UpdateInfo?> CheckForUpdatesAsync(bool isManualCheck = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var curVersion = GetCurrentVersion();
            var curVerStr = $"{curVersion.Major}.{curVersion.Minor}.{curVersion.Build}";

            var requestUri = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.UserAgent.ParseAdd("Lexora-Desktop-Client");
            request.Headers.Accept.ParseAdd("application/vnd.github.v3+json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub Releases check returned status code {StatusCode}", response.StatusCode);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            var releaseTitle = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? tagName : tagName;
            var releaseNotes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            var publishedAt = root.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTimeOffset(out var dt)
                ? dt
                : DateTimeOffset.UtcNow;

            string downloadUrl = string.Empty;
            long fileSize = 0;
            string? sha256Checksum = null;

            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsProp.EnumerateArray())
                {
                    var assetName = asset.TryGetProperty("name", out var anProp) ? anProp.GetString() ?? "" : "";
                    if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.TryGetProperty("browser_download_url", out var bdlProp) ? bdlProp.GetString() ?? "" : "";
                        fileSize = asset.TryGetProperty("size", out var sProp) ? sProp.GetInt64() : 0;
                    }
                    else if (assetName.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
                    {
                        // In case a sha256 companion file is attached
                        var shaUrl = asset.TryGetProperty("browser_download_url", out var bdlProp) ? bdlProp.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(shaUrl))
                        {
                            try
                            {
                                var shaText = await _httpClient.GetStringAsync(shaUrl, cancellationToken);
                                var match = Regex.Match(shaText, @"[a-fA-F0-9]{64}");
                                if (match.Success)
                                {
                                    sha256Checksum = match.Value;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Failed to fetch companion .sha256 file from {Url}", shaUrl);
                            }
                        }
                    }
                }
            }

            // If sha256 was not found in assets, check if it was specified in the release body
            if (string.IsNullOrEmpty(sha256Checksum))
            {
                var bodyShaMatch = Regex.Match(releaseNotes, @"(?:SHA256|sha256)[\s:]+([a-fA-F0-9]{64})");
                if (bodyShaMatch.Success)
                {
                    sha256Checksum = bodyShaMatch.Groups[1].Value;
                }
            }

            var info = new UpdateInfo(
                CurrentVersion: curVerStr,
                NewVersion: tagName,
                ReleaseTitle: string.IsNullOrWhiteSpace(releaseTitle) ? tagName : releaseTitle,
                ReleaseNotes: releaseNotes,
                DownloadUrl: downloadUrl,
                FileSizeBytes: fileSize,
                Sha256Checksum: sha256Checksum,
                PublishedAt: publishedAt);

            LatestDiscoveredUpdate = info;
            return info;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for software updates from GitHub Releases");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (string.IsNullOrWhiteSpace(update.DownloadUrl))
        {
            throw new InvalidOperationException("Update asset download URL is missing.");
        }

        var baseTempDir = Path.Combine(Path.GetTempPath(), "Lexora", "updates");
        Directory.CreateDirectory(baseTempDir);

        // Sanitize version to prevent path traversal
        var safeVersion = Regex.Replace(update.NewVersion, @"[^a-zA-Z0-9_\.-]", "_");
        var fileName = $"Lexora-Setup-{safeVersion}.exe";
        var targetFilePath = Path.Combine(baseTempDir, fileName);

        // Verify canonical path to enforce Path Traversal Defense (AGENTS.md 6.1)
        var fullPath = Path.GetFullPath(targetFilePath);
        var canonicalBaseDir = Path.GetFullPath(baseTempDir);
        if (!fullPath.StartsWith(canonicalBaseDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException("Potential path traversal attempt detected in download target path.");
        }

        _logger.LogInformation("Downloading software update {Version} to {Path} from {Url}", update.NewVersion, fullPath, update.DownloadUrl);

        using var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? update.FileSizeBytes;
        await using (var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await remoteStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                var percent = totalBytes > 0 ? (double)totalRead / totalBytes * 100 : 0;
                progress?.Report(new UpdateDownloadProgress(totalRead, totalBytes, percent));
            }
        }

        // Cryptographic integrity verification
        if (!string.IsNullOrWhiteSpace(update.Sha256Checksum))
        {
            _logger.LogInformation("Verifying SHA-256 checksum for {File}", fullPath);
            await using var verifyStream = File.OpenRead(fullPath);
            var computedHashBytes = await SHA256.HashDataAsync(verifyStream, cancellationToken);
            var computedHashHex = Convert.ToHexString(computedHashBytes);

            if (!string.Equals(computedHashHex, update.Sha256Checksum, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(fullPath); } catch { /* best effort cleanup */ }
                throw new InvalidDataException($"SHA-256 checksum mismatch. Expected '{update.Sha256Checksum}', computed '{computedHashHex}'. File has been discarded.");
            }

            _logger.LogInformation("SHA-256 verification passed for {File}", fullPath);
        }

        return fullPath;
    }

    /// <inheritdoc/>
    public async Task LaunchInstallerAndExitAsync(string installerFilePath, bool silent = true)
    {
        if (string.IsNullOrWhiteSpace(installerFilePath) || !File.Exists(installerFilePath))
        {
            throw new FileNotFoundException("Installer executable was not found.", installerFilePath);
        }

        _logger.LogInformation("Initiating pre-update database snapshot and application exit for {Installer}", installerFilePath);

        // Step 1: Pre-update database backup snapshot
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var backupDir = Path.Combine(appData, "Lexora", "backups", "pre-update");
            Directory.CreateDirectory(backupDir);
            await _backupService.CreateBackupAsync(backupDir);
            _logger.LogInformation("Pre-update database snapshot created successfully in {Dir}", backupDir);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pre-update database backup encountered an error, continuing update invocation.");
        }

        // Step 2: Clear SQLite connection pools so no database files are locked during overwrite
        try
        {
            SqliteConnection.ClearAllPools();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error clearing SQLite connection pools");
        }

        // Step 3: Launch Inno Setup installer
        // /CLOSEAPPLICATIONS instructs Inno Setup to cleanly close running instances of Lexora
        // /RESTARTAPPLICATIONS re-launches the application after upgrade
        // /SILENT executes with a minimal progress dialog without blocking Next prompts
        var arguments = silent
            ? "/SILENT /SP- /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS"
            : "/CLOSEAPPLICATIONS /RESTARTAPPLICATIONS";

        var startInfo = new ProcessStartInfo
        {
            FileName = installerFilePath,
            Arguments = arguments,
            UseShellExecute = true
        };

        Process.Start(startInfo);

        // Step 4: Gracefully terminate current process
        await Task.Delay(250);
        Environment.Exit(0);
    }
}
