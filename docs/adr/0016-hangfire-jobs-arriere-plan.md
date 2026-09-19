# ADR-0016: Hangfire + stockage PostgreSQL pour les jobs en arrière-plan

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Le pipeline d'import de bandes dessinées (extraction d'archive, conversion
d'images en WebP, extraction PDF, upload de couverture vers Cloudinary,
génération du CBZ final) est un traitement potentiellement long qui ne doit
pas bloquer une requête HTTP ni l'UI Blazor Server.

Le projet utilise **Hangfire** (`Hangfire.AspNetCore`, `Hangfire.Core`,
`Hangfire.PostgreSql`) pour exécuter ces traitements en arrière-plan, avec
**PostgreSQL comme stockage de jobs** (`UsePostgreSqlStorage`) — donc pas de
dépendance à un service de file de messages séparé (Redis, RabbitMQ,
Azure Service Bus). La configuration utilise deux queues (`import`,
`default`) et un seul worker (`WorkerCount = 1`, commenté `// Sequential for
RPi4` — cohérent avec la capacité limitée du matériel choisi, cf.
[ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md)). Le dashboard
Hangfire est exposé sur `/hangfire`, restreint au rôle `Admin`
(`HangfireAuthorizationFilter`).

À noter : un second mécanisme d'exécution en arrière-plan coexiste dans le
projet, `FileWatcherService` (`IHostedService`), qui surveille un
répertoire d'import par polling et déclenche les jobs Hangfire. La
répartition des responsabilités entre les deux (lequel fait quoi, faut-il
les unifier) est une question **encore ouverte**, suivie séparément dans le
backlog. Cette ADR ne couvre que le choix de Hangfire comme moteur
d'exécution des jobs eux-mêmes.

## Options considérées

- **Option A — Hangfire + stockage PostgreSQL (retenue)**
  - Pour : pas de service supplémentaire à héberger (réutilise
    l'instance PostgreSQL déjà en place, cf. [ADR-0003](0003-postgresql-self-hosted-au-lieu-de-neon.md)) ;
    dashboard de monitoring intégré ; gestion de queues, retries et
    planification déjà résolue par la bibliothèque ; intégration .NET native
    simple (`AddHangfire`, `[Queue]` attribute).
  - Contre : les performances de Hangfire avec un stockage SQL (par
    rapport à Redis) sont plus limitées à très haut débit — non
    contraignant ici (usage mono-utilisateur, un seul worker).
- **Option B — File de messages dédiée (Redis, RabbitMQ) + workers custom**
  - Pour : meilleure scalabilité théorique à fort volume.
  - Contre : service supplémentaire à héberger et maintenir sur le
    Raspberry Pi, complexité disproportionnée par rapport au volume de
    jobs réel (usage mono-utilisateur).
- **Option C — `IHostedService` custom pour tous les traitements en arrière-plan**
  - Pour : pas de dépendance externe, contrôle total.
  - Contre : réimplique de réimplémenter soi-même queue, retries,
    persistance de l'état des jobs et dashboard — Hangfire couvre déjà tout
    cela ; le projet utilise d'ailleurs déjà un `IHostedService`
    (`FileWatcherService`) pour la détection de fichiers, mais pas pour le
    traitement lui-même, précisément pour éviter cette réimplémentation.

## Décision

On utilise **Hangfire** avec **stockage PostgreSQL** pour exécuter tous les
traitements longs en arrière-plan (pipeline d'import), avec un seul worker
et deux queues (`import`, `default`), dashboard restreint au rôle `Admin`.

## Conséquences

### Positives

- Pas de service d'infrastructure supplémentaire : Hangfire réutilise
  l'instance PostgreSQL déjà déployée.
- Dashboard de monitoring des jobs disponible sans développement
  supplémentaire, sécurisé par rôle.
- Persistance native de l'état des jobs : un redémarrage de l'application
  ne perd pas les jobs en attente.

### Négatives

- Un seul worker (`WorkerCount = 1`) signifie que les imports sont traités
  **séquentiellement** — acceptable pour un usage mono-utilisateur mais un
  facteur limitant si le volume d'import augmentait.
- Aucune politique de retry explicite n'est configurée à ce jour pour les
  jobs d'import (comportement implicite de Hangfire) — décision ouverte
  suivie séparément dans le backlog ("Durcissement du pipeline d'import").
- La coexistence avec `FileWatcherService` (mécanisme d'exécution en
  arrière-plan distinct, pour la détection de fichiers) répartit la
  logique d'import entre deux systèmes différents — clarification de cette
  répartition suivie séparément dans le backlog.

### Couches impactées

- [x] Web (configuration Hangfire, dashboard, `FileWatcherService`)
- [x] Application (`ProcessImportJobCommandHandler` exécuté comme job Hangfire)
- [ ] Domain
- [x] Persistence (stockage des jobs dans PostgreSQL)

## Liens

- `Web/Program.cs` (`AddHangfire`, `UsePostgreSqlStorage`, `WorkerCount`)
- `Web/Infrastructure/HangfireAuthorizationFilter.cs`
- `Web/Services/FileWatcherService.cs`, `Web/Services/ImportOrchestrator.cs`
- `Application/ImportJobs/ProcessImportJob/ProcessImportJobCommandHandler.cs`
- [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md), [ADR-0003](0003-postgresql-self-hosted-au-lieu-de-neon.md)
- Backlog (ouvertes) : "Durcissement du pipeline d'import", "Répartition des responsabilités entre jobs Hangfire et `FileWatcherService`"
