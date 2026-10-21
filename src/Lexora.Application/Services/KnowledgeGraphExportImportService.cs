using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class KnowledgeGraphExportImportService : IKnowledgeGraphExportImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IVocabularyKnowledgeGraphService _graphService;
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISmartSearchCacheService? _cacheService;

    public KnowledgeGraphExportImportService(
        IVocabularyKnowledgeGraphService graphService,
        IVocabularyRepository vocabRepo,
        IUnitOfWork unitOfWork,
        ISmartSearchCacheService? cacheService = null)
    {
        _graphService = graphService;
        _vocabRepo = vocabRepo;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<string> ExportToJsonAsync(Guid? centerWordId = null, CancellationToken cancellationToken = default)
    {
        var package = new KnowledgeGraphExportPackageDto();

        if (centerWordId.HasValue && centerWordId.Value != Guid.Empty)
        {
            var graph = await _graphService.BuildGraphAsync(centerWordId.Value, cancellationToken);
            if (graph != null)
            {
                package.CenterTerm = graph.CenterNode.Term;
                package.Nodes.Add(MapToExportNode(graph.CenterNode));

                foreach (var node in graph.ConnectedNodes)
                {
                    package.Nodes.Add(MapToExportNode(node));
                }

                foreach (var edge in graph.Edges)
                {
                    package.Edges.Add(new KnowledgeGraphExportEdgeDto
                    {
                        Source = edge.SourceTerm,
                        Target = edge.TargetTerm,
                        Relation = edge.RelationshipType.ToString(),
                        Weight = 0.85,
                        Explanation = edge.RelationshipLabel
                    });
                }
            }
        }
        else
        {
            // Global export of top vocabulary concepts
            var paged = await _graphService.GetConceptsPagedAsync(new KnowledgeGraphQueryDto(Page: 1, PageSize: 50), cancellationToken);
            package.CenterTerm = "All Concepts (Top 50)";

            var processedEdges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in paged.Items.Take(25))
            {
                var g = await _graphService.BuildGraphAsync(item.Id, cancellationToken);
                if (g == null) continue;

                if (!package.Nodes.Any(n => string.Equals(n.Term, g.CenterNode.Term, StringComparison.OrdinalIgnoreCase)))
                {
                    package.Nodes.Add(MapToExportNode(g.CenterNode));
                }

                foreach (var cn in g.ConnectedNodes)
                {
                    if (!package.Nodes.Any(n => string.Equals(n.Term, cn.Term, StringComparison.OrdinalIgnoreCase)))
                    {
                        package.Nodes.Add(MapToExportNode(cn));
                    }
                }

                foreach (var edge in g.Edges)
                {
                    var key = $"{edge.SourceTerm}->{edge.TargetTerm}:{edge.RelationshipType}";
                    if (processedEdges.Add(key))
                    {
                        package.Edges.Add(new KnowledgeGraphExportEdgeDto
                        {
                            Source = edge.SourceTerm,
                            Target = edge.TargetTerm,
                            Relation = edge.RelationshipType.ToString(),
                            Weight = 0.85,
                            Explanation = edge.RelationshipLabel
                        });
                    }
                }
            }
        }

        return JsonSerializer.Serialize(package, JsonOptions);
    }

    public async Task<string> ExportToCsvAsync(Guid? centerWordId = null, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SourceTerm,TargetTerm,Relationship,Weight,Level,Explanation");

        if (centerWordId.HasValue && centerWordId.Value != Guid.Empty)
        {
            var graph = await _graphService.BuildGraphAsync(centerWordId.Value, cancellationToken);
            if (graph != null)
            {
                foreach (var edge in graph.Edges)
                {
                    var targetNode = graph.ConnectedNodes.FirstOrDefault(n => n.Id == edge.TargetId);
                    var level = targetNode != null ? targetNode.Level.ToString() : "None";
                    sb.AppendLine($"{EscapeCsv(edge.SourceTerm)},{EscapeCsv(edge.TargetTerm)},{edge.RelationshipType},0.85,{level},{EscapeCsv(edge.RelationshipLabel)}");
                }
            }
        }
        else
        {
            var paged = await _graphService.GetConceptsPagedAsync(new KnowledgeGraphQueryDto(Page: 1, PageSize: 30), cancellationToken);
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in paged.Items)
            {
                var g = await _graphService.BuildGraphAsync(item.Id, cancellationToken);
                if (g == null) continue;

                foreach (var edge in g.Edges)
                {
                    var key = $"{edge.SourceTerm}->{edge.TargetTerm}:{edge.RelationshipType}";
                    if (processed.Add(key))
                    {
                        var targetNode = g.ConnectedNodes.FirstOrDefault(n => n.Id == edge.TargetId);
                        var level = targetNode != null ? targetNode.Level.ToString() : "None";
                        sb.AppendLine($"{EscapeCsv(edge.SourceTerm)},{EscapeCsv(edge.TargetTerm)},{edge.RelationshipType},0.85,{level},{EscapeCsv(edge.RelationshipLabel)}");
                    }
                }
            }
        }

        return sb.ToString();
    }

    public async Task<string> ExportToMarkdownMindmapAsync(Guid? centerWordId = null, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        if (centerWordId.HasValue && centerWordId.Value != Guid.Empty)
        {
            var graph = await _graphService.BuildGraphAsync(centerWordId.Value, cancellationToken);
            if (graph != null)
            {
                AppendGraphMarkdown(sb, graph);
            }
        }
        else
        {
            sb.AppendLine("# Lexora Semantic Knowledge Network Mindmap");
            sb.AppendLine($"> Exported on: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Multi-Word Mindmap\n");

            var paged = await _graphService.GetConceptsPagedAsync(new KnowledgeGraphQueryDto(Page: 1, PageSize: 20), cancellationToken);
            foreach (var item in paged.Items)
            {
                var g = await _graphService.BuildGraphAsync(item.Id, cancellationToken);
                if (g != null)
                {
                    AppendGraphMarkdown(sb, g);
                    sb.AppendLine("\n---\n");
                }
            }
        }

        return sb.ToString();
    }

    private static void AppendGraphMarkdown(StringBuilder sb, VocabularyKnowledgeGraphDto graph)
    {
        var c = graph.CenterNode;
        sb.AppendLine($"# {c.Term}");
        sb.AppendLine($"- **CEFR**: {c.Level} | **POS**: {c.PartOfSpeech} | **IPA**: {c.Ipa ?? "—"}");
        sb.AppendLine($"- **Nghĩa tiếng Việt**: {c.MeaningVi}");
        if (!string.IsNullOrWhiteSpace(c.MeaningEn)) sb.AppendLine($"- **Definition (EN)**: {c.MeaningEn}");

        var syns = graph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.Synonym).ToList();
        if (syns.Count > 0)
        {
            sb.AppendLine("\n## 🔄 Synonyms (Từ đồng nghĩa)");
            foreach (var s in syns)
                sb.AppendLine($"- **{s.Term}** ({s.Level} - {s.PartOfSpeech}): {s.MeaningVi} *({s.Explanation})*");
        }

        var ants = graph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.Antonym).ToList();
        if (ants.Count > 0)
        {
            sb.AppendLine("\n## ⚡ Antonyms (Từ trái nghĩa)");
            foreach (var a in ants)
                sb.AppendLine($"- **{a.Term}** ({a.Level} - {a.PartOfSpeech}): {a.MeaningVi} *({a.Explanation})*");
        }

        var fams = graph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.WordFamily).ToList();
        if (fams.Count > 0)
        {
            sb.AppendLine("\n## 🌳 Word Family (Họ từ)");
            foreach (var f in fams)
                sb.AppendLine($"- **{f.Term}** ({f.PartOfSpeech}): {f.MeaningVi} *({f.Explanation})*");
        }

        var colls = graph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.Collocation).ToList();
        if (colls.Count > 0)
        {
            sb.AppendLine("\n## 🔗 Collocations (Cụm từ kết hợp)");
            foreach (var cl in colls)
                sb.AppendLine($"- **{cl.Term}**: {cl.MeaningVi}");
        }

        var topics = graph.ConnectedNodes.Where(n => n.NodeType == KnowledgeGraphNodeType.TopicCluster).ToList();
        if (topics.Count > 0)
        {
            sb.AppendLine("\n## 🌐 Same Domain (Cùng chủ đề)");
            foreach (var t in topics)
                sb.AppendLine($"- **{t.Term}** ({t.Level}): {t.MeaningVi}");
        }
    }

    public async Task<KnowledgeGraphImportAnalysisResultDto> AnalyzeImportAsync(string content, string format, CancellationToken cancellationToken = default)
    {
        var items = new List<KnowledgeGraphImportItemDto>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            warnings.Add("Nội dung import trống.");
            return new KnowledgeGraphImportAnalysisResultDto(items, 0, 0, 0, 0, warnings);
        }

        var trimmed = content.Trim();
        bool isJson = format.Equals("json", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("{") || trimmed.StartsWith("[");

        if (isJson)
        {
            ParseJsonImport(trimmed, items, warnings);
            if (items.Count == 0 && (trimmed.Contains(',') || trimmed.Contains(';') || trimmed.Contains('\t') || trimmed.Contains('\n')))
            {
                // Fallback attempt with CSV if JSON parser yielded 0 items
                var csvItems = new List<KnowledgeGraphImportItemDto>();
                var csvWarnings = new List<string>();
                ParseCsvImport(trimmed, csvItems, csvWarnings);
                if (csvItems.Count > 0)
                {
                    items.AddRange(csvItems);
                    warnings.Clear();
                }
            }
        }
        else
        {
            ParseCsvImport(trimmed, items, warnings);
            if (items.Count == 0 && (trimmed.StartsWith("{") || trimmed.StartsWith("[")))
            {
                // Fallback attempt with JSON if CSV parser yielded 0 items
                var jsonItems = new List<KnowledgeGraphImportItemDto>();
                var jsonWarnings = new List<string>();
                ParseJsonImport(trimmed, jsonItems, jsonWarnings);
                if (jsonItems.Count > 0)
                {
                    items.AddRange(jsonItems);
                    warnings.Clear();
                }
            }
        }

        // Validate items against database
        var distinctTerms = items
            .SelectMany(i => new[] { i.SourceTerm, i.TargetTerm })
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingWordsMap = new Dictionary<string, Word>(StringComparer.OrdinalIgnoreCase);
        foreach (var term in distinctTerms)
        {
            var match = await _vocabRepo.GetByTermAsync(term, cancellationToken);
            if (match != null)
            {
                existingWordsMap[term] = match;
            }
        }

        int duplicateCount = 0;
        int missingWordsCount = 0;
        int validCount = 0;
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var it in items)
        {
            var key = $"{it.SourceTerm}->{it.TargetTerm}:{it.RelationshipType}";
            if (!seenKeys.Add(key))
            {
                it.IsDuplicate = true;
                it.IsValid = false;
                it.StatusMessage = "Quan hệ bị trùng lặp trong tệp dữ liệu";
                duplicateCount++;
                continue;
            }

            it.SourceExists = existingWordsMap.ContainsKey(it.SourceTerm);
            it.TargetExists = existingWordsMap.ContainsKey(it.TargetTerm);

            if (!it.SourceExists || !it.TargetExists)
            {
                missingWordsCount++;
                it.StatusMessage = !it.SourceExists && !it.TargetExists
                    ? $"Cả hai từ '{it.SourceTerm}' & '{it.TargetTerm}' chưa có trong từ điển (sẽ tự động tạo nếu được chọn)"
                    : !it.SourceExists
                        ? $"Từ nguồn '{it.SourceTerm}' chưa có trong từ điển"
                        : $"Từ đích '{it.TargetTerm}' chưa có trong từ điển";
            }
            else
            {
                it.StatusMessage = "Hợp lệ - Sẵn sàng liên kết";
            }

            it.IsValid = true;
            validCount++;
        }

        return new KnowledgeGraphImportAnalysisResultDto(
            items,
            items.Count,
            validCount,
            duplicateCount,
            missingWordsCount,
            warnings);
    }

    private static void ParseJsonImport(string json, List<KnowledgeGraphImportItemDto> items, List<string> warnings)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("edges", out var edgesElem) && edgesElem.ValueKind == JsonValueKind.Array)
            {
                int row = 1;
                foreach (var edge in edgesElem.EnumerateArray())
                {
                    var src = edge.TryGetProperty("source", out var sProp) ? sProp.GetString() ?? "" : "";
                    var tgt = edge.TryGetProperty("target", out var tProp) ? tProp.GetString() ?? "" : "";
                    var relStr = edge.TryGetProperty("relation", out var rProp) ? rProp.GetString() ?? "Synonym" : "Synonym";
                    var weight = edge.TryGetProperty("weight", out var wProp) ? wProp.GetDouble() : 0.85;
                    var expl = edge.TryGetProperty("explanation", out var eProp) ? eProp.GetString() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(tgt)) continue;

                    items.Add(new KnowledgeGraphImportItemDto
                    {
                        RowNumber = row++,
                        SourceTerm = src.Trim().ToLowerInvariant(),
                        TargetTerm = tgt.Trim().ToLowerInvariant(),
                        RelationshipType = ParseRelationType(relStr),
                        Weight = weight,
                        Explanation = expl
                    });
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                int row = 1;
                foreach (var item in root.EnumerateArray())
                {
                    var src = item.TryGetProperty("source", out var sProp) ? sProp.GetString() ?? "" : "";
                    var tgt = item.TryGetProperty("target", out var tProp) ? tProp.GetString() ?? "" : "";
                    var relStr = item.TryGetProperty("relation", out var rProp) ? rProp.GetString() ?? "Synonym" : "Synonym";
                    var expl = item.TryGetProperty("explanation", out var eProp) ? eProp.GetString() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(tgt)) continue;

                    items.Add(new KnowledgeGraphImportItemDto
                    {
                        RowNumber = row++,
                        SourceTerm = src.Trim().ToLowerInvariant(),
                        TargetTerm = tgt.Trim().ToLowerInvariant(),
                        RelationshipType = ParseRelationType(relStr),
                        Weight = 0.85,
                        Explanation = expl
                    });
                }
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Lỗi định dạng JSON: {ex.Message}");
        }
    }

    private static void ParseCsvImport(string csv, List<KnowledgeGraphImportItemDto> items, List<string> warnings)
    {
        var lines = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        int rowNumber = 1;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            // Header check
            if (line.StartsWith("SourceTerm", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Source,", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = ParseCsvLine(line);
            if (parts.Count < 2) continue;

            var src = parts[0].Trim().ToLowerInvariant();
            var tgt = parts[1].Trim().ToLowerInvariant();
            var rel = parts.Count > 2 ? parts[2].Trim() : "Synonym";
            double weight = 0.85;
            if (parts.Count > 3 && double.TryParse(parts[3], out var parsedWeight))
            {
                weight = parsedWeight;
            }
            var expl = parts.Count > 5 ? parts[5].Trim() : (parts.Count > 4 ? parts[4].Trim() : "");

            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(tgt)) continue;

            items.Add(new KnowledgeGraphImportItemDto
            {
                RowNumber = rowNumber++,
                SourceTerm = src,
                TargetTerm = tgt,
                RelationshipType = ParseRelationType(rel),
                Weight = weight,
                Explanation = expl
            });
        }
    }

    public async Task<KnowledgeGraphImportCommitResultDto> CommitImportAsync(
        KnowledgeGraphImportAnalysisResultDto analysis,
        bool autoCreateMissingWords,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        int importedCount = 0;
        int updatedWordsCount = 0;
        int createdWordsCount = 0;

        try
        {
            var validItems = analysis.Items.Where(i => i.IsValid && !i.IsDuplicate).ToList();
            if (validItems.Count == 0)
            {
                return new KnowledgeGraphImportCommitResultDto(true, 0, 0, 0, errors);
            }

            var wordsToProcess = validItems
                .SelectMany(i => new[] { i.SourceTerm, i.TargetTerm })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var wordsMap = new Dictionary<string, Word>(StringComparer.OrdinalIgnoreCase);
            foreach (var term in wordsToProcess)
            {
                var w = await _vocabRepo.GetByTermAsync(term, cancellationToken);
                if (w != null)
                {
                    wordsMap[term] = w;
                }
                else if (autoCreateMissingWords)
                {
                    // Create minimal word entry with fully initialized ReviewCard for spaced repetition
                    var newWord = new Word
                    {
                        Id = Guid.NewGuid(),
                        Term = term,
                        NormalizedTerm = term.Trim().ToLowerInvariant(),
                        Level = CefrLevel.B1,
                        PrimaryPartOfSpeech = PartOfSpeech.Noun,
                        Notes = string.Empty,
                        Meanings = new List<WordMeaning>()
                    };

                    newWord.Meanings.Add(new WordMeaning
                    {
                        Id = Guid.NewGuid(),
                        WordId = newWord.Id,
                        Word = newWord,
                        DefinitionVi = $"Khái niệm liên kết trong Semantic Knowledge Graph: {term}",
                        DefinitionEn = $"Semantic network concept: {term}",
                        PartOfSpeech = PartOfSpeech.Noun
                    });

                    newWord.ReviewCard = new ReviewCard
                    {
                        WordId = newWord.Id,
                        Word = newWord,
                        Status = LearningStatus.New,
                        EaseFactor = 2.5,
                        IntervalDays = 0,
                        Repetitions = 0,
                        Lapses = 0,
                        NextReviewAt = DateTime.UtcNow
                    };

                    await _vocabRepo.AddAsync(newWord, cancellationToken);
                    wordsMap[term] = newWord;
                    createdWordsCount++;
                }
            }

            // Apply relations into word notes atomically
            var modifiedWords = new HashSet<Word>();

            foreach (var item in validItems)
            {
                if (!wordsMap.TryGetValue(item.SourceTerm, out var srcWord)) continue;
                if (!wordsMap.TryGetValue(item.TargetTerm, out var tgtWord)) continue;

                var prefix = item.RelationshipType switch
                {
                    KnowledgeGraphNodeType.Synonym => "syn:",
                    KnowledgeGraphNodeType.Antonym => "ant:",
                    KnowledgeGraphNodeType.WordFamily => "family:",
                    KnowledgeGraphNodeType.Collocation => "colloc:",
                    _ => "rel:"
                };

                // Append relation tag if not already present
                var tag = $"[{prefix} {item.TargetTerm}]";
                if (string.IsNullOrWhiteSpace(srcWord.Notes))
                {
                    srcWord.Notes = tag;
                    modifiedWords.Add(srcWord);
                }
                else if (!srcWord.Notes.Contains(item.TargetTerm, StringComparison.OrdinalIgnoreCase))
                {
                    srcWord.Notes += $" {tag}";
                    modifiedWords.Add(srcWord);
                }

                importedCount++;
            }

            updatedWordsCount = modifiedWords.Count;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _cacheService?.InvalidateVocabulary();

            return new KnowledgeGraphImportCommitResultDto(
                true,
                importedCount,
                updatedWordsCount,
                createdWordsCount,
                errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Lỗi trong quá trình commit dữ liệu: {ex.Message}");
            return new KnowledgeGraphImportCommitResultDto(false, importedCount, updatedWordsCount, createdWordsCount, errors);
        }
    }

    public string GetSampleTemplate(string format)
    {
        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            return @"{
  ""title"": ""Sample Lexora Semantic Graph"",
  ""edges"": [
    {
      ""source"": ""abandon"",
      ""target"": ""desert"",
      ""relation"": ""Synonym"",
      ""weight"": 0.92,
      ""explanation"": ""Từ bỏ, bỏ rơi nơi chốn hoặc người""
    },
    {
      ""source"": ""abandon"",
      ""target"": ""maintain"",
      ""relation"": ""Antonym"",
      ""weight"": 0.95,
      ""explanation"": ""Từ bỏ vs. Duy trì""
    },
    {
      ""source"": ""abandon"",
      ""target"": ""abandonment"",
      ""relation"": ""WordFamily"",
      ""weight"": 0.90,
      ""explanation"": ""Gốc từ abandon (Danh từ)""
    },
    {
      ""source"": ""abandon"",
      ""target"": ""abandon hope"",
      ""relation"": ""Collocation"",
      ""weight"": 0.88,
      ""explanation"": ""Cụm cố định: từ bỏ hy vọng""
    }
  ]
}";
        }

        return @"SourceTerm,TargetTerm,Relationship,Weight,Level,Explanation
abandon,desert,Synonym,0.92,B1,Từ bỏ hoặc bỏ rơi
abandon,maintain,Antonym,0.95,B2,Từ bỏ vs. Duy trì
abandon,abandonment,WordFamily,0.90,B2,Danh từ cùng gốc từ
abandon,abandon hope,Collocation,0.88,B2,Cụm từ bỏ hy vọng";
    }

    private static KnowledgeGraphExportNodeDto MapToExportNode(GraphNodeDto node)
    {
        return new KnowledgeGraphExportNodeDto
        {
            Term = node.Term,
            Level = node.Level.ToString(),
            PartOfSpeech = node.PartOfSpeech.ToString(),
            MeaningVi = node.MeaningVi,
            MeaningEn = node.MeaningEn,
            Ipa = node.Ipa
        };
    }

    private static KnowledgeGraphNodeType ParseRelationType(string str)
    {
        var s = str.Trim().ToLowerInvariant();
        if (s.Contains("ant") || s.Contains("trái")) return KnowledgeGraphNodeType.Antonym;
        if (s.Contains("fam") || s.Contains("họ")) return KnowledgeGraphNodeType.WordFamily;
        if (s.Contains("colloc") || s.Contains("cụm")) return KnowledgeGraphNodeType.Collocation;
        if (s.Contains("topic") || s.Contains("chủ đề")) return KnowledgeGraphNodeType.TopicCluster;
        return KnowledgeGraphNodeType.Synonym;
    }

    private static string EscapeCsv(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Contains(',') || text.Contains('"') || text.Contains('\n'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
        return text;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    current.Append('\"');
                    i++;
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
}
