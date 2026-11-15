using System.Net;
using System.Text;
using System.Text.Json;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services;

public partial class ExportImportService
{
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

    public async Task<string> ExportVocabularyOnlyJsonAsync(
        IEnumerable<Guid>? wordIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu từ vựng...", "Querying database..."));
        var words = await GetWordsForExportAsync(wordIds, cancellationToken);

        progress?.Report(new ProgressReport(50, words.Count, words.Count, "Đang định dạng cấu trúc JSON...", "Serializing models..."));
        var exportData = words.Select(w =>
        {
            var meanings = (w.Meanings ?? [])
                .OrderBy(m => m.OrderIndex)
                .Select(m => new MeaningOnlyExportModel
                {
                    PartOfSpeech = m.PartOfSpeech,
                    DefinitionVi = m.DefinitionVi,
                    DefinitionEn = m.DefinitionEn
                }).ToList();

            return new VocabularyOnlyExportModel
            {
                Term = w.Term,
                Meanings = meanings
            };
        }).ToList();

        progress?.Report(new ProgressReport(85, words.Count, words.Count, "Đang tạo dữ liệu xuất...", "Writing JSON buffer..."));
        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        progress?.Report(new ProgressReport(100, words.Count, words.Count, "Xuất dữ liệu hoàn tất!", "Done."));
        return json;
    }

    public async Task<string> ExportVocabularyOnlyCsvAsync(
        IEnumerable<Guid>? wordIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu từ vựng...", "Querying database..."));
        var words = await GetWordsForExportAsync(wordIds, cancellationToken);
        var sb = new StringBuilder();

        sb.AppendLine("Term,DefinitionVi,DefinitionEn,PartOfSpeech");

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
            var defVi = string.Join(" | ", meanings.Select(m => m.DefinitionVi));
            var defEn = string.Join(" | ", meanings.Where(m => !string.IsNullOrWhiteSpace(m.DefinitionEn)).Select(m => m.DefinitionEn!));
            var pos = string.Join(" | ", meanings.Select(m => m.PartOfSpeech.ToString()).Distinct());
            if (string.IsNullOrEmpty(pos)) pos = w.PrimaryPartOfSpeech.ToString();

            sb.AppendLine(string.Join(",",
                EscapeCsv(w.Term),
                EscapeCsv(defVi),
                EscapeCsv(defEn),
                EscapeCsv(pos)
            ));
        }

        progress?.Report(new ProgressReport(100, total, total, "Xuất CSV hoàn tất!", "Done."));
        return sb.ToString();
    }

    public async Task<string> ExportVocabularyOnlyTxtAsync(
        IEnumerable<Guid>? wordIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu từ vựng...", "Querying database..."));
        var words = await GetWordsForExportAsync(wordIds, cancellationToken);
        var sb = new StringBuilder();

        sb.AppendLine("# Lexora Vocabulary Only Export (Term <tab> Meaning)");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("# Format: Term <tab> DefinitionVi");
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
            var defVi = string.Join(" | ", meanings.Select(m => m.DefinitionVi));
            if (string.IsNullOrWhiteSpace(defVi)) defVi = w.Term;

            sb.AppendLine($"{w.Term}\t{defVi}");
        }

        progress?.Report(new ProgressReport(100, total, total, "Xuất TXT hoàn tất!", "Done."));
        return sb.ToString();
    }

    public async Task<string> ExportToAnkiTsvAsync(
        IEnumerable<Guid>? wordIds = null,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        progress?.Report(new ProgressReport(10, 0, 0, "Trích xuất dữ liệu Anki TSV...", "Querying database..."));
        var words = await GetWordsForExportAsync(wordIds, cancellationToken);
        var sb = new StringBuilder();

        sb.AppendLine("#separator:Tab");
        sb.AppendLine("#html:true");
        sb.AppendLine("#tags column:4");
        sb.AppendLine($"# Lexora Anki Flashcard Deck ({DateTime.UtcNow:yyyy-MM-dd})");
        sb.AppendLine();

        int current = 0;
        int total = words.Count;
        foreach (var w in words)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current++;
            if (current % 25 == 0 || current == total)
            {
                int pct = (int)((double)current / Math.Max(1, total) * 75) + 20;
                progress?.Report(new ProgressReport(pct, current, total, $"Đang xuất Anki TSV ({current}/{total})...", w.Term));
            }

            var frontParts = new List<string> { $"<b>{EscapeTsvHtml(w.Term)}</b>" };
            var phonetics = new List<string>();
            if (!string.IsNullOrWhiteSpace(w.PhoneticUk)) phonetics.Add($"UK {EscapeTsvHtml(w.PhoneticUk)}");
            if (!string.IsNullOrWhiteSpace(w.PhoneticUs)) phonetics.Add($"US {EscapeTsvHtml(w.PhoneticUs)}");
            if (phonetics.Count > 0)
            {
                frontParts.Add($"<br><small>{string.Join(" | ", phonetics)}</small>");
            }
            var levelBadge = w.Level != CefrLevel.None ? $" • {w.Level}" : string.Empty;
            frontParts.Add($"<br><i>[{w.PrimaryPartOfSpeech}{levelBadge}]</i>");
            var frontCol = string.Concat(frontParts);

            var backSegments = new List<string>();
            var orderedMeanings = (w.Meanings ?? []).OrderBy(m => m.OrderIndex).ToList();
            if (orderedMeanings.Count > 0)
            {
                foreach (var m in orderedMeanings)
                {
                    var line = $"<b>[{m.PartOfSpeech}]</b> {EscapeTsvHtml(m.DefinitionVi)}";
                    if (!string.IsNullOrWhiteSpace(m.DefinitionEn))
                    {
                        line += $" <i>({EscapeTsvHtml(m.DefinitionEn)})</i>";
                    }
                    backSegments.Add(line);

                    foreach (var ex in m.Examples ?? [])
                    {
                        var exLine = $"&nbsp;&nbsp;&bull; {EscapeTsvHtml(ex.SentenceEn)}";
                        if (!string.IsNullOrWhiteSpace(ex.SentenceVi))
                        {
                            exLine += $" &rarr; {EscapeTsvHtml(ex.SentenceVi)}";
                        }
                        backSegments.Add(exLine);
                    }
                }
            }
            else
            {
                backSegments.Add(EscapeTsvHtml(w.Term));
            }
            var backCol = string.Join("<br>", backSegments);

            var notesCol = string.IsNullOrWhiteSpace(w.Notes) ? string.Empty : EscapeTsvHtml(w.Notes);

            var tags = new List<string> { "lexora" };
            if (w.Level != CefrLevel.None) tags.Add($"cefr_{w.Level}");
            foreach (var wt in (w.WordTopics ?? []).Where(t => t.Topic != null))
            {
                tags.Add(SanitizeAnkiTag(wt.Topic.Name));
            }
            foreach (var wet in (w.WordExamTags ?? []).Where(e => e.ExamTag != null))
            {
                tags.Add(SanitizeAnkiTag(wet.ExamTag.Name));
            }
            var tagsCol = string.Join(" ", tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase));

            sb.AppendLine($"{frontCol}\t{backCol}\t{notesCol}\t{tagsCol}");
        }

        progress?.Report(new ProgressReport(100, total, total, "Xuất Anki TSV hoàn tất!", "Done."));
        return sb.ToString();
    }

    private static string EscapeTsvHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var encoded = WebUtility.HtmlEncode(input);
        return encoded
            .Replace("\t", " ")
            .Replace("\r\n", "<br>")
            .Replace("\n", "<br>")
            .Replace("\r", "<br>");
    }

    private static string SanitizeAnkiTag(string rawTag)
    {
        if (string.IsNullOrWhiteSpace(rawTag)) return string.Empty;
        return rawTag.Trim().Replace(' ', '_').Replace('\t', '_');
    }
}
