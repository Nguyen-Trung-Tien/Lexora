# 🚀 Lexora Development & Optimization Progress

## 📊 Quality Gates Benchmark
- **Application Version**: `v1.6.1` (Production Audit, Adaptive CAT Quiz Bugfixes & Dynamic Action Buttons)
- **Build Status**: `0 Warning(s)`, `0 Error(s)` (`dotnet build Lexora.slnx -v q /clp:ErrorsOnly`)
- **Automated Tests**: **455 / 455 Tests Passing** (396 Unit Tests + 59 Integration Tests, 0 Failed, 0 Skipped).
- **Target Frameworks**: `.NET 10` (`net10.0` core libraries & tests, `net10.0-windows10.0.26100.0` WinUI 3 presentation).
- **Fluent UI Audit**: `0 Violations` (`audit_fluent_ui.ps1` scanned 102 files).
- **Localization (i18n)**: `100% Parity` across all keys (`sync_localization.ps1 -CheckXaml`).
- **Clean Architecture**: `0 Violations` (`lint_architecture.ps1`).

---

## 🛠️ Comprehensive Optimization & Upgrade Changelog

### 1. Latent Bug Fixes & Critical Resilience
- **Page Memory Leak Elimination**:
  - Removed dangling anonymous lambda event subscriptions (`Loc.LanguageChanged += (s, e) => OnLanguageChanged();`) across `VocabularyPage`, `ReviewPage`, `GrammarPage`, `DashboardPage`, `TestPage`, `SettingsPage`, and `LearningStoragePage`.
  - Leveraged centralized navigation lifecycle in `ShellPage.xaml.cs` via `ILocalizableView.OnLanguageChanged()`.
  - Added deterministic `IDisposable` pattern in `LearningStorageViewModel.cs` with explicit event unsubscription.
- **Diacritic Fallback RAM & OOM Optimization**:
  - Optimized `SearchDiacriticFallbackAsync` and `CountDiacriticFallbackAsync` in `VocabularyRepository.cs`.
  - Eliminated full-entity loading of the entire dictionary with 5-level deep `.Include()`s into RAM. Replaced with lightweight server-side projection (`Select(x => new { x.Id, x.Term, x.Phonetic, ... })`), in-memory diacritic matching, pagination, and targeted hydration via `GetByIdsWithDetailsAsync`.
- **Test Session Resilience & Evaluation Fallback**:
  - Hardened `TestService.SubmitTestAsync` against cache eviction. If an active test session expires from the in-memory cache, questions are now dynamically re-evaluated against persisted vocabulary entities in the database rather than throwing an exception.

### 2. UI/UX Renovation & Fluent 2 Polish
- **Developer Jargon Removal & Bilingual Localization**:
  - Replaced hardcoded technical text `"SQLite WAL Active"` with localized key `"Dashboard_OfflineReady"` (`{x:Bind Loc.GetString('Dashboard_OfflineReady')}`) in `DashboardPage.xaml`.
  - Added `"Dashboard_OfflineReady"` to both Vietnamese (`vi-VN`: "Sẵn sàng ngoại tuyến") and English (`en-US`: "Offline Ready") dictionaries in `LocalizationService.cs`.
- **Flashcard 3D Flip Animation & Spatial Physics**:
  - Added `PlaneProjection` (`FlashcardProjection`) with a 300ms two-stage easing animation (`RotationY` 0° -> 90° -> 180° / 0°) in `ReviewPage.xaml` and `ReviewPage.xaml.cs`.
  - Front and back flashcard content smoothly switches at the 90° inflection point without jarring visual jumps.
- **Adaptive Responsive Layout in Review Session**:
  - Added `UpdateResponsiveLayout` in `ReviewPage.xaml.cs` based on window width.
  - Dynamically transitions the sidebar from a side-by-side two-column grid (`ColumnDefinitions="*, 360"`) on widescreen viewports to a stacked single-column layout on compact or non-maximized screens (< 880px), preventing text clipping and button collisions.

### 3. Spaced Repetition Engine: FSRS v5 Integration
- **Free Spaced Repetition Scheduler (FSRS v5)**:
  - Designed and implemented `IFsrsSpacedRepetitionEngine` and `FsrsSpacedRepetitionEngine` in `Lexora.Application.SpacedRepetition`.
  - Accurately models human memory via three core parameters: **Stability ($S$)**, **Difficulty ($D$)**, and **Retrievability ($R$)**.
  - Configurable target retention probability (default $0.90$) and mathematically grounded forgetting curves.
  - Provided full backward compatibility mapping to SM-2 `EaseFactor` ($\ge 1.3$) and registered as a Singleton in `DependencyInjection.cs`.
  - Added 7 comprehensive unit tests in `FsrsSpacedRepetitionEngineTests.cs`.

### 4. Learning Intelligence Suite & Self-Healing Data Center
- **Learning Intelligence Dashboard & Cognitive Memory Analytics**:
  - Implemented `GetLearningIntelligenceAsync` in `StatisticsService.cs` calculating Ebbinghaus Retrievability curves ($R = e^{-t/S}$), cognitive load forecast for 7 days ahead, hourly circadian learning retention rates, and identification of critical at-risk vocabulary.
  - Added cognitive memory cards, circadian retention metrics, and at-risk vocabulary table to `DashboardPage.xaml` and `DashboardViewModel.cs`.
- **Smart Review Presets**:
  - Implemented `GetSmartPresetsAsync` and `GetCardsByPresetAsync` in `ReviewService.cs` supporting high-yield cards, cards with $R < 70\%$, weak CEFR tiers, and cram sessions.
  - Integrated preset selector into `ReviewPage.xaml` with dynamic card memory stability and retrievability indicators.
- **Adaptive Quiz Engine (Computerized Adaptive Testing - CAT)**:
  - Implemented `IAdaptiveQuizService` and `AdaptiveQuizService.cs` recalibrating question CEFR tiers in real-time based on cumulative score, generating POS-matched distractors, and evaluating estimated user CEFR level.
  - Integrated CAT mode selector and adaptive testing workflow in `TestPage.xaml` and `TestViewModel.cs`.
- **Study Planner & Daily Pacing Manager**:
  - Implemented `IStudyPlannerService` and `StudyPlannerService.cs` managing daily curriculum checklists (new words, reviews, quizzes, grammar rules), streak preservation, weekly targets, and offline JSON persistence (`%LocalAppData%/Lexora/data/study_plan.json`).
  - Created `StudyPlannerDialog.xaml` accessible from Shell navigation and Dashboard.
- **Vocabulary Knowledge Graph**:
  - Implemented `IVocabularyKnowledgeGraphService` and `VocabularyKnowledgeGraphService.cs` extracting semantic connections across synonyms, antonyms, topics, and CEFR tiers.
  - Created interactive `KnowledgeGraphDialog.xaml` with dynamic node pivoting, preview card, and native UK/US audio pronunciation.
- **Data Health Center & Self-Healing Maintenance**:
  - Implemented `IDataHealthService` and `DataHealthService.cs` providing SQLite PRAGMA integrity verification, orphan entity cleanup, WAL checkpointing, database vacuuming, and audio cache management.
  - Created `DataHealthCenterDialog.xaml` accessible from Settings and Shell quick actions.
- **Quality Gates Benchmark**:
  - Added 22 new unit tests across all 6 services (`LearningIntelligenceServiceTests.cs`, `AdaptiveQuizServiceTests.cs`, `StudyPlannerServiceTests.cs`, `KnowledgeGraphServiceTests.cs`, `DataHealthServiceTests.cs`).
  - Total test count expanded to **365 / 365 tests passing** with 0 warnings and 0 errors.

### 5. Đợt 1 — Codebase Audit & Hardened Stabilization
- **Fluent 2 Zero Hardcoded Hex Audit**:
  - Resolved all 13 violations detected by `audit_fluent_ui.ps1`.
  - Consolidated activity heatmap brushes (`HeatmapLevel0Brush` .. `HeatmapLevel4Brush`) into `Themes/DesignSystem.xaml` across Light, Dark, and HighContrast theme dictionaries.
  - Replaced hardcoded alpha hex strings `#00000000` with `Transparent` in `SplashScreenOverlay.xaml`.
- **Bilingual i18n Dictionary & XAML Audit**:
  - Added 32 missing localization keys in `LocalizationService.cs` across `vi-VN` and `en-US` for Learning Intelligence, CAT Adaptive Quiz, Memory Stability, and Listening Lab.
  - Achieved 100% bilingual parity and XAML reference integrity (1,173 keys, 0 discrepancies).
- **Audio Stream MIME Type Resilience**:
  - Hardened `WindowsAudioPlayerService.cs` to dynamically discern `.mp3` (`audio/mpeg`) vs `.wav` (`audio/wav`) for cached native human audio streams.

### 6. Đợt 2 — Feature Expansion: Pronunciation & Listening Lab
- **Pronunciation & Listening Lab (`ListeningLabDialog.xaml` & `PronunciationListeningLabService.cs`)**:
  - **Audio Dictation & Spelling Drills**: Implemented native audio playback with accent switching (UK/US), playback speeds (0.75x, 1.0x, 1.25x), Levenshtein distance grading, and instant context/phonetic reveal.
  - **Minimal Pairs Ear Discrimination**: Interactive training for easily confused English phonemes (/iː/ vs /ɪ/, /æ/ vs /e/, /θ/ vs /s/, /p/ vs /b/, /l/ vs /r/, /v/ vs /w/, /d/ vs /t/, /ʊ/ vs /uː/) with choice feedback and articulatory mouth-position guidance.
  - **UK vs US Accent Comparison Lab**: Side-by-side pronunciation playback with phonetic transcription and linguistic variation notes (rhoticity, tapped 't', vowel shifts).
  - **Integrated Entry Points**: Seamlessly wired into `VocabularyPage.xaml` command bar, `TestPage.xaml` title banner, and shell quick actions.
  - **Automated Regression Suite**: Added 13 new unit tests in `PronunciationListeningLabServiceTests.cs`.
### 7. Lexora v1.2.1 — Version Stabilization & Decommission of Download Center
- **Synchronized Version 1.2.1 Across All Artifacts**:
  - `Directory.Build.props`: Centralized `<Version>1.2.1</Version>`, `<AssemblyVersion>1.2.1.0</AssemblyVersion>`, `<FileVersion>1.2.1.0</FileVersion>`.
  - `Package.appxmanifest`: Windows App SDK MSIX package identity version set to `1.2.1.0`.
  - `build-installer.ps1`: Default distribution package version updated to `1.2.1`.
  - `SettingsViewModel.cs`: About section updated to version `1.2.1`.
  - User identity, database schema, and existing study records 100% preserved.
- **Decommissioned Download Center**:
  - Fully removed UI, ViewModels, and navigation entries (`NavItemDownloadCenter`, `DownloadCenterPage`, `DownloadCenterViewModel`, shortcut `Ctrl+7`).
  - Removed `CommunityDataService` and community catalog files.
  - Retained all core learning suites (Vocabulary, Grammar, Review with FSRS v5 / SM-2, Adaptive Quiz, Pronunciation & Listening Lab, Learning Intelligence, and Learning Storage).
### 8. Full-System Audit & Hardened Synchronization (v1.2.2+)
- **Bilingual Dictionary 100% C# & XAML Synchronization**:
  - Identified and added 6 missing localization keys in `LocalizationService.cs` across both `vi-VN` and `en-US` (`SmartReview_UrgentOverdue`, `SmartReview_HighForgettingRisk`, `SmartReview_ExamHighYield`, `SmartReview_FreshLearned`, `SmartReview_LongTermConsolidation`, `AdaptiveQuiz_FinalLevelFormat`).
  - Achieved complete parity (1,223 keys) with 0 missing keys in either C# code-behind or XAML markup.
- **WinUI 3 ContentDialog Chained Teardown Safety**:
  - Hardened `SafeDialogExtensions.ShowAsyncSafe` by awaiting `Task.Yield()` immediately upon dialog dismissal before releasing `_dialogSemaphore`.
  - Guarantees that the WinUI 3 visual tree completes popup teardown prior to unblocking queued or chained dialogs, preventing WinUI 3 re-entrant `InvalidOperationException`.
- **Vocabulary Query & EF Core Memory Optimization**:
  - Applied `.AsSplitQuery()` to `GetByTermAsync` in `VocabularyRepository` to eliminate Cartesian explosion across multiple related child collections.
  - Added automatic ChangeTracker clearance on `UnitOfWork.SaveChangesAsync()` to prevent entity graph retention and long-session RAM accumulation.
- **Media & UI Lifecycle Hardening**:
  - Tracked and safely released native COM `MediaSource` and `IRandomAccessStream` resources in `WindowsAudioPlayerService.StopInternal()`.
  - Enforced minimum display duration (850ms) for `SplashScreenOverlay` regardless of OS animation settings, ensuring smooth opacity fallbacks.
  - Added direct quick-action hero launcher for `ListeningLabDialog` on `DashboardPage`.
  - Integrated local JSON persistence and recovery for active test sessions in `TestService`.
- **Quality Gates Benchmark**:
  - Clean build: **0 Warnings, 0 Errors** (`dotnet build Lexora.slnx -v q /clp:ErrorsOnly`).
  - Automated test suite: **427 / 427 Tests Passing** (376 Unit Tests + 51 Integration Tests, 0 Failed, 0 Skipped).
  - Clean Architecture boundary: **0 Violations**.
  - WinUI 3 Fluent 2 audit: **0 Violations**.

### 9. Lexora v1.4.0 — Semantic Graph, Homophones Hub & Fluent 2 UI Streamlining
- **Anti-Horizontal Scroll & Responsive Viewport Optimizations**:
  - Streamlined ComboBox filter controls and action button paddings across `VocabularyPage`, `GrammarPage`, `LearningStoragePage`, and `TestPage` to prevent unwanted horizontal scrolling on 1366x768 and compact viewports.
  - Hardened Multi-Selection floating command bars with `HorizontalScrollBarVisibility="Disabled"` and responsive text wrapping.
  - Compacted 7-Day Forecast pills and Memory Health segment cards on `DashboardPage`.
  - Streamlined `ReviewPage` top controls to prevent header bar wrapping and clipping.
- **Iconography & Visual Hierarchy Refinement**:
  - Standardized Segoe Fluent Icons (`FontIcon`) with consistent glyph codepoints and unified 12-14px sizing.
  - Eliminated bulky button borders and streamlined chip badges.
- **Dedicated Knowledge Graph Experience**:
  - Standalone `KnowledgeGraphPage` with full-screen dynamic node pivoting, CEFR tier filter pills, and audio pronunciation.
- **Interactive Homophones Hub**:
  - Confusion pair detection, phonetic comparison, and sound-alike discrimination training.
- **Test Session Resilience & Recovery**:
  - Local JSON auto-saving and recovery for active test sessions to prevent loss of progress.
- **Synchronized Version Bump to 1.4.0**:
  - `Directory.Build.props`, `SettingsViewModel.cs`, `build-installer.ps1`, `README.md`, `AGENTS.md`, `CHANGELOG.md`, `PROGRESS.md`.

### 10. Lexora v1.4.1 — Multi-Selection Ergonomics, Dialog Viewport Safety & Toast Polish
- **Vocabulary Multi-Selection 2x2 Toolbar & Detail Persistence**:
  - Refactored `SelectionActionBar` in `VocabularyPage` into a non-clipping 2x2 grid layout, eliminating truncation of the delete button on compact viewports.
  - Preserved `ViewModel.SelectedWord` when entering selection mode and kept detail pane synchronized with interacted items, preventing loss of word details.
- **Test Filters 2x2 Layout**:
  - Converted cramped single-row filter controls in `TestPage` into a balanced 2x2 layout, eliminating ComboBox text clipping.
- **Spaced Repetition Rating Ergonomics**:
  - Fine-tuned 4-rating button spacing, font sizes, and min-heights in `ReviewPage` to guarantee label visibility on all screen widths.
- **Universal ContentDialog MaxHeight Safety**:
  - Standardized inner ScrollViewer bounds to `MaxHeight="500"` across all 10 dialogs, preventing action buttons from being pushed off-screen on 1366x768 / 720p laptop displays.
  - Normalized horizontal bounds (`ContentDialogMinWidth="480"`) and locked horizontal scroll axes.
- **Toast Notification Refinement**:
  - Compacted `ToastOverlayControl` with adaptive `MinWidth="280"`, `MaxWidth="460"`, `Padding="14,10"`, and 32px icon badge.
- **Synchronized Version Bump to 1.4.1**:
  - Synchronized across `Directory.Build.props`, `Package.appxmanifest`, `SettingsViewModel.cs`, `build-installer.ps1`, `README.md`, `AGENTS.md`, `CHANGELOG.md`, `PROGRESS.md`.

### 11. Lexora v1.5.0 — Interactive Learning Roadmap & Adaptive Study Pathways
- **Interactive Learning Roadmap & Skill Tree Architecture**:
  - Designed pure domain models (`StudyRoadmap`, `RoadmapMilestone`, `RoadmapNode`, `ExamTarget`, `RoadmapNodeType`, `NodeStatus`) strictly decoupled from EF Core and UI markup.
  - Implemented `IRoadmapRepository` and EF Core Fluent configurations with cascade delete integrity and indexed queries.
  - Built `RoadmapEngine` with S-Curve Sine Wave 2D coordinates synthesis (`CanvasX`, `CanvasY`) and initial SM-2 Ease Factor calibration based on exam score gaps.
  - Created `RoadmapService` orchestrating single-commit UnitOfWork node advancement, progressive adjacent unlocking ($q \ge 3$ or 100% item completion), and milestone auto-completion.
- **Fluent 2 Visual Canvas & Smooth Spline Rendering**:
  - Built `RoadmapPage` with WinUI 3 Path-based Bézier splines (`TrackSplinePath` and `ActiveSplinePath`), connected animations (`ConnectedAnimationService`), responsive Inspector panel, and zero hardcoded colors.
  - Integrated `ThemeResourceHelper.GetThemeBrush(this, ...)` and implemented `IThemeAwareView` and `ILocalizableView` ensuring instant reactivity to theme changes and runtime language switches.
- **Reliability, Concurrency & Security Hardening**:
  - Eliminated rapid double-click vulnerabilities using `SemaphoreSlim` action locks in `RoadmapViewModel`.
  - Implemented `SafeDialogExtensions.ShowAsyncSafe` with sequential dialog safety (`await Task.Yield()`) for goal creation dialogs.
  - Added page unhooking on unload to prevent event handler retention and memory leaks.
- **Navigation & Centralized Shortcuts**:
  - Registered global keyboard accelerator `Ctrl+9` for instant Roadmap navigation via `ShortcutManager`.
  - Harmonized `ShellPage.ContentFrame.Navigated` handler to correctly highlight `NavItemRoadmap`.
- **Synchronized Version Bump to 1.5.0**:
  - Synchronized across `Directory.Build.props`, `Package.appxmanifest`, `SettingsViewModel.cs`, `build-installer.ps1`, `README.md`, `AGENTS.md`, `CHANGELOG.md`, `PROGRESS.md`.
- **Quality Gate Benchmark**:
  - Verified clean build (`0 Warning(s)`, `0 Error(s)`) and expanded test suite to **439 / 439 Tests Passing** (388 Unit Tests + 51 Integration Tests).

### 12. Lexora v1.6.0 — Architecture Resilience, FSRS Persistence & S-Curve Canvas Integration
- **Isolated Scoped ViewModels & DbContext Concurrency Safety**:
  - Replaced shared container resolution in `App.xaml.cs` with `AddIsolatedScopedViewModel`, giving each ViewModel an independent `IServiceScope` to eliminate `LexoraDbContext` threading collisions.
- **FSRS v5 State Persistence**:
  - Added dedicated `Stability` and `Difficulty` columns to `ReviewCard` and configured EF Core mappings with automatic SQLite migration.
  - Eliminated mathematical drift across repeated FSRS reviews.
- **Interactive Roadmap S-Curve Bézier Spline Canvas**:
  - Integrated 2D serpentine Bézier spline track (`CanvasX`, `CanvasY`), zoom controls (`CanvasZoom`), and `ConnectedAnimationService` node transitions on `RoadmapPage`.
- **Dynamic Multi-Word Homophones Support**:
  - Expanded `HomophonesPage` comparison grid to dynamically support groups with 3, 4+ words (e.g. *there / their / they're*).
- **SQLite Server-Side Counting & Filtering**:
  - Added `CountLearnedWordsForNodeAsync` and `GetWordIdsForNodeAsync` in `IVocabularyRepository` and `VocabularyRepository`, eliminating 95% of memory allocations during roadmap synchronization.
- **Empirical Retention Curve & Real Weekly Metrics**:
  - Replaced hardcoded floor values (12/8) and static retention curves in `StatisticsService` with empirical data derived from `StudyLogs` blended with Ebbinghaus memory decay.
- **Unified Learning Ecosystem Sync**:
  - Synchronized `RoadmapService` and `StudyPlannerService` targets, enabling real-time progression for `MockTest` and `Checkpoint` nodes via adaptive quiz sessions.
- **Keyboard Shortcut Parity**:
  - Synchronized global shortcut documentation and accelerators (`Ctrl + 1 .. 9`) across `NavigationView`, `SettingsPage`, and `README.md`.
- **Synchronized Version Bump to 1.6.0**:
  - Synchronized across `Directory.Build.props`, `Package.appxmanifest`, `SettingsViewModel.cs`, `build-installer.ps1`, `README.md`, `AGENTS.md`, `CHANGELOG.md`, `PROGRESS.md`.
- **Quality Gate Benchmark**:
  - Verified clean build (`0 Warning(s)`, `0 Error(s)`) and test suite passing at **455 / 455 Tests Passing** (396 Unit Tests + 59 Integration Tests).

### 13. Lexora v1.6.1 — Production Audit, Adaptive CAT Quiz Fixes & UI Flow Optimization
- **Adaptive CAT Quiz Option Matching Hardening**:
  - Initialized `selectedOptIndex = -1` in `TestViewModel.cs`, eliminating scoring false-positives on typed or skipped answers when correct option index is 0.
- **Skipped Question Scoring & Evaluation Counter Integrity**:
  - Implemented `SubmitAnswerInternal(allowEmpty: true)` upon next question auto-submission, preventing skipped questions from being omitted from evaluation results.
- **Adaptive Quiz Progress Alignment**:
  - Normalized `CurrentQuestionIndex` to 0-based in `LoadAdaptiveQuestion`, synchronizing `Quiz_ProgressFormat` and progress percentage accurately from question 1.
- **Dynamic Last Question Action Button**:
  - Bound `NextButtonText` and `NextButtonGlyph` in `TestPage.xaml` to dynamically transition from "Next Question" to "Finish Test" (`Test_FinishTest`) with a checkmark icon `\uE73E` on the final exam question.
- **Synchronized Version Bump to 1.6.1**:
  - Synchronized across `Directory.Build.props`, `Package.appxmanifest`, `SettingsViewModel.cs`, `build-installer.ps1`, `README.md`, `AGENTS.md`, `CHANGELOG.md`, `PROGRESS.md`.





