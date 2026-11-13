# Vue 3 — Diagramme de composants du conteneur Web (Blazor Server)

**Portée** : l'application `Web` et les couches `Application`, `Domain`, `Persistence` qu'elle compose (racine de composition : `Web/Program.cs`, `AddApplication()`, `AddInfrastructure()`). Découpage par responsabilité, pas par dossier.

```mermaid
flowchart TB
    user["👤 Utilisateur"]
    auth0["Auth0"]

    subgraph web["Web — Blazor Server"]
        pages["Pages et composants Razor<br/>[MudBlazor, code-behind]<br/>Livres, Bibliothèques, Import, Flux, Statistiques, Admin"]
        auth["Authentification<br/>[CustomAuthenticationStateProvider, Auth0Configuration]<br/>Cookie de session, rôles"]
        ep["Endpoints minimaux<br/>[BooksEndpoints, AccountsEndpoints]<br/>Téléchargement et pages de BD, login/logout"]
        svc["Services de présentation<br/>[Web/Services, Web/Models]<br/>BooksService, LibrariesService, ImportService, ViewModels"]
        jobs["Adaptateurs de jobs<br/>[HangfireImportJobEnqueuer, FeedImport*Job,<br/>ImportOrchestrator, FileWatcherService]"]
        infra["Clients sortants protégés<br/>[MinifluxClient, DebridLinkClient, ArticlePageFetcher,<br/>FeedImportFileDownloader, SsrfGuardHandler]"]
        health["Observabilité<br/>[HealthChecks, LogLevelSwitches, Serilog]"]
    end

    subgraph app["Application — CQRS"]
        handlers["Handlers de commandes et requêtes<br/>[ICommandHandler / IQueryHandler, Scrutor]<br/>Books, Libraries, ImportJobs, FeedImports, Settings, Statistics, Users"]
        search["Recherche d'ISBN<br/>[ComicSearchService + BnF, Google Books,<br/>OpenLibrary, Bubble, Bedetheque]"]
        ports["Interfaces (ports)<br/>[IRepository, IUnitOfWork, I*ReadService,<br/>ILibraryLocalStorage…]"]
    end

    subgraph dom["Domain"]
        model["Entités, valeurs, erreurs, Result&lt;T&gt;<br/>Book, Library, User, ImportJob, FeedImportDecision"]
    end

    subgraph per["Persistence"]
        repos["Dépôts et services de lecture<br/>[EF Core : ApplicationDbContext, UnitOfWork,<br/>*Repository, *ReadService]"]
        storage["Stockage local et traitement de fichiers<br/>[LibraryLocalStorage, ArchiveExtractor, PdfImageExtractor,<br/>ImageProcessor, ComicArchiveBuilder, IsbnPageScanner]"]
        cld["CloudinaryService"]
    end

    db[("PostgreSQL")]
    fs[("Système de fichiers")]
    cloud["Cloudinary"]
    ext["Miniflux, Debrid-Link, sites,<br/>sources ISBN"]

    user -->|"Interagit (SignalR)"| pages
    user -->|"GET /api/books/…"| ep
    pages -->|"Vérifie l'identité"| auth
    auth -->|"OIDC"| auth0
    pages -->|"Appelle"| svc
    ep -->|"Envoie commandes/requêtes"| handlers
    svc -->|"Envoie commandes/requêtes"| handlers
    jobs -->|"Exécute les traitements"| handlers
    handlers -->|"Applique les règles"| model
    handlers -->|"Dépend de"| ports
    handlers -->|"Interroge"| search
    repos -.->|"Implémente"| ports
    storage -.->|"Implémente"| ports
    cld -.->|"Implémente"| ports
    infra -.->|"Implémente"| ports
    repos -->|"SQL (Npgsql)"| db
    storage -->|"Lit/écrit"| fs
    cld -->|"HTTPS"| cloud
    infra -->|"HTTPS"| ext
    search -->|"HTTPS"| ext
    jobs -->|"Enfile / dépile"| db
    health -->|"Sonde"| db
```

## Composants

| Composant | Responsabilité | Emplacement |
|---|---|---|
| Pages et composants Razor | UI, code-behind + CSS isolé (ADR-0011) ; pas de logique métier | `Web/Components/**` |
| Authentification | Cookie de session 7 jours, clés Data Protection en base, rôles dont `Admin` | `Web/Configuration/Auth0Configuration.cs`, `CustomAuthenticationStateProvider.cs` |
| Endpoints minimaux | `GET /api/books/{id}/download`, `/api/books/{id}/pages/{n}`, comptes ; sous autorisation | `Web/EndPoints/` |
| Services de présentation | Façade entre l'UI et les handlers, ViewModels pré-calculés | `Web/Services/`, `Web/Models/` |
| Adaptateurs de jobs | Mise en file Hangfire, orchestrateurs ouvrant un scope DI par job, surveillance du dossier d'import | `Web/Services/Hangfire*`, `*Orchestrator`, `FileWatcherService` |
| Clients sortants protégés | Clients HTTP vers Miniflux, Debrid-Link, sites, filtrés par liste d'hôtes (anti-SSRF, ADR-0015) | `Web/Infrastructure/` |
| Observabilité | `/health` (Npgsql, Cloudinary, Miniflux, Debrid-Link, dossier d'import), niveaux de log modifiables à chaud (ADR-0020) | `Web/Infrastructure/*HealthCheck.cs`, `LogLevelSwitches.cs` |
| Handlers CQRS | Cas d'usage, validation, `Result<T>` (ADR-0002, 0004) ; CQRS maison sans MediatR | `Application/*/` |
| Recherche d'ISBN | Interroge BnF → Google Books → OpenLibrary → Bubble → Bedetheque, garde le premier résultat ; chaque source désactivable | `Application/ComicInfoSearch/` |
| Domain | Entités, hiérarchie Book en TPT (ADR-0005), erreurs, `Result<T>` | `Domain/` |
| Dépôts / lecture | Écriture via `IRepository`/`IUnitOfWork`, lecture via `I*ReadService` | `Persistence/Repositories`, `Persistence/Queries` |
| Stockage et traitement de fichiers | Rangement, extraction d'archives et PDF, conversion WebP, CBZ, ComicInfo.xml, OCR | `Persistence/LocalStorage`, `Persistence/Services` |

## Règles de dépendances (observé, `tests/Architecture.Tests`)

`Web → Application → Domain` ; `Persistence → Application, Domain` ; `Domain` sans dépendance. Les types EF Core ne remontent pas.

## Écarts à noter

- Les clients Miniflux, Debrid-Link et la récupération d'articles vivent dans `Web/Infrastructure`, tandis que Cloudinary et les fichiers sont dans `Persistence`, et les services de recherche d'ISBN dans `Application/ComicInfoSearch`. Voir questions ouvertes.
- Le diagramme regroupe les adaptateurs « implémente » par flèche pointillée ; l'enregistrement DI exact est dans `Persistence/ProjectDependencyInjection.cs` et `Web/Program.cs`.
