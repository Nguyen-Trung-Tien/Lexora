using System.Text.Json;
using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public partial class ExportImportService : IExportImportService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IUnitOfWork _unitOfWork;

    private const int MaxImportPayloadLength = 20_000_000; // 20 MB safety limit
    private const int ImportBatchSize = 500;

    public static readonly JsonSerializerOptions JsonOptions = SmartImportParser.JsonOptions;

    public static string StripMarkdownCodeFence(string input) => SmartImportParser.StripMarkdownCodeFence(input);

    public static List<WordExportModel> DeserializeWordsJson(string rawContent) => SmartImportParser.DeserializeWordsJson(rawContent);

    public ExportImportService(IVocabularyRepository vocabRepo, IUnitOfWork unitOfWork)
    {
        _vocabRepo = vocabRepo;
        _unitOfWork = unitOfWork;
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
            var defVi = m.ResolvedDefinitionVi;
            var matchMeaning = existing.Meanings.FirstOrDefault(em =>
                em.PartOfSpeech == m.PartOfSpeech &&
                em.DefinitionVi.Trim().Equals(defVi.Trim(), StringComparison.OrdinalIgnoreCase));

            if (matchMeaning == null)
            {
                var newMeaning = new WordMeaning
                {
                    WordId = existing.Id,
                    PartOfSpeech = m.PartOfSpeech != 0 ? m.PartOfSpeech : existing.PrimaryPartOfSpeech,
                    DefinitionVi = defVi,
                    DefinitionEn = m.ResolvedDefinitionEn,
                    Explanation = m.Explanation,
                    OrderIndex = existing.Meanings.Count
                };
                foreach (var ex in m.Examples)
                {
                    if (!string.IsNullOrWhiteSpace(ex.ResolvedSentenceEn))
                    {
                        newMeaning.Examples.Add(new ExampleSentence
                        {
                            WordMeaningId = newMeaning.Id,
                            SentenceEn = ex.ResolvedSentenceEn,
                            SentenceVi = ex.ResolvedSentenceVi
                        });
                    }
                }
                existing.Meanings.Add(newMeaning);
            }
            else
            {
                foreach (var ex in m.Examples)
                {
                    var senEn = ex.ResolvedSentenceEn;
                    if (!string.IsNullOrWhiteSpace(senEn) && !matchMeaning.Examples.Any(ee => ee.SentenceEn.Trim().Equals(senEn.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        matchMeaning.Examples.Add(new ExampleSentence
                        {
                            WordMeaningId = matchMeaning.Id,
                            SentenceEn = senEn,
                            SentenceVi = ex.ResolvedSentenceVi
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

    private static Dictionary<string, Topic> BuildTopicLookup(IEnumerable<Topic>? topics)
    {
        var dict = new Dictionary<string, Topic>(StringComparer.OrdinalIgnoreCase);
        if (topics != null)
        {
            foreach (var t in topics)
            {
                var key = t.Name.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(key) && !dict.ContainsKey(key))
                {
                    dict[key] = t;
                }
            }
        }
        return dict;
    }

    private static Dictionary<string, ExamTag> BuildExamTagLookup(IEnumerable<ExamTag>? examTags)
    {
        var dict = new Dictionary<string, ExamTag>(StringComparer.OrdinalIgnoreCase);
        if (examTags != null)
        {
            foreach (var et in examTags)
            {
                var key = et.Name.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(key) && !dict.ContainsKey(key))
                {
                    dict[key] = et;
                }
            }
        }
        return dict;
    }

    public static bool TryParsePartOfSpeech(string? input, out PartOfSpeech pos) => SmartImportParser.TryParsePartOfSpeech(input, out pos);

    public static PartOfSpeech ParsePartOfSpeech(string? input, PartOfSpeech defaultPos = PartOfSpeech.Noun) => SmartImportParser.ParsePartOfSpeech(input, defaultPos);

    public static List<PartOfSpeech> ParseMultiplePartsOfSpeech(string? input, PartOfSpeech fallback = PartOfSpeech.Noun) => SmartImportParser.ParseMultiplePartsOfSpeech(input, fallback);

    public static CefrLevel ParseCefrLevel(string? input, CefrLevel defaultLevel = CefrLevel.None) => SmartImportParser.ParseCefrLevel(input, defaultLevel);

    public static string NormalizeTerm(string? term) => SmartImportParser.NormalizeTerm(term);

    public static string CleanTerm(string term) => SmartImportParser.CleanTerm(term);

    public static ParsedSmartTxtRow? ParseSmartTxtLine(string rawLine) => SmartImportParser.ParseSmartTxtLine(rawLine);

    private static string SafeTruncate(string? value, int maxLength) => SmartImportParser.SafeTruncate(value, maxLength);

    private static string? SafeTruncateNullable(string? value, int maxLength) => SmartImportParser.SafeTruncateNullable(value, maxLength);

    private static List<WordMeaning> BuildWordMeanings(Guid wordId, SmartImportItemDto item)
    {
        var list = new List<WordMeaning>();
        if (item.RichMeanings != null && item.RichMeanings.Count > 0)
        {
            int mIdx = 0;
            foreach (var rm in item.RichMeanings)
            {
                var defVi = !string.IsNullOrWhiteSpace(rm.DefinitionVi)
                    ? rm.DefinitionVi.Trim()
                    : (!string.IsNullOrWhiteSpace(rm.DefinitionEn) ? rm.DefinitionEn.Trim() : item.Term.Trim());

                var meaning = new WordMeaning
                {
                    WordId = wordId,
                    PartOfSpeech = rm.PartOfSpeech != 0 ? rm.PartOfSpeech : (item.PartOfSpeech != 0 ? item.PartOfSpeech : PartOfSpeech.Noun),
                    DefinitionVi = SafeTruncate(defVi, 500),
                    DefinitionEn = SafeTruncateNullable(rm.DefinitionEn, 1000),
                    Explanation = SafeTruncateNullable(rm.Explanation, 1000),
                    OrderIndex = mIdx++
                };

                if (rm.Examples != null)
                {
                    foreach (var ex in rm.Examples)
                    {
                        if (!string.IsNullOrWhiteSpace(ex.SentenceEn))
                        {
                            meaning.Examples.Add(new ExampleSentence
                            {
                                WordMeaningId = meaning.Id,
                                SentenceEn = SafeTruncate(ex.SentenceEn, 1000),
                                SentenceVi = SafeTruncate(ex.SentenceVi ?? string.Empty, 1000),
                                AudioPath = ex.AudioPath
                            });
                        }
                    }
                }

                list.Add(meaning);
            }
        }
        else
        {
            var defVi = !string.IsNullOrWhiteSpace(item.DefinitionVi)
                ? item.DefinitionVi.Trim()
                : (!string.IsNullOrWhiteSpace(item.DefinitionEn) ? item.DefinitionEn.Trim() : item.Term.Trim());

            var meaning = new WordMeaning
            {
                WordId = wordId,
                PartOfSpeech = item.PartOfSpeech != 0 ? item.PartOfSpeech : PartOfSpeech.Noun,
                DefinitionVi = SafeTruncate(defVi, 500),
                DefinitionEn = SafeTruncateNullable(item.DefinitionEn, 1000),
                OrderIndex = 0
            };

            if (!string.IsNullOrWhiteSpace(item.Examples))
            {
                var exTokens = item.Examples.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var ex in exTokens)
                {
                    var parts = ex.Split(new[] { "=>" }, StringSplitOptions.RemoveEmptyEntries);
                    var en = parts.Length > 0 ? parts[0].Trim() : ex.Trim();
                    var vi = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                    if (!string.IsNullOrWhiteSpace(en))
                    {
                        meaning.Examples.Add(new ExampleSentence
                        {
                            WordMeaningId = meaning.Id,
                            SentenceEn = SafeTruncate(en, 1000),
                            SentenceVi = SafeTruncate(vi, 1000)
                        });
                    }
                }
            }

            list.Add(meaning);
        }

        return list;
    }

    private static string EscapeCsv(string field) => SmartImportParser.EscapeCsv(field);

    private static List<List<string>> ParseCsvRecords(string csvContent) => SmartImportParser.ParseCsvRecords(csvContent);

    private class VocabularyOnlyExportModel
    {
        public string Term { get; set; } = string.Empty;
        public List<MeaningOnlyExportModel> Meanings { get; set; } = new();
    }

    private class MeaningOnlyExportModel
    {
        public PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Noun;
        public string DefinitionVi { get; set; } = string.Empty;
        public string? DefinitionEn { get; set; }
    }

    private static List<string> ParseCsvLine(string line, char delimiter) => SmartImportParser.ParseCsvLine(line, delimiter);
}
