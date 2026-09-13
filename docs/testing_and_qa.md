# 🧪 Testing & Quality Assurance (QA) Guide

This document details the testing architecture, quality assurance protocols, and automated test suites for **Lexora**.

---

## 📑 Test Suite Overview

Lexora maintains a strict **Zero Error & Zero Warning** policy with **100% test pass rate** across all suites:

| Test Project | Type | Test Count | Target Duration | Dependencies |
| :--- | :--- | :---: | :---: | :--- |
| **`Lexora.Tests.Unit`** | Unit | 40 | < 1.0 s | In-Memory, No I/O, No Database |
| **`Lexora.Tests.Integration`** | Integration | 24 | ~3.0 s | SQLite In-Memory / Isolated Temp DB |
| **Total** | | **64** | **< 5.0 s** | |

---

## 1. Unit Test Suite (`Lexora.Tests.Unit`)

### Test Coverage Taxonomy:
1. **`Sm2SpacedRepetitionEngineTests`**:
   - Boundary tests for Ease Factor floor ($EF \ge 1.3$).
   - Verification of interval progression across ratings (`Again`, `Hard`, `Good`, `Easy`).
   - Lapse handling (reset to 1 day interval, repetitions = 0, status = `Learning`).
2. **`ExportImportServiceTests`**:
   - RFC-4180 CSV parser resilience with multiline quotes and commas.
   - JSON serialization and deserialization integrity.
   - Input payload size boundary enforcement (> 20 MB rejection).
   - Duplicate term deduplication on batch import.
3. **`SecurityAndResilienceTests`**:
   - Path traversal prevention (`..`, encoded delimiters `%2e%2e`).
   - Audio file extension whitelisting (`.mp3`, `.wav`, `.ogg`).
   - Canonical directory boundary checks (`Path.GetFullPath`).
4. **`TestServiceTests`**:
   - Quiz generation logic for all 4 test modes (*Word-to-Meaning*, *Meaning-to-Word*, *Fill-in-the-Blank*, *Multiple Choice*).
   - Distractor generation with same Part-of-Speech matching.
   - Stale session purge logic.
5. **`ReviewSessionTests` & `StatisticsServiceTests`**:
   - Review rating submissions and study log recording.
   - Daily activity heatmap calculations and streak arithmetic.
   - CEFR and Exam readiness aggregations.
6. **`GrammarServiceTests` & `UserPreferencesServiceTests`**:
   - Grammar rule retrieval, filtering, and practice drill verification.
   - User settings persistence and theme defaults.

---

## 2. Integration Test Suite (`Lexora.Tests.Integration`)

### Test Coverage Taxonomy:
1. **`VocabularyCrudIntegrationTests`**:
   - Full CRUD lifecycle for `Word`, `WordMeaning`, `ExampleSentence`, and `ReviewCard`.
   - Cascading delete verification (deleting word removes meanings, examples, and cards).
   - Normalized term uniqueness and fast prefix search queries.
2. **`DatabaseIntegrityAndPerformanceTests`**:
   - SQLite PRAGMA verification (`journal_mode=WAL`, `foreign_keys=ON`, `synchronous=NORMAL`).
   - Transaction rollback on mid-batch constraint violations.
   - Concurrency tests ensuring simultaneous reads during writes.
3. **`DatabaseBackupServiceTests`**:
   - Snapshot creation into temporary `.bak` files.
   - 16-byte SQLite magic header validation (`SQLite format 3\0`).
   - Corrupted file rejection tests without altering the live database.
   - Connection pool clearance (`SqliteConnection.ClearAllPools`).
4. **`GrammarIntegrationTests` & `StatisticsIntegrationTests`**:
   - EF Core relationship mapping between `GrammarRule` and `GrammarExample`.
   - Complex SQL aggregation queries across `StudyLogs` and `UserStreaks`.
5. **`TestPracticeIntegrationTests`**:
   - Real database-driven quiz session lifecycle and score recording.

---

## 3. Running Tests Locally

### Command Line:

```powershell
# Run all tests in the solution
dotnet test Lexora.slnx

# Run only unit tests
dotnet test tests/Lexora.Tests.Unit/Lexora.Tests.Unit.csproj

# Run only integration tests
dotnet test tests/Lexora.Tests.Integration/Lexora.Tests.Integration.csproj

# Run tests with detailed verbosity
dotnet test Lexora.slnx --logger "console;verbosity=detailed"
```

### Visual Studio:
- Open **Test Explorer** (<kbd>Ctrl</kbd> + <kbd>E</kbd>, <kbd>T</kbd>).
- Click **Run All Tests in View**.

---

## 4. Quality Assurance & Pre-Commit Checklist

Before submitting a Pull Request or completing any engineering task:

- [ ] `dotnet build Lexora.slnx` completes with **0 errors and 0 warnings**.
- [ ] `dotnet test Lexora.slnx` completes with **64/64 tests passed**.
- [ ] No raw hex colors added to XAML (only Fluent `{ThemeResource ...}` brushes).
- [ ] No direct `LexoraDbContext` queries made from `Lexora.Presentation`.
- [ ] All new public methods accept `CancellationToken` and pass it down.
- [ ] New file I/O operations have sanitized paths and traversal defenses.
- [ ] Critical UI actions (card ratings, quiz submissions) have concurrency guards.
