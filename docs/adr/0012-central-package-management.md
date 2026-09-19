# ADR-0012: Central Package Management pour les versions de packages NuGet

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Le projet est composé de plusieurs `.csproj` (Domain, Application,
Persistence, Web, et cinq projets de tests). Sans gestion centralisée, la
version d'un même package (ex. `xunit.v3`, `NSubstitute`) pourrait diverger
d'un projet à l'autre, avec des incohérences difficiles à repérer.

Le projet active **Central Package Management** (CPM) via
`Directory.Packages.props` à la racine : `ManagePackageVersionsCentrally=true`,
avec `CentralPackageTransitivePinningEnabled=false`. Toutes les versions de
packages (EF Core, Npgsql, Scrutor, Hangfire, Auth0, MudBlazor,
FluentValidation, la stack de tests, etc.) sont déclarées une seule fois
dans ce fichier ; chaque `.csproj` référence les packages sans version
(`<PackageReference Include="..." />`).

## Options considérées

- **Option A — Central Package Management (retenue)**
  - Pour : une seule source de vérité pour chaque version de package,
    élimine les divergences de version entre projets ; upgrade d'un
    package en un seul endroit ; intégré nativement au SDK .NET (pas
    d'outil tiers).
  - Contre : `Directory.Packages.props` devient un fichier central que tout
    changement de dépendance doit traverser, potentiel point de friction
    sur un gros monorepo (non significatif ici, taille du projet actuelle).
- **Option B — Versions déclarées par projet (`PackageReference` avec version explicite)**
  - Pour : chaque projet est autonome, pas de fichier central à connaître.
  - Contre : risque réel de divergence de version entre projets au fil du
    temps (ex. deux versions différentes de `NSubstitute` dans deux
    projets de test), source de bugs difficiles à diagnostiquer
    ("ça marche dans un projet mais pas l'autre").
- **Option C — Paket (gestionnaire de dépendances tiers)**
  - Pour : gestion de dépendances avancée (lock file strict, résolution
    déterministe).
  - Contre : outil supplémentaire à installer et maintenir pour un besoin
    déjà couvert nativement par CPM depuis le SDK .NET ; aucun signal dans
    le projet indiquant un besoin au-delà de ce que CPM fournit.

## Décision

On active **Central Package Management** via `Directory.Packages.props` à
la racine de la solution pour toutes les versions de packages NuGet.

## Conséquences

### Positives

- Impossible d'avoir deux projets référençant des versions différentes du
  même package par accident.
- Mise à jour d'un package (ex. lors d'une PR Renovate) se fait en un seul
  endroit, quel que soit le nombre de projets qui l'utilisent.
- Visibilité complète de la surface de dépendances du projet dans un seul
  fichier, utile pour un audit de sécurité ou de licence.

### Négatives

- Un `.csproj` ne peut pas facilement utiliser une version différente d'un
  package pour une raison ponctuelle (ex. compatibilité) sans passer par un
  override explicite (`VersionOverride`) — cas non rencontré à ce jour dans
  le projet.

### Couches impactées

- [x] Domain
- [x] Application
- [x] Persistence
- [x] Web
- [x] tests (tous les projets de test)

## Liens

- `Directory.Packages.props`
- `.github/renovate.json` — les mises à jour automatiques de dépendances passent par ce fichier central
