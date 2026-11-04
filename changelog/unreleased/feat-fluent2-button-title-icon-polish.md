### Added
- [UI/UX Iconography]: Integrated standardized Segoe Fluent Icons (`&#xE81C;`, `&#xE73E;`, `&#xE76C;`, `&#xE762;`, `&#xE894;`, `&#xE711;`, `&#xE72C;`, `&#xE710;`, `&#xE890;`) across Learning Storage, Vocabulary, Grammar, Review, and Test pages.
- [Page Header Standard]: Unified branded Icon Badges for Learning Storage and Roadmap pages with consistent 42x42 dimensions and accent tinting.

### Changed
- [Microcopy & Localization]: Streamlined Vietnamese and English button labels in `LocalizationService` (removed redundant parenthesis like "(Weak)", updated action verbs "Luyện tập", "Thi đánh giá", "Xác nhận", "Tiếp tục").
- [Roadmap Microcopy & Colors]: Optimized learning roadmap button copy to concise action verbs ("+25% Tiến độ", "Hoàn thành", "Học ngay", "Đồng bộ", "Tạo lộ trình") and synchronized icon and text foreground brushes (`TextOnAccentFillColorPrimaryBrush`, `TextFillColorPrimaryBrush`, `SystemFillColorSuccessBrush`).
- [Vocabulary Toolbar]: Renamed misleading "Chép chính tả" label on Pronunciation & Listening Lab toolbar button to "Luyện nghe" (`Vocab_BtnListeningLab`).
- [Multi-Selection Actions]: Synchronized icon and typography styles for Select All and Deselect All buttons across all multi-select action bars.

### Fixed
- [Roadmap Deadlock]: Resolved UI freeze and lockup when clicking "Advance Progress (+25%)" or "Mark Completed" by decoupling `LoadActiveRoadmapCoreAsync` from `_actionLock` re-entrance in `RoadmapViewModel.cs`, safely releasing submitting state and restoring interactive controls.
- [Learning Storage]: Resolved runtime XAML parse exception (`undeclared prefix [Line: 19 Position: 169]`) when opening Add Word/Grammar candidate dialogs in `LearningStoragePage.xaml.cs` by adding missing `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` to dynamic DataTemplates and escaping localized string tokens.

