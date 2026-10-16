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

## Milestone M2: UX Resilience, FSRS Engine & Memory Optimization (v1.1.0) — ACTIVE 🚀
- **Status**: Active / In Progress
- **Primary Deliverables**:
  - Free Spaced Repetition Scheduler (FSRS v5) integration alongside SM-2.
  - 3D Flashcard flip physics using WinUI 3 `PlaneProjection` with smooth inflection transitions.
  - Elimination of WinUI 3 event handler memory leaks and diacritic RAM reduction.
  - Production AI Agent operating system: modular rules, personas, skills, and CI workflows.
  - Responsive layout adaptation for viewports < 880px to eliminate horizontal clipping.
- **Exit Criteria**:
  - All automated tests green.
  - Build verified with zero warnings.
  - Changelog fragment enforcer passing in GitHub Actions.

---

## Milestone M3: Offline AI Inference & Encrypted Mesh Sync (v2.0.0) — PLANNED 🔮
- **Status**: Future Roadmap
- **Primary Deliverables**:
  - On-device Small Language Model (SLM) integration via ONNX Runtime (Gemma-2B / Phi-3 Mini) for example sentence generation.
  - End-to-end encrypted synchronization over WebDAV / P2P.
  - Offline IPA pronunciation evaluation via Whisper.cpp.
  - Multi-profile learner vaults with biometric / PIN authentication.
- **Exit Criteria**:
  - Fully offline operation with zero telemetry or unexpected network requests.
  - Database encryption via SQLCipher.
