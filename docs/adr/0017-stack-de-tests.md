# ADR-0017: Stack de tests — xUnit v3, NSubstitute, AwesomeAssertions, bUnit

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Le projet a besoin d'une stack de tests cohérente sur cinq projets
(`Domain.UnitTests`, `Application.UnitTests`, `Persistence.Integration.Tests`,
`Architecture.Tests`, `Web.Tests`). Les choix retenus, tous déclarés dans
`Directory.Packages.props` (cf. [ADR-0012](0012-central-package-management.md)) :

- **xUnit v3** (`xunit.v3`) comme framework de test, plutôt que xUnit v2.
- **NSubstitute** pour le mocking, plutôt que Moq.
- **AwesomeAssertions** pour les assertions fluides, plutôt que
  FluentAssertions.
- **bUnit** pour les tests de composants Blazor (`Web.Tests`).
- **MockQueryable.NSubstitute** pour simuler des `IQueryable` EF Core dans
  les tests unitaires sans base de données réelle.
- **NetArchTest.Rules** pour les tests d'architecture (cf. [ADR-0010](0010-architecture-en-couches-netarchtest.md)).

Le choix d'AwesomeAssertions plutôt que FluentAssertions correspond à une
tendance plus large de l'écosystème .NET suite au changement de licence de
FluentAssertions (version 8+, payante pour un usage commercial) —
AwesomeAssertions est un fork resté sous licence libre avec une API quasi
identique.

**Point important sur l'usage actuel** : les tests de composants **bUnit
sont en pause**. La dépendance et l'outillage sont en place
(`Web.Tests`), mais l'écriture/maintenance active de tests de composants
Blazor n'est pas poursuivie pour le moment — un choix assumé de
priorisation plutôt qu'un oubli. Les autres couches de tests (Domain,
Application, Persistence, Architecture) restent actives.

## Options considérées

- **Option A — xUnit v3 + NSubstitute + AwesomeAssertions + bUnit (retenue)**
  - Pour : stack cohérente et alignée sur les tendances actuelles de
    l'écosystème .NET ; AwesomeAssertions évite la question de licence de
    FluentAssertions ; NSubstitute a une API plus concise que Moq pour les
    besoins du projet ; bUnit reste l'outil de référence pour tester des
    composants Blazor le jour où cette pratique reprend.
  - Contre : xUnit v3 est plus récent et moins documenté par la communauté
    que xUnit v2 ; AwesomeAssertions, étant un fork plus jeune, a un
    écosystème de plugins/documentation moins large que FluentAssertions.
- **Option B — xUnit v2 + Moq + FluentAssertions (stack "historique" .NET)**
  - Pour : stack la plus documentée et la plus connue de l'écosystème .NET.
  - Contre : FluentAssertions v8+ nécessite une licence commerciale ; Moq a
    eu ses propres controverses de licence/télémétrie par le passé.
- **Option C — MSTest ou NUnit à la place de xUnit**
  - Pour : alternatives valables et bien supportées.
  - Contre : aucun signal dans le projet indiquant un besoin spécifique à
    MSTest/NUnit ; xUnit reste le standard de facto pour les nouveaux
    projets .NET.

## Décision

On utilise **xUnit v3** comme framework de test, **NSubstitute** pour le
mocking, **AwesomeAssertions** pour les assertions, et **bUnit** pour les
tests de composants Blazor — avec les tests bUnit actuellement **en pause**
(infrastructure conservée, écriture non active).

## Conséquences

### Positives

- Pas de dépendance à une bibliothèque de test sous licence commerciale
  (AwesomeAssertions).
- Stack homogène sur les cinq projets de tests, avec des conventions
  communes (`CLAUDE.md` documente déjà la convention de nommage
  `MethodName_Should_DoExpectation_WhenCondition`).

### Négatives

- Les tests bUnit étant en pause, la couverture des composants Blazor
  (`Web/Components/`) ne progresse pas activement : une régression
  d'interface (logique dans un `.razor.cs`) peut ne pas être détectée par
  la suite de tests. Les autres couches (handlers, entités, règles
  d'architecture) restent couvertes.
- xUnit v3 étant plus récent, certains outils tiers ou tutoriels en ligne
  peuvent encore cibler xUnit v2 — à garder en tête en cas de blocage
  d'outillage.

### Couches impactées

- [x] Domain (`Domain.UnitTests`)
- [x] Application (`Application.UnitTests`)
- [x] Persistence (`Persistence.Integration.Tests`)
- [x] Web (`Web.Tests`, bUnit en pause)

## Liens

- `Directory.Packages.props` (versions xUnit v3, NSubstitute, AwesomeAssertions, bUnit, MockQueryable.NSubstitute)
- `CLAUDE.md` — tableau des projets de tests et convention de nommage
- [ADR-0010](0010-architecture-en-couches-netarchtest.md) — NetArchTest, testé par la même stack
- [ADR-0012](0012-central-package-management.md) — versions centralisées de ces packages
