### Added
- **Update Notification System**: Added automatic background check for software updates on application launch, integrated with Windows Fluent 2 Toast notification overlay and interactive action button to navigate directly to the update view.
- **Navigation InfoBadge**: Added real-time attention dot `InfoBadge` to the Settings navigation menu item when an update is discovered.
- **Auto-Check Updates Preference**: Added `AutoCheckUpdates` user preference and UI ToggleSwitch in `SettingsPage` to let users control automatic update checking.
- **Interactive Toast Actions**: Enhanced `ToastOverlayControl` and `IToastNotificationService` to support action buttons with direct navigation callbacks.
- **Unit Tests**: Added unit tests in `UpdateServiceTests` verifying `LatestDiscoveredUpdate` property and `UserPreferences.AutoCheckUpdates` default value.
