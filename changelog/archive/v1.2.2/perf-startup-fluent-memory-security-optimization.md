### Added
- [Presentation/Splash]: Generated and bundled lightweight pre-scaled asset `Assets/brand/lexora-logo-splash.png` (256x256, 68KB) with `BitmapImage` logical decoding properties and early preloading hook in `App.OnLaunched`.
- [Presentation/Navigation]: Integrated Fluent Design `SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }` for buttery-smooth horizontal transitions across all pages.

### Changed
- [Presentation/Startup]: Removed artificial `Task.Delay` pauses in `StartupOrchestrator` and optimized splash screen entrance threshold in `MainWindow`, eliminating visual stutter and slow logo rendering.
- [Presentation/Theme]: Dynamically resolved Fluent `{ThemeResource ...}` brushes for `HeatmapLevelToForegroundConverter`, maintaining 100% theme dictionary parity in Light and Dark modes.

### Fixed
- [Presentation/Memory]: Fixed memory leaks in `DashboardViewModel` by replacing anonymous lambda event handlers with explicit `IDisposable` event unhooking from `ILocalizationService.LanguageChanged`.
- [Presentation/Lifecycle]: Implemented `IDisposable` across `VocabularyViewModel`, `GrammarViewModel`, and `TestViewModel` to reliably cancel and dispose background tasks, search cancellation tokens, and timers.
- [Infrastructure/Security]: Hardened canonical path traversal validation with trailing directory separators in `UpdateService.DownloadUpdateAsync`.
