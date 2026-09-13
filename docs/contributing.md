# 🤝 Contributing to Lexora

Thank you for your interest in contributing to **Lexora**! We welcome code contributions, bug reports, documentation enhancements, and feature suggestions.

---

## 📑 Code of Conduct & Standards

All contributors must respect our engineering principles and maintain respectful, constructive collaboration.

Before writing code, please review our core architectural rules:
- [Clean Architecture & Boundaries](../rules/01-clean-architecture.md)
- [C# 13 & .NET 10 Standards](../rules/02-csharp-dotnet-standards.md)
- [WinUI 3 & Fluent UI Guidelines](../rules/03-winui3-fluent-design.md)
- [Security & Data Protection](../rules/04-security-and-data-protection.md)
- [Spaced Repetition SM-2 Integrity](../rules/05-spaced-repetition-sm2.md)
- [Testing & Quality Gates](../rules/06-testing-and-verification.md)

---

## 🛠️ Development Environment Setup

### Required Tools:
1. **OS**: Windows 10 (Version 1809+, Build 17763) or Windows 11 (Recommended).
2. **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (v10.0 or higher).
3. **IDE**:
   - Visual Studio 2026 / Visual Studio 2022 (v17.10+) with the **.NET Desktop Development** and **Windows App SDK** workloads installed.
   - Or VS Code with the **C# Dev Kit** extension.
4. **Packaging Tool** *(Optional, for installer generation)*:
   - [Inno Setup 6](https://jrsoftware.org/isdl.php).

### Fork & Clone:
```powershell
# Clone your fork
git clone https://github.com/<your-username>/Lexora.git
cd Lexora

# Restore dependencies & build
dotnet build Lexora.slnx

# Run tests
dotnet test Lexora.slnx
```

---

## 🌿 Git Workflow & Branching Strategy

1. Create a descriptive feature branch from `main`:
   ```powershell
   git checkout -b feature/topic-filter-enhancement
   # or
   git checkout -b fix/card-rating-race-condition
   ```
2. Commit messages should follow the **Conventional Commits** specification:
   - `feat: add audio speed adjustment in review session`
   - `fix: prevent rating button double-click re-entrancy`
   - `docs: update spaced repetition interval examples`
   - `test: add edge-case unit tests for SM-2 ease factor floor`
   - `refactor: extract reusable quiz option selector control`

---

## 📋 Pull Request Checklist

Before submitting a Pull Request, ensure that:

- [ ] `dotnet build Lexora.slnx` builds with **0 errors and 0 warnings**.
- [ ] `dotnet test Lexora.slnx` passes with **all 64 tests green**.
- [ ] Any new public methods include XML documentation comments.
- [ ] Any new features or bug fixes have corresponding unit or integration tests.
- [ ] No hardcoded hex color brushes are introduced in XAML.
- [ ] Your branch is rebased on top of the latest `main` branch.
