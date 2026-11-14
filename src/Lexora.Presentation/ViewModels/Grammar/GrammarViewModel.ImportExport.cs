using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;

namespace Lexora.Presentation.ViewModels;

public partial class GrammarViewModel
{
    public Task<string> ExportToJsonAsync(IEnumerable<Guid>? ruleIds = null, IProgress<ProgressReport>? progress = null)
        => _grammarExportImportService.ExportToJsonAsync(ruleIds, progress: progress);

    public Task<string> ExportToCsvAsync(IEnumerable<Guid>? ruleIds = null, IProgress<ProgressReport>? progress = null)
        => _grammarExportImportService.ExportToCsvAsync(ruleIds, progress: progress);

    public Task<string> ExportToTxtAsync(IEnumerable<Guid>? ruleIds = null, IProgress<ProgressReport>? progress = null)
        => _grammarExportImportService.ExportToTxtAsync(ruleIds, progress: progress);

    public async Task<ImportResult> ImportSmartAsync(
        string content,
        string? fileNameHint = null,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null)
    {
        var result = await _grammarExportImportService.ImportSmartAsync(content, fileNameHint, conflictResolution, progress: progress);
        await RefreshAfterImportAsync();
        return result;
    }

    public Task<ImportResult> ImportDataAsync(
        string content,
        string format,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null)
        => format.ToLowerInvariant() switch
        {
            "json" => _grammarExportImportService.ImportFromJsonAsync(content, conflictResolution, progress: progress),
            "csv"  => _grammarExportImportService.ImportFromCsvAsync(content, conflictResolution, progress: progress),
            "txt"  => _grammarExportImportService.ImportFromTxtAsync(content, conflictResolution, progress: progress),
            _      => Task.FromResult(new ImportResult(0, 0, [$"Unsupported format: {format}"], 0))
        };

    public string GetSampleTemplate(string format)
        => _grammarExportImportService.GetSampleTemplate(format);

    /// <summary>Refreshes the rule list after a successful import.</summary>
    public async Task RefreshAfterImportAsync()
    {
        await LoadTopicsAsync();
        await LoadRulesAsync();
    }
}
