### Fixed
- [SafeDialogExtensions]: Unified all modal dialog triggers across Presentation views (`DashboardPage`, `TestPage`, `VocabularyPage`) to use `ShowAsyncSafe` with UI thread validation and stable `XamlRoot` resolution, completely eliminating raw `ShowAsync()` race conditions and COMException / InvalidOperationException dialog crashes.
- [LearningStorageViewModel]: Fixed unmanaged wait handle resource leak by ensuring `_dbLock` semaphore is properly disposed in `Dispose()`.
- [StudyPlannerService]: Replaced direct truncate-and-write with atomic temporary file swapping (`File.Move` with `overwrite: true`) to prevent data corruption during power loss or abrupt shutdown.

### Added
- [StudyPlannerServiceTests]: Added regression unit tests verifying atomic state persistence, zero temporary file leakage, and graceful fallback when encountering malformed storage JSON.
