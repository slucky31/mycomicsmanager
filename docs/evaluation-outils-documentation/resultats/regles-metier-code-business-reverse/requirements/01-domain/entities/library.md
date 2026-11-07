# ENT-Library — Bibliothèque

**Statut :** Brouillon · **Composant propriétaire :** Bibliothèques (`bibliotheques`) · **Stocké dans :** `Libraries`

## Définition

Une **bibliothèque** est une collection nommée de livres appartenant à un utilisateur, avec une couleur, une icône et un ordre de tri par défaut. Elle est soit **physique** (livres papier), soit **numérique** (livres lisibles en ligne, avec un dossier de stockage et un dossier d'import sur le serveur). Une bibliothèque ne contient que des livres de son propre type.

## Identifiants

| Identifiant | Signification métier | Source |
|---|---|---|
| Identifiant de la bibliothèque (GUID) | Identité technique | `Domain/Libraries/Library.cs:65` |
| Nom (par utilisateur) | Doit être unique pour un utilisateur, sans tenir compte de la casse ni des espaces de bord (contrôle applicatif seulement) | `Persistence/Queries/LibraryReadService.cs:80-88`, `Application/Libraries/Create/CreateLibraryCommandHandler.cs:17-20` |
| Dossier de stockage | Nom dérivé : nom sans accents en majuscules ; dossier d'import : ce nom suivi de l'identifiant | `Domain/Libraries/Library.cs:21-23` |

## Attributs clés (métier uniquement)

| Attribut (nom métier) | Signification | Valeurs / format autorisés | Colonne / champ | Source |
|---|---|---|---|---|
| Nom | Intitulé affiché | Non vide, 200 caractères max | `Name` | `Domain/Libraries/Library.cs:43`, `Domain/Libraries/LibraryConstants.cs:5` |
| Couleur | Couleur d'affichage | Non vide, 7 caractères max (code `#RRGGBB`) | `Color` | `Domain/Libraries/Library.cs:48`, `Domain/Libraries/LibraryConstants.cs:6` |
| Icône | Icône d'affichage | Non vide, 100 caractères max | `Icon` | `Domain/Libraries/Library.cs:53`, `Domain/Libraries/LibraryConstants.cs:7` |
| Type de bibliothèque | Physique ou numérique ; fixé à la création, jamais modifié | Voir Données de référence | `BookType` | `Domain/Libraries/Library.cs:15`, `Domain/Libraries/Library.cs:38-41` |
| Propriétaire | Utilisateur propriétaire | Obligatoire (non vide) | `UserId` | `Domain/Libraries/Library.cs:17`, `Domain/Libraries/Library.cs:58-61` |
| Ordre de tri par défaut | Tri initial des livres | Valeur définie de la liste ; « plus récents d'abord » par défaut | `DefaultBookSortOrder` | `Domain/Libraries/Library.cs:19`, `Domain/Libraries/Library.cs:106-115` |

## Cycle de vie

Pas de statut. Création, modification (couleur, icône, tri, nom) et suppression par le propriétaire uniquement. Pour une bibliothèque numérique, créer / renommer / supprimer entraîne la création / le déplacement / la suppression de ses dossiers.

| État de départ | État d'arrivée | Déclencheur / qui | Règles | Source (où l'état change) |
|---|---|---|---|---|
| (création) | Active | Création par l'utilisateur ; doublon de nom refusé ; dossiers créés avant l'enregistrement si numérique | — | `Application/Libraries/Create/CreateLibraryCommandHandler.cs:17-47` |
| Active | Active (renommée) | Renommage par le propriétaire ; dossiers déplacés ; retour arrière du premier déplacement si le second échoue | — | `Application/Libraries/Update/UpdateLibraryCommandHandler.cs:62-96` |
| Active | (supprimée) | Suppression par le propriétaire ; dossiers supprimés avant l'enregistrement si numérique ; livres supprimés en cascade | BR-DB-007 | `Application/Libraries/Delete/DeleteLibraryCommandHandler.cs:18-44` |

## Relations

| Entité liée | Relation (métier) | Cardinalité |
|---|---|---|
| [ENT-Book](book.md) | La bibliothèque contient des livres de son type (physique ↔ livre physique, numérique ↔ livre numérique) | 1 bibliothèque → 0..n livres |
| [ENT-User](user.md) | La bibliothèque appartient à un utilisateur (référence logique, sans clé étrangère en base) | n bibliothèques → 1 utilisateur |
| [ENT-ImportJob](import-job.md) | Les archives importées sont rattachées à une bibliothèque numérique (référence logique, sans clé étrangère en base) | 1 bibliothèque → 0..n tâches |

## Contraintes et règles résidant en base

- BR-DB-007 : suppression en cascade des livres.
- BR-DB-011 : longueurs maximales du nom, de la couleur et de l'icône.
- Unicité du nom : contrôle applicatif seulement, voir D-DOM-001.
- Compatibilité de type livre / bibliothèque : contrôle applicatif seulement (`Domain/Libraries/Library.cs:117-130`).

## Données de référence

| Liste | Valeurs | Source |
|---|---|---|
| Type de bibliothèque | Physique (0), Numérique (1). Une ancienne valeur « Toutes » (2) a été convertie en Physique par migration puis supprimée | `Domain/Libraries/LibraryBookType.cs:3-7`, `Persistence/Migrations/20260227102924_RemoveIsDefaultFromLibraries.cs:13-14` |
| Ordre de tri des livres | Plus récents d'abord (0, défaut), Plus anciens d'abord (1), Série puis tome croissant (2) | `Domain/Libraries/BookSortOrder.cs:3-8` |

## Commentaires du produit
