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
    double Confidence
);

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

public record ExamTagDto(
    Guid Id,
    ExamType Type,
    string Name,
    string? TargetScore
);
