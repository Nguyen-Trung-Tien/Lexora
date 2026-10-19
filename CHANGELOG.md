# ðŸ““ Changelog
All notable changes to the Lexora desktop application will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- [Presentation/Dashboard]: Exposed direct hero launcher on Dashboard page for pronunciation and accent comparison laboratory (`ListeningLabDialog`).
- [Application/Test]: Added active test session auto-save and interrupted session recovery in `TestService` via local JSON persistence.
- [Presentation/i18n]: Added missing localization keys in `LocalizationService.cs` across both `vi-VN` and `en-US` dictionaries for Smart Review preset titles (`SmartReview_UrgentOverdue`, `SmartReview_HighForgettingRisk`, `SmartReview_ExamHighYield`, `SmartReview_FreshLearned`, `SmartReview_LongTermConsolidation`) and Adaptive Quiz final proficiency level format (`AdaptiveQuiz_FinalLevelFormat`).
- [Tests/Unit]: Added automated verification test cases in `LocalizationServiceTests.cs` for all newly added keys, achieving 100% dictionary coverage and parity across 1,223 keys.

### Changed
- [Infrastructure/Data]: Applied `.AsSplitQuery()` to `GetByTermAsync` in `VocabularyRepository` to eliminate Cartesian explosion across multiple related collections.
- [Infrastructure/Data]: Added automatic ChangeTracker clearance on `UnitOfWork.SaveChangesAsync()` to prevent entity graph retention and memory accumulation during long desktop sessions.

### Fixed
- [Infrastructure/Audio]: Properly tracking and releasing native COM `MediaSource` and `IRandomAccessStream` resources in `WindowsAudioPlayerService.StopInternal()`.
- [Presentation/Splash]: Enforced minimum display duration (850ms) regardless of OS animation settings and ensured opacity fallbacks for smooth startup transition in `SplashScreenOverlay`.
- [Presentation/Dialogs]: Hardened `SafeDialogExtensions.ShowAsyncSafe` against WinUI 3 visual tree dialog closing collisions by awaiting `Task.Yield()` prior to releasing `_dialogSemaphore`, guaranteeing safe sequential dialog chaining and preventing `InvalidOperationException`.


## [1.2.2] - 2026-10-19

### Added
- [Presentation/Splash]: Generated and bundled lightweight pre-scaled asset `Assets/brand/lexora-logo-splash.png` (256x256, 68KB) with `BitmapImage` logical decoding properties and early preloading hook in `App.OnLaunched`.
- [Presentation/Navigation]: Integrated Fluent Design `SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }` for buttery-smooth horizontal transitions across all pages.

### Changed
- [Presentation/Startup]: Removed artificial `Task.Delay` pauses in `StartupOrchestrator` and optimized splash screen entrance threshold in `MainWindow`, eliminating visual stutter and slow logo rendering.
- [Presentation/Theme]: Dynamically resolved Fluent `{ThemeResource ...}` brushes for `HeatmapLevelToForegroundConverter`, maintaining 100% theme dictionary parity in Light and Dark modes.

### Fixed
- [Presentation/Theme]: Resolved abnormal color artifacts when switching bidirectionally between Dark and Light modes. Added standard WinUI 3 "Default" theme dictionary to `DesignSystem.xaml`, synchronized symmetrical Dark/Light custom tokens and system brush overrides, introduced bidirectional leakage guards in `ThemeResourceHelper`, implemented `IThemeAwareView` lifecycle across all cached pages (`DashboardPage`, `VocabularyPage`, `ReviewPage`, `TestPage`, `LearningStoragePage`, `SettingsPage`, `GrammarPage`), and enabled dynamic visual/segmented button re-styling on navigation and theme switching.
- [Presentation/Responsive]: Optimized and fixed responsive layout adaptations across all major views (`VocabularyPage`, `TestPage`, `DashboardPage`, `GrammarPage`, `LearningStoragePage`) and dialogs (`ListeningLabDialog`, `DataHealthCenterDialog`) for compact viewports (< 960px and < 1050px) preventing toolbar clipping, squashed button stacks, and horizontal scroll overflows.
- [Presentation/Theme]: Enhanced light mode visual contrast across all cards, badges, chips, and items (`SubtleFillColorSecondaryBrush`, `CardStrokeColorDefaultBrush`, high-contrast semantic brushes); enlarged startup splash milestone dots to 8px with vivid progression states (Current: Vibrant Blue, Done: Emerald Green, Pending: Defined Slate Gray) and rich aurora bloom.
- [Presentation/Memory]: Fixed memory leaks in `DashboardViewModel` by replacing anonymous lambda event handlers with explicit `IDisposable` event unhooking from `ILocalizationService.LanguageChanged`.
- [Presentation/Lifecycle]: Implemented `IDisposable` across `VocabularyViewModel`, `GrammarViewModel`, and `TestViewModel` to reliably cancel and dispose background tasks, search cancellation tokens, and timers.
- [Infrastructure/Security]: Hardened canonical path traversal validation with trailing directory separators in `UpdateService.DownloadUpdateAsync`.

## [1.2.1] - 2026-10-17

### Added
- Add comprehensive agent skills for WinUI 3 Fluent audit, i18n synchronization, SM-2 verification, and architecture linting (PR #14)
- Version 1.2.1 upgrade with Download Center for project repository downloads and community vocabulary/grammar sharing (v1.2.1)
- **Cinematic Startup Experience**: Upgraded `SplashScreenOverlay` into a premium cinematic Fluent 2 opening experience featuring dual-tier atmospheric aurora radial glow, specular light sheen sweep over the squircle brand emblem, energy shockwave ring pulse, and coordinated typography entrance.
- **Deterministic Milestone Progress Feedback**: Introduced truthful, multi-stage progress tracking (`0.12 -> 0.35 -> 0.70 -> 1.00`) in `IStartupOrchestrator` and `SplashScreenOverlay`, accompanied by real-time percentage readout and 4 responsive milestone status pills (`Core`, `Database`, `Data`, `Ready`).
- **Comprehensive Unit Test Suite**: Added `StartupOrchestratorTests.cs` verifying initial state, monotonic progress reporting, error handling, cancellation, and milestone event emission.
- [Pronunciation & Listening Lab]: Implemented end-to-end `IPronunciationListeningLabService`, `PronunciationListeningLabService`, and `ListeningLabDialog` featuring Audio Dictation with Levenshtein grading, Minimal Pairs phoneme discrimination (/iË/ vs /Éª/, /Ã¦/ vs /e/, /Î¸/ vs /s/, etc.) with articulatory guides, and UK vs US side-by-side accent comparison.
- [Automated Tests]: Added 13 new unit tests in `PronunciationListeningLabServiceTests.cs`, raising total passing test suite to 378/378 tests.
- [Learning Intelligence Dashboard]: Cognitive memory half-life calculations, peak circadian performance time slots, weekly mastery velocity tracking, and real-time exam readiness assessment indexes for TOEIC, IELTS, and VSTEP.
- [Smart Review & Memory Dynamics]: Real-time stability ($S$ days) and retrievability ($R\%$) badges on active flashcards; 5 high-yield study preset queues (Urgent Overdue, High Forgetting Risk, Exam High-Yield, Fresh Learned, Long-Term Consolidation).
- [Adaptive Quiz (CAT Engine)]: Computerized Adaptive Testing system with dynamic CEFR recalibration (+1 level on consecutive streaks, -1 level on errors) and post-quiz exam score projections.
- [Daily Study Planner]: Daily curriculum generator (due cards, new words, weak words, grammar, adaptive quiz), goal pacing engine (On Track, Ahead, Behind), and offline JSON goal storage (`%LocalAppData%/Lexora/data/study_planner_state.json`).
- [Vocabulary Knowledge Graph]: Interactive semantic relationship browser visualizing synonyms, antonyms, word families, collocations, and topic clusters with node pivoting and audio playback.
- [Data Health Center]: Comprehensive SQLite diagnostic suite executing `PRAGMA integrity_check`, `quick_check`, fragmentation analysis, orphan record cleanup, and audio cache cleaner.
- [Testing & Quality Assurance]: Added 22 new unit tests across `AdaptiveQuizServiceTests`, `SmartReviewServiceTests`, `StudyPlannerServiceTests`, and `VocabularyKnowledgeGraphServiceTests`, raising test suite coverage to 365 / 365 passing tests.
- [Software Update System]: Production-grade automatic software update engine leveraging GitHub Releases API with automated background check, progress tracking, and seamless Inno Setup silent upgrade handoff.
- [Update Safety & Security]: Pre-update SQLite database snapshots to `%LocalAppData%/Lexora/backups/pre-update/`, connection pool flushing to eliminate file locks, SHA-256 cryptographic checksum verification, and canonical path traversal protection.
- [Settings UI]: Added modern Fluent 2 Software Updates card in SettingsPage with bilingual release notes preview, download progress bar, and instant install & restart.
- [Spaced Repetition Configuration]: Added algorithm switcher (SM-2 classic vs FSRS v5 3D memory model) and target retention rate selector (85%, 90%, 95%) in [SettingsPage.xaml](file:///d:/LEARN/Lexora/src/Lexora.Presentation/Views/SettingsPage.xaml) persisted in [UserPreferences.cs](file:///d:/LEARN/Lexora/src/Lexora.Application/DTOs/UserPreferences.cs).
- [Forgetting Curve & Retention Comparison]: Added cognitive forgetting curve card comparing empirical retention against Ebbinghaus theoretical decay across key interval milestones (Day 1, 3, 7, 14, 30, 60+), along with personalized High-Impact Learning Recommendation banner in [DashboardPage.xaml](file:///d:/LEARN/Lexora/src/Lexora.Presentation/Views/DashboardPage.xaml).
- [Dynamic Smart Review Presets]: Integrated dynamic flyout in [ReviewPage.xaml](file:///d:/LEARN/Lexora/src/Lexora.Presentation/Views/ReviewPage.xaml) populated from `GetSmartPresetsAsync()`, featuring glyph icons, bilingual titles/descriptions, and real-time available card count badges.
- [Startup Review Toast Notification]: Automated interactive toast alert in [ShellViewModel.cs](file:///d:/LEARN/Lexora/src/Lexora.Presentation/ViewModels/ShellViewModel.cs) informing users of pending review cards on launch with 1-click navigation to the review session.
- **Update Notification System**: Added automatic background check for software updates on application launch, integrated with Windows Fluent 2 Toast notification overlay and interactive action button to navigate directly to the update view.
- **Navigation InfoBadge**: Added real-time attention dot `InfoBadge` to the Settings navigation menu item when an update is discovered.
- **Auto-Check Updates Preference**: Added `AutoCheckUpdates` user preference and UI ToggleSwitch in `SettingsPage` to let users control automatic update checking.
- **Interactive Toast Actions**: Enhanced `ToastOverlayControl` and `IToastNotificationService` to support action buttons with direct navigation callbacks.
- **Unit Tests**: Added unit tests in `UpdateServiceTests` verifying `LatestDiscoveredUpdate` property and `UserPreferences.AutoCheckUpdates` default value.
- **Global Dark & Light Mode Quick Toggle**:
- Added `NavItemThemeToggle` to `NavigationView.FooterMenuItems` in `ShellPage.xaml` alongside Settings with dynamic Sun (`\uE706`) and Moon (`\uE708`) glyphs.
- Added `ToggleTheme()` to `IThemeService` and `ThemeService.cs`, enabling 1-click theme switching from anywhere in the application.
- Registered global keyboard shortcut `Ctrl+T` in `ShortcutManager.cs` to instantly toggle between Dark and Light mode, even while dialogs are open.
- Implemented real-time dynamic theme propagation to active modal `ContentDialog` instances (`SafeDialogExtensions.CurrentlyActiveDialog`).
- Added bilingual localization keys (`Nav_ThemeLight`, `Nav_ThemeDark`, `Nav_ThemeToggle`, `Shell_TooltipThemeToggleLight`, `Shell_TooltipThemeToggleDark`) for `vi-VN` and `en-US`.
- [StudyPlannerServiceTests]: Added regression unit tests verifying atomic state persistence, zero temporary file leakage, and graceful fallback when encountering malformed storage JSON.
- [Feature Name]: Concise description of added capability.

### Changed
- **Zero-Jank Cross-Dissolve Window Transition**: Enhanced `MainWindow.xaml.cs` to eliminate visual flashing and UI thread frame hitches by synchronizing entrance timing (~800ms animated, instant on reduced motion), executing visual tree layout yield via `await Task.Yield()`, and orchestrating a simultaneous zoom-through cross-dissolve into `ShellPage`.
- **Localization Parity**: Added bilingual (`vi-VN` and `en-US`) keys for edition badge, milestone step labels, and enhanced initialization status messages in `LocalizationService`.
- [Inno Setup Configuration]: Updated `LexoraSetup.iss` with `RestartApplications=yes`, automated silent relaunch support upon update completion, temporary update cache cleanup on uninstall, and a bilingual data retention prompt.
- [ReviewService]: Dynamically routes scheduling algorithms, interval previews, and retention probabilities to SM-2 or FSRS v5 based on active user preferences.
- [NavigationService]: Optimized page switching with Fluent `EntranceNavigationTransitionInfo` and DispatcherQueue-based yielding for smooth transitions.
- [Views]: Ensured page keyboard shortcuts are preserved and restored when navigating between cached pages.
- [VocabularyPage]: Refactored top command bar into a responsive 3-column layout with horizontal scrolling container for action buttons, preventing search box squashing and control clipping on 1366x768 or unmaximized windows.
- [VocabularyPage]: Refactored memory statistics indicators into a responsive 2x2 grid with text trimming, ensuring no label truncation on compact side pane widths.
- [Dialogs]: Adjusted `MaxHeight` boundaries in `WordEditorDialog` and `DataHealthCenterDialog` to guarantee optimal visibility and prevent clipping on 768p displays.
- [Component]: Description of modified behavior.

### Fixed
- [Fluent 2 Design Compliance]: Replaced hardcoded hex colors in `App.xaml` by consolidating HeatmapLevel brushes into `Themes/DesignSystem.xaml`, and eliminated hardcoded alpha hex strings `#00000000` with `Transparent` in `SplashScreenOverlay.xaml` (100% compliant with zero audit violations).
- [Bilingual Parity & Localization]: Added 32 missing localization keys across Vietnamese (`vi-VN`) and English (`en-US`) for Learning Intelligence, CAT Adaptive Quiz, Memory Stability, and Listening Lab, achieving 100% dictionary and XAML reference parity (1,173 keys).
- [Audio MIME Type Handling]: Updated `WindowsAudioPlayerService` to dynamically detect `.mp3` vs `.wav` cached human audio streams.
- [Navigation]: Resolved application crash when switching navigation tabs rapidly by implementing request coalescing, pending navigation queueing, safe frame event lifecycle handlers, and defensive error boundaries.
- [LearningStorage]: Fixed `ObjectDisposedException` on returning to cached LearningStoragePage by preventing premature `SemaphoreSlim` disposal during page unload.
- [Fluent 2 Compliance]: Replaced hardcoded `Foreground="White"` in `SmartImportDialog` step indicator with `{ThemeResource TextOnAccentFillColorPrimaryBrush}`.
- [Responsive Layouts]: Converted rigid fixed widths in `OperationProgressDialog` and `SmartImportDialog` to `MaxWidth` with stretch alignments.
- [Localization]: Eliminated hardcoded text literals in `GrammarPage`, `LearningStoragePage`, and `ReviewPage`, registering `Action_Refresh` across bilingual dictionaries.
- **DataHealthCenterDialog Layout & Sizing**:
- Restructured `DataHealthCenterDialog` into a 2-row layout with scrollable telemetry (Row 0) and pinned bottom Action Bar (Row 1), ensuring `Run Diagnostics`, `Clear Audio Cache`, and `Repair & Optimize` buttons are permanently visible and never pushed out of the viewport.
- Added `ContentDialogMaxWidth` (`740`) and `ContentDialogMinWidth` (`560`) resources, resolving narrow popup rendering in WinUI 3.
- Implemented `ContentDialog.TitleTemplate` with a brand Fluent icon and bold heading.
- Routed invocation in `SettingsPage.xaml.cs` through `dialog.ShowAsyncSafe()` for root stability and theme synchronization.
- **Real-Time Language Switching (i18n)**: Resolved asynchronous dispatcher nesting lag during language changes by checking `DispatcherQueue.HasThreadAccess` across all localizable pages (`SettingsPage`, `VocabularyPage`, `DashboardPage`, `ReviewPage`, `TestPage`, `GrammarPage`, `LearningStoragePage`, `ShellPage`, and `MainWindow`).
- **Settings ViewModel Localization Sync**: Implemented `UpdateLanguageState()` and `IDisposable` in `SettingsViewModel` subscribing to `ILocalizationService.LanguageChanged` to dynamically update update status, TTS voice diagnostic descriptions, and database status messages.
- **Settings ToggleSwitch Parity**: Added localized string bindings (`Settings_Toggle_On` and `Settings_Toggle_Off`) to `AutoCheckUpdates` toggle switch in `SettingsPage.xaml`.
- **Instant Language Button Refresh**: Wired instantaneous `OnLanguageChanged()` execution directly on language selection button clicks (`LangViButton_Click`, `LangEnButton_Click`) for immediate, zero-latency visual transition without requiring app restart.
- [SafeDialogExtensions]: Unified all modal dialog triggers across Presentation views (`DashboardPage`, `TestPage`, `VocabularyPage`) to use `ShowAsyncSafe` with UI thread validation and stable `XamlRoot` resolution, completely eliminating raw `ShowAsync()` race conditions and COMException / InvalidOperationException dialog crashes.
- [LearningStorageViewModel]: Fixed unmanaged wait handle resource leak by ensuring `_dbLock` semaphore is properly disposed in `Dispose()`.
- [StudyPlannerService]: Replaced direct truncate-and-write with atomic temporary file swapping (`File.Move` with `overwrite: true`) to prevent data corruption during power loss or abrupt shutdown.
- [AutoUpdate]: Resolved application crash (`COMException 0x8001010E: The application called an interface that was marshalled for a different thread`) when checking for software updates by replacing `Task`-based `EnsureOnUIThreadAsync` with a direct `DispatcherQueueAwaitable` and introducing thread-safe `RunOnUI` dispatching across `SettingsViewModel`.
- [Presentation]: Fixed cross-thread property mutations affecting WinUI 3 `ProgressRing.IsActive` during asynchronous update checks and downloads.
- Add comprehensive real-time bilingual support for Learning Storage and Memory Health (i18n-storage)
- [Issue/Bug]: Description of bug resolved.

### Security
- [Security Fix]: Description of security hardening applied.

### Added
- Enterprise-grade AI Agent Governance runtime: `.agents/skills/lexora-quality-audit/` for automated verification.
- GitHub Actions CI/CD workflows for quality gate enforcement and SemVer validation.

---

## [1.1.0] - 2026-10-16
### Added
- **FSRS v5 Spaced Repetition Engine**: Introduced `IFsrsSpacedRepetitionEngine` implementing the Free Spaced Repetition Scheduler v5 algorithm with Stability ($S$), Difficulty ($D$), and Retrievability ($R$) metrics.
- **Flashcard 3D Spatial Physics**: Implemented two-stage 300ms 3D flip animation using WinUI 3 `PlaneProjection` with smooth 90Â° content transition.
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
