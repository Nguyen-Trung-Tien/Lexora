# 📋 Rule 08: Release Notes Log & User-Facing Version Documentation

> **Status**: MANDATORY INVARIANT  
> **Applies To**: `release-notes/`, `Lexora.Application.Interfaces.IReleaseNotesService`, `Lexora.Infrastructure.Services.ReleaseNotesService`, `Lexora.Presentation.Views.ReleaseNotesLogDialog`

---

## 1. Context & Purpose
In Lexora, technical commit histories reside in `CHANGELOG.md` and PR fragments in `changelog/`.
In contrast, the `release-notes/` folder houses **user-facing, easily understandable Markdown release logs**. Every learner using Lexora must be able to view and comprehend what has changed, what new learning features are available, and how their experience is improved without deciphering technical code jargon.

---

## 2. Directory Structure & File Naming Invariant

```
release-notes/
├── README.md               # Folder conventions and usage guide
├── template.md             # Canonical template for new version release notes
├── v1.6.1.md               # Current release notes log
├── v1.6.0.md
└── ...
```

- Every new release **MUST** have a corresponding `vX.Y.Z.md` file located directly under `release-notes/`.
- File names strictly follow `v<Major>.<Minor>.<Patch>.md`.

---

## 3. Writing Style Invariant: Simple, Concise & Markdown-Formatted

Release notes must be written in simple, clear, concise markdown language:
1. **Focus on Learner Value**: Describe features from the perspective of an English exam learner (TOEIC, IELTS, VSTEP). Explain *what* the feature does and *how* to use it.
2. **Standard Markdown Structure**:
   - Level 1 Heading: `# Lexora vX.Y.Z — [Tiêu đề bản phát hành / Release Title]`
   - Date metadata: `> Ngày phát hành: YYYY-MM-DD | Phiên bản: X.Y.Z`
   - Highlights: `## 🌟 Điểm nổi bật (Highlights)` (1-2 sentences)
   - New Features: `## 🚀 Tính năng mới (New Features)` (bullet points with bold title)
   - Improvements: `## ⚡ Cải tiến & Trải nghiệm (Improvements & UX)`
   - Bug Fixes: `## 🛠️ Sửa lỗi & Độ ổn định (Bug Fixes & Stability)`
   - Bilingual Summary: `## 🇬🇧 English Summary`
3. **No Raw Code Diffs**: Never output internal class names (e.g. `VocabularyRepository`), SQL transaction logs, or method signatures in release notes.

---

## 4. In-App Viewer Integration

1. **Content Copy**: `Lexora.Presentation.csproj` copies `release-notes\**\*` to the target and publish directories (`CopyToOutputDirectory="PreserveNewest"`).
2. **Service Contract**: `IReleaseNotesService` and `ReleaseNotesService` provide offline version enumerations with descending SemVer sorting.
3. **Fluent 2 Native Dialog**: `ReleaseNotesLogDialog` renders release notes using `ReleaseNotesRenderer` with zero hardcoded colors, Segoe Fluent Icons, and real-time Dark/Light theme adaptability.
4. **Accessible Entrypoints**:
   - `SettingsPage`: Button "Nhật ký phát hành" under Software Updates.
   - `HelpPage`: Button "Nhật ký phát hành" in the Help Center Hero banner.

---

## 5. Developer & AI Agent Quality Gate Checklist
Before declaring a version update or bump complete:
- [ ] `release-notes/vX.Y.Z.md` created matching `release-notes/template.md`.
- [ ] Content verified: simple, concise, user-friendly markdown.
- [ ] In-app `ReleaseNotesLogDialog` tested and displaying the new version notes properly.
- [ ] Solution compiles with `0 Warning(s)`, `0 Error(s)`.

