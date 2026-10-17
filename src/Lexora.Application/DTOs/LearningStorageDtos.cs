using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record LearningStorageItemDto(
    Guid Id,
    StorageItemType ItemType,
    Guid? WordId,
    WordDto? Word,
    Guid? GrammarRuleId,
    GrammarRuleDto? GrammarRule,
    StorageLearningStatus Status,
    StorageAdditionReason ReasonAdded,
    string? Note,
    int IncorrectCount,
    int CorrectCount,
    double EaseFactor,
    int IntervalDays,
    int Repetitions,
    int Lapses,
    double RetentionProbability,
    DateTime? LastReviewedAt,
    DateTime NextReviewAt,
    DateTime CreatedAt,
    string? CustomDefinitionOrExplanation = null,
    string? CustomSubtitle = null)
{
    public string Title => ItemType == StorageItemType.Vocabulary
        ? (Word?.Term ?? string.Empty)
        : (GrammarRule?.Title ?? string.Empty);

    public string Subtitle => CustomSubtitle ?? (ItemType == StorageItemType.Vocabulary
        ? $"{Word?.PhoneticUs ?? Word?.PhoneticUk ?? string.Empty} • {(Word?.PrimaryPartOfSpeech != null ? WordDto.GetAbbreviation(Word.PrimaryPartOfSpeech) : string.Empty)}"
        : (GrammarRule?.Structure ?? string.Empty));

    public string DefinitionOrExplanation
    {
        get
        {
            if (CustomDefinitionOrExplanation != null)
                return CustomDefinitionOrExplanation;

            bool isVi = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
            if (ItemType == StorageItemType.Vocabulary)
            {
                var meaning = Word?.Meanings.FirstOrDefault();
                if (meaning == null) return string.Empty;
                return (!isVi && !string.IsNullOrWhiteSpace(meaning.DefinitionEn))
                    ? meaning.DefinitionEn
                    : meaning.DefinitionVi;
            }
            else
            {
                if (GrammarRule == null) return string.Empty;
                return (!isVi && !string.IsNullOrWhiteSpace(GrammarRule.ExplanationEn))
                    ? GrammarRule.ExplanationEn
                    : GrammarRule.ExplanationVi;
            }
        }
    }

    public bool IsDueToday => NextReviewAt <= DateTime.UtcNow && Status != StorageLearningStatus.Mastered;
    public bool IsWeak => Status == StorageLearningStatus.Weak;
    public bool IsLearning => Status == StorageLearningStatus.Learning;
    public bool IsMastered => Status == StorageLearningStatus.Mastered;
    public bool IsNew => Status == StorageLearningStatus.New;
    public bool IsVocabulary => ItemType == StorageItemType.Vocabulary;

    public string FormattedRetention => $"{Math.Round(RetentionProbability, 1)}%";
    public string FormattedInterval => IntervalDays <= 1 ? "1d" : $"{IntervalDays}d";
}

public record StorageStatisticsDto(
    int Total,
    int VocabCount,
    int GrammarCount,
    int NewCount,
    int LearningCount,
    int WeakCount,
    int MasteredCount,
    int DueTodayCount,
    double AverageRetention);

public record StoragePagedResultDto(
    IReadOnlyList<LearningStorageItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public record StoragePracticeQuestionDto(
    Guid StorageItemId,
    StorageItemType ItemType,
    Guid ReferenceId,
    int QuestionNumber,
    string Prompt,
    string? SubPrompt,
    string? AudioPath,
    string CorrectAnswer,
    IReadOnlyList<string> Options,
    string Explanation,
    string? UserAnswer = null,
    bool? IsCorrect = null,
    WordDto? Word = null,
    GrammarRuleDto? GrammarRule = null);

public record StoragePracticeSubmissionDto(
    Guid StorageItemId,
    string SelectedAnswer,
    int ResponseTimeMs = 0);

public record StoragePracticeResultDto(
    int Total,
    int Correct,
    int Incorrect,
    double ScorePercent,
    IReadOnlyList<StoragePracticeQuestionDto> EvaluatedQuestions);
