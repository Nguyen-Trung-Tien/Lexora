---
type: changed
title: Redesign Roadmap feature with Fluent 2 learning pathway, replace legacy canvas, and fix UI state loss on exit
issue: roadmap-canvas-redesign-and-persistence-fix
date: 2026-11-04
---

### Added
- **Roadmap History Deletion & Progress Reset**:
  - `IRoadmapService.ResetRoadmapProgressAsync`: Allows users to reset the active or selected roadmap's learning progress back to 0% (unlocks initial Phase 1 lesson, resets learned items, and locks downstream lessons) with a confirmation dialog.
  - `IRoadmapService.ClearInactiveRoadmapsAsync`: Cleans up past/inactive roadmaps from the database with 1-click while safeguarding the active target.
  - `IRoadmapService.ClearAllRoadmapsAsync`: Allows complete history wipe and return to pristine clean state.
  - UI action buttons for "Reset Progress" in active roadmap header, "Clear Inactive History" and "Clear All Roadmaps" in the Manage Goals workspace, each guarded by safe `ContentDialog` confirmations.
  - Automated integration test coverage in `RoadmapPersistenceIntegrationTests.cs` (451/451 total tests passing).

### Changed
- **Eliminated 2D Canvas Skill Map**: Completely removed the fragile 1600px 2D canvas, manual Bézier curve drawing, coordinate math, nested scroll jank, zooming glitches, and layout clipping.
- **Modern Fluent 2 Learning Pathway UI**: Designed and implemented a responsive, intuitive, and modern learning track featuring:
  - Responsive Overview Metric KPI Cards (Overall Progress, Target Scores, Days Remaining, Mastered Lessons).
  - Prominent "Up Next Lesson" Hero Banner with direct quick-action practice launcher.
  - Three dedicated View Modes: Milestone Track (interactive master-detail pathway), Timeline (chronological schedule), and Manage Goals (multi-target management with instant activation and deletion).
  - Responsive Master-Detail Inspector with lesson details, linked grammar/vocabulary topic displays, SM-2 Ease Factor calibration, and actions ("Start Learning", "Mark Mastered", "Advance +25%").
  - Quick-start Presets for TOEIC (750+), IELTS (6.5+), and VSTEP (B2).

### Fixed
- **Roadmap Blank UI ("Mất Giao Diện") & Persistence on Exit ("Không Lưu Giao Diện")**:
  - Added self-healing fallback in `RoadmapRepository.GetActiveRoadmapWithDetailsAsync`: if roadmaps exist in SQLite but none is flagged active, the newest roadmap is automatically marked active and persisted.
  - Added `RoadmapViewMode` and `LastActiveRoadmapId` to `UserPreferences` so the user's active tab and active roadmap selection persist across application restarts.
  - Decoupled rendering from window dimension measurements and async canvas clearing, guaranteeing instantaneous and stable UI presentation.

