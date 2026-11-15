using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services;

public partial class ExportImportService
{
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
        HashSet<string>? existingTerms = null;
        try
        {
            existingTerms = await _vocabRepo.GetAllNormalizedTermsAsync(cancellationToken);
        }
        catch
        {
            existingTerms = null;
        }

        if (existingTerms == null || existingTerms.Count == 0)
        {
            var existingList = await _vocabRepo.SearchAsync(take: 100000, cancellationToken: cancellationToken)
                ?? (IReadOnlyList<Word>)Array.Empty<Word>();
            existingTerms = existingList
                .Select(w => !string.IsNullOrEmpty(w.NormalizedTerm) ? w.NormalizedTerm : w.Term.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                var jsonWords = DeserializeWordsJson(content);
                int rowIdx = 0;
                foreach (var jw in jsonWords)
                {
                    rowIdx++;
                    string term = jw.ResolvedTerm;
                    bool isTermValid = !string.IsNullOrWhiteSpace(term);
                    string normalized = NormalizeTerm(term);
                    bool isDuplicate = isTermValid && (existingTerms.Contains(normalized) || seenInBatch.Contains(normalized));

                    var richMeanings = new List<WordMeaningCreateDto>();
                    var allPosList = new List<PartOfSpeech>();

                    if (jw.AllPartsOfSpeech != null && jw.AllPartsOfSpeech.Count > 0)
                    {
                        allPosList.AddRange(jw.AllPartsOfSpeech);
                    }
                    else if (jw.PartsOfSpeech != null && jw.PartsOfSpeech.Count > 0)
                    {
                        foreach (var p in jw.PartsOfSpeech)
                        {
                            allPosList.Add(ParsePartOfSpeech(p));
                        }
                    }

                    if (jw.Meanings != null && jw.Meanings.Count > 0)
                    {
                        foreach (var m in jw.Meanings)
                        {
                            var mPos = m.PartOfSpeech != 0
                                ? m.PartOfSpeech
                                : (!string.IsNullOrWhiteSpace(m.Pos)
                                    ? ParsePartOfSpeech(m.Pos)
                                    : (jw.PrimaryPartOfSpeech != 0
                                        ? jw.PrimaryPartOfSpeech
                                        : (!string.IsNullOrWhiteSpace(jw.Pos) ? ParsePartOfSpeech(jw.Pos) : PartOfSpeech.Noun)));

                            if (!allPosList.Contains(mPos))
                            {
                                allPosList.Add(mPos);
                            }

                            var exList = (m.Examples ?? []).Select(ex => new ExampleSentenceCreateDto(
                                SentenceEn: ex.ResolvedSentenceEn,
                                SentenceVi: ex.ResolvedSentenceVi,
                                AudioPath: null
                            )).Where(e => !string.IsNullOrWhiteSpace(e.SentenceEn)).ToList();

                            var effectiveDefVi = !string.IsNullOrWhiteSpace(m.ResolvedDefinitionVi)
                                ? m.ResolvedDefinitionVi
                                : (!string.IsNullOrWhiteSpace(m.ResolvedDefinitionEn) ? m.ResolvedDefinitionEn : string.Empty);

                            richMeanings.Add(new WordMeaningCreateDto(
                                PartOfSpeech: mPos,
                                DefinitionVi: effectiveDefVi,
                                DefinitionEn: m.ResolvedDefinitionEn,
                                Explanation: m.Explanation,
                                Examples: exList
                            ));
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(jw.ResolvedDefinitionVi) || !string.IsNullOrWhiteSpace(jw.ResolvedDefinitionEn))
                    {
                        var mPos = jw.PrimaryPartOfSpeech != 0
                            ? jw.PrimaryPartOfSpeech
                            : (!string.IsNullOrWhiteSpace(jw.Pos) ? ParsePartOfSpeech(jw.Pos) : PartOfSpeech.Noun);
                        if (!allPosList.Contains(mPos)) allPosList.Add(mPos);

                        var effectiveDefVi = !string.IsNullOrWhiteSpace(jw.ResolvedDefinitionVi)
                            ? jw.ResolvedDefinitionVi
                            : (jw.ResolvedDefinitionEn ?? string.Empty);

                        richMeanings.Add(new WordMeaningCreateDto(
                            PartOfSpeech: mPos,
                            DefinitionVi: effectiveDefVi,
                            DefinitionEn: jw.ResolvedDefinitionEn,
                            Explanation: null,
                            Examples: new List<ExampleSentenceCreateDto>()
                        ));
                    }

                    var primaryPos = jw.PrimaryPartOfSpeech != 0
                        ? jw.PrimaryPartOfSpeech
                        : (!string.IsNullOrWhiteSpace(jw.Pos)
                            ? ParsePartOfSpeech(jw.Pos)
                            : (allPosList.Count > 0 ? allPosList[0] : PartOfSpeech.Noun));

                    if (!allPosList.Contains(primaryPos))
                    {
                        allPosList.Insert(0, primaryPos);
                    }

                    var firstMeaning = richMeanings.FirstOrDefault();
                    string defVi = firstMeaning?.DefinitionVi ?? (!string.IsNullOrWhiteSpace(jw.ResolvedDefinitionVi) ? jw.ResolvedDefinitionVi : (jw.ResolvedDefinitionEn ?? string.Empty));
                    string? defEn = firstMeaning?.DefinitionEn ?? jw.ResolvedDefinitionEn;

                    if (string.IsNullOrWhiteSpace(defVi) && !string.IsNullOrWhiteSpace(defEn))
                    {
                        defVi = defEn;
                    }

                    var allExamples = richMeanings
                        .SelectMany(m => m.Examples)
                        .Select(ex => !string.IsNullOrWhiteSpace(ex.SentenceVi) ? $"{ex.SentenceEn} => {ex.SentenceVi}" : ex.SentenceEn)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                    string? exText = allExamples.Count > 0 ? string.Join(" | ", allExamples) : (!string.IsNullOrWhiteSpace(jw.Examples) ? jw.Examples : null);
                    var resolvedTopics = jw.ResolvedTopics;
                    string? topicsText = resolvedTopics.Count > 0 ? string.Join("; ", resolvedTopics) : null;
                    var resolvedExamTags = jw.ResolvedExamTags;
                    string? examsText = resolvedExamTags.Count > 0 ? string.Join("; ", resolvedExamTags) : null;

                    bool hasDefinition = !string.IsNullOrWhiteSpace(defVi) || !string.IsNullOrWhiteSpace(defEn);
                    string? validationMsg = null;
                    if (!isTermValid) validationMsg = "Thuật ngữ không được để trống / Term is required.";
                    else if (!hasDefinition) validationMsg = "Thiếu định nghĩa / Missing definition.";

                    bool isValid = isTermValid && hasDefinition;

                    if (isTermValid)
                    {
                        seenInBatch.Add(normalized);
                    }

                    previewItems.Add(new SmartImportItemDto(
                        RowIndex: rowIdx,
                        Term: term,
                        Level: jw.Level,
                        PartOfSpeech: primaryPos,
                        DefinitionVi: defVi,
                        DefinitionEn: defEn,
                        PhoneticUk: jw.ResolvedPhoneticUk,
                        PhoneticUs: jw.ResolvedPhoneticUs,
                        Examples: exText,
                        Topics: topicsText,
                        ExamTags: examsText,
                        Notes: jw.Notes,
                        IsDuplicate: isDuplicate,
                        IsValid: isValid,
                        ValidationMessage: validationMsg,
                        IsSelected: isValid && !isDuplicate,
                        RichMeanings: richMeanings,
                        AllPartsOfSpeech: allPosList.Distinct().ToList()
                    ));
                }
            }
            catch (Exception ex)
            {
                var trimmed = StripMarkdownCodeFence(content).TrimStart();
                if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
                {
                    format = DataFormat.Txt;
                }
                else
                {
                    previewItems.Add(new SmartImportItemDto(
                        RowIndex: 1,
                        Term: "Lỗi định dạng JSON / Invalid JSON",
                        Level: CefrLevel.None,
                        PartOfSpeech: PartOfSpeech.Noun,
                        DefinitionVi: string.Empty,
                        DefinitionEn: null,
                        PhoneticUk: null,
                        PhoneticUs: null,
                        Examples: null,
                        Topics: null,
                        ExamTags: null,
                        Notes: ex.Message,
                        IsDuplicate: false,
                        IsValid: false,
                        ValidationMessage: $"Cấu trúc JSON không hợp lệ: {ex.Message}",
                        IsSelected: false,
                        RichMeanings: [],
                        AllPartsOfSpeech: []
                    ));
                }
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
                    string normalized = NormalizeTerm(term);
                    bool isDuplicate = isTermValid && (existingTerms.Contains(normalized) || seenInBatch.Contains(normalized));
                    if (isTermValid) seenInBatch.Add(normalized);

                    string? validationMsg = null;
                    if (!isTermValid) validationMsg = "Thiếu thuật ngữ / Missing term.";
                    else if (string.IsNullOrWhiteSpace(defVi) && string.IsNullOrWhiteSpace(defEn)) validationMsg = "Thiếu định nghĩa tiếng Việt / Missing Vietnamese definition.";

                    var richMeanings = new List<WordMeaningCreateDto>();
                    if (!string.IsNullOrWhiteSpace(defVi) || !string.IsNullOrWhiteSpace(defEn))
                    {
                        var exList = new List<ExampleSentenceCreateDto>();
                        if (!string.IsNullOrWhiteSpace(examples))
                        {
                            var exTokens = examples.Split(new[] { " | ", "; " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var ex in exTokens)
                            {
                                var arrowIdx = ex.IndexOf("=>", StringComparison.Ordinal);
                                string en = arrowIdx >= 0 ? ex.Substring(0, arrowIdx).Trim() : ex.Trim();
                                string vi = arrowIdx >= 0 ? ex.Substring(arrowIdx + 2).Trim() : string.Empty;
                                if (!string.IsNullOrWhiteSpace(en))
                                {
                                    exList.Add(new ExampleSentenceCreateDto(SafeTruncate(en, 1000), SafeTruncate(vi, 1000), null));
                                }
                            }
                        }

                        richMeanings.Add(new WordMeaningCreateDto(
                            PartOfSpeech: pos,
                            DefinitionVi: SafeTruncate(!string.IsNullOrWhiteSpace(defVi) ? defVi : defEn!, 500),
                            DefinitionEn: SafeTruncateNullable(defEn, 1000),
                            Explanation: null,
                            Examples: exList
                        ));
                    }

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
                        IsValid: isTermValid && (!string.IsNullOrWhiteSpace(defVi) || !string.IsNullOrWhiteSpace(defEn)),
                        ValidationMessage: validationMsg,
                        IsSelected: isTermValid && (!string.IsNullOrWhiteSpace(defVi) || !string.IsNullOrWhiteSpace(defEn)) && !isDuplicate,
                        RichMeanings: richMeanings,
                        AllPartsOfSpeech: [pos]
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

                var parsed = ParseSmartTxtLine(rawLine);
                if (parsed == null) continue;

                rowIdx++;
                string term = parsed.Term;
                string defVi = parsed.DefinitionVi;
                string? defEn = parsed.DefinitionEn;
                PartOfSpeech pos = parsed.PartOfSpeech;
                CefrLevel level = parsed.Level;
                string? ipaUk = parsed.PhoneticUk;
                string? ipaUs = parsed.PhoneticUs;
                string? examples = parsed.Examples;

                bool isTermValid = !string.IsNullOrWhiteSpace(term);
                string normalized = NormalizeTerm(term);
                bool isDuplicate = isTermValid && (existingTerms.Contains(normalized) || seenInBatch.Contains(normalized));
                if (isTermValid) seenInBatch.Add(normalized);

                string? validationMsg = null;
                if (!isTermValid) validationMsg = "Thiếu thuật ngữ / Missing term.";
                else if (string.IsNullOrWhiteSpace(defVi) && string.IsNullOrWhiteSpace(defEn)) validationMsg = "Thiếu định nghĩa / Missing definition.";

                var richMeanings = new List<WordMeaningCreateDto>();
                if (!string.IsNullOrWhiteSpace(defVi) || !string.IsNullOrWhiteSpace(defEn))
                {
                    var exList = new List<ExampleSentenceCreateDto>();
                    if (!string.IsNullOrWhiteSpace(examples))
                    {
                        var exTokens = examples.Split(new[] { " | ", "; " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var ex in exTokens)
                        {
                            var arrowIdx = ex.IndexOf("=>", StringComparison.Ordinal);
                            string en = arrowIdx >= 0 ? ex.Substring(0, arrowIdx).Trim() : ex.Trim();
                            string vi = arrowIdx >= 0 ? ex.Substring(arrowIdx + 2).Trim() : string.Empty;
                            if (!string.IsNullOrWhiteSpace(en))
                            {
                                exList.Add(new ExampleSentenceCreateDto(SafeTruncate(en, 1000), SafeTruncate(vi, 1000), null));
                            }
                        }
                    }

                    richMeanings.Add(new WordMeaningCreateDto(
                        PartOfSpeech: pos,
                        DefinitionVi: SafeTruncate(!string.IsNullOrWhiteSpace(defVi) ? defVi : defEn!, 500),
                        DefinitionEn: SafeTruncateNullable(defEn, 1000),
                        Explanation: null,
                        Examples: exList
                    ));
                }

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
                    Topics: null,
                    ExamTags: null,
                    Notes: null,
                    IsDuplicate: isDuplicate,
                    IsValid: isTermValid && (!string.IsNullOrWhiteSpace(defVi) || !string.IsNullOrWhiteSpace(defEn)),
                    ValidationMessage: validationMsg,
                    IsSelected: isTermValid && (!string.IsNullOrWhiteSpace(defVi) || !string.IsNullOrWhiteSpace(defEn)) && !isDuplicate,
                    RichMeanings: richMeanings,
                    AllPartsOfSpeech: [pos]
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
}
