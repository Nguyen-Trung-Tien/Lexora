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

    public async Task<string> ExportToJsonAsync(CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu từ vựng...", "Querying database..."));
        var words = await _vocabRepo.SearchAsync(take: 100000, cancellationToken: cancellationToken);

        progress?.Report(new ProgressReport(50, words.Count, words.Count, "Đang định dạng cấu trúc JSON...", "Serializing models..."));
        var exportData = words.Select(w =>
        {
            var distinctPos = w.Meanings.Select(m => m.PartOfSpeech).Distinct().ToList();
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
                Meanings = w.Meanings.OrderBy(m => m.OrderIndex).Select(m => new MeaningExportModel
                {
                    PartOfSpeech = m.PartOfSpeech,
                    DefinitionVi = m.DefinitionVi,
                    DefinitionEn = m.DefinitionEn,
                    Explanation = m.Explanation,
                    Examples = m.Examples.Select(e => new ExampleExportModel
                    {
                        SentenceEn = e.SentenceEn,
                        SentenceVi = e.SentenceVi
                    }).ToList()
                }).ToList(),
                Topics = w.WordTopics.Select(wt => wt.Topic.Name).ToList(),
                ExamTags = w.WordExamTags.Select(wet => wet.ExamTag.Name).ToList()
            };
        }).ToList();

        progress?.Report(new ProgressReport(85, words.Count, words.Count, "Đang tạo dữ liệu xuất...", "Writing JSON buffer..."));
        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        progress?.Report(new ProgressReport(100, words.Count, words.Count, "Xuất dữ liệu hoàn tất!", "Done."));
        return json;
    }

    public async Task<string> ExportToCsvAsync(CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu từ vựng...", "Querying database..."));
        var words = await _vocabRepo.SearchAsync(take: 100000, cancellationToken: cancellationToken);
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

            var distinctPos = w.Meanings.Select(m => m.PartOfSpeech).Distinct().ToList();
            var posDisplay = distinctPos.Count > 1
                ? string.Join("; ", distinctPos.Select(p => p.ToString()))
                : w.PrimaryPartOfSpeech.ToString();

            var meaningsJoined = string.Join(" | ", w.Meanings.Select(m =>
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

            var examplesJoined = string.Join(" | ", w.Meanings.SelectMany(m => m.Examples).Select(e => $"{e.SentenceEn} => {e.SentenceVi}"));
            var topicsJoined = string.Join(";", w.WordTopics.Select(wt => wt.Topic.Name));
            var examTagsJoined = string.Join(";", w.WordExamTags.Select(wet => wet.ExamTag.Name));

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

    public async Task<string> ExportToTxtAsync(CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu từ vựng...", "Querying database..."));
        var words = await _vocabRepo.SearchAsync(take: 100000, cancellationToken: cancellationToken);
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

            var primaryDef = w.Meanings.OrderBy(m => m.OrderIndex).FirstOrDefault()?.DefinitionVi ?? w.Term;
            var distinctPos = w.Meanings.Select(m => m.PartOfSpeech).Distinct().ToList();
            var posDisplay = distinctPos.Count > 1
                ? string.Join(" / ", distinctPos.Select(p => p.ToString()))
                : w.PrimaryPartOfSpeech.ToString();

            var level = w.Level.ToString();
            var uk = w.PhoneticUk ?? "";
            var us = w.PhoneticUs ?? "";
            var examples = string.Join(" | ", w.Meanings.SelectMany(m => m.Examples).Select(e => $"{e.SentenceEn} => {e.SentenceVi}"));

            sb.AppendLine($"{w.Term}\t{primaryDef}\t{posDisplay}\t{level}\t{uk}\t{us}\t{examples}");
        }

        progress?.Report(new ProgressReport(100, total, total, "Xuất TXT hoàn tất!", "Done."));
        return sb.ToString();
    }

    private const int MaxImportPayloadLength = 20_000_000; // 20 MB safety limit

    public async Task<ImportResult> ImportFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new ImportResult(0, 0, new List<string> { "JSON data is empty" });
        }

        if (jsonContent.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" });
        }

        try
        {
            progress?.Report(new ProgressReport(5, 0, 0, "Đang phân tích cấu trúc JSON...", "Parsing JSON buffer..."));
            var list = JsonSerializer.Deserialize<List<WordExportModel>>(jsonContent, JsonOptions);
            if (list == null || list.Count == 0)
            {
                return new ImportResult(0, 0, new List<string> { "JSON data is empty or invalid" });
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
                    skipped++;
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

                // If no meanings specified, add a default placeholder
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

        return new ImportResult(imported, skipped, errors);
    }

    public async Task<ImportResult> ImportFromCsvAsync(string csvContent, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return new ImportResult(0, 0, new List<string> { "CSV content is empty" });
        }

        if (csvContent.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" });
        }

        try
        {
            progress?.Report(new ProgressReport(5, 0, 0, "Đang phân tích cấu trúc CSV...", "Parsing CSV rows..."));
            var records = ParseCsvRecords(csvContent);
            if (records.Count <= 1)
            {
                return new ImportResult(0, 0, new List<string> { "CSV content has no data rows" });
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

                var existing = await _vocabRepo.GetByTermAsync(normalized, cancellationToken);
                if (existing != null)
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

                // Parse meanings: e.g. "[Verb] hợp tác = collaborate (note) | [Noun] sự hợp tác"
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

                    // Check for [PartOfSpeech] prefix
                    if (defVi.StartsWith('[') && defVi.Contains(']'))
                    {
                        var closeIdx = defVi.IndexOf(']');
                        var posStr = defVi.Substring(1, closeIdx - 1);
                        meaningPos = ParsePartOfSpeech(posStr, meaningPos);
                        defVi = defVi.Substring(closeIdx + 1).Trim();
                    }

                    // Check for optional " = DefinitionEn"
                    string? defEn = null;
                    var equalsIdx = defVi.IndexOf(" = ", StringComparison.Ordinal);
                    if (equalsIdx > 0)
                    {
                        defEn = defVi.Substring(equalsIdx + 3).Trim();
                        defVi = defVi.Substring(0, equalsIdx).Trim();
                    }

                    // Check for optional "(Explanation)" at end
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

                    var wordMeaning = new WordMeaning
                    {
                        WordId = word.Id,
                        PartOfSpeech = meaningPos,
                        DefinitionVi = string.IsNullOrWhiteSpace(defVi) ? term : defVi,
                        DefinitionEn = defEn,
                        Explanation = explanation,
                        OrderIndex = idx++
                    };

                    word.Meanings.Add(wordMeaning);
                }

                // Parse examples: "Sentence en => Sentence vi | Another en => Another vi"
                if (!string.IsNullOrWhiteSpace(examplesText) && word.Meanings.Count > 0)
                {
                    var primaryMeaning = word.Meanings.First();
                    var exampleTokens = examplesText.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var exToken in exampleTokens)
                    {
                        var parts = exToken.Split(new[] { " => " }, StringSplitOptions.RemoveEmptyEntries);
                        var en = parts.Length > 0 ? parts[0].Trim() : "";
                        var vi = parts.Length > 1 ? parts[1].Trim() : "";
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

        return new ImportResult(imported, skipped, errors);
    }

    public async Task<ImportResult> ImportFromTxtAsync(string txtContent, CancellationToken cancellationToken = default, IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(txtContent))
        {
            return new ImportResult(0, 0, new List<string> { "TXT content is empty" });
        }

        if (txtContent.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" });
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
                    // Look for common delimiters: " - ", " : ", " | ", " = "
                    string[] delimiters = { " - ", " : ", " | ", " = " };
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
                        // Single word per line without definition
                        term = trimmed;
                        definition = trimmed;
                    }
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

                var normalized = term.ToLowerInvariant();

                // Intra-file deduplication
                if (!seenTerms.Add(normalized))
                {
                    skipped++;
                    continue;
                }

                // Database deduplication
                var existing = await _vocabRepo.GetByTermAsync(normalized, cancellationToken);
                if (existing != null)
                {
                    skipped++;
                    continue;
                }

                // Parse Part of Speech
                var pos = ParsePartOfSpeech(posStr, PartOfSpeech.Noun);

                // Parse CEFR Level
                var level = ParseCefrLevel(levelStr, CefrLevel.None);

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

                // Parse examples if present
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

            if (imported > 0)
            {
                progress?.Report(new ProgressReport(95, imported, imported, "Đang lưu vào cơ sở dữ liệu...", "Committing transaction..."));
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            progress?.Report(new ProgressReport(100, imported, imported, "Nhập TXT hoàn tất!", "Complete."));
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing TXT: {ex.Message}");
        }

        return new ImportResult(imported, skipped, errors);
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
}
