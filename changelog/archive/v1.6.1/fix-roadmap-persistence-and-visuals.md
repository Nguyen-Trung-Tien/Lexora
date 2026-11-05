---
type: fix
title: Fix Roadmap persistence, DI captive dependency and visual Bézier spline rendering
issue: roadmap-persistence-loss
date: 2026-11-04
---

### Fixed
- **Roadmap Persistence & Retention**: Resolved issue where created roadmaps and study progression were lost upon app exit or navigation by deactivating all old active roadmaps in `RoadmapService.GenerateAdaptiveRoadmapAsync` and selecting newest active roadmap via `OrderByDescending(r => r.CreatedAt)` in `RoadmapRepository`.
- **DI Captive Dependency Resolution**: Corrected `RoadmapViewModel` lifetime from `Singleton` to `Transient` in `App.xaml.cs`, eliminating captive scoped `LexoraDbContext` conflicts and state corruption across page navigations.
- **Bézier Spline Sequence Correction**: Fixed critical curve rendering bug in `RoadmapPage.xaml.cs` where nodes were incorrectly ordered by local milestone `OrderIndex` instead of `CanvasY`, restoring smooth serpentine Bézier spline traversal.
- **Roadmap Visual Enhancement**: Redesigned roadmap canvas with Fluent 2 milestone phase badges, polished node cards (164x72), active status indicators, and added direct "Advance Progress" and "Mark as Mastered" actions in the inspector.
- **EF Core Identity Conflict Prevention**: Fixed `InvalidOperationException` crash when generating a new roadmap while an active roadmap is already tracked in the DbContext by introducing `IRoadmapRepository.DeactivateAllRoadmapsAsync` and handling tracked entity resolution in `Repository<T>.UpdateAsync`.
- **XAML Binding InvalidCastException Fix**: Removed invalid `Visibility` to `Style` type binding on roadmap view mode buttons that triggered app crashes upon page navigation and mode switching.

