using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IKnowledgeGraphExportImportService
{
    Task<string> ExportToJsonAsync(Guid? centerWordId = null, CancellationToken cancellationToken = default);
    Task<string> ExportToCsvAsync(Guid? centerWordId = null, CancellationToken cancellationToken = default);
    Task<string> ExportToMarkdownMindmapAsync(Guid? centerWordId = null, CancellationToken cancellationToken = default);
    Task<KnowledgeGraphImportAnalysisResultDto> AnalyzeImportAsync(string content, string format, CancellationToken cancellationToken = default);
    Task<KnowledgeGraphImportCommitResultDto> CommitImportAsync(KnowledgeGraphImportAnalysisResultDto analysis, bool autoCreateMissingWords, CancellationToken cancellationToken = default);
    string GetSampleTemplate(string format);
}
