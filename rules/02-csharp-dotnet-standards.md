# 💻 Rule 02: C# 13 & .NET 10 Coding Standards

---

## 1. Target Frameworks & Modern Language Features

Lexora targets modern .NET:
- **Class Libraries (`Domain`, `Application`, `Infrastructure`)**: `net10.0`
- **Presentation (`WinUI 3`)**: `net10.0-windows10.0.26100.0`
- **Test Projects (`Lexora.Tests.Unit`, `Lexora.Tests.Integration`)**: `net10.0`
- **C# Language Version**: Modern C# 13 / 14 features enabled by default.

---

## 2. Mandatory Coding Conventions

### 2.1. Primary Constructors
Use primary constructors for services, repositories, and ViewModels where dependency injection is performed:

```csharp
// Correct
public class VocabularyService(
    IVocabularyRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<VocabularyService> logger) : IVocabularyService
{
    private readonly IVocabularyRepository _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<VocabularyService> _logger = logger;
}
```

### 2.2. File-Scoped Namespaces
Always use file-scoped namespaces to reduce indentation depth:

```csharp
// Correct
namespace Lexora.Application.Services;

public class ReviewService { ... }

// Prohibited
namespace Lexora.Application.Services
{
    public class ReviewService { ... }
}
```

### 2.3. Nullable Reference Types (`#nullable enable`)
All projects must maintain Nullable Reference Types enabled without suppressing warnings:
- Mark optional properties explicitly with `?`.
- Check and validate parameters at public service boundaries using `ArgumentNullException.ThrowIfNull(param)`.
- Avoid null-forgiving operator `!` unless accompanied by an explanatory code comment or in unit test setups where state is guaranteed.

### 2.4. Asynchronous Programming & Cancellation Tokens
- Every I/O bound, database, or long-running method must be asynchronous (`async Task` or `async Task<T>`).
- Always accept a `CancellationToken cancellationToken = default` parameter in service and repository methods.
- Pass the token down to all underlying EF Core methods (`ToListAsync(cancellationToken)`, `SaveChangesAsync(cancellationToken)`).
- Never call `.Result` or `.Wait()` on asynchronous tasks to prevent thread starvation and WinUI 3 UI deadlocks. Use `await`.

### 2.5. CommunityToolkit.Mvvm Conventions
In WinUI 3, ensure full WinRT / CsWinRT AOT compatibility and zero compiler warnings:
- ViewModels inherit from `ObservableObject`.
- Use C# 13 partial properties with `[ObservableProperty]`:
  ```csharp
  [ObservableProperty]
  public partial string SearchTerm { get; set; } = string.Empty;

  [ObservableProperty]
  public partial bool IsLoading { get; set; }
  ```
- Use `[RelayCommand]` on async methods:
  ```csharp
  [RelayCommand]
  private async Task LoadVocabularyAsync(CancellationToken cancellationToken = default)
  {
      ...
  }
  ```

---

## 3. Immutability & Record Usage

- For DTOs, query filter specifications, and statistical summary records, use immutable C# positional records:
  ```csharp
  public record VocabularySearchFilter(
      string? Query,
      CefrLevel? Level,
      Guid? TopicId,
      ExamType? Exam,
      int PageIndex = 0,
      int PageSize = 50);
  ```
- Use `IReadOnlyList<T>` for collection properties in DTOs to signal immutability to consumers.
