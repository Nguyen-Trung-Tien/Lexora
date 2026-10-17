### Added
- [Software Update System]: Production-grade automatic software update engine leveraging GitHub Releases API with automated background check, progress tracking, and seamless Inno Setup silent upgrade handoff.
- [Update Safety & Security]: Pre-update SQLite database snapshots to `%LocalAppData%/Lexora/backups/pre-update/`, connection pool flushing to eliminate file locks, SHA-256 cryptographic checksum verification, and canonical path traversal protection.
- [Settings UI]: Added modern Fluent 2 Software Updates card in SettingsPage with bilingual release notes preview, download progress bar, and instant install & restart.

### Changed
- [Inno Setup Configuration]: Updated `LexoraSetup.iss` with `RestartApplications=yes`, automated silent relaunch support upon update completion, temporary update cache cleanup on uninstall, and a bilingual data retention prompt.
