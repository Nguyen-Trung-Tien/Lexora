# 🗺️ Feature: Interactive Learning Roadmap & Adaptive Study Pathways

## Summary
- **Type**: Feature & Stabilization
- **Module**: Learning Roadmap, Spaced Repetition, Navigation, WinUI 3 Presentation
- **Version**: 1.5.0

## Key Changes
- **Interactive Roadmap Engine**: S-Curve Bézier spline synthesis and adaptive skill trees for TOEIC, IELTS, and VSTEP.
- **SM-2 Difficulty Calibration**: Score gap analysis dynamically adjusting initial Ease Factor weights.
- **Progressive Node Unlocking**: Single-commit atomic unlock progression through `IRoadmapRepository` and `IUnitOfWork`.
- **Centralized Shortcut**: Global `Ctrl+9` keyboard accelerator for roadmap navigation.
- **Hardening**: `SafeDialogExtensions.ShowAsyncSafe`, `_actionLock` concurrency guarding, `ILocalizableView` & `IThemeAwareView` lifecycle integration.

