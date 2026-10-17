# UJ-FO-002 — Parcourir mes bibliothèques

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté.
- **Objectif :** Voir ses bibliothèques, leur contenu en un coup d'œil, et naviguer vers les autres parties de l'application.
- **Entrée :** `/` ou `/libraries/list`, ou l'entrée « Libraries » de la navigation (`Web/Components/Pages/Libraries/LibrariesList.razor:6-7`, `Web/Components/Layout/NavBar.razor:7-10`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Liste des bibliothèques | Ouvre la liste | Une carte par bibliothèque, classées par nom : jusqu'à 3 dernières couvertures (livres récents ayant une couverture), icône, nom, type (« Physical » / « Digital »), nombre de livres (« No books yet » si aucun) ; sous-titre récapitulatif « N libraries · M books » | — | Résumés de bibliothèques (LIB) : `Application/Libraries/Summaries/GetLibrarySummariesQueryHandler.cs`, `Persistence/Queries/LibraryReadService.cs:56-78` | `Web/Components/Pages/Libraries/LibrariesList.razor.cs:33-45`, `Web/Components/Pages/Libraries/LibrariesList.razor:15-18`, `Web/Models/LibraryCardViewModel.cs:22-29`, `Application/Libraries/Summaries/LibrarySummary.cs:15` |
| 2 | Liste vide | — | « No libraries yet » et invitation « Create a library to start adding your books », avec un bouton « New library » | — | — | `Web/Components/Pages/Libraries/LibrariesList.razor:28-35` |
| 3 | Liste | Ouvre une carte | Affiche les livres de la bibliothèque (UJ-FO-005) | — | — | `Web/Components/Pages/Libraries/LibrariesList.razor:40-47`, `Web/Components/Pages/Libraries/LibraryCard.razor:10` |
| 4 | Navigation | Choisit Statistics, Import, Feeds ou Settings | Ouvre la page correspondante ; la pastille de « Feeds » indique les arbitrages en attente. Sur téléphone la barre du bas remplace la barre latérale (« Stats » au lieu de « Statistics ») ; la barre latérale liste aussi chaque bibliothèque | BR-UI-004 | Comptage des arbitrages (FDI) | `Web/Components/Layout/NavBar.razor:15-55,59-87` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Chargement de la liste | Échec signalé par un message temporaire ; une liste déjà affichée est conservée, mais au premier chargement l'écran montre ensuite l'état vide « No libraries yet » (voir D-UI-007 proposée) | « Failed to load the libraries » | n/a | `Web/Components/Pages/Libraries/LibrariesList.razor.cs:36-44`, `Web/Components/Pages/Libraries/LibrariesList.razor:28-36` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Voir ses bibliothèques | Utilisateur connecté (ses propres bibliothèques) | `Web/Components/Pages/_Imports.razor:1`, `Persistence/Queries/LibraryReadService.cs:59` (filtre sur le propriétaire), `Application/Libraries/Summaries/GetLibrarySummariesQueryHandler.cs:12-15` |

## Liés

BR-UI-004, UJ-FO-003, UJ-FO-004, UJ-FO-005, D-UI-007.

Vérificateur 2026-10-17 : 4 étapes, 1 validation et 1 permission relues. Corrigé : étape 3, citation `Web/Components/Pages/Libraries/LibrariesList.razor:46-56` (qui pointait sur la feuille d'actions) remplacée par les lignes de la carte ; étape 1, ordre (nom puis identifiant), nombre de couvertures (3 au plus, celles de livres sans couverture ignorées) et libellés exacts ajoutés ; étape 4, citation `Web/Components/Layout/NavBar.razor:38-48` (« Feeds » seulement) étendue à toute la navigation. Accessibilité contrôlée : bouton « New library » avec `aria-label`, menu d'actions de carte avec `aria-label` « More actions for <nom> », couvertures décoratives masquées (`aria-hidden`). Confiance sur la pastille (BR-UI-004) inchangée : le calcul du compte est hors de ce lot.

## Commentaires du produit
