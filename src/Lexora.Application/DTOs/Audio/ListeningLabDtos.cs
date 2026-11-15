namespace Lexora.Application.DTOs;

public record ListeningDictationChallengeDto(
    Guid WordId,
    string Term,
    string? PhoneticUk,
    string? PhoneticUs,
    string DefinitionVi,
    string? DefinitionEn,
    string Level,
    string? ExampleSentenceEn,
    string? ExampleSentenceVi
);

public enum DictationDiffType
{
    Correct,
    Incorrect,
    Missing,
    Extra
}

public record DictationDiffTokenDto(
    string Text,
    DictationDiffType Type,
    string? Expected = null
);

public record DictationEvaluationResultDto(
    bool IsExactMatch,
    int LevenshteinDistance,
    double AccuracyPercent,
    string ExpectedTerm,
    string UserInput,
    string FeedbackMessage,
    string? Phonetic,
    IReadOnlyList<DictationDiffTokenDto>? DiffTokens = null,
    IReadOnlyList<DictationDiffTokenDto>? WordDiffTokens = null,
    double WordAccuracyPercent = 0,
    int MatchedWords = 0,
    int TotalWords = 0
);

public record MinimalPairChallengeDto(
    Guid Id,
    string PhonemePairLabel,
    string WordA,
    string PhoneticA,
    string MeaningA,
    string WordB,
    string PhoneticB,
    string MeaningB,
    string TargetWord,
    string TargetPhonetic,
    string Tip
);

public record AccentComparisonItemDto(
    Guid WordId,
    string Term,
    string? PhoneticUk,
    string? PhoneticUs,
    string DefinitionVi,
    string KeyDifferenceNote
);

public record ListeningLabSummaryDto(
    int TotalChallengesCompleted,
    int CorrectChallenges,
    double AccuracyPercent,
    int TotalMinimalPairsCompleted,
    int CorrectMinimalPairs
);
