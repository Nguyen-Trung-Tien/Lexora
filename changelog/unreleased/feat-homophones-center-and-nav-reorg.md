### Added
- [Homophones Center]: New interactive Homophones & Sound-Alikes learning module with comparison cards, audio pronunciation, and responsive master-detail layout.
- [Smart Import/Export for Homophones]: Added multi-format (JSON, CSV, TXT) import/export with intelligent parsing, sample template generation, and conflict resolution.
- [Starter Pack Seeds]: Seeded standard homophone sets (bare/bear, knight/night, flour/flower, etc.) with automatic database migration.

### Changed
- [Navigation Menu]: Restructured ShellPage navigation into four logical groups (Overview, Learning & Vocabulary, Practice & Review, Storage & Archive) with WinUI 3 Fluent 2 section headers.
- [Global Shortcuts]: Harmonized application-wide Ctrl+1 through Ctrl+7 shortcuts across ShellPage and ShortcutManager.
- [Startup Progress Bar]: Implemented 60fps smooth easing interpolation and graceful stage pacing for the splash screen progress bar, eliminating instantaneous progress jumping.

### Fixed
- [WinUI 3 Header Click Crash]: Added safe type checking in NavView_ItemInvoked to prevent crashes when clicking on NavigationViewItemHeader.
- [Bilingual Parity]: Added full bilingual localization keys across vi-VN and en-US in LocalizationService.
