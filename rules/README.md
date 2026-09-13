# 📜 Lexora Engineering Rules & Standards Directory

Welcome to the **Lexora Rules Hub**. This directory codifies all technical invariants, architectural boundaries, coding conventions, WinUI 3 UX guidelines, security protections, and testing protocols governing the Lexora repository.

Every developer and AI agent working on this codebase **MUST** adhere to these rules without exception.

---

## 📑 Rule Modules Index

| Rule Identifier | Domain | Scope & Primary Objectives | Target Layer |
| :--- | :--- | :--- | :--- |
| [**01-clean-architecture.md**](01-clean-architecture.md) | **Architecture & Hierarchy** | Enforces unidirectional layer dependencies, DTO encapsulation, and strict prohibition of direct EF Core queries in the Presentation layer. | All Layers |
| [**02-csharp-dotnet-standards.md**](02-csharp-dotnet-standards.md) | **C# 13 & .NET 10 Standards** | Modern language idioms, primary constructors, file-scoped namespaces, Nullable Reference Types, async/await with `CancellationToken`. | All Layers |
| [**03-winui3-fluent-design.md**](03-winui3-fluent-design.md) | **WinUI 3 & Fluent UX** | Zero hardcoded hex colors (`ThemeResource` brushes only), dynamic Mica backdrop, responsive `UniformGridLayout`, dialog safety (`XamlRoot`, top `InfoBar`). | `Lexora.Presentation` |
| [**04-security-and-data-protection.md**](04-security-and-data-protection.md) | **Security & Persistence** | Path traversal sanitization, SQLite WAL mode, 16-byte magic header database restore validation, connection pool flushing, 20MB payload boundary. | `Lexora.Infrastructure`, `Lexora.Application` |
| [**05-spaced-repetition-sm2.md**](05-spaced-repetition-sm2.md) | **SM-2 Algorithm Integrity** | Mathematical SuperMemo SM-2 rules, Ease Factor floor ($EF \ge 1.3$), lapse handling, UI concurrency state guards on rating buttons. | `Lexora.Application`, `Lexora.Presentation` |
| [**06-testing-and-verification.md**](06-testing-and-verification.md) | **Testing & Quality Gates** | Mandatory 0 error & 0 warning build policy, 64/64 tests passing, unit test vs. SQLite in-memory integration test separation. | `tests/*` |

---

## 🏛️ Rule Precedence

1. **System Invariants & Security** (`rules/04-...` & `rules/05-...`) supersede visual or convenience preferences.
2. **Architectural Boundaries** (`rules/01-...`) must never be compromised for fast prototyping.
3. **Build Quality Gates** (`rules/06-...`) are blocking: code that produces warnings or failing tests shall not be merged.
4. For AI agents, see also the project root [AGENTS.md](../AGENTS.md) which mirrors these requirements into autonomous workflows.
