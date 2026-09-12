<p align="center">
  <img src="assets/brand/lexora-logo-full-light.png#gh-light-mode-only" alt="Lexora Logo" width="540" />
  <img src="assets/brand/lexora-logo-full-dark.png#gh-dark-mode-only" alt="Lexora Logo" width="540" />
</p>

<h1 align="center">Lexora — Intelligent Vocabulary &amp; Grammar Manager</h1>

<p align="center">
  <strong>High-performance, offline-first English learning & spaced repetition desktop application for Windows 11</strong><br>
  <em>Built with C# 13 / .NET 10 • WinUI 3 (Windows App SDK) • Clean Architecture • SQLite & EF Core 10 • Inno Setup 6</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010-0078D4?style=flat-square&logo=windows11&logoColor=white" alt="Platform" />
  <img src="https://img.shields.io/badge/Framework-.NET%2010.0-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/UI-WinUI%203%20Windows%20App%20SDK-0078D4?style=flat-square&logo=windows&logoColor=white" alt="WinUI 3" />
  <img src="https://img.shields.io/badge/Architecture-Clean%20Architecture-107C41?style=flat-square" alt="Clean Architecture" />
  <img src="https://img.shields.io/badge/Database-SQLite%20%2F%20EF%20Core%2010-003B57?style=flat-square&logo=sqlite&logoColor=white" alt="SQLite" />
  <img src="https://img.shields.io/badge/Algorithm-SuperMemo%20SM--2-E3008C?style=flat-square" alt="SM-2" />
  <img src="https://img.shields.io/badge/Tests-55%2F55%20Passed-2EA44F?style=flat-square" alt="Tests" />
  <img src="https://img.shields.io/badge/Installer-Inno%20Setup%206-FF8C00?style=flat-square" alt="Inno Setup" />
</p>

---

## 📖 Table of Contents

- [Overview & Key Features](#-overview--key-features)
- [Clean Architecture & System Design](#-clean-architecture--system-design)
- [Spaced Repetition Algorithm (SM-2)](#-spaced-repetition-algorithm-sm-2)
- [Keyboard Shortcuts](#-keyboard-shortcuts)
- [Quick Start Guide](#-quick-start-guide)
  - [Prerequisites](#prerequisites)
  - [Building & Testing](#building--testing)
  - [Running the App](#running-the-app)
- [Packaging & Installer Distribution](#-packaging--installer-distribution)
- [Project Layout](#-project-layout)
- [Documentation & References](#-documentation--references)
- [License & Contributions](#-license--contributions)

---

## ✨ Overview & Key Features

Lexora is an enterprise-grade, offline-first personal English learning platform engineered specifically for Windows desktop environments. Combining cognitive science (Ebbinghaus forgetting curve & SuperMemo SM-2) with Windows 11 Fluent Design, Lexora empowers learners to master English vocabulary and grammar with measurable long-term retention.

### 🌟 Core Capabilities

| Feature | Description |
| :--- | :--- |
| 🧠 **SM-2 Spaced Repetition** | Adaptive algorithm calculating review intervals ($I(n)$), ease factors ($EF \ge 1.3$), and scheduling based on active recall response ratings (*Again*, *Hard*, *Good*, *Easy*). |
| 📚 **Rich Vocabulary Management** | Comprehensive word entries with CEFR levels (A1–C2), POS categorization, UK/US IPA phonetics, multiple nuances, context examples, and exam taggings (TOEIC, VSTEP, IELTS). |
| 🔊 **Native Pronunciation Audio** | Offline-cached English audio with automatic local storage fallback, thread-safe background playback, and input path traversal sanitization. |
| 📝 **Interactive Practice & Quizzes** | 4 customizable quiz modes: *Word-to-Meaning*, *Meaning-to-Word*, *Fill-in-the-Blank*, and *Multiple Choice* with immediate scoring and review suggestions. |
| 📐 **Grammar Mastery Engine** | Structured grammar catalog with formula syntax highlighting, comprehensive Vietnamese & English explanations, and prominent *Common Mistakes* guidance. |
| 📊 **Analytics & Heatmap** | GitHub-style daily learning activity heatmap, day streak counters, CEFR level distribution charts, and exam certification readiness tracking. |
| 🔄 **Robust Import & Export** | High-throughput RFC-4180 multiline CSV and JSON data exchange, payload validation (up to 20MB), deduplication, and single-click database backup/restore. |
| 🎨 **Windows 11 Fluent Design** | Modern Mica backdrop, adaptive Light/Dark theme switching, responsive layouts, 250ms debounced search, and zero UI thread stalls. |

---

## 🏛️ Clean Architecture & System Design

Lexora enforces strict architectural boundaries following **Clean Architecture** and **Domain-Driven Design (DDD)**:

```text
┌────────────────────────────────────────────────────────────────────────┐
│                        Lexora.Presentation                             │
│       (WinUI 3 Desktop App • MVVM • Mica Backdrop • Dialogs)           │
└───────────────────┬──────────────────────────────┬─────────────────────┘
                    │                              │
                    ▼                              │
┌──────────────────────────────────────┐           │
│          Lexora.Application          │           │
│   (Use Cases • SM-2 • DTOs • Ports)  │           │
└───────────────────┬──────────────────┘           │
                    │                              │
                    ▼                              ▼
┌──────────────────────────────────────┐  ┌──────────────────────────────┐
│            Lexora.Domain             │◄─┤    Lexora.Infrastructure     │
│ (Entities • Value Objects • Enums)   │  │ (EF Core 10 • SQLite • Repos)│
└──────────────────────────────────────┘  └──────────────────────────────┘
```

### Layer Responsibility Matrix

1. **`Lexora.Domain`** (`net10.0`):
   - Pure domain models: `Word`, `WordMeaning`, `ExampleSentence`, `Topic`, `ExamTag`, `ReviewCard`, `StudySession`, `StudyLog`, `GrammarRule`, `GrammarExample`, `UserStreak`.
   - Domain enums: `PartOfSpeech`, `CefrLevel`, `ExamType`, `LearningStatus`, `ReviewRating`, `TestType`.
   - Zero third-party ORM or UI dependencies.

2. **`Lexora.Application`** (`net10.0`):
   - SuperMemo SM-2 engine (`Sm2SpacedRepetitionEngine`) calculating next review schedules and streak progressions.
   - Application contracts & implementations: `VocabularyService`, `ReviewService`, `StatisticsService`, `GrammarService`, `TestService`, `ExportImportService`.
   - Thread-safe, atomic single-commit business transactions.

3. **`Lexora.Infrastructure`** (`net10.0`):
   - EF Core 10 with SQLite in WAL mode (`LexoraDbContext`).
   - Repository pattern & Unit of Work implementation with entity tracking conflict resolution.
   - Safe audio caching (`LocalAudioStorageService`) with strict path traversal defenses.
   - Database backup/restore engine (`DatabaseBackupService`) with 16-byte SQLite header validation and connection pool flushing.

4. **`Lexora.Presentation`** (`net10.0-windows10.0.26100.0`):
   - Windows App SDK / WinUI 3 desktop application.
   - MVVM powered by `CommunityToolkit.Mvvm` (C# 13 partial properties, command bindings).
   - Responsive layouts (`UniformGridLayout`, adaptive dialogs with top-docked `InfoBar` validation).
   - Dynamic theme switching (`ThemeService`) and runtime bilingual localization (`vi-VN` & `en-US`).

---

## 🧮 Spaced Repetition Algorithm (SM-2)

Lexora utilizes a mathematically verified implementation of the **SuperMemo SM-2** spaced repetition model:

$$\text{EF}' = \max\left(1.3,\; \text{EF} + \left(0.1 - (5 - q) \cdot (0.08 + (5 - q) \cdot 0.02)\right)\right)$$

$$\text{Interval}(n) = \begin{cases} 1 \text{ day}, & n = 1 \\ 6 \text{ days}, & n = 2 \\ \text{Round}(\text{Interval}(n - 1) \cdot \text{EF}'), & n > 2 \end{cases}$$

- **Ratings**: $q \in \{1, 2, 4, 5\}$ mapped to *Again (1)*, *Hard (2)*, *Good (4)*, *Easy (5)*.
- **Ease Factor Floor**: Guaranteed $\text{EF} \ge 1.3$ to prevent cards from falling into inescapable review traps.
- **Streak Preservation**: Daily active reviews automatically log `StudyLog` entries and update consecutive learning streaks.

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action | Scope |
| :--- | :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>1</kbd> | Navigate to **Dashboard** | Global |
| <kbd>Ctrl</kbd> + <kbd>2</kbd> | Navigate to **Vocabulary** | Global |
| <kbd>Ctrl</kbd> + <kbd>3</kbd> | Navigate to **Spaced Review** | Global |
| <kbd>Ctrl</kbd> + <kbd>4</kbd> | Navigate to **Practice & Test** | Global |
| <kbd>Ctrl</kbd> + <kbd>5</kbd> | Navigate to **Grammar** | Global |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Focus Global Search Box | Global |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | Add New Word / Grammar Rule | Vocabulary & Grammar Pages |
| <kbd>Ctrl</kbd> + <kbd>E</kbd> | Edit Selected Word / Rule | Vocabulary & Grammar Pages |
| <kbd>Delete</kbd> | Delete Selected Entry (with confirmation) | Vocabulary & Grammar Pages |
| <kbd>Ctrl</kbd> + <kbd>P</kbd> | Start Grammar Practice Mode | Grammar Page |
| <kbd>Space</kbd> | Flip Flashcard to reveal Answer | Review Session |
| <kbd>1</kbd> / <kbd>2</kbd> / <kbd>3</kbd> / <kbd>4</kbd> | Rate Card (*Again*, *Hard*, *Good*, *Easy*) | Review Session |

---

## 🚀 Quick Start Guide

### Prerequisites

- **OS**: Windows 10 (Version 1809+, Build 17763) or Windows 11 (Recommended).
- **SDK**: [.NET 10 SDK (v10.0+)](https://dotnet.microsoft.com/download/dotnet/10.0).
- **Tooling**: Visual Studio 2026 / VS Code with C# Dev Kit.
- **Installer Tooling** *(Optional)*: [Inno Setup 6](https://jrsoftware.org/isdl.php).

### Building & Testing

```powershell
# 1. Clone repository
git clone https://github.com/Nguyen-Trung-Tien/Lexora.git
cd Lexora

# 2. Build entire solution
dotnet build Lexora.slnx

# 3. Run all unit and integration tests (55 tests)
dotnet test Lexora.slnx
```

### Running the App

```powershell
# Launch WinUI 3 desktop application
dotnet run --project src/Lexora.Presentation/Lexora.Presentation.csproj
```

---

## 📦 Packaging & Installer Distribution

Lexora provides an automated, one-click build and packaging pipeline using **Inno Setup 6** for generating a standalone, self-contained Windows installer:

```powershell
# Run packaging pipeline (Runs tests -> Publishes Self-contained x64 -> Compiles Setup.exe)
powershell -ExecutionPolicy Bypass -File ./build-installer.ps1 -Version "1.0.0"
```

### Installer Characteristics
- **Output File**: `publish/installer/Lexora-Setup-v1.0.0.exe` (~73 MB).
- **Self-Contained Deployment**: Includes .NET 10 runtime and Windows App SDK binaries; runs immediately without requiring runtime prerequisites on end-user machines.
- **User Data Retention**: Upgrading or uninstalling prompts users to preserve their study progress and SQLite database (`%LocalAppData%\Lexora\data\lexora.db`).
- **Dual-Mode Privileges**: Supports standard non-admin per-user installs as well as system-wide administrator installations.

---

## 📂 Project Layout

```text
Lexora/
├── .agents/                        # AI agent & pair programming configurations
├── assets/                         # Vector & high-res brand identity assets
│   └── brand/                      # App icons, SVG logos, guidelines
├── docs/                           # Detailed technical documentation
│   ├── architecture.md             # Clean Architecture & Layer boundaries
│   ├── database_schema.md          # SQLite schema, indexing & cascades
│   ├── spaced_repetition.md        # SM-2 mathematical model & proof
│   └── installer_and_distribution.md # Inno Setup packaging specification
├── installer/                      # Inno Setup compiler scripts
│   └── LexoraSetup.iss             # Inno Setup 6 configuration script
├── publish/                        # Build artifacts and installer outputs
│   ├── installer/                  # Generated Lexora-Setup-v*.exe
│   └── win-x64/                    # Published self-contained binaries
├── src/
│   ├── Lexora.Domain/              # Pure business entities, enums, repository contracts
│   ├── Lexora.Application/         # SM-2 engine, DTOs, service implementations
│   ├── Lexora.Infrastructure/      # EF Core 10, SQLite, caching, backup engines
│   └── Lexora.Presentation/        # WinUI 3 Fluent UI, MVVM ViewModels, Pages, Dialogs
├── tests/
│   ├── Lexora.Tests.Unit/          # SM-2 math, business rules, resilience unit tests
│   └── Lexora.Tests.Integration/   # EF Core SQLite in-memory integration tests
├── AGENTS.md                       # Developer & AI Agent development guidelines
├── PROGRESS.md                     # Comprehensive phase completion tracking
├── build-installer.ps1             # Automated packaging & installer script
├── build-release.ps1               # Automated release publish script
└── Lexora.slnx                     # Solution file
```

---

## 📚 Documentation & References

- [Architecture & Layer Guidelines](docs/architecture.md)
- [Database Schema & Migration Standards](docs/database_schema.md)
- [Spaced Repetition & SM-2 Specifications](docs/spaced_repetition.md)
- [Installer & Distribution Guide](docs/installer_and_distribution.md)
- [Developer & AI Agent Rules](AGENTS.md)
- [Feature Progress & Verification History](PROGRESS.md)

---

## 📄 License & Contributions

Developed by **Lexora Team**. Distributed under the MIT License. Contributions and issues are welcome via GitHub Pull Requests.
