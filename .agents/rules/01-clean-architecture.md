# 🏛️ Rule 01: Clean Architecture & Layer Integrity

---

## 1. Architectural Hierarchy & Reference Rules

Lexora enforces strict unidirectional layer dependencies according to Clean Architecture:

```text
┌────────────────────────────────────────────────────────┐
│                  Lexora.Presentation                   │
│         (WinUI 3 • MVVM • Shell • ViewModels)          │
└───────────────┬────────────────────────┬───────────────┘
                │                        │
                ▼                        │
┌────────────────────────────────┐       │
│       Lexora.Application       │       │
│ (Use Cases • SM-2 • DTO Ports) │       │
└───────────────┬────────────────┘       │
                │                        │
                ▼                        ▼
┌────────────────────────────────┐  ┌────────────────────────────┐
│         Lexora.Domain          │◄─┤   Lexora.Infrastructure    │
│(Entities • Value Objects • Repo│  │(EF Core 10 • SQLite • Caches│
└────────────────────────────────┘  └────────────────────────────┘
```

### Reference Invariants:
1. **`Lexora.Domain`** (`net10.0`):
   - **CANNOT** reference any other project in the solution.
   - **CANNOT** reference EF Core, SQLite, WinUI, or third-party frameworks.
   - Contains pure C# domain entities (`Word`, `WordMeaning`, `ExampleSentence`, `ReviewCard`, `StudySession`, `StudyLog`, `GrammarRule`, `GrammarExample`, `UserStreak`), enums, and repository abstractions (`IVocabularyRepository`, etc.).
2. **`Lexora.Application`** (`net10.0`):
   - References **ONLY** `Lexora.Domain`.
   - Contains service interfaces, use case implementations, business DTOs, and the core SM-2 algorithm engine (`Sm2SpacedRepetitionEngine`).
   - Contains no direct database or I/O access code.
3. **`Lexora.Infrastructure`** (`net10.0`):
   - References `Lexora.Domain` and `Lexora.Application`.
   - Implements repository interfaces, `IUnitOfWork`, file system audio caching (`LocalAudioStorageService`), and backup routines (`DatabaseBackupService`) via EF Core 10 and SQLite.
4. **`Lexora.Presentation`** (`net10.0-windows10.0.26100.0`):
   - References `Lexora.Application`, `Lexora.Infrastructure` (solely for dependency injection wiring in `App.xaml.cs`), and `Lexora.Domain`.
   - Contains WinUI 3 views, controls, dialogs, and MVVM ViewModels.

---

## 2. Strict Boundary Rules

### 🚨 Critical Anti-Patterns (Forbidden)
1. **Direct EF Core Queries in Presentation**:
   - ViewModels and Views **MUST NEVER** inject `LexoraDbContext` or execute LINQ-to-Entities queries directly.
   - All data fetching and persistence **MUST** pass through Application service interfaces (e.g., `IVocabularyService`, `IReviewService`, `ITestService`, `IGrammarService`, `IStatisticsService`).
2. **Entity Leakage into UI Binding**:
   - Where operations require mutations, pass Data Transfer Objects (DTOs) or domain models via defined service contracts.
   - Do not bind EF Core change-tracked entities directly to two-way input fields if concurrent modifications or partial cancellation can corrupt DbContext state.
3. **Circular Dependencies**:
   - Circular project references are strictly forbidden by build configuration.
4. **Bypassing the Unit of Work**:
   - Database mutations must be committed atomically via `await _unitOfWork.SaveChangesAsync(cancellationToken)`.
   - Intermediate partial saves that leave relational entities in half-persisted states are prohibited.

---

## 3. Dependency Injection Conventions

All dependencies must be wired in `App.xaml.cs` via `Microsoft.Extensions.Hosting` (`IHost` / `IServiceCollection`):

- **Singletons**: Stateless engines, storage services, and application orchestrators:
  - `ISpacedRepetitionEngine` -> `Sm2SpacedRepetitionEngine`
  - `ILocalAudioStorageService` -> `LocalAudioStorageService`
  - `INavigationService` -> `NavigationService`
  - `IThemeService` -> `ThemeService`
  - `ILocalizationService` -> `LocalizationService`
  - `IAudioPlayerService` -> `WindowsAudioPlayerService`
- **Scoped**: Database contexts, repositories, unit of work, and application services:
  - `LexoraDbContext`
  - `IUnitOfWork` -> `UnitOfWork`
  - `IVocabularyService`, `IReviewService`, `ITestService`, etc.
- **Transient**: ViewModels:
  - `ShellViewModel`, `DashboardViewModel`, `VocabularyViewModel`, `ReviewViewModel`, `TestViewModel`, `GrammarViewModel`, `SettingsViewModel`.
