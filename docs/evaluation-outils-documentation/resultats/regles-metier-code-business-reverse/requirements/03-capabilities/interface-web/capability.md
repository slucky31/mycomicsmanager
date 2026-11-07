# Interface web (Blazor) — Capacité

**Composant :** `Web/Components/` · **Préfixe :** `UI` · **Technologie :** Blazor Server, MudBlazor, FluentValidation · **Dernière extraction :** 2026-10-17 · **Statut :** Brouillon

## Objectif

L'interface web est la seule porte d'entrée des utilisateurs : ils y gèrent leurs bibliothèques de bandes dessinées (livres papier et fichiers numériques), ajoutent des livres, lisent les livres numériques, importent des fichiers, traitent les articles repérés par l'import par flux, consultent des statistiques et, pour les administrateurs, règlent le fonctionnement de l'application. Elle ne porte presque aucune logique de fond : la plupart des règles sont dans les services et les gestionnaires de commandes, mais l'écran en contient qui lui sont propres (validations de saisie, confirmations, activation des boutons, choix proposés).

## Responsabilités

- Imposer la connexion, présenter la navigation et réserver l'administration au rôle Administrateur.
- Contrôler la saisie des formulaires (bibliothèque, livre, ISBN) avant envoi et expliquer les refus.
- Demander une confirmation avant les actions irréversibles (suppression de bibliothèque, de livre, de lecture, de décisions d'import).
- Proposer les actions adaptées au type de bibliothèque (physique ou numérique).
- Restituer l'avancement des traitements de fond (imports, import par flux) et orienter l'utilisateur quand il doit trancher.
- Mémoriser les préférences d'affichage (tri, vue, réglages du lecteur) et la progression de lecture.

## Non géré ici

- Création, modification et suppression effectives des livres et bibliothèques : composants Livres (BK) et Bibliothèques (LIB), voir aussi BR-DB-007 et BR-DB-008.
- Conversion des fichiers importés et analyse des pages : composant Import de fichiers (IMP).
- Décisions, téléchargements et synchronisation de l'import par flux : composant Import par flux (FDI).
- Interrogation des catalogues externes : composant Recherche de métadonnées (SRC).
- Calcul des indicateurs : composant Statistiques (STA).
- Authentification et création du compte à la première connexion : composant Utilisateurs et authentification (USR).
- Pas de couche de services web distincte : l'écran appelle des services applicatifs ; seules deux adresses d'API existent (pages du lecteur et téléchargement), rattachées aux Livres.

## Points d'entrée

Chaque écran est un point d'entrée. Le détail des écrans et leur état d'avancement sont dans [_progress.md](_progress.md).

| # | Type | Route | Sens métier | Appelé par | Règles | Source |
|---|---|---|---|---|---|---|
| 1 | Écran | `/login` | Connexion | Redirection depuis toute page protégée | BR-UI-001, BR-UI-003 | `Web/Components/Pages/Login.razor:1-17` |
| 2 | Écran | `/Error` | Page d'erreur | Gestion d'erreur du serveur | BR-UI-001 | `Web/Components/Pages/Error.razor:1-2` |
| 3 | Écran | `/`, `/libraries/list` | Liste des bibliothèques | Navigation | BR-UI-011 | `Web/Components/Pages/Libraries/LibrariesList.razor:6-7` |
| 4 | Écran | `/libraries/add` | Création d'une bibliothèque | Liste des bibliothèques | BR-UI-005 à 010 | `Web/Components/Pages/Libraries/AddLibrary.razor:3` |
| 5 | Écran | `/libraries/{id}/edit` | Modification d'une bibliothèque | Menu d'une bibliothèque | BR-UI-005 à 010 | `Web/Components/Pages/Libraries/EditLibrary.razor:3` |
| 6 | Écran | `/libraries/{id}` | Livres d'une bibliothèque | Liste des bibliothèques | BR-UI-012 à 016, 031 à 034 | `Web/Components/Pages/Libraries/LibraryDetailPage.razor:3` |
| 7 | Écran | `/books/add/scan` | Scan du code-barres | Menu « New book » | BR-UI-018 | `Web/Components/Pages/Books/AddBookScan.razor:1` |
| 8 | Écran | `/books/add/manual` | Saisie de l'ISBN | Menu « New book » | BR-UI-019 | `Web/Components/Pages/Books/AddBookManual.razor:1` |
| 9 | Écran | `/books/add/form` | Ajout d'un livre | Scan, saisie de l'ISBN, saisie manuelle | BR-UI-017, 020 à 028 | `Web/Components/Pages/Books/AddBookForm.razor:1` |
| 10 | Écran | `/books/{id}` | Fiche d'un livre | Bibliothèque | BR-UI-029, 031 à 035 | `Web/Components/Pages/Books/BookDetail.razor:1` |
| 11 | Écran | `/books/{id}/edit` | Modification d'un livre | Fiche, menu | BR-UI-022 à 026, 028, 030 | `Web/Components/Pages/Books/EditBook.razor.cs:25-26` |
| 12 | Écran | `/books/{id}/find-details` | Compléter un livre depuis le web | Fiche, menu, scan | BR-UI-036 à 040 | `Web/Components/Pages/Books/FindBookDetails.razor:1` |
| 13 | Écran | `/books/{id}/read` | Lecteur | Fiche | BR-UI-041 à 047 | `Web/Components/Pages/Books/BookReader.razor:1` |
| 14 | Écran | `/import` | Import de fichiers | Navigation, bibliothèque numérique | BR-UI-048 à 053 | `Web/Components/Pages/Import.razor:3` |
| 15 | Écran | `/feed-imports` | Décisions d'import par flux | Navigation | BR-UI-054 à 062 | `Web/Components/Pages/FeedImportsPage.razor:1` |
| 16 | Écran | `/statistics` | Statistiques | Navigation | BR-UI-063 | `Web/Components/Pages/Statistics.razor:1` |
| 17 | Écran | `/settings` | Compte, santé, administration | Navigation | BR-UI-002, 064, 065 | `Web/Components/Pages/UserSettings.razor:1` |
| 18 | Écran | `/admin/log-levels` | Niveaux de journalisation | Paramètres (administrateur) | BR-UI-002, 066 | `Web/Components/Pages/Admin/LogLevels.razor:1-2` |
| 19 | Écran | (navigation) | Barre latérale / barre du bas | Toutes les pages | BR-UI-004 | `Web/Components/Layout/NavBar.razor:1-87` |

## Données gérées / touchées

| Entité | Accès | Notes |
|---|---|---|
| [ENT-Book](../../01-domain/entities/book.md) | Lecture / écriture via services | Ajout, modification, suppression, déplacement |
| [ENT-Library](../../01-domain/entities/library.md) | Lecture / écriture via services | Création, modification, suppression, tri par défaut |
| [ENT-ReadingDate](../../01-domain/entities/reading-date.md) | Lecture / écriture via services | Ajout depuis la fiche et le lecteur ; suppression à la modification |
| [ENT-ImportJob](../../01-domain/entities/import-job.md) | Lecture, suppression, échec forcé | Suivi des imports |
| [ENT-FeedImportDecision](../../01-domain/entities/feed-import-decision.md) | Lecture / action | Arbitrage, correction, suppression |
| [ENT-FeatureToggleOverride](../../01-domain/entities/feature-toggle-override.md) | Écriture (administrateur) | Fonctionnalités activables |

## Dépendances

| Dépend de | Raison métier | Comment | Source |
|---|---|---|---|
| Services applicatifs `Web/Services/` (livres, bibliothèques, imports, flux, statistiques, lecteur) | Toute action de l'utilisateur | Appel direct dans le processus (commandes et requêtes CQRS) | `Web/Services/BooksService.cs:34-40` |
| Recherche de métadonnées (SRC) | Retrouver un livre par ISBN, compléter une fiche | Appel de services de catalogue | `Web/Components/Pages/Books/FindBookDetails.razor.cs:14-21` |
| Hébergeur d'images (INT-002) | Copier les couvertures externes | Service d'images | `Web/Components/Pages/Books/FindBookDetails.razor.cs:364` |
| Tâches de fond (Hangfire) | Synchronisation du flux | File de tâches | `Web/Services/FeedImportService.cs:75` |
| Navigateur : caméra, lecture de texte (OCR), plein écran | Scan du code-barres, lecture de l'ISBN | Scripts `isbnScanner.js`, `isbnOcr.js`, `bookReader.js` | `Web/Components/Pages/Books/AddBookScan.razor.cs:54`, `BookReader.razor.cs:201,558` |

## Événements / messages publiés

Néant. L'écran reçoit seulement l'événement interne « décisions modifiées » qui met à jour la pastille de navigation (`Web/Components/Layout/NavBar.razor.cs:24`).

## Configuration à portée métier

| Clé | Sens métier | Valeur dans le dépôt | Valeur de production connue ? |
|---|---|---|---|
| `Import:SupportedExtensions` | Formats de fichiers importables | `.cbz .cbr .zip .rar .pdf` (`Web/appsettings.json:71`) | Non |
| `Import:MaxFileSizeMb` | Taille maximale d'un fichier importé | 1024 (`Web/appsettings.json:72`) | Non |
| `FeedImport:Enabled` | Import par flux activé au départ | `false` (voir carte des composants) | Non |

## Règles métier

Voir [rules.md](rules.md). 66 règles : Élevée 56 / Moyenne 10 / Faible 0.

## Questions ouvertes et anomalies suspectées

Voir [_questions.md](_questions.md) et [_defects.md](_defects.md).
