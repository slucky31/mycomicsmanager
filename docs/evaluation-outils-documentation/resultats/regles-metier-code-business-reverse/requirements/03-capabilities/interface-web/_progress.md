# Avancement — Interface web (interface-web)

- **Commit analysé :** `631720b92a58d61a0eb09343f7cbfc2f14a5f1ce` (identique à `projects/mycomics/project.md`)
- **Début :** 2026-10-17
- **Procédure :** `/re-ui front-end` (`kit/procedures/re-ui.md`)
- **Pas de plan de répartition** (`_run/plan.md` absent) : pas de réservation ni de journal d'exécution.

## Écrans (point d'entrée = un écran)

| # | Écran | Route | Garde d'accès | Parcours | Fait |
|---|---|---|---|---|---|
| 1 | Connexion | `/login` | Anonyme | UJ-FO-001 | [x] |
| 2 | Erreur | `/Error` | Anonyme | UJ-FO-001 | [x] |
| 3 | Liste des bibliothèques | `/`, `/libraries/list` | Connecté | UJ-FO-002, UJ-FO-004 | [x] |
| 4 | Création d'une bibliothèque | `/libraries/add` | Connecté | UJ-FO-003 | [x] |
| 5 | Modification d'une bibliothèque | `/libraries/{id}/edit` | Connecté | UJ-FO-004 | [x] |
| 6 | Livres d'une bibliothèque | `/libraries/{id}` | Connecté | UJ-FO-005, UJ-FO-014 | [x] |
| 7 | Scan du code-barres | `/books/add/scan` | Connecté | UJ-FO-006 | [x] |
| 8 | Saisie de l'ISBN | `/books/add/manual` | Connecté | UJ-FO-006 | [x] |
| 9 | Ajout d'un livre | `/books/add/form` | Connecté | UJ-FO-006 | [x] |
| 10 | Fiche d'un livre | `/books/{id}` | Connecté | UJ-FO-007, UJ-FO-010, UJ-FO-014 | [x] |
| 11 | Modification d'un livre | `/books/{id}/edit` | Connecté | UJ-FO-007 | [x] |
| 12 | Compléter un livre depuis le web | `/books/{id}/find-details` | Connecté | UJ-FO-008 | [x] |
| 13 | Lecteur | `/books/{id}/read` | Connecté | UJ-FO-009, UJ-FO-010 | [x] |
| 14 | Import de fichiers | `/import` | Connecté | UJ-FO-011 | [x] |
| 15 | Décisions d'import par flux | `/feed-imports` | Connecté | UJ-FO-012 | [x] |
| 16 | Statistiques | `/statistics` | Connecté | UJ-FO-013 | [x] |
| 17 | Paramètres | `/settings` | Connecté ; section Administration : rôle `Admin` | UJ-FO-001, UJ-FO-015 | [x] |
| 18 | Niveaux de journalisation | `/admin/log-levels` | Rôle `Admin` | UJ-FO-015 | [x] |
| 19 | Navigation (barre latérale / du bas) | (toutes) | Connecté | UJ-FO-002 | [x] |

Composants partagés relus avec leurs écrans : boîte de confirmation, boîte de déplacement, sélecteur de bibliothèque, formulaire de livre, carte d'import, feuille d'actions.

**Pas relus en détail (hors règles métier) :** feuilles de style, scripts du navigateur (`wwwroot/js`), détail du modèle de vue des décisions d'import (Q-UI-011).
