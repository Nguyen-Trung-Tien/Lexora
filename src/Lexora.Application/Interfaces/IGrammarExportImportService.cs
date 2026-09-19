using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IGrammarExportImportService
{
    /// <summary>Exports all grammar rules to a JSON string.</summary>
    Task<string> ExportToJsonAsync(CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>Exports all grammar rules to a CSV string.</summary>
    Task<string> ExportToCsvAsync(CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>Imports grammar rules from a JSON string, de-duplicating by title.</summary>
    Task<ImportResult> ImportFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);

    /// <summary>Imports grammar rules from a CSV string, de-duplicating by title.</summary>
    Task<ImportResult> ImportFromCsvAsync(string csvContent, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
}
