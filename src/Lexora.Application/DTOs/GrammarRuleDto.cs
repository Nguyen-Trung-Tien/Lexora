using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record GrammarRuleDto(
    Guid Id,
    string Title,
    string Structure,
    string ExplanationVi,
    string? ExplanationEn,
    string? CommonMistakes,
    CefrLevel Level,
    ExamType ExamTarget,
    string? Topic,
    List<GrammarExampleDto> Examples
);

public record GrammarExampleDto(
    Guid Id,
    string SentenceEn,
    string SentenceVi,
    string? Note
);
