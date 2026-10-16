# 📚 Lexora Documentation Hub

Welcome to the **Lexora Documentation Portal**. This library contains complete guides, specifications, architectural blueprints, and user manuals for the Lexora platform.

---

## 🧭 Navigation by Persona

### 🎓 For Learners & End Users
- [**User Guide & Learning Manual**](user_guide.md): Complete walkthrough of vocabulary management, spaced repetition flashcards, practice quizzes, grammar explorer, data backup/restore, and keyboard shortcuts.

### 💻 For Software Engineers & Contributors
- [**Architecture & System Design**](architecture.md): Clean Architecture principles, layer boundaries, dependency injection, and concurrency designs.
- [**Architecture Decision Records (ADRs)**](adr/): Complete history of architectural decisions, including [ADR-0001 (Clean Architecture)](adr/ADR-0001-clean-architecture-dotnet10.md), [ADR-0002 (SQLite WAL)](adr/ADR-0002-offline-first-sqlite-wal.md), and [ADR-0003 (SM-2 Spaced Repetition)](adr/ADR-0003-sm2-spaced-repetition-engine.md).
- [**Application Services & API Reference**](api_and_services.md): Detailed specifications of all service interfaces, contracts, methods, and DTOs.
- [**Database Schema & Persistence**](database_schema.md): Complete SQLite tables, ERD diagram, indexes, foreign key cascades, and PRAGMAs.
- [**Spaced Repetition (SM-2) Engine**](spaced_repetition.md): Mathematical derivations, worked examples, and learning lifecycle states.
- [**Testing & Quality Assurance Guide**](testing_and_qa.md): Taxonomy of the 64 unit & integration tests, execution guide, and quality gates.
- [**Contributing Guidelines**](contributing.md): Setup, branching model, conventional commits, and PR checklist.

### 🚀 For DevOps & Release Engineers
- [**Installer & Distribution Guide**](installer_and_distribution.md): Inno Setup 6 packaging automation, silent switches, and clean upgrade workflows.

---

## 📜 Engineering Rules & Invariants

All developers and autonomous agents must follow the repository rules located in [`../rules/`](../rules/README.md):
- [Rule 01: Clean Architecture & Boundaries](../rules/01-clean-architecture.md)
- [Rule 02: C# 13 & .NET 10 Coding Standards](../rules/02-csharp-dotnet-standards.md)
- [Rule 03: WinUI 3 & Fluent UI/UX Guidelines](../rules/03-winui3-fluent-design.md)
- [Rule 04: Security & Data Protection Invariants](../rules/04-security-and-data-protection.md)
- [Rule 05: Spaced Repetition (SM-2) Integrity](../rules/05-spaced-repetition-sm2.md)
- [Rule 06: Testing & Quality Gate Protocols](../rules/06-testing-and-verification.md)
