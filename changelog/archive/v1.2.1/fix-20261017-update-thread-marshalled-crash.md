### Fixed
- [AutoUpdate]: Resolved application crash (`COMException 0x8001010E: The application called an interface that was marshalled for a different thread`) when checking for software updates by replacing `Task`-based `EnsureOnUIThreadAsync` with a direct `DispatcherQueueAwaitable` and introducing thread-safe `RunOnUI` dispatching across `SettingsViewModel`.
- [Presentation]: Fixed cross-thread property mutations affecting WinUI 3 `ProgressRing.IsActive` during asynchronous update checks and downloads.
