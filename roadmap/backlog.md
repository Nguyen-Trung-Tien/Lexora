# 📋 Lexora Engineering Backlog

> Prioritized technical backlog with layer classifications, priority tags, and clear acceptance criteria.

---

## ⚡ Active Sprint: v1.3.0 / v2.0.0 Architecture & Resilience

### Feature: SQLCipher Database Encryption at Rest
- **Layer**: `Lexora.Infrastructure`, `Lexora.Domain`
- **Priority**: High
- **Status**: Ready for Planning
- **Acceptance Criteria**:
  - [ ] Implement optional master password prompt at startup.
  - [ ] Re-encrypt SQLite database using 256-bit AES via Microsoft.Data.Sqlite with SQLCipher extension.
  - [ ] Maintain backward compatibility for unencrypted legacy databases.

### Feature: On-Device Sentence Generation (ONNX Runtime)
- **Layer**: `Lexora.Infrastructure`, `Lexora.Application`
- **Priority**: High
- **Status**: Ready for Planning
- **Acceptance Criteria**:
  - [ ] Download quantized SLM (e.g., Gemma 2B or Phi-3.5 Mini) into `%LocalAppData%/Lexora/models/`.
  - [ ] Provide `IExampleGeneratorService` executing local CPU inference without sending prompts to external APIs.
  - [ ] Generate CEFR-calibrated example sentences and Vietnamese explanations.

### Feature: Export Anki Package (.apkg)
- **Layer**: `Lexora.Application`, `Lexora.Infrastructure`
- **Priority**: Medium
- **Status**: Backlog
- **Acceptance Criteria**:
  - [ ] Package vocabulary decks and audio assets into standard `.apkg` format for import into Anki mobile/desktop.

---

## 🏆 Completed Milestones & Sprints (v1.0.0 - v1.3.0)

### Feature: Comprehensive User Guide & Onboarding Hub (`HelpPage`, `HelpViewModel`)
- **Layer**: `Lexora.Presentation`, `Lexora.Application`
- **Delivered**: v1.3.0
- **Results**: Centralized Help Hub in Shell navigation, 6 structured interactive guide modules, SM-2/FSRS algorithm formula breakdown, contextual TeachingTips across all 6 core pages, global F1/Ctrl+H shortcuts, 100% bilingual parity.

### Feature: Pronunciation & Listening Lab (`ListeningLabDialog`)
- **Layer**: `Lexora.Application`, `Lexora.Presentation`
- **Delivered**: v1.2.1 / v1.2.2
- **Results**: Audio Dictation with Levenshtein scoring, Minimal Pairs phoneme discrimination, UK vs US side-by-side accent comparison. 13 unit tests added.

### Feature: Learning Intelligence & Cognitive Analytics
- **Layer**: `Lexora.Application`, `Lexora.Presentation`
- **Delivered**: v1.2.0 / v1.2.1
- **Results**: Ebbinghaus Retrievability modeling, at-risk vocabulary detector, 7-day cognitive load forecast, circadian retention analysis, and Smart Review presets.

### Feature: Computerized Adaptive Testing (CAT) & Session Recovery
- **Layer**: `Lexora.Application`, `Lexora.Presentation`
- **Delivered**: v1.2.1 / v1.2.2
- **Results**: Dynamic CEFR difficulty adjustment, test session persistence, and crash recovery.

### Feature: Vocabulary Knowledge Graph & Semantic Traversal
- **Layer**: `Lexora.Application`, `Lexora.Presentation`
- **Delivered**: v1.2.1
- **Results**: Semantic network exploring synonyms, antonyms, collocations, topics, and CEFR tiers with audio playback.

### Feature: Data Health Center & SQLite Maintenance
- **Layer**: `Lexora.Infrastructure`, `Lexora.Presentation`
- **Delivered**: v1.2.1
- **Results**: PRAGMA integrity check, WAL checkpointing, orphan record cleanup, and audio cache manager.

### Feature: Learning Storage Vault (`LearningStoragePage`)
- **Layer**: `Lexora.Domain`, `Lexora.Application`, `Lexora.Infrastructure`, `Lexora.Presentation`
- **Delivered**: v1.2.2
- **Results**: Dedicated vault for flagged words and grammar rules with custom study statuses and addition reasons.

### Feature: Software Update Engine & Pre-Update Snapshots
- **Layer**: `Lexora.Infrastructure`, `Lexora.Presentation`
- **Delivered**: v1.2.1 / v1.2.2
- **Results**: GitHub Releases API integration, path traversal protection, SQLite pre-update backup, and Inno Setup silent upgrade handoff.

### Feature: Standardize AI Agent Workspaces & Skill Invocation
- **Layer**: Cross-Cutting / Tooling
- **Delivered**: v1.2.1
- **Results**: 8 standardized agent skills, 4 personas, 7 rules, and GitHub Actions CI quality gates.
