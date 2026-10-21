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
- **Exploration History & Safe Breadcrumbs**: Added breadcrumb navigation history stack with `NavigateBackCommand` to retrace explored vocabulary concepts.
- **Fluent 2 Design & Zero-Warning Gate**: 100% theme brush compliance across light and dark modes, bilingual localization (`vi-VN` / `en-US`), 0 build warnings, and 398 / 398 unit and integration tests passing.
