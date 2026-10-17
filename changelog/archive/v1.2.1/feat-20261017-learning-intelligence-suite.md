### Added
- [Learning Intelligence Dashboard]: Cognitive memory half-life calculations, peak circadian performance time slots, weekly mastery velocity tracking, and real-time exam readiness assessment indexes for TOEIC, IELTS, and VSTEP.
- [Smart Review & Memory Dynamics]: Real-time stability ($S$ days) and retrievability ($R\%$) badges on active flashcards; 5 high-yield study preset queues (Urgent Overdue, High Forgetting Risk, Exam High-Yield, Fresh Learned, Long-Term Consolidation).
- [Adaptive Quiz (CAT Engine)]: Computerized Adaptive Testing system with dynamic CEFR recalibration (+1 level on consecutive streaks, -1 level on errors) and post-quiz exam score projections.
- [Daily Study Planner]: Daily curriculum generator (due cards, new words, weak words, grammar, adaptive quiz), goal pacing engine (On Track, Ahead, Behind), and offline JSON goal storage (`%LocalAppData%/Lexora/data/study_planner_state.json`).
- [Vocabulary Knowledge Graph]: Interactive semantic relationship browser visualizing synonyms, antonyms, word families, collocations, and topic clusters with node pivoting and audio playback.
- [Data Health Center]: Comprehensive SQLite diagnostic suite executing `PRAGMA integrity_check`, `quick_check`, fragmentation analysis, orphan record cleanup, and audio cache cleaner.
- [Testing & Quality Assurance]: Added 22 new unit tests across `AdaptiveQuizServiceTests`, `SmartReviewServiceTests`, `StudyPlannerServiceTests`, and `VocabularyKnowledgeGraphServiceTests`, raising test suite coverage to 365 / 365 passing tests.
