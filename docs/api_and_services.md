# 🔌 Application Services & Architecture API Reference

This document provides a comprehensive technical reference for the core service interfaces, contracts, DTOs, and persistence managers in **Lexora**.

---

## 📑 Service Catalog

```text
Lexora.Application
├── IVocabularyService               # Vocabulary lifecycle, multi-meaning management, filtering
├── IReviewService                  # Spaced repetition queue retrieval, grading, streak orchestration
├── ISpacedRepetitionEngine          # Pure SM-2 mathematical model calculations
├── IFsrsSpacedRepetitionEngine      # FSRS v5 3D memory model (Stability, Difficulty, Retrievability)
├── IPronunciationListeningLabService # Dictation grading, minimal pairs ear training, UK/US accents
├── IVocabularyKnowledgeGraphService # Semantic relationship network & node pivoting
├── IAdaptiveQuizService             # Computerized Adaptive Testing (CAT) dynamic CEFR recalibration
├── IStudyPlannerService             # Daily curriculum generation, pacing indicators, streak protection
├── ILearningStorageService          # Targeted vault for marked words and grammar rules
├── ITestService                    # Practice quiz generation, question scoring, session maintenance
├── IGrammarService                 # Grammar rule catalog, formula lookups, rule practice
├── IStatisticsService              # Retention analytics, streak queries, CEFR & exam readiness
├── ISmartSearchService             # Intelligent cached & fuzzy multi-field search engine
└── IExportImportService            # RFC-4180 CSV & JSON serialization, validation, deduplication

Lexora.Infrastructure
├── IDatabaseBackupService          # Snapshot creation, SQLite header verification, recovery
├── IDataHealthService              # SQLite PRAGMA integrity, vacuuming, WAL maintenance, audio cache
├── IUpdateService                  # GitHub Releases update check, hash verification, silent installer
├── ILocalAudioStorageService       # Local audio file caching, filename traversal sanitization
└── IUnitOfWork                     # Atomic single-commit database transaction boundary
```

---

## 1. `IVocabularyService` & DTOs

Namespace: `Lexora.Application.Contracts`

Manages dictionary queries, term normalization, definition assignments, and automatic review card creation.

```csharp
public interface IVocabularyService
{
    Task<IReadOnlyList<WordDto>> SearchWordsAsync(VocabularySearchFilter filter, CancellationToken ct = default);
    Task<WordDetailsDto?> GetWordByIdAsync(Guid wordId, CancellationToken ct = default);
    Task<Guid> CreateWordAsync(CreateWordCommand command, CancellationToken ct = default);
    Task UpdateWordAsync(UpdateWordCommand command, CancellationToken ct = default);
    Task DeleteWordAsync(Guid wordId, CancellationToken ct = default);
    Task<IReadOnlyList<TopicDto>> GetAllTopicsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ExamTagDto>> GetAllExamTagsAsync(CancellationToken ct = default);
}
```

### Key DTO Contracts:
- `VocabularySearchFilter`: Positional record supporting `Query`, `CefrLevel`, `TopicId`, `ExamType`, `PageIndex`, `PageSize`.
- `WordDetailsDto`: Full view model record encapsulating word properties, list of `WordMeaningDto`, nested `ExampleSentenceDto`, and associated tags.
- `CreateWordCommand`: Validation contract; automatically creates the matching `ReviewCard` in the database within the same atomic transaction.

---

## 2. `IReviewService` & `ISpacedRepetitionEngine`

Namespace: `Lexora.Application.Contracts`

Coordinates active recall sessions, SuperMemo SM-2 mathematical calculations, and daily learning streak updates.

```csharp
public interface IReviewService
{
    Task<IReadOnlyList<ReviewCardDto>> GetDueCardsAsync(int limit = 50, CancellationToken ct = default);
    Task<ReviewCardDto?> GetCardForWordAsync(Guid wordId, CancellationToken ct = default);
    Task<ReviewResultDto> SubmitReviewAsync(Guid cardId, ReviewRating rating, int responseTimeMs, CancellationToken ct = default);
    Task<ReviewQueueSummaryDto> GetQueueSummaryAsync(CancellationToken ct = default);
}

public interface ISpacedRepetitionEngine
{
    Sm2CalculationResult CalculateNextReview(
        double currentEaseFactor,
        int currentIntervalDays,
        int currentRepetitions,
        ReviewRating rating,
        DateTime referenceTimeUtc);
}
```

### Invariants:
- `Sm2CalculationResult`: Returns `NewEaseFactor` ($\ge 1.3$), `NewIntervalDays` ($\ge 1$), `NewRepetitions`, `NextReviewAtUtc`, and `IsLapse`.
- `SubmitReviewAsync`:
  1. Retrieves card entity with tracking.
  2. Executes `CalculateNextReview`.
  3. Appends a new `StudyLog` entry with response time latency.
  4. Calls `IReviewRepository.UpdateStreakAsync()`.
  5. Commits atomically via `_unitOfWork.SaveChangesAsync(ct)`.

---

## 3. `ITestService`

Namespace: `Lexora.Application.Contracts`

Generates dynamic quizzes and scores user answers across 4 question modes:

```csharp
public interface ITestService
{
    Task<TestSessionDto> GenerateTestAsync(TestConfigurationDto config, CancellationToken ct = default);
    Task<QuestionEvaluationDto> SubmitAnswerAsync(Guid sessionId, Guid questionId, string submittedAnswer, CancellationToken ct = default);
    Task<TestSummaryDto> CompleteSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task CleanupStaleSessionsAsync(TimeSpan maxAge, CancellationToken ct = default);
}
```

### Quiz Generation Logic:
- Distractor generation extracts random false definitions/words from distinct entries in the database matching the same Part of Speech (POS) to provide authentic, challenging distractors.
- Stale sessions (> 2 hours) are purged automatically via background or startup tasks to avoid memory accumulation.

---

## 4. `IGrammarService`

Namespace: `Lexora.Application.Contracts`

Catalog and practice engine for English grammar structures:

```csharp
public interface IGrammarService
{
    Task<IReadOnlyList<GrammarRuleSummaryDto>> GetRulesAsync(GrammarFilter filter, CancellationToken ct = default);
    Task<GrammarRuleDetailsDto?> GetRuleByIdAsync(Guid ruleId, CancellationToken ct = default);
    Task<Guid> CreateRuleAsync(CreateGrammarRuleCommand command, CancellationToken ct = default);
    Task UpdateRuleAsync(UpdateGrammarRuleCommand command, CancellationToken ct = default);
    Task DeleteRuleAsync(Guid ruleId, CancellationToken ct = default);
    Task<IReadOnlyList<GrammarPracticeQuestionDto>> GeneratePracticeDrillAsync(Guid ruleId, int count = 5, CancellationToken ct = default);
}
```

---

## 5. `IStatisticsService`

Namespace: `Lexora.Application.Contracts`

Provides aggregated analytics for the Dashboard:

```csharp
public interface IStatisticsService
{
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DailyActivityDto>> GetDailyActivityHeatmapAsync(int days = 365, CancellationToken ct = default);
    Task<CefrDistributionDto> GetCefrDistributionAsync(CancellationToken ct = default);
    Task<ExamReadinessDto> GetExamReadinessAsync(ExamType examType, CancellationToken ct = default);
}
```

---

## 6. `IExportImportService`

Namespace: `Lexora.Application.Contracts`

Data exchange engine with schema validation:

```csharp
public interface IExportImportService
{
    Task<ExportResultDto> ExportToJsonAsync(Stream destinationStream, CancellationToken ct = default);
    Task<ExportResultDto> ExportToCsvAsync(Stream destinationStream, CancellationToken ct = default);
    Task<ImportResultDto> ImportFromJsonAsync(Stream sourceStream, CancellationToken ct = default);
    Task<ImportResultDto> ImportFromCsvAsync(Stream sourceStream, CancellationToken ct = default);
}
```

- Enforces a 20 MB ceiling before reading streams.
- Performs normalized term deduplication.
- Executes batch insertion inside an atomic transaction.

---

## 7. `IDatabaseBackupService` & `ILocalAudioStorageService`

Namespace: `Lexora.Infrastructure.Contracts`

```csharp
public interface IDatabaseBackupService
{
    Task<BackupResultDto> CreateBackupAsync(string targetFilePath, CancellationToken ct = default);
    Task<RestoreResultDto> RestoreBackupAsync(string sourceBackupPath, CancellationToken ct = default);
}

public interface ILocalAudioStorageService
{
    Task<string> StoreAudioAsync(string term, Stream audioStream, string extension, CancellationToken ct = default);
    Task<Stream?> GetAudioStreamAsync(string relativePath, CancellationToken ct = default);
    bool DeleteAudio(string relativePath);
}
```

- `RestoreBackupAsync`: Checks the 16-byte SQLite magic header, clears connection pools, removes `-wal` and `-shm` companion files, and swaps the file safely.
- `StoreAudioAsync`: Sanitizes the file name with regex whitelisting and validates directory canonical boundaries before writing.

---

## 8. `IPronunciationListeningLabService`

Namespace: `Lexora.Application.Interfaces`

Provides comprehensive listening, dictation, and phoneme discrimination drills:

```csharp
public interface IPronunciationListeningLabService
{
    Task<DictationResultDto> GradeDictationAsync(string expectedText, string submittedText, CancellationToken ct = default);
    Task<IReadOnlyList<MinimalPairExerciseDto>> GetMinimalPairExercisesAsync(CancellationToken ct = default);
    Task<AccentComparisonDto> GetAccentComparisonAsync(string wordOrPhrase, CancellationToken ct = default);
}
```

- Calculates Levenshtein edit distance and similarity percentage for audio dictation drills.
- Provides articulatory mouth-position guidance for minimal pair phoneme discrimination.
- Resolves UK and US audio streams for side-by-side accent comparison.

---

## 9. `IVocabularyKnowledgeGraphService`

Namespace: `Lexora.Application.Interfaces`

Extracts and models semantic networks across vocabulary items:

```csharp
public interface IVocabularyKnowledgeGraphService
{
    Task<KnowledgeGraphDto> BuildGraphAsync(Guid rootWordId, int maxDepth = 2, CancellationToken ct = default);
    Task<IReadOnlyList<WordNodeDto>> FindRelatedWordsAsync(string term, CancellationToken ct = default);
}
```

- Traverses relationships across synonyms, antonyms, shared topics, and CEFR tiers.
- Powers dynamic node pivoting and interactive preview cards in `KnowledgeGraphDialog`.

---

## 10. `IAdaptiveQuizService` & `IStudyPlannerService`

Namespace: `Lexora.Application.Interfaces`

- **`IAdaptiveQuizService`**: Computerized Adaptive Testing (CAT) dynamic CEFR recalibration (+1 level on consecutive correct answers, -1 level on errors), generating POS-matched distractors and post-quiz level estimation.
- **`IStudyPlannerService`**: Generates daily curricula (due cards, new words, quizzes, grammar rules), tracks pacing against weekly goals, and manages streak preservation.

---

## 11. `ILearningStorageService` & `IDataHealthService`

Namespace: `Lexora.Application.Interfaces` & `Lexora.Infrastructure.Services`

- **`ILearningStorageService`**: Dedicated CRUD and status transition manager for `LearningStorageItem` entries.
- **`IDataHealthService`**: Executes SQLite `PRAGMA integrity_check`, WAL checkpointing, database vacuuming, orphan record purging, and audio cache analysis.
