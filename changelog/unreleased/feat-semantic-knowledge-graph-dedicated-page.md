# feat: Dedicated Semantic Knowledge Graph Page with Pagination, Smart Search & Radial Visualization

- **Category**: Added / Changed
- **Date**: 2026-10-21
- **Layer**: Presentation, Application, Domain

### Summary of Changes
- **Dedicated Page Architecture**: Extracted the Semantic Knowledge Graph out of modal dialog into a full-screen, dedicated desktop experience (`KnowledgeGraphPage.xaml`, `KnowledgeGraphViewModel.cs`) accessible via primary navigation (`NavItemKnowledgeGraph`, `Ctrl+4`) and direct word inspection from `VocabularyPage`.
- **Intelligent Pagination & Catalog**: Added concept pagination (`KnowledgeGraphConceptsPagedResultDto`, `KnowledgeGraphQueryDto`) with customizable page sizes, CEFR level filter chips (A1-C2), and real-time concept count indicators.
- **Smart Recommendations & Fast Exploration**: Implemented `GetSmartRecommendationsAsync` and `GetConceptsPagedAsync` in `IVocabularyKnowledgeGraphService` and `VocabularyKnowledgeGraphService` to provide high-yield word suggestions with explainable semantic reasons and estimated network connectivity.
- **Dual Visualization Modes**:
  - **Radial Orbit Canvas**: Visual graph canvas rendering the central core concept surrounded by orbit nodes color-coded by relationship type with connecting stroke lines and interactive single-click inspect / double-click pivot navigation.
  - **Connections Card List**: Card-based categorized view (Synonyms, Antonyms, Word Family, Collocations, Topic Clusters) with rationale explanations, connection weights, and audio playback.
- **Smart Import & Multi-Format Export**:
  - Implemented `IKnowledgeGraphExportImportService` and `KnowledgeGraphExportImportService` supporting:
    - **JSON Knowledge Graph**: Full node & edge graph structure with metadata and weights.
    - **CSV Relationship Matrix**: Tabular relationship dataset (`SourceTerm`, `TargetTerm`, `Relationship`, `Weight`, `Level`, `Explanation`).
    - **Markdown Mindmap**: Hierarchical outline formatting for Obsidian, Notion, and Logseq note-taking.
  - Added `KnowledgeGraphImportDialog` with a 3-step wizard: source selection / paste, conflict resolution, live relationship validation preview, auto-creation of missing vocabulary, and single-commit atomic note persistence.
- **Linguistic Precision & Curated Academic Thesaurus**:
  - Introduced `CuratedAcademicThesaurus` providing verified, high-accuracy relationship pairs for core Oxford 3000 / TOEIC / IELTS academic vocabulary.
  - Eliminated naive single-token definition overlap false positives, expanded Vietnamese/English stop words, enforced part-of-speech compatibility for synonyms, and removed arbitrary antonym heuristics.
- **Enhanced Graph UX & Usability**:
  - Added floating canvas Zoom controls (Zoom In, Zoom Out, Reset 100%) with dynamic scaling.
  - Added Quick Actions on inspected nodes: "Sao chép từ" (`Copy Term` to clipboard), "Xem trong Từ điển" (`View in Vocabulary`), and native audio pronunciation.
- **Fluent 2 Design & Quality Gates**: 100% theme brush compliance, dual localization (`vi-VN` / `en-US`), 0 build warnings, and **404 / 404 tests passing** (353 Unit + 51 Integration).
