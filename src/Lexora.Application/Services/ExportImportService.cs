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

    public async Task<string> ExportToJsonAsync(CancellationToken cancellationToken = default)
    {
        var words = await _vocabRepo.SearchAsync(take: 100000, cancellationToken: cancellationToken);

        var exportData = words.Select(w => new WordExportModel
        {
            Term = w.Term,
            PhoneticUk = w.PhoneticUk,
            PhoneticUs = w.PhoneticUs,
            Level = w.Level,
            PrimaryPartOfSpeech = w.PrimaryPartOfSpeech,
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
        }).ToList();

        return JsonSerializer.Serialize(exportData, JsonOptions);
    }

    public async Task<string> ExportToCsvAsync(CancellationToken cancellationToken = default)
    {
        var words = await _vocabRepo.SearchAsync(take: 100000, cancellationToken: cancellationToken);
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Term,Level,PartOfSpeech,PhoneticUk,PhoneticUs,MeaningsVi,Examples,Topics,ExamTags,Notes");

        foreach (var w in words)
        {
            var meaningsJoined = string.Join(" | ", w.Meanings.Select(m => $"[{m.PartOfSpeech}] {m.DefinitionVi}"));
            var examplesJoined = string.Join(" | ", w.Meanings.SelectMany(m => m.Examples).Select(e => $"{e.SentenceEn} => {e.SentenceVi}"));
            var topicsJoined = string.Join(";", w.WordTopics.Select(wt => wt.Topic.Name));
            var examTagsJoined = string.Join(";", w.WordExamTags.Select(wet => wet.ExamTag.Name));

            sb.AppendLine(string.Join(",",
                EscapeCsv(w.Term),
                w.Level.ToString(),
                w.PrimaryPartOfSpeech.ToString(),
                EscapeCsv(w.PhoneticUk ?? ""),
                EscapeCsv(w.PhoneticUs ?? ""),
                EscapeCsv(meaningsJoined),
                EscapeCsv(examplesJoined),
                EscapeCsv(topicsJoined),
                EscapeCsv(examTagsJoined),
                EscapeCsv(w.Notes ?? "")
            ));
        }

        return sb.ToString();
    }

    public async Task<string> ExportToTxtAsync(CancellationToken cancellationToken = default)
    {
        var words = await _vocabRepo.SearchAsync(take: 100000, cancellationToken: cancellationToken);
        var sb = new StringBuilder();

        // Comments header
        sb.AppendLine("# Lexora Vocabulary Export");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("# Format: Term <tab> PrimaryDefinition <tab> PartOfSpeech <tab> Level <tab> PhoneticUk <tab> PhoneticUs <tab> Examples");
        sb.AppendLine("# Note: Compatible with Lexora, Anki (TSV), and Quizlet.");
        sb.AppendLine();

        foreach (var w in words)
        {
            var primaryDef = w.Meanings.OrderBy(m => m.OrderIndex).FirstOrDefault()?.DefinitionVi ?? w.Term;
            var pos = w.PrimaryPartOfSpeech.ToString();
            var level = w.Level.ToString();
            var uk = w.PhoneticUk ?? "";
            var us = w.PhoneticUs ?? "";
            var examples = string.Join(" | ", w.Meanings.SelectMany(m => m.Examples).Select(e => $"{e.SentenceEn} => {e.SentenceVi}"));

            sb.AppendLine($"{w.Term}\t{primaryDef}\t{pos}\t{level}\t{uk}\t{us}\t{examples}");
        }

        return sb.ToString();
    }

    private const int MaxImportPayloadLength = 20_000_000; // 20 MB safety limit

    public async Task<ImportResult> ImportFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default)
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
            var list = JsonSerializer.Deserialize<List<WordExportModel>>(jsonContent, JsonOptions);
            if (list == null || list.Count == 0)
            {
                return new ImportResult(0, 0, new List<string> { "JSON data is empty or invalid" });
            }

            var rawTopics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
            var allTopics = rawTopics != null ? rawTopics.ToDictionary(t => t.Name.ToLowerInvariant()) : new Dictionary<string, Topic>();

            var rawTags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
            var allExamTags = rawTags != null ? rawTags.ToDictionary(et => et.Name.ToLowerInvariant()) : new Dictionary<string, ExamTag>();

            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in list)
            {
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

                var word = new Word
                {
                    Term = item.Term.Trim(),
                    NormalizedTerm = normalized,
                    PhoneticUk = item.PhoneticUk,
                    PhoneticUs = item.PhoneticUs,
                    Level = item.Level,
                    PrimaryPartOfSpeech = item.PrimaryPartOfSpeech,
                    Notes = item.Notes
                };

                int idx = 0;
                foreach (var m in item.Meanings)
                {
                    var meaning = new WordMeaning
                    {
                        WordId = word.Id,
                        PartOfSpeech = m.PartOfSpeech,
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

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing JSON: {ex.Message}");
        }

        return new ImportResult(imported, skipped, errors);
    }

    public async Task<ImportResult> ImportFromCsvAsync(string csvContent, CancellationToken cancellationToken = default)
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
            var records = ParseCsvRecords(csvContent);
            if (records.Count <= 1)
            {
                return new ImportResult(0, 0, new List<string> { "CSV content has no data rows" });
            }

            var rawTopics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
            var allTopics = rawTopics != null ? rawTopics.ToDictionary(t => t.Name.ToLowerInvariant()) : new Dictionary<string, Topic>();

            var rawTags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
            var allExamTags = rawTags != null ? rawTags.ToDictionary(et => et.Name.ToLowerInvariant()) : new Dictionary<string, ExamTag>();

            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Skip row 0 (header)
            for (int r = 1; r < records.Count; r++)
            {
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
                if (columns.Count > 1 && Enum.TryParse<CefrLevel>(columns[1].Trim(), true, out var parsedLevel))
                {
                    level = parsedLevel;
                }

                // Parse POS
                PartOfSpeech pos = PartOfSpeech.Noun;
                if (columns.Count > 2 && Enum.TryParse<PartOfSpeech>(columns[2].Trim(), true, out var parsedPos))
                {
                    pos = parsedPos;
                }

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
                    PrimaryPartOfSpeech = pos,
                    PhoneticUk = phoneticUk,
                    PhoneticUs = phoneticUs,
                    Notes = notes
                };

                // Parse meanings: e.g. "[Verb] hợp tác | [Noun] sự hợp tác"
                var meaningTokens = meaningsVi.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries);
                if (meaningTokens.Length == 0)
                {
                    meaningTokens = new[] { meaningsVi };
                }

                int idx = 0;
                foreach (var mToken in meaningTokens)
                {
                    var meaningPos = pos;
                    var defVi = mToken.Trim();

                    // Check for [PartOfSpeech] prefix
                    if (defVi.StartsWith('[') && defVi.Contains(']'))
                    {
                        var closeIdx = defVi.IndexOf(']');
                        var posStr = defVi.Substring(1, closeIdx - 1);
                        if (Enum.TryParse<PartOfSpeech>(posStr, true, out var extractedPos))
                        {
                            meaningPos = extractedPos;
                        }
                        defVi = defVi.Substring(closeIdx + 1).Trim();
                    }

                    var wordMeaning = new WordMeaning
                    {
                        WordId = word.Id,
                        PartOfSpeech = meaningPos,
                        DefinitionVi = string.IsNullOrWhiteSpace(defVi) ? term : defVi,
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

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing CSV: {ex.Message}");
        }

        return new ImportResult(imported, skipped, errors);
    }

    public async Task<ImportResult> ImportFromTxtAsync(string txtContent, CancellationToken cancellationToken = default)
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
                var pos = PartOfSpeech.Noun;
                if (!string.IsNullOrWhiteSpace(posStr) && Enum.TryParse<PartOfSpeech>(posStr, true, out var parsedPos))
                {
                    pos = parsedPos;
                }

                // Parse CEFR Level
                var level = CefrLevel.None;
                if (!string.IsNullOrWhiteSpace(levelStr) && Enum.TryParse<CefrLevel>(levelStr, true, out var parsedLevel))
                {
                    level = parsedLevel;
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
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing TXT: {ex.Message}");
        }

        return new ImportResult(imported, skipped, errors);
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
