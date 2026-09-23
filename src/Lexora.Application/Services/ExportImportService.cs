using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class ExportImportService : IExportImportService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public ExportImportService(IVocabularyRepository vocabRepo, IUnitOfWork unitOfWork)
    {
        _vocabRepo = vocabRepo;
        _unitOfWork = unitOfWork;
    }

    private async Task<IReadOnlyList<Word>> GetWordsForExportAsync(IEnumerable<Guid>? wordIds, CancellationToken cancellationToken)
    {
        if (wordIds != null)
        {
            var idList = wordIds.Distinct().ToList();
            if (idList.Count > 0)
            {
                return await _vocabRepo.GetByIdsWithDetailsAsync(idList, cancellationToken);
            }
        }
        return await _vocabRepo.SearchAsync(take: 100000, cancellationToken: cancellationToken);
    }

    public async Task<string> ExportToJsonAsync(
        IEnumerable<Guid>? wordIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu từ vựng...", "Querying database..."));
        var words = await GetWordsForExportAsync(wordIds, cancellationToken);

        progress?.Report(new ProgressReport(50, words.Count, words.Count, "Đang định dạng cấu trúc JSON...", "Serializing models..."));
        var exportData = words.Select(w =>
        {
            var meanings = w.Meanings ?? [];
            var distinctPos = meanings.Select(m => m.PartOfSpeech).Distinct().ToList();
            if (distinctPos.Count == 0) distinctPos.Add(w.PrimaryPartOfSpeech);

            return new WordExportModel
            {
                Term = w.Term,
                PhoneticUk = w.PhoneticUk,
                PhoneticUs = w.PhoneticUs,
                Level = w.Level,
                PrimaryPartOfSpeech = w.PrimaryPartOfSpeech,
                AllPartsOfSpeech = distinctPos,
                Notes = w.Notes,
                Meanings = meanings.OrderBy(m => m.OrderIndex).Select(m => new MeaningExportModel
                {
                    PartOfSpeech = m.PartOfSpeech,
                    DefinitionVi = m.DefinitionVi,
                    DefinitionEn = m.DefinitionEn,
                    Explanation = m.Explanation,
                    Examples = (m.Examples ?? []).Select(e => new ExampleExportModel
                    {
                        SentenceEn = e.SentenceEn,
                        SentenceVi = e.SentenceVi
                    }).ToList()
                }).ToList(),
                Topics = (w.WordTopics ?? []).Where(wt => wt.Topic != null).Select(wt => wt.Topic.Name).ToList(),
                ExamTags = (w.WordExamTags ?? []).Where(wet => wet.ExamTag != null).Select(wet => wet.ExamTag.Name).ToList()
            };
        }).ToList();

        progress?.Report(new ProgressReport(85, words.Count, words.Count, "Đang tạo dữ liệu xuất...", "Writing JSON buffer..."));
        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        progress?.Report(new ProgressReport(100, words.Count, words.Count, "Xuất dữ liệu hoàn tất!", "Done."));
        return json;
    }

    public async Task<string> ExportToCsvAsync(
        IEnumerable<Guid>? wordIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu từ vựng...", "Querying database..."));
        var words = await GetWordsForExportAsync(wordIds, cancellationToken);
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Term,Level,PartOfSpeech,PhoneticUk,PhoneticUs,MeaningsVi,Examples,Topics,ExamTags,Notes");

        int current = 0;
        int total = words.Count;
        foreach (var w in words)
        {
            current++;
            if (current % 25 == 0 || current == total)
            {
                int pct = (int)((double)current / total * 75) + 20;
                progress?.Report(new ProgressReport(pct, current, total, $"Đang định dạng CSV ({current}/{total})...", w.Term));
            }

            var meanings = w.Meanings ?? [];
            var distinctPos = meanings.Select(m => m.PartOfSpeech).Distinct().ToList();
            var posDisplay = distinctPos.Count > 1
                ? string.Join("; ", distinctPos.Select(p => p.ToString()))
                : w.PrimaryPartOfSpeech.ToString();

            var meaningsJoined = string.Join(" | ", meanings.Select(m =>
            {
                var text = $"[{m.PartOfSpeech}] {m.DefinitionVi}";
                if (!string.IsNullOrWhiteSpace(m.DefinitionEn))
                {
                    text += $" = {m.DefinitionEn}";
                }
                if (!string.IsNullOrWhiteSpace(m.Explanation))
                {
                    text += $" ({m.Explanation})";
                }
                return text;
            }));

            var examplesJoined = string.Join(" | ", meanings.SelectMany(m => m.Examples ?? []).Select(e => $"{e.SentenceEn} => {e.SentenceVi}"));
            var topicsJoined = string.Join(";", (w.WordTopics ?? []).Where(wt => wt.Topic != null).Select(wt => wt.Topic.Name));
            var examTagsJoined = string.Join(";", (w.WordExamTags ?? []).Where(wet => wet.ExamTag != null).Select(wet => wet.ExamTag.Name));

            sb.AppendLine(string.Join(",",
                EscapeCsv(w.Term),
                w.Level.ToString(),
                posDisplay,
                EscapeCsv(w.PhoneticUk ?? ""),
                EscapeCsv(w.PhoneticUs ?? ""),
                EscapeCsv(meaningsJoined),
                EscapeCsv(examplesJoined),
                EscapeCsv(topicsJoined),
                EscapeCsv(examTagsJoined),
                EscapeCsv(w.Notes ?? "")
            ));
        }

        progress?.Report(new ProgressReport(100, total, total, "Xuất CSV hoàn tất!", "Done."));
        return sb.ToString();
    }

    public async Task<string> ExportToTxtAsync(
        IEnumerable<Guid>? wordIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu từ vựng...", "Querying database..."));
        var words = await GetWordsForExportAsync(wordIds, cancellationToken);
        var sb = new StringBuilder();

        // Comments header
        sb.AppendLine("# Lexora Vocabulary Export");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("# Format: Term <tab> PrimaryDefinition <tab> PartOfSpeech <tab> Level <tab> PhoneticUk <tab> PhoneticUs <tab> Examples");
        sb.AppendLine("# Note: Compatible with Lexora, Anki (TSV), and Quizlet.");
        sb.AppendLine();

        int current = 0;
        int total = words.Count;
        foreach (var w in words)
        {
            current++;
            if (current % 25 == 0 || current == total)
            {
                int pct = (int)((double)current / total * 75) + 20;
                progress?.Report(new ProgressReport(pct, current, total, $"Đang định dạng văn bản ({current}/{total})...", w.Term));
            }

            var meanings = w.Meanings ?? [];
            var primaryDef = meanings.OrderBy(m => m.OrderIndex).FirstOrDefault()?.DefinitionVi ?? w.Term;
            var distinctPos = meanings.Select(m => m.PartOfSpeech).Distinct().ToList();
            var posDisplay = distinctPos.Count > 1
                ? string.Join(" / ", distinctPos.Select(p => p.ToString()))
                : w.PrimaryPartOfSpeech.ToString();

            var level = w.Level != CefrLevel.None ? w.Level.ToString() : "";
            var uk = w.PhoneticUk ?? "";
            var us = w.PhoneticUs ?? "";
            var examples = string.Join(" | ", meanings.SelectMany(m => m.Examples ?? []).Select(e => $"{e.SentenceEn} => {e.SentenceVi}"));

            sb.AppendLine($"{w.Term}\t{primaryDef}\t{posDisplay}\t{level}\t{uk}\t{us}\t{examples}");
        }

        progress?.Report(new ProgressReport(100, total, total, "Xuất TXT hoàn tất!", "Done."));
        return sb.ToString();
    }

    private const int MaxImportPayloadLength = 20_000_000; // 20 MB safety limit

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

    public async Task<SmartImportAnalysisResultDto> AnalyzeSmartImportAsync(
        string content,
        string? fileNameHint = null,
        IReadOnlyList<SmartImportColumnMappingDto>? columnMappings = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SmartImportAnalysisResultDto(
                DataFormat.Unknown,
                Array.Empty<string>(),
                Array.Empty<SmartImportColumnMappingDto>(),
                Array.Empty<SmartImportItemDto>(),
                0, 0, 0, 0);
        }

        if (content.Length > MaxImportPayloadLength)
        {
            throw new ArgumentException($"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB");
        }

        var format = SmartContentDetector.DetectFormat(content, fileNameHint);
        var previewItems = new List<SmartImportItemDto>();
        var headers = new List<string>();
        var mappings = new List<SmartImportColumnMappingDto>();

        // Cache existing terms for fast duplicate detection
        var existingList = await _vocabRepo.SearchAsync(take: 100000, cancellationToken: cancellationToken)
            ?? (IReadOnlyList<Word>)Array.Empty<Word>();
        var existingTerms = existingList
            .Select(w => !string.IsNullOrEmpty(w.NormalizedTerm) ? w.NormalizedTerm : w.Term.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (format == DataFormat.Json)
        {
            headers.AddRange(["term", "level", "primaryPartOfSpeech", "meanings", "phoneticUk", "phoneticUs", "topics", "examTags", "notes"]);
            mappings.AddRange([
                new("term", SmartImportField.Term),
                new("level", SmartImportField.Level),
                new("primaryPartOfSpeech", SmartImportField.PartOfSpeech),
                new("meanings", SmartImportField.DefinitionVi),
                new("phoneticUk", SmartImportField.PhoneticUk),
                new("phoneticUs", SmartImportField.PhoneticUs),
                new("topics", SmartImportField.Topics),
                new("examTags", SmartImportField.ExamTags),
                new("notes", SmartImportField.Notes)
            ]);

            try
            {
                var jsonWords = JsonSerializer.Deserialize<List<WordExportModel>>(content, JsonOptions) ?? new();
                int rowIdx = 0;
                foreach (var jw in jsonWords)
                {
                    rowIdx++;
                    bool isTermValid = !string.IsNullOrWhiteSpace(jw.Term);
                    string term = jw.Term?.Trim() ?? string.Empty;
                    string normalized = term.ToLowerInvariant();
                    bool isDuplicate = existingTerms.Contains(normalized);

                    var firstMeaning = jw.Meanings?.FirstOrDefault();
                    string defVi = firstMeaning?.DefinitionVi ?? string.Empty;
                    string? defEn = firstMeaning?.DefinitionEn;
                    var pos = jw.PrimaryPartOfSpeech != 0 ? jw.PrimaryPartOfSpeech : (firstMeaning?.PartOfSpeech != 0 ? firstMeaning!.PartOfSpeech : PartOfSpeech.Noun);

                    var allExamples = jw.Meanings?.SelectMany(m => m.Examples ?? []).Select(ex => ex.SentenceEn).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    string? exText = allExamples != null && allExamples.Count > 0 ? string.Join(" | ", allExamples) : null;
                    string? topicsText = jw.Topics != null && jw.Topics.Count > 0 ? string.Join("; ", jw.Topics) : null;
                    string? examsText = jw.ExamTags != null && jw.ExamTags.Count > 0 ? string.Join("; ", jw.ExamTags) : null;

                    string? validationMsg = null;
                    if (!isTermValid) validationMsg = "Thuật ngữ không được để trống / Term is required.";
                    else if (string.IsNullOrWhiteSpace(defVi)) validationMsg = "Chưa có định nghĩa tiếng Việt / Missing Vietnamese definition.";

                    previewItems.Add(new SmartImportItemDto(
                        RowIndex: rowIdx,
                        Term: term,
                        Level: jw.Level,
                        PartOfSpeech: pos,
                        DefinitionVi: defVi,
                        DefinitionEn: defEn,
                        PhoneticUk: jw.PhoneticUk,
                        PhoneticUs: jw.PhoneticUs,
                        Examples: exText,
                        Topics: topicsText,
                        ExamTags: examsText,
                        Notes: jw.Notes,
                        IsDuplicate: isDuplicate,
                        IsValid: isTermValid && !string.IsNullOrWhiteSpace(defVi),
                        ValidationMessage: validationMsg
                    ));
                }
            }
            catch
            {
                format = DataFormat.Txt;
            }
        }

        if (format == DataFormat.Csv)
        {
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length > 0)
            {
                char sep = lines[0].Contains(';') ? ';' : ',';
                var headerLine = lines[0];
                var rawHeaders = ParseCsvLine(headerLine, sep);
                headers.AddRange(rawHeaders);

                // Auto-detect or use user mappings
                if (columnMappings != null && columnMappings.Count > 0)
                {
                    mappings.AddRange(columnMappings);
                }
                else
                {
                    for (int i = 0; i < rawHeaders.Count; i++)
                    {
                        var h = rawHeaders[i].Trim().ToLowerInvariant();
                        var target = SmartImportField.Ignore;
                        if (h.Contains("term") || h.Contains("word") || h == "từ" || h == "tu") target = SmartImportField.Term;
                        else if (h.Contains("level") || h.Contains("cefr")) target = SmartImportField.Level;
                        else if (h.Contains("part of speech") || h.Contains("partofspeech") || h.Contains("pos") || h.Contains("từ loại") || h.Contains("loại từ") || h.Contains("tu loai")) target = SmartImportField.PartOfSpeech;
                        else if (h.Contains("meaning") || h.Contains("definition") || h.Contains("nghĩa") || h.Contains("dịch") || h == "định nghĩa") target = SmartImportField.DefinitionVi;
                        else if (h.Contains("definitionen") || h.Contains("english")) target = SmartImportField.DefinitionEn;
                        else if (h.Contains("phonetic") || h.Contains("pronunciation") || h.Contains("phát âm") || h.Contains("ipa") || h == "uk") target = SmartImportField.PhoneticUk;
                        else if (h == "us" || h.Contains("phoneticus")) target = SmartImportField.PhoneticUs;
                        else if (h.Contains("example") || h.Contains("ví dụ") || h == "vd") target = SmartImportField.Examples;
                        else if (h.Contains("topic") || h.Contains("chủ đề")) target = SmartImportField.Topics;
                        else if (h.Contains("exam") || h.Contains("tag")) target = SmartImportField.ExamTags;
                        else if (h.Contains("note") || h.Contains("ghi chú")) target = SmartImportField.Notes;

                        mappings.Add(new SmartImportColumnMappingDto(rawHeaders[i], target));
                    }
                }

                // Parse records
                for (int rowIdx = 1; rowIdx < lines.Length; rowIdx++)
                {
                    var line = lines[rowIdx];
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

                    var cols = ParseCsvLine(line, sep);
                    string term = "";
                    CefrLevel level = CefrLevel.None;
                    PartOfSpeech pos = PartOfSpeech.Noun;
                    string defVi = "";
                    string? defEn = null;
                    string? ipaUk = null;
                    string? ipaUs = null;
                    string? examples = null;
                    string? topics = null;
                    string? examTags = null;
                    string? notes = null;

                    for (int c = 0; c < Math.Min(cols.Count, mappings.Count); c++)
                    {
                        var val = cols[c].Trim();
                        switch (mappings[c].TargetField)
                        {
                            case SmartImportField.Term: term = val; break;
                            case SmartImportField.Level: level = ParseCefrLevel(val); break;
                            case SmartImportField.PartOfSpeech: pos = ParsePartOfSpeech(val); break;
                            case SmartImportField.DefinitionVi: defVi = val; break;
                            case SmartImportField.DefinitionEn: defEn = string.IsNullOrWhiteSpace(val) ? null : val; break;
                            case SmartImportField.PhoneticUk: ipaUk = string.IsNullOrWhiteSpace(val) ? null : val; break;
                            case SmartImportField.PhoneticUs: ipaUs = string.IsNullOrWhiteSpace(val) ? null : val; break;
                            case SmartImportField.Examples: examples = string.IsNullOrWhiteSpace(val) ? null : val; break;
                            case SmartImportField.Topics: topics = string.IsNullOrWhiteSpace(val) ? null : val; break;
                            case SmartImportField.ExamTags: examTags = string.IsNullOrWhiteSpace(val) ? null : val; break;
                            case SmartImportField.Notes: notes = string.IsNullOrWhiteSpace(val) ? null : val; break;
                        }
                    }

                    bool isTermValid = !string.IsNullOrWhiteSpace(term);
                    string normalized = term.ToLowerInvariant();
                    bool isDuplicate = isTermValid && existingTerms.Contains(normalized);

                    string? validationMsg = null;
                    if (!isTermValid) validationMsg = "Thiếu thuật ngữ / Missing term.";
                    else if (string.IsNullOrWhiteSpace(defVi)) validationMsg = "Thiếu định nghĩa tiếng Việt / Missing Vietnamese definition.";

                    previewItems.Add(new SmartImportItemDto(
                        RowIndex: rowIdx,
                        Term: term,
                        Level: level,
                        PartOfSpeech: pos,
                        DefinitionVi: defVi,
                        DefinitionEn: defEn,
                        PhoneticUk: ipaUk,
                        PhoneticUs: ipaUs,
                        Examples: examples,
                        Topics: topics,
                        ExamTags: examTags,
                        Notes: notes,
                        IsDuplicate: isDuplicate,
                        IsValid: isTermValid && !string.IsNullOrWhiteSpace(defVi),
                        ValidationMessage: validationMsg
                    ));
                }
            }
        }

        if (format == DataFormat.Txt)
        {
            headers.AddRange(["Term", "DefinitionVi", "PartOfSpeech / Level", "Examples"]);
            mappings.AddRange([
                new("Col 1", SmartImportField.Term),
                new("Col 2", SmartImportField.DefinitionVi),
                new("Col 3", SmartImportField.PartOfSpeech),
                new("Col 4", SmartImportField.Examples)
            ]);

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int rowIdx = 0;
            foreach (var rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith('#') || rawLine.StartsWith("//")) continue;
                rowIdx++;

                string term = "";
                string defVi = "";
                PartOfSpeech pos = PartOfSpeech.Noun;
                CefrLevel level = CefrLevel.None;
                string? examples = null;

                if (rawLine.Contains('\t'))
                {
                    var parts = rawLine.Split('\t', StringSplitOptions.TrimEntries);
                    if (parts.Length > 0) term = parts[0];
                    if (parts.Length > 1) defVi = parts[1];
                    if (parts.Length > 2)
                    {
                        pos = ParsePartOfSpeech(parts[2]);
                        level = ParseCefrLevel(parts[2]);
                    }
                    if (parts.Length > 3) examples = parts[3];
                }
                else if (rawLine.Contains(" - "))
                {
                    var parts = rawLine.Split(new[] { " - " }, StringSplitOptions.TrimEntries);
                    if (parts.Length > 0) term = parts[0];
                    if (parts.Length > 1) defVi = parts[1];
                    if (parts.Length > 2) examples = parts[2];
                }
                else if (rawLine.Contains(':'))
                {
                    int colonIdx = rawLine.IndexOf(':');
                    term = rawLine.Substring(0, colonIdx).Trim();
                    defVi = rawLine.Substring(colonIdx + 1).Trim();
                }
                else
                {
                    term = rawLine.Trim();
                }

                // If term contains parenthetical POS e.g. "abundant (adj)", extract it
                if (term.Contains('(') && term.Contains(')'))
                {
                    int start = term.IndexOf('(');
                    int end = term.IndexOf(')', start);
                    if (end > start)
                    {
                        var inside = term.Substring(start + 1, end - start - 1);
                        var detectedPos = ParsePartOfSpeech(inside);
                        if (detectedPos != PartOfSpeech.Noun || inside.Equals("n", StringComparison.OrdinalIgnoreCase))
                        {
                            pos = detectedPos;
                            term = (term.Substring(0, start) + term.Substring(end + 1)).Trim();
                        }
                    }
                }

                bool isTermValid = !string.IsNullOrWhiteSpace(term);
                string normalized = term.ToLowerInvariant();
                bool isDuplicate = isTermValid && existingTerms.Contains(normalized);

                string? validationMsg = null;
                if (!isTermValid) validationMsg = "Thiếu thuật ngữ / Missing term.";
                else if (string.IsNullOrWhiteSpace(defVi)) validationMsg = "Thiếu định nghĩa / Missing definition.";

                previewItems.Add(new SmartImportItemDto(
                    RowIndex: rowIdx,
                    Term: term,
                    Level: level,
                    PartOfSpeech: pos,
                    DefinitionVi: defVi,
                    DefinitionEn: null,
                    PhoneticUk: null,
                    PhoneticUs: null,
                    Examples: examples,
                    Topics: null,
                    ExamTags: null,
                    Notes: null,
                    IsDuplicate: isDuplicate,
                    IsValid: isTermValid && !string.IsNullOrWhiteSpace(defVi),
                    ValidationMessage: validationMsg
                ));
            }
        }

        int total = previewItems.Count;
        int valid = previewItems.Count(p => p.IsValid && !p.IsDuplicate);
        int duplicate = previewItems.Count(p => p.IsDuplicate);
        int invalid = previewItems.Count(p => !p.IsValid);

        return new SmartImportAnalysisResultDto(
            DetectedFormat: format,
            AvailableHeaders: headers,
            ColumnMappings: mappings,
            PreviewItems: previewItems,
            TotalParsed: total,
            ValidCount: valid,
            DuplicateCount: duplicate,
            InvalidCount: invalid
        );
    }

    public async Task<SmartImportCommitResultDto> CommitSmartImportAsync(
        SmartImportCommitRequestDto request,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int updated = 0;
        int skipped = 0;
        var errors = new List<string>();

        var selectedItems = request.Items.Where(i => i.IsSelected && i.IsValid).ToList();
        int total = selectedItems.Count;

        if (total == 0)
        {
            return new SmartImportCommitResultDto(0, 0, 0, new List<string> { "Không có mục hợp lệ nào được chọn / No valid items selected." });
        }

        var rawTopics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
        var allTopics = rawTopics != null ? rawTopics.ToDictionary(t => t.Name.ToLowerInvariant()) : new Dictionary<string, Topic>();

        var rawTags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
        var allExamTags = rawTags != null ? rawTags.ToDictionary(et => et.Name.ToLowerInvariant()) : new Dictionary<string, ExamTag>();

        int current = 0;
        foreach (var item in selectedItems)
        {
            current++;
            if (current % 10 == 0 || current == total)
            {
                int pct = (int)((double)current / total * 80) + 10;
                progress?.Report(new ProgressReport(pct, current, total, $"Đang nhập: {current} / {total}", item.Term));
            }

            var normalized = item.Term.Trim().ToLowerInvariant();
            var existing = await _vocabRepo.GetByTermAsync(normalized, cancellationToken);

            if (existing != null)
            {
                if (request.ConflictResolution == ImportConflictResolution.Overwrite)
                {
                    if (item.Level != CefrLevel.None) existing.Level = item.Level;
                    if (!string.IsNullOrWhiteSpace(item.PhoneticUk)) existing.PhoneticUk = item.PhoneticUk;
                    if (!string.IsNullOrWhiteSpace(item.PhoneticUs)) existing.PhoneticUs = item.PhoneticUs;
                    if (!string.IsNullOrWhiteSpace(item.Notes)) existing.Notes = item.Notes;

                    // Merge meaning if not existing
                    bool hasMeaning = existing.Meanings.Any(m => string.Equals(m.DefinitionVi.Trim(), item.DefinitionVi.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (!hasMeaning && !string.IsNullOrWhiteSpace(item.DefinitionVi))
                    {
                        var newM = new WordMeaning
                        {
                            WordId = existing.Id,
                            PartOfSpeech = item.PartOfSpeech,
                            DefinitionVi = item.DefinitionVi.Trim(),
                            DefinitionEn = item.DefinitionEn?.Trim(),
                            OrderIndex = existing.Meanings.Count
                        };
                        if (!string.IsNullOrWhiteSpace(item.Examples))
                        {
                            var exTokens = item.Examples.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var ex in exTokens)
                            {
                                newM.Examples.Add(new ExampleSentence { WordMeaningId = newM.Id, SentenceEn = ex.Trim() });
                            }
                        }
                        existing.Meanings.Add(newM);
                    }

                    await _vocabRepo.UpdateAsync(existing, cancellationToken);
                    updated++;
                }
                else
                {
                    skipped++;
                }
                continue;
            }

            // New word
            var word = new Word
            {
                Term = item.Term.Trim(),
                NormalizedTerm = normalized,
                PhoneticUk = string.IsNullOrWhiteSpace(item.PhoneticUk) ? null : item.PhoneticUk.Trim(),
                PhoneticUs = string.IsNullOrWhiteSpace(item.PhoneticUs) ? null : item.PhoneticUs.Trim(),
                Level = item.Level,
                PrimaryPartOfSpeech = item.PartOfSpeech,
                Notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var meaning = new WordMeaning
            {
                WordId = word.Id,
                PartOfSpeech = item.PartOfSpeech,
                DefinitionVi = item.DefinitionVi.Trim(),
                DefinitionEn = string.IsNullOrWhiteSpace(item.DefinitionEn) ? null : item.DefinitionEn.Trim(),
                OrderIndex = 0
            };

            if (!string.IsNullOrWhiteSpace(item.Examples))
            {
                var exTokens = item.Examples.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var ex in exTokens)
                {
                    meaning.Examples.Add(new ExampleSentence { WordMeaningId = meaning.Id, SentenceEn = ex.Trim() });
                }
            }
            word.Meanings.Add(meaning);

            // Link topics
            if (!string.IsNullOrWhiteSpace(item.Topics))
            {
                var tTokens = item.Topics.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var tname in tTokens)
                {
                    if (allTopics.TryGetValue(tname.ToLowerInvariant(), out var top))
                    {
                        word.WordTopics.Add(new WordTopic { WordId = word.Id, TopicId = top.Id });
                    }
                }
            }

            // Link exam tags
            if (!string.IsNullOrWhiteSpace(item.ExamTags))
            {
                var etTokens = item.ExamTags.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var etname in etTokens)
                {
                    if (allExamTags.TryGetValue(etname.ToLowerInvariant(), out var et))
                    {
                        word.WordExamTags.Add(new WordExamTag { WordId = word.Id, ExamTagId = et.Id });
                    }
                }
            }

            word.ReviewCard = new ReviewCard
            {
                WordId = word.Id,
                Status = LearningStatus.New,
                NextReviewAt = DateTime.UtcNow
            };

            await _vocabRepo.AddAsync(word, cancellationToken);
            imported++;
        }

        progress?.Report(new ProgressReport(95, total, total, "Đang lưu thay đổi vào cơ sở dữ liệu...", "Saving transaction..."));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        progress?.Report(new ProgressReport(100, total, total, "Hoàn tất nhập dữ liệu!", "Import complete."));

        return new SmartImportCommitResultDto(imported, updated, skipped, errors);
    }

    public async Task<ImportResult> ImportFromJsonAsync(
        string jsonContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int updated = 0;
        int skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new ImportResult(0, 0, new List<string> { "JSON data is empty" }, 0);
        }

        if (jsonContent.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" }, 0);
        }

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

        try
        {
            progress?.Report(new ProgressReport(5, 0, 0, "Đang phân tích cấu trúc JSON...", "Parsing JSON buffer..."));
            var list = JsonSerializer.Deserialize<List<WordExportModel>>(trimmedJson, JsonOptions);
            if (list == null || list.Count == 0)
            {
                return new ImportResult(0, 0, new List<string> { "JSON data is empty or invalid" }, 0);
            }

            int total = list.Count;
            progress?.Report(new ProgressReport(10, 0, total, $"Đã tìm thấy {total} từ vựng. Đang chuẩn bị cơ sở dữ liệu...", "Querying metadata..."));

            var rawTopics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
            var allTopics = rawTopics != null ? rawTopics.ToDictionary(t => t.Name.ToLowerInvariant()) : new Dictionary<string, Topic>();

            var rawTags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
            var allExamTags = rawTags != null ? rawTags.ToDictionary(et => et.Name.ToLowerInvariant()) : new Dictionary<string, ExamTag>();

            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int current = 0;
            foreach (var item in list)
            {
                current++;
                if (current % 5 == 0 || current == total)
                {
                    int pct = (int)((double)current / total * 75) + 15;
                    progress?.Report(new ProgressReport(pct, current, total, $"Đang xử lý: {current} / {total}", item.Term));
                }

                if (string.IsNullOrWhiteSpace(item.Term))
                {
                    skipped++;
                    continue;
                }

                var normalized = item.Term.Trim().ToLowerInvariant();

                // Check intra-file duplicate
                if (!seenTerms.Add(normalized))
                {
                    skipped++;
                    continue;
                }

                var existing = await _vocabRepo.GetByTermAsync(normalized, cancellationToken);
                if (existing != null)
                {
                    if (conflictResolution == ImportConflictResolution.Overwrite)
                    {
                        if (item.Level != CefrLevel.None) existing.Level = item.Level;
                        if (!string.IsNullOrWhiteSpace(item.PhoneticUk)) existing.PhoneticUk = item.PhoneticUk;
                        if (!string.IsNullOrWhiteSpace(item.PhoneticUs)) existing.PhoneticUs = item.PhoneticUs;
                        if (!string.IsNullOrWhiteSpace(item.Notes)) existing.Notes = item.Notes;

                        MergeMeanings(existing, item.Meanings);
                        await _vocabRepo.UpdateAsync(existing, cancellationToken);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                    continue;
                }

                var primaryPos = item.PrimaryPartOfSpeech != 0
                    ? item.PrimaryPartOfSpeech
                    : (item.Meanings.FirstOrDefault(m => m.PartOfSpeech != 0)?.PartOfSpeech ?? PartOfSpeech.Noun);

                var word = new Word
                {
                    Term = item.Term.Trim(),
                    NormalizedTerm = normalized,
                    PhoneticUk = item.PhoneticUk,
                    PhoneticUs = item.PhoneticUs,
                    Level = item.Level,
                    PrimaryPartOfSpeech = primaryPos,
                    Notes = item.Notes
                };

                int idx = 0;
                foreach (var m in item.Meanings)
                {
                    var meaningPos = m.PartOfSpeech != 0 ? m.PartOfSpeech : primaryPos;
                    var meaning = new WordMeaning
                    {
                        WordId = word.Id,
                        PartOfSpeech = meaningPos,
                        DefinitionVi = m.DefinitionVi,
                        DefinitionEn = m.DefinitionEn,
                        Explanation = m.Explanation,
                        OrderIndex = idx++
                    };

                    foreach (var ex in m.Examples)
                    {
                        meaning.Examples.Add(new ExampleSentence
                        {
                            WordMeaningId = meaning.Id,
                            SentenceEn = ex.SentenceEn,
                            SentenceVi = ex.SentenceVi
                        });
                    }

                    word.Meanings.Add(meaning);
                }

                if (word.Meanings.Count == 0)
                {
                    word.Meanings.Add(new WordMeaning
                    {
                        WordId = word.Id,
                        PartOfSpeech = word.PrimaryPartOfSpeech,
                        DefinitionVi = item.Term,
                        OrderIndex = 0
                    });
                }

                // Link topics
                foreach (var tname in item.Topics)
                {
                    if (allTopics.TryGetValue(tname.ToLowerInvariant(), out var topic))
                    {
                        word.WordTopics.Add(new WordTopic { WordId = word.Id, TopicId = topic.Id });
                    }
                }

                // Link exam tags
                foreach (var etname in item.ExamTags)
                {
                    if (allExamTags.TryGetValue(etname.ToLowerInvariant(), out var examTag))
                    {
                        word.WordExamTags.Add(new WordExamTag { WordId = word.Id, ExamTagId = examTag.Id });
                    }
                }

                // Spaced repetition card
                word.ReviewCard = new ReviewCard
                {
                    WordId = word.Id,
                    Status = LearningStatus.New,
                    NextReviewAt = DateTime.UtcNow
                };

                await _vocabRepo.AddAsync(word, cancellationToken);
                imported++;
            }

            progress?.Report(new ProgressReport(95, total, total, "Đang ghi dữ liệu vào cơ sở dữ liệu...", "Committing transaction..."));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            progress?.Report(new ProgressReport(100, total, total, "Nhập dữ liệu hoàn tất!", "Complete."));
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing JSON: {ex.Message}");
        }

        return new ImportResult(imported, skipped, errors, updated);
    }

    public async Task<ImportResult> ImportFromCsvAsync(
        string csvContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int updated = 0;
        int skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return new ImportResult(0, 0, new List<string> { "CSV content is empty" }, 0);
        }

        if (csvContent.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" }, 0);
        }

        try
        {
            progress?.Report(new ProgressReport(5, 0, 0, "Đang phân tích cấu trúc CSV...", "Parsing CSV rows..."));
            var records = ParseCsvRecords(csvContent);
            if (records.Count <= 1)
            {
                return new ImportResult(0, 0, new List<string> { "CSV content has no data rows" }, 0);
            }

            int total = records.Count - 1;
            progress?.Report(new ProgressReport(10, 0, total, $"Đã tìm thấy {total} dòng từ vựng...", "Querying metadata..."));

            var rawTopics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
            var allTopics = rawTopics != null ? rawTopics.ToDictionary(t => t.Name.ToLowerInvariant()) : new Dictionary<string, Topic>();

            var rawTags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
            var allExamTags = rawTags != null ? rawTags.ToDictionary(et => et.Name.ToLowerInvariant()) : new Dictionary<string, ExamTag>();

            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = 1; r < records.Count; r++)
            {
                if (r % 5 == 0 || r == records.Count - 1)
                {
                    int pct = (int)((double)r / total * 75) + 15;
                    progress?.Report(new ProgressReport(pct, r, total, $"Đang xử lý CSV: {r} / {total}", records[r].Count > 0 ? records[r][0] : null));
                }

                var columns = records[r];
                if (columns.Count == 0 || string.IsNullOrWhiteSpace(columns[0]))
                {
                    skipped++;
                    continue;
                }

                string term = columns[0].Trim();
                string normalized = term.ToLowerInvariant();

                // Check intra-file duplicate
                if (!seenTerms.Add(normalized))
                {
                    skipped++;
                    continue;
                }

                // Parse Level
                CefrLevel level = CefrLevel.None;
                if (columns.Count > 1)
                {
                    level = ParseCefrLevel(columns[1].Trim(), CefrLevel.None);
                }

                // Parse POS
                var posList = columns.Count > 2
                    ? ParseMultiplePartsOfSpeech(columns[2].Trim(), PartOfSpeech.Noun)
                    : new List<PartOfSpeech> { PartOfSpeech.Noun };
                var primaryPos = posList.Count > 0 ? posList[0] : PartOfSpeech.Noun;

                string? phoneticUk = columns.Count > 3 && !string.IsNullOrWhiteSpace(columns[3]) ? columns[3].Trim() : null;
                string? phoneticUs = columns.Count > 4 && !string.IsNullOrWhiteSpace(columns[4]) ? columns[4].Trim() : null;
                string meaningsVi = columns.Count > 5 ? columns[5].Trim() : term;
                string examplesText = columns.Count > 6 ? columns[6].Trim() : "";
                string topicsText = columns.Count > 7 ? columns[7].Trim() : "";
                string examTagsText = columns.Count > 8 ? columns[8].Trim() : "";
                string? notes = columns.Count > 9 ? columns[9].Trim() : null;

                var existing = await _vocabRepo.GetByTermAsync(normalized, cancellationToken);
                if (existing != null)
                {
                    if (conflictResolution == ImportConflictResolution.Overwrite)
                    {
                        if (level != CefrLevel.None) existing.Level = level;
                        if (!string.IsNullOrWhiteSpace(phoneticUk)) existing.PhoneticUk = phoneticUk;
                        if (!string.IsNullOrWhiteSpace(phoneticUs)) existing.PhoneticUs = phoneticUs;
                        if (!string.IsNullOrWhiteSpace(notes)) existing.Notes = notes;

                        var parsedMeanings = ParseMeaningsFromCsv(term, meaningsVi, posList, primaryPos, examplesText);
                        MergeMeanings(existing, parsedMeanings);
                        await _vocabRepo.UpdateAsync(existing, cancellationToken);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                    continue;
                }

                var word = new Word
                {
                    Term = term,
                    NormalizedTerm = normalized,
                    Level = level,
                    PrimaryPartOfSpeech = primaryPos,
                    PhoneticUk = phoneticUk,
                    PhoneticUs = phoneticUs,
                    Notes = notes
                };

                var generatedMeanings = ParseMeaningsFromCsv(term, meaningsVi, posList, primaryPos, examplesText);
                foreach (var gm in generatedMeanings)
                {
                    var meaning = new WordMeaning
                    {
                        WordId = word.Id,
                        PartOfSpeech = gm.PartOfSpeech,
                        DefinitionVi = gm.DefinitionVi,
                        DefinitionEn = gm.DefinitionEn,
                        Explanation = gm.Explanation,
                        OrderIndex = word.Meanings.Count
                    };
                    foreach (var ex in gm.Examples)
                    {
                        meaning.Examples.Add(new ExampleSentence
                        {
                            WordMeaningId = meaning.Id,
                            SentenceEn = ex.SentenceEn,
                            SentenceVi = ex.SentenceVi
                        });
                    }
                    word.Meanings.Add(meaning);
                }

                // Link topics from CSV
                if (!string.IsNullOrWhiteSpace(topicsText))
                {
                    var topicTokens = topicsText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var tname in topicTokens)
                    {
                        if (allTopics.TryGetValue(tname.ToLowerInvariant(), out var topic))
                        {
                            word.WordTopics.Add(new WordTopic { WordId = word.Id, TopicId = topic.Id });
                        }
                    }
                }

                // Link exam tags from CSV
                if (!string.IsNullOrWhiteSpace(examTagsText))
                {
                    var tagTokens = examTagsText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var etname in tagTokens)
                    {
                        if (allExamTags.TryGetValue(etname.ToLowerInvariant(), out var examTag))
                        {
                            word.WordExamTags.Add(new WordExamTag { WordId = word.Id, ExamTagId = examTag.Id });
                        }
                    }
                }

                word.ReviewCard = new ReviewCard
                {
                    WordId = word.Id,
                    Status = LearningStatus.New,
                    NextReviewAt = DateTime.UtcNow
                };

                await _vocabRepo.AddAsync(word, cancellationToken);
                imported++;
            }

            progress?.Report(new ProgressReport(95, total, total, "Đang ghi dữ liệu vào cơ sở dữ liệu...", "Committing transaction..."));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            progress?.Report(new ProgressReport(100, total, total, "Nhập CSV hoàn tất!", "Complete."));
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing CSV: {ex.Message}");
        }

        return new ImportResult(imported, skipped, errors, updated);
    }

    public async Task<ImportResult> ImportFromTxtAsync(
        string txtContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int updated = 0;
        int skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(txtContent))
        {
            return new ImportResult(0, 0, new List<string> { "TXT content is empty" }, 0);
        }

        if (txtContent.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" }, 0);
        }

        try
        {
            progress?.Report(new ProgressReport(5, 0, 0, "Đang phân tích cấu trúc văn bản TXT...", "Parsing TXT lines..."));
            using var reader = new StringReader(txtContent);
            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? line;
            int lineNumber = 0;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                lineNumber++;
                var trimmed = line.Trim();

                // Skip empty lines or comments
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#') || trimmed.StartsWith("//"))
                {
                    continue;
                }

                // Strip leading markdown bullets
                trimmed = trimmed.TrimStart('-', '*', '•', ' ').Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                if (lineNumber % 5 == 0)
                {
                    progress?.Report(new ProgressReport(Math.Min(85, lineNumber), lineNumber, 0, $"Đang xử lý dòng {lineNumber}...", trimmed));
                }

                string term = "";
                string definition = "";
                string? posStr = null;
                string? levelStr = null;
                string? phoneticUk = null;
                string? phoneticUs = null;
                string? examplesStr = null;

                if (trimmed.Contains('\t'))
                {
                    // Tab-delimited (Lexora export / Anki / Quizlet)
                    var parts = trimmed.Split('\t');
                    term = parts[0].Trim();
                    if (parts.Length > 1) definition = parts[1].Trim();
                    if (parts.Length > 2) posStr = parts[2].Trim();
                    if (parts.Length > 3) levelStr = parts[3].Trim();
                    if (parts.Length > 4) phoneticUk = parts[4].Trim();
                    if (parts.Length > 5) phoneticUs = parts[5].Trim();
                    if (parts.Length > 6) examplesStr = parts[6].Trim();
                }
                else
                {
                    // Look for common delimiters: " -> ", " => ", " : ", " - ", " | ", " = "
                    string[] delimiters = { " -> ", " => ", " : ", " - ", " | ", " = " };
                    int foundDelimIdx = -1;
                    string matchedDelim = "";

                    foreach (var delim in delimiters)
                    {
                        var idx = trimmed.IndexOf(delim, StringComparison.Ordinal);
                        if (idx > 0 && (foundDelimIdx == -1 || idx < foundDelimIdx))
                        {
                            foundDelimIdx = idx;
                            matchedDelim = delim;
                        }
                    }

                    if (foundDelimIdx > 0)
                    {
                        term = trimmed.Substring(0, foundDelimIdx).Trim();
                        definition = trimmed.Substring(foundDelimIdx + matchedDelim.Length).Trim();
                    }
                    else if (trimmed.Contains(':'))
                    {
                        var idx = trimmed.IndexOf(':');
                        term = trimmed.Substring(0, idx).Trim();
                        definition = trimmed.Substring(idx + 1).Trim();
                    }
                    else if (trimmed.Contains('-'))
                    {
                        var idx = trimmed.IndexOf('-');
                        term = trimmed.Substring(0, idx).Trim();
                        definition = trimmed.Substring(idx + 1).Trim();
                    }
                    else
                    {
                        term = trimmed;
                        definition = trimmed;
                    }
                }

                // Strip markdown bold like **term**
                if (term.StartsWith("**") && term.EndsWith("**") && term.Length > 4)
                {
                    term = term.Substring(2, term.Length - 4).Trim();
                }

                if (string.IsNullOrWhiteSpace(term))
                {
                    skipped++;
                    continue;
                }

                // If term contains phonetic like "abandon (/əˈbæn.dən/)", extract it
                if (term.Contains('/') && phoneticUk == null)
                {
                    var slashStart = term.IndexOf('/');
                    var slashEnd = term.LastIndexOf('/');
                    if (slashEnd > slashStart)
                    {
                        phoneticUk = term.Substring(slashStart, slashEnd - slashStart + 1).Trim();
                        phoneticUs = phoneticUk;
                        term = term.Substring(0, slashStart).Trim().TrimEnd('(', '[').Trim();
                    }
                }

                // If term contains POS like "collaborate (v)", extract POS
                if (posStr == null && term.EndsWith(')') && term.Contains('('))
                {
                    var openIdx = term.LastIndexOf('(');
                    var candidatePos = term.Substring(openIdx + 1, term.Length - openIdx - 2).Trim();
                    if (candidatePos.Length <= 4)
                    {
                        posStr = candidatePos;
                        term = term.Substring(0, openIdx).Trim();
                    }
                }

                var normalized = term.ToLowerInvariant();

                // Intra-file deduplication
                if (!seenTerms.Add(normalized))
                {
                    skipped++;
                    continue;
                }

                var pos = ParsePartOfSpeech(posStr, PartOfSpeech.Noun);
                var level = ParseCefrLevel(levelStr, CefrLevel.None);

                // Database deduplication
                var existing = await _vocabRepo.GetByTermAsync(normalized, cancellationToken);
                if (existing != null)
                {
                    if (conflictResolution == ImportConflictResolution.Overwrite)
                    {
                        if (level != CefrLevel.None) existing.Level = level;
                        if (!string.IsNullOrWhiteSpace(phoneticUk)) existing.PhoneticUk = phoneticUk;
                        if (!string.IsNullOrWhiteSpace(phoneticUs)) existing.PhoneticUs = phoneticUs;

                        if (!string.IsNullOrWhiteSpace(definition) && !existing.Meanings.Any(m => m.DefinitionVi.Equals(definition, StringComparison.OrdinalIgnoreCase)))
                        {
                            existing.Meanings.Add(new WordMeaning
                            {
                                WordId = existing.Id,
                                PartOfSpeech = pos,
                                DefinitionVi = definition,
                                OrderIndex = existing.Meanings.Count
                            });
                        }
                        await _vocabRepo.UpdateAsync(existing, cancellationToken);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                    continue;
                }

                var word = new Word
                {
                    Term = term,
                    NormalizedTerm = normalized,
                    Level = level,
                    PrimaryPartOfSpeech = pos,
                    PhoneticUk = !string.IsNullOrWhiteSpace(phoneticUk) ? phoneticUk : null,
                    PhoneticUs = !string.IsNullOrWhiteSpace(phoneticUs) ? phoneticUs : null
                };

                var primaryMeaning = new WordMeaning
                {
                    WordId = word.Id,
                    PartOfSpeech = pos,
                    DefinitionVi = !string.IsNullOrWhiteSpace(definition) ? definition : term,
                    OrderIndex = 0
                };

                if (!string.IsNullOrWhiteSpace(examplesStr))
                {
                    var exampleTokens = examplesStr.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var exToken in exampleTokens)
                    {
                        var arrowIdx = exToken.IndexOf("=>", StringComparison.Ordinal);
                        string en = arrowIdx >= 0 ? exToken.Substring(0, arrowIdx).Trim() : exToken.Trim();
                        string vi = arrowIdx >= 0 ? exToken.Substring(arrowIdx + 2).Trim() : "";
                        if (!string.IsNullOrWhiteSpace(en))
                        {
                            primaryMeaning.Examples.Add(new ExampleSentence
                            {
                                WordMeaningId = primaryMeaning.Id,
                                SentenceEn = en,
                                SentenceVi = vi
                            });
                        }
                    }
                }

                word.Meanings.Add(primaryMeaning);

                word.ReviewCard = new ReviewCard
                {
                    WordId = word.Id,
                    Status = LearningStatus.New,
                    NextReviewAt = DateTime.UtcNow
                };

                await _vocabRepo.AddAsync(word, cancellationToken);
                imported++;
            }

            if (imported > 0 || updated > 0)
            {
                progress?.Report(new ProgressReport(95, imported + updated, imported + updated, "Đang lưu vào cơ sở dữ liệu...", "Committing transaction..."));
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            progress?.Report(new ProgressReport(100, imported + updated, imported + updated, "Nhập TXT hoàn tất!", "Complete."));
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing TXT: {ex.Message}");
        }

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
                "term": "perseverance",
                "phoneticUk": "/ˌpɜː.sɪˈvɪə.rəns/",
                "phoneticUs": "/ˌpɝː.səˈvɪr.əns/",
                "level": "B2",
                "primaryPartOfSpeech": "Noun",
                "notes": "Từ vựng xuất hiện nhiều trong kỳ thi IELTS & TOEIC",
                "meanings": [
                  {
                    "partOfSpeech": "Noun",
                    "definitionVi": "sự kiên trì, bền bỉ",
                    "definitionEn": "continued effort to do or achieve something despite difficulties",
                    "examples": [
                      {
                        "sentenceEn": "Through hard work and perseverance, she succeeded in her exams.",
                        "sentenceVi": "Nhờ làm việc chăm chỉ và kiên trì, cô ấy đã đỗ các kỳ thi."
                      }
                    ]
                  }
                ],
                "topics": ["Personal Development"],
                "examTags": ["IELTS", "TOEIC"]
              }
            ]
            """;
        }

        if (clean.Contains("csv"))
        {
            return """
            Term,Level,PartOfSpeech,PhoneticUk,PhoneticUs,MeaningsVi,Examples,Topics,ExamTags,Notes
            "collaborate","B2","Verb","/kəˈlæb.ə.reɪt/","/kəˈlæb.ə.reɪt/","[Verb] hợp tác = to work together with someone","We collaborated on the project => Chúng tôi đã hợp tác trong dự án","Work;Business","TOEIC;IELTS","Thường đi với with hoặc on"
            "diligent","B1","Adjective","/ˈdɪl.ɪ.dʒənt/","/ˈdɪl.ə.dʒənt/","[Adjective] siêng năng, cần cù = careful and using a lot of effort","A diligent student => Một học sinh cần cù","Education","IELTS",""
            """;
        }

        return """
        # Mẫu danh sách từ vựng Lexora (Hỗ trợ Tab TSV, Anki, Quizlet, Oxford, Markdown)
        # Cách 1: Phân tách bằng Tab (TSV)
        perseverance	sự kiên trì	Noun	B2	/ˌpɜː.sɪˈvɪə.rəns/	/ˌpɝː.səˈvɪr.əns/	Hard work and perseverance => Chăm chỉ và kiên trì
        collaborate	hợp tác	Verb	B2

        # Cách 2: Dạng gạch ngang hoặc hai chấm hoặc mũi tên
        diligent - siêng năng, cần cù
        resilient : kiên cường, có khả năng phục hồi
        accomplish -> hoàn thành, đạt được
        """;
    }

    private static void MergeMeanings(Word existing, List<MeaningExportModel> incomingMeanings)
    {
        foreach (var m in incomingMeanings)
        {
            var matchMeaning = existing.Meanings.FirstOrDefault(em =>
                em.PartOfSpeech == m.PartOfSpeech &&
                em.DefinitionVi.Trim().Equals(m.DefinitionVi.Trim(), StringComparison.OrdinalIgnoreCase));

            if (matchMeaning == null)
            {
                var newMeaning = new WordMeaning
                {
                    WordId = existing.Id,
                    PartOfSpeech = m.PartOfSpeech != 0 ? m.PartOfSpeech : existing.PrimaryPartOfSpeech,
                    DefinitionVi = m.DefinitionVi,
                    DefinitionEn = m.DefinitionEn,
                    Explanation = m.Explanation,
                    OrderIndex = existing.Meanings.Count
                };
                foreach (var ex in m.Examples)
                {
                    newMeaning.Examples.Add(new ExampleSentence
                    {
                        WordMeaningId = newMeaning.Id,
                        SentenceEn = ex.SentenceEn,
                        SentenceVi = ex.SentenceVi
                    });
                }
                existing.Meanings.Add(newMeaning);
            }
            else
            {
                foreach (var ex in m.Examples)
                {
                    if (!matchMeaning.Examples.Any(ee => ee.SentenceEn.Trim().Equals(ex.SentenceEn.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        matchMeaning.Examples.Add(new ExampleSentence
                        {
                            WordMeaningId = matchMeaning.Id,
                            SentenceEn = ex.SentenceEn,
                            SentenceVi = ex.SentenceVi
                        });
                    }
                }
            }
        }
    }

    private static List<MeaningExportModel> ParseMeaningsFromCsv(
        string term,
        string meaningsVi,
        List<PartOfSpeech> posList,
        PartOfSpeech primaryPos,
        string examplesText)
    {
        var result = new List<MeaningExportModel>();
        var meaningTokens = meaningsVi.Split(new[] { " | ", " // ", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (meaningTokens.Length == 0)
        {
            meaningTokens = new[] { meaningsVi };
        }

        int idx = 0;
        foreach (var mToken in meaningTokens)
        {
            var meaningPos = idx < posList.Count ? posList[idx] : primaryPos;
            var defVi = mToken.Trim();

            if (defVi.StartsWith('[') && defVi.Contains(']'))
            {
                var closeIdx = defVi.IndexOf(']');
                var posStr = defVi.Substring(1, closeIdx - 1);
                meaningPos = ParsePartOfSpeech(posStr, meaningPos);
                defVi = defVi.Substring(closeIdx + 1).Trim();
            }

            string? defEn = null;
            var equalsIdx = defVi.IndexOf(" = ", StringComparison.Ordinal);
            if (equalsIdx > 0)
            {
                defEn = defVi.Substring(equalsIdx + 3).Trim();
                defVi = defVi.Substring(0, equalsIdx).Trim();
            }

            string? explanation = null;
            if (defEn != null && defEn.EndsWith(')') && defEn.Contains('('))
            {
                var openIdx = defEn.LastIndexOf('(');
                explanation = defEn.Substring(openIdx + 1, defEn.Length - openIdx - 2).Trim();
                defEn = defEn.Substring(0, openIdx).Trim();
            }
            else if (defVi.EndsWith(')') && defVi.Contains('('))
            {
                var openIdx = defVi.LastIndexOf('(');
                explanation = defVi.Substring(openIdx + 1, defVi.Length - openIdx - 2).Trim();
                defVi = defVi.Substring(0, openIdx).Trim();
            }

            var exportModel = new MeaningExportModel
            {
                PartOfSpeech = meaningPos,
                DefinitionVi = string.IsNullOrWhiteSpace(defVi) ? term : defVi,
                DefinitionEn = defEn,
                Explanation = explanation
            };
            result.Add(exportModel);
            idx++;
        }

        if (!string.IsNullOrWhiteSpace(examplesText) && result.Count > 0)
        {
            var primary = result[0];
            var exampleTokens = examplesText.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var exToken in exampleTokens)
            {
                var parts = exToken.Split(new[] { " => " }, StringSplitOptions.RemoveEmptyEntries);
                var en = parts.Length > 0 ? parts[0].Trim() : "";
                var vi = parts.Length > 1 ? parts[1].Trim() : "";
                if (!string.IsNullOrWhiteSpace(en))
                {
                    primary.Examples.Add(new ExampleExportModel
                    {
                        SentenceEn = en,
                        SentenceVi = vi
                    });
                }
            }
        }

        return result;
    }

    public static PartOfSpeech ParsePartOfSpeech(string? input, PartOfSpeech defaultPos = PartOfSpeech.Noun)
    {
        if (string.IsNullOrWhiteSpace(input))
            return defaultPos;

        var clean = input.Trim().ToLowerInvariant().TrimEnd('.', ',');

        if (Enum.TryParse<PartOfSpeech>(clean, ignoreCase: true, out var exact))
            return exact;

        return clean switch
        {
            "n" or "noun" or "danh từ" or "danh tu" => PartOfSpeech.Noun,
            "v" or "verb" or "động từ" or "dong tu" => PartOfSpeech.Verb,
            "adj" or "a" or "adjective" or "tính từ" or "tinh tu" => PartOfSpeech.Adjective,
            "adv" or "adverb" or "trạng từ" or "phó từ" or "trang tu" or "pho tu" => PartOfSpeech.Adverb,
            "prep" or "preposition" or "giới từ" or "gioi tu" => PartOfSpeech.Preposition,
            "conj" or "conjunction" or "liên từ" or "lien tu" => PartOfSpeech.Conjunction,
            "pron" or "pronoun" or "đại từ" or "dai tu" => PartOfSpeech.Noun,
            "int" or "interj" or "interjection" or "thán từ" or "than tu" => PartOfSpeech.Interjection,
            "idm" or "idiom" or "thành ngữ" or "thanh ngu" => PartOfSpeech.Idiom,
            "phrv" or "phr v" or "phrasal verb" or "phrasal_verb" or "cụm động từ" or "cum dong tu" => PartOfSpeech.PhrasalVerb,
            "np" or "noun phrase" or "noun_phrase" or "cụm danh từ" or "cum danh tu" => PartOfSpeech.NounPhrase,
            "phrase" or "phr" or "cụm từ" or "cum tu" => PartOfSpeech.Phrase,
            _ => defaultPos
        };
    }

    public static List<PartOfSpeech> ParseMultiplePartsOfSpeech(string? input, PartOfSpeech fallback = PartOfSpeech.Noun)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [fallback];

        var tokens = input.Split(new[] { ';', ',', '/', '&', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<PartOfSpeech>();
        var seen = new HashSet<PartOfSpeech>();

        foreach (var token in tokens)
        {
            var parsed = ParsePartOfSpeech(token, fallback);
            if (seen.Add(parsed))
            {
                result.Add(parsed);
            }
        }

        return result.Count > 0 ? result : [fallback];
    }

    public static CefrLevel ParseCefrLevel(string? input, CefrLevel defaultLevel = CefrLevel.None)
    {
        if (string.IsNullOrWhiteSpace(input))
            return defaultLevel;

        var clean = input.Trim().ToLowerInvariant();
        if (Enum.TryParse<CefrLevel>(clean, ignoreCase: true, out var exact))
            return exact;

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

    private static readonly System.Buffers.SearchValues<char> CsvFormulaTriggers = System.Buffers.SearchValues.Create("=+-@\t\r");

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";

        // Mitigate CSV Formula Injection (CWE-1236)
        bool isFormula = CsvFormulaTriggers.Contains(field[0]);
        var safeField = isFormula ? "'" + field : field;

        if (isFormula || safeField.Contains(',') || safeField.Contains('"') || safeField.Contains('\n') || safeField.Contains('\r'))
        {
            return $"\"{safeField.Replace("\"", "\"\"")}\"";
        }
        return safeField;
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

    private class WordExportModel
    {
        public string Term { get; set; } = string.Empty;
        public string? PhoneticUk { get; set; }
        public string? PhoneticUs { get; set; }
        public CefrLevel Level { get; set; }
        public PartOfSpeech PrimaryPartOfSpeech { get; set; }
        public List<PartOfSpeech> AllPartsOfSpeech { get; set; } = new();
        public string? Notes { get; set; }
        public List<MeaningExportModel> Meanings { get; set; } = new();
        public List<string> Topics { get; set; } = new();
        public List<string> ExamTags { get; set; } = new();
    }

    private class MeaningExportModel
    {
        public PartOfSpeech PartOfSpeech { get; set; }
        public string DefinitionVi { get; set; } = string.Empty;
        public string? DefinitionEn { get; set; }
        public string? Explanation { get; set; }
        public List<ExampleExportModel> Examples { get; set; } = new();
    }

    private class ExampleExportModel
    {
        public string SentenceEn { get; set; } = string.Empty;
        public string SentenceVi { get; set; } = string.Empty;
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(line)) return result;

        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++; // skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString().Trim());
        return result;
    }
}
