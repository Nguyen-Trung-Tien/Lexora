# ADR-0002: Offline-First Persistence Using SQLite in WAL Mode

- **Status**: Accepted
- **Date**: 2026-02-10
- **Authors**: Architectural Board
- **Deciders**: Lexora Core Team

## Context & Problem Statement
Learners preparing for TOEIC/IELTS/VSTEP require continuous access to their vocabulary vaults without internet access. The application needs a local database engine that provides ACID transactions, high read throughput, and resistance to file corruption during unexpected power cuts or process terminations.

## Decision Drivers
- Zero server dependency for core learning workflows.
- Concurrency: Background sync/import tasks must not block UI read operations.
- Data integrity: Resilient against application crashes.

## Considered Options
1. Embedded LiteDB (Document store).
2. Local JSON files per deck.
3. SQLite 3 with EF Core in Write-Ahead Logging (WAL) Mode.

## Decision Outcome
Chosen option: **SQLite 3 with EF Core in Write-Ahead Logging (WAL) Mode**.

### Key Configurations
- Default path: `%LocalAppData%/Lexora/data/lexora.db`.
- PRAGMAs:
  - `journal_mode = WAL` (enables non-blocking concurrent readers during writes).
  - `synchronous = NORMAL`.
  - `foreign_keys = ON`.

### Positive Consequences
- Robust ACID guarantees.
- Full compatibility with EF Core LINQ queries and migrations.
- Simple single-file database backup and restore with 16-byte magic header verification.

### Negative Consequences / Trade-offs
- Backup operations require flushing connection pools (`SqliteConnection.ClearAllPools()`) to ensure WAL locks are released before copying.
