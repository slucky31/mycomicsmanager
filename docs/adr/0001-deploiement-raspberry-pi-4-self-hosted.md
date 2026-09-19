# ADR-0001: Cible de déploiement — Raspberry Pi 4 self-hosted (arm64)

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

MyComicsManager est une application personnelle mono-utilisateur. Elle doit
tourner en continu (import de fichiers, jobs en arrière-plan, base de
données) à faible coût et sans dépendance à un fournisseur cloud payant.

Plusieurs éléments du code et de la CI imposaient déjà cette contrainte sans
qu'elle soit écrite nulle part :

- `Web/Program.cs` configure Hangfire avec `WorkerCount = 1` et un
  commentaire explicite `// Sequential for RPi4`.
- Les deux workflows GitHub Actions (`dotnet-core-build.yml`,
  `dotnet-core-publish.yml`) construisent les images Docker uniquement pour
  `linux/arm64` (via QEMU/Buildx), jamais `linux/amd64`.
- `docs/MIGRATION-POSTGRESQL.md` documente le passage d'un PostgreSQL managé
  (Neon) vers un PostgreSQL self-hosted en conteneur Docker, avec la
  justification "appli perso mono-utilisateur" et une seule instance
  partagée entre staging et prod.
- Les images publiées sont poussées sur `ghcr.io/slucky31/mcm`, pas sur un
  registre cloud managé (ACR/ECR).

Sans ADR, ces contraintes (une seule instance, un seul worker, arm64
uniquement) apparaissent comme des limitations arbitraires plutôt que comme
la conséquence assumée d'un choix d'hébergement.

## Options considérées

- **Option A — Raspberry Pi 4 self-hosted (retenue)**
  - Pour : coût quasi nul, contrôle total, suffisant pour un usage
    mono-utilisateur, cohérent avec l'existant (Docker + ghcr.io déjà en
    place).
  - Contre : pas de haute disponibilité, capacité de calcul limitée,
    nécessite de gérer soi-même les mises à jour OS/Docker et les sauvegardes.
- **Option B — Hébergement cloud managé (VM ou PaaS)**
  - Pour : scalabilité, haute disponibilité, moins de maintenance
    infrastructure.
  - Contre : coût récurrent injustifié pour un usage mono-utilisateur, va à
    l'encontre de l'objectif "app perso à coût nul".
- **Option C — Serverless / Functions**
  - Pour : coût à l'usage potentiellement très bas.
  - Contre : incompatible avec Blazor Server (connexion SignalR persistante),
    avec Hangfire (worker long-running), et avec FileWatcherService (process
    qui doit tourner en continu).

## Décision

On héberge MyComicsManager sur un **Raspberry Pi 4 (arm64), self-hosted**,
via Docker. Une seule instance de l'application et une seule instance
PostgreSQL, partagées entre staging et prod. Les images sont construites
uniquement pour `linux/arm64` et publiées sur `ghcr.io/slucky31/mcm`.

## Conséquences

### Positives

- Coût d'hébergement nul (hors électricité), cohérent avec un projet perso.
- Simplicité : pas de gestion multi-région, pas de load balancer.
- La CI ne construit qu'une seule architecture, ce qui simplifie et accélère
  les pipelines.

### Négatives

- Pas de haute disponibilité : une panne matérielle du Pi met l'app hors
  ligne (pas de plan de reprise documenté à ce jour).
- Le déploiement multi-instance n'est pas supporté par le code actuel : les
  migrations EF Core s'exécutent automatiquement au démarrage
  (`Web/Program.cs`) sans verrou distribué. Un `ponytail:` explicite dans le
  code signale ce risque si l'app tournait un jour avec plusieurs répliques
  → voir la candidate ADR ouverte "Sécurité des migrations EF Core au
  démarrage".
- Capacité de calcul limitée : le pipeline d'import (extraction PDF, WebP)
  doit rester séquentiel (`WorkerCount = 1`), ce qui limite le débit
  d'import à un job à la fois.
- Toute évolution future vers du multi-utilisateur ou de la haute
  disponibilité nécessitera de revisiter cette ADR.

### Couches impactées

- [x] Web (configuration Hangfire, hosting)
- [x] Persistence (connexion PostgreSQL unique, migrations au démarrage)
- [ ] Application
- [ ] Domain

## Liens

- `docs/MIGRATION-POSTGRESQL.md` — détail de la migration Neon → self-hosted
- `.github/workflows/dotnet-core-build.yml`, `.github/workflows/dotnet-core-publish.yml`
- `Web/Dockerfile`
- `Web/Program.cs` (configuration Hangfire, `MigrateAsync`)
- Candidate liée (backlog) : "Sécurité des migrations EF Core au démarrage si déploiement multi-instance"
