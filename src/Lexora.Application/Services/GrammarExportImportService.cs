using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class GrammarExportImportService : IGrammarExportImportService
{
    private readonly IGrammarRepository _grammarRepo;
    private readonly IUnitOfWork _unitOfWork;

    private const int MaxImportPayloadLength = 20_000_000; // 20 MB

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public GrammarExportImportService(IGrammarRepository grammarRepo, IUnitOfWork unitOfWork)
    {
        _grammarRepo = grammarRepo;
        _unitOfWork = unitOfWork;
    }

    private async Task<IReadOnlyList<GrammarRule>> GetRulesForExportAsync(IEnumerable<Guid>? ruleIds, CancellationToken cancellationToken)
    {
        var allRules = await _grammarRepo.GetAllAsync(cancellationToken);
        if (ruleIds != null)
        {
            var idSet = ruleIds.ToHashSet();
            if (idSet.Count > 0)
            {
                return allRules.Where(r => idSet.Contains(r.Id)).ToList();
            }
        }
        return allRules;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EXPORT
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string> ExportToJsonAsync(
        IEnumerable<Guid>? ruleIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu ngữ pháp...", "Querying database..."));
        var rules = await GetRulesForExportAsync(ruleIds, cancellationToken);

        progress?.Report(new ProgressReport(55, rules.Count, rules.Count, "Đang định dạng JSON...", "Serializing models..."));
        var exportData = rules.Select(r => new GrammarRuleExportModel
        {
            Title = r.Title,
            Structure = r.Structure,
            ExplanationVi = r.ExplanationVi,
            ExplanationEn = r.ExplanationEn,
            CommonMistakes = r.CommonMistakes,
            Level = r.Level.ToString(),
            ExamTarget = r.ExamTarget.ToString(),
            Topic = r.Topic,
            Examples = r.Examples.Select(e => new GrammarExampleExportModel
            {
                SentenceEn = e.SentenceEn,
                SentenceVi = e.SentenceVi,
                Note = e.Note
            }).ToList()
        }).ToList();

        progress?.Report(new ProgressReport(85, rules.Count, rules.Count, "Đang tạo tệp xuất...", "Writing JSON buffer..."));
        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        progress?.Report(new ProgressReport(100, rules.Count, rules.Count, "Xuất ngữ pháp hoàn tất!", "Done."));
        return json;
    }

    public async Task<string> ExportToCsvAsync(
        IEnumerable<Guid>? ruleIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu ngữ pháp...", "Querying database..."));
        var rules = await GetRulesForExportAsync(ruleIds, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Title,Structure,ExplanationVi,ExplanationEn,CommonMistakes,Level,ExamTarget,Topic,ExamplesEn,ExamplesVi");

        int current = 0;
        int total = rules.Count;
        foreach (var r in rules)
        {
            current++;
            int pct = (int)(10.0 + 80.0 * current / Math.Max(1, total));
            progress?.Report(new ProgressReport(pct, current, total, "Đang xuất quy tắc ngữ pháp...", $"Rule {current}/{total}: {r.Title}"));

            var examplesEn = string.Join(" | ", r.Examples.Select(e => e.SentenceEn.Replace("\"", "\"\"")));
            var examplesVi = string.Join(" | ", r.Examples.Select(e => e.SentenceVi.Replace("\"", "\"\"")));

            sb.AppendLine(string.Join(",", new[]
            {
                CsvQuote(r.Title),
                CsvQuote(r.Structure),
                CsvQuote(r.ExplanationVi),
                CsvQuote(r.ExplanationEn ?? string.Empty),
                CsvQuote(r.CommonMistakes ?? string.Empty),
                CsvQuote(r.Level.ToString()),
                CsvQuote(r.ExamTarget.ToString()),
                CsvQuote(r.Topic ?? string.Empty),
                CsvQuote(examplesEn),
                CsvQuote(examplesVi)
            }));
        }

        progress?.Report(new ProgressReport(100, total, total, "Xuất CSV hoàn tất!", "Done."));
        return sb.ToString();
    }

    public async Task<string> ExportToTxtAsync(
        IEnumerable<Guid>? ruleIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu ngữ pháp...", "Querying database..."));
        var rules = await GetRulesForExportAsync(ruleIds, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("# Lexora Grammar Export");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("# Note: Compatible with Markdown readers and text notes.");
        sb.AppendLine();

        int current = 0;
        int total = rules.Count;
        foreach (var r in rules)
        {
            current++;
            int pct = (int)(10.0 + 80.0 * current / Math.Max(1, total));
            progress?.Report(new ProgressReport(pct, current, total, "Đang xuất định dạng văn bản...", r.Title));

            sb.AppendLine("================================================================================");
            sb.AppendLine($"# {r.Title}");
            sb.AppendLine($"Level: {r.Level} | Target: {r.ExamTarget} | Topic: {r.Topic ?? "General"}");
            sb.AppendLine($"Structure: {r.Structure}");
            sb.AppendLine($"ExplanationVi: {r.ExplanationVi}");
            if (!string.IsNullOrWhiteSpace(r.ExplanationEn))
            {
                sb.AppendLine($"ExplanationEn: {r.ExplanationEn}");
            }
            if (!string.IsNullOrWhiteSpace(r.CommonMistakes))
            {
                sb.AppendLine($"CommonMistakes: {r.CommonMistakes}");
            }

            if (r.Examples.Count > 0)
            {
                sb.AppendLine("Examples:");
                foreach (var ex in r.Examples)
                {
                    sb.AppendLine($"- {ex.SentenceEn} => {ex.SentenceVi}");
                }
            }
            sb.AppendLine();
        }

        progress?.Report(new ProgressReport(100, total, total, "Xuất TXT hoàn tất!", "Done."));
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IMPORT
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ImportResult> ImportSmartAsync(
        string content,
        string? fileNameHint = null,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
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
        int imported = 0, updated = 0, skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jsonContent))
            return new ImportResult(0, 0, ["JSON content is empty."], 0);

        if (jsonContent.Length > MaxImportPayloadLength)
            return new ImportResult(0, 0, [$"Payload exceeds maximum allowed size of {MaxImportPayloadLength / 1_048_576} MB."], 0);

        var trimmedJson = jsonContent.Trim();
        if (trimmedJson.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = trimmedJson.IndexOf('\n');
            if (firstNl >= 0)
            {
                trimmedJson = trimmedJson.Substring(firstNl + 1);
            }
            var lastFence = trimmedJson.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
            {
                trimmedJson = trimmedJson.Substring(0, lastFence);
            }
            trimmedJson = trimmedJson.Trim();
        }

        List<GrammarRuleExportModel>? list;
        try
        {
            list = JsonSerializer.Deserialize<List<GrammarRuleExportModel>>(trimmedJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new ImportResult(0, 0, [$"Invalid JSON: {ex.Message}"], 0);
        }

        if (list == null || list.Count == 0)
            return new ImportResult(0, 0, ["JSON data is empty or invalid."], 0);

        progress?.Report(new ProgressReport(5, 0, list.Count, "Đang phân tích dữ liệu JSON...", "Parsing..."));

        var existing = await _grammarRepo.GetAllAsync(cancellationToken);
        var existingDict = existing
            .GroupBy(r => r.Title.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        int total = list.Count;
        var newRules = new List<GrammarRule>();

        for (int i = 0; i < list.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int pct = (int)(10.0 + 80.0 * i / Math.Max(1, total));
            var model = list[i];

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                skipped++;
                errors.Add($"Row {i + 1}: Title is required — skipped.");
                continue;
            }

            var titleKey = model.Title.Trim().ToLowerInvariant();
            if (existingDict.TryGetValue(titleKey, out var existingRule))
            {
                if (conflictResolution == ImportConflictResolution.Overwrite)
                {
                    if (!string.IsNullOrWhiteSpace(model.Structure)) existingRule.Structure = model.Structure.Trim();
                    if (!string.IsNullOrWhiteSpace(model.ExplanationVi)) existingRule.ExplanationVi = model.ExplanationVi.Trim();
                    if (!string.IsNullOrWhiteSpace(model.ExplanationEn)) existingRule.ExplanationEn = model.ExplanationEn.Trim();
                    if (!string.IsNullOrWhiteSpace(model.CommonMistakes)) existingRule.CommonMistakes = model.CommonMistakes.Trim();
                    if (!string.IsNullOrWhiteSpace(model.Topic)) existingRule.Topic = model.Topic.Trim();
                    if (Enum.TryParse<CefrLevel>(model.Level, ignoreCase: true, out var lvl)) existingRule.Level = lvl;
                    if (Enum.TryParse<ExamType>(model.ExamTarget, ignoreCase: true, out var examTgt)) existingRule.ExamTarget = examTgt;

                    if (model.Examples != null)
                    {
                        foreach (var ex in model.Examples)
                        {
                            if (!string.IsNullOrWhiteSpace(ex.SentenceEn) &&
                                !existingRule.Examples.Any(e => e.SentenceEn.Trim().Equals(ex.SentenceEn.Trim(), StringComparison.OrdinalIgnoreCase)))
                            {
                                existingRule.Examples.Add(new GrammarExample
                                {
                                    GrammarRuleId = existingRule.Id,
                                    SentenceEn = ex.SentenceEn.Trim(),
                                    SentenceVi = (ex.SentenceVi ?? string.Empty).Trim(),
                                    Note = ex.Note?.Trim()
                                });
                            }
                        }
                    }

                    await _grammarRepo.UpdateAsync(existingRule, cancellationToken);
                    updated++;
                }
                else
                {
                    skipped++;
                }
                continue;
            }

            if (!Enum.TryParse<CefrLevel>(model.Level, ignoreCase: true, out var level))
                level = CefrLevel.B1;
            if (!Enum.TryParse<ExamType>(model.ExamTarget, ignoreCase: true, out var examTarget))
                examTarget = ExamType.General;

            var rule = new GrammarRule
            {
                Title = model.Title.Trim(),
                Structure = (model.Structure ?? string.Empty).Trim(),
                ExplanationVi = (model.ExplanationVi ?? string.Empty).Trim(),
                ExplanationEn = model.ExplanationEn?.Trim(),
                CommonMistakes = model.CommonMistakes?.Trim(),
                Level = level,
                ExamTarget = examTarget,
                Topic = model.Topic?.Trim()
            };

            if (model.Examples != null)
            {
                foreach (var ex in model.Examples)
                {
                    if (!string.IsNullOrWhiteSpace(ex.SentenceEn))
                    {
                        rule.Examples.Add(new GrammarExample
                        {
                            SentenceEn = ex.SentenceEn.Trim(),
                            SentenceVi = (ex.SentenceVi ?? string.Empty).Trim(),
                            Note = ex.Note?.Trim()
                        });
                    }
                }
            }

            newRules.Add(rule);
            existingDict[titleKey] = rule;
            imported++;

            progress?.Report(new ProgressReport(pct, i + 1, total, "Đang nhập ngữ pháp...", $"Processed: {model.Title}"));
        }

        if (newRules.Count > 0)
        {
            progress?.Report(new ProgressReport(92, imported, total, "Đang lưu vào cơ sở dữ liệu...", "Committing transaction..."));
            await _grammarRepo.AddRangeAsync(newRules, cancellationToken);
        }

        if (newRules.Count > 0 || updated > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        progress?.Report(new ProgressReport(100, imported + updated, total, "Nhập ngữ pháp hoàn tất!", "Complete."));
        return new ImportResult(imported, skipped, errors, updated);
    }

    public async Task<ImportResult> ImportFromCsvAsync(
        string csvContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0, updated = 0, skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(csvContent))
            return new ImportResult(0, 0, ["CSV content is empty."], 0);

        if (csvContent.Length > MaxImportPayloadLength)
            return new ImportResult(0, 0, [$"Payload exceeds maximum allowed size of {MaxImportPayloadLength / 1_048_576} MB."], 0);

        var records = ParseCsvRecords(csvContent);
        if (records.Count < 2)
            return new ImportResult(0, 0, ["CSV content has no data rows."], 0);

        progress?.Report(new ProgressReport(5, 0, records.Count - 1, "Đang phân tích CSV...", "Parsing header..."));

        var header = records[0];
        int idxTitle = header.FindIndex(h => h.Equals("Title", StringComparison.OrdinalIgnoreCase));
        int idxStructure = header.FindIndex(h => h.Equals("Structure", StringComparison.OrdinalIgnoreCase));
        int idxExplVi = header.FindIndex(h => h.Equals("ExplanationVi", StringComparison.OrdinalIgnoreCase));
        int idxExplEn = header.FindIndex(h => h.Equals("ExplanationEn", StringComparison.OrdinalIgnoreCase));
        int idxMistakes = header.FindIndex(h => h.Equals("CommonMistakes", StringComparison.OrdinalIgnoreCase));
        int idxLevel = header.FindIndex(h => h.Equals("Level", StringComparison.OrdinalIgnoreCase));
        int idxExam = header.FindIndex(h => h.Equals("ExamTarget", StringComparison.OrdinalIgnoreCase));
        int idxTopic = header.FindIndex(h => h.Equals("Topic", StringComparison.OrdinalIgnoreCase));
        int idxExamplesEn = header.FindIndex(h => h.Equals("ExamplesEn", StringComparison.OrdinalIgnoreCase));
        int idxExamplesVi = header.FindIndex(h => h.Equals("ExamplesVi", StringComparison.OrdinalIgnoreCase));

        if (idxTitle < 0)
            return new ImportResult(0, 0, ["CSV missing required 'Title' column."], 0);

        var existing = await _grammarRepo.GetAllAsync(cancellationToken);
        var existingDict = existing
            .GroupBy(r => r.Title.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        int dataLines = records.Count - 1;
        var newRules = new List<GrammarRule>();

        for (int i = 1; i < records.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int pct = (int)(10.0 + 80.0 * (i - 1) / Math.Max(1, dataLines));
            var cols = records[i];

            string title = idxTitle < cols.Count ? cols[idxTitle].Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                skipped++;
                errors.Add($"Row {i + 1}: Title is empty — skipped.");
                continue;
            }

            string structure = idxStructure >= 0 && idxStructure < cols.Count ? cols[idxStructure].Trim() : string.Empty;
            string explVi = idxExplVi >= 0 && idxExplVi < cols.Count ? cols[idxExplVi].Trim() : string.Empty;
            string explEn = idxExplEn >= 0 && idxExplEn < cols.Count ? cols[idxExplEn].Trim() : string.Empty;
            string mistakes = idxMistakes >= 0 && idxMistakes < cols.Count ? cols[idxMistakes].Trim() : string.Empty;
            string levelStr = idxLevel >= 0 && idxLevel < cols.Count ? cols[idxLevel].Trim() : string.Empty;
            string examStr = idxExam >= 0 && idxExam < cols.Count ? cols[idxExam].Trim() : string.Empty;
            string topic = idxTopic >= 0 && idxTopic < cols.Count ? cols[idxTopic].Trim() : string.Empty;
            string examplesEn = idxExamplesEn >= 0 && idxExamplesEn < cols.Count ? cols[idxExamplesEn].Trim() : string.Empty;
            string examplesVi = idxExamplesVi >= 0 && idxExamplesVi < cols.Count ? cols[idxExamplesVi].Trim() : string.Empty;

            var level = ParseCefrLevel(levelStr, CefrLevel.B1);
            var examTarget = ParseExamType(examStr, ExamType.General);

            var titleKey = title.ToLowerInvariant();
            if (existingDict.TryGetValue(titleKey, out var existingRule))
            {
                if (conflictResolution == ImportConflictResolution.Overwrite)
                {
                    if (!string.IsNullOrWhiteSpace(structure)) existingRule.Structure = structure;
                    if (!string.IsNullOrWhiteSpace(explVi)) existingRule.ExplanationVi = explVi;
                    if (!string.IsNullOrWhiteSpace(explEn)) existingRule.ExplanationEn = explEn;
                    if (!string.IsNullOrWhiteSpace(mistakes)) existingRule.CommonMistakes = mistakes;
                    if (!string.IsNullOrWhiteSpace(topic)) existingRule.Topic = topic;
                    existingRule.Level = level;
                    existingRule.ExamTarget = examTarget;

                    var enPartsExisting = examplesEn.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var viPartsExisting = examplesVi.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < enPartsExisting.Length; j++)
                    {
                        var sentEn = enPartsExisting[j].Trim();
                        if (!string.IsNullOrWhiteSpace(sentEn) &&
                            !existingRule.Examples.Any(e => e.SentenceEn.Trim().Equals(sentEn, StringComparison.OrdinalIgnoreCase)))
                        {
                            existingRule.Examples.Add(new GrammarExample
                            {
                                GrammarRuleId = existingRule.Id,
                                SentenceEn = sentEn,
                                SentenceVi = j < viPartsExisting.Length ? viPartsExisting[j].Trim() : string.Empty
                            });
                        }
                    }

                    await _grammarRepo.UpdateAsync(existingRule, cancellationToken);
                    updated++;
                }
                else
                {
                    skipped++;
                }
                continue;
            }

            var rule = new GrammarRule
            {
                Title = title,
                Structure = structure,
                ExplanationVi = explVi,
                ExplanationEn = string.IsNullOrWhiteSpace(explEn) ? null : explEn,
                CommonMistakes = string.IsNullOrWhiteSpace(mistakes) ? null : mistakes,
                Level = level,
                ExamTarget = examTarget,
                Topic = string.IsNullOrWhiteSpace(topic) ? null : topic
            };

            var enParts = examplesEn.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var viParts = examplesVi.Split('|', StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < enParts.Length; j++)
            {
                var sentEn = enParts[j].Trim();
                if (string.IsNullOrWhiteSpace(sentEn)) continue;
                rule.Examples.Add(new GrammarExample
                {
                    SentenceEn = sentEn,
                    SentenceVi = j < viParts.Length ? viParts[j].Trim() : string.Empty
                });
            }

            newRules.Add(rule);
            existingDict[titleKey] = rule;
            imported++;

            progress?.Report(new ProgressReport(pct, i, dataLines, "Đang nhập ngữ pháp...", $"Processed: {title}"));
        }

        if (newRules.Count > 0)
        {
            progress?.Report(new ProgressReport(92, imported, dataLines, "Đang lưu vào cơ sở dữ liệu...", "Committing transaction..."));
            await _grammarRepo.AddRangeAsync(newRules, cancellationToken);
        }

        if (newRules.Count > 0 || updated > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        progress?.Report(new ProgressReport(100, imported + updated, dataLines, "Nhập CSV hoàn tất!", "Complete."));
        return new ImportResult(imported, skipped, errors, updated);
    }

    public async Task<ImportResult> ImportFromTxtAsync(
        string txtContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0, updated = 0, skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(txtContent))
            return new ImportResult(0, 0, ["TXT content is empty."], 0);

        if (txtContent.Length > MaxImportPayloadLength)
            return new ImportResult(0, 0, [$"Payload exceeds maximum allowed size of {MaxImportPayloadLength / 1_048_576} MB."], 0);

        progress?.Report(new ProgressReport(5, 0, 0, "Đang phân tích định dạng văn bản...", "Parsing blocks..."));

        var existing = await _grammarRepo.GetAllAsync(cancellationToken);
        var existingDict = existing
            .GroupBy(r => r.Title.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var lines = txtContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var blocks = new List<List<string>>();
        var currentBlock = new List<string>();

        foreach (var rawLine in lines)
        {
            var l = rawLine.Trim();
            if (l.StartsWith("===") || l.StartsWith("---"))
            {
                if (currentBlock.Count > 0)
                {
                    blocks.Add(currentBlock);
                    currentBlock = new List<string>();
                }
                continue;
            }

            if (l.StartsWith('#') && !l.StartsWith("##") && !l.StartsWith("# Lexora") && currentBlock.Count > 0)
            {
                blocks.Add(currentBlock);
                currentBlock = new List<string>();
            }

            if (!string.IsNullOrWhiteSpace(l) && !l.StartsWith("# Lexora") && !l.StartsWith("# Generated") && !l.StartsWith("# Note"))
            {
                currentBlock.Add(l);
            }
        }
        if (currentBlock.Count > 0)
        {
            blocks.Add(currentBlock);
        }

        var newRules = new List<GrammarRule>();
        int total = blocks.Count;

        for (int b = 0; b < blocks.Count; b++)
        {
            var block = blocks[b];
            string title = "";
            string structure = "";
            string explanationVi = "";
            string? explanationEn = null;
            string? mistakes = null;
            string? topic = null;
            CefrLevel level = CefrLevel.B1;
            ExamType examTarget = ExamType.General;
            var examples = new List<(string en, string vi)>();

            foreach (var line in block)
            {
                if (line.StartsWith('#'))
                {
                    title = line.TrimStart('#', ' ').Trim();
                }
                else if (line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                {
                    title = line.Substring(6).Trim();
                }
                else if (line.StartsWith("Structure:", StringComparison.OrdinalIgnoreCase))
                {
                    structure = line.Substring(10).Trim();
                }
                else if (line.StartsWith("ExplanationVi:", StringComparison.OrdinalIgnoreCase))
                {
                    explanationVi = line.Substring(14).Trim();
                }
                else if (line.StartsWith("ExplanationEn:", StringComparison.OrdinalIgnoreCase))
                {
                    explanationEn = line.Substring(14).Trim();
                }
                else if (line.StartsWith("CommonMistakes:", StringComparison.OrdinalIgnoreCase))
                {
                    mistakes = line.Substring(15).Trim();
                }
                else if (line.StartsWith("Level:", StringComparison.OrdinalIgnoreCase))
                {
                    var segs = line.Split('|');
                    foreach (var seg in segs)
                    {
                        var parts = seg.Split(':');
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim().ToLowerInvariant();
                            var val = parts[1].Trim();
                            if (key == "level") level = ParseCefrLevel(val, CefrLevel.B1);
                            else if (key == "target") examTarget = ParseExamType(val, ExamType.General);
                            else if (key == "topic") topic = val;
                        }
                    }
                }
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    var exText = line.Substring(2).Trim();
                    var arrowIdx = exText.IndexOf("=>", StringComparison.Ordinal);
                    if (arrowIdx > 0)
                    {
                        var en = exText.Substring(0, arrowIdx).Trim();
                        var vi = exText.Substring(arrowIdx + 2).Trim();
                        examples.Add((en, vi));
                    }
                    else
                    {
                        examples.Add((exText, ""));
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                skipped++;
                continue;
            }

            var titleKey = title.ToLowerInvariant();
            if (existingDict.TryGetValue(titleKey, out var existingRule))
            {
                if (conflictResolution == ImportConflictResolution.Overwrite)
                {
                    if (!string.IsNullOrWhiteSpace(structure)) existingRule.Structure = structure;
                    if (!string.IsNullOrWhiteSpace(explanationVi)) existingRule.ExplanationVi = explanationVi;
                    if (!string.IsNullOrWhiteSpace(explanationEn)) existingRule.ExplanationEn = explanationEn;
                    if (!string.IsNullOrWhiteSpace(mistakes)) existingRule.CommonMistakes = mistakes;
                    if (!string.IsNullOrWhiteSpace(topic)) existingRule.Topic = topic;
                    existingRule.Level = level;
                    existingRule.ExamTarget = examTarget;

                    foreach (var (en, vi) in examples)
                    {
                        if (!existingRule.Examples.Any(e => e.SentenceEn.Trim().Equals(en.Trim(), StringComparison.OrdinalIgnoreCase)))
                        {
                            existingRule.Examples.Add(new GrammarExample
                            {
                                GrammarRuleId = existingRule.Id,
                                SentenceEn = en,
                                SentenceVi = vi
                            });
                        }
                    }

                    await _grammarRepo.UpdateAsync(existingRule, cancellationToken);
                    updated++;
                }
                else
                {
                    skipped++;
                }
                continue;
            }

            var rule = new GrammarRule
            {
                Title = title,
                Structure = structure,
                ExplanationVi = string.IsNullOrWhiteSpace(explanationVi) ? title : explanationVi,
                ExplanationEn = explanationEn,
                CommonMistakes = mistakes,
                Level = level,
                ExamTarget = examTarget,
                Topic = topic
            };

            foreach (var (en, vi) in examples)
            {
                rule.Examples.Add(new GrammarExample
                {
                    SentenceEn = en,
                    SentenceVi = vi
                });
            }

            newRules.Add(rule);
            existingDict[titleKey] = rule;
            imported++;
        }

        if (newRules.Count > 0)
        {
            await _grammarRepo.AddRangeAsync(newRules, cancellationToken);
        }

        if (newRules.Count > 0 || updated > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        progress?.Report(new ProgressReport(100, imported + updated, total, "Nhập TXT hoàn tất!", "Complete."));
        return new ImportResult(imported, skipped, errors, updated);
    }

    public string GetSampleTemplate(string format)
    {
        var clean = format.ToLowerInvariant();
        if (clean.Contains("json"))
        {
            return """
            [
              {
                "title": "Present Perfect Tense",
                "structure": "S + have/has + V3/ed",
                "explanationVi": "Diễn tả hành động xảy ra trong quá khứ kéo dài đến hiện tại hoặc vừa mới xảy ra.",
                "explanationEn": "Used to describe an action that occurred at an indefinite time in the past or began in the past and continues to the present.",
                "commonMistakes": "Không dùng thì hiện tại hoàn thành với mốc thời gian cụ thể trong quá khứ như yesterday, in 1999.",
                "level": "B1",
                "examTarget": "General",
                "topic": "Tenses",
                "examples": [
                  {
                    "sentenceEn": "I have lived in this city for over five years.",
                    "sentenceVi": "Tôi đã sống ở thành phố này hơn năm năm rồi.",
                    "note": "Diễn tả hành động bắt đầu ở quá khứ và vẫn tiếp diễn."
                  },
                  {
                    "sentenceEn": "She has just finished her master's degree.",
                    "sentenceVi": "Cô ấy vừa mới hoàn thành bằng thạc sĩ."
                  }
                ]
              }
            ]
            """;
        }

        if (clean.Contains("csv"))
        {
            return """
            Title,Structure,ExplanationVi,ExplanationEn,CommonMistakes,Level,ExamTarget,Topic,ExamplesEn,ExamplesVi
            "First Conditional","If + S + V(s/es), S + will + V-inf","Diễn tả điều kiện có thể xảy ra ở hiện tại hoặc tương lai","Used to express real conditions in the present or future","Tránh dùng 'will' trong mệnh đề If","A2","General","Conditionals","If it rains tomorrow, we will stay at home | If you study hard, you will pass","Nếu ngày mai trời mưa, chúng ta sẽ ở nhà | Nếu bạn học chăm chỉ, bạn sẽ đỗ"
            """;
        }

        return """
        # Mẫu danh sách quy tắc ngữ pháp Lexora (Markdown / TXT)
        ================================================================================
        # First Conditional (Câu điều kiện loại 1)
        Level: A2 | Target: General | Topic: Conditionals
        Structure: If + S + V(s/es), S + will + V-inf
        ExplanationVi: Diễn tả điều kiện và kết quả có thể xảy ra ở hiện tại hoặc tương lai.
        ExplanationEn: Used to talk about things which might happen in the future.
        CommonMistakes: Không dùng 'will' trong mệnh đề If (e.g. Không dùng 'If it will rain').
        Examples:
        - If it rains tomorrow, we will stay at home. => Nếu ngày mai trời mưa, chúng tôi sẽ ở nhà.
        - If you work hard, you will achieve your dreams. => Nếu bạn làm việc chăm chỉ, bạn sẽ đạt được ước mơ.
        """;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly System.Buffers.SearchValues<char> CsvFormulaTriggers = System.Buffers.SearchValues.Create("=+-@\t\r");

    private static string CsvQuote(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        // Mitigate CSV Formula Injection (CWE-1236)
        bool isFormula = CsvFormulaTriggers.Contains(value[0]);
        var safeField = isFormula ? "'" + value : value;

        if (isFormula || safeField.Contains(',') || safeField.Contains('"') || safeField.Contains('\n') || safeField.Contains('\r'))
        {
            return $"\"{safeField.Replace("\"", "\"\"")}\"";
        }
        return safeField;
    }

    private static CefrLevel ParseCefrLevel(string? input, CefrLevel defaultLevel = CefrLevel.B1)
    {
        if (string.IsNullOrWhiteSpace(input)) return defaultLevel;
        var clean = input.Trim().ToLowerInvariant();
        if (Enum.TryParse<CefrLevel>(clean, ignoreCase: true, out var exact)) return exact;
        return clean switch
        {
            "a1" or "beginner" => CefrLevel.A1,
            "a2" or "elementary" => CefrLevel.A2,
            "b1" or "intermediate" => CefrLevel.B1,
            "b2" or "upper intermediate" or "upper-intermediate" or "upperintermediate" => CefrLevel.B2,
            "c1" or "advanced" => CefrLevel.C1,
            "c2" or "mastery" or "proficiency" => CefrLevel.C2,
            _ => defaultLevel
        };
    }

    private static ExamType ParseExamType(string? input, ExamType defaultTarget = ExamType.General)
    {
        if (string.IsNullOrWhiteSpace(input)) return defaultTarget;
        var clean = input.Trim().ToLowerInvariant();
        if (Enum.TryParse<ExamType>(clean, ignoreCase: true, out var exact)) return exact;
        return clean switch
        {
            "ielts" => ExamType.Ielts,
            "toeic" => ExamType.Toeic,
            "vstep" => ExamType.Vstep,
            _ => defaultTarget
        };
    }

    private static List<List<string>> ParseCsvRecords(string csvContent)
    {
        var records = new List<List<string>>();
        var currentRecord = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csvContent.Length; i++)
        {
            char c = csvContent[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < csvContent.Length && csvContent[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++; // skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                currentRecord.Add(currentField.ToString());
                currentField.Clear();
            }
            else if ((c == '\r' || c == '\n') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csvContent.Length && csvContent[i + 1] == '\n')
                {
                    i++;
                }

                currentRecord.Add(currentField.ToString());
                currentField.Clear();

                if (currentRecord.Any(f => !string.IsNullOrWhiteSpace(f)))
                {
                    records.Add(currentRecord);
                }
                currentRecord = new List<string>();
            }
            else
            {
                currentField.Append(c);
            }
        }

        if (currentField.Length > 0 || currentRecord.Count > 0)
        {
            currentRecord.Add(currentField.ToString());
            if (currentRecord.Any(f => !string.IsNullOrWhiteSpace(f)))
            {
                records.Add(currentRecord);
            }
        }

        return records;
    }

    private class GrammarRuleExportModel
    {
        public string Title { get; set; } = string.Empty;
        public string Structure { get; set; } = string.Empty;
        public string ExplanationVi { get; set; } = string.Empty;
        public string? ExplanationEn { get; set; }
        public string? CommonMistakes { get; set; }
        public string Level { get; set; } = "B1";
        public string ExamTarget { get; set; } = "General";
        public string? Topic { get; set; }
        public List<GrammarExampleExportModel> Examples { get; set; } = [];
    }

    private class GrammarExampleExportModel
    {
        public string SentenceEn { get; set; } = string.Empty;
        public string SentenceVi { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
