### Changed
- [VocabularyPage]: Refactored top command bar into a responsive 3-column layout with horizontal scrolling container for action buttons, preventing search box squashing and control clipping on 1366x768 or unmaximized windows.
- [VocabularyPage]: Refactored memory statistics indicators into a responsive 2x2 grid with text trimming, ensuring no label truncation on compact side pane widths.
- [Dialogs]: Adjusted `MaxHeight` boundaries in `WordEditorDialog` and `DataHealthCenterDialog` to guarantee optimal visibility and prevent clipping on 768p displays.

### Fixed
- [Fluent 2 Compliance]: Replaced hardcoded `Foreground="White"` in `SmartImportDialog` step indicator with `{ThemeResource TextOnAccentFillColorPrimaryBrush}`.
- [Responsive Layouts]: Converted rigid fixed widths in `OperationProgressDialog` and `SmartImportDialog` to `MaxWidth` with stretch alignments.
- [Localization]: Eliminated hardcoded text literals in `GrammarPage`, `LearningStoragePage`, and `ReviewPage`, registering `Action_Refresh` across bilingual dictionaries.
