# ADR-0002: CQRS maison sans MediatR

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Le projet suit un pattern CQRS : chaque cas d'usage est une `Command` ou une
`Query` traitée par un `Handler` dédié, dans
`Application/{Feature}/{Operation}/`.

MediatR est la bibliothèque de référence pour implémenter ce pattern en
.NET, et `Analysis.md` avait à un moment recommandé de l'adopter. Ce n'est
pas ce qui a été fait : le projet définit ses propres abstractions dans
`Application/Abstractions/Messaging/` (`ICommand`, `ICommand<T>`,
`IQuery<T>`, `ICommandHandler<T>`, `IQueryHandler<T, R>`), et l'enregistrement
des handlers dans le conteneur DI se fait via **Scrutor**
(`Application/ApplicationDependencyInjection.cs`, `services.Scan(...)`)
plutôt que via le scanner d'assembly intégré de MediatR.

Sans ADR, un contributeur qui connaît MediatR peut se demander pourquoi le
projet ne l'utilise pas, ou proposer de l'ajouter sans connaître le contexte
de cette décision.

## Options considérées

- **Option A — Abstractions maison + Scrutor (retenue)**
  - Pour : zéro dépendance externe pour un mécanisme somme toute simple
    (mapper une requête vers un handler) ; contrôle total sur les
    interfaces (`ICommand<T>`, `IQueryHandler<T,R>`) et sur les règles
    imposées par `Architecture.Tests` (handlers `sealed`, suffixes de nommage
    stricts) ; pas de comportement "magique" à documenter (pipeline
    behaviors, `IRequestHandler` générique, etc.).
  - Contre : pas de pipeline de comportements prêt à l'emploi (logging,
    validation, transactions) — à réimplémenter manuellement si besoin ;
    moins familier pour un nouveau contributeur habitué à MediatR.
- **Option B — MediatR**
  - Pour : bibliothèque standard, largement connue, pipeline behaviors
    (validation, logging) disponibles out-of-the-box.
  - Contre : dépendance supplémentaire pour un besoin déjà couvert
    simplement par Scrutor + interfaces custom ; changement de licence de
    MediatR (v10+, commercial pour usage professionnel) à surveiller — non
    bloquant ici (projet perso) mais un facteur qui a pu peser contre
    l'adoption.

## Décision

On garde les abstractions maison (`ICommand`, `ICommand<T>`, `IQuery<T>`,
`ICommandHandler<T>`, `IQueryHandler<T,R>`) définies dans
`Application/Abstractions/Messaging/`, avec enregistrement automatique des
handlers via **Scrutor**. On n'adopte pas MediatR.

## Conséquences

### Positives

- Aucune dépendance externe pour le routage commande/requête → handler.
- Les règles architecturales (nommage `*Command`/`*Query`/`*Handler`,
  handlers `sealed`, interfaces `Application` préfixées `I`) sont définies
  et vérifiées par le projet lui-même via `tests/Architecture.Tests/ArchitectureTests.cs`
  (NetArchTest), sans dépendre des conventions d'une bibliothèque tierce.
- Le code des handlers reste simple à lire pour quiconque connaît le pattern
  CQRS, même sans connaître MediatR.

### Négatives

- Pas de pipeline behaviors génériques : la validation d'entrée, le logging,
  etc. doivent être codés dans chaque handler (cf. règle CLAUDE.md "CQRS
  Handlers — Input Validation") plutôt que factorisés dans un middleware
  partagé.
- Si le besoin de pipeline behaviors (transactions automatiques, retry,
  cross-cutting logging) devient important, il faudra soit les construire
  à la main au-dessus des abstractions actuelles, soit revisiter cette ADR.

### Couches impactées

- [x] Application (abstractions `ICommand`/`IQuery`/Handlers, DI Scrutor)
- [ ] Domain
- [ ] Persistence
- [ ] Web

## Liens

- `Application/Abstractions/Messaging/ICommand.cs`, `IQuery.cs`,
  `ICommandHandler.cs`, `IQueryHandler.cs`
- `Application/ApplicationDependencyInjection.cs` (enregistrement Scrutor)
- `tests/Architecture.Tests/ArchitectureTests.cs` (règles NetArchTest sur le
  nommage et la structure des handlers)
- `CLAUDE.md` — section "CQRS Pattern"
