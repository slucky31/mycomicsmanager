# ADR-0010: Architecture en couches (Clean Architecture) avec garde-fous NetArchTest

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Le projet est structuré en quatre couches : `Domain/`, `Application/`,
`Persistence/`, `Web/` (cf. `CLAUDE.md`, section "Architecture"), suivant
les principes de Clean Architecture : `Domain` ne dépend de rien, `Application`
ne dépend que de `Domain`, `Persistence` implémente les interfaces
définies par `Application` sans que `Web` ne dépende directement d'EF Core,
et `Web` (composition root) assemble le tout.

Ce qui distingue ce projet d'une simple convention de nommage de dossiers :
ces règles sont **vérifiées automatiquement** par des tests dans
`tests/Architecture.Tests/ArchitectureTests.cs`, en utilisant **NetArchTest**.
Les règles imposées incluent notamment :

- Sens de dépendance entre couches (Domain → rien, Application → Domain
  uniquement, Persistence → pas de dépendance vers Web).
- Conventions de nommage CQRS : les classes doivent se terminer par
  `Command`, `Query` ou `Handler` selon leur rôle.
- Les handlers doivent être `sealed`.
- Les interfaces définies dans `Application` doivent commencer par `I`.

Ces règles sont donc auto-appliquées à chaque build/test (`dotnet test`),
et non laissées à la seule discipline des revues de code.

## Options considérées

- **Option A — Clean Architecture avec garde-fous automatisés NetArchTest (retenue)**
  - Pour : les violations de couche (ex. `Web` qui importerait directement
    un type EF Core, ou `Domain` qui dépendrait d'`Application`) sont
    détectées immédiatement en CI, pas seulement en revue de code humaine ;
    les conventions de nommage CQRS restent cohérentes sur tout le projet
    sans effort de vigilance manuelle continue.
  - Contre : coût de mise en place et de maintenance des règles
    NetArchTest ; une règle mal calibrée peut bloquer un refactoring
    légitime et nécessiter d'ajuster le test en plus du code.
- **Option B — Architecture en couches sans vérification automatisée**
  - Pour : plus simple à mettre en place initialement, pas de dépendance
    supplémentaire (NetArchTest).
  - Contre : rien n'empêche techniquement une dépendance incorrecte
    (ex. `Domain` référençant EF Core) de s'introduire au fil du temps,
    seule la vigilance en revue de code protège les frontières de couches —
    fragile sur la durée, surtout avec des contributions générées par IA.
- **Option C — Vertical Slice Architecture (pas de couches horizontales strictes)**
  - Pour : chaque fonctionnalité regroupe tout son code (UI, logique, accès
    données) dans un seul dossier, réduit le nombre de fichiers à toucher
    pour une fonctionnalité donnée.
  - Contre : abandon du découpage Domain/Application/Persistence/Web déjà
    en place dans tout le projet existant ; changement d'architecture trop
    coûteux et non justifié pour un projet déjà avancé.

## Décision

On conserve l'architecture en couches Domain → Application → Persistence →
Web, avec les règles de dépendance et de nommage **vérifiées
automatiquement** par NetArchTest dans `tests/Architecture.Tests/ArchitectureTests.cs`,
exécuté à chaque `dotnet test`.

## Conséquences

### Positives

- Les frontières architecturales sont protégées par une vérification
  automatique, pas uniquement par la discipline humaine — particulièrement
  utile quand une partie du code est générée ou modifiée par un agent IA.
- Les conventions de nommage CQRS (`*Command`, `*Query`, `*Handler`) restent
  homogènes dans tout le projet sans effort de relecture manuelle dédié.
- Toute violation architecturale casse la suite de tests, donc la CI, avant
  d'atteindre `main`.

### Négatives

- Les règles NetArchTest doivent être maintenues en même temps que
  l'architecture évolue ; un refactoring légitime qui change une frontière
  de couche nécessite de mettre à jour `ArchitectureTests.cs` en
  conséquence.
- Un développeur ou un agent qui ignore l'existence de ces tests peut être
  surpris par un échec de build qui ne pointe pas directement vers son
  code métier mais vers une règle de convention.

### Couches impactées

- [x] Domain
- [x] Application
- [x] Persistence
- [x] Web

## Liens

- `tests/Architecture.Tests/ArchitectureTests.cs` (règles NetArchTest)
- `CLAUDE.md` — section "Architecture" et "Layer Rules"
- [ADR-0002](0002-cqrs-maison-sans-mediatr.md) — conventions CQRS vérifiées par ces mêmes tests
