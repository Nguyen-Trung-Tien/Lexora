### Added
- [Localization]: Added 30+ comprehensive bilingual localization keys in `LocalizationService` (`vi-VN` and `en-US`) covering `HomophoneEditorDialog` form controls, `HomophonesPage` export formats, dynamic badges (POS, CEFR, exam types), and empty state text.

### Changed
- [Popup Dialogs]: Redesigned `HomophoneEditorDialog` to a responsive side-by-side card layout (`MaxWidth="820"`, `CornerRadius="16"`, `TitleTemplate` with brand icon) eliminating vertical scrolling and visually aligning with `HomophonesPage`.
- [Dialog Surface Consistency]: Synchronized interior card surface brushes across `KnowledgeGraphDialog` and `KnowledgeGraphImportDialog` to `CardBackgroundFillColorDefaultBrush` and `SubtleFillColorSecondaryBrush`, preventing visual wash-out.
- [Page Background Synchronization]: Removed opaque `LayerFillColorDefaultBrush` backgrounds from `<Page>` in `HomophonesPage.xaml` and `KnowledgeGraphPage.xaml`, restoring native Windows Mica backdrop transparency consistent with all other pages.
- [Catalog Pane Harmonization]: Standardized left navigation panes across `HomophonesPage` and `KnowledgeGraphPage` to `SubtleFillColorSecondaryBrush` with nested cards using `CardBackgroundFillColorDefaultBrush`.
