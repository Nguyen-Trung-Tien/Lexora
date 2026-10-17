---
title: "Fix popup layout cut-off and add global Dark/Light mode toggle"
type: "fix"
author: "AI Assistant"
date: "2026-10-17"
---

### Fixed
- **DataHealthCenterDialog Layout & Sizing**:
  - Restructured `DataHealthCenterDialog` into a 2-row layout with scrollable telemetry (Row 0) and pinned bottom Action Bar (Row 1), ensuring `Run Diagnostics`, `Clear Audio Cache`, and `Repair & Optimize` buttons are permanently visible and never pushed out of the viewport.
  - Added `ContentDialogMaxWidth` (`740`) and `ContentDialogMinWidth` (`560`) resources, resolving narrow popup rendering in WinUI 3.
  - Implemented `ContentDialog.TitleTemplate` with a brand Fluent icon and bold heading.
  - Routed invocation in `SettingsPage.xaml.cs` through `dialog.ShowAsyncSafe()` for root stability and theme synchronization.

### Added
- **Global Dark & Light Mode Quick Toggle**:
  - Added `NavItemThemeToggle` to `NavigationView.FooterMenuItems` in `ShellPage.xaml` alongside Settings with dynamic Sun (`\uE706`) and Moon (`\uE708`) glyphs.
  - Added `ToggleTheme()` to `IThemeService` and `ThemeService.cs`, enabling 1-click theme switching from anywhere in the application.
  - Registered global keyboard shortcut `Ctrl+T` in `ShortcutManager.cs` to instantly toggle between Dark and Light mode, even while dialogs are open.
  - Implemented real-time dynamic theme propagation to active modal `ContentDialog` instances (`SafeDialogExtensions.CurrentlyActiveDialog`).
  - Added bilingual localization keys (`Nav_ThemeLight`, `Nav_ThemeDark`, `Nav_ThemeToggle`, `Shell_TooltipThemeToggleLight`, `Shell_TooltipThemeToggleDark`) for `vi-VN` and `en-US`.
