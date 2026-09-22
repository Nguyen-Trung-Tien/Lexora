using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public enum ImportConflictResolution
{
    /// <summary>Skip existing items to keep database entries untouched.</summary>
    Skip = 0,

    /// <summary>Update existing items and merge newly discovered meanings and examples.</summary>
    Overwrite = 1
}

public record ImportResult(int TotalImported, int TotalSkipped, List<string> Errors, int TotalUpdated = 0);

public interface IExportImportService
{
    Task<string> ExportToJsonAsync(IEnumerable<Guid>? wordIds = null, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
    Task<string> ExportToCsvAsync(IEnumerable<Guid>? wordIds = null, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
    Task<string> ExportToTxtAsync(IEnumerable<Guid>? wordIds = null, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    Task<ImportResult> ImportFromJsonAsync(string jsonContent, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
    Task<ImportResult> ImportFromCsvAsync(string csvContent, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
    Task<ImportResult> ImportFromTxtAsync(string txtContent, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>
    /// Intelligently analyzes the content or file extension, auto-detects JSON/CSV/TXT format, and imports with the chosen conflict resolution.
    /// </summary>
    Task<ImportResult> ImportSmartAsync(string content, string? fileNameHint = null, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>
    /// Generates a ready-to-use sample template string in JSON, CSV, or TXT format.
    /// </summary>
    string GetSampleTemplate(string format);
}
