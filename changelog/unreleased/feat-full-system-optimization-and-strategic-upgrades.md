### Added
- [Listening Lab Quick Action]: Exposed direct hero launcher on Dashboard page for pronunciation and accent comparison laboratory (`ListeningLabDialog`).
- [Test Session Persistence]: Added active test session auto-save and interrupted session recovery in `TestService` via local JSON persistence.

### Changed
- [Vocabulary Query Optimization]: Applied `.AsSplitQuery()` to `GetByTermAsync` in `VocabularyRepository` to eliminate Cartesian explosion across multiple related collections.
- [EF Core Memory Management]: Added automatic ChangeTracker clearance on `UnitOfWork.SaveChangesAsync()` to prevent entity graph retention and memory accumulation during long desktop sessions.

### Fixed
- [Windows Audio COM Leak]: Properly tracking and releasing native COM `MediaSource` and IRandomAccessStream resources in `WindowsAudioPlayerService.StopInternal()`.
- [Splash Screen Display Bug]: Enforced minimum display duration (850ms) regardless of OS animation settings and ensured opacity fallbacks for smooth startup transition.
