# ADR-0009: Serilog pour le logging applicatif

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

L'application a besoin de journaliser son fonctionnement (requêtes HTTP,
erreurs, exécution du pipeline d'import, jobs Hangfire) de façon
structurée, exploitable après coup (debug d'un incident sur le Raspberry
Pi, sans accès à un outil d'observabilité cloud).

Le projet utilise **Serilog** (`Serilog.AspNetCore`), configuré via
`appsettings.json` avec deux sinks : **Console** et **File**
(`Serilog.Sinks.Console`, `Serilog.Sinks.File`), rotation quotidienne des
fichiers, format `CompactJsonFormatter` (`Serilog.Formatting.Compact`), et
des enrichers (`FromLogContext`, `WithMachineName`, `WithThreadId`).
`UseSerilogRequestLogging()` est activé dans `Web/Program.cs` pour logger
chaque requête HTTP. Les fichiers de log sont écrits sous `Web/log/`.

Un point encore à trancher, non couvert par cette ADR : le projet mélange
aujourd'hui des appels statiques `Log.X` et des `ILogger<T>` injectés par
DI (relevé comme `TEST-M11` dans la revue interne du projet) — suivi
séparément dans le backlog ("Convention de logging").

## Options considérées

- **Option A — Serilog (retenue)**
  - Pour : logging structuré (JSON) nativement supporté, sinks multiples
    faciles à combiner (Console + File ici, extensible vers d'autres sinks
    sans changer le code d'appel), enrichers pour ajouter du contexte
    (machine, thread, propriétés custom) sans changer les call sites.
  - Contre : une dépendance supplémentaire par rapport à l'abstraction
    `Microsoft.Extensions.Logging` seule ; nécessite de connaître l'API
    Serilog (enrichers, sinks) en plus de l'abstraction standard.
- **Option B — `Microsoft.Extensions.Logging` natif (Console/Debug providers uniquement)**
  - Pour : zéro dépendance externe, suffisant pour un simple logging texte.
  - Contre : pas de format structuré JSON out-of-the-box, pas de rotation
    de fichiers native, moins adapté pour analyser des logs après coup sur
    un serveur self-hosted sans outil d'agrégation.
- **Option C — Solution cloud managée (Application Insights, Seq hébergé, etc.)**
  - Pour : recherche/visualisation avancée des logs.
  - Contre : dépendance à un service externe payant ou à héberger en plus,
    disproportionné par rapport à [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md)
    (déploiement self-hosted à coût nul, usage mono-utilisateur).

## Décision

On utilise **Serilog** avec sinks Console + File (JSON compact, rotation
quotidienne) comme unique système de logging de l'application.

## Conséquences

### Positives

- Logs structurés en JSON, exploitables par des outils simples (`jq`,
  grep) même sans plateforme d'agrégation.
- Rotation de fichiers automatique : pas de gestion manuelle de la taille
  des logs sur le Raspberry Pi.
- Enrichers (machine, thread, contexte) ajoutent de l'info utile au
  débogage sans changer chaque appel de log.

### Négatives

- Les logs restent locaux au conteneur/hôte (`Web/log/`) : pas de
  centralisation ni d'alerting automatique en cas d'erreur critique — à
  surveiller manuellement.
- La convention d'utilisation (statique `Log.X` vs `ILogger<T>` injecté)
  n'est pas encore uniformisée dans le code — décision ouverte séparée.
- Pas de logging dédié aux événements de sécurité (échecs d'authentification,
  refus d'autorisation) identifié à ce jour — point relevé mais non traité
  par cette ADR.

### Couches impactées

- [x] Web (`Program.cs`, `UseSerilogRequestLogging`, configuration des sinks)
- [ ] Domain
- [ ] Application
- [ ] Persistence

## Liens

- `appsettings.json` (configuration Serilog : sinks, enrichers, rotation)
- `Web/Program.cs` (`UseSerilogRequestLogging`)
- Backlog (ouverte) : "Convention de logging : `ILogger<T>` injecté vs appels statiques `Log.*`"
