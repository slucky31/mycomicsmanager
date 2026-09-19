# Architecture Decision Records

Ce dossier contient les Architecture Decision Records (ADR) de MyComicsManager :
un enregistrement court de chaque décision technique structurante, pourquoi elle
a été prise, et ce qu'elle implique.

## Pourquoi

Le projet est déjà bien avancé et de nombreuses décisions importantes (CQRS
maison, `Result<T>`, hébergement PostgreSQL, cible de déploiement Raspberry Pi,
etc.) existent uniquement dans le code ou dans la tête du mainteneur. Les ADR
formalisent ces choix pour qu'ils restent compréhensibles plus tard (par vous,
ou par un agent/contributeur qui reprend le projet).

## Statuts

- **Proposée** — en discussion, pas encore actée
- **Acceptée** — décision prise, en vigueur
- **Rejetée** — envisagée puis écartée (à garder : évite de la reproposer sans contexte)
- **Dépréciée** — n'est plus d'actualité mais n'a pas de remplaçante formelle
- **Remplacée par ADR-XXXX** — une décision plus récente l'a supplantée

## Créer une nouvelle ADR

1. Copier `template.md` vers `NNNN-titre-en-kebab-case.md` (numéro suivant, 4 chiffres)
2. Remplir le template
3. Ajouter une ligne dans l'index ci-dessous
4. Committer (une ADR peut être son propre commit `docs(adr): ...`)

Ne jamais modifier une ADR **Acceptée** rétroactivement pour refléter un
changement d'avis : en écrire une nouvelle qui la remplace, et mettre à jour le
statut de l'ancienne.

Note : le projet utilise déjà des commentaires `ponytail:` dans le code pour
noter des compromis techniques ponctuels acceptés (ex. `Persistence/ApplicationDbContext.cs`,
`Web/Program.cs`). Ces commentaires restent adaptés aux micro-décisions
locales ; les ADR sont réservées aux décisions structurantes qui affectent
plusieurs fichiers, couches, ou le choix d'une techno/convention.

## Index

| ADR | Titre | Statut | Date |
| --- | ----- | ------ | ---- |
| [0001](0001-deploiement-raspberry-pi-4-self-hosted.md) | Cible de déploiement — Raspberry Pi 4 self-hosted (arm64) | Acceptée | 2026-09-19 |
| [0002](0002-cqrs-maison-sans-mediatr.md) | CQRS maison sans MediatR | Acceptée | 2026-09-19 |
| [0003](0003-postgresql-self-hosted-au-lieu-de-neon.md) | PostgreSQL self-hosted (instance unique staging/prod) au lieu de Neon | Acceptée | 2026-09-19 |
| [0004](0004-result-pattern-pour-gestion-erreurs.md) | `Result<T>` / `TError` pour la gestion d'erreurs métier | Acceptée | 2026-09-19 |
| [0005](0005-tpt-pour-hierarchie-book.md) | TPT (Table-Per-Type) pour la hiérarchie `Book` | Acceptée | 2026-09-19 |
| [0006](0006-blazor-server-plutot-que-webassembly.md) | Blazor Server (plutôt que WebAssembly) | Acceptée | 2026-09-19 |
| [0007](0007-mudblazor-comme-librairie-de-composants.md) | MudBlazor comme librairie de composants UI | Acceptée | 2026-09-19 |
| [0008](0008-auth0-pour-authentification.md) | Auth0 pour l'authentification | Acceptée | 2026-09-19 |
| [0009](0009-serilog-pour-le-logging.md) | Serilog pour le logging applicatif | Acceptée | 2026-09-19 |
| [0010](0010-architecture-en-couches-netarchtest.md) | Architecture en couches (Clean Architecture) avec garde-fous NetArchTest | Acceptée | 2026-09-19 |
| [0011](0011-code-behind-et-css-isolation.md) | Convention code-behind (`.razor.cs`) + CSS isolation (`.razor.css`) | Acceptée | 2026-09-19 |
| [0012](0012-central-package-management.md) | Central Package Management pour les versions de packages NuGet | Acceptée | 2026-09-19 |
| [0013](0013-outils-qualite-et-securite.md) | Outils qualité/sécurité — SonarCloud + CodeQL + Renovate (sans DeepSource) | Acceptée | 2026-09-19 |
| [0014](0014-validation-a-plusieurs-niveaux.md) | Validation à plusieurs niveaux (FluentValidation + handler + domaine) | Acceptée | 2026-09-19 |
| [0015](0015-garde-fou-ssrf-appels-sortants.md) | Garde-fou SSRF (allow-list) sur tous les appels HTTP sortants | Acceptée | 2026-09-19 |
| [0016](0016-hangfire-jobs-arriere-plan.md) | Hangfire + stockage PostgreSQL pour les jobs en arrière-plan | Acceptée | 2026-09-19 |
| [0017](0017-stack-de-tests.md) | Stack de tests — xUnit v3, NSubstitute, AwesomeAssertions, bUnit (bUnit en pause) | Acceptée | 2026-09-19 |
| [0018](0018-conventional-commits-versionize.md) | Conventional Commits + Versionize pour le versioning automatique | Acceptée | 2026-09-19 |

## Backlog de candidates

Liste des décisions identifiées dans le code, à documenter au fur et à mesure.
Cocher/déplacer vers l'index au fur et à mesure qu'une ADR est rédigée.

### Rétroactives (décision déjà en place)

- [x] Architecture en couches (Domain/Application/Persistence/Web) + garde-fous NetArchTest — [ADR-0010](0010-architecture-en-couches-netarchtest.md)
- [x] CQRS maison sans MediatR (`Application/Abstractions/Messaging`) — [ADR-0002](0002-cqrs-maison-sans-mediatr.md)
- [x] `Result<T>` / `TError` pour la gestion d'erreurs métier (vs exceptions) — [ADR-0004](0004-result-pattern-pour-gestion-erreurs.md)
- [x] TPT (Table-Per-Type) pour la hiérarchie `Book`/`PhysicalBook`/`DigitalBook` — [ADR-0005](0005-tpt-pour-hierarchie-book.md)
- [x] PostgreSQL self-hosted, une seule instance partagée staging/prod (migration depuis Neon) — [ADR-0003](0003-postgresql-self-hosted-au-lieu-de-neon.md)
- [x] Blazor Server (plutôt que WebAssembly) — [ADR-0006](0006-blazor-server-plutot-que-webassembly.md)
- [x] MudBlazor comme librairie de composants — [ADR-0007](0007-mudblazor-comme-librairie-de-composants.md)
- [x] Convention code-behind (`.razor.cs`) + CSS isolation (`.razor.css`) — [ADR-0011](0011-code-behind-et-css-isolation.md)
- [x] Auth0 pour l'authentification + `CustomAuthenticationStateProvider` — [ADR-0008](0008-auth0-pour-authentification.md)
- [x] Central Package Management (`Directory.Packages.props`) — [ADR-0012](0012-central-package-management.md)
- [x] Validation à plusieurs niveaux : FluentValidation (UI/DTO) + handler CQRS + `Result` (invariants domaine) — [ADR-0014](0014-validation-a-plusieurs-niveaux.md)
- [x] Serilog (Console + File, JSON compact) — [ADR-0009](0009-serilog-pour-le-logging.md)
- [x] Outils qualité/sécurité : SonarCloud + CodeQL + Renovate (DeepSource retiré) — [ADR-0013](0013-outils-qualite-et-securite.md)
- [x] Conventional Commits + Versionize pour versioning/changelog automatiques — [ADR-0018](0018-conventional-commits-versionize.md)
- [x] Garde-fou SSRF (allow-list) sur tous les appels HTTP sortants — [ADR-0015](0015-garde-fou-ssrf-appels-sortants.md)
- [x] Hangfire + stockage PostgreSQL pour les jobs en arrière-plan — [ADR-0016](0016-hangfire-jobs-arriere-plan.md)
- [x] Cible de déploiement : Raspberry Pi 4 self-hosted (arm64), Docker + ghcr.io — [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md)
- [ ] SerpApi comme proxy pour interroger Bedetheque (scraping direct bloqué)
- [x] Stack de tests : xUnit v3, NSubstitute, AwesomeAssertions, bUnit — [ADR-0017](0017-stack-de-tests.md)

### Ouvertes (décision à finaliser)

- [ ] Migration identité Auth0 : passage complet du fallback email → `sub`/`AuthId`
- [ ] Modèle d'autorisation général (ownership par ressource, policies au-delà du dashboard Hangfire)
- [ ] Sécurité des migrations EF Core au démarrage si déploiement multi-instance
- [x] Consolidation des outils d'analyse statique (SonarCloud/CodeQL/DeepSource se recouvrent) — tranché par [ADR-0013](0013-outils-qualite-et-securite.md)
- [ ] Convention de logging : `ILogger<T>` injecté vs appels statiques `Log.*`
- [ ] Durcissement du pipeline d'import (pagination, politique de retry Hangfire, limites anti-DoS sur l'extraction PDF)
- [ ] Refonte graphique de la liste de livres (options en discussion dans TODO.md)
- [ ] Répartition des responsabilités entre jobs Hangfire et `FileWatcherService`
- [ ] Convention unique de sortie des rapports de couverture de tests
