using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public class WordExportModel
{
    public string Term { get; set; } = string.Empty;
    public string? Word { get; set; }
    public string? PhoneticUk { get; set; }
    public string? PhoneticUs { get; set; }
    public CefrLevel Level { get; set; }
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
    public List<string> Topics { get; set; } = new();
    public List<string> ExamTags { get; set; } = new();

    public string ResolvedTerm => !string.IsNullOrWhiteSpace(Term)
        ? Term.Trim()
        : (!string.IsNullOrWhiteSpace(Word) ? Word.Trim() : string.Empty);

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
}

public class MeaningExportModel
{
    public PartOfSpeech PartOfSpeech { get; set; }
    public string? Pos { get; set; }
    public string DefinitionVi { get; set; } = string.Empty;
    public string? DefinitionEn { get; set; }
    public string? Definition { get; set; }
    public string? Meaning { get; set; }
    public string? MeaningVi { get; set; }
    public string? MeaningEn { get; set; }
    public string? Explanation { get; set; }
    public List<ExampleExportModel> Examples { get; set; } = new();

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
