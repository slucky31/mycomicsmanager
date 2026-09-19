# ADR-0005: TPT (Table-Per-Type) pour la hiérarchie `Book`

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

`Book` est une entité **abstraite** (`Domain/Books/Book.cs`), avec deux
types concrets : `PhysicalBook` et `DigitalBook`, chacun avec sa propre
factory (`PhysicalBook.Create(...)`, `DigitalBook.Create(...)`) retournant
`Result<T>`.

EF Core propose plusieurs stratégies de mapping objet-relationnel pour une
hiérarchie d'héritage : TPH (Table-Per-Hierarchy, une seule table avec
colonne discriminante), TPT (Table-Per-Type, une table par type avec clé
étrangère vers la table de base) et TPC (Table-Per-Concrete-type).

Le projet a choisi **TPT**, explicitement commenté dans le code :
`Persistence/ApplicationDbContext.cs` (`// TPT (Table Per Type) for Book
hierarchy`) avec `modelBuilder.Entity<PhysicalBook>().ToTable("PhysicalBooks")`
et `modelBuilder.Entity<DigitalBook>().ToTable("DigitalBooks")`. Les
colonnes communes (Serie, Title, ISBN, etc.) vivent dans la table `Books`,
les colonnes spécifiques à chaque type dans leur table respective.

## Options considérées

- **Option A — TPT (retenue)**
  - Pour : schéma normalisé, pas de colonnes `NULL` pour les attributs
    spécifiques à un type (ex. un champ propre à `DigitalBook` n'existe
    physiquement que dans `DigitalBooks`) ; contraintes NOT NULL utilisables
    par type ; migrations lisibles (une table = un type).
  - Contre : requêtes sur la hiérarchie complète (`Books` toutes confondues)
    nécessitent des `JOIN` entre `Books` et les tables filles, plus coûteux
    que TPH sur de gros volumes.
- **Option B — TPH (Table-Per-Hierarchy)**
  - Pour : une seule table, pas de `JOIN`, meilleures performances de
    lecture sur la hiérarchie complète.
  - Contre : toutes les colonnes spécifiques à `PhysicalBook` ou
    `DigitalBook` deviennent nullable dans une même table, colonne
    discriminante à gérer, schéma moins auto-documenté.
- **Option C — TPC (Table-Per-Concrete-type)**
  - Pour : pas de `JOIN`, chaque type a sa table complète et autonome.
  - Contre : duplication des colonnes communes entre `PhysicalBooks` et
    `DigitalBooks` ; clés primaires globalement uniques plus délicates à
    garantir ; support EF Core plus récent et moins mature que TPT/TPH.

## Décision

On mappe la hiérarchie `Book` en **TPT** : une table `Books` pour les
colonnes communes, `PhysicalBooks` et `DigitalBooks` pour les colonnes
spécifiques à chaque type concret.

## Conséquences

### Positives

- Schéma relationnel propre : pas de colonnes nullables "parce qu'elles
  n'existent que pour l'autre type".
- Contraintes NOT NULL et types de colonnes adaptés à chaque sous-type.
- Le mapping reflète directement la modélisation du domaine (`Book`
  abstrait + deux types concrets), ce qui facilite la lecture croisée
  code/schéma.

### Négatives

- Toute requête qui a besoin des colonnes spécifiques (ex. lister tous les
  `PhysicalBook` avec leurs champs propres) doit joindre `Books` et
  `PhysicalBooks` — à surveiller si le volume de livres devient important
  (actuellement un usage mono-utilisateur, cf. [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md),
  donc pas un problème de performance identifié à ce jour).
- Ajouter un troisième type concret à la hiérarchie `Book` nécessiterait une
  nouvelle table + migration, à faire consciemment.

### Couches impactées

- [x] Persistence (`ApplicationDbContext`, migrations TPT)
- [x] Domain (hiérarchie `Book`/`PhysicalBook`/`DigitalBook`)
- [ ] Application
- [ ] Web

## Liens

- `Persistence/ApplicationDbContext.cs` (configuration TPT)
- `Domain/Books/Book.cs`, `Domain/Books/PhysicalBook.cs`, `Domain/Books/DigitalBook.cs`
- Migration `20260227082728_AddLibrariesAndBookTypes` (création des tables PhysicalBooks/DigitalBooks en TPT)
