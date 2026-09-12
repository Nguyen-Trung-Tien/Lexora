# Lexora — English Learning Manager

> High-performance, offline-first English learning and vocabulary mastery desktop application built with **C# / .NET 10**, **WinUI 3 (Windows App SDK)**, **Clean Architecture**, and **Entity Framework Core with SQLite**.

---

## 🏛️ Clean Architecture Overview

Lexora strictly follows Clean Architecture and Domain-Driven Design principles:

```text
Lexora.Presentation (WinUI 3 Desktop App / MVVM)
    │
    ├──► Lexora.Application (Use Cases, SM-2 Engine, DTOs, Service Interfaces)
    │        │
    │        └──► Lexora.Domain (Entities, Value Objects, Enums, Contracts)
    │                  ▲
    └──► Lexora.Infrastructure (EF Core 10, SQLite, Repositories, File Storage)
             │
             └──► Lexora.Application
```

### Layer Breakdown

1. **`Lexora.Domain`** (`net10.0`):
   - Pure domain models: `Word`, `WordMeaning`, `ExampleSentence`, `Topic`, `ExamTag`, `ReviewCard`, `StudySession`, `StudyLog`, `GrammarRule`, `GrammarExample`, `UserStreak`.
   - Domain enums: `PartOfSpeech`, `CefrLevel`, `ExamType` (TOEIC, VSTEP, IELTS), `LearningStatus`, `ReviewRating`, `TestType`.
   - Repository interfaces (`IVocabularyRepository`, `IReviewRepository`, `IGrammarRepository`, `IUnitOfWork`).
   - Zero UI or third-party ORM dependencies.

2. **`Lexora.Application`** (`net10.0`):
   - SuperMemo SM-2 Spaced Repetition engine (`Sm2SpacedRepetitionEngine`).
   - Service contracts & implementations: `VocabularyService`, `ReviewService`, `StatisticsService`, `GrammarService`, `ExportImportService`.
   - Data Transfer Objects (DTOs) decoupled from persistence entities.

3. **`Lexora.Infrastructure`** (`net10.0`):
   - Entity Framework Core with SQLite (`LexoraDbContext`).
   - Repository implementations & transaction coordination (`UnitOfWork`).
   - Automatic database migration & starter pack data seeding (`SqliteDatabaseInitializer`).
   - Local audio pronunciation caching (`LocalAudioStorageService`) and backup/restore (`DatabaseBackupService`).

4. **`Lexora.Presentation`** (`net10.0-windows10.0.26100.0`):
   - Windows App SDK / WinUI 3 desktop application with modern Fluent Design (Mica backdrop).
   - MVVM architecture powered by `CommunityToolkit.Mvvm` (partial properties, commands).
   - Shell navigation (`NavigationView`) connecting Dashboard, Vocabulary, Review, Grammar, and Settings.
   - Dynamic Theme support (`System`, `Light`, `Dark`) and multi-language Localization (`Tiếng Việt`, `English`).

5. **`tests/`**:
   - `Lexora.Tests.Unit`: Comprehensive unit tests for SM-2 spaced repetition calculation and domain business rules.
   - `Lexora.Tests.Integration`: Integration tests for SQLite database lifecycle, relational foreign keys, cascades, and repository queries.

---

## 🚀 Quick Start

### Prerequisites
- Windows 10 (version 1809+) or Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows App SDK runtime (installed automatically or available via Windows Update)

### Building the Solution
```powershell
dotnet build Lexora.slnx
```

### Running Tests
```powershell
dotnet test Lexora.slnx
```

### Running the Application
```powershell
dotnet run --project src/Lexora.Presentation/Lexora.Presentation.csproj
```

---

## 🗺️ 8-Phase Master Roadmap

> Chi tiết tiến độ từng tính năng được cập nhật tại [PROGRESS.md](file:///d:/LEARN/Lexora/PROGRESS.md).

- [x] **Phase 1 — Foundation**: Solution setup, Clean Architecture, DI, EF Core + SQLite, MVVM, Theme/Localization, Unit & Integration tests.
- [x] **Phase 2 — Vocabulary**: Full CRUD, multiple meanings, IPA + audio player, CEFR levels, TOEIC/VSTEP exam tagging, CSV/JSON import/export.
- [x] **Phase 3 — Learning & Review**: Leitner/SM-2 spaced repetition, Remembered/NotRemembered tracking, NextReviewAt scheduling, daily reviews & interactive flip flashcards.
- [ ] **Phase 4 — Vocabulary Test**: Word-to-Meaning, Meaning-to-Word, Multiple Choice, Fill-in-the-blank, Part-of-Speech, and Mixed quizzes.
- [ ] **Phase 5 — Grammar**: Grammar structure explorer, explanations, examples, common mistake alerts, and grammar quizzes.
- [ ] **Phase 6 — Dashboard & Statistics**: Learning streaks, retention accuracy charts, TOEIC/VSTEP progress breakdowns, study history timeline.
- [ ] **Phase 7 — UI/UX Polish**: Fluent animations, keyboard shortcuts, accessibility, high-contrast & reduced motion support.
- [ ] **Phase 8 — Quality & Release**: Full regression testing, performance profiling, backup verification, release packaging.
