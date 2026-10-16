---
name: changelog-bump
description: Generates standardized changelog fragments in changelog/unreleased/ and automates release consolidation into CHANGELOG.md.
version: 1.1.0
tags: [changelog, keep-a-changelog, semver, release]
---

# 📝 Skill: Changelog Bump & Fragment Management

## 1. Capabilities & Workflow
This skill automates [Keep a Changelog](https://keepachangelog.com/) and [Semantic Versioning 2.0.0](https://semver.org/) fragment authoring and release consolidation.

### Key Workflows:
1. **Feature/Bugfix Fragment Creation**: Creates a timestamped `.md` file in `changelog/unreleased/` under the appropriate category (`Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`).
2. **Release Consolidation**: Merges all unreleased fragments into `CHANGELOG.md` under `## [X.Y.Z] - YYYY-MM-DD` and moves processed fragments to `changelog/archive/vX.Y.Z/`.

---

## 2. Execution Commands

### Create an Unreleased Fragment:
```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/changelog-bump/scripts/bump_changelog.ps1 -Type "Added" -Description "Add smart fuzzy search algorithm for vocabulary terms" -PrOrIssue "PR #12"
```

### Consolidate Fragments into a Release:
```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/changelog-bump/scripts/bump_changelog.ps1 -Consolidate -ReleaseVersion "1.1.0"
```

---

## 3. Supported Fragment Categories

| Category | When to Use |
| :--- | :--- |
| `Added` | New user-facing features, commands, or capabilities |
| `Changed` | Changes in existing functionality or UI workflows |
| `Deprecated` | Features earmarked for deprecation in upcoming versions |
| `Removed` | Features or legacy APIs completely removed |
| `Fixed` | Bug fixes and crash resolutions |
| `Security` | Security vulnerabilities and hardening improvements |
