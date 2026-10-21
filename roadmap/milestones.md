# 🏁 Lexora Engineering Milestones

> This document defines formal engineering milestones, target release dates, deliverables, and mandatory exit criteria for the Lexora project.

---

## Milestone M1: Core Foundation & SM-2 Engine (v1.0.0) — COMPLETED ✅
- **Status**: Completed (Release 1.0.0)
- **Primary Deliverables**:
  - Clean Architecture foundation: `Domain`, `Application`, `Infrastructure`, `Presentation`.
  - Offline-first SQLite database with WAL mode and atomic single-commit transactions.
  - SuperMemo-2 (SM-2) Spaced Repetition engine with mathematical ease factor floor ($EF \ge 1.3$).
  - Full dual-language localization (`vi-VN`, `en-US`) with instant runtime switching.
  - Complete Inno Setup installer packaging automation (`build-installer.ps1`).
- **Exit Criteria**:
  - 343 / 343 unit & integration tests passing.
  - 0 compiler warnings and 0 errors on .NET 10.

---

## Milestone M2: UX Resilience, FSRS Engine & Learning Intelligence Suite (v1.1.0 - v1.2.2) — COMPLETED ✅
- **Status**: Completed (Releases 1.1.0, 1.2.1, 1.2.2)
- **Primary Deliverables**:
  - Free Spaced Repetition Scheduler (FSRS v5) integration alongside SM-2.
  - 3D Flashcard flip physics using WinUI 3 `PlaneProjection` with smooth inflection transitions.
  - Elimination of WinUI 3 event handler memory leaks via `ILocalizableView` and `IThemeAwareView`.
  - Diacritic search RAM optimization (server-side projections) and `.AsSplitQuery()` query tuning.
  - Learning Intelligence Suite: Ebbinghaus Retrievability modeling, at-risk vocabulary detector, and circadian metrics.
  - Smart Review Presets: High-Yield, Forgetting Risk ($R < 70\%$), Weak CEFR, and Exam Cram.
  - Computerized Adaptive Testing (CAT) Adaptive Quiz engine with session recovery.
  - Vocabulary Knowledge Graph with dynamic node pivoting and UK/US audio playback.
  - Pronunciation & Listening Lab (Audio Dictation, Minimal Pairs phoneme discrimination, UK vs US Accents).
  - Learning Storage Vault (`LearningStoragePage`) for targeted vocabulary and grammar retention.
  - Data Health Center for SQLite diagnostics, integrity checks, and cache maintenance.
  - Production AI Agent operating system: 8 modular skills, personas, rules, and GitHub Actions CI workflows.
  - Responsive layout adaptation across all pages and dialogs for compact viewports (< 960px).
  - Software Update System with GitHub Releases API, pre-update snapshots, and silent installer handoff.
- **Exit Criteria**:
  - 390 / 390 automated unit & integration tests passing (0 failed, 0 skipped).
  - Solution build verified with 0 warnings and 0 errors on .NET 10.
  - 100% bilingual parity across 1,223 keys (`vi-VN` and `en-US`).
  - Zero hardcoded colors invariant validated across all 85 XAML and presentation files.

---

## Milestone M2.5: User Guide, System Onboarding & Quality Maturation (v1.3.0) — COMPLETED ✅
- **Status**: Completed (Release 1.3.0)
- **Primary Deliverables**:
  - Centralized User Guide & Onboarding Hub (`HelpPage`, `HelpViewModel`) in Shell Footer Navigation.
  - 6 interactive structured guide modules with real-time search, category pills, and shortcut cheat sheet.
  - Interactive mathematical algorithm specifications for SM-2 and FSRS spaced repetition engines.
  - Contextual help buttons and interactive Fluent 2 `TeachingTip` popups across 6 core screens with 1-click jumps to guides.
  - Global navigation shortcuts (`F1` and `Ctrl+H`) for instant help access.
  - 100% bilingual parity across `vi-VN` and `en-US` dictionaries in `LocalizationService`.
  - Comprehensive memory leak prevention with unhooked event listeners on `Unloaded`.
- **Exit Criteria**:
  - 411 / 411 automated unit & integration tests passing (360 Unit + 51 Integration, 0 failed, 0 skipped).
  - Solution build verified with 0 warnings and 0 errors on .NET 10.
  - 100% Fluent 2 `{ThemeResource ...}` compliance.

---

## Milestone M3: Offline AI Inference & Encrypted Mesh Sync (v2.0.0) — ACTIVE PLANNING 🔮
- **Status**: Active Planning / Next Milestone
- **Primary Deliverables**:
  - On-device Small Language Model (SLM) integration via ONNX Runtime (Gemma-2B / Phi-3 Mini) for example sentence generation.
  - End-to-end encrypted synchronization over WebDAV / P2P.
  - Offline IPA pronunciation evaluation via Whisper.cpp.
  - Multi-profile learner vaults with biometric / PIN authentication.
  - SQLCipher 256-bit AES database encryption at rest.
- **Exit Criteria**:
  - Fully offline operation with zero telemetry or unexpected network requests.
  - Backward compatibility for unencrypted v1.x databases.
