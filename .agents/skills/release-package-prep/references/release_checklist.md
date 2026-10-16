# 🚀 Lexora Release Checklist

Before producing or deploying an official release package, ensure each of the following gates is satisfied:

## 1. Pre-Flight Verification
- [ ] **Clean Build**: `dotnet build Lexora.slnx` compiles with `0 Warning(s)`, `0 Error(s)`.
- [ ] **Test Suites**: `dotnet test Lexora.slnx --no-build` passes all 343 tests.
- [ ] **Clean Architecture Lint**: `.agents/skills/clean-architecture-lint/scripts/lint_architecture.ps1` returns 0 violations.
- [ ] **WinUI 3 Fluent UI Audit**: `.agents/skills/winui3-fluent-audit/scripts/audit_fluent_ui.ps1` returns 0 hardcoded color violations.
- [ ] **i18n Parity Audit**: `.agents/skills/i18n-localization-sync/scripts/sync_localization.ps1 -CheckXaml` returns 100% parity.
- [ ] **SM-2 Engine Test**: `.agents/skills/sm2-algorithm-verify/scripts/verify_sm2.ps1` returns PASSED.

## 2. Changelog & Versioning
- [ ] All PR fragments in `changelog/unreleased/` have been consolidated into `CHANGELOG.md` under `## [X.Y.Z] - YYYY-MM-DD`.
- [ ] Version string is updated in `build-installer.ps1` (or passed via `-Version "X.Y.Z"`).
- [ ] `README.md` and `PROGRESS.md` version badges match.

## 3. Packaging & Distribution
- [ ] Execute `powershell -ExecutionPolicy Bypass -File ./build-installer.ps1 -Version "X.Y.Z"`
- [ ] Verify `installer/Lexora-Setup-X.Y.Z-win-x64.exe` is created with valid SHA256 checksum in `installer/SHA256SUMS.txt`.
- [ ] Perform smoke-test of installed desktop binary.
