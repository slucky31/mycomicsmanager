# Base de données et modèle de domaine — Capacité

**Composant :** `Domain/`, `Persistence/` · **Préfixe :** `DB` · **Technologie :** C# / EF Core / PostgreSQL · **Dernière extraction :** 2026-10-17 · **Statut :** Brouillon

## Objet

La base conserve les livres, bibliothèques, utilisateurs, tâches d'import et décisions d'import par flux de MyComicsManager. Le modèle est géré par migrations EF Core ; la base ne contient ni procédure stockée, ni fonction, ni vue, ni déclencheur : la logique résidant en base se limite aux contraintes (unicités, cascades, longueurs, jetons de concurrence) décrites dans [rules.md](rules.md).

## Responsabilités

- Garantir l'unicité d'un import non terminé par fichier source (BR-DB-001), d'une décision par livre d'article (BR-DB-003), d'une surcharge par fonctionnalité (BR-DB-005) et d'une URL Bédéthèque par ISBN (BR-DB-006).
- Propager les suppressions (bibliothèque → livres → lectures ; décision → historique) : BR-DB-007 à BR-DB-009.
- Détecter les modifications concurrentes (BR-DB-002, BR-DB-004).
- Limiter la taille des champs (BR-DB-010, BR-DB-011) et horodater les enregistrements (BR-DB-012).
- Conserver les clés de protection des sessions (BR-DB-014).

## Hors périmètre

- Les règles de gestion (compatibilité livre / bibliothèque, unicité du nom, de l'ISBN, notes) vivent dans l'application : voir D-DOM-001 à D-DOM-003.
- Aucun objet de base hors du dépôt n'a été repéré (pas de script DDL externe) ; la base de production est auto-hébergée, seules les migrations du dépôt font foi.

## Points d'entrée

Ici, un point d'entrée est un groupe de tables accédé par l'application (référencé par « Utilisé dans » des règles).

| # | Type | Nom | Sens métier | Appelé par | Règles | Source |
|---|---|---|---|---|---|---|
| 1 | Table | `Libraries` | Bibliothèques des utilisateurs | Bibliothèques | BR-DB-007, BR-DB-011 | `Persistence/ApplicationDbContext.cs:45-49` |
| 2 | Table | `Users` | Utilisateurs | Utilisateurs et authentification | BR-DB-012 | `Persistence/ApplicationDbContext.cs:51` |
| 3 | Table | `Books` | Livres (racine) | Livres | BR-DB-007, BR-DB-008, BR-DB-010 | `Persistence/ApplicationDbContext.cs:53-70` |
| 4 | Table | `PhysicalBooks` | Livres physiques | Livres | BR-DB-013 | `Persistence/ApplicationDbContext.cs:73` |
| 5 | Table | `DigitalBooks` | Livres numériques | Livres, Import de fichiers | BR-DB-010, BR-DB-013 | `Persistence/ApplicationDbContext.cs:74-78` |
| 6 | Table | `ReadingDates` | Lectures datées et notées | Livres | BR-DB-008 | `Persistence/ApplicationDbContext.cs:99` |
| 7 | Table | `IsbnBedethequeUrls` | Cache des fiches Bédéthèque | Recherche de métadonnées | BR-DB-006 | `Persistence/ApplicationDbContext.cs:101-104` |
| 8 | Table | `ImportJobs` | Tâches d'import de fichiers | Import de fichiers | BR-DB-001, BR-DB-002, BR-DB-011 | `Persistence/ApplicationDbContext.cs:80-96` |
| 9 | Table | `FeedImportDecisions` | Décisions d'import par flux | Import par flux | BR-DB-003, BR-DB-004 | `Persistence/ApplicationDbContext.cs:115-136` |
| 10 | Table | `FeedImportDecisionEvents` | Historique des décisions | Import par flux | BR-DB-009 | `Persistence/ApplicationDbContext.cs:127-138` |
| 11 | Table | `FeatureToggleOverrides` | Fonctionnalités activées / désactivées par l'administrateur | Paramètres et administration | BR-DB-005 | `Persistence/ApplicationDbContext.cs:106-108` |
| 12 | Table | `DataProtectionKeys` | Clés de protection des cookies | Utilisateurs et authentification | BR-DB-014 | `Persistence/ApplicationDbContext.cs:37-38` |

## Données possédées / touchées

| Entité | Accès | Notes |
|---|---|---|
| [ENT-Book](../../01-domain/entities/book.md) | Propriétaire | Héritage « une table par type » (ADR 0005) |
| [ENT-ReadingDate](../../01-domain/entities/reading-date.md) | Propriétaire | |
| [ENT-Library](../../01-domain/entities/library.md) | Propriétaire | |
| [ENT-User](../../01-domain/entities/user.md) | Propriétaire | |
| [ENT-ImportJob](../../01-domain/entities/import-job.md) | Propriétaire | Cycle de vie à 8 statuts |
| [ENT-FeedImportDecision](../../01-domain/entities/feed-import-decision.md) | Propriétaire | Cycle de vie à 9 statuts |
| [ENT-FeatureToggleOverride](../../01-domain/entities/feature-toggle-override.md) | Propriétaire | |

## Tables techniques

Non retenues comme entités métier :

| Table | Rôle technique | Source |
|---|---|---|
| `IsbnBedethequeUrls` | Cache des adresses de fiches Bédéthèque par ISBN (règle BR-DB-006) | `Persistence/ApplicationDbContext.cs:101-104` |
| `FeedImportDecisionEvents` | Historique des changements de statut, décrit dans l'entité parente | `Persistence/ApplicationDbContext.cs:138` |
| `DataProtectionKeys` | Anneau de clés de la plateforme pour les cookies d'authentification | `Persistence/ApplicationDbContext.cs:37-38` |
| `__EFMigrationsHistory` | Historique des migrations EF Core (54 fichiers dans `Persistence/Migrations/`) | `Persistence/Migrations/` |

## Dépendances

| Dépend de | Raison | Comment | Source |
|---|---|---|---|
| PostgreSQL | Stockage | EF Core (Npgsql) | `Persistence/ProjectDependencyInjection.cs` |

## Configuration à portée métier

| Clé | Sens métier | Valeur dans le dépôt | Valeur de production connue ? |
|---|---|---|---|
| `ConnectionStrings` | Chaîne de connexion (secret, non consignée) | Non consignée | Non |

## Règles métier

Voir [rules.md](rules.md) : 14 règles (Élevée 10 / Moyenne 4 / Faible 0).

## Questions ouvertes et anomalies suspectées

Voir [../../01-domain/_questions.md](../../01-domain/_questions.md) et [../../01-domain/_defects.md](../../01-domain/_defects.md).
