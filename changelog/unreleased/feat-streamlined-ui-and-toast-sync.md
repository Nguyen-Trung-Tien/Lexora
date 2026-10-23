### Added
- [Review Focus Mode]: Implemented dedicated distraction-free Zen study mode toggle on ReviewPage (`IsFocusMode`) that hides sidebars and centers flashcard with optimal cognitive ergonomics.
- [Bilingual Localization]: Added `Review_FocusMode` and `Review_FocusModeTooltip` keys across both `vi-VN` and `en-US` dictionaries in `LocalizationService`.

### Changed
- [Top-Center Floating Toast]: Repositioned `ToastOverlayControl` in `ShellPage.xaml` to the top-center of the screen (`HorizontalAlignment="Center"`, `VerticalAlignment="Top"`), enhanced with fluid slide-down entrance from top and slide-up exit animations.
- [Vocabulary & Grammar Toolbars]: Streamlined command bar layouts in `VocabularyPage` and `GrammarPage`, eliminating layout-shifting inline InfoBars in favor of non-blocking `IToastNotificationService` toasts.
- [Dashboard Layout]: Streamlined Hero Section badge hierarchy to reduce cognitive noise, and introduced responsive 4-column KPI metric card layout on desktop viewports with seamless 2x2 fallback for compact widths.

### Fixed
- [Layout Shift Elimination]: Resolved vertical jumping of list content when notifications are triggered on core study pages.
