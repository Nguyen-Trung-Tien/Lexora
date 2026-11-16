using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services.Phrases;

public partial class ConversationPhraseService
{
    private const int MaxImportPayloadLength = 20 * 1024 * 1024; // 20 MB

    private static readonly JsonSerializerOptions PhraseJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<string> ExportToJsonAsync(
        IEnumerable<Guid>? phraseIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(15, 0, 0, "Loading phrases...", "Loading phrases..."));
        var items = await GetPhrasesForExportAsync(phraseIds, cancellationToken);
        var models = items.Select(ToExportModel).ToList();
        progress?.Report(new ProgressReport(95, models.Count, models.Count, "Formatting JSON...", "Formatting JSON..."));
        return JsonSerializer.Serialize(models, PhraseJsonOptions);
    }

    public async Task<string> ExportToCsvAsync(
        IEnumerable<Guid>? phraseIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        var items = await GetPhrasesForExportAsync(phraseIds, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("Category,ContextTag,CefrLevel,Register,ExamTarget,PromptSentence,PromptTranslation,SampleResponse,ResponseTranslation,KeyPhrases,FormalVariation,CasualVariation,CommonMistakesVi,AdditionalSuggestions,IsFavorite");

        foreach (var p in items)
        {
            sb.AppendLine(string.Join(',',
                EscapeCsv(p.Category),
                EscapeCsv(p.ContextTag),
                EscapeCsv(p.CefrLevel.ToString()),
                EscapeCsv(p.Register.ToString()),
                EscapeCsv(p.ExamTarget?.ToString() ?? string.Empty),
                EscapeCsv(p.PromptSentence),
                EscapeCsv(p.PromptTranslation),
                EscapeCsv(p.SampleResponse),
                EscapeCsv(p.ResponseTranslation),
                EscapeCsv(p.KeyPhrases),
                EscapeCsv(p.FormalVariation),
                EscapeCsv(p.CasualVariation),
                EscapeCsv(p.CommonMistakesVi),
                EscapeCsv(p.AdditionalSuggestions),
                p.IsFavorite ? "true" : "false"));
        }

        progress?.Report(new ProgressReport(100, items.Count, items.Count, "CSV ready", "CSV ready"));
        return sb.ToString();
    }

    public async Task<string> ExportToTxtAsync(
        IEnumerable<Guid>? phraseIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        var items = await GetPhrasesForExportAsync(phraseIds, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("Category\tContextTag\tCefrLevel\tRegister\tExamTarget\tPromptSentence\tPromptTranslation\tSampleResponse\tResponseTranslation\tKeyPhrases");

        foreach (var p in items)
        {
            sb.AppendLine(string.Join('\t',
                SanitizeTabField(p.Category),
                SanitizeTabField(p.ContextTag),
                p.CefrLevel.ToString(),
                p.Register.ToString(),
                p.ExamTarget?.ToString() ?? string.Empty,
                SanitizeTabField(p.PromptSentence),
                SanitizeTabField(p.PromptTranslation),
                SanitizeTabField(p.SampleResponse),
                SanitizeTabField(p.ResponseTranslation),
                SanitizeTabField(p.KeyPhrases)));
        }

        progress?.Report(new ProgressReport(100, items.Count, items.Count, "TXT ready", "TXT ready"));
        return sb.ToString();
    }

    public async Task<string> ExportToFileAsync(
        string targetPath,
        string allowedBaseDirectory,
        DataFormat format,
        IEnumerable<Guid>? phraseIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(allowedBaseDirectory);

        var candidatePath = Path.IsPathRooted(targetPath)
            ? targetPath
            : Path.Combine(allowedBaseDirectory, targetPath);

        var validatedPath = SafePathHelper.ValidatePathWithinDirectory(candidatePath, allowedBaseDirectory);
        var directory = Path.GetDirectoryName(validatedPath) ?? allowedBaseDirectory;
        var safeFileName = SafePathHelper.SanitizeFileName(Path.GetFileName(validatedPath));
        var finalPath = SafePathHelper.ValidatePathWithinDirectory(Path.Combine(directory, safeFileName), allowedBaseDirectory);

        var content = format switch
        {
            DataFormat.Json => await ExportToJsonAsync(phraseIds, cancellationToken),
            DataFormat.Csv => await ExportToCsvAsync(phraseIds, cancellationToken),
            _ => await ExportToTxtAsync(phraseIds, cancellationToken)
        };

        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(finalPath, content, new UTF8Encoding(false), cancellationToken);
        return finalPath;
    }

    public async Task<ImportResult> ImportSmartAsync(
        string content,
        string? fileNameHint = null,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        if (ValidateImportPayload(content) is { } sizeError)
        {
            return sizeError;
        }

        var format = SmartContentDetector.DetectFormat(content, fileNameHint);
        return format switch
        {
            DataFormat.Json => await ImportFromJsonAsync(content, conflictResolution, cancellationToken, progress),
            DataFormat.Csv => await ImportFromCsvAsync(content, conflictResolution, cancellationToken, progress),
            _ => await ImportFromTxtAsync(content, conflictResolution, cancellationToken, progress)
        };
    }

    public async Task<ImportResult> ImportFromJsonAsync(
        string jsonContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        if (ValidateImportPayload(jsonContent) is { } invalid)
        {
            return invalid;
        }

        var cleaned = SmartImportParser.StripMarkdownCodeFence(jsonContent);
        List<ConversationPhraseExportModel>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<ConversationPhraseExportModel>>(cleaned, PhraseJsonOptions);
        }
        catch (JsonException ex)
        {
            return new ImportResult(0, 0, [$"Invalid JSON payload: {ex.Message}"], 0);
        }

        if (parsed is null || parsed.Count == 0)
        {
            return new ImportResult(0, 0, ["No conversation phrases found in JSON payload."], 0);
        }

        return await CommitImportedModelsAsync(parsed, conflictResolution, cancellationToken, progress);
    }

    public Task<ImportResult> ImportFromCsvAsync(
        string csvContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
        => ImportDelimitedAsync(csvContent, ',', conflictResolution, cancellationToken, progress);

    public Task<ImportResult> ImportFromTxtAsync(
        string txtContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        var delimiter = txtContent?.Contains('\t') == true ? '\t' : '|';
        return ImportDelimitedAsync(txtContent ?? string.Empty, delimiter, conflictResolution, cancellationToken, progress);
    }

    public string GetSampleTemplate(string format) => format?.Trim().ToLowerInvariant() switch
    {
        "json" => JsonSerializer.Serialize(new[]
        {
            new ConversationPhraseExportModel
            {
                Category = "Business & TOEIC",
                ContextTag = "Client Meeting",
                CefrLevel = CefrLevel.B2,
                Register = PhraseRegister.Formal,
                ExamTarget = ExamTarget.Toeic,
                PromptSentence = "Could we walk through the revised timeline together?",
                PromptTranslation = "Chúng ta có thể xem qua lộ trình đã điều chỉnh cùng nhau không?",
                SampleResponse = "Certainly. Let me pull up the milestone breakdown for Q3.",
                ResponseTranslation = "Chắc chắn rồi. Để tôi mở bảng phân rã mốc quý 3.",
                KeyPhrases = "walk through; revised timeline; pull up",
                FormalVariation = "Would you like us to review the updated schedule together?",
                CasualVariation = "Sure, let's check out the updated schedule together.",
                CommonMistakesVi = "Tránh dùng 'discuss about the timeline' (thừa 'about')."
            }
        }, PhraseJsonOptions),
        "csv" => "Category,ContextTag,CefrLevel,Register,ExamTarget,PromptSentence,PromptTranslation,SampleResponse,ResponseTranslation,KeyPhrases\nBusiness & TOEIC,Client Meeting,B2,Formal,Toeic,\"Could we walk through the timeline?\",\"Chúng ta xem qua lộ trình nhé?\",\"Certainly, let's review it now.\",\"Chắc chắn rồi, hãy xem ngay bây giờ.\",\"walk through; timeline\"",
        _ => "Category\tContextTag\tCefrLevel\tRegister\tExamTarget\tPromptSentence\tPromptTranslation\tSampleResponse\tResponseTranslation\tKeyPhrases\nBusiness & TOEIC\tClient Meeting\tB2\tFormal\tToeic\tCould we walk through the timeline?\tChúng ta xem qua lộ trình nhé?\tCertainly, let's review it now.\tChắc chắn rồi, hãy xem ngay bây giờ.\twalk through; timeline"
    };

    private async Task<ImportResult> ImportDelimitedAsync(
        string rawContent,
        char delimiter,
        ImportConflictResolution conflictResolution,
        CancellationToken cancellationToken,
        IProgress<ProgressReport>? progress)
    {
        if (ValidateImportPayload(rawContent) is { } invalid)
        {
            return invalid;
        }

        var lines = rawContent.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var models = new List<ConversationPhraseExportModel>();
        var errors = new List<string>();

        int startIdx = lines.Length > 0 &&
            (lines[0].Contains("PromptSentence", StringComparison.OrdinalIgnoreCase) ||
             lines[0].Contains("Category", StringComparison.OrdinalIgnoreCase)) ? 1 : 0;

        for (int i = startIdx; i < lines.Length; i++)
        {
            var cols = SmartImportParser.ParseCsvLine(lines[i], delimiter);
            if (cols.Count < 4)
            {
                errors.Add($"Line {i + 1}: expected at least Category, PromptSentence, SampleResponse fields.");
                continue;
            }

            if (cols.Count >= 9)
            {
                _ = Enum.TryParse<CefrLevel>(cols[2], true, out var level);
                _ = Enum.TryParse<PhraseRegister>(cols[3], true, out var register);
                ExamTarget? exam = Enum.TryParse<ExamTarget>(cols[4], true, out var parsedExam) ? parsedExam : null;

                models.Add(new ConversationPhraseExportModel
                {
                    Category = cols[0],
                    ContextTag = cols[1],
                    CefrLevel = level == CefrLevel.None ? CefrLevel.B1 : level,
                    Register = register,
                    ExamTarget = exam,
                    PromptSentence = cols[5],
                    PromptTranslation = cols[6],
                    SampleResponse = cols[7],
                    ResponseTranslation = cols[8],
                    KeyPhrases = cols.Count > 9 ? cols[9] : null,
                    FormalVariation = cols.Count > 10 ? cols[10] : null,
                    CasualVariation = cols.Count > 11 ? cols[11] : null,
                    CommonMistakesVi = cols.Count > 12 ? cols[12] : null,
                    AdditionalSuggestions = cols.Count > 13 ? cols[13] : null,
                    IsFavorite = cols.Count > 14 && bool.TryParse(cols[14], out var fav) && fav
                });
            }
            else
            {
                models.Add(new ConversationPhraseExportModel
                {
                    Category = cols[0],
                    PromptSentence = cols[1],
                    PromptTranslation = cols[2],
                    SampleResponse = cols[3],
                    ResponseTranslation = cols.Count > 4 ? cols[4] : string.Empty
                });
            }
        }

        var result = await CommitImportedModelsAsync(models, conflictResolution, cancellationToken, progress);
        errors.AddRange(result.Errors);
        return result with { Errors = errors };
    }

    private async Task<ImportResult> CommitImportedModelsAsync(
        List<ConversationPhraseExportModel> models,
        ImportConflictResolution conflictResolution,
        CancellationToken cancellationToken,
        IProgress<ProgressReport>? progress)
    {
        var existingAll = await repository.GetAllAsync(cancellationToken);
        var byNormalizedPrompt = existingAll
            .Where(p => !string.IsNullOrWhiteSpace(p.NormalizedPrompt))
            .GroupBy(p => p.NormalizedPrompt, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int imported = 0, updated = 0, skipped = 0;
        var errors = new List<string>();
        var now = DateTime.UtcNow;

        for (int idx = 0; idx < models.Count; idx++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var m = models[idx];
            var prompt = TrimOrEmpty(m.PromptSentence);
            var response = TrimOrEmpty(m.SampleResponse);

            if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(response))
            {
                errors.Add($"Item {idx + 1}: missing PromptSentence or SampleResponse.");
                continue;
            }

            var normalizedKey = prompt.ToLowerInvariant();
            if (!seenInBatch.Add(normalizedKey))
            {
                skipped++;
                continue;
            }

            if (byNormalizedPrompt.TryGetValue(normalizedKey, out var existing))
            {
                if (conflictResolution == ImportConflictResolution.Skip)
                {
                    skipped++;
                    continue;
                }

                ApplyModelToEntity(existing, m, prompt, response);
                existing.UpdatedAt = now;
                await repository.UpdateAsync(existing, cancellationToken);
                updated++;
                continue;
            }

            var entity = new ConversationPhrase { Status = LearningStatus.New, EaseFactor = 2.5 };
            ApplyModelToEntity(entity, m, prompt, response);
            byNormalizedPrompt[normalizedKey] = entity;
            await repository.AddAsync(entity, cancellationToken);
            imported++;
        }

        if (imported > 0 || updated > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        progress?.Report(new ProgressReport(100, models.Count, models.Count, "Import complete", "Import complete"));
        return new ImportResult(imported, skipped, errors, updated);
    }

    private async Task<IReadOnlyList<ConversationPhrase>> GetPhrasesForExportAsync(
        IEnumerable<Guid>? phraseIds,
        CancellationToken cancellationToken)
    {
        var all = await repository.GetAllAsync(cancellationToken);
        if (phraseIds is null)
        {
            return all;
        }

        var idSet = phraseIds.ToHashSet();
        return idSet.Count == 0 ? all : all.Where(p => idSet.Contains(p.Id)).ToList();
    }

    private static ImportResult? ValidateImportPayload(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new ImportResult(0, 0, ["Import content cannot be empty."], 0);
        }

        if (content.Length > MaxImportPayloadLength || Encoding.UTF8.GetByteCount(content) > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, ["Payload exceeds maximum allowed size of 20 MB."], 0);
        }

        return null;
    }

    private static void ApplyModelToEntity(
        ConversationPhrase entity,
        ConversationPhraseExportModel model,
        string trimmedPrompt,
        string trimmedResponse)
    {
        entity.Category = NormalizeCategory(model.Category);
        entity.ContextTag = TrimOrEmpty(model.ContextTag);
        entity.PromptSentence = trimmedPrompt;
        entity.PromptTranslation = TrimOrEmpty(model.PromptTranslation);
        entity.SampleResponse = trimmedResponse;
        entity.ResponseTranslation = TrimOrEmpty(model.ResponseTranslation);
        entity.AdditionalSuggestions = TrimOrEmpty(model.AdditionalSuggestions);
        entity.CefrLevel = model.CefrLevel == CefrLevel.None ? CefrLevel.B1 : model.CefrLevel;
        entity.Register = model.Register;
        entity.ExamTarget = model.ExamTarget;
        entity.KeyPhrases = TrimOrEmpty(model.KeyPhrases);
        entity.FormalVariation = TrimOrEmpty(model.FormalVariation);
        entity.CasualVariation = TrimOrEmpty(model.CasualVariation);
        entity.CommonMistakesVi = TrimOrEmpty(model.CommonMistakesVi);
        entity.IsFavorite = model.IsFavorite || entity.IsFavorite;
        entity.UpdateNormalizedFields();
    }

    private static ConversationPhraseExportModel ToExportModel(ConversationPhrase p) => new()
    {
        Category = p.Category,
        ContextTag = p.ContextTag,
        CefrLevel = p.CefrLevel,
        Register = p.Register,
        ExamTarget = p.ExamTarget,
        PromptSentence = p.PromptSentence,
        PromptTranslation = p.PromptTranslation,
        SampleResponse = p.SampleResponse,
        ResponseTranslation = p.ResponseTranslation,
        KeyPhrases = p.KeyPhrases,
        FormalVariation = p.FormalVariation,
        CasualVariation = p.CasualVariation,
        CommonMistakesVi = p.CommonMistakesVi,
        AdditionalSuggestions = p.AdditionalSuggestions,
        IsFavorite = p.IsFavorite
    };

    private static string EscapeCsv(string? value)
        => Lexora.Application.Common.SafeCsvHelper.EscapeCsvField(value, alwaysQuote: true);

    private static string SanitizeTabField(string? value)
        => Lexora.Application.Common.SafeCsvHelper.SanitizeTsvField(value);
}
