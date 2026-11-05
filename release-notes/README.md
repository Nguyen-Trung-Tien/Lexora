# 📋 Release Notes Log Directory — Lexora

This directory contains the user-facing **Release Notes Logs** for each official version of the Lexora desktop application.

Whenever a new version is released or bumped, developers or AI Agents **MUST** create a corresponding markdown file (e.g., `v1.6.2.md`) based on the canonical [`template.md`](./template.md).

---

## 🎯 Purpose & Authoring Standards

Unlike `CHANGELOG.md` (which focuses on low-level developer commits, internal classes, and repository diffs), files in `release-notes/` are crafted specifically for **learners**:
1. **Simple, Concise & Understandable**: Written in straightforward, accessible English. Avoid internal code jargon such as private methods, repository names, or database column names.
2. **Focus on Learner Benefits**: Clearly describe what each new feature does and how it helps users prepare for TOEIC, IELTS, and VSTEP exams.
3. **Clean Markdown Format**: Use standard headings (`#`, `##`), bullet points (`- `), bold text (`**term**`), and emojis so that Lexora's in-app `ReleaseNotesRenderer` can render the notes natively and beautifully.
4. **Offline Desktop Availability**: These markdown files are automatically packaged with the desktop installer and displayed directly in the in-app **Release Notes Log Dialog**.

---

## 📁 Directory Structure

```
release-notes/
├── README.md               # This specification and guideline document
├── template.md             # Standard template for authoring new release notes
├── v1.6.1.md               # Version 1.6.1 (Current)
├── v1.6.0.md               # Version 1.6.0
├── v1.5.0.md               # Version 1.5.0
├── v1.4.1.md               # Version 1.4.1
├── v1.4.0.md               # Version 1.4.0
├── v1.2.2.md               # Version 1.2.2
└── v1.2.1.md               # Version 1.2.1
```

---

## 🔄 Release Workflow Checklist

When creating a new release:
1. Copy content from [`template.md`](./template.md) to a new file named `vX.Y.Z.md` (e.g., `v1.7.0.md`).
2. Fill in the release title, release date (`YYYY-MM-DD`), executive highlights, new features, and fixes in clear English.
3. Verify presentation in the desktop application by opening the **Release Notes Log** dialog in `SettingsPage` or `HelpPage`.
4. Keep `AGENTS.md` and `CHANGELOG.md` synchronized with the new release version.
