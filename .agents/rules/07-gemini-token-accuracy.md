# 🧠 Antigravity Gemini Token Optimization & Precision Protocol

This rule governs all AI coding sessions within Lexora.

## 1. Token Economy (Tiết kiệm Token)
- **Targeted Code Inspection**: ALWAYS use `grep_search` before `view_file`. When calling `view_file`, specify `StartLine` and `EndLine` to read only 30-80 relevant lines. Never read files >150 lines in full.
- **In-Place Diff Edits**: NEVER use `write_to_file` to overwrite existing files for small edits. Always use `replace_file_content` or `multi_replace_file_content`.
- **Targeted Testing**: Test single classes during development (`dotnet test --filter "FullyQualifiedName~<ClassName>"`). Run full suite only at final verification.
- **Concise Responses**: Be direct, do not echo user requests, avoid pasting entire files, and provide clickable file links.

## 2. Functional Precision (Code Chính Xác)
- **Verify Before Inventing**: Grep `Lexora.Domain` and `Lexora.Application` to check existing entities/DTOs/interfaces before creating new types.
- **Strict Clean Architecture**: Unidirectional flow: `Domain` -> `Application` -> `Infrastructure` -> `Presentation`. No EF Core in Presentation/ViewModels.
- **C# 13 & MVVM**: Use C# 13 partial properties for `[ObservableProperty]`, primary constructors, and `CancellationToken` in async methods.
- **Fluent 2 UX**: Zero hardcoded hex colors (`{ThemeResource ...}` brushes only). WinUI dialogs require `XamlRoot` and `await Task.Yield()`.
- **Bilingual i18n**: All UI strings must be added to both `vi-VN` and `en-US` dictionaries in `LocalizationService.cs`.
- **Quality Gate**: Execute `dotnet build Lexora.slnx` (0 warnings, 0 errors) and ensure all automated tests pass before concluding.
