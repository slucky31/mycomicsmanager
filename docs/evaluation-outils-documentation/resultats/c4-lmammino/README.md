# Architecture C4 — MyComicsManager

Documentation d'architecture produite par rétro-ingénierie du dépôt (modèle C4, diagrammes Mermaid).

| Vue | Fichier |
|---|---|
| 1. Contexte | [01-contexte.md](01-contexte.md) |
| 2. Conteneurs | [02-conteneurs.md](02-conteneurs.md) |
| 3. Composants (conteneur Web/Blazor) | [03-composants-web.md](03-composants-web.md) |
| Questions ouvertes | [04-questions-ouvertes.md](04-questions-ouvertes.md) |

## Périmètre

Ce dépôt est **le système complet** MyComicsManager : une application web personnelle de gestion de collection de bandes dessinées (.NET 10, Blazor Server). Les systèmes tiers (Auth0, Miniflux, Debrid-Link, sources de métadonnées ISBN, Cloudinary) sont montrés comme externes. L'hébergement (Raspberry Pi 4 auto-hébergé, ADR-0001) n'est documenté que lorsqu'il influence l'architecture.

## Méthode : observé vs inféré

- **Observé** : lu directement dans le code, la configuration, le `Dockerfile`, la CI ou les ADR (`docs/adr/`).
- **Inféré** : déduit ; signalé par la mention « (inféré) ».

## Observations principales

- Solution .NET 10 en couches : `Domain` → `Application` → `Persistence` → `Web` (racine de composition), vérifiée par `tests/Architecture.Tests` (NetArchTest).
- Une seule application déployable : `Web` (Blazor Server, port 8080, image Docker `linux/arm64`, `Web/Dockerfile`), qui héberge aussi le serveur Hangfire et deux `IHostedService` (`FileWatcherService`, `LibraryIconsWarmupService`).
- PostgreSQL auto-hébergé : données métier, file de jobs Hangfire, clés Data Protection, surcharges de feature toggles.
- Système de fichiers local : bibliothèques de BD (`LocalStorage:RootPath`), dossier d'import (`Import:ImportDirectory`), espace temporaire.
- Appels sortants HTTP protégés par `SsrfGuardHandler` (liste d'hôtes autorisés, ADR-0015).

## Hypothèses et inférences

- (inféré) Un seul utilisateur réel, d'après l'ADR-0001 (« application personnelle mono-utilisateur »), alors que le modèle gère plusieurs comptes et un rôle `Admin`.
- (inféré) Miniflux et l'application tournent sur le même hôte/réseau Docker (`Miniflux:BaseUrl = http://miniflux:8080`).
