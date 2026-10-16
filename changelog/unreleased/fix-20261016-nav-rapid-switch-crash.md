### Fixed
- [Navigation]: Resolved application crash when switching navigation tabs rapidly by implementing request coalescing, pending navigation queueing, safe frame event lifecycle handlers, and defensive error boundaries.
- [LearningStorage]: Fixed `ObjectDisposedException` on returning to cached LearningStoragePage by preventing premature `SemaphoreSlim` disposal during page unload.

### Changed
- [NavigationService]: Optimized page switching with Fluent `EntranceNavigationTransitionInfo` and DispatcherQueue-based yielding for smooth transitions.
- [Views]: Ensured page keyboard shortcuts are preserved and restored when navigating between cached pages.
