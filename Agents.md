# Repository Guidelines

## Project Structure & Module Organization
The solution `MyComicsManager.sln` follows a layered layout so features stay isolated and testable.
- `Domain/` contains core entities, value objects, and error definitions.
- `Application/` hosts CQRS handlers, interfaces, and orchestrators that depend on the domain abstractions.
- `Persistence/` provides EF Core infrastructure and PostgreSQL data access; migrations live under `Persistence/Migrations`.
- `Web/` delivers the MudBlazor UI, endpoints, and configuration files (`appsettings.*`, `wwwroot/`).
- `tests/` is split into unit, integration, architecture, and web test projects that mirror the runtime layers.

## Build, Test, and Development Commands
- `dotnet restore` installs shared packages declared in `Directory.Packages.props`.
- `dotnet build MyComicsManager.sln` compiles every project with analyzers enabled by `Directory.Build.props`.
- `dotnet run --project Web/Web.csproj` starts the local site and API using `appsettings.Development.json`.
- `dotnet watch run --project Web/Web.csproj` enables hot reload while tweaking components or endpoints.
- `dotnet test MyComicsManager.sln` executes all xUnit suites; append `--collect:"XPlat Code Coverage"` when you need coverage artifacts.

## Coding Style & Naming Conventions
`.editorconfig` enforces space indentation, CRLF endings, and UTF-8 BOM encoding; builds fail if style regressions slip in.
- C# files use four spaces; project and props XML files use two.
- Prefer `var` when the type is clear, keep `using` directives sorted, and embrace object or collection initializers when analyzers suggest them.
- Follow standard .NET naming (PascalCase for types and constants, camelCase for locals and private fields) and keep test names descriptive (`Handle_ShouldReturnSuccess_WhenInputValid`).
- Run `dotnet format --verify-no-changes` before committing to auto-fix stylistic nits.

## Testing Guidelines
- Unit coverage lives under `tests/Application.UnitTests` and `tests/Domain.UnitTests` using xUnit, AwesomeAssertions, and NSubstitute.
- `tests/Persistence.Integration.Tests` and `tests/Base.Integration.Tests` hit EF Core and PostgreSQL paths; export `ConnectionStrings__Default` or update `appsettings.Development.json` before running.
- Architecture rules in `tests/Architecture.Tests` guard project boundaries; extend them when adding new layers.
- Mirror the existing naming style (`Verb_Should_DoExpectation_WhenCondition`) and prefer fixture builders to keep arrange blocks lean.

## Commit & Pull Request Guidelines
- Keep commits concise and conventional (`type(scope): summary`), as seen in history (`fix(deps): ...`, `chore(release): ...`).
- Ensure PR titles start with `feat` or `fix` to satisfy `.github/semantic.yml`; other metadata can live in the description.
- In the PR body, describe the change surface, link the tracked issue, and include screenshots or curl snippets for UI or API tweaks.

## Security & Configuration Tips
- Store secrets (Auth0, PostgreSQL, Serilog sinks) in environment variables; never commit production credentials.
- Update `appsettings.Production.json` only with sanitized defaults and document overrides in the PR if configuration keys change.
