using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IHomophoneExportImportService
{
    Task<string> ExportToJsonAsync(IEnumerable<Guid>? groupIds = null, CancellationToken cancellationToken = default);
    Task<string> ExportToCsvAsync(IEnumerable<Guid>? groupIds = null, CancellationToken cancellationToken = default);
    Task<string> ExportToTxtAsync(IEnumerable<Guid>? groupIds = null, CancellationToken cancellationToken = default);

    Task<SmartHomophoneImportAnalysisDto> AnalyzeImportAsync(string content, string? fileNameHint = null, CancellationToken cancellationToken = default);
    Task<ImportResult> CommitImportAsync(SmartHomophoneCommitRequestDto request, CancellationToken cancellationToken = default);

    string GetSampleTemplate(string format);
}
