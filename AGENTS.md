# AGENTS.md — Developer & AI Agent Guidelines for Lexora

Welcome to the Lexora codebase. Any agent working on this repository MUST strictly follow the architecture and development guidelines outlined below.

---

## 1. Architectural Integrity & Boundary Rules

1. **Clean Architecture Hierarchy**:
   - `Lexora.Domain` CANNOT reference any other project or third-party ORM/UI framework. It must remain pure .NET.
   - `Lexora.Application` can only reference `Lexora.Domain`.
   - `Lexora.Infrastructure` references `Lexora.Domain` and `Lexora.Application`.
   - `Lexora.Presentation` references `Lexora.Application`, `Lexora.Infrastructure`, and `Lexora.Domain`.
   - **Crucial Rule**: The Presentation layer (ViewModels and Views) MUST NEVER directly execute EF Core queries or reference `LexoraDbContext`. All business data and queries must pass through Application services or repository interfaces.

2. **Database & Migrations**:
   - The database is SQLite, stored by default in `%LocalAppData%/Lexora/data/lexora.db`.
   - Entity configurations reside in `LexoraDbContext.OnModelCreating`.
   - Do NOT introduce breaking database schema changes without evaluating backward compatibility and migration impacts.

---

## 2. Coding Standards & Conventions

- **Target Framework**: .NET 10 (`net10.0` for class libraries, `net10.0-windows10.0.26100.0` for WinUI).
- **C# Language Features**: C# 13/14 modern syntax:
  - Use primary constructors where appropriate.
  - Use file-scoped namespaces (`namespace Lexora.Domain;`).
  - Enable Nullable Reference Types (`#nullable enable`).
  - Use async/await with `CancellationToken` support in all service and repository methods.
- **MVVM Conventions**:
  - ViewModels inherit from `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.
  - Use `[ObservableProperty] public partial ...` on properties (C# 13 partial properties) to ensure full WinRT / CsWinRT AOT compatibility and avoid compiler warnings.
  - Use `[RelayCommand]` on methods for command binding.

---

## 3. Spaced Repetition (SM-2) Integrity

- Spaced repetition calculations must be routed through `ISpacedRepetitionEngine` (`Sm2SpacedRepetitionEngine`).
- `EaseFactor` must never drop below 1.3.
- All review submissions must log a `StudyLog` entry and invoke `IReviewRepository.UpdateStreakAsync`.

---

## 4. Build & Verification Commands

Before completing any task or sub-phase, agents MUST run and verify:

```powershell
# 1. Build entire solution
dotnet build Lexora.slnx

# 2. Run all unit and integration tests
dotnet test Lexora.slnx
```

All tests MUST pass with 0 errors and 0 warnings.
