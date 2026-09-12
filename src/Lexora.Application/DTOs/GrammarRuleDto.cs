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

public record GrammarRuleCreateOrUpdateDto(
    Guid? Id,
    string Title,
    string Structure,
    string ExplanationVi,
    string? ExplanationEn,
    string? CommonMistakes,
    CefrLevel Level,
    ExamType ExamTarget,
    string? Topic,
    List<GrammarExampleCreateDto> Examples
);

public record GrammarExampleCreateDto(
    string SentenceEn,
    string SentenceVi,
    string? Note
);

public record GrammarQuizQuestionDto(
    Guid RuleId,
    int QuestionNumber,
    string Prompt,
    string? SubPrompt,
    string CorrectAnswer,
    List<string> Options,
    string Explanation,
    string? UserAnswer = null,
    bool? IsCorrect = null
)
{
    public string CorrectnessBadgeColor => IsCorrect == true ? "#107C41" : "#D83B01";
    public string CorrectnessText => IsCorrect == true ? "Correct" : "Incorrect";
}

public record GrammarQuizSubmissionDto(
    Guid RuleId,
    int QuestionNumber,
    string SelectedAnswer
);

public record GrammarPracticeResultDto(
    int TotalQuestions,
    int CorrectCount,
    int IncorrectCount,
    double ScorePercentage,
    List<GrammarQuizQuestionDto> Questions
);
