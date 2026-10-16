# 🧪 Rule 06: Testing & Quality Gate Protocols

---

## 1. Zero Error & Zero Warning Policy

All code in `Lexora.slnx` must build cleanly with:
- **0 Compilation Errors**
- **0 Compiler Warnings** (with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` enabled where configured).
- **100% Test Pass Rate** across all unit and integration test suites.

---

## 2. Test Suites Structure & Responsibilities

```text
tests/
├── Lexora.Tests.Unit/          # 40 Unit Tests
│   ├── Sm2SpacedRepetitionEngineTests.cs
│   ├── ExportImportServiceTests.cs
│   ├── GrammarServiceTests.cs
│   ├── ReviewSessionTests.cs
│   ├── SecurityAndResilienceTests.cs
│   ├── StatisticsServiceTests.cs
│   ├── TestServiceTests.cs
│   └── UserPreferencesServiceTests.cs
└── Lexora.Tests.Integration/   # 24 Integration Tests
    ├── VocabularyCrudIntegrationTests.cs
    ├── DatabaseIntegrityAndPerformanceTests.cs
    ├── DatabaseBackupServiceTests.cs
    ├── GrammarIntegrationTests.cs
    ├── SqliteDatabaseTests.cs
    ├── StatisticsIntegrationTests.cs
    └── TestPracticeIntegrationTests.cs
```

### 2.1. Unit Test Rules (`Lexora.Tests.Unit`)
- Must run completely in-memory without accessing the local disk, network, or live SQLite files.
- Verify purely mathematical models (SM-2 calculations, Ease Factor bounds, rating impacts), DTO transformations, boundary security filters, and business rules.
- Fast execution target: All 40 unit tests must execute in **< 1.5 seconds**.

### 2.2. Integration Test Rules (`Lexora.Tests.Integration`)
- Test repositories, EF Core entity mappings, foreign key cascades, and database transactions.
- Use SQLite in-memory mode (`Data Source=:memory:`) with `SqliteConnection` kept open for the test duration, or unique temporary test databases that are strictly deleted during `DisposeAsync()`.
- Never mutate the developer's live `%LocalAppData%\Lexora\data\lexora.db` database.

---

## 3. Test Naming Conventions

All test methods must follow the `MethodName_Condition_ExpectedResult` standard:
```csharp
[Fact]
public void CalculateNextReview_WhenRatingIsAgain_ResetsRepetitionsAndSchedulesForTomorrow()
{
    // Arrange ...
    // Act ...
    // Assert ...
}

[Theory]
[InlineData("test..txt")]
[InlineData("../secret.db")]
public void SanitizeFileName_WhenPathTraversalAttempted_ThrowsSecurityException(string input)
{
    ...
}
```

---

## 4. Verification Protocol Before Submitting Changes

Every developer and AI agent must run:

```powershell
# 1. Build entire solution
dotnet build Lexora.slnx

# 2. Run all tests
dotnet test Lexora.slnx
```

Both commands must complete with **Exit Code 0** before any code is marked done or committed.
