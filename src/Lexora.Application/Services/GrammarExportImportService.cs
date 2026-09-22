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

    // ─────────────────────────────────────────────────────────────────────────
    // EXPORT
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string> ExportToJsonAsync(
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu ngữ pháp...", "Querying database..."));
        var rules = await _grammarRepo.GetAllAsync(cancellationToken);

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
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu ngữ pháp...", "Querying database..."));
        var rules = await _grammarRepo.GetAllAsync(cancellationToken);

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

    // ─────────────────────────────────────────────────────────────────────────
    // IMPORT
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ImportResult> ImportFromJsonAsync(
        string jsonContent,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0, skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jsonContent))
            return new ImportResult(0, 0, ["JSON content is empty."]);

        if (jsonContent.Length > MaxImportPayloadLength)
            return new ImportResult(0, 0, [$"Payload exceeds maximum allowed size of {MaxImportPayloadLength / 1_048_576} MB."]);

        List<GrammarRuleExportModel>? list;
        try
        {
            list = JsonSerializer.Deserialize<List<GrammarRuleExportModel>>(jsonContent, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new ImportResult(0, 0, [$"Invalid JSON: {ex.Message}"]);
        }

        if (list == null || list.Count == 0)
            return new ImportResult(0, 0, ["JSON data is empty or invalid."]);

        progress?.Report(new ProgressReport(5, 0, list.Count, "Đang phân tích dữ liệu JSON...", "Parsing..."));

        // Load existing titles for deduplication
        var existing = await _grammarRepo.GetAllAsync(cancellationToken);
        var existingTitles = existing
            .Select(r => r.Title.Trim().ToLowerInvariant())
            .ToHashSet();

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
            if (existingTitles.Contains(titleKey))
            {
                skipped++;
                progress?.Report(new ProgressReport(pct, i + 1, total, "Đang nhập ngữ pháp...", $"Skipping duplicate: {model.Title}"));
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
            existingTitles.Add(titleKey);
            imported++;

            progress?.Report(new ProgressReport(pct, i + 1, total, "Đang nhập ngữ pháp...", $"Processed: {model.Title}"));
        }

        if (newRules.Count > 0)
        {
            progress?.Report(new ProgressReport(92, imported, total, "Đang lưu vào cơ sở dữ liệu...", "Committing transaction..."));
            await _grammarRepo.AddRangeAsync(newRules, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        progress?.Report(new ProgressReport(100, imported, total, "Nhập ngữ pháp hoàn tất!", "Complete."));
        return new ImportResult(imported, skipped, errors);
    }

    public async Task<ImportResult> ImportFromCsvAsync(
        string csvContent,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0, skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(csvContent))
            return new ImportResult(0, 0, ["CSV content is empty."]);

        if (csvContent.Length > MaxImportPayloadLength)
            return new ImportResult(0, 0, [$"Payload exceeds maximum allowed size of {MaxImportPayloadLength / 1_048_576} MB."]);

        var records = ParseCsvRecords(csvContent);
        if (records.Count < 2)
            return new ImportResult(0, 0, ["CSV content has no data rows."]);

        progress?.Report(new ProgressReport(5, 0, records.Count - 1, "Đang phân tích CSV...", "Parsing header..."));

        // Parse header
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
            return new ImportResult(0, 0, ["CSV missing required 'Title' column."]);

        var existing = await _grammarRepo.GetAllAsync(cancellationToken);
        var existingTitles = existing
            .Select(r => r.Title.Trim().ToLowerInvariant())
            .ToHashSet();

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

            var titleKey = title.ToLowerInvariant();
            if (existingTitles.Contains(titleKey))
            {
                skipped++;
                progress?.Report(new ProgressReport(pct, i, dataLines, "Đang nhập ngữ pháp...", $"Skipping duplicate: {title}"));
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

            // Pipe-separated examples: "Example 1 | Example 2"
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
            existingTitles.Add(titleKey);
            imported++;

            progress?.Report(new ProgressReport(pct, i, dataLines, "Đang nhập ngữ pháp...", $"Processed: {title}"));
        }

        if (newRules.Count > 0)
        {
            progress?.Report(new ProgressReport(92, imported, dataLines, "Đang lưu vào cơ sở dữ liệu...", "Committing transaction..."));
            await _grammarRepo.AddRangeAsync(newRules, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        progress?.Report(new ProgressReport(100, imported, dataLines, "Nhập CSV hoàn tất!", "Complete."));
        return new ImportResult(imported, skipped, errors);
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

    // ─────────────────────────────────────────────────────────────────────────
    // Internal Export / Import Models
    // ─────────────────────────────────────────────────────────────────────────

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
