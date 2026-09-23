using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record WordDto(
    Guid Id,
    string Term,
    string? PhoneticUk,
    string? PhoneticUs,
    string? AudioUkPath,
    string? AudioUsPath,
    CefrLevel Level,
    PartOfSpeech PrimaryPartOfSpeech,
    string? Notes,
    List<WordMeaningDto> Meanings,
    List<string> Topics,
    List<string> ExamTags,
    LearningStatus? LearningStatus,
    DateTime? NextReviewAt,
    double Confidence,
    int IntervalDays = 0,
    double EaseFactor = 2.5,
    int Lapses = 0,
    int Repetitions = 0,
    int TotalReviews = 0,
    double RetentionRate = 100.0
)
{
    public string PrimaryDefinition => Meanings.FirstOrDefault()?.DefinitionVi ?? string.Empty;
    public bool IsDueNow => NextReviewAt.HasValue && NextReviewAt.Value <= DateTime.UtcNow;

    public IReadOnlyList<PartOfSpeech> AllPartsOfSpeech
    {
        get
        {
            var list = new List<PartOfSpeech> { PrimaryPartOfSpeech };
            if (Meanings != null)
            {
                foreach (var m in Meanings)
                {
                    if (!list.Contains(m.PartOfSpeech))
                    {
                        list.Add(m.PartOfSpeech);
                    }
                }
            }
            return list;
        }
    }

    public bool HasMultiplePartsOfSpeech => AllPartsOfSpeech.Count > 1;

    public bool IsIdiom => AllPartsOfSpeech.Contains(PartOfSpeech.Idiom);

    public bool IsPhrasalVerb => AllPartsOfSpeech.Contains(PartOfSpeech.PhrasalVerb);

    public bool IsPhrase => AllPartsOfSpeech.Any(p => p is PartOfSpeech.Phrase or PartOfSpeech.NounPhrase);

    public bool IsIdiomOrPhrase => AllPartsOfSpeech.Any(p =>
        p is PartOfSpeech.Idiom or PartOfSpeech.NounPhrase or PartOfSpeech.PhrasalVerb or PartOfSpeech.Phrase);

    public string PartsOfSpeechDisplay
    {
        get
        {
            var abbreviations = AllPartsOfSpeech.Select(GetAbbreviation);
            return string.Join(" • ", abbreviations);
        }
    }

    public static string GetAbbreviation(PartOfSpeech pos) => pos switch
    {
        PartOfSpeech.Noun => "n.",
        PartOfSpeech.Verb => "v.",
        PartOfSpeech.Adjective => "adj.",
        PartOfSpeech.Adverb => "adv.",
        PartOfSpeech.PhrasalVerb => "phr. v.",
        PartOfSpeech.NounPhrase => "n. phr.",
        PartOfSpeech.Idiom => "idiom",
        PartOfSpeech.Preposition => "prep.",
        PartOfSpeech.Conjunction => "conj.",
        PartOfSpeech.Interjection => "interj.",
        _ => pos.ToString().ToLowerInvariant()
    };
}

public record WordMeaningDto(
    Guid Id,
    PartOfSpeech PartOfSpeech,
    string DefinitionVi,
    string? DefinitionEn,
    string? Explanation,
    int OrderIndex,
    List<ExampleSentenceDto> Examples
);

public record ExampleSentenceDto(
    Guid Id,
    string SentenceEn,
    string SentenceVi,
    string? AudioPath
);

public record WordCreateOrUpdateDto(
    Guid? Id,
    string Term,
    string? PhoneticUk,
    string? PhoneticUs,
    string? AudioUkPath,
    string? AudioUsPath,
    CefrLevel Level,
    PartOfSpeech PrimaryPartOfSpeech,
    string? Notes,
    List<WordMeaningCreateDto> Meanings,
    List<Guid> TopicIds,
    List<Guid> ExamTagIds
);

public record WordMeaningCreateDto(
    PartOfSpeech PartOfSpeech,
    string DefinitionVi,
    string? DefinitionEn,
    string? Explanation,
    List<ExampleSentenceCreateDto> Examples
);

public record ExampleSentenceCreateDto(
    string SentenceEn,
    string SentenceVi,
    string? AudioPath
);

public record TopicDto(
    Guid Id,
    string Name,
    string? Description,
    string? IconGlyph,
    string? ColorHex
);

public record TopicCreateOrUpdateDto(
    string Name,
    string? Description = null,
    string? IconGlyph = null,
    string? ColorHex = null
);


public record ExamTagDto(
    Guid Id,
    ExamType Type,
    string Name,
    string? TargetScore
);

public record ContextUnitDto(
    Guid Id,
    string Name,
    string? Description,
    string? IconGlyph,
    string? ColorHex,
    int TotalWords,
    int RememberedWords,
    int NotRememberedWords,
    double MasteryPercentage,
    IReadOnlyList<WordDto> Words
);

