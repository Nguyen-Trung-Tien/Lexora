using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;

namespace Lexora.Presentation.ViewModels;

public partial class PhrasesViewModel
{
    public Task<string> ExportPhrasesToFileAsync(
        string targetPath,
        string allowedBaseDirectory,
        DataFormat format,
        CancellationToken cancellationToken = default)
        => phraseService.ExportToFileAsync(
            targetPath,
            allowedBaseDirectory,
            format,
            phraseIds: null,
            cancellationToken: cancellationToken);

    public async Task<ImportResult?> ImportPhrasesContentAsync(
        string content,
        string? fileNameHint,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default)
    {
        if (IsSubmitting) return null;
        IsSubmitting = true;

        try
        {
            var result = await phraseService.ImportSmartAsync(
                content,
                fileNameHint,
                conflictResolution,
                cancellationToken);

            await LoadDataAsync();
            var summary = string.Format(
                localizationService.GetString("Phrases_ImportSummaryFormat"),
                result.TotalImported,
                result.TotalUpdated,
                result.TotalSkipped);
            SetStatus(summary, isError: result.TotalImported == 0 && result.TotalUpdated == 0 && result.Errors.Count > 0);
            return result;
        }
        catch (Exception ex)
        {
            SetStatus($"{localizationService.GetString("Phrases_ErrorImport")}: {ex.Message}", isError: true);
            return null;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public string GetSampleTemplate(string format)
        => phraseService.GetSampleTemplate(format);
}
