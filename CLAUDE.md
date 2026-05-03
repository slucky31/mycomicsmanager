# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

```bash
# Build
dotnet restore
dotnet build MyComicsManager.slnx

# Run locally (port 8080)
dotnet run --project Web/Web.csproj
dotnet watch run --project Web/Web.csproj    # Hot reload

# Test
dotnet test MyComicsManager.slnx
dotnet test MyComicsManager.slnx --collect:"XPlat Code Coverage"

# Code style
dotnet format --verify-no-changes            # Check compliance
dotnet format                                 # Auto-fix

# Database migrations
dotnet ef migrations add MigrationName --project Persistence
dotnet ef database update --project Persistence
```

**Environment Variables for Tests:**
- `ConnectionStrings__NeonConnectionUnitTests` - PostgreSQL connection for integration tests

## Architecture

This is a .NET 10 application following Clean Architecture with CQRS pattern:

```
Domain/          → Entities, value objects, errors (no dependencies)
Application/     → CQRS handlers, interfaces (depends on Domain only)
Persistence/     → EF Core, repositories, migrations (implements Application interfaces)
Web/             → Blazor Server UI with MudBlazor, Auth0 (composition root)
tests/           → Unit, integration, architecture, component tests
```

**Layer Rules (enforced by Architecture.Tests):**
- Domain has zero external dependencies
- Application depends only on Domain abstractions
- Persistence implements data access without leaking EF Core types upward
- Web consumes Application layer; business logic stays outside Razor components

## CQRS Pattern

Commands and queries in `Application/{Feature}/{Operation}/`:

```csharp
// Command example
public record CreateBookCommand(string Serie, string Title, string ISBN) : ICommand<Book>;

public sealed class CreateBookCommandHandler : ICommandHandler<CreateBookCommand, Book>
{
    public async Task<Result<Book>> Handle(CreateBookCommand request, CancellationToken ct)
    {
        // Business logic here
        return book;
    }
}
```

Handlers are auto-registered via Scrutor in `ApplicationDependencyInjection.cs`.

## Error Handling

Uses `Result<T>` pattern from `Domain/Primitives/` instead of exceptions for known error cases.

## Testing

| Project | Purpose | Tools |
|---------|---------|-------|
| Application.UnitTests | CQRS handlers | xUnit, NSubstitute, AwesomeAssertions |
| Domain.UnitTests | Entities, value objects | xUnit, AwesomeAssertions |
| Persistence.Integration.Tests | EF Core, PostgreSQL | xUnit + real database |
| Architecture.Tests | Layer boundary validation | NetArchTest |
| Web.Tests | Blazor components | bUnit |

**Test naming:** `MethodName_Should_DoExpectation_WhenCondition` — start with the name of the method under test, not the class name.

## Code Style

Enforced via `.editorconfig`:
- 4 spaces for C#, 2 spaces for XML/props files
- CRLF endings, UTF-8 with BOM
- Prefer `var` when type is clear
- Sorted `using` directives

## Commit & PR Guidelines

- Conventional commits: `type(scope): summary` (e.g., `feat(books): add ISBN validation`)
- PR titles must start with `feat` or `fix` (enforced by `.github/semantic.yml`)
- Never commit secrets; use environment variables

## Key Technologies

- PostgreSQL with EF Core (Neon connection)
- MudBlazor UI components
- Auth0 authentication
- Serilog logging
- FluentValidation
- Central package management via `Directory.Packages.props`

## Security & Configuration

- Store secrets (Auth0, PostgreSQL, Serilog sinks) in environment variables; never commit production credentials.
- Update `appsettings.Production.json` only with sanitized defaults; document overrides in the PR if configuration keys change.

## Code Quality Rules

### Accessibility (Blazor / MudBlazor)
- Every icon-only `MudIconButton` must have an `aria-label` attribute.
- Every icon-only `MudMenu` trigger must have an `AriaLabel` parameter (e.g., `"More actions"`).

### Defensive Parsing
- Never use `Convert.ToInt32(str, 16)` to parse hex strings — use `int.TryParse` with `NumberStyles.HexNumber` and fall back to a safe default on failure.

### Cognitive Complexity
- Method cognitive complexity must not exceed 15 (SonarQube). Extract nested blocks into private methods when needed.

### Testing
- Do not add a test whose assertion is already fully covered by an existing test (no redundant tests).

### CQRS Handlers — Side-effect Ordering
- In a `CommandHandler`, all domain validations must complete successfully **before** any file-system side-effect (`ILibraryLocalStorage.Move`, etc.) to prevent divergence between storage and database.

### View / ViewModel (Blazor)
- Never perform repeated collection traversals (e.g., `ReadingDates.MaxBy(...)`) inside `SortBy` lambdas or Razor templates. Pre-compute derived values in a ViewModel (e.g., `BookListItemViewModel.From(Book)`) and pass that ViewModel to components.

### CQRS Handlers — Input Validation
- Validate all input parameters (format, value range, nullity) at the start of `Handle` and return an appropriate domain error before any call to a repository or external service.

### Pagination — Deterministic Sort Order
- Every paginated query must end with a unique tie-breaker key (e.g., `.ThenBy(b => b.Id)`) so ordering is stable and pages contain no duplicates or gaps.

### Blazor — Error Handling in Load Methods
- A data-loading method must never leave the UI in an inconsistent state on failure. Always add an `else if (result.IsFailure)` branch that shows the error via Snackbar + `Log.Error`, and only update local state when `result.IsSuccess`.

### Blazor — Async Operations and Stale Results
- Before any async call that could become stale (infinite scroll, filter change), capture the current query parameters. On response, verify the parameters have not changed before applying results, and use `try/finally` to always reset loading flags.

### Blazor — JS Interop Side Effects
- Any C# state mutation tied to a `JS.InvokeVoidAsync` call must happen **after** the call returns, never before, so that a JS exception leaves the state consistent and allows a retry on the next render.

### IDisposable — Replace-and-Dispose
- When replacing an `IDisposable` field (e.g., `CancellationTokenSource`), always call `.Dispose()` on the previous instance before assigning the new one to avoid resource leaks.
