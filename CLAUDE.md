# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

```bash
# Build
dotnet restore
dotnet build MyComicsManager.sln

# Run locally (port 8080)
dotnet run --project Web/Web.csproj
dotnet watch run --project Web/Web.csproj    # Hot reload

# Test
dotnet test MyComicsManager.sln
dotnet test MyComicsManager.sln --collect:"XPlat Code Coverage"

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

**Test naming:** `Verb_Should_DoExpectation_WhenCondition`

## Code Style

Enforced via `.editorconfig`:
- 4 spaces for C#, 2 spaces for XML/props files
- CRLF endings, UTF-8 with BOM
- Prefer `var` when type is clear
- Sorted `using` directives
- Test names: `Handle_ShouldReturnSuccess_WhenInputValid`

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
