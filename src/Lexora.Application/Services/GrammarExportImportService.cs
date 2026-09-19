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

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return new ImportResult(0, 0, ["CSV content has no data rows."]);

        progress?.Report(new ProgressReport(5, 0, lines.Length - 1, "Đang phân tích CSV...", "Parsing header..."));

        // Parse header
        var header = ParseCsvLine(lines[0]);
        int idxTitle = Array.FindIndex(header, h => h.Equals("Title", StringComparison.OrdinalIgnoreCase));
        int idxStructure = Array.FindIndex(header, h => h.Equals("Structure", StringComparison.OrdinalIgnoreCase));
        int idxExplVi = Array.FindIndex(header, h => h.Equals("ExplanationVi", StringComparison.OrdinalIgnoreCase));
        int idxExplEn = Array.FindIndex(header, h => h.Equals("ExplanationEn", StringComparison.OrdinalIgnoreCase));
        int idxMistakes = Array.FindIndex(header, h => h.Equals("CommonMistakes", StringComparison.OrdinalIgnoreCase));
        int idxLevel = Array.FindIndex(header, h => h.Equals("Level", StringComparison.OrdinalIgnoreCase));
        int idxExam = Array.FindIndex(header, h => h.Equals("ExamTarget", StringComparison.OrdinalIgnoreCase));
        int idxTopic = Array.FindIndex(header, h => h.Equals("Topic", StringComparison.OrdinalIgnoreCase));
        int idxExamplesEn = Array.FindIndex(header, h => h.Equals("ExamplesEn", StringComparison.OrdinalIgnoreCase));
        int idxExamplesVi = Array.FindIndex(header, h => h.Equals("ExamplesVi", StringComparison.OrdinalIgnoreCase));

        if (idxTitle < 0)
            return new ImportResult(0, 0, ["CSV missing required 'Title' column."]);

        var existing = await _grammarRepo.GetAllAsync(cancellationToken);
        var existingTitles = existing
            .Select(r => r.Title.Trim().ToLowerInvariant())
            .ToHashSet();

        int dataLines = lines.Length - 1;
        var newRules = new List<GrammarRule>();

        for (int i = 1; i < lines.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int pct = (int)(10.0 + 80.0 * (i - 1) / Math.Max(1, dataLines));
            var cols = ParseCsvLine(lines[i]);

            string title = idxTitle < cols.Length ? cols[idxTitle].Trim() : string.Empty;
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

            string structure = idxStructure >= 0 && idxStructure < cols.Length ? cols[idxStructure].Trim() : string.Empty;
            string explVi = idxExplVi >= 0 && idxExplVi < cols.Length ? cols[idxExplVi].Trim() : string.Empty;
            string explEn = idxExplEn >= 0 && idxExplEn < cols.Length ? cols[idxExplEn].Trim() : string.Empty;
            string mistakes = idxMistakes >= 0 && idxMistakes < cols.Length ? cols[idxMistakes].Trim() : string.Empty;
            string levelStr = idxLevel >= 0 && idxLevel < cols.Length ? cols[idxLevel].Trim() : string.Empty;
            string examStr = idxExam >= 0 && idxExam < cols.Length ? cols[idxExam].Trim() : string.Empty;
            string topic = idxTopic >= 0 && idxTopic < cols.Length ? cols[idxTopic].Trim() : string.Empty;
            string examplesEn = idxExamplesEn >= 0 && idxExamplesEn < cols.Length ? cols[idxExamplesEn].Trim() : string.Empty;
            string examplesVi = idxExamplesVi >= 0 && idxExamplesVi < cols.Length ? cols[idxExamplesVi].Trim() : string.Empty;

            if (!Enum.TryParse<CefrLevel>(levelStr, ignoreCase: true, out var level))
                level = CefrLevel.B1;
            if (!Enum.TryParse<ExamType>(examStr, ignoreCase: true, out var examTarget))
                examTarget = ExamType.General;

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

    private static string CsvQuote(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());
        return [.. result];
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
