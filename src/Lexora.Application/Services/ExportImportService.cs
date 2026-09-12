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

    public async Task<ImportResult> ImportFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        int imported = 0;
        int skipped = 0;
        var errors = new List<string>();

        try
        {
            var list = JsonSerializer.Deserialize<List<WordExportModel>>(jsonContent, JsonOptions);
            if (list == null || list.Count == 0)
            {
                return new ImportResult(0, 0, new List<string> { "JSON data is empty or invalid" });
            }

            var allTopics = (await _vocabRepo.GetAllTopicsAsync(cancellationToken)).ToDictionary(t => t.Name.ToLowerInvariant());
            var allExamTags = (await _vocabRepo.GetAllExamTagsAsync(cancellationToken)).ToDictionary(et => et.Name.ToLowerInvariant());

            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item.Term))
                {
                    skipped++;
                    continue;
                }

                var normalized = item.Term.Trim().ToLowerInvariant();
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

        try
        {
            using var reader = new StringReader(csvContent);
            string? headerLine = await reader.ReadLineAsync(cancellationToken);
            if (headerLine == null)
            {
                return new ImportResult(0, 0, new List<string> { "CSV content is empty" });
            }

            string? line;
            int lineNumber = 1;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = ParseCsvLine(line);
                if (columns.Count == 0 || string.IsNullOrWhiteSpace(columns[0]))
                {
                    skipped++;
                    continue;
                }

                string term = columns[0].Trim();
                string normalized = term.ToLowerInvariant();

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

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
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
