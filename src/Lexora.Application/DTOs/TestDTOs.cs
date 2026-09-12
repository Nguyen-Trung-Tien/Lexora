using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record TestConfigurationDto(
    TestType Type = TestType.Mixed,
    int QuestionCount = 10,
    CefrLevel? Level = null,
    Guid? TopicId = null,
    ExamType? ExamType = null,
    bool WeakWordsOnly = false
);

public enum TestScoreTier
{
    Excellent,
    Good,
    NeedsPractice
}

public record TestQuestionDto(
    Guid WordId,
    int QuestionNumber,
    TestType QuestionType,
    string Prompt,
    string? SubPrompt,
    string? AudioPath,
    string CorrectAnswer,
    List<string> Options,
    string Explanation,
    WordDto TargetWord,
    string? UserAnswer = null,
    bool? IsCorrect = null
)
{
    public string CorrectnessText => IsCorrect == true ? "Correct" : "Incorrect";
}

public record TestAnswerSubmissionDto(
    Guid WordId,
    int QuestionNumber,
    string SelectedAnswer,
    int ResponseTimeMs = 0
);

public record TestSessionDto(
    Guid SessionId,
    TestConfigurationDto Configuration,
    DateTime StartedAt,
    List<TestQuestionDto> Questions
);

public record TestResultDto(
    Guid SessionId,
    TestType TestType,
    DateTime StartedAt,
    DateTime CompletedAt,
    int TotalQuestions,
    int CorrectCount,
    int IncorrectCount,
    double ScorePercentage,
    TimeSpan TimeTaken,
    List<TestQuestionDto> Questions,
    List<WordDto> WeakWords,
    List<WordDto> RecommendedReviewWords,
    TestScoreTier ScoreTier = TestScoreTier.NeedsPractice
);
