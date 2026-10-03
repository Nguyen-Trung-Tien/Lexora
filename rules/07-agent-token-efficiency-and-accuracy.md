# 🧠 Rule 07 — AI Agent Token Efficiency & Precision Engineering Protocol

> **Target Layer**: All AI Agent Workflows & Developer Tooling in Antigravity  
> **Key Objective**: Minimize token consumption (prompt + completion) while maximizing code accuracy, zero-hallucination execution, and strict adherence to Lexora's Clean Architecture.

---

## 1. Context & Token Economy (Tiết kiệm Token & Tối ưu Context)

### 1.1 Targeted Context Retrieval over Blind File Dumps
- **Targeted Slicing (`view_file`)**:
  - **NEVER** view entire files (>150 lines) when inspecting a specific method, property, or error location.
  - **ALWAYS** use `grep_search` first to find the exact line numbers of the symbol, then call `view_file` with a tight range (e.g. `StartLine = 120, EndLine = 170`).
  - Avoid re-reading files that were already examined in the recent context of the same session.

### 1.2 In-Place Patching over Full-File Rewrites
- **Zero Full-File Overwrites for Existing Code**:
  - **NEVER** use `write_to_file` with `Overwrite = true` on existing codebase files just to modify a few lines. This drains thousands of output tokens, risks omitting existing imports or helper methods, and creates unnecessary diff noise.
  - **ALWAYS** use `replace_file_content` for contiguous edits or `multi_replace_file_content` for non-contiguous changes.

### 1.3 Focused Terminal Execution & Output Paging
- **Targeted Test Execution**:
  - When actively developing or fixing a component, do **NOT** run the entire test suite (`dotnet test Lexora.slnx`) on every minor iteration.
  - **Filter by Class or Namespace**:
    ```powershell
    dotnet test --filter "FullyQualifiedName~VocabularyServiceTests"
    dotnet test --filter "FullyQualifiedName~Sm2SpacedRepetitionEngineTests"
    ```
  - Only execute the full test suite (`dotnet test Lexora.slnx --no-build`) during the final Quality Gate step.
- **Paging & Command Quietness**:
  - Keep terminal outputs concise (e.g. `git log -n 5`, `dotnet build -v q /clp:NoSummary`). Avoid dumping massive build logs or unpaged outputs into context.

### 1.4 High-Density, Concise Communication
- Do **not** echo or paraphrase the user's prompt back to them.
- Jump straight to the architectural rationale, file changes with clickable links `[FileName](file:///...)`, and verification results.
- Do **not** re-paste entire modified files in the chat response; explain only the core delta and non-obvious design choices.

---

## 2. Precision Engineering & Anti-Hallucination Invariants (Code Chính Xác)

### 2.1 The "Verify Before Implement" Protocol
- **Check Existing Abstractions**:
  - Before creating a new Entity, DTO, Repository, Enum, Converter, or Service, use `grep_search` to verify if an equivalent or reusable contract already exists in `Lexora.Domain` or `Lexora.Application`.
  - Prevent accidental duplication of models or divergence of DTO schemas.

### 2.2 Unidirectional Flow of New Features
Every new feature must follow the strict Clean Architecture cascade:
```
1. Lexora.Domain         -> Define Entity, Value Object, Domain Enum, or Domain Exception.
2. Lexora.Application    -> Define DTO, Service Interface, and Business Logic implementation.
3. Lexora.Infrastructure -> Configure EF Core Fluent API in OnModelCreating, Implement Repository/Service.
4. Lexora.Presentation   -> Implement ViewModel (ObservableProperty partial), Register DI, Bind View (ThemeResource).
```
- **Violations Are Strictly Prohibited**: Never query `LexoraDbContext` from ViewModels or inject Presentation dependencies into Application/Domain.

### 2.3 C# 13 / .NET 10 Idiomatic Accuracy
- **Observable Properties**: Use C# 13 partial properties with CommunityToolkit MVVM:
  ```csharp
  [ObservableProperty]
  public partial string SearchTerm { get; set; } = string.Empty;
  ```
  *(Prevents CsWinRT / AOT warnings and ensures 100% WinUI 3 markup compatibility).*
- **Primary Constructors**: Use primary constructors for dependency injection:
  ```csharp
  public class VocabularyService(IVocabularyRepository repository, IUnitOfWork unitOfWork) : IVocabularyService
  ```
- **CancellationToken Support**: All asynchronous I/O methods must propagate `CancellationToken cancellationToken = default`.

### 2.4 WinUI 3 & Fluent 2 UI Precision
- **Zero Hex Colors**: Never use `#RRGGBB` in XAML or code-behind. Use `{ThemeResource ...}` brushes.
- **Dialog Safety**:
  - Set `dialog.XamlRoot = this.XamlRoot;`.
  - Use `InfoBar` at the top for validation errors.
  - Insert `await Task.Yield();` between consecutive dialogs.
- **i18n Dual-Registration**: All user-facing text must be added to **both** `vi-VN` and `en-US` in `LocalizationService.cs`.

### 2.5 Verification Quality Gate
- After every code modification, verify:
  1. `dotnet build Lexora.slnx` -> Must produce **0 warnings and 0 errors**.
  2. Targeted test or full test suite passes completely.
