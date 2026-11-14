using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services;

public record ParsedSmartTxtRow(
    string Term,
    string DefinitionVi,
    string? DefinitionEn,
    PartOfSpeech PartOfSpeech,
    CefrLevel Level,
    string? PhoneticUk,
    string? PhoneticUs,
    string? Examples
);

/// <summary>
/// High-performance parsing and normalization engine for smart vocabulary import formats (JSON, CSV, TSV, Markdown, Text).
/// </summary>
public static class SmartImportParser
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new FlexiblePartOfSpeechJsonConverter(),
            new FlexibleCefrLevelJsonConverter(),
            new FlexibleStringListJsonConverter(),
            new FlexibleExampleListJsonConverter(),
            new JsonStringEnumConverter()
        }
    };

    private static readonly Regex NumberPrefixRegex = new(@"^\s*(?:\[\d+\]|\d+[\.\)\:\-]|[\*\-\+•–—])\s*", RegexOptions.Compiled);
    private static readonly Regex PhoneticSlashRegex = new(@"/([^/]{1,100})/", RegexOptions.Compiled);
    private static readonly Regex PhoneticBracketRegex = new(@"\[([^\]]{1,100})\]", RegexOptions.Compiled);
    private static readonly Regex CefrTagRegex = new(@"\[\s*(A1|A2|B1|B2|C1|C2)\s*\]|\(\s*(A1|A2|B1|B2|C1|C2)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PosTagRegex = new(@"[\(\[]\s*(n|v|adj|adv|prep|conj|pron|int|interj|phr|phrase|idiom|idm|danh từ|động từ|tính từ|trạng từ|giới từ|liên từ|thành ngữ)\s*[\)\]]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly SearchValues<char> CsvFormulaTriggers = SearchValues.Create("=+-@\t\r");

    /// <summary>
    /// Strips markdown code blocks (e.g. ```json ... ```) and leading BOM/whitespace.
    /// </summary>
    public static string StripMarkdownCodeFence(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var trimmed = input.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            int firstNl = trimmed.IndexOf('\n');
            if (firstNl >= 0)
            {
                trimmed = trimmed.Substring(firstNl + 1);
            }
            int lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
            {
                trimmed = trimmed.Substring(0, lastFence);
            }
            trimmed = trimmed.Trim();
        }
        return trimmed.Trim();
    }

    /// <summary>
    /// Robustly deserializes JSON vocabulary arrays or wrapped JSON objects.
    /// </summary>
    public static List<WordExportModel> DeserializeWordsJson(string rawContent)
    {
        var cleanJson = StripMarkdownCodeFence(rawContent);
        if (string.IsNullOrWhiteSpace(cleanJson)) return new List<WordExportModel>();

        // Fast path: direct array
        if (cleanJson.StartsWith('['))
        {
            return JsonSerializer.Deserialize<List<WordExportModel>>(cleanJson, JsonOptions) ?? new List<WordExportModel>();
        }

        // Object path: inspect with JsonDocument
        if (cleanJson.StartsWith('{'))
        {
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            // Check common array properties
            string[] candidates = ["words", "data", "items", "vocabulary", "vocab", "list", "content", "records"];
            foreach (var propName in candidates)
            {
                if (root.TryGetProperty(propName, out var arrayEl) && arrayEl.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<WordExportModel>>(arrayEl.GetRawText(), JsonOptions) ?? new List<WordExportModel>();
                }
            }

            // Check if any property in root is an Array
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<WordExportModel>>(prop.Value.GetRawText(), JsonOptions) ?? new List<WordExportModel>();
                }
            }

            // Single word object check
            if (root.TryGetProperty("term", out _) || root.TryGetProperty("word", out _))
            {
                var single = JsonSerializer.Deserialize<WordExportModel>(cleanJson, JsonOptions);
                return single != null ? [single] : new List<WordExportModel>();
            }

            // Dictionary format check: { "apple": "quả táo" } or { "apple": { ... } }
            var dictWords = new List<WordExportModel>();
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    dictWords.Add(new WordExportModel
                    {
                        Term = prop.Name,
                        DefinitionVi = prop.Value.GetString() ?? string.Empty
                    });
                }
                else if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    var item = JsonSerializer.Deserialize<WordExportModel>(prop.Value.GetRawText(), JsonOptions);
                    if (item != null)
                    {
                        if (string.IsNullOrWhiteSpace(item.Term) && string.IsNullOrWhiteSpace(item.Word))
                        {
                            item.Term = prop.Name;
                        }
                        dictWords.Add(item);
                    }
                }
            }
            if (dictWords.Count > 0)
            {
                return dictWords;
            }
        }

        // Fallback
        return JsonSerializer.Deserialize<List<WordExportModel>>(cleanJson, JsonOptions) ?? new List<WordExportModel>();
    }

    public static bool TryParsePartOfSpeech(string? input, out PartOfSpeech pos)
    {
        pos = PartOfSpeech.Noun;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var clean = input.Trim().Trim('(', ')', '[', ']').Trim().ToLowerInvariant().TrimEnd('.', ',');
        if (Enum.TryParse<PartOfSpeech>(clean, ignoreCase: true, out var exact))
        {
            pos = exact;
            return true;
        }

        switch (clean)
        {
            case "n" or "noun" or "danh từ" or "danh tu":
                pos = PartOfSpeech.Noun;
                return true;
            case "v" or "verb" or "động từ" or "dong tu":
                pos = PartOfSpeech.Verb;
                return true;
            case "adj" or "a" or "adjective" or "tính từ" or "tinh tu":
                pos = PartOfSpeech.Adjective;
                return true;
            case "adv" or "adverb" or "trạng từ" or "phó từ" or "trang tu" or "pho tu":
                pos = PartOfSpeech.Adverb;
                return true;
            case "prep" or "preposition" or "giới từ" or "gioi tu":
                pos = PartOfSpeech.Preposition;
                return true;
            case "conj" or "conjunction" or "liên từ" or "lien tu":
                pos = PartOfSpeech.Conjunction;
                return true;
            case "pron" or "pronoun" or "đại từ" or "dai tu":
                pos = PartOfSpeech.Noun;
                return true;
            case "int" or "interj" or "interjection" or "thán từ" or "than tu":
                pos = PartOfSpeech.Interjection;
                return true;
            case "idm" or "idiom" or "thành ngữ" or "thanh ngu":
                pos = PartOfSpeech.Idiom;
                return true;
            case "phrv" or "phr v" or "phrasal verb" or "phrasal_verb" or "cụm động từ" or "cum dong tu":
                pos = PartOfSpeech.PhrasalVerb;
                return true;
            case "np" or "noun phrase" or "noun_phrase" or "cụm danh từ" or "cum danh tu":
                pos = PartOfSpeech.NounPhrase;
                return true;
            case "phrase" or "phr" or "cụm từ" or "cum tu":
                pos = PartOfSpeech.Phrase;
                return true;
            default:
                return false;
        }
    }

    public static PartOfSpeech ParsePartOfSpeech(string? input, PartOfSpeech defaultPos = PartOfSpeech.Noun)
    {
        if (string.IsNullOrWhiteSpace(input))
            return defaultPos;

        var clean = input.Trim().Trim('(', ')', '[', ']').Trim().ToLowerInvariant().TrimEnd('.', ',');

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

    public static string NormalizeTerm(string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return string.Empty;
        var tokens = term.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", tokens).ToLowerInvariant();
    }

    public static string CleanTerm(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return string.Empty;
        var t = term.Trim('"', '\'', '`', ' ', '\t');
        if (t.StartsWith("**") && t.EndsWith("**") && t.Length >= 4)
        {
            t = t.Substring(2, t.Length - 4).Trim();
        }
        else if (t.StartsWith('*') && t.EndsWith('*') && t.Length >= 2)
        {
            t = t.Substring(1, t.Length - 2).Trim();
        }
        return t.Trim();
    }

    public static string SafeTruncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }

    public static string? SafeTruncateNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }

    public static ParsedSmartTxtRow? ParseSmartTxtLine(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine)) return null;
        var trimmedLine = rawLine.Trim();
        if (trimmedLine.StartsWith('#') || trimmedLine.StartsWith("//")) return null;

        // 1. Markdown table row: | Term | POS | Meaning | Example |
        if (trimmedLine.StartsWith('|') && trimmedLine.EndsWith('|') && trimmedLine.Length > 2)
        {
            var rawCells = trimmedLine.Split('|');
            var cells = rawCells
                .Select(c => c.Trim())
                .Skip(1)
                .Take(rawCells.Length - 2)
                .ToList();

            // Skip table divider |---|---|---|
            if (cells.All(c => string.IsNullOrWhiteSpace(c) || c.All(ch => ch == '-' || ch == ':' || ch == ' ')))
            {
                return null;
            }

            // Skip table header
            if (cells.Count > 0 && (cells[0].Equals("term", StringComparison.OrdinalIgnoreCase) ||
                                    cells[0].Equals("word", StringComparison.OrdinalIgnoreCase) ||
                                    cells[0].Equals("từ", StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            if (cells.Count > 0)
            {
                string cellTerm = CleanTerm(cells[0]);
                string cellDef = string.Empty;
                string? cellExamples = null;
                string? cellPhonetic = null;
                var pos = PartOfSpeech.Noun;
                var level = CefrLevel.None;

                for (int i = 1; i < cells.Count; i++)
                {
                    var cell = cells[i];
                    if (string.IsNullOrWhiteSpace(cell)) continue;

                    // 1. Phonetic e.g. /rɪˈzɪl.jənt/
                    if (cell.StartsWith('/') && cell.EndsWith('/') && cell.Length > 2)
                    {
                        cellPhonetic = cell;
                        continue;
                    }

                    // 2. CEFR level e.g. B2, [B2], C1
                    var detectedLevel = ParseCefrLevel(cell);
                    if (detectedLevel != CefrLevel.None && (cell.Length <= 4 || cell.StartsWith('[') || cell.StartsWith('(')))
                    {
                        level = detectedLevel;
                        continue;
                    }

                    // 3. Part of Speech e.g. (adj), n, verb
                    if (TryParsePartOfSpeech(cell, out var detectedPos))
                    {
                        pos = detectedPos;
                        continue;
                    }

                    // 4. Definition or Example
                    if (string.IsNullOrWhiteSpace(cellDef))
                    {
                        cellDef = cell;
                    }
                    else if (string.IsNullOrWhiteSpace(cellExamples))
                    {
                        cellExamples = cell;
                    }
                    else
                    {
                        cellExamples += "; " + cell;
                    }
                }

                return new ParsedSmartTxtRow(
                    Term: cellTerm,
                    DefinitionVi: cellDef,
                    DefinitionEn: null,
                    PartOfSpeech: pos,
                    Level: level,
                    PhoneticUk: cellPhonetic,
                    PhoneticUs: cellPhonetic,
                    Examples: cellExamples
                );
            }
        }

        // 2. Strip leading numbering or bullet points
        var working = NumberPrefixRegex.Replace(trimmedLine, "").Trim();

        // 3. Tab-delimited format (TSV / Anki / Quizlet / Excel / Google Sheets copy-paste)
        if (working.Contains('\t'))
        {
            var parts = working.Split('\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count >= 2)
            {
                string cellTerm = CleanTerm(parts[0]);
                string cellDef = string.Empty;
                string? cellExamples = null;
                string? cellPhonetic = null;
                var pos = PartOfSpeech.Noun;
                var level = CefrLevel.None;

                for (int i = 1; i < parts.Count; i++)
                {
                    var part = parts[i];
                    if (string.IsNullOrWhiteSpace(part)) continue;

                    // Phonetic e.g. /əˈbʌn.dənt/
                    if (part.StartsWith('/') && part.EndsWith('/') && part.Length > 2)
                    {
                        cellPhonetic = part;
                        continue;
                    }

                    // CEFR e.g. B2, [B2], C1
                    var detectedLevel = ParseCefrLevel(part);
                    if (detectedLevel != CefrLevel.None && (part.Length <= 4 || part.StartsWith('[') || part.StartsWith('(')))
                    {
                        level = detectedLevel;
                        continue;
                    }

                    // POS e.g. (adj), [n], verb
                    if (TryParsePartOfSpeech(part, out var detectedPos))
                    {
                        pos = detectedPos;
                        continue;
                    }

                    // Definition or Examples
                    if (string.IsNullOrWhiteSpace(cellDef))
                    {
                        cellDef = part;
                    }
                    else if (string.IsNullOrWhiteSpace(cellExamples))
                    {
                        cellExamples = part;
                    }
                    else
                    {
                        cellExamples += "; " + part;
                    }
                }

                // If term contains inline phonetic or POS e.g. "abundant /əˈbʌn.dənt/ (adj)"
                var slashMatchInTerm = PhoneticSlashRegex.Match(cellTerm);
                if (slashMatchInTerm.Success)
                {
                    cellPhonetic ??= slashMatchInTerm.Value.Trim();
                    cellTerm = cellTerm.Remove(slashMatchInTerm.Index, slashMatchInTerm.Length).Trim();
                }

                var posMatchInTerm = PosTagRegex.Match(cellTerm);
                if (posMatchInTerm.Success && pos == PartOfSpeech.Noun)
                {
                    pos = ParsePartOfSpeech(posMatchInTerm.Groups[1].Value);
                    cellTerm = cellTerm.Remove(posMatchInTerm.Index, posMatchInTerm.Length).Trim();
                }

                return new ParsedSmartTxtRow(
                    Term: CleanTerm(cellTerm),
                    DefinitionVi: cellDef,
                    DefinitionEn: null,
                    PartOfSpeech: pos,
                    Level: level,
                    PhoneticUk: cellPhonetic,
                    PhoneticUs: cellPhonetic,
                    Examples: cellExamples
                );
            }
        }

        // 4. Extract phonetic transcription
        string? phonetic = null;
        var slashMatch = PhoneticSlashRegex.Match(working);
        if (slashMatch.Success)
        {
            phonetic = slashMatch.Value.Trim();
            working = working.Remove(slashMatch.Index, slashMatch.Length).Trim();
        }

        // 5. Extract CEFR level e.g. [B2] or (B2)
        var levelVal = CefrLevel.None;
        var cefrMatch = CefrTagRegex.Match(working);
        if (cefrMatch.Success)
        {
            levelVal = ParseCefrLevel(cefrMatch.Groups[1].Success ? cefrMatch.Groups[1].Value : cefrMatch.Groups[2].Value);
            working = working.Remove(cefrMatch.Index, cefrMatch.Length).Trim();
        }

        // 6. Extract Part of Speech e.g. (adj) or [v]
        var posVal = PartOfSpeech.Noun;
        var posMatch = PosTagRegex.Match(working);
        if (posMatch.Success)
        {
            posVal = ParsePartOfSpeech(posMatch.Groups[1].Value);
            working = working.Remove(posMatch.Index, posMatch.Length).Trim();
        }

        // 7. Split by delimiter: " | ", "::", " — ", " – ", " - ", " : ", ":", "="
        string termPart = "";
        string defPart = "";
        string? examples = null;

        if (working.Contains(" | "))
        {
            var parts = working.Split(new[] { " | " }, StringSplitOptions.TrimEntries);
            if (parts.Length > 0) termPart = parts[0];
            if (parts.Length > 1) defPart = parts[1];
            if (parts.Length > 2) examples = parts[2];
        }
        else if (working.Contains("::"))
        {
            int idx = working.IndexOf("::", StringComparison.Ordinal);
            termPart = working.Substring(0, idx).Trim();
            defPart = working.Substring(idx + 2).Trim();
        }
        else if (working.Contains(" — "))
        {
            var parts = working.Split(new[] { " — " }, StringSplitOptions.TrimEntries);
            if (parts.Length > 0) termPart = parts[0];
            if (parts.Length > 1) defPart = parts[1];
            if (parts.Length > 2) examples = parts[2];
        }
        else if (working.Contains(" – "))
        {
            var parts = working.Split(new[] { " – " }, StringSplitOptions.TrimEntries);
            if (parts.Length > 0) termPart = parts[0];
            if (parts.Length > 1) defPart = parts[1];
            if (parts.Length > 2) examples = parts[2];
        }
        else if (working.Contains(" - "))
        {
            var parts = working.Split(new[] { " - " }, StringSplitOptions.TrimEntries);
            if (parts.Length > 0) termPart = parts[0];
            if (parts.Length > 1) defPart = parts[1];
            if (parts.Length > 2) examples = parts[2];
        }
        else if (working.Contains(" : "))
        {
            int idx = working.IndexOf(" : ", StringComparison.Ordinal);
            termPart = working.Substring(0, idx).Trim();
            defPart = working.Substring(idx + 3).Trim();
        }
        else if (working.Contains(':') && !working.Contains("http://") && !working.Contains("https://"))
        {
            int idx = working.IndexOf(':');
            termPart = working.Substring(0, idx).Trim();
            defPart = working.Substring(idx + 1).Trim();
        }
        else if (working.Contains(" = "))
        {
            int idx = working.IndexOf(" = ", StringComparison.Ordinal);
            termPart = working.Substring(0, idx).Trim();
            defPart = working.Substring(idx + 3).Trim();
        }
        else if (working.Contains('—'))
        {
            int idx = working.IndexOf('—');
            termPart = working.Substring(0, idx).Trim();
            defPart = working.Substring(idx + 1).Trim();
        }
        else if (working.Contains('–'))
        {
            int idx = working.IndexOf('–');
            termPart = working.Substring(0, idx).Trim();
            defPart = working.Substring(idx + 1).Trim();
        }
        else
        {
            termPart = working.Trim();
        }

        // Secondary check for example separator in defPart: " => ", " - e.g. ", " - ex: ", " - ví dụ: "
        if (examples == null && !string.IsNullOrWhiteSpace(defPart))
        {
            if (defPart.Contains(" => "))
            {
                var exSplit = defPart.Split(new[] { " => " }, 2, StringSplitOptions.TrimEntries);
                defPart = exSplit[0];
                examples = exSplit[1];
            }
            else if (defPart.Contains(" - e.g. ", StringComparison.OrdinalIgnoreCase))
            {
                var idx = defPart.IndexOf(" - e.g. ", StringComparison.OrdinalIgnoreCase);
                examples = defPart.Substring(idx + 8).Trim();
                defPart = defPart.Substring(0, idx).Trim();
            }
            else if (defPart.Contains(" - ví dụ: ", StringComparison.OrdinalIgnoreCase))
            {
                var idx = defPart.IndexOf(" - ví dụ: ", StringComparison.OrdinalIgnoreCase);
                examples = defPart.Substring(idx + 10).Trim();
                defPart = defPart.Substring(0, idx).Trim();
            }
        }

        string cleanedTerm = CleanTerm(termPart);
        string cleanedDef = defPart.Trim('"', '\'', ' ', '\t');

        return new ParsedSmartTxtRow(
            Term: cleanedTerm,
            DefinitionVi: cleanedDef,
            DefinitionEn: null,
            PartOfSpeech: posVal,
            Level: levelVal,
            PhoneticUk: phonetic,
            PhoneticUs: phonetic,
            Examples: examples
        );
    }

    public static string EscapeCsv(string field)
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

    public static List<List<string>> ParseCsvRecords(string csvContent)
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

    public static List<string> ParseCsvLine(string line, char delimiter)
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
