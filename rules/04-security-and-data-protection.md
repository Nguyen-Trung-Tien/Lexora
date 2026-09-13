# 🔒 Rule 04: Security & Data Protection Invariants

---

## 1. File Path Sanitization & Path Traversal Defenses

Lexora accesses the local file system for pronunciation audio caching (`LocalAudioStorageService`), user CSV/JSON exports/imports, and database backups (`DatabaseBackupService`).

### Path Traversal Defense Checklist:
1. **Filename Sanitization**:
   - Strip directory traversal sequences (`..`, `/`, `\`, encoded equivalents `%2e%2e`).
   - Retain only whitelisted characters: `[a-zA-Z0-9_\-\.]`.
2. **Canonical Base Directory Check**:
   - Always verify that the resolved canonical path lies strictly inside the intended storage root:
     ```csharp
     var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, sanitizedFileName));
     if (!fullPath.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
     {
         throw new SecurityException("Path traversal attempt detected.");
     }
     ```
3. **Audio File Extensions**:
   - Restrict allowed audio file extensions exclusively to `.mp3`, `.wav`, and `.ogg`.

---

## 2. Database Backup & Restore Integrity

When restoring a database backup:

1. **Magic Header Verification**:
   - The restore engine **MUST** inspect the first 16 bytes of any `.bak` or `.db` candidate file before modifying the current database.
   - The file header must match the standard SQLite magic string:
     ```csharp
     // SQLite 3 file format signature: "SQLite format 3\0"
     private static readonly byte[] SqliteMagicHeader =
     [
         0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66,
         0x6F, 0x72, 0x6D, 0x61, 0x74, 0x20, 0x33, 0x00
     ];
     ```
   - If the header does not match, reject immediately with a descriptive `InvalidOperationException`.
2. **Connection Pool & WAL Lock Clearance**:
   - Prior to replacing the active database file, flush and close all active connections:
     ```csharp
     Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
     ```
   - Delete any existing Write-Ahead Log companion files (`lexora.db-wal` and `lexora.db-shm`) to prevent catalog corruption.
3. **Atomic Replacement**:
   - Maintain a backup of the current database prior to swap so a failed restore can automatically roll back.

---

## 3. Data Import Payload Boundaries

1. **Input Size Ceilings**:
   - All bulk imports (CSV and JSON parsers in `ExportImportService`) must enforce an upper boundary check:
     - Default maximum file size: **20 MB**.
     - Files exceeding this threshold must be rejected prior to in-memory buffering.
2. **Deduplication & Sanitization**:
   - De-duplicate entries by normalized terms (`NormalizedTerm`) before persisting.
   - Validate UTF-8 encoding; reject corrupted byte streams.
3. **Transaction Isolation**:
   - Execute batch inserts in a single transaction using `IUnitOfWork.SaveChangesAsync()`. If any entity validation fails, roll back the entire batch to preserve referential integrity.
