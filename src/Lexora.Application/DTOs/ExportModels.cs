using System.Text.Json.Serialization;
using Lexora.Application.Common;
using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public class WordExportModel
{
    public string Term { get; set; } = string.Empty;
    public string? Word { get; set; }
    public string? PhoneticUk { get; set; }
    public string? PhoneticUs { get; set; }
    public string? Phonetic { get; set; }
    public string? Ipa { get; set; }
    public string? Pronunciation { get; set; }

    [JsonConverter(typeof(FlexibleCefrLevelJsonConverter))]
    public CefrLevel Level { get; set; }

    [JsonConverter(typeof(FlexiblePartOfSpeechJsonConverter))]
    public PartOfSpeech PrimaryPartOfSpeech { get; set; }

    public string? Pos { get; set; }
    public string? PartOfSpeech { get; set; }
    public List<PartOfSpeech> AllPartsOfSpeech { get; set; } = new();
    public List<string>? PartsOfSpeech { get; set; }
    public string? Notes { get; set; }
    public List<MeaningExportModel> Meanings { get; set; } = new();
    public string? Definition { get; set; }
    public string? DefinitionVi { get; set; }
    public string? DefinitionEn { get; set; }
    public string? Meaning { get; set; }
    public string? MeaningVi { get; set; }
    public string? MeaningEn { get; set; }
    public string? Examples { get; set; }
    public string? Example { get; set; }

    [JsonConverter(typeof(FlexibleStringListJsonConverter))]
    public List<string> Topics { get; set; } = new();
    public string? Topic { get; set; }

    [JsonConverter(typeof(FlexibleStringListJsonConverter))]
    public List<string> ExamTags { get; set; } = new();
    public string? ExamTag { get; set; }
    public List<string>? Exams { get; set; }

    public string ResolvedTerm => !string.IsNullOrWhiteSpace(Term)
        ? Term.Trim()
        : (!string.IsNullOrWhiteSpace(Word) ? Word.Trim() : string.Empty);

    public string? ResolvedPhoneticUk => !string.IsNullOrWhiteSpace(PhoneticUk)
        ? PhoneticUk.Trim()
        : (!string.IsNullOrWhiteSpace(Phonetic)
            ? Phonetic.Trim()
            : (!string.IsNullOrWhiteSpace(Ipa)
                ? Ipa.Trim()
                : (!string.IsNullOrWhiteSpace(Pronunciation) ? Pronunciation.Trim() : null)));

    public string? ResolvedPhoneticUs => !string.IsNullOrWhiteSpace(PhoneticUs)
        ? PhoneticUs.Trim()
        : ResolvedPhoneticUk;

    public string ResolvedDefinitionVi => !string.IsNullOrWhiteSpace(DefinitionVi)
        ? DefinitionVi.Trim()
        : (!string.IsNullOrWhiteSpace(MeaningVi)
            ? MeaningVi.Trim()
            : (!string.IsNullOrWhiteSpace(Definition)
                ? Definition.Trim()
                : (!string.IsNullOrWhiteSpace(Meaning) ? Meaning.Trim() : string.Empty)));

    public string? ResolvedDefinitionEn => !string.IsNullOrWhiteSpace(DefinitionEn)
        ? DefinitionEn.Trim()
        : (!string.IsNullOrWhiteSpace(MeaningEn) ? MeaningEn.Trim() : null);

    public List<string> ResolvedTopics
    {
        get
        {
            var list = new List<string>(Topics ?? []);
            if (!string.IsNullOrWhiteSpace(Topic) && !list.Contains(Topic.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                list.Add(Topic.Trim());
            }
            return list;
        }
    }

    public List<string> ResolvedExamTags
    {
        get
        {
            var list = new List<string>(ExamTags ?? []);
            if (Exams != null)
            {
                foreach (var ex in Exams)
                {
                    if (!string.IsNullOrWhiteSpace(ex) && !list.Contains(ex.Trim(), StringComparer.OrdinalIgnoreCase))
                        list.Add(ex.Trim());
                }
            }
            if (!string.IsNullOrWhiteSpace(ExamTag) && !list.Contains(ExamTag.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                list.Add(ExamTag.Trim());
            }
            return list;
        }
    }
}

public class MeaningExportModel
{
    [JsonConverter(typeof(FlexiblePartOfSpeechJsonConverter))]
    public PartOfSpeech PartOfSpeech { get; set; }

    public string? Pos { get; set; }
    public string DefinitionVi { get; set; } = string.Empty;
    public string? DefinitionEn { get; set; }
    public string? Definition { get; set; }
    public string? Meaning { get; set; }
    public string? MeaningVi { get; set; }
    public string? MeaningEn { get; set; }
    public string? Vi { get; set; }
    public string? En { get; set; }
    public string? Explanation { get; set; }

    [JsonConverter(typeof(FlexibleExampleListJsonConverter))]
    public List<ExampleExportModel> Examples { get; set; } = new();

    public string ResolvedDefinitionVi => !string.IsNullOrWhiteSpace(DefinitionVi)
        ? DefinitionVi.Trim()
        : (!string.IsNullOrWhiteSpace(MeaningVi)
            ? MeaningVi.Trim()
            : (!string.IsNullOrWhiteSpace(Vi)
                ? Vi.Trim()
                : (!string.IsNullOrWhiteSpace(Definition)
                    ? Definition.Trim()
                    : (!string.IsNullOrWhiteSpace(Meaning) ? Meaning.Trim() : string.Empty))));

    public string? ResolvedDefinitionEn => !string.IsNullOrWhiteSpace(DefinitionEn)
        ? DefinitionEn.Trim()
        : (!string.IsNullOrWhiteSpace(MeaningEn)
            ? MeaningEn.Trim()
            : (!string.IsNullOrWhiteSpace(En) ? En.Trim() : null));
}

public class ExampleExportModel
{
    public string SentenceEn { get; set; } = string.Empty;
    public string SentenceVi { get; set; } = string.Empty;
    public string? En { get; set; }
    public string? Vi { get; set; }
    public string? Sentence { get; set; }
    public string? Translation { get; set; }

    public string ResolvedSentenceEn => !string.IsNullOrWhiteSpace(SentenceEn)
        ? SentenceEn.Trim()
        : (!string.IsNullOrWhiteSpace(En)
            ? En.Trim()
            : (!string.IsNullOrWhiteSpace(Sentence) ? Sentence.Trim() : string.Empty));

    public string ResolvedSentenceVi => !string.IsNullOrWhiteSpace(SentenceVi)
        ? SentenceVi.Trim()
        : (!string.IsNullOrWhiteSpace(Vi)
            ? Vi.Trim()
            : (!string.IsNullOrWhiteSpace(Translation) ? Translation.Trim() : string.Empty));
}

public class GrammarRuleExportModel
{
    public string Title { get; set; } = string.Empty;
    public string Structure { get; set; } = string.Empty;
    public string ExplanationVi { get; set; } = string.Empty;
    public string? ExplanationEn { get; set; }
    public string? CommonMistakes { get; set; }
    public string Level { get; set; } = "B1";
    public string ExamTarget { get; set; } = "General";
    public string? Topic { get; set; }
    public List<GrammarExampleExportModel> Examples { get; set; } = [];
}

public class GrammarExampleExportModel
{
    public string SentenceEn { get; set; } = string.Empty;
    public string SentenceVi { get; set; } = string.Empty;
    public string? Note { get; set; }
}
