# Base de données et modèle de domaine — Usage SQL

**Composant :** `Domain/`, `Persistence/` · **Préfixe :** `DB` · **Scan :** [_sql-scan.md](_sql-scan.md) (généré par `kit/tools/sql_scan.py`) · **Dernière revue :** 2026-10-17

## Bases utilisées

| Base | Clé de configuration | Rôle | Source |
|---|---|---|---|
| PostgreSQL (une instance partagée par la préproduction et la production) | `ConnectionStrings` (valeur non consignée) | Magasin opérationnel unique | `Persistence/ProjectDependencyInjection.cs:27-29` |

Schéma SQL par défaut : aucun (PostgreSQL via EF Core, pas de `dbo`).

## Tables et vues accédées

Le scan ne trouve aucun SQL écrit à la main : tous les accès passent par EF Core. Les tables ci-dessous sont celles du modèle.

| Objet | Base | Accès (C/R/U/D) | Sens métier | Points d'entrée | Règles | Statut |
|---|---|---|---|---|---|---|
| Libraries | PostgreSQL | C, R, U, D | Bibliothèques | EP #1 | BR-DB-007, BR-DB-011 | Confirmé |
| Users | PostgreSQL | C, R | Utilisateurs | EP #2 | BR-DB-012 | Confirmé |
| Books | PostgreSQL | C, R, U, D | Livres | EP #3 | BR-DB-007, BR-DB-010 | Confirmé |
| PhysicalBooks | PostgreSQL | C, R, U, D | Livres physiques | EP #4 | BR-DB-013 | Confirmé |
| DigitalBooks | PostgreSQL | C, R, U, D | Livres numériques | EP #5 | BR-DB-010, BR-DB-013 | Confirmé |
| ReadingDates | PostgreSQL | C, R, U, D | Lectures datées | EP #6 | BR-DB-008 | Confirmé |
| IsbnBedethequeUrls | PostgreSQL | C, R | Cache des fiches Bédéthèque | EP #7 | BR-DB-006 | Confirmé |
| ImportJobs | PostgreSQL | C, R, U, D | Tâches d'import | EP #8 | BR-DB-001, BR-DB-002, BR-DB-011 | Confirmé |
| FeedImportDecisions | PostgreSQL | C, R, U, D | Décisions d'import par flux | EP #9 | BR-DB-003, BR-DB-004 | Confirmé |
| FeedImportDecisionEvents | PostgreSQL | C, R, D | Historique des décisions | EP #10 | BR-DB-009 | Confirmé |
| FeatureToggleOverrides | PostgreSQL | C, R, U, D | Fonctionnalités activables | EP #11 | BR-DB-005 | Confirmé |
| DataProtectionKeys | PostgreSQL | C, R | Clés de protection des cookies | EP #12 | BR-DB-014 | Confirmé |

## Procédures et fonctions stockées appelées

Aucune (confirmé par le scan : `_None found_`).

## Objets définis dans ce composant

Définis par les migrations EF Core (`Persistence/Migrations/`, 54 fichiers). Aucun DDL écrit à la main hormis les index de recherche approchée.

| Objet | Type | Sens métier | Lit | Écrit | Exécute | Statut |
|---|---|---|---|---|---|---|
| Index unique filtré sur le chemin d'origine des tâches d'import | Index | BR-DB-001 | — | — | — | Confirmé |
| Colonne `xmin` (jeton de concurrence) sur ImportJobs et FeedImportDecisions | Colonne système | BR-DB-002, BR-DB-004 | — | — | — | Confirmé |
| Index unique utilisateur + entrée + rang | Index | BR-DB-003 | — | — | — | Confirmé |
| Index unique sur la fonctionnalité | Index | BR-DB-005 | — | — | — | Confirmé |
| Index unique sur l'ISBN du cache Bédéthèque | Index | BR-DB-006 | — | — | — | Confirmé |
| Clés étrangères en cascade (livres → bibliothèque, lectures → livre, historique → décision) | Contraintes | BR-DB-007, BR-DB-008, BR-DB-009 | — | — | — | Confirmé |
| Index de recherche approchée (trigrammes) sur titre et série | Index | Technique : recherche de livres, sans règle métier | — | — | — | Confirmé |

## SQL dynamique (revue manuelle)

| Source | Ce qu'il construit | Objets touchés | Risque / note | Statut |
|---|---|---|---|---|
| `Persistence/Migrations/20260321120000_AddTrigramIndexesForBooksSearch.cs:13-21` | Création des index de trigrammes (SQL brut de migration) | Books | Nécessite l'extension de trigrammes de PostgreSQL ; non vérifié en production | Non vérifié |

## Dépendances inter-bases / serveurs liés

Aucune.

## Notes

- Aucun script DDL externe n'a été repéré : si des objets existent hors du dépôt, ils n'ont pas été analysés.
- Les requêtes de filtrage (LINQ) vivent dans les composants fonctionnels (`Persistence/Queries/`, `Persistence/Repositories/`) et seront traitées avec chacun d'eux.
