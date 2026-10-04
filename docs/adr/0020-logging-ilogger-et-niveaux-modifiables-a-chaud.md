# ADR-0020: Logging — `ILogger<T>` injecté, niveaux modifiables à chaud, fichiers HTTP séparés

**Statut** : Acceptée
**Date** : 2026-10-04
**Décideurs** : @nicolas-dufaut

## Contexte

[ADR-0009](0009-serilog-pour-le-logging.md) a retenu Serilog, mais trois
problèmes sont apparus à l'usage :

- **Deux styles de logger coexistaient** : `Serilog.Log.ForContext<T>()` statique
  dans la plupart des classes, `ILogger<T>` injecté dans quelques-unes, et le
  `Log` global *sans contexte* dans les pages Razor. Une partie des logs
  n'avait donc pas de `SourceContext` (#951).
- **Aucun moyen de changer un niveau sans redémarrer** : diagnostiquer la
  synchro Miniflux ou les requêtes SQL impose de modifier `appsettings` sur le
  serveur puis de redémarrer (#1066).
- **Les logs HTTP noient les logs métier** dans `log/log-<date>.txt`
  (4 lignes par appel sortant `HttpClient` + 1 par requête entrante) (#1065).

## Analyse de l'implémentation JHipster (préalable de #1066)

Le *Logs dashboard* de JHipster repose sur :

- **Back** : l'endpoint Spring Boot Actuator `LoggersEndpoint`
  (`GET /management/loggers` → `levels` + `loggers: { name: { configuredLevel, effectiveLevel } }`,
  `POST /management/loggers/{name}` avec `{ configuredLevel }`, `null` = retour à
  l'héritage). Les loggers forment une hiérarchie par namespace (Logback) avec
  un logger `ROOT`. `/management/**` est réservé à `ROLE_ADMIN`.
- **Front** (`admin/logs`, ex. `logs.ts` / `logs.html` du template Angular) :
  chargement de la liste, compteur « There are N loggers », champ de filtre
  (`includes` insensible à la casse sur le nom), tri par nom ou niveau, et pour
  chaque logger une rangée de boutons TRACE / DEBUG / INFO / WARN / ERROR / OFF ;
  le bouton du niveau **effectif** est mis en évidence, un clic poste le niveau
  puis recharge la liste.

Transposition :

| JHipster | MyComicsManager | |
| --- | --- | --- |
| `configuredLevel` / `effectiveLevel` | `LoggerLevel(Name, ConfiguredLevel?, EffectiveLevel)` | repris tel quel |
| Hiérarchie Logback + `ROOT` | `LogLevelSwitches` résout l'héritage (parent déclaré le plus proche, puis `ROOT` = `Serilog:MinimumLevel:Default`) | adapté : Serilog n'applique que l'override le plus spécifique, chaque switch porte donc le niveau effectif |
| `configuredLevel: null` → héritage | bouton « hériter » (`Set(name, null)`) + mention *inherited* | repris |
| Loggers créés dynamiquement | Loggers **déclarés au démarrage** : classes prenant un `ILogger<T>` (constructeur ou propriété injectée), leurs namespaces parents, préfixes de bibliothèques connus et overrides d'`appsettings` | adapté : la table d'overrides Serilog est figée à la construction du logger |
| Niveaux TRACE…OFF | Debug / Info / Warning / Error / None (`LevelAlias.Off`) | adapté (Verbose et Fatal non proposés) |
| Filtre, compteur, tri | `MudTable` avec filtre par nom, compteur, tri nom/niveau, pagination | repris |
| `ROLE_ADMIN` sur `/management/**` | `[Authorize(Roles = "Admin")]` (rôle du dashboard Hangfire), lien NavBar dans `AuthorizeView Roles="Admin"` | repris |
| Bouton « Réinitialiser » | `ResetAll()` → niveaux d'`appsettings` | ajouté |
| API REST + sélection de microservice (gateway) | appel direct au singleton depuis Blazor Server | abandonné (pas d'API ni de microservices) |
| Persistance | en mémoire, retour à `appsettings` au redémarrage | abandonné pour l'instant |

## Options considérées

- **`reloadOnChange` de Serilog.Settings.Configuration** — zéro code, mais ne
  modifie que les overrides déjà déclarés dans `appsettings` et impose d'éditer
  le fichier sur le serveur.
- **Serilog.UI / visualiseurs de logs** — affichent les logs, ne pilotent pas les niveaux.
- **`LoggingLevelSwitch` par logger déclaré au démarrage + page admin** — un peu
  de code, mais même ergonomie que JHipster.

## Décision

- **Convention** : chaque classe logge via un `ILogger<T>` injecté (pages Razor :
  `[Inject] private ILogger<T> Logger`). Le `Serilog.Log` statique est réservé à
  `Program.cs` ; un test NetArchTest l'impose. Application et Persistence ne
  dépendent plus de Serilog, seulement de `Microsoft.Extensions.Logging.Abstractions`.
- **Niveaux à chaud** : `Web/Infrastructure/LogLevelSwitches` +
  page `/admin/log-levels` (`Web/Components/Pages/Admin/LogLevels`).
- **Fichiers** : `Web/Configuration/LoggingConfiguration.WriteToSplitFiles` écrit
  `log/http-<date>.txt` (sources `System.Net.Http.HttpClient`,
  `Serilog.AspNetCore.RequestLoggingMiddleware`, `Microsoft.AspNetCore.Hosting.Diagnostics`)
  et `log/log-<date>.txt` (tout le reste). La console garde tout ; le sink
  `File` est retiré d'`appsettings.json`.

## Conséquences

- **Positives** : tous les logs portent un `SourceContext` ; un admin passe une
  classe ou un namespace en Debug (ou coupe une bibliothèque bavarde) sans
  redémarrer ; logs métier lisibles.
- **Négatives** : un constructeur de plus par classe qui logge (tests :
  `NullLogger<T>.Instance`) ; une bibliothèque absente de la liste n'est
  pilotable que via un préfixe parent (`Microsoft`, `System`…) ; les chemins des
  fichiers de log sont en dur dans le code (`log/…`).
- **Couches impactées** : Application / Persistence / Web

## Liens

- ADR liées : [ADR-0009](0009-serilog-pour-le-logging.md), [ADR-0010](0010-architecture-en-couches-netarchtest.md)
- Issues : #951, #1065, #1066
