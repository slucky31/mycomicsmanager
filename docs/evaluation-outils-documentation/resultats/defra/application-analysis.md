<!-- Fichiers d'entrée traités (lus intégralement sauf mention « partiel ») :
- src/mycomicsmanager/MyComicsManager.slnx
- src/mycomicsmanager/Directory.Packages.props
- src/mycomicsmanager/CLAUDE.md (fourni automatiquement par l'environnement lors de la lecture ; non utilisé comme source de faits métier)
- src/mycomicsmanager/Web/Web.csproj
- src/mycomicsmanager/Web/appsettings.json
- src/mycomicsmanager/Web/Program.cs
- src/mycomicsmanager/Web/CustomAuthenticationStateProvider.cs
- src/mycomicsmanager/Web/Configuration/Auth0Configuration.cs
- src/mycomicsmanager/Web/Configuration/FeedImportConfiguration.cs
- src/mycomicsmanager/Web/EndPoints/AccountsEndpoints.cs
- src/mycomicsmanager/Web/EndPoints/BooksEndpoints.cs
- src/mycomicsmanager/Web/Infrastructure/HangfireAuthorizationFilter.cs
- src/mycomicsmanager/Web/Infrastructure/SsrfGuardHandler.cs
- src/mycomicsmanager/Web/Infrastructure/MinifluxClient.cs (partiel)
- src/mycomicsmanager/Web/Infrastructure/DebridLinkClient.cs (partiel)
- src/mycomicsmanager/Web/Services/FeedImportSyncJob.cs
- src/mycomicsmanager/Web/Services/FeedImportDownloadJob.cs
- src/mycomicsmanager/Web/Services/FeedImportNotifier.cs
- src/mycomicsmanager/Web/Services/FileWatcherService.cs
- src/mycomicsmanager/Web/Services/ImportJobHandlers.cs
- src/mycomicsmanager/Web/Services/ImportOrchestrator.cs
- src/mycomicsmanager/Web/Services/HangfireImportJobEnqueuer.cs
- src/mycomicsmanager/Web/Services/BookMoveWorkflow.cs
- src/mycomicsmanager/Web/Components/Layout/NavBar.razor
- src/mycomicsmanager/Web/Components/Pages/Admin/LogLevels.razor.cs
- src/mycomicsmanager/Web/Components/Pages/Books/AddBookScan.razor.cs
- (fichiers Web/Components/**/*.razor et *.razor.cs : parcourus uniquement par recherche de motifs @page / Authorize / AuthorizeView / Admin, non lus en détail)
- src/mycomicsmanager/Domain/Books/Book.cs, DigitalBook.cs, PhysicalBook.cs, BookConstants.cs, ReadingDate.cs, BookMetadata.cs, ReaderSettings.cs, IsbnScanState.cs, IsbnBedethequeUrl.cs, BooksError.cs
- src/mycomicsmanager/Domain/Libraries/Library.cs, LibraryConstants.cs, LibraryBookType.cs, BookSortOrder.cs, LibrariesError.cs
- src/mycomicsmanager/Domain/Users/User.cs
- src/mycomicsmanager/Domain/ImportJobs/ImportJob.cs, ImportJobStatus.cs, ImportJobConstants.cs, ImportJobError.cs
- src/mycomicsmanager/Domain/FeedImports/FeedImportDecision.cs, FeedImportDecisionStatus.cs, FeedImportConstants.cs, DownloadCandidate.cs, DownloadMirror.cs, FeedImportDecisionEvent.cs, FeedImportArbitrationKind.cs, FeedImportDecidedBy.cs, ComicTitleParser.cs, SerieMatcher.cs, FeedImportError.cs
- src/mycomicsmanager/Domain/Settings/FeatureToggle.cs, FeatureToggleOverride.cs
- src/mycomicsmanager/Application/Books/Create/CreateBookCommandHandler.cs, Update/UpdateBookCommandHandler.cs, Delete/DeleteBookCommandHandler.cs, Move/MoveBookCommandHandler.cs, AddReadingDate/AddReadingDateCommandHandler.cs, MoveTargets/GetBookMoveTargetsQueryHandler.cs, List/GetPagedBooksQueryHandler.cs, Read/GetBookPageQueryHandler.cs, Read/GetBookReaderInfoQueryHandler.cs, ReadingProgress/UpdateReadingProgressCommandHandler.cs
- src/mycomicsmanager/Application/Books/IsbnScan/BookIsbnScan.cs, IsbnScanOutcome.cs, IsbnOcrSettings.cs, ScanBookIsbnCommandHandler.cs, ScanLibraryIsbnsCommandHandler.cs, StartLibraryIsbnScanCommandHandler.cs
- src/mycomicsmanager/Application/Libraries/Create|Update|Delete/*CommandHandler.cs
- src/mycomicsmanager/Application/ImportJobs/ImportSettings.cs, Create/CreateImportJobCommandHandler.cs, Process/ProcessImportJobCommandHandler.cs, ForceFail/ForceFailImportJobCommandHandler.cs, Delete/DeleteImportJobCommandHandler.cs
- src/mycomicsmanager/Application/FeedImports/FeedImportSettings.cs, Sync/SyncFeedImportsCommandHandler.cs, Analyze/AnalyzeFeedImportDecisionCommandHandler.cs, Analysis/DuplicateBookFinder.cs, Analysis/DownloadLinkGrouper.cs, Analysis/FeedImportAnalysisRules.cs, Analysis/Sites/PlaneteBdLinkExtractor.cs, Analysis/Sites/ZoneEbookLinkExtractor.cs, Download/DownloadFeedImportDecisionCommandHandler.cs, Arbitrate/ResolveFeedImportArbitrationCommandHandler.cs, Manage/ManageFeedImportDecisionCommandHandler.cs, Delete/DeleteDownloadedFeedImportDecisionsCommandHandler.cs
- src/mycomicsmanager/Application/ComicInfoSearch/ComicSearchService.cs, BedethequeService.cs, BedethequeCircuit.cs, BnfCatalogueService.cs (partiel), BubbleService.cs (partiel)
- src/mycomicsmanager/Application/Helpers/IsbnHelper.cs, FileNameIsbnExtractor.cs, TextIsbnExtractor.cs, IsbnScanPages.cs
- src/mycomicsmanager/Application/Settings/FeatureToggles.cs, FeatureToggleDefaults.cs, SetFeatureToggle/SetFeatureToggleCommandHandler.cs
- src/mycomicsmanager/Application/Statistics/Get/GetStatisticsQueryHandler.cs, StatisticsDto.cs
- src/mycomicsmanager/Application/Users/Create/CreateUserCommandHandler.cs
- src/mycomicsmanager/Persistence/ApplicationDbContext.cs
- src/mycomicsmanager/Persistence/Queries/BookReadService.cs, StatisticsReadService.cs
- src/mycomicsmanager/Persistence/LocalStorage/ImportDirectoryStorage.cs
- src/mycomicsmanager/Persistence/Services/ImageProcessorService.cs, ComicInfoXmlService.cs, ComicPageReaderService.cs, IsbnPageScanner.cs, CloudinaryService.cs
- src/mycomicsmanager/scripts/ImportBooks/Program.cs, scripts/FillBedethequeUrls/Program.cs, scripts/FixCoverImages/Program.cs
- Les autres fichiers (migrations EF Core, DTO/commandes/requêtes simples, interfaces, composants Razor, extracteurs de liens génériques, dépôts) ont été repérés par glob mais NON lus en détail : leur contenu n'est pas décrit ici au-delà de ce qui se déduit des fichiers lus.
-->

# Analyse applicative — MyComicsManager (MCM)

> Remarque de périmètre : le code source ne contient ni projet VB.NET ni solution `.sln` ; la solution est au format `.slnx` et les projets sont des `.csproj` (.NET 10). Tous les constats ci-dessous sont tirés du code lu (voir la liste de provenance en tête de fichier). Ce qui n'a pas été lu est signalé comme tel et n'est pas supposé.

## 1. Vue d'ensemble de l'application

- **Objectif :** gérer des collections de bandes dessinées (livres physiques recensés par ISBN et livres numériques CBZ importés depuis des fichiers), avec lecture en ligne, suivi des lectures/notes, recherche automatique de métadonnées et import automatisé depuis des flux RSS (Miniflux).
- **Pile technologique :** C# / .NET 10, Blazor Server (rendu interactif serveur) avec MudBlazor, architecture propre (Clean Architecture) avec CQRS maison (`ICommand`, `IQuery`, handlers enregistrés par Scrutor), motif `Result<T>` / `TError` à la place des exceptions, EF Core + PostgreSQL, Hangfire (PostgreSQL) pour les tâches de fond, Serilog.
- **Version du framework :** `net10.0` (`Web/Web.csproj`), EF Core 10.0.13, Npgsql EF 10.0.3, MudBlazor 9.11.0, Hangfire 1.8.25 (`Directory.Packages.props`).
- **Structure de la solution** (`MyComicsManager.slnx`) :
  - `Domain` : entités, objets valeur, erreurs, règles pures (aucune dépendance externe).
  - `Application` : handlers CQRS, interfaces, services de recherche de métadonnées, analyse des flux, paramètres.
  - `Persistence` : `ApplicationDbContext`, dépôts, services de lecture, stockage local (bibliothèques, dossier d'import, espace temporaire), traitement d'images/archives/OCR, Cloudinary.
  - `Web` : racine de composition, composants Razor, endpoints minimaux, services de fond (Hangfire, `FileWatcherService`), clients HTTP (Miniflux, Debrid-Link), health checks.
  - `scripts/FillBedethequeUrls`, `scripts/FixCoverImages`, `scripts/ImportBooks` : outils ponctuels en ligne de commande (seul `FixCoverImages` est référencé dans le `.slnx`).
  - Projets de tests référencés dans le `.slnx` : `Application.UnitTests`, `Architecture.Tests`, `Base.Integration.Tests`, `Domain.UnitTests`, `Persistence.Tests`, `Web.Tests` (non lus).
- **Dépendances externes :** Auth0 (`Auth0.AspNetCore.Authentication`), Hangfire (+ `Hangfire.PostgreSql`), MudBlazor, Serilog, FluentValidation, Ardalis.GuardClauses, Scrutor, CloudinaryDotNet, HtmlAgilityPack, SharpCompress, SkiaSharp, PdfPig, AspNetCore.HealthChecks.*, `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`. Aucune assembly interne Defra n'est présente (`Directory.Packages.props`).
- **Résumé de la configuration** (`Web/appsettings.json`, `Web/Program.cs`) :
  - Authentification : Auth0 (OpenID Connect, portées `openid profile email`) ; cookie d'authentification de 7 jours avec expiration glissante ; clés Data Protection stockées en base.
  - Chaîne de connexion `ConnectionStrings:DefaultConnection` (PostgreSQL).
  - `LocalStorage:RootPath` (racine des bibliothèques numériques, doit exister et être absolue).
  - `Import` : `ImportDirectory`, `TempDirectory`, `ImageTargetWidth` 1400, `PollingIntervalSeconds` 30, extensions `.cbz .cbr .zip .rar .pdf`, `MaxFileSizeMb` (1024 dans le fichier de configuration, 500 par défaut dans la classe).
  - `IsbnOcr` (Tesseract, langue `eng`, 5 pages à chaque extrémité, délai 30 s par page).
  - Sources de métadonnées : `OpenLibrary`, `BnfCatalogue`, `GoogleBooks`, `Bedetheque` (via SerpApi, pause Cloudflare 24 h), `Bubble` (Algolia, clé API requise).
  - `FeedImport` (désactivé par défaut, intervalle 30 min, bibliothèque cible « À trier », hôtes sources `planete-bd.org`, `zone-ebook.com`, hôtes de téléchargement autorisés), `Miniflux` (catégorie « BD »), `DebridLink` (priorité des hébergeurs).
  - `Cloudinary` (couvertures).
  - Au démarrage : migrations EF appliquées automatiquement, vérification de santé (arrêt si un composant est `Unhealthy`), chargement des bascules de fonctionnalités, planification de la synchronisation Miniflux.
  - Hangfire : 1 worker (séquentiel, prévu pour Raspberry Pi 4), files `import` et `default`, tableau de bord sur `/hangfire`.

## 2. Rôles utilisateurs et contrôle d'accès

| Rôle | Permissions / Accès | Source |
|------|---------------------|--------|
| Utilisateur authentifié | Accès à toutes les pages (`@attribute [Authorize]` dans `Pages/_Imports.razor`) ; ne voit et ne modifie que ses propres bibliothèques, livres, imports et décisions de flux (contrôle `library.UserId == request.UserId`, sinon « non trouvé ») | `Web/Components/Pages/_Imports.razor`, handlers `Application/**` |
| Admin | En plus : page `/admin/log-levels`, carte « Administration » (bascules de fonctionnalités, niveaux de journalisation) dans `/settings`, tableau de bord Hangfire `/hangfire` | `Web/Components/Pages/Admin/LogLevels.razor`, `Web/Components/Pages/UserSettings.razor`, `Web/Infrastructure/HangfireAuthorizationFilter.cs` |
| Anonyme | Seulement la page `/login` et les endpoints `/Account/Login`, `/Account/Logout`, `/health` | `Web/Components/Pages/Login.razor`, `Web/EndPoints/AccountsEndpoints.cs`, `Web/Program.cs` |
| Utilisateur du flux (compte technique) | Les décisions créées par la synchronisation de fond sont rattachées à l'utilisateur dont l'e-mail est `FeedImport:UserEmail` | `Web/Services/FeedImportSyncJob.cs`, `Application/FeedImports/FeedImportSettings.cs` |

- **Mécanisme d'authentification :** Auth0 (OIDC) via `AddAuth0WebAppAuthentication`, session par cookie (7 jours, glissante). Un utilisateur applicatif est créé automatiquement à la première connexion (provisionnement par `sub` Auth0 et e-mail) dans `CustomAuthenticationStateProvider`.
- **Approche d'autorisation :** attributs `[Authorize]` / `Roles="Admin"` sur les pages Razor, `AuthorizeView Roles="Admin"`, `RequireAuthorization()` sur les endpoints, filtre Hangfire exigeant le rôle `Admin`. L'isolation des données par utilisateur est faite dans les handlers (propriété de la bibliothèque). L'origine du rôle `Admin` (revendication Auth0) n'est pas définie dans le code lu.

## 3. Fonctionnalités et capacités

#### Gestion des bibliothèques
- **Description :** création, modification (couleur, icône, nom, ordre de tri par défaut), suppression de bibliothèques de type `Physical` ou `Digital`. Pour une bibliothèque numérique, un dossier de stockage et un dossier d'import sont créés/renommés/supprimés.
- **Pages/écrans :** `/` et `/libraries/list`, `/libraries/add`, `/libraries/{id}/edit`, `/libraries/{id}` (vues liste et couvertures).
- **Fichiers sources :** `Application/Libraries/*`, `Domain/Libraries/*`, `Web/Components/Pages/Libraries/*`, `Web/Services/LibrariesService.cs` (non lu).

#### Gestion des livres physiques et numériques
- **Description :** ajout manuel, par formulaire ou par scan de code-barres (caméra, `js/isbnScanner.js`), recherche de détails par ISBN, modification, suppression, déplacement entre bibliothèques de même type, recherche texte et pagination dans une bibliothèque, téléchargement du CBZ.
- **Pages/écrans :** `/books/add/manual`, `/books/add/form`, `/books/add/scan`, `/books/{id}`, `/books/{id}/edit`, `/books/{id}/find-details`, boîte de dialogue de déplacement.
- **Fichiers sources :** `Application/Books/*`, `Domain/Books/*`, `Web/EndPoints/BooksEndpoints.cs`, `Web/Components/Pages/Books/*`, `Web/Components/Pages/Dialogs/MoveBookDialog.razor`.

#### Suivi des lectures et notes
- **Description :** chaque lecture est une date de lecture avec une note de 1 à 5 ; suppression possible ; la dernière lecture et la dernière note sont affichées dans les listes.
- **Fichiers sources :** `Application/Books/AddReadingDate/*`, `DeleteReadingDate/*`, `Domain/Books/ReadingDate.cs`, `Persistence/Queries/BookReadService.cs`.

#### Lecteur de bandes dessinées en ligne
- **Description :** lecture page par page d'un CBZ ; pages décompressées en mémoire à la demande ; progression (dernière page lue) enregistrée ; réglages d'affichage par livre (mise en page, défilement, sens de lecture).
- **Pages/écrans :** `/books/{id}/read`, endpoint `/api/books/{bookId}/pages/{pageIndex}`.
- **Fichiers sources :** `Application/Books/Read/*`, `ReadingProgress/*`, `ReaderDisplay/*`, `Persistence/Services/ComicPageReaderService.cs`, `Web/Components/Pages/Books/BookReader.razor(.cs)`.

#### Import de fichiers numériques
- **Description :** dépôt d'un fichier dans le dossier d'import d'une bibliothèque numérique → tâche d'import → extraction, conversion WebP, recherche de métadonnées, envoi de la couverture, construction d'un CBZ final.
- **Pages/écrans :** `/import` (suivi des tâches, marquage en échec, suppression).
- **Fichiers sources :** `Application/ImportJobs/*`, `Web/Services/FileWatcherService.cs`, `Persistence/Services/*`, `Persistence/LocalStorage/*`.

#### Recherche de métadonnées par ISBN
- **Description :** interrogation successive de la BnF, Google Books, Open Library, Bubble puis Bédéthèque ; téléversement de la couverture sur Cloudinary.
- **Fichiers sources :** `Application/ComicInfoSearch/*`.

#### Lecture de l'ISBN par OCR
- **Description :** lecture par Tesseract de l'ISBN imprimé sur les pages des livres numériques sans ISBN ; affectation automatique, candidats multiples ou « non trouvé » ; lancement par bibliothèque en tâche de fond.
- **Fichiers sources :** `Application/Books/IsbnScan/*`, `Persistence/Services/IsbnPageScanner.cs`, `Application/Helpers/TextIsbnExtractor.cs`, `IsbnScanPages.cs`.

#### Import automatique depuis les flux (Miniflux / Debrid-Link)
- **Description :** synchronisation des articles favoris Miniflux d'une catégorie, analyse de la page de l'article, extraction et regroupement des liens de téléchargement, détection de doublons, arbitrage manuel, téléchargement via Debrid-Link, dépôt dans la bibliothèque « À trier ».
- **Pages/écrans :** `/feed-imports` (avec pastille d'arbitrages en attente dans la navigation).
- **Fichiers sources :** `Application/FeedImports/*`, `Domain/FeedImports/*`, `Web/Services/FeedImport*.cs`, `Web/Infrastructure/*Client.cs`, `Web/Components/Pages/FeedImportsPage.razor(.cs)`.

#### Statistiques
- **Description :** totaux (livres, séries, bibliothèques, pages, stockage numérique), livres lus/non lus, nombre de lectures, note moyenne, lectures par mois sur 12 mois, globalement ou par bibliothèque.
- **Pages/écrans :** `/statistics`.
- **Fichiers sources :** `Application/Statistics/*`, `Persistence/Queries/StatisticsReadService.cs`.

#### Administration (bascules de fonctionnalités, journalisation)
- **Description :** activation/désactivation à chaud de `FeedImport`, `IsbnOcr`, `Bedetheque`, `Bnf`, `GoogleBooks`, `OpenLibrary`, `Bubble` ; réglage à l'exécution des niveaux de journalisation par logger.
- **Pages/écrans :** `/settings` (section Administration), `/admin/log-levels`.
- **Fichiers sources :** `Application/Settings/*`, `Domain/Settings/*`, `Web/Components/Pages/Admin/*`, `Web/Infrastructure/LogLevelSwitches.cs` (non lu).

#### Supervision
- **Description :** endpoint `/health` (PostgreSQL, dossier d'import, Cloudinary, Debrid-Link, Miniflux, état de l'application), tableau de bord Hangfire.
- **Fichiers sources :** `Web/Program.cs`, `Web/Infrastructure/*HealthCheck.cs` (non lus).

#### Outils ponctuels (scripts)
- **Description :** `ImportBooks` (import historique d'un export JSON de type MongoDB vers la bibliothèque physique « Read Books »), `FillBedethequeUrls` (remplissage du cache ISBN → URL Bédéthèque via Serper), `FixCoverImages` (re-téléversement des couvertures vers Cloudinary).
- **Fichiers sources :** `scripts/*/Program.cs`.

## 4. Workflows et comportements

#### Connexion et provisionnement de l'utilisateur
- **Type :** orienté utilisateur
- **Déclencheur :** accès à `/login` ou `/Account/Login`.
- **Étapes :**
  1. `/Account/Login` lance un défi Auth0 avec redirection (`Web/EndPoints/AccountsEndpoints.cs`) ; le schéma HTTPS est forcé pour le déchargement SSL.
  2. Au premier appel d'état d'authentification d'un circuit, l'utilisateur est recherché par `sub` ; s'il est inconnu et qu'un e-mail existe, un `User` est créé (`Web/CustomAuthenticationStateProvider.cs`).
  3. Le provisionnement est partagé entre appels concurrents et relancé en cas d'échec.
- **Transitions d'état :** aucune.
- **Fichiers sources :** `Web/CustomAuthenticationStateProvider.cs`, `Web/EndPoints/AccountsEndpoints.cs`.

#### Ajout d'un livre physique
- **Type :** orienté utilisateur
- **Déclencheur :** scan de code-barres, saisie manuelle ou formulaire.
- **Étapes :**
  1. Le scan JS renvoie un ISBN ; il est validé (`IsbnHelper.IsValidISBN`), sinon message d'erreur (`AddBookScan.razor.cs`).
  2. Recherche des détails par ISBN (voir « Recherche de métadonnées »).
  3. `CreateBookCommandHandler` : champs obligatoires, ISBN valide, note 0–5, refus de doublon d'ISBN, bibliothèque existante et appartenant à l'utilisateur, création de `PhysicalBook`, contrôle de compatibilité de type, ajout d'une date de lecture du jour si la note est > 0, enregistrement.
- **Fichiers sources :** `Application/Books/Create/CreateBookCommandHandler.cs`, `Domain/Books/PhysicalBook.cs`.

#### Déplacement d'un livre
- **Type :** orienté utilisateur
- **Déclencheur :** action « Déplacer » ; `BookMoveWorkflow` affiche la boîte de dialogue avec les bibliothèques de même type et suggère celle qui contient le plus de livres de la même série.
- **Étapes :**
  1. Chargement des cibles (`GetBookMoveTargetsQueryHandler`), comptage des livres de même série (comparaison normalisée par `SerieMatcher.Normalize`).
  2. `MoveBookCommandHandler` : vérifie propriété du livre et de la cible, type de bibliothèque identique, livre pas déjà dans la cible.
  3. Livre numérique : déplacement du fichier vers le dossier de la bibliothèque cible, mise à jour en base ; en cas d'échec, le fichier est remis dans la bibliothèque d'origine (compensation).
- **Fichiers sources :** `Application/Books/Move/*`, `Application/Books/MoveTargets/*`, `Web/Services/BookMoveWorkflow.cs`.

#### Import d'un fichier numérique (pipeline d'import)
- **Type :** système/arrière-plan
- **Déclencheur :** fichier détecté par `FileWatcherService` (scan au démarrage puis sondage toutes les 30 s) dans `{ImportDirectory}/{NOM}_{LibraryId}/`, ou dépôt par l'import de flux.
- **Étapes :**
  1. `FileWatcherService` ignore le dossier `errors`, les extensions non prises en charge et les fichiers hors dossier de bibliothèque ; attend que le fichier soit stable (10 essais de 500 ms), crée un `ImportJob` (refus si une tâche active existe déjà pour ce chemin) et le met en file Hangfire.
  2. `ProcessImportJobCommandHandler` : si la tâche n'est plus `Pending` et n'est pas terminale, elle est marquée en échec (reprise d'une tâche bloquée) et le fichier est déplacé en `errors`.
  3. Contrôle d'espace disque : 3 × la taille du fichier d'origine.
  4. `Extracting` : PDF → extraction d'images, sinon extraction d'archive ; lecture éventuelle de `ComicInfo.xml`.
  5. `Converting` : conversion de chaque page en WebP (largeur 1400, double page ×2, qualité 75) avec progression enregistrée tous les 10 %.
  6. `SearchingMetadata` : ISBN issu du nom de fichier, sinon de `ComicInfo.xml`, sinon lecture OCR des pages ; recherche de métadonnées par ISBN ; écriture d'un `ComicInfo.xml` mis à jour.
  7. `UploadingCover` : téléversement de la première page sur Cloudinary (dossier `digital-covers`), au mieux (n'échoue jamais le pipeline).
  8. `BuildingArchive` : construction du CBZ (`{ISBN}.cbz` ou `{id}.cbz`).
  9. `Completed` : validation du domaine, déplacement du CBZ dans la bibliothèque, création du `DigitalBook`, enregistrement des candidats ISBN éventuels, clôture de la tâche ; en cas d'échec d'enregistrement, le fichier déplacé est supprimé.
  10. Fichier d'origine supprimé en cas de succès, sinon déplacé dans `errors` (suffixe horodaté en cas de collision).
- **Transitions d'état :** `Pending → Extracting → Converting → SearchingMetadata → UploadingCover → BuildingArchive → Completed` ; `Failed` depuis tout état non terminal.
- **Fichiers sources :** `Web/Services/FileWatcherService.cs`, `Application/ImportJobs/Process/ProcessImportJobCommandHandler.cs`, `Domain/ImportJobs/ImportJob.cs`, `Persistence/Services/ImageProcessorService.cs`, `Persistence/LocalStorage/ImportDirectoryStorage.cs`.

#### Recherche de métadonnées par ISBN
- **Type :** système
- **Déclencheur :** ajout/recherche de détails d'un livre, étape d'import.
- **Étapes :**
  1. Nettoyage de l'ISBN (tirets et espaces retirés).
  2. Ordre des sources : BnF, Google Books, Open Library, Bubble, Bédéthèque ; la première qui trouve le livre l'emporte. La BnF n'ayant pas de couverture, la couverture est cherchée dans Google Books, Open Library puis Bubble.
  3. Titre/série/volume déduits par motifs (« , tome N », « - tome N », « vol. N », « #N »…) pour les sources textuelles.
  4. La couverture est téléversée sur Cloudinary (publicId = ISBN normalisé) ; en cas d'échec, l'URL d'origine est conservée.
  5. Bédéthèque : cache ISBN → URL, sinon recherche SerpApi sous trois formes d'ISBN (brut, avec tirets, court), puis analyse HTML ; un défi Cloudflare suspend les recherches Bédéthèque pour `CloudflarePauseHours` (24 h) pour toute l'application.
  6. Chaque source est désactivable par bascule ; toute erreur inattendue renvoie « non trouvé ».
- **Fichiers sources :** `Application/ComicInfoSearch/*`.

#### Lecture de l'ISBN par OCR
- **Type :** système/arrière-plan (par bibliothèque) et orienté utilisateur (par livre)
- **Déclencheur :** démarrage d'un scan de bibliothèque ou d'un livre ; étape d'import.
- **Étapes :**
  1. `StartLibraryIsbnScanCommandHandler` : bibliothèque de l'utilisateur et de type numérique, liste des livres sans ISBN jamais scannés, mise en file si la liste n'est pas vide, retourne le nombre de livres.
  2. `ScanLibraryIsbnsCommandHandler` : scan livre par livre, chacun enregistré indépendamment ; résumé (assignés, avec candidats, sans ISBN, non scannés).
  3. `IsbnPageScanner` : ordre de scan des pages (1, 2, dernière, avant-dernière, puis 5 premières et 5 dernières) ; arrêt dès qu'un ISBN valide est trouvé ; moteur OCR indisponible → « non scanné ».
  4. `BookIsbnScan` : un seul ISBN non utilisé par un autre livre est affecté ; sinon les ISBN sont conservés comme candidats (l'utilisateur choisit).
- **Transitions d'état :** `IsbnScanState` : `NotScanned → HasIsbn | Candidates | NotFound`.
- **Fichiers sources :** `Application/Books/IsbnScan/*`, `Persistence/Services/IsbnPageScanner.cs`.

#### Synchronisation et analyse des flux
- **Type :** système/arrière-plan
- **Déclencheur :** tâche récurrente Hangfire `feed-import-sync` (cron déduit de `SyncIntervalMinutes`), si la bascule `FeedImport` est active.
- **Étapes :**
  1. Résolution de l'utilisateur via `FeedImport:UserEmail` ; arrêt si absent.
  2. `SyncFeedImportsCommandHandler` : recherche de la catégorie Miniflux (comparaison insensible à la casse), lecture des articles favoris, création d'une `FeedImportDecision` par article inconnu, puis retrait de l'étoile. La décision est toujours enregistrée avant de retirer l'étoile ; en cas d'échec d'enregistrement l'article reste favori pour la prochaine synchronisation.
  3. Analyse des décisions `Pending` (chacune dans sa portée) : domaine source autorisé, page récupérée en HTTPS, extracteur de site (Planète BD, Zone-Ebook, ou générique) , regroupement des liens en livres/miroirs, création de décisions sœurs pour les articles multi-livres, contrôle de doublon.
  4. Mise en file d'un job de téléchargement par décision `LinksExtracted` (si `DebridLink:ApiKey` est renseignée).
- **Transitions d'état :** voir « Décision d'import de flux ».
- **Fichiers sources :** `Web/Services/FeedImportSyncJob.cs`, `Application/FeedImports/Sync/*`, `Analyze/*`, `Analysis/*`.

#### Décision d'import de flux (cycle de vie, arbitrage, téléchargement)
- **Type :** mixte (système et utilisateur)
- **Déclencheur :** synchronisation, action manuelle sur `/feed-imports`.
- **Étapes :**
  1. Analyse → `LinksExtracted` (aucun doublon), `SkippedDuplicate` (doublon certain), `AwaitingArbitration` (liens ambigus ou doublon probable) ou `Failed`.
  2. Arbitrage : `ConfirmNotDuplicate` / `ConfirmDuplicate` pour un doublon probable ; pour des liens ambigus, garder un candidat ou fusionner tous les miroirs en un seul livre.
  3. Actions manuelles : ignorer, forcer le téléchargement, réessayer, corriger série/titre/volume, supprimer, supprimer en masse les décisions téléchargées dont l'import a réussi ou dont la tâche a été supprimée.
  4. Téléchargement (`DownloadFeedImportDecisionCommandHandler`) : clé Debrid-Link requise ; récupération ou création de la bibliothèque numérique cible (couleur `#5C6BC0`, icône `CollectionsBookmark`) ; passage à `Downloading` ; essai des miroirs par ordre de priorité des hébergeurs et selon les domaines pris en charge par Debrid-Link ; refus des extensions non prises en charge et des fichiers trop gros ; hôte de téléchargement vérifié (HTTPS, hôtes autorisés) ; arrêt immédiat sur clé invalide, quota atteint, fichier trop gros ou type non pris en charge ; création de l'`ImportJob` avant le dépôt du fichier pour que l'observateur de fichiers ne crée pas de doublon ; dépôt atomique (copie en `.part` puis renommage) ; mise en file de l'import ; statut `Downloaded`.
- **Transitions d'état :** `Pending → LinksExtracted | AwaitingArbitration | SkippedDuplicate | Failed` ; `LinksExtracted → Downloading → Downloaded | Failed` ; `AwaitingArbitration → LinksExtracted | SkippedDuplicate` ; `Ignored` depuis `Pending, LinksExtracted, AwaitingArbitration, SkippedDuplicate, Failed` ; réouverture (`Retry`, `Correct`) vers `Pending`. `Imported` est défini mais non utilisé.
- **Fichiers sources :** `Domain/FeedImports/FeedImportDecision.cs`, `Application/FeedImports/*`.

#### Bascule d'une fonctionnalité
- **Type :** orienté utilisateur (Admin)
- **Déclencheur :** commutateur dans `/settings`.
- **Étapes :** rejet d'une valeur inconnue ; activation refusée si la configuration requise manque ; si la valeur demandée égale celle de la configuration, la surcharge est supprimée ; sinon surcharge créée/mise à jour ; cache mémoire mis à jour (singleton).
- **Fichiers sources :** `Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs`, `FeatureToggleDefaults.cs`, `FeatureToggles.cs`.

#### Lecture d'un livre
- **Type :** orienté utilisateur
- **Étapes :** informations du lecteur (nombre de pages, dernière page bornée à la taille actuelle de l'archive, réglages) ; chaque page est servie avec `Cache-Control: private, max-age=86400` et un ETag dérivé de la date de modification du fichier ; progression mise à jour seulement si l'index change ; aucune page n'est stockée sur le serveur.
- **Fichiers sources :** `Application/Books/Read/*`, `Web/EndPoints/BooksEndpoints.cs`.

#### Démarrage de l'application
- **Type :** système
- **Étapes :** validation des options (au démarrage), migrations, création des dossiers d'import/temp, chargement des bascules, planification de la synchronisation, bilan de santé (échec fatal si `Unhealthy`, avertissement si `Degraded`), création des dossiers d'import des bibliothèques numériques par `FileWatcherService`.
- **Fichiers sources :** `Web/Program.cs`, `Web/Services/FileWatcherService.cs`.

## 5. Règles métier et validation

| ID | Règle | Description | Criticité | Source |
|------|------|-------------|-----------|--------|
| BR-001 | Isolation par utilisateur | Une bibliothèque, un livre, une tâche d'import ou une décision n'est accessible qu'à son propriétaire ; sinon la ressource est signalée « non trouvée » pour ne pas révéler son existence | Core | `Application/**/*Handler.cs` |
| BR-002 | Validité de l'ISBN | ISBN-10 (somme pondérée modulo 11, dernier caractère chiffre ou X) ou ISBN-13 (clé de contrôle modulo 10) après suppression des tirets/espaces et mise en majuscules | Core | `Application/Helpers/IsbnHelper.cs` |
| BR-003 | Unicité de l'ISBN | Création : refus si un livre avec le même ISBN normalisé existe (`BOK409`) ; modification : refus si un autre livre l'utilise | Core | `CreateBookCommandHandler.cs`, `UpdateBookCommandHandler.cs` |
| BR-004 | Livre physique | Série, titre et ISBN obligatoires, bibliothèque obligatoire ; la mise à jour exige aussi un ISBN | Core | `Domain/Books/PhysicalBook.cs` |
| BR-005 | Livre numérique | Série, titre, chemin de fichier obligatoires, bibliothèque non vide, taille > 0 ; l'ISBN est facultatif | Core | `Domain/Books/DigitalBook.cs` |
| BR-006 | Longueurs maximales des livres | Série 200, titre 200, ISBN 20, lien image 500, auteurs 200, éditeurs 200, chemin de fichier 500 | Supporting | `Domain/Books/BookConstants.cs` |
| BR-007 | Note de création | Note 0 à 5 à la création (0 = non lu) ; une note > 0 crée une date de lecture du jour | Core | `CreateBookCommandHandler.cs` |
| BR-008 | Note de lecture | Une lecture ajoutée manuellement exige une note de 1 à 5 ; la date est l'instant courant (UTC) | Core | `AddReadingDateCommandHandler.cs` |
| BR-009 | Compatibilité livre/bibliothèque | Une bibliothèque `Physical` ne contient que des `PhysicalBook`, une `Digital` que des `DigitalBook` (`LIB505`) | Core | `Domain/Libraries/Library.cs` |
| BR-010 | Validation de la bibliothèque | Nom ≤ 200 non vide, couleur ≤ 7 non vide, icône ≤ 100 non vide, utilisateur non vide, type défini | Supporting | `Domain/Libraries/Library.cs`, `LibraryConstants.cs` |
| BR-011 | Nom de bibliothèque unique par utilisateur | Création et renommage refusés si le nom existe déjà (`LIB409`) | Core | `CreateLibraryCommandHandler.cs`, `UpdateLibraryCommandHandler.cs` |
| BR-012 | Dossiers des bibliothèques numériques | Chemin = nom sans diacritiques en majuscules ; dossier d'import = `{CHEMIN}_{Id}` ; création, renommage (avec retour arrière si le second déplacement échoue) et suppression uniquement pour le type numérique | Core | `Library.cs`, `Create/Update/DeleteLibraryCommandHandler.cs` |
| BR-013 | Déplacement de livre | Même type de bibliothèque obligatoire, cible différente de la source, identifiants non vides ; contrôles avant tout déplacement de fichier ; compensation si l'enregistrement échoue | Core | `MoveBookCommandHandler.cs`, `Book.cs` |
| BR-014 | Suggestion de bibliothèque cible | La cible suggérée est celle qui contient le plus de livres de la même série (normalisée), départage par nom | Peripheral | `GetBookMoveTargetsQueryHandler.cs` |
| BR-015 | Suppression d'un livre numérique | Le fichier est supprimé après l'enregistrement de la suppression en base | Supporting | `DeleteBookCommandHandler.cs` |
| BR-016 | Progression de lecture | Index de page ≥ 0 ; enregistrement uniquement si la valeur change ; page de reprise bornée au nombre réel de pages | Supporting | `DigitalBook.cs`, `UpdateReadingProgressCommandHandler.cs`, `GetBookReaderInfoQueryHandler.cs` |
| BR-017 | Réglages du lecteur | Les trois énumérations doivent être des valeurs définies ; défaut = `FitWidth`, `Continuous`, `LeftToRight` | Peripheral | `Domain/Books/ReaderSettings.cs` |
| BR-018 | Sécurité du chemin des archives | Seules les archives situées sous la racine de stockage sont lues ou téléchargeables ; téléchargement réservé aux livres numériques | Core | `ComicPageReaderService.cs`, `BooksEndpoints.cs` |
| BR-019 | Taille maximale d'une page | Une page de plus de 50 Mo est refusée (archive considérée corrompue) | Peripheral | `ComicPageReaderService.cs` |
| BR-020 | Pagination | Page et taille de page > 0 ; tri par défaut `IdDesc` ; recherche insensible à la casse sur titre et série ; tri stable par `Id` | Supporting | `GetPagedBooksQueryHandler.cs`, `BookReadService.cs` |
| BR-021 | Extensions d'import | `.cbz .cbr .zip .rar .pdf` pour la création d'une tâche d'import ; bibliothèque obligatoirement numérique ; taille > 0 | Core | `CreateImportJobCommandHandler.cs`, `ImportSettings.cs` |
| BR-022 | Unicité des tâches actives | Une seule tâche non terminale (statuts autres que `Completed`/`Failed`) par chemin de fichier d'origine (`IMP409B`) | Core | `CreateImportJobCommandHandler.cs`, `ApplicationDbContext.cs` |
| BR-023 | Transitions de tâche d'import | Avancement strictement séquentiel ; `Completed` seulement depuis `BuildingArchive` ; aucune modification d'une tâche terminée ou échouée | Core | `Domain/ImportJobs/ImportJob.cs` |
| BR-024 | Suppression d'une tâche | Réservée aux tâches terminées ou échouées (`IMP412`) | Supporting | `DeleteImportJobCommandHandler.cs` |
| BR-025 | Espace disque d'import | Espace libre requis = 3 × taille du fichier d'origine | Supporting | `ProcessImportJobCommandHandler.cs` |
| BR-026 | Résolution de l'ISBN à l'import | Priorité : nom de fichier, puis `ComicInfo.xml`, puis OCR des pages ; plusieurs ISBN lus → aucun n'est choisi, ils deviennent candidats | Core | `ProcessImportJobCommandHandler.cs` |
| BR-027 | Métadonnées par défaut à l'import | Série = série du `ComicInfo` ou nom de fichier ; titre = titre ou série ; volume 1 par défaut ; année seule → 1er janvier ; les métadonnées trouvées par ISBN remplacent celles du fichier | Supporting | `ProcessImportJobCommandHandler.cs` |
| BR-028 | Conversion des images | Largeur cible 1400 px (×2 pour une double page), WebP qualité 75, dimensions max 8 000 px, WebP déjà à la bonne largeur copié tel quel, pages nommées `page-001.webp`… | Supporting | `ImageProcessorService.cs` |
| BR-029 | Nom du CBZ final | `{ISBN normalisé}.cbz` si l'ISBN est valide, sinon `{id de tâche}.cbz` | Supporting | `ProcessImportJobCommandHandler.cs` |
| BR-030 | Couverture au mieux | L'échec du téléversement de couverture n'échoue jamais l'import ; la couverture est la première page | Supporting | `ProcessImportJobCommandHandler.cs` |
| BR-031 | Tâche bloquée | Une tâche relancée dans un état intermédiaire est marquée en échec et son fichier déplacé en `errors` | Supporting | `ProcessImportJobCommandHandler.cs` |
| BR-032 | Sécurité des chemins d'import | Tout chemin doit rester sous le dossier d'import ; noms de fichiers sans caractères invalides ; collision → suffixe ` (2)`, ` (3)`… | Core | `ImportDirectoryStorage.cs` |
| BR-033 | Lecture de l'ISBN dans un nom de fichier | Motifs : `ISBN-…`, `(…)`, `[…]`, 13 chiffres commençant par 978/979, 10 chiffres ; seul un ISBN valide est retenu | Supporting | `FileNameIsbnExtractor.cs` |
| BR-034 | Extraction d'ISBN par OCR | Seuls les nombres dont la clé de contrôle est valide sont retenus ; confusions OCR (O→0, I/l/\|→1, Z→2, S→5, B→8) tolérées seulement après le libellé « ISBN » ; un ISBN-10 sans tirets n'est pas accepté hors libellé | Supporting | `TextIsbnExtractor.cs` |
| BR-035 | Ordre de scan OCR | Pages 1, 2, dernière, avant-dernière, puis `PagesFromEachEnd` pages à chaque extrémité ; arrêt au premier ISBN trouvé | Peripheral | `IsbnScanPages.cs`, `IsbnPageScanner.cs` |
| BR-036 | Affectation d'un ISBN scanné | Un seul ISBN inutilisé par un autre livre est affecté ; sinon candidats ; scan interdit si le livre a déjà un ISBN ; une fois un ISBN posé, les candidats sont effacés | Core | `BookIsbnScan.cs`, `ScanBookIsbnCommandHandler.cs`, `DigitalBook.cs` |
| BR-037 | État du scan ISBN | `HasIsbn` si ISBN ; sinon `Candidates` si candidats ; sinon `NotScanned` si jamais scanné, `NotFound` autrement | Supporting | `Domain/Books/IsbnScanState.cs` |
| BR-038 | Ordre des sources de métadonnées | BnF, Google Books, Open Library, Bubble, Bédéthèque ; première source qui trouve gagne | Core | `ComicSearchService.cs` |
| BR-039 | Pause Bédéthèque | Un défi Cloudflare suspend toute recherche Bédéthèque pendant `CloudflarePauseHours` (24 h) ; sans clé SerpApi la recherche est ignorée | Supporting | `BedethequeService.cs`, `BedethequeCircuit.cs` |
| BR-040 | Garde SSRF | Chaque client HTTP sortant n'accepte que des hôtes listés (HTTPS, sous-domaines selon le cas), 5 redirections maximum, redirections suivies manuellement, en-têtes sensibles retirés lors d'un changement d'hôte | Core | `Web/Infrastructure/SsrfGuardHandler.cs`, `Web/Program.cs`, `FeedImportConfiguration.cs` |
| BR-041 | Hôtes de couverture Cloudinary | Le téléversement par URL n'accepte que `www.bedetheque.com`, `books.google.com`, `books.googleusercontent.com`, `covers.openlibrary.org` (HTTPS) | Supporting | `CloudinaryService.cs` |
| BR-042 | Entrée de flux valide | Utilisateur et identifiant Miniflux non vides, URL absolue http(s) ≤ 2 000 caractères ; titre vide → URL ; titre tronqué à 500 | Supporting | `FeedImportDecision.cs` |
| BR-043 | Idempotence de la synchronisation | Unicité (utilisateur, entrée Miniflux, index) ; une entrée déjà connue n'est que dé-étoilée à nouveau | Core | `SyncFeedImportsCommandHandler.cs`, `ApplicationDbContext.cs` |
| BR-044 | Analyse de l'article | Domaine source dans `AllowedSourceHosts` (sous-domaines inclus), page toujours récupérée en HTTPS ; aucun lien vers un hôte autorisé → échec ; liens vers hôtes non pris en charge → décision `Failed` conservant les liens | Core | `AnalyzeFeedImportDecisionCommandHandler.cs` |
| BR-045 | Regroupement des liens | Même nom de fichier normalisé = miroirs d'un même livre ; clés de groupe fournies par le site prioritaires ; un fichier nommé + liens sans nom sur d'autres hôtes = miroirs ; mélange nommé/sans nom, ou plusieurs liens sans nom sur le même hôte = ambigu (arbitrage) | Core | `DownloadLinkGrouper.cs` |
| BR-046 | Détection de doublon certain | Même ISBN, ou même série normalisée (similarité 1) et même volume | Core | `DuplicateBookFinder.cs` |
| BR-047 | Détection de doublon probable | Similarité de série ≥ 0,85 (distance de Levenshtein normalisée) avec même volume, ou série identique sans volume connu | Core | `DuplicateBookFinder.cs`, `SerieMatcher.cs` |
| BR-048 | Normalisation des séries | Majuscules, sans diacritiques ni ponctuation, article initial ou final (LE, LA, LES, L, UN, UNE, DES, THE, A) retiré | Supporting | `SerieMatcher.cs` |
| BR-049 | Analyse de titre | Extraction série/volume/titre par motifs (« Tome 3 », « T03 », « Vol. 105 », « #40 », « n°12 », « Série - 20 - Titre ») ; plages de tomes (« Tomes 1 à 5 ») sans volume ; extensions de fichier retirées ; groupes entre crochets/parenthèses conservés seulement s'ils portent un volume | Supporting | `ComicTitleParser.cs` |
| BR-050 | Titre de l'article vs nom de fichier | Pour un article à un seul livre, le titre de l'article prime ; pour plusieurs livres, le nom de fichier de chacun prime | Supporting | `FeedImportAnalysisRules.cs` |
| BR-051 | Transitions des décisions de flux | Analyse seulement depuis `Pending`/`AwaitingArbitration` ; téléchargement seulement depuis `LinksExtracted` ; `Fail` impossible depuis un statut final ; chaque transition ajoute un événement d'historique | Core | `FeedImportDecision.cs` |
| BR-052 | Actions manuelles autorisées | Ignorer : `Pending, LinksExtracted, AwaitingArbitration, SkippedDuplicate, Failed` ; forcer le téléchargement : un seul candidat et doublon (certain ou probable) ; corriger : un seul candidat, série obligatoire ; réessayer : `Failed`, `Ignored`, ou `Downloading` périmé | Supporting | `FeedImportDecision.cs` |
| BR-053 | Téléchargement périmé | Un téléchargement `Downloading` depuis plus d'1 h est considéré interrompu (réessai et suppression possibles) ; un téléchargement en cours ne peut ni être supprimé ni relancé | Supporting | `FeedImportConstants.cs`, `FeedImportDecision.cs` |
| BR-054 | Arrêt sur erreur globale | Clé Debrid-Link invalide, quota atteint, fichier trop gros ou type non pris en charge arrêtent l'essai des miroirs suivants ; les autres échecs sont tracés et le miroir suivant est essayé | Supporting | `DownloadFeedImportDecisionCommandHandler.cs` |
| BR-055 | Priorité des hébergeurs | Les miroirs sont triés selon `DebridLink:HosterPriority` (tri stable), puis filtrés par les domaines pris en charge par Debrid-Link (si la liste est indisponible, tous sont essayés) | Supporting | `DownloadFeedImportDecisionCommandHandler.cs` |
| BR-056 | Suppression en masse des décisions téléchargées | Seules les décisions dont la tâche d'import est `Completed` ou a été supprimée sont retirées ; les livres importés ne sont pas touchés | Supporting | `DeleteDownloadedFeedImportDecisionsCommandHandler.cs` |
| BR-057 | Intervalle de synchronisation | 1 à 59 minutes, ou multiple de 60 jusqu'à 1 440 | Supporting | `FeedImportSyncJob.cs` |
| BR-058 | Configuration de flux requise | Si `FeedImport:Enabled` : e-mail utilisateur, URL Miniflux http(s) absolue, clé API et catégorie Miniflux obligatoires | Supporting | `FeedImportConfiguration.cs`, `FeatureToggleDefaults.cs` |
| BR-059 | Bascules de fonctionnalités | Activer une fonctionnalité dont la configuration est incomplète est refusé ; revenir à la valeur de configuration supprime la surcharge ; `Bubble` n'est actif qu'avec une clé API | Supporting | `SetFeatureToggleCommandHandler.cs`, `FeatureToggleDefaults.cs` |
| BR-060 | Statistiques | 12 mois d'historique de lectures, mois sans lecture complétés à 0 ; filtre optionnel par bibliothèque appartenant à l'utilisateur | Supporting | `GetStatisticsQueryHandler.cs`, `StatisticsReadService.cs` |
| BR-061 | Résolution de l'utilisateur par API | L'identifiant d'utilisateur est résolu par la revendication `sub`, puis par l'e-mail pour les comptes non migrés | Supporting | `BooksEndpoints.cs` |
| BR-062 | Cache HTTP des pages | Pages servies en cache privé 24 h, ETag lié à la date de modification de l'archive | Peripheral | `BooksEndpoints.cs` |
| BR-063 | Journaux et accès Hangfire | Tableau de bord Hangfire réservé aux administrateurs authentifiés | Supporting | `HangfireAuthorizationFilter.cs` |

## 6. Modèle de domaine

#### Book (abstraite)
- **Objectif :** socle commun aux livres physiques et numériques.
- **Fichier source :** `Domain/Books/Book.cs`

| Propriété | Type | Description | Source |
|-----------|------|-------------|--------|
| Id | Guid (UUID v7) | Identifiant (classe `Entity<Guid>`) | `Book.cs` |
| LibraryId / Library | Guid / Library | Bibliothèque contenante | `Book.cs` |
| Serie | string | Série | `Book.cs` |
| Title | string | Titre | `Book.cs` |
| ISBN | string? | ISBN normalisé | `Book.cs` |
| VolumeNumber | int | Numéro de tome (1 par défaut) | `Book.cs` |
| ImageLink | string | URL de couverture | `Book.cs` |
| Authors | string | Auteurs | `Book.cs` |
| Publishers | string | Éditeurs | `Book.cs` |
| PublishDate | DateOnly? | Date de publication | `Book.cs` |
| NumberOfPages | int? | Nombre de pages | `Book.cs` |
| ReadingDates | IReadOnlyList\<ReadingDate\> | Lectures | `Book.cs` |

#### PhysicalBook
- **Objectif :** exemplaire papier, identifié par ISBN obligatoire.
- **Fichier source :** `Domain/Books/PhysicalBook.cs` (aucune propriété supplémentaire).

#### DigitalBook
- **Objectif :** livre numérique (CBZ) avec lecteur intégré et scan d'ISBN.
- **Fichier source :** `Domain/Books/DigitalBook.cs`

| Propriété | Type | Description | Source |
|-----------|------|-------------|--------|
| FilePath | string | Chemin du CBZ | `DigitalBook.cs` |
| FileSize | long | Taille en octets | `DigitalBook.cs` |
| LastReadPage | int | Dernière page lue (base 0) | `DigitalBook.cs` |
| ReaderLayout / ReaderScrolling / ReadingDirection | enum | Réglages d'affichage | `DigitalBook.cs` |
| IsbnCandidates | IReadOnlyList\<string\> | ISBN lus par OCR en attente de choix | `DigitalBook.cs` |
| IsbnScannedAt | DateTime? | Date du dernier scan | `DigitalBook.cs` |

#### ReadingDate
- **Objectif :** une lecture d'un livre avec sa note.
- **Fichier source :** `Domain/Books/ReadingDate.cs`

| Propriété | Type | Description | Source |
|-----------|------|-------------|--------|
| Date | DateTime | Date de lecture | `ReadingDate.cs` |
| Rating | int | Note | `ReadingDate.cs` |
| BookId | Guid | Livre concerné | `ReadingDate.cs` |

#### BookMetadata (enregistrement) et ReaderSettings (enregistrement)
- **Objectif :** objets valeur de transport des métadonnées et des réglages du lecteur.
- **Fichiers sources :** `Domain/Books/BookMetadata.cs`, `ReaderSettings.cs`

#### IsbnBedethequeUrl
- **Objectif :** cache ISBN → URL d'album Bédéthèque.
- **Fichier source :** `Domain/Books/IsbnBedethequeUrl.cs`

| Propriété | Type | Description | Source |
|-----------|------|-------------|--------|
| ISBN | string | ISBN (unique, ≤ 20) | `IsbnBedethequeUrl.cs` |
| Url | string | URL (≤ 500) | `IsbnBedethequeUrl.cs` |

#### Library
- **Objectif :** collection de livres d'un utilisateur, physique ou numérique.
- **Fichier source :** `Domain/Libraries/Library.cs`

| Propriété | Type | Description | Source |
|-----------|------|-------------|--------|
| Name | string | Nom (≤ 200) | `Library.cs` |
| Color | string | Couleur (≤ 7) | `Library.cs` |
| Icon | string | Icône (≤ 100) | `Library.cs` |
| BookType | LibraryBookType | Physique ou numérique | `Library.cs` |
| UserId | Guid | Propriétaire | `Library.cs` |
| DefaultBookSortOrder | BookSortOrder | Tri par défaut | `Library.cs` |
| RelativePath (calculée, non persistée) | string | Nom sans diacritiques en majuscules | `Library.cs`, `ApplicationDbContext.cs` |
| ImportDirectoryName (calculée) | string | `{RelativePath}_{Id}` | `Library.cs` |

#### User
- **Objectif :** utilisateur applicatif lié à un compte Auth0.
- **Fichier source :** `Domain/Users/User.cs`

| Propriété | Type | Description | Source |
|-----------|------|-------------|--------|
| Email | string | Adresse e-mail | `User.cs` |
| AuthId | string | Identifiant Auth0 (`sub`) | `User.cs` |

#### ImportJob
- **Objectif :** suivi d'un import de fichier numérique.
- **Fichier source :** `Domain/ImportJobs/ImportJob.cs`

| Propriété | Type | Description | Source |
|-----------|------|-------------|--------|
| OriginalFileName / OriginalFilePath / OriginalFileSize | string / string / long | Fichier d'origine | `ImportJob.cs` |
| LibraryId | Guid | Bibliothèque cible | `ImportJob.cs` |
| DigitalBookId | Guid? | Livre créé | `ImportJob.cs` |
| Status | ImportJobStatus | Étape courante | `ImportJob.cs` |
| CreatedAt / CompletedAt | DateTime / DateTime? | Dates | `ImportJob.cs` |
| ErrorMessage / ErrorStep | string? | Erreur (≤ 2 000 / ≤ 100) | `ImportJob.cs` |
| ConvertedImagesCount / TotalImagesToConvert | int | Progression de conversion | `ImportJob.cs` |
| xmin | uint | Jeton de concurrence PostgreSQL | `ApplicationDbContext.cs` |

#### FeedImportDecision
- **Objectif :** décision d'import d'un livre trouvé dans un article de flux.
- **Fichier source :** `Domain/FeedImports/FeedImportDecision.cs`

| Propriété | Type | Description | Source |
|-----------|------|-------------|--------|
| UserId | Guid | Propriétaire | `FeedImportDecision.cs` |
| MinifluxEntryId, ItemIndex | long, int | Entrée Miniflux et rang du livre dans l'article (0 = premier) | `FeedImportDecision.cs` |
| EntryTitle, EntryUrl, PublishedAt | string, string, DateTime? | Article | `FeedImportDecision.cs` |
| ParsedSerie / ParsedTitle / ParsedVolume | string? / string? / int? | Identité analysée | `FeedImportDecision.cs` |
| Links | string? (jsonb) | Candidats de téléchargement sérialisés | `FeedImportDecision.cs`, `ApplicationDbContext.cs` |
| ChosenMirror, MatchedBookId, ImportJobId | string?, Guid?, Guid? | Miroir retenu, livre en doublon, tâche d'import | `FeedImportDecision.cs` |
| Status, ArbitrationKind, DecidedBy | enums | État, type d'arbitrage, auteur de la décision | `FeedImportDecision.cs` |
| Reason, ErrorMessage, ErrorStep | string | Motif et erreur | `FeedImportDecision.cs` |
| CreatedAt, UpdatedAt | DateTime | Dates | `FeedImportDecision.cs` |
| Events | liste de FeedImportDecisionEvent | Historique | `FeedImportDecision.cs` |

#### FeedImportDecisionEvent
- **Objectif :** historique des transitions d'une décision.
- **Fichier source :** `Domain/FeedImports/FeedImportDecisionEvent.cs` (FeedImportDecisionId, OccurredAt, PreviousStatus, Status, DecidedBy, Description).

#### DownloadCandidate / DownloadMirror / ParsedComicTitle
- **Objectif :** objets valeur (un livre et ses miroirs ; un miroir = URL + hôte ; résultat d'analyse de titre).
- **Fichiers sources :** `Domain/FeedImports/DownloadCandidate.cs`, `DownloadMirror.cs`, `ParsedComicTitle.cs` (ce dernier non lu).

#### FeatureToggleOverride
- **Objectif :** valeur choisie par l'administrateur qui remplace la configuration.
- **Fichier source :** `Domain/Settings/FeatureToggleOverride.cs` (Toggle stocké en chaîne ≤ 64, unique ; Enabled).

#### Enumerations

| Nom de l'énumération | Valeurs | Source |
|-----------|--------|--------|
| LibraryBookType | Physical, Digital | `Domain/Libraries/LibraryBookType.cs` |
| BookSortOrder | IdDesc=0, IdAsc=1, SerieAndVolumeAsc=2 | `Domain/Libraries/BookSortOrder.cs` |
| ImportJobStatus | Pending, Extracting, Converting, SearchingMetadata, UploadingCover, BuildingArchive, Completed, Failed | `Domain/ImportJobs/ImportJobStatus.cs` |
| FeedImportDecisionStatus | Pending, LinksExtracted, AwaitingArbitration, SkippedDuplicate, Downloading, Downloaded, Imported (inutilisé), Ignored, Failed | `Domain/FeedImports/FeedImportDecisionStatus.cs` |
| FeedImportArbitrationKind | None, AmbiguousLinks, ProbableDuplicate | `Domain/FeedImports/FeedImportArbitrationKind.cs` |
| FeedImportDecidedBy | Auto, User | `Domain/FeedImports/FeedImportDecidedBy.cs` |
| FeatureToggle | FeedImport, IsbnOcr, Bedetheque, Bnf, GoogleBooks, OpenLibrary, Bubble | `Domain/Settings/FeatureToggle.cs` |
| ReaderLayout | FitWidth, SinglePage, TwoPages | `Domain/Books/ReaderSettings.cs` |
| ReaderScrolling | Continuous, PageByPage | `Domain/Books/ReaderSettings.cs` |
| ReadingDirection | LeftToRight, RightToLeft | `Domain/Books/ReaderSettings.cs` |
| IsbnScanState | HasIsbn, NotScanned, Candidates, NotFound | `Domain/Books/IsbnScanState.cs` |
| IsbnScanOutcome | Assigned, WithCandidates, WithoutIsbn, NotScanned | `Application/Books/IsbnScan/IsbnScanOutcome.cs` |
| DuplicateMatchKind | None, Probable, Certain | `Application/FeedImports/Analysis/DuplicateBookFinder.cs` |
| FeedImportArbitrationAction / FeedImportDecisionAction | Valeurs non lues, mais utilisées : NotDuplicate, ConfirmDuplicate, KeepCandidate (+ fusion) ; ForceDownload, Ignore, Retry | `Application/FeedImports/Arbitrate`, `Manage` |

Codes d'erreur métier (préfixes) : `BOK***` (livres), `LIB***` (bibliothèques), `IMP***` (imports), `FEED***` (flux), `USR`/`UsersError`, `FeatureToggleError`, `StatisticsError` (fichiers `Domain/*/…Error.cs`, les trois derniers non lus).

#### Relations

| Entité A | Entité B | Type de relation | Source |
|----------|----------|------------------|--------|
| User | Library | Un à plusieurs (UserId) | `Library.cs` |
| Library | Book | Un à plusieurs, suppression en cascade | `ApplicationDbContext.cs` |
| Book | ReadingDate | Un à plusieurs, suppression en cascade | `ApplicationDbContext.cs` |
| Book | PhysicalBook / DigitalBook | Héritage, une table par type (TPT) | `ApplicationDbContext.cs` |
| Library | ImportJob | Un à plusieurs (LibraryId, indexé) | `ImportJob.cs`, `ApplicationDbContext.cs` |
| ImportJob | DigitalBook | Un à un facultatif (DigitalBookId) | `ImportJob.cs` |
| FeedImportDecision | FeedImportDecisionEvent | Un à plusieurs, cascade | `ApplicationDbContext.cs` |
| FeedImportDecision | Book (doublon) | Référence facultative (MatchedBookId) | `FeedImportDecision.cs` |
| FeedImportDecision | ImportJob | Référence facultative (ImportJobId) | `FeedImportDecision.cs` |
| FeedImportDecision | User | Plusieurs à un (UserId) | `FeedImportDecision.cs` |

## 7. Points d'intégration

| Intégration | Type | Point d'accès / Cible | Direction | Source |
|-------------|------|-----------------------|-----------|--------|
| Auth0 | API (OIDC) | `Auth0:Domain`, `Auth0:ClientId` | Bidirectionnel | `Web/Program.cs`, `AccountsEndpoints.cs` |
| PostgreSQL | Système externe | `ConnectionStrings:DefaultConnection` (EF Core, Hangfire, Data Protection) | Bidirectionnel | `Web/Program.cs`, `Persistence/ApplicationDbContext.cs` |
| Miniflux | API | `Miniflux:BaseUrl` (`/v1/categories`, `/v1/entries`, `/v1/entries/{id}/bookmark`), en-tête `X-Auth-Token` | Bidirectionnel | `Web/Infrastructure/MinifluxClient.cs` |
| Sites d'articles (Planète BD, Zone-Ebook) | API (HTTP, HTML) | Hôtes de `FeedImport:AllowedSourceHosts` | Sortant | `Web/Configuration/FeedImportConfiguration.cs`, `Analysis/Sites/*` |
| Debrid-Link v2 | API | `DebridLink:BaseUrl` (`downloader/domains`, `downloader/add`, `account/infos`), jeton Bearer | Sortant | `Web/Infrastructure/DebridLinkClient.cs` |
| Hébergeurs de fichiers / serveurs Debrid-Link | Entrée/sortie fichier (HTTP) | Hôtes `AllowedDownloadHosts` et `DebridLink:DownloadHosts`, délai 30 min | Entrant (téléchargement) | `FeedImportConfiguration.cs`, `Web/Infrastructure/FeedImportFileDownloader.cs` (non lu) |
| BnF (SRU) | API | `catalogue.bnf.fr/api/SRU` (Dublin Core) | Sortant | `Application/ComicInfoSearch/BnfCatalogueService.cs` |
| Google Books | API | `www.googleapis.com/books/v1` | Sortant | `GoogleBooksService.cs` (non lu), `Web/Program.cs` |
| Open Library | API | `openlibrary.org`, `covers.openlibrary.org` | Sortant | `OpenLibraryService.cs` (non lu), `Web/Program.cs` |
| Bubble (Algolia + site) | API | `www.bubblebd.com`, `6a891z72zd-dsn.algolia.net` | Sortant | `BubbleService.cs` |
| Bédéthèque | Système externe (HTML) | `www.bedetheque.com` | Sortant | `BedethequeService.cs` |
| SerpApi | API | `serpapi.com/search.json` | Sortant | `BedethequeService.cs` |
| Serper | API | `google.serper.dev/search` (script uniquement) | Sortant | `scripts/FillBedethequeUrls/Program.cs` |
| Cloudinary | API | Compte `Cloudinary:*` (dossiers de couvertures, `digital-covers`) | Sortant | `Persistence/Services/CloudinaryService.cs` |
| Tesseract | Système externe (processus) | Exécutable `IsbnOcr:TesseractPath` | Sortant | `Application/Books/IsbnScan/IsbnOcrSettings.cs`, `Persistence/Services/TesseractPageTextRecognizer.cs` (non lu) |
| Système de fichiers | Entrée/sortie fichier | `LocalStorage:RootPath`, `Import:ImportDirectory`, `Import:TempDirectory` | Bidirectionnel | `Persistence/LocalStorage/*`, `FileWatcherService.cs` |
| Endpoints HTTP internes | API | `/api/books/{id}/download`, `/api/books/{id}/pages/{n}`, `/health`, `/hangfire` | Entrant | `BooksEndpoints.cs`, `Web/Program.cs` |
| Caméra du navigateur (JS) | API navigateur | `wwwroot/js/isbnScanner.js` | Entrant | `AddBookScan.razor.cs` |
| Export JSON de type MongoDB | Entrée fichier | Fichier `--file` | Entrant | `scripts/ImportBooks/Program.cs` |

Aucun envoi d'e-mail ni service web SOAP n'a été identifié dans le code lu.

## 8. Rapports

Aucun rapport (Crystal Reports, SSRS ou génération de rapports en code) n'a été identifié dans le code source. La seule restitution agrégée est la page Statistiques :

| Rapport | Type | Objectif | Sources de données | Paramètres | Format de sortie | Source |
|--------|------|---------|-------------|------------|---------------|--------|
| Statistiques de lecture et de collection | Généré par code (page Blazor) | Totaux et lectures par mois sur 12 mois | Tables des livres, lectures et bibliothèques via `IStatisticsReadService` | Utilisateur, bibliothèque facultative | Affichage à l'écran (`StatisticsDto`) | `Application/Statistics/Get/*`, `Persistence/Queries/StatisticsReadService.cs`, `Web/Components/Pages/Statistics.razor` |

## 9. Références croisées : application vers base de données

#### 9.1 Modes d'accès aux données

- **Approche principale :** Entity Framework Core 10 avec Npgsql (PostgreSQL), dépôts génériques (`IRepository<T, Guid>`), dépôts spécialisés, services de lecture (`*ReadService`) en LINQ avec `AsNoTracking`, unité de travail (`IUnitOfWork`). Migrations appliquées au démarrage (`Database.MigrateAsync`). Hangfire utilise son propre schéma PostgreSQL ; les clés Data Protection sont stockées via `IDataProtectionKeyContext`. Les scripts utilisent aussi `ApplicationDbContext` (dont `ExecuteUpdateAsync`).

#### 9.2 Correspondance entité – table

| Entité / Classe | Table(s) de base de données | Source |
|---------------|-------------------|--------|
| Library | `Libraries` | `Persistence/ApplicationDbContext.cs` |
| User | `Users` | `ApplicationDbContext.cs` |
| Book | `Books` | `ApplicationDbContext.cs` |
| PhysicalBook | `PhysicalBooks` (TPT) | `ApplicationDbContext.cs` |
| DigitalBook | `DigitalBooks` (TPT) ; `IsbnCandidates` en collection primitive | `ApplicationDbContext.cs` |
| ReadingDate | `ReadingDates` | `ApplicationDbContext.cs` |
| IsbnBedethequeUrl | `IsbnBedethequeUrls` | `ApplicationDbContext.cs` |
| ImportJob | `ImportJobs` | `ApplicationDbContext.cs` |
| FeedImportDecision | `FeedImportDecisions` | `ApplicationDbContext.cs` |
| FeedImportDecisionEvent | `FeedImportDecisionEvents` | `ApplicationDbContext.cs` |
| FeatureToggleOverride | `FeatureToggleOverrides` | `ApplicationDbContext.cs` |
| DataProtectionKey | `DataProtectionKeys` | `ApplicationDbContext.cs` |

#### 9.3 Appels de procédures stockées

Aucun appel de procédure stockée n'a été identifié : l'accès aux données passe par EF Core (LINQ). Une migration `20260321120000_AddTrigramIndexesForBooksSearch` crée des index trigramme pour la recherche (contenu non lu ; voir l'analyse de base de données pour le détail).

| Procédure stockée | Fichier(s) appelant(s) | Objectif | Source |
|-----------------|-----------------------|---------|--------|
| (aucune) | — | — | — |
