using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public record ImportResult(int TotalImported, int TotalSkipped, List<string> Errors);

public interface IExportImportService
{
    Task<string> ExportToJsonAsync(CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
    Task<string> ExportToCsvAsync(CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
    Task<string> ExportToTxtAsync(CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
    Task<ImportResult> ImportFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
    Task<ImportResult> ImportFromCsvAsync(string csvContent, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
    Task<ImportResult> ImportFromTxtAsync(string txtContent, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null);
}
