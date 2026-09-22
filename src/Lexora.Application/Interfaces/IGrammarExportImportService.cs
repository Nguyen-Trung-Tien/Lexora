using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IGrammarExportImportService
{
    /// <summary>Exports grammar rules to a JSON string.</summary>
    Task<string> ExportToJsonAsync(IEnumerable<Guid>? ruleIds = null, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>Exports grammar rules to a CSV string.</summary>
    Task<string> ExportToCsvAsync(IEnumerable<Guid>? ruleIds = null, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>Exports grammar rules to a formatted TXT/Markdown string.</summary>
    Task<string> ExportToTxtAsync(IEnumerable<Guid>? ruleIds = null, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>Imports grammar rules from a JSON string.</summary>
    Task<ImportResult> ImportFromJsonAsync(string jsonContent, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>Imports grammar rules from a CSV string.</summary>
    Task<ImportResult> ImportFromCsvAsync(string csvContent, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>Imports grammar rules from a TXT/Markdown string.</summary>
    Task<ImportResult> ImportFromTxtAsync(string txtContent, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>
    /// Intelligently analyzes content or file extension, auto-detects JSON/CSV/TXT format, and imports grammar rules.
    /// </summary>
    Task<ImportResult> ImportSmartAsync(string content, string? fileNameHint = null, ImportConflictResolution conflictResolution = ImportConflictResolution.Skip, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>
    /// Generates a ready-to-use sample template string in JSON, CSV, or TXT format.
    /// </summary>
    string GetSampleTemplate(string format);
}
