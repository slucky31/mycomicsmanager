# Repository Guidelines

## Project Structure & Module Organization
The solution `MyComicsManager.sln` follows a layered layout so features stay isolated and testable.
- `Domain/` contains core entities, value objects, and error definitions (no external dependencies).
- `Application/` hosts CQRS handlers, interfaces, and orchestrators that depend on the domain abstractions only.
- `Persistence/` provides EF Core infrastructure and PostgreSQL data access; migrations live under `Persistence/Migrations`.
- `Web/` delivers the MudBlazor UI, endpoints, and configuration files (`appsettings.*`, `wwwroot/`).
- `tests/` is split into unit, integration, architecture, and web test projects that mirror the runtime layers.

**Layer Rules (enforced by Architecture.Tests):**
- Domain has zero external dependencies.
- Application depends only on Domain abstractions.
- Persistence implements data access without leaking EF Core types upward.
- Web consumes Application layer; business logic stays outside Razor components.

## Build, Test, and Development Commands
- `dotnet restore` installs shared packages declared in `Directory.Packages.props`.
- `dotnet build MyComicsManager.sln` compiles every project with analyzers enabled by `Directory.Build.props`.
- `dotnet run --project Web/Web.csproj` starts the local site on port 8080 using `appsettings.Development.json`.
- `dotnet watch run --project Web/Web.csproj` enables hot reload while tweaking components or endpoints.
- `dotnet test MyComicsManager.sln` executes all xUnit suites; append `--collect:"XPlat Code Coverage"` when you need coverage artifacts.
- `dotnet ef migrations add MigrationName --project Persistence` adds a new EF Core migration.
- `dotnet ef database update --project Persistence` applies pending migrations.

**Environment Variables for Tests:**
- `ConnectionStrings__NeonConnectionUnitTests` — PostgreSQL connection string required by integration tests.

## CQRS Pattern

Commands and queries live in `Application/{Feature}/{Operation}/`:

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

## Coding Style & Naming Conventions
`.editorconfig` enforces space indentation, CRLF endings, and UTF-8 BOM encoding; builds fail if style regressions slip in.
- C# files use four spaces; project and props XML files use two.
- Prefer `var` when the type is clear, keep `using` directives sorted, and embrace object or collection initializers when analyzers suggest them.
- Follow standard .NET naming (PascalCase for types and constants, camelCase for locals and private fields).
- Run `dotnet format --verify-no-changes` before committing; run `dotnet format` to auto-fix stylistic nits.

## Testing Guidelines

| Project | Purpose | Tools |
|---------|---------|-------|
| Application.UnitTests | CQRS handlers | xUnit, NSubstitute, AwesomeAssertions |
| Domain.UnitTests | Entities, value objects | xUnit, AwesomeAssertions |
| Persistence.Integration.Tests | EF Core, PostgreSQL | xUnit + real database |
| Base.Integration.Tests | Shared integration helpers | xUnit + real database |
| Architecture.Tests | Layer boundary validation | NetArchTest |
| Web.Tests | Blazor components | bUnit |

- Mirror the existing naming style (`Verb_Should_DoExpectation_WhenCondition`) and prefer fixture builders to keep arrange blocks lean.
- Architecture rules in `tests/Architecture.Tests` guard project boundaries; extend them when adding new layers.

## Commit & Pull Request Guidelines
- Keep commits concise and conventional (`type(scope): summary`, e.g., `feat(books): add ISBN validation`).
- Ensure PR titles start with `feat` or `fix` to satisfy `.github/semantic.yml`; other metadata can live in the description.
- In the PR body, describe the change surface, link the tracked issue, and include screenshots or curl snippets for UI or API tweaks.

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
