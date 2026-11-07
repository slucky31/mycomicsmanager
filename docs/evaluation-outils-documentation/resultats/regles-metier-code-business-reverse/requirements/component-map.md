# Carte des composants

Produite par `/re-discover` (commit analysé : `631720b92a58d61a0eb09343f7cbfc2f14a5f1ce`, 2026-10-17). À corriger à la main si besoin ; toutes les autres commandes la lisent.
**Dossier** = nom du dossier sous `<OUT>/03-capabilities/`.
**Préfixe** = 2 à 4 lettres utilisées dans les identifiants de règles, questions et anomalies. Ne jamais changer un préfixe une fois des identifiants créés.

MyComicsManager est un **monolithe** .NET 10 (Blazor Server) en architecture en couches avec CQRS maison. Il n'y a ni microservice, ni couche de services web distincte, ni application batch séparée : les traitements de fond tournent dans le processus Web (Hangfire + services hébergés). Les composants ci-dessous sont donc des **domaines fonctionnels** qui traversent les couches (Domain / Application / Persistence / Web), plus les composants transverses.

| Composant | Type | Chemin dans le dépôt | Dossier | Préfixe | Stack | Invoqué via | Points d'entrée (approx.) | Notes |
|---|---|---|---|---|---|---|---|---|
| Base de données et modèle de domaine | Base de données | `Domain/`, `Persistence/ApplicationDbContext.cs`, `Persistence/Migrations/` (54 fichiers) | `database` | DB | C# / EF Core / PostgreSQL | — | 12 tables (`Persistence/ApplicationDbContext.cs:15-38`) | À extraire avec `/re-domain` ; hiérarchie `Book` en TPT (ADR 0005) |
| Livres | Domaine fonctionnel | `Application/Books/`, `Web/Services/Book*.cs`, `Web/Components/Pages/Books/` | `livres` | BK | C# / CQRS / Blazor | UI + API | 14 handlers ; 5 pages ; 2 endpoints | Création, modification, déplacement, lecture, dates de lecture, scan ISBN |
| Bibliothèques | Domaine fonctionnel | `Application/Libraries/`, `Web/Components/Pages/Libraries/` | `bibliotheques` | LIB | C# / CQRS / Blazor | UI | 6 handlers ; 4 pages | Création/édition/suppression, résumés |
| Import de fichiers (cbz/cbr/zip/rar/pdf) | Traitement de fond | `Application/ImportJobs/`, `Web/Services/Import*.cs`, `Web/Services/FileWatcherService.cs`, `Persistence/Services/` | `import-fichiers` | IMP | C# / Hangfire / Tesseract | Surveillance de dossier + UI + file Hangfire | 6 handlers ; 1 service hébergé ; 1 page | Conversion d'archives, OCR ISBN (`IsbnOcr`) |
| Import par flux (Miniflux / Debrid-Link) | Traitement de fond + UI | `Application/FeedImports/`, `Web/Services/FeedImport*.cs`, `Web/Infrastructure/Miniflux*.cs`, `DebridLinkClient.cs`, `SsrfGuardHandler.cs`, `Domain/FeedImports/` | `import-flux` | FDI | C# / Hangfire | Tâche récurrente `feed-import-sync` + UI | 9 handlers ; 3 jobs ; 1 page | Désactivé par défaut (`FeedImport:Enabled=false`, `Web/appsettings.json`) |
| Recherche de métadonnées | Intégration | `Application/ComicInfoSearch/`, `Application/Books/Create` | `recherche-metadonnees` | SRC | C# / HttpClient | Appels depuis l'UI de création de livre | 5 services de catalogue | OpenLibrary, BnF, Google Books, Bédéthèque (via SerpApi), Bubble |
| Statistiques | Domaine fonctionnel | `Application/Statistics/`, `Persistence/Queries/StatisticsReadService.cs` | `statistiques` | STA | C# / Blazor | UI | 1 handler ; 1 page | |
| Utilisateurs et authentification | Transverse | `Application/Users/`, `Web/EndPoints/AccountsEndpoints.cs`, `Web/CustomAuthenticationStateProvider.cs` | `utilisateurs-auth` | USR | C# / Auth0 | UI + 2 endpoints | 1 handler ; 2 endpoints ; 1 page (`/login`) | Rôle `Admin` utilisé pour Hangfire et l'administration |
| Paramètres et administration | Transverse | `Application/Settings/`, `Web/Services/FeatureToggleService.cs`, `Web/Components/Pages/Admin/`, `Web/Components/Pages/UserSettings.razor` | `parametres-admin` | SET | C# / Blazor | UI | 1 handler ; 3 pages | Bascules de fonctionnalités (`Domain/Settings/FeatureToggle.cs:5-14`), niveaux de journalisation |
| Stockage de fichiers et services image | Bibliothèque / infrastructure | `Persistence/LocalStorage/`, `Persistence/Services/`, `Web/Infrastructure/CloudinaryHealthCheck.cs` | `stockage-images` | STO | C# / Cloudinary | Appelée par Livres, Import | — | Appelée par ≥ 2 composants : règles propres (RULES §6a) |
| Exploitation (santé, Hangfire, journaux) | Infrastructure | `Web/Program.cs`, `Web/Infrastructure/*HealthCheck.cs`, `Web/Configuration/` | `exploitation` | OPS | ASP.NET Core / Serilog | `/health`, `/hangfire` | 2 endpoints techniques | Démarrage refusé si une dépendance est injoignable (`Web/Program.cs:288`) |
| Scripts utilitaires | Scripts hors ligne | `scripts/ImportBooks`, `scripts/FillBedethequeUrls`, `scripts/FixCoverImages` | `scripts` | SCR | C# console | Exécution manuelle | 3 programmes | Probablement ponctuels ; portée à confirmer |
| Interface web (Blazor) | UI (unique) | `Web/Components/` (51 fichiers `.razor`) | `interface-web` | UI | Blazor Server / MudBlazor | Navigateur | 19 routes `@page` | Parcours de bout en bout : `/re-ui` |

Aucun composant « services web » ni « batch » séparé : les lignes génériques du modèle ont été retirées. Les 2 endpoints API (`/api/books/{bookId}/download`, `/api/books/{bookId:guid}/pages/{pageIndex:int}`, `Web/EndPoints/BooksEndpoints.cs:17-18`) sont couverts dans **Livres**.

## Systèmes externes (tiers)

| Contrepartie | ID d'intégration | Connecté via le composant | Protocole / format | Notes |
|---|---|---|---|---|
| Auth0 | INT-001 | Utilisateurs et authentification | OIDC | `Web/Program.cs:182-191` |
| Cloudinary | INT-002 | Stockage de fichiers et services image | API HTTPS (SDK) | Images de couverture ; `Web/appsettings.json` clé `Cloudinary` |
| OpenLibrary | INT-003 | Recherche de métadonnées | REST / JSON | `Web/Program.cs:84` |
| BnF (catalogue) | INT-004 | Recherche de métadonnées | HTTP | `Web/Program.cs:98` |
| Google Books | INT-005 | Recherche de métadonnées | REST / JSON | `Web/Program.cs:116` |
| Bédéthèque | INT-006 | Recherche de métadonnées | Pages HTML (scraping probable) | `Web/Program.cs:133` ; pause en cas de blocage Cloudflare (`CloudflarePauseHours`) |
| SerpApi | INT-007 | Recherche de métadonnées | REST / JSON | `Web/Program.cs:142` ; sert à interroger Bédéthèque |
| Bubble (Algolia) | INT-008 | Recherche de métadonnées | REST / JSON | `Web/Program.cs:156` |
| Miniflux | INT-009 | Import par flux | REST / JSON | `Web/Configuration/FeedImportConfiguration.cs:36` |
| Debrid-Link | INT-010 | Import par flux | REST / JSON | `Web/Infrastructure/DebridLinkClient.cs` |
| Hébergeurs de téléchargement (1fichier, rapidgator, turbobit…) | INT-011 | Import par flux | HTTP | Liste `FeedImport:AllowedDownloadHosts` ; garde SSRF (`Web/Infrastructure/SsrfGuardHandler.cs`) |
| Sites sources d'articles (planete-bd.org, zone-ebook.com) | INT-012 | Import par flux | HTML | `FeedImport:AllowedSourceHosts`, `Web/Infrastructure/ArticlePageFetcher.cs` |
| PostgreSQL (auto-hébergé) | — | Base de données | SQL | Infrastructure, pas un tiers métier |
