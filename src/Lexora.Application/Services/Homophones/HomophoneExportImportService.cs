using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class HomophoneExportImportService : IHomophoneExportImportService
{
    private readonly IHomophoneRepository _homophoneRepo;
    private readonly IUnitOfWork _unitOfWork;

    private const int MaxImportPayloadLength = 20_000_000; // 20 MB ceiling per AGENTS.md rule 6.3

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public HomophoneExportImportService(IHomophoneRepository homophoneRepo, IUnitOfWork unitOfWork)
    {
        _homophoneRepo = homophoneRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> ExportToJsonAsync(IEnumerable<Guid>? groupIds = null, CancellationToken cancellationToken = default)
    {
        var groups = await GetGroupsForExportAsync(groupIds, cancellationToken);
        var exportData = groups.Select(g => new HomophoneImportExportGroupDto
        {
            Title = g.Title,
            PhoneticIpa = g.PhoneticIpa,
            Type = g.Type.ToString(),
            Level = g.DifficultyLevel.ToString(),
            ExamTag = g.ExamTag.ToString(),
            DifferentiationTip = g.DifferentiationTip,
            Notes = g.Notes,
            Items = g.Items.Select(i => new HomophoneImportExportItemDto
            {
                Term = i.Term,
                PartOfSpeech = i.PartOfSpeech.ToString(),
                MeaningVi = i.MeaningVi,
                MeaningEn = i.MeaningEn,
                PhoneticSpecific = i.PhoneticSpecific,
                ExampleSentence = i.ExampleSentence,
                ExampleTranslation = i.ExampleTranslation
            }).ToList()
        }).ToList();

        return JsonSerializer.Serialize(exportData, JsonOptions);
    }

    public async Task<string> ExportToCsvAsync(IEnumerable<Guid>? groupIds = null, CancellationToken cancellationToken = default)
    {
        var groups = await GetGroupsForExportAsync(groupIds, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("GroupTitle,Phonetic,Type,Level,ExamTag,DifferentiationTip,Term,PartOfSpeech,MeaningVi,MeaningEn,PhoneticSpecific,ExampleSentence,ExampleTranslation");

        foreach (var group in groups)
        {
            if (group.Items.Count == 0)
            {
                sb.AppendLine($"{EscapeCsv(group.Title)},{EscapeCsv(group.PhoneticIpa)},{EscapeCsv(group.Type.ToString())},{EscapeCsv(group.DifficultyLevel.ToString())},{EscapeCsv(group.ExamTag.ToString())},{EscapeCsv(group.DifferentiationTip ?? string.Empty)},,,,,,,");
            }
            else
            {
                foreach (var item in group.Items)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(group.Title),
                        EscapeCsv(group.PhoneticIpa),
                        EscapeCsv(group.Type.ToString()),
                        EscapeCsv(group.DifficultyLevel.ToString()),
                        EscapeCsv(group.ExamTag.ToString()),
                        EscapeCsv(group.DifferentiationTip ?? string.Empty),
                        EscapeCsv(item.Term),
                        EscapeCsv(item.PartOfSpeech.ToString()),
                        EscapeCsv(item.MeaningVi),
                        EscapeCsv(item.MeaningEn ?? string.Empty),
                        EscapeCsv(item.PhoneticSpecific ?? string.Empty),
                        EscapeCsv(item.ExampleSentence ?? string.Empty),
                        EscapeCsv(item.ExampleTranslation ?? string.Empty)
                    ));
                }
            }
        }

        return sb.ToString();
    }

    public async Task<string> ExportToTxtAsync(IEnumerable<Guid>? groupIds = null, CancellationToken cancellationToken = default)
    {
        var groups = await GetGroupsForExportAsync(groupIds, cancellationToken);
        var sb = new StringBuilder();

        foreach (var group in groups)
        {
            var itemStrings = group.Items.Select(i => $"{i.Term} ({i.PartOfSpeech}): {i.MeaningVi}");
            var itemsMerged = string.Join(" || ", itemStrings);
            var tip = !string.IsNullOrWhiteSpace(group.DifferentiationTip) ? $" // Mẹo: {group.DifferentiationTip}" : string.Empty;
            sb.AppendLine($"{group.Title} [{group.PhoneticIpa}] => {itemsMerged}{tip}");
        }

        return sb.ToString();
    }

    public async Task<SmartHomophoneImportAnalysisDto> AnalyzeImportAsync(string content, string? fileNameHint = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SmartHomophoneImportAnalysisDto
            {
                DetectedFormat = DataFormat.Unknown,
                ParseErrors = ["Nội dung nhập trống. Vui lòng cung cấp file hoặc dán văn bản."]
            };
        }

        if (content.Length > MaxImportPayloadLength)
        {
            return new SmartHomophoneImportAnalysisDto
            {
                DetectedFormat = DataFormat.Unknown,
                ParseErrors = [$"Dung lượng dữ liệu vượt quá giới hạn an toàn 20MB ({content.Length:N0} bytes)."]
            };
        }

        var format = DetectFormat(content, fileNameHint);
        var parsedGroups = new List<HomophoneImportExportGroupDto>();
        var errors = new List<string>();

        try
        {
            switch (format)
            {
                case DataFormat.Json:
                    parsedGroups = ParseJson(content, errors);
                    break;
                case DataFormat.Csv:
                    parsedGroups = ParseCsv(content, errors);
                    break;
                case DataFormat.Txt:
                default:
                    parsedGroups = ParseTxt(content, errors);
                    format = DataFormat.Txt;
                    break;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Lỗi định dạng: {ex.Message}");
        }

        var existingTitles = await _homophoneRepo.GetAllGroupTitlesAsync(cancellationToken);
        var previewList = new List<SmartHomophonePreviewItemDto>();
        int rowNumber = 1;
        int duplicateCount = 0;
        int validCount = 0;
        int totalWords = 0;

        foreach (var grp in parsedGroups)
        {
            var isDuplicate = existingTitles.Contains(grp.Title.Trim().ToLowerInvariant());
            if (isDuplicate) duplicateCount++;

            var isValid = !string.IsNullOrWhiteSpace(grp.Title) && grp.Items.Count > 0;
            if (isValid) validCount++;

            totalWords += grp.Items.Count;
            var summary = string.Join(" / ", grp.Items.Select(i => i.Term));

            previewList.Add(new SmartHomophonePreviewItemDto
            {
                RowNumber = rowNumber++,
                Title = grp.Title,
                PhoneticIpa = grp.PhoneticIpa,
                Type = grp.Type,
                Level = grp.Level,
                DifferentiationTip = grp.DifferentiationTip,
                ItemCount = grp.Items.Count,
                ItemsSummary = summary,
                IsDuplicate = isDuplicate,
                IsValid = isValid,
                ValidationMessage = !isValid ? "Thiếu tiêu đề hoặc danh sách từ thành phần" : (isDuplicate ? "Nhóm từ đã tồn tại" : null),
                RawGroup = grp
            });
        }

        return new SmartHomophoneImportAnalysisDto
        {
            TotalGroupsFound = parsedGroups.Count,
            ValidGroupsCount = validCount,
            DuplicateGroupsCount = duplicateCount,
            TotalWordsCount = totalWords,
            DetectedFormat = format,
            PreviewItems = previewList,
            ParseErrors = errors
        };
    }

    public async Task<ImportResult> CommitImportAsync(SmartHomophoneCommitRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        int importedCount = 0;
        int updatedCount = 0;
        int skippedCount = 0;
        var errors = new List<string>();

        var titlesToImport = request.GroupsToCommit
            .Select(g => g.Title.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingGroupsDict = await _homophoneRepo.GetGroupsByTitlesWithItemsAsync(titlesToImport, cancellationToken);

        foreach (var groupDto in request.GroupsToCommit)
        {
            if (string.IsNullOrWhiteSpace(groupDto.Title) || groupDto.Items.Count == 0)
            {
                skippedCount++;
                continue;
            }

            var titleKey = groupDto.Title.Trim().ToLowerInvariant();
            if (existingGroupsDict.TryGetValue(titleKey, out var existingGroup))
            {
                if (request.ConflictResolution == ImportConflictResolution.Skip)
                {
                    skippedCount++;
                    continue;
                }

                // Overwrite mode: Update group metadata and merge/replace items
                existingGroup.PhoneticIpa = !string.IsNullOrWhiteSpace(groupDto.PhoneticIpa) ? groupDto.PhoneticIpa : existingGroup.PhoneticIpa;
                existingGroup.DifferentiationTip = !string.IsNullOrWhiteSpace(groupDto.DifferentiationTip) ? groupDto.DifferentiationTip : existingGroup.DifferentiationTip;
                existingGroup.Notes = !string.IsNullOrWhiteSpace(groupDto.Notes) ? groupDto.Notes : existingGroup.Notes;
                existingGroup.UpdatedAt = DateTime.UtcNow;

                _homophoneRepo.RemoveItems(existingGroup.Items);
                existingGroup.Items.Clear();

                foreach (var itemDto in groupDto.Items)
                {
                    if (string.IsNullOrWhiteSpace(itemDto.Term)) continue;
                    existingGroup.Items.Add(new HomophoneItem
                    {
                        HomophoneGroupId = existingGroup.Id,
                        Term = itemDto.Term.Trim(),
                        NormalizedTerm = itemDto.Term.Trim().ToLowerInvariant(),
                        PartOfSpeech = ParseEnum(itemDto.PartOfSpeech, PartOfSpeech.Noun),
                        MeaningVi = itemDto.MeaningVi?.Trim() ?? string.Empty,
                        MeaningEn = itemDto.MeaningEn?.Trim(),
                        PhoneticSpecific = itemDto.PhoneticSpecific?.Trim(),
                        ExampleSentence = itemDto.ExampleSentence?.Trim(),
                        ExampleTranslation = itemDto.ExampleTranslation?.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await _homophoneRepo.UpdateAsync(existingGroup, cancellationToken);
                updatedCount++;
            }
            else
            {
                // New group
                var newGroup = new HomophoneGroup
                {
                    Title = groupDto.Title.Trim(),
                    PhoneticIpa = groupDto.PhoneticIpa?.Trim() ?? string.Empty,
                    Type = ParseEnum(groupDto.Type, HomophoneType.ExactHomophone),
                    DifficultyLevel = ParseEnum(groupDto.Level, CefrLevel.None),
                    ExamTag = ParseEnum(groupDto.ExamTag, ExamType.General),
                    DifferentiationTip = groupDto.DifferentiationTip?.Trim(),
                    Notes = groupDto.Notes?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                foreach (var itemDto in groupDto.Items)
                {
                    if (string.IsNullOrWhiteSpace(itemDto.Term)) continue;
                    newGroup.Items.Add(new HomophoneItem
                    {
                        HomophoneGroupId = newGroup.Id,
                        Term = itemDto.Term.Trim(),
                        NormalizedTerm = itemDto.Term.Trim().ToLowerInvariant(),
                        PartOfSpeech = ParseEnum(itemDto.PartOfSpeech, PartOfSpeech.Noun),
                        MeaningVi = itemDto.MeaningVi?.Trim() ?? string.Empty,
                        MeaningEn = itemDto.MeaningEn?.Trim(),
                        PhoneticSpecific = itemDto.PhoneticSpecific?.Trim(),
                        ExampleSentence = itemDto.ExampleSentence?.Trim(),
                        ExampleTranslation = itemDto.ExampleTranslation?.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await _homophoneRepo.AddAsync(newGroup, cancellationToken);
                importedCount++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ImportResult(importedCount, skippedCount, errors, updatedCount);
    }

    public string GetSampleTemplate(string format)
    {
        return (format?.ToLowerInvariant()) switch
        {
            "json" => """
                [
                  {
                    "title": "bare / bear",
                    "phoneticIpa": "/beə(r)/",
                    "type": "ExactHomophone",
                    "level": "B1",
                    "examTag": "TOEIC",
                    "differentiationTip": "'Bare' nghĩa là trần trụi/trống rỗng. 'Bear' nghĩa là con gấu hoặc chịu đựng.",
                    "items": [
                      {
                        "term": "bare",
                        "partOfSpeech": "Adjective",
                        "meaningVi": "trần trụi, trống không",
                        "meaningEn": "not clothed or covered",
                        "exampleSentence": "He walked on the cold floor with bare feet.",
                        "exampleTranslation": "Anh ấy đi chân trần trên sàn nhà lạnh."
                      },
                      {
                        "term": "bear",
                        "partOfSpeech": "Verb",
                        "meaningVi": "chịu đựng",
                        "meaningEn": "to endure or carry",
                        "exampleSentence": "I cannot bear this noise any longer.",
                        "exampleTranslation": "Tôi không thể chịu nổi tiếng ồn này thêm nữa."
                      }
                    ]
                  },
                  {
                    "title": "flour / flower",
                    "phoneticIpa": "/flaʊ.ər/",
                    "type": "ExactHomophone",
                    "level": "A2",
                    "examTag": "General",
                    "differentiationTip": "'Flour' là bột mì (dùng làm bánh). 'Flower' là bông hoa.",
                    "items": [
                      {
                        "term": "flour",
                        "partOfSpeech": "Noun",
                        "meaningVi": "bột mì",
                        "exampleSentence": "We need two cups of flour to bake the cake.",
                        "exampleTranslation": "Chúng ta cần hai cốc bột mì để nướng bánh."
                      },
                      {
                        "term": "flower",
                        "partOfSpeech": "Noun",
                        "meaningVi": "bông hoa",
                        "exampleSentence": "He gave her a bunch of red flowers.",
                        "exampleTranslation": "Anh ấy tặng cô ấy một bó hoa màu đỏ."
                      }
                    ]
                  }
                ]
                """,
            "csv" => """
                GroupTitle,Phonetic,Type,Level,ExamTag,DifferentiationTip,Term,PartOfSpeech,MeaningVi,MeaningEn,PhoneticSpecific,ExampleSentence,ExampleTranslation
                "bare / bear","/beə(r)/",ExactHomophone,B1,TOEIC,"Bare: trần trụi; Bear: chịu đựng",bare,Adjective,"trần trụi, trống rỗng","not covered",,"He walked with bare feet.","Anh ấy đi chân trần."
                "bare / bear","/beə(r)/",ExactHomophone,B1,TOEIC,"Bare: trần trụi; Bear: chịu đựng",bear,Verb,"chịu đựng","to endure",,"I cannot bear this pain.","Tôi không thể chịu nổi nỗi đau này."
                "knight / night","/naɪt/",ExactHomophone,B1,IELTS,"Knight: hiệp sĩ; Night: ban đêm",knight,Noun,"hiệp sĩ","medieval warrior",,"The brave knight protected the castle.","Người hiệp sĩ dũng cảm đã bảo vệ lâu đài."
                "knight / night","/naɪt/",ExactHomophone,B1,IELTS,"Knight: hiệp sĩ; Night: ban đêm",night,Noun,"ban đêm","period of darkness",,"The stars shine brightly at night.","Những vì sao tỏa sáng rực rỡ vào ban đêm."
                """,
            _ => """
                bare / bear [/beə(r)/] => bare (Adjective): trần trụi || bear (Verb): chịu đựng // Mẹo: Bare là trần trụi, Bear là chịu đựng
                knight / night [/naɪt/] => knight (Noun): hiệp sĩ || night (Noun): ban đêm // Mẹo: Knight có k câm nghĩa là hiệp sĩ
                flour / flower [/flaʊ.ər/] => flour (Noun): bột mì || flower (Noun): bông hoa // Mẹo: Flour để làm bánh, Flower để ngắm
                """
        };
    }

    private async Task<IReadOnlyList<HomophoneGroup>> GetGroupsForExportAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken)
    {
        var all = await _homophoneRepo.GetAllWithItemsAsync(cancellationToken);
        if (groupIds != null)
        {
            var set = groupIds.ToHashSet();
            if (set.Count > 0)
            {
                return all.Where(g => set.Contains(g.Id)).ToList();
            }
        }
        return all;
    }

    private static DataFormat DetectFormat(string content, string? fileNameHint)
    {
        var ext = Path.GetExtension(fileNameHint ?? string.Empty).ToLowerInvariant();
        if (ext == ".json") return DataFormat.Json;
        if (ext == ".csv") return DataFormat.Csv;
        if (ext == ".txt" || ext == ".tsv") return DataFormat.Txt;

        var trimmed = content.TrimStart();
        if (trimmed.StartsWith('[') || trimmed.StartsWith('{')) return DataFormat.Json;
        if (trimmed.Contains(',') && trimmed.Contains('\n') && (trimmed.Contains("GroupTitle") || trimmed.Contains("Phonetic"))) return DataFormat.Csv;

        return DataFormat.Txt;
    }

    private static List<HomophoneImportExportGroupDto> ParseJson(string json, List<string> errors)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<HomophoneImportExportGroupDto>>(json, JsonOptions);
            return list ?? [];
        }
        catch (Exception ex)
        {
            errors.Add($"JSON Parse error: {ex.Message}");
            return [];
        }
    }

    private static List<HomophoneImportExportGroupDto> ParseCsv(string csv, List<string> errors)
    {
        var groupsMap = new Dictionary<string, HomophoneImportExportGroupDto>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(csv);
        string? headerLine = reader.ReadLine();
        if (headerLine == null) return [];

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = ParseCsvLine(line);
            if (parts.Count < 7) continue;

            var title = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(title)) continue;

            if (!groupsMap.TryGetValue(title, out var group))
            {
                group = new HomophoneImportExportGroupDto
                {
                    Title = title,
                    PhoneticIpa = parts.Count > 1 ? parts[1].Trim() : string.Empty,
                    Type = parts.Count > 2 ? parts[2].Trim() : "ExactHomophone",
                    Level = parts.Count > 3 ? parts[3].Trim() : "None",
                    ExamTag = parts.Count > 4 ? parts[4].Trim() : "None",
                    DifferentiationTip = parts.Count > 5 ? parts[5].Trim() : null,
                    Items = []
                };
                groupsMap[title] = group;
            }

            var term = parts.Count > 6 ? parts[6].Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(term))
            {
                group.Items.Add(new HomophoneImportExportItemDto
                {
                    Term = term,
                    PartOfSpeech = parts.Count > 7 ? parts[7].Trim() : "Noun",
                    MeaningVi = parts.Count > 8 ? parts[8].Trim() : string.Empty,
                    MeaningEn = parts.Count > 9 ? parts[9].Trim() : null,
                    PhoneticSpecific = parts.Count > 10 ? parts[10].Trim() : null,
                    ExampleSentence = parts.Count > 11 ? parts[11].Trim() : null,
                    ExampleTranslation = parts.Count > 12 ? parts[12].Trim() : null
                });
            }
        }

        return groupsMap.Values.ToList();
    }

    private static List<HomophoneImportExportGroupDto> ParseTxt(string txt, List<string> errors)
    {
        var groups = new List<HomophoneImportExportGroupDto>();
        using var reader = new StringReader(txt);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Pattern: bare / bear [/beə(r)/] => bare (Adjective): trần trụi || bear (Verb): chịu đựng // Mẹo: ...
            string tip = string.Empty;
            if (line.Contains("//"))
            {
                var tipParts = line.Split("//", 2);
                line = tipParts[0].Trim();
                tip = tipParts[1].Replace("Mẹo:", "").Trim();
            }

            string title = line;
            string phonetic = string.Empty;
            string itemsPart = string.Empty;

            if (line.Contains("=>"))
            {
                var mainParts = line.Split("=>", 2);
                var left = mainParts[0].Trim();
                itemsPart = mainParts[1].Trim();

                if (left.Contains('[') && left.Contains(']'))
                {
                    var pStart = left.IndexOf('[');
                    var pEnd = left.IndexOf(']');
                    title = left[..pStart].Trim();
                    phonetic = left.Substring(pStart + 1, pEnd - pStart - 1).Trim();
                }
                else
                {
                    title = left;
                }
            }

            if (string.IsNullOrWhiteSpace(title)) continue;

            var group = new HomophoneImportExportGroupDto
            {
                Title = title,
                PhoneticIpa = phonetic,
                Type = "ExactHomophone",
                DifferentiationTip = !string.IsNullOrWhiteSpace(tip) ? tip : null,
                Items = []
            };

            if (!string.IsNullOrWhiteSpace(itemsPart))
            {
                var itemTokens = itemsPart.Split("||");
                foreach (var tok in itemTokens)
                {
                    var itemStr = tok.Trim();
                    if (string.IsNullOrWhiteSpace(itemStr)) continue;

                    string term = itemStr;
                    string pos = "Noun";
                    string meaning = string.Empty;

                    if (itemStr.Contains(':'))
                    {
                        var mParts = itemStr.Split(':', 2);
                        meaning = mParts[1].Trim();
                        var leftSide = mParts[0].Trim();

                        if (leftSide.Contains('(') && leftSide.Contains(')'))
                        {
                            var posStart = leftSide.IndexOf('(');
                            var posEnd = leftSide.IndexOf(')');
                            term = leftSide[..posStart].Trim();
                            pos = leftSide.Substring(posStart + 1, posEnd - posStart - 1).Trim();
                        }
                        else
                        {
                            term = leftSide;
                        }
                    }

                    group.Items.Add(new HomophoneImportExportItemDto
                    {
                        Term = term,
                        PartOfSpeech = pos,
                        MeaningVi = meaning
                    });
                }
            }

            if (group.Items.Count > 0)
            {
                groups.Add(group);
            }
        }

        return groups;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
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

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        return Enum.TryParse<TEnum>(value.Trim(), true, out var result) ? result : defaultValue;
    }
}
