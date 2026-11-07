# ENT-ReadingDate — Date de lecture

**Statut :** Brouillon · **Composant propriétaire :** Livres (`livres`) · **Stocké dans :** `ReadingDates`

## Définition

Une **date de lecture** enregistre qu'un utilisateur a lu un livre à une date donnée, avec une note. Un livre peut avoir plusieurs lectures (relectures).

## Identifiants

| Identifiant | Signification métier | Source |
|---|---|---|
| Identifiant de la lecture (GUID) | Identité technique | `Domain/Books/ReadingDate.cs:17` |

## Attributs clés (métier uniquement)

| Attribut (nom métier) | Signification | Valeurs / format autorisés | Colonne / champ | Source |
|---|---|---|---|---|
| Date | Moment de la lecture | Date et heure ; fixée à « maintenant » (UTC) par les écrans | `Date` | `Domain/Books/ReadingDate.cs:7`, `Application/Books/AddReadingDate/AddReadingDateCommandHandler.cs:41` |
| Note | Appréciation de la lecture | Entier de 1 à 5 à l'ajout ; 0 à la création d'un livre signifie « non lu » et ne crée aucune lecture | `Rating` | `Application/Books/AddReadingDate/AddReadingDateCommandHandler.cs:24-27`, `Application/Books/Create/CreateBookCommandHandler.cs:30-33`, `Application/Books/Create/CreateBookCommandHandler.cs:77-81` |
| Livre | Livre lu | Obligatoire | `BookId` | `Domain/Books/ReadingDate.cs:11` |

## Cycle de vie

Pas de statut. Une lecture est créée (création d'un livre noté, ajout depuis la fiche ou le lecteur), supprimée avec `RemoveReadingDate` (`Application/Books/DeleteReadingDate/DeleteReadingDateCommandHandler.cs:31`) ou supprimée avec son livre. La méthode de modification (`Domain/Books/ReadingDate.cs:25-29`) n'est appelée que par le script d'import en masse (`scripts/ImportBooks/Program.cs:195`) : aucune modification depuis l'application web repérée, voir Q-DOM-005.

| État de départ | État d'arrivée | Déclencheur / qui | Règles | Source (où l'état change) |
|---|---|---|---|---|
| (création) | Lecture enregistrée | Création d'un livre avec une note > 0 | — | `Application/Books/Create/CreateBookCommandHandler.cs:78-81` |
| (création) | Lecture enregistrée | Ajout d'une lecture sur un livre existant | — | `Application/Books/AddReadingDate/AddReadingDateCommandHandler.cs:41-43` |
| Lecture enregistrée | (supprimée) | Suppression par l'utilisateur ou suppression du livre | BR-DB-008 | `Application/Books/DeleteReadingDate/DeleteReadingDateCommandHandler.cs:31`, `Persistence/ApplicationDbContext.cs:60-64` |

## Relations

| Entité liée | Relation (métier) | Cardinalité |
|---|---|---|
| [ENT-Book](book.md) | La lecture appartient à un livre | n lectures → 1 livre |

## Contraintes et règles résidant en base

- BR-DB-008 : suppression en cascade avec le livre.
- La note n'est validée que par les gestionnaires (1 à 5, ou 0 à la création) ; le domaine et la base n'imposent aucune borne (`Domain/Books/ReadingDate.cs:13-23`). Voir Q-DOM-005.

## Données de référence

Aucune.

## Commentaires du produit
