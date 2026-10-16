# 📓 Changelog
All notable changes to the Lexora desktop application will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]
### Added
- Enterprise-grade AI Agent Governance runtime: `.agents/skills/lexora-quality-audit/` for automated verification.
- GitHub Actions CI/CD workflows for quality gate enforcement and SemVer validation.

---

## [1.1.0] - 2026-10-16
### Added
- **FSRS v5 Spaced Repetition Engine**: Introduced `IFsrsSpacedRepetitionEngine` implementing the Free Spaced Repetition Scheduler v5 algorithm with Stability ($S$), Difficulty ($D$), and Retrievability ($R$) metrics.
- **Flashcard 3D Spatial Physics**: Implemented two-stage 300ms 3D flip animation using WinUI 3 `PlaneProjection` with smooth 90° content transition.
- **Adaptive Review Viewport**: Implemented dynamic single/dual-column layout switching in `ReviewPage` to prevent layout clipping on non-maximized viewports (< 880px).
- **Test Session Cache Fallback**: Hardened `TestService.SubmitTestAsync` to dynamically reconstruct evaluation questions from the database if memory cache expires.

### Changed
- **Diacritic Search RAM Optimization**: Refactored `SearchDiacriticFallbackAsync` and `CountDiacriticFallbackAsync` in `VocabularyRepository.cs` from loading full entities into memory to lightweight server-side projection, cutting RAM footprint by ~85%.
- **Bilingual Terminology Polish**: Replaced technical jargon `"SQLite WAL Active"` with localized key `"Dashboard_OfflineReady"` in both `vi-VN` and `en-US` dictionaries.

### Fixed
- **Page Memory Leaks**: Eliminated hanging anonymous lambda event subscriptions to `Loc.LanguageChanged` across all pages (`VocabularyPage`, `ReviewPage`, `GrammarPage`, `DashboardPage`, `TestPage`, `SettingsPage`, `LearningStoragePage`).
- **LearningStorageViewModel Leak**: Implemented explicit `IDisposable` pattern to detach event listeners on disposal.

---

## [1.0.0] - 2026-08-01
### Added
- **Clean Architecture Foundation**: Pure domain core (`Lexora.Domain`), orchestrating use cases (`Lexora.Application`), persistence/TTS (`Lexora.Infrastructure`), and desktop client (`Lexora.Presentation`).
- **SuperMemo-2 (SM-2) Spaced Repetition**: Mathematical engine calculating review intervals with an Ease Factor floor of 1.3.
- **SQLite Database with WAL Mode**: Embedded zero-configuration database with Write-Ahead Logging for high concurrency.
- **Two-Tier Audio Synthesis**: Local SHA-256 cached native audio playback with Windows Speech Synthesis fallback (US/UK voices).
- **Comprehensive Quality Gates**: 343 automated tests (292 unit tests + 51 integration tests) passing with 0 warnings and 0 errors.
- **Security Defenses**: Directory traversal whitelist validation on all file import/export operations and 20MB file size ceiling.
