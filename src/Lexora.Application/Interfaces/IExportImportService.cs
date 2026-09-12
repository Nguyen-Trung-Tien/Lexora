using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public record ImportResult(int TotalImported, int TotalSkipped, List<string> Errors);

public interface IExportImportService
{
    Task<string> ExportToJsonAsync(CancellationToken cancellationToken = default);
    Task<string> ExportToCsvAsync(CancellationToken cancellationToken = default);
    Task<ImportResult> ImportFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default);
    Task<ImportResult> ImportFromCsvAsync(string csvContent, CancellationToken cancellationToken = default);
}
