# GitHub Copilot Instructions for MyComicsManager

## Repository Snapshot
- MyComicsManager is a .NET solution built around a strict layered architecture: `Domain` (entities, value objects, errors) → `Application` (CQRS handlers, orchestrators, abstractions) → `Persistence` (EF Core/PostgreSQL infrastructure) → `Web` (MudBlazor UI, endpoints, configuration).
- Test projects in `tests/` mirror the runtime layers: unit tests for `Domain` and `Application`, persistence and base integration suites, plus architecture and web-focused checks.
- Shared packages live in `Directory.Packages.props`; analyzers and formatting rules are enforced through `Directory.Build.props` and `.editorconfig`.

## Coding Style
- Respect `.editorconfig`: UTF-8 BOM, CRLF endings, four-space indentation for C#, two spaces for project/props XML.
- Prefer `var` when the type is obvious, keep `using` directives sorted, favor object/collection initializers, and avoid trailing whitespace.
- Follow .NET naming conventions; test names should read `Verb_Should_DoExpectation_WhenCondition` (e.g., `Handle_ShouldReturnSuccess_WhenInputValid`).
- Run or assume `dotnet format --verify-no-changes` passes after edits; keep generated code analyzer-clean.

## Architecture Guardrails
- Preserve layer boundaries: `Domain` stays free of infrastructure concerns; `Application` depends only on domain abstractions; `Persistence` implements data access details without leaking EF Core types upward; `Web` consumes the application layer and leaves business logic outside razor/components.
- When adding features, model business rules in `Domain`, expose commands/queries via `Application`, add EF configurations/migrations under `Persistence`, and surface UI/API wiring through `Web`.
- Uphold architecture tests—avoid cross-layer references or shortcuts that would fail `tests/Architecture.Tests`.

## Testing Expectations
- Unit tests use xUnit with AwesomeAssertions and NSubstitute; mirror existing fixture/builder patterns to keep arrange blocks concise.
- Persistence and base integration tests rely on PostgreSQL—ensure new EF work is covered there.
- Keep naming and structure consistent so `dotnet test MyComicsManager.sln` and optional coverage collection continue to succeed.

## Tooling & Commands
- Standard workflow: `dotnet restore`, `dotnet build MyComicsManager.sln`, `dotnet test MyComicsManager.sln`.
- Local runs use `dotnet run --project Web/Web.csproj`; prefer `dotnet watch run` while iterating on UI endpoints.

## Security & Configuration
- Never hard-code secrets (Auth0, PostgreSQL, Serilog sinks). Use environment variables or documented overrides in `appsettings.Development.json`.
- Production configs must stay sanitized; call out any new configuration keys explicitly.

## Collaboration Notes
- Keep commits conventional (`type(scope): summary`) and ensure PR titles begin with `feat` or `fix` to satisfy `.github/semantic.yml`.
- Summaries should describe surface area, link issues, and provide screenshots or curl snippets for UI/API changes.

Use these guardrails when generating code, docs, or tests so suggestions stay aligned with the project’s architecture and tooling.
