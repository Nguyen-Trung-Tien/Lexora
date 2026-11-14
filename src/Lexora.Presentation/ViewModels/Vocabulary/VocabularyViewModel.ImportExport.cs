using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;

namespace Lexora.Presentation.ViewModels;

public partial class VocabularyViewModel
{
    public async Task<string> ExportToJsonAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportToJsonAsync(wordIds, progress: progress);
    }

    public async Task<string> ExportToCsvAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportToCsvAsync(wordIds, progress: progress);
    }

    public async Task<string> ExportToTxtAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportToTxtAsync(wordIds, progress: progress);
    }

    public async Task<string> ExportVocabularyOnlyJsonAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportVocabularyOnlyJsonAsync(wordIds, progress: progress);
    }

    public async Task<string> ExportVocabularyOnlyCsvAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportVocabularyOnlyCsvAsync(wordIds, progress: progress);
    }

    public async Task<string> ExportVocabularyOnlyTxtAsync(IEnumerable<Guid>? wordIds = null, IProgress<ProgressReport>? progress = null)
    {
        return await _exportImportService.ExportVocabularyOnlyTxtAsync(wordIds, progress: progress);
    }

    public async Task<ImportResult> ImportSmartAsync(
        string content,
        string? fileName = null,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _exportImportService.ImportSmartAsync(content, fileName, conflictResolution, cancellationToken, progress);
        await LoadWordsAsync();
        _ = LoadFiltersAsync();
        _ = LoadContextUnitsAsync();
        return result;
    }

    public async Task<ImportResult> ImportDataAsync(
        string content,
        string format,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null)
    {
        var result = format.ToLowerInvariant() switch
        {
            "json" => await _exportImportService.ImportFromJsonAsync(content, conflictResolution, progress: progress),
            "csv" => await _exportImportService.ImportFromCsvAsync(content, conflictResolution, progress: progress),
            "txt" => await _exportImportService.ImportFromTxtAsync(content, conflictResolution, progress: progress),
            _ => await _exportImportService.ImportSmartAsync(content, "", conflictResolution, progress: progress)
        };
        await LoadWordsAsync();
        _ = LoadFiltersAsync();
        _ = LoadContextUnitsAsync();
        return result;
    }

    public async Task<ImportResult> ImportDataAsync(string content, bool isJson, IProgress<ProgressReport>? progress = null)
    {
        return await ImportDataAsync(content, isJson ? "json" : "csv", ImportConflictResolution.Skip, progress);
    }

    public string GetSampleTemplate(string format)
    {
        return _exportImportService.GetSampleTemplate(format);
    }
}
