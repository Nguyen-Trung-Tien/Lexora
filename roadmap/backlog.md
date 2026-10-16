# 📋 Lexora Engineering Backlog

> Prioritized technical backlog with layer classifications, priority tags, and clear acceptance criteria.

---

## ⚡ Active Sprint (v1.1.0 Hardening)

### Feature: Standardize AI Agent Workspaces & Skill Invocation
- **Layer**: Cross-Cutting / Tooling
- **Priority**: High
- **Status**: Completed
- **Acceptance Criteria**:
  - [x] Standardize `.agents/rules/` and `rules/` directory with numbered, single-responsibility files.
  - [x] Create `.agents/personas/` for role separation (`architect`, `fluent-ui-designer`, `qa-engineer`, `security-auditor`).
  - [x] Create actionable agent skills with automation scripts in `.agents/skills/`:
    - `clean-architecture-lint` (static boundary analysis)
    - `winui3-fluent-audit` (Fluent 2 zero-hardcoded-colors & responsive layout)
    - `i18n-localization-sync` (bilingual dictionary & XAML parity validation)
    - `sm2-algorithm-verify` (SM-2 formula & test invariant verification)
    - `db-migration-check` (SQLite schema, WAL mode & backup safety)
    - `lexora-quality-audit` (0-warning build & test suite verification)
    - `changelog-bump` (Keep-a-Changelog fragment creation & release consolidation)
    - `release-package-prep` (version synchronization & release packaging readiness)
  - [x] Establish `docs/adr/` with template and foundational records.
  - [x] Establish `roadmap/` and `changelog/` fragment directories.

### Feature: Changelog Fragment CI Verification
- **Layer**: DevOps / CI
- **Priority**: Medium
- **Status**: Completed
- **Acceptance Criteria**:
  - [x] GitHub Actions workflow `.github/workflows/changelog-enforcer.yml` checks for fragment files on Pull Requests.

---

## 🎯 Upcoming Backlog (v1.2.0 - v2.0.0)

### Feature: SQLCipher Database Encryption at Rest
- **Layer**: `Lexora.Infrastructure`, `Lexora.Domain`
- **Priority**: High
- **Acceptance Criteria**:
  - [ ] Implement optional master password prompt at startup.
  - [ ] Re-encrypt SQLite database using 256-bit AES via Microsoft.Data.Sqlite with SQLCipher extension.
  - [ ] Maintain backward compatibility for unencrypted legacy databases.

### Feature: On-Device Sentence Generation (ONNX Runtime)
- **Layer**: `Lexora.Infrastructure`, `Lexora.Application`
- **Priority**: Medium
- **Acceptance Criteria**:
  - [ ] Download quantized SLM (e.g., Gemma 2B or Phi-3.5 Mini) into `%LocalAppData%/Lexora/models/`.
  - [ ] Provide `IExampleGeneratorService` executing local CPU inference without sending prompts to external APIs.
  - [ ] Generate CEFR-calibrated example sentences and Vietnamese explanations.

### Feature: Export Anki Package (.apkg)
- **Layer**: `Lexora.Application`, `Lexora.Infrastructure`
- **Priority**: Medium
- **Acceptance Criteria**:
  - [ ] Package vocabulary decks and audio assets into standard `.apkg` format for import into Anki mobile/desktop.
