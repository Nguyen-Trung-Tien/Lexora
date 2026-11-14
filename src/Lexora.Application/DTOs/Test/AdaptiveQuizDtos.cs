using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record AdaptiveQuizConfigDto(
    int QuestionCount = 10,
    ExamType ExamTarget = ExamType.General,
    CefrLevel InitialLevel = CefrLevel.B1,
    bool FocusWeakWords = true,
    int TimeLimitSeconds = 0 // 0 = untimed
);

public record AdaptiveQuestionDto(
    Guid SessionId,
    int QuestionIndex,
    Guid WordId,
    string PromptTerm,
    string QuestionText,
    TestType QuestionType,
    CefrLevel DifficultyLevel,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex,
    string Explanation,
    string? ExampleSentence,
    string? AudioPath
);

public record AdaptiveAnswerEvaluationDto(
    int QuestionIndex,
    Guid WordId,
    bool IsCorrect,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    int ResponseTimeMs,
    CefrLevel PreviousCalibratedLevel,
    CefrLevel NewCalibratedLevel,
    double RunningScorePercent,
    string FeedbackMessage
);

public record AdaptiveQuizSummaryDto(
    Guid SessionId,
    int TotalQuestions,
    int CorrectCount,
    int IncorrectCount,
    double AccuracyPercentage,
    int AverageResponseTimeMs,
    CefrLevel StartingLevel,
    CefrLevel FinalEstimatedLevel,
    string EstimatedToeicBand,
    string EstimatedIeltsBand,
    string EstimatedVstepLevel,
    IReadOnlyList<WordDto> DifficultWordsToReview,
    IReadOnlyList<string> KeyStrengths,
    IReadOnlyList<string> RecommendedNextSteps,
    DateTime CompletedAt
);
