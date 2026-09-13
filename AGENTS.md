# AGENTS.md — Developer & AI Agent Guidelines for Lexora

Welcome to the Lexora codebase. Any agent or developer working on this repository MUST strictly adhere to the architecture, design, and code quality standards outlined below.

---

## 1. Architectural Integrity & Boundary Rules

1. **Clean Architecture Hierarchy**:
   - `Lexora.Domain` CANNOT reference any other project, ORM, or UI framework. It must remain pure .NET.
   - `Lexora.Application` references ONLY `Lexora.Domain`.
   - `Lexora.Infrastructure` references `Lexora.Domain` and `Lexora.Application`.
   - `Lexora.Presentation` references `Lexora.Application`, `Lexora.Infrastructure`, and `Lexora.Domain`.
   - **Critical Rule**: The Presentation layer (ViewModels and Views) MUST NEVER directly execute EF Core queries or reference `LexoraDbContext`. All queries, mutations, and business logic MUST pass through Application services or repository interfaces.

2. **Database & Schema Rules**:
   - The database is SQLite, located by default at `%LocalAppData%/Lexora/data/lexora.db`.
   - Entity configurations reside exclusively in `LexoraDbContext.OnModelCreating`.
   - Do NOT introduce breaking database schema changes without evaluating backward compatibility and migration impacts.
   - Database updates must be executed in atomic single-commit transactions (`await _unitOfWork.SaveChangesAsync()`). Do NOT call intermediate saves that risk partial state corruptions.

---

## 2. Coding Standards & Conventions

- **Target Framework**: .NET 10 (`net10.0` for class libraries, `net10.0-windows10.0.26100.0` for WinUI 3).
- **C# Language Features**: C# 13/14 modern syntax:
  - Use primary constructors where appropriate.
  - Use file-scoped namespaces (`namespace Lexora.Domain;`).
  - Enable Nullable Reference Types (`#nullable enable`) across all projects.
  - Use async/await with `CancellationToken` support in all long-running service and repository methods.
- **MVVM Conventions**:
  - ViewModels inherit from `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.
  - Use `[ObservableProperty] public partial ...` on properties (C# 13 partial properties) to ensure full WinRT / CsWinRT AOT compatibility and avoid compiler warnings.
  - Use `[RelayCommand]` on methods for command binding.

---

## 3. WinUI 3 & Fluent UI/UX Guidelines

- **Zero Hardcoded Colors**:
  - NEVER use hardcoded hex colors (e.g. `#FFF4CE`, `#E6F2FB`, `#323130`) for backgrounds or text.
  - ALWAYS use Fluent `{ThemeResource ...}` brushes (e.g. `LayerFillColorDefaultBrush`, `CardBackgroundFillColorDefaultBrush`, `SystemFillColorCautionBrush`, `AccentTextFillColorPrimaryBrush`) to ensure 100% visual fidelity across Light, Dark, and High-Contrast themes.
- **Responsive Layouts**:
  - NEVER use high fixed widths (e.g. `Width="680"`) on dialogs or cards that cause clipping on low-resolution or non-maximized screens. Use `MaxWidth` with `HorizontalAlignment="Stretch"`.
  - Use `UniformGridLayout` on `ItemsRepeater` for multi-card displays rather than fixed-width horizontal scroll lists.
- **Dialog Architecture & Safety**:
  - Set `dialog.XamlRoot = this.XamlRoot;` before showing any `ContentDialog`.
  - Place validation error feedback at the TOP of the dialog using an `InfoBar` so it is immediately visible without scrolling.
  - When chaining sequential dialogs, ALWAYS insert `await Task.Yield();` before opening the next dialog to prevent WinUI 3 `An async operation was already in progress` visual tree collision exceptions.

---

## 4. Security & Data Protection Invariants

- **Path Traversal Defense**: All file, audio, and backup service paths MUST be sanitized with strict alphanumeric whitelists and verified via `Path.GetFullPath(target).StartsWith(allowedBaseDir)`.
- **Database Backup & Restore Integrity**:
  - SQLite backups must be verified against the 16-byte magic header (`SQLite format 3\0`) prior to restoration.
  - Always execute `SqliteConnection.ClearAllPools()` and delete companion `-wal` and `-shm` lockfiles before replacing an active database file.
- **Payload Limits**: All import parsers (CSV and JSON) must enforce input length boundaries (default 20MB ceiling) and de-duplicate records before batch persistence.

---

## 5. Spaced Repetition (SM-2) Integrity

- Spaced repetition calculations must be routed through `ISpacedRepetitionEngine` (`Sm2SpacedRepetitionEngine`).
- `EaseFactor` must never drop below 1.3 (`EF >= 1.3`).
- Every review submission must log a `StudyLog` entry and invoke `IReviewRepository.UpdateStreakAsync`.
- Review card ratings in UI must be protected by concurrency state guards to prevent double-click re-entrancy.

---

## 6. Build, Verification & Packaging Protocol

Before completing any task or sub-phase, agents MUST run and verify:

```powershell
# 1. Build entire solution (Must complete with 0 errors and 0 warnings)
dotnet build Lexora.slnx

# 2. Run all unit and integration tests (All 64 tests MUST pass)
dotnet test Lexora.slnx

# 3. Packaging verification (When modifying installer or release configs)
powershell -ExecutionPolicy Bypass -File ./build-installer.ps1 -Version "1.0.0"
```

All 64 tests (40 Unit + 24 Integration) MUST pass with **0 errors and 0 warnings**.

---

## 7. Modular Rules Reference

For in-depth explanations and checklists by topic, see:
- [Rule 01: Clean Architecture & Boundaries](rules/01-clean-architecture.md)
- [Rule 02: C# 13 & .NET 10 Coding Standards](rules/02-csharp-dotnet-standards.md)
- [Rule 03: WinUI 3 & Fluent UI/UX Guidelines](rules/03-winui3-fluent-design.md)
- [Rule 04: Security & Data Protection Invariants](rules/04-security-and-data-protection.md)
- [Rule 05: Spaced Repetition (SM-2) Integrity](rules/05-spaced-repetition-sm2.md)
- [Rule 06: Testing & Quality Gate Protocols](rules/06-testing-and-verification.md)

