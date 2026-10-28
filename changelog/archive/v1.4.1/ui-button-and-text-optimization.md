### Changed
- [UI/UX]: Streamlined button dimensions, padding, and corner radii across main views to Fluent 2 standards.
- [UI/UX]: Rebuilt multi-selection toolbar in VocabularyPage into a non-clipping 2x2 grid layout, eliminating button clipping on narrow viewports.
- [UI/UX]: Refactored exam/topic/level filter controls in TestPage into a 2x2 responsive grid, preventing localized label truncation.
- [UI/UX]: Standardized all 10 ContentDialogs with safe MaxHeight="500" bounds to ensure action buttons are never pushed off-screen on 1366x768 laptop displays.
- [UI/UX]: Refined ToastOverlayControl visual balance with adaptive MinWidth="280", MaxWidth="460", compact padding, and 32px icon badge.

### Fixed
- [Vocabulary]: Fixed issue where activating multi-selection mode cleared ViewModel.SelectedWord and caused the Word Detail pane to disappear.
- [UI/UX]: Locked horizontal scroll axes on vertical scroll containers across Vocabulary, Grammar, Homophones, and Editor dialogs to eliminate accidental horizontal jitter.
