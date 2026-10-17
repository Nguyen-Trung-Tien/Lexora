using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface ICommunityDataService
{
    /// <summary>
    /// Fetches the catalog from remote repository or local/embedded fallback.
    /// </summary>
    Task<CommunityCatalogDto> GetCatalogAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns filtered packages from the catalog.
    /// </summary>
    Task<IReadOnlyList<CommunityPackageMetadataDto>> GetAvailablePackagesAsync(CommunityPackageFilterDto? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all locally tracked installed community packages.
    /// </summary>
    Task<IReadOnlyList<InstalledPackageRecord>> GetInstalledPackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets installed package record by ID if installed.
    /// </summary>
    Task<InstalledPackageRecord?> GetInstalledRecordAsync(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and validates a community package payload via HTTPS or local repository fallback.
    /// </summary>
    Task<CommunityPackagePayloadDto> DownloadPackageAsync(CommunityPackageMetadataDto package, IProgress<ProgressReport>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a community package payload into the database with atomic transaction and conflict resolution.
    /// </summary>
    Task<ImportResult> InstallPackageAsync(CommunityPackagePayloadDto packagePayload, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, IProgress<ProgressReport>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports vocabulary or grammar rules into a shareable .lexpkg JSON string with metadata.
    /// </summary>
    Task<string> ExportPackageToJsonAsync(CommunityPackageMetadataDto metadata, IEnumerable<Guid>? selectedIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses and verifies a package payload JSON against security and schema constraints.
    /// </summary>
    Task<CommunityPackagePayloadDto> ParseAndValidatePackageAsync(string jsonContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads from direct URL or parses direct file content and imports it into the system.
    /// </summary>
    Task<ImportResult> ImportPackageFromFileOrUrlAsync(string contentOrUrl, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, IProgress<ProgressReport>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the tracking record for an installed package.
    /// </summary>
    Task<bool> DeleteInstalledRecordAsync(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a newer version of the package is available in the catalog.
    /// </summary>
    Task<bool> CheckPackageUpdateAsync(string packageId, string currentVersion, CancellationToken cancellationToken = default);
}
