### Added
- [Spaced Repetition Configuration]: Added algorithm switcher (SM-2 classic vs FSRS v5 3D memory model) and target retention rate selector (85%, 90%, 95%) in [SettingsPage.xaml](file:///d:/LEARN/Lexora/src/Lexora.Presentation/Views/SettingsPage.xaml) persisted in [UserPreferences.cs](file:///d:/LEARN/Lexora/src/Lexora.Application/DTOs/UserPreferences.cs).
- [Forgetting Curve & Retention Comparison]: Added cognitive forgetting curve card comparing empirical retention against Ebbinghaus theoretical decay across key interval milestones (Day 1, 3, 7, 14, 30, 60+), along with personalized High-Impact Learning Recommendation banner in [DashboardPage.xaml](file:///d:/LEARN/Lexora/src/Lexora.Presentation/Views/DashboardPage.xaml).
- [Dynamic Smart Review Presets]: Integrated dynamic flyout in [ReviewPage.xaml](file:///d:/LEARN/Lexora/src/Lexora.Presentation/Views/ReviewPage.xaml) populated from `GetSmartPresetsAsync()`, featuring glyph icons, bilingual titles/descriptions, and real-time available card count badges.
- [Startup Review Toast Notification]: Automated interactive toast alert in [ShellViewModel.cs](file:///d:/LEARN/Lexora/src/Lexora.Presentation/ViewModels/ShellViewModel.cs) informing users of pending review cards on launch with 1-click navigation to the review session.

### Changed
- [ReviewService]: Dynamically routes scheduling algorithms, interval previews, and retention probabilities to SM-2 or FSRS v5 based on active user preferences.
