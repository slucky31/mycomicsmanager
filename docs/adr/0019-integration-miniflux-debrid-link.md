# ADR-0019: Import automatique des BD depuis Miniflux via Debrid-Link

**Statut** : Acceptée
**Date** : 2026-10-02
**Décideurs** : @nicolas-dufaut

## Contexte

Les BD numériques arrivent dans MCM par le dossier d'import d'une
bibliothèque digitale (`ImportSettings.ImportDirectory`), surveillé par
`FileWatcherService` puis traité par le pipeline Hangfire
`ProcessImportJobCommandHandler` (cf. [ADR-0016](0016-hangfire-jobs-arriere-plan.md)).

En amont, le repérage des BD à récupérer se fait à la main : lecture de
deux flux RSS (planete-bd.org, zone-ebook.com) dans **Miniflux**, puis
téléchargement manuel des fichiers depuis des hébergeurs (1fichier,
Rapidgator...) et dépôt dans le dossier d'import. L'objectif (issue #1057)
est d'automatiser cette chaîne sans perdre le contrôle : chaque décision
de l'automate doit être tracée et corrigeable.

Contraintes :

- **Miniflux est déjà en place** sur le Raspberry Pi qui héberge MCM
  (cf. [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md)) :
  conteneur `miniflux` (`miniflux/miniflux:2.2.12`), base dédiée sur
  l'instance PostgreSQL partagée (cf. [ADR-0003](0003-postgresql-self-hosted-au-lieu-de-neon.md)),
  sur le réseau Docker externe `pi-postgres`, interface exposée uniquement
  sur l'IP Tailscale (port `5100`).
- Les hébergeurs de fichiers limitent fortement le téléchargement gratuit ;
  un service de **débridage** (Debrid-Link, abonnement existant) fournit
  des liens directs.
- Tous les appels sortants passent par le garde-fou SSRF
  (cf. [ADR-0015](0015-garde-fou-ssrf-appels-sortants.md)).
- Le Pi est mono-worker Hangfire (`WorkerCount = 1`).

## Options considérées

- **Option A — Miniflux comme « boîte de réception » + Debrid-Link, décisions tracées en base (retenue)**
  - Pour : réutilise le lecteur RSS déjà utilisé au quotidien ; le geste
    « mettre une étoile » suffit pour demander un import ; l'API Miniflux
    est simple (jeton `X-Auth-Token`) et joignable sans sortir du réseau
    Docker ; Debrid-Link résout le problème des hébergeurs ; la table de
    décisions rend chaque choix automatique visible et réversible.
  - Contre : deux dépendances externes de plus (Miniflux, Debrid-Link) ;
    la qualité dépend du parsing HTML de deux sites tiers, susceptible de
    casser si leur mise en page change.
- **Option B — MCM lit directement les flux RSS**
  - Pour : une dépendance en moins.
  - Contre : réimplémente un lecteur RSS (suivi des articles lus,
    rafraîchissement, interface de tri) ; perd le geste de sélection
    manuel dans un outil déjà en place ; tout article publié devient un
    candidat, il faudrait une autre interface pour choisir.
- **Option C — Téléchargement direct depuis les hébergeurs, sans débrideur**
  - Pour : pas d'abonnement ni d'API tierce.
  - Contre : attentes, captchas et quotas des hébergeurs gratuits rendent
    l'automatisation peu fiable ; chaque hébergeur aurait son propre client.
- **Option D — Statu quo (manuel)**
  - Pour : aucun développement.
  - Contre : tâche répétitive, pas de détection des doublons, pas de trace.

## Décision

On retient l'**option A**, avec les règles suivantes :

1. **Sélection** : un article ★ dans la catégorie Miniflux configurée
   (`Miniflux:CategoryName`, « BD » par défaut) est une demande d'import.
2. **Traçabilité** : chaque article devient une `FeedImportDecision`
   (statuts `Pending` → `LinksExtracted` / `AwaitingArbitration` /
   `SkippedDuplicate` → `Downloading` → `Downloaded` → `Imported`, ou
   `Ignored` / `Failed`), avec raison lisible, auteur de la décision
   (`Auto` / `User`) et historique (`FeedImportDecisionEvents`). Unicité
   `(UserId, MinifluxEntryId)` : une entrée n'est jamais traitée deux fois.
3. **Ordre « enregistrer puis retirer l'étoile »** : la décision est
   sauvegardée **avant** de retirer l'étoile. Si le retrait échoue, la
   synchronisation suivante retrouve l'entrée connue et ne fait que
   réessayer le retrait. Comme l'API Miniflux ne propose qu'une bascule
   (`PUT /v1/entries/{id}/bookmark`), l'état de l'entrée est vérifié avant
   de basculer. Une fois l'article enregistré, Miniflux n'est plus utilisé :
   le suivi se fait dans la page `/feed-imports`.
4. **Propriétaire** : le job tourne sans utilisateur connecté ; les
   décisions sont rattachées à l'utilisateur MCM désigné par
   `FeedImport:UserEmail`.
5. **Réseau et SSRF** :
   - Miniflux est appelé par le réseau Docker interne
     (`http://miniflux:8080`). `SsrfGuardHandler` accepte exceptionnellement
     le **HTTP**, et uniquement vers l'hôte de `Miniflux:BaseUrl` ;
   - les pages d'articles ne sont récupérées que si leur domaine est dans
     `FeedImport:AllowedSourceHosts` ;
   - les liens extraits ne sont retenus que si leur domaine est dans
     `FeedImport:AllowedDownloadHosts`, **sous-domaines compris** (les
     hébergeurs servent souvent depuis `*.1fichier.com`, etc.) ; les liens
     directs renvoyés par Debrid-Link doivent, eux, être en HTTPS sur un
     domaine de cette liste ou de `DebridLink:DownloadHosts` (serveurs de
     fichiers de Debrid-Link), sans redirection automatique ;
   - API Debrid-Link : hôte fixe, HTTPS, `AllowAutoRedirect = false`.
6. **Doublons** : avant tout téléchargement, recherche dans toutes les
   bibliothèques de l'utilisateur (série + tome normalisés, ISBN si connu).
   Correspondance certaine → `SkippedDuplicate` ; probable ou regroupement
   de liens ambigu → `AwaitingArbitration`, jamais de téléchargement
   automatique.
7. **Dépôt** : téléchargement en streaming dans `Import:TempDirectory`
   (jamais dans le dossier surveillé), contrôles de taille et d'extension,
   puis déplacement atomique dans le dossier d'import de la bibliothèque
   digitale `FeedImport:TargetLibraryName` (« À trier », créée au besoin).
   Le pipeline d'import existant prend le relais ; la décision est reliée à
   l'`ImportJob` créé. L'`ImportJob` est créé **avant** le dépôt du fichier :
   le `FileWatcherService` trouve alors une tâche active pour ce chemin et
   n'en crée pas une seconde. Le fichier arrive en `.part` (ignoré par le
   watcher) puis est renommé dans le même dossier.
8. **Orchestration** : job Hangfire récurrent `feed-import-sync`
   (`FeedImport:SyncIntervalMinutes`), un job par entrée pour le
   téléchargement, bouton « Synchroniser maintenant ».
9. **Désactivable** : `FeedImport:Enabled = false` par défaut ; le job
   récurrent est alors retiré. Les secrets (`Miniflux__ApiKey`,
   `DebridLink__ApiKey`) passent uniquement par variables d'environnement.

Mise en œuvre découpée en 5 lots (issue #1057) : 1. cette ADR et la
documentation ; 2. décisions + client Miniflux + synchronisation + page en
lecture seule (#1058) ; 3. extraction des liens + doublons + arbitrage ;
4. Debrid-Link + téléchargement + dépôt ; 5. actions manuelles complètes.

## Conséquences

### Positives

- Le geste utilisateur reste minimal (une étoile) et l'outil de lecture
  reste celui déjà utilisé.
- Chaque décision automatique est visible, explicable et corrigeable ;
  rien n'est téléchargé en cas de doute.
- Aucun nouveau service à héberger : Miniflux est déjà en place,
  Debrid-Link est un service externe.
- Réutilisation complète du pipeline d'import existant.

### Négatives

- Les extracteurs de liens dépendent de la structure HTML de deux sites
  tiers ; un changement de leur côté casse l'extraction (atténué par des
  fixtures de pages réelles et un extracteur générique de repli).
- Exception à la règle « HTTPS uniquement » d'[ADR-0015](0015-garde-fou-ssrf-appels-sortants.md)
  pour Miniflux, limitée à un seul hôte du réseau Docker interne.
- Dépendance à un abonnement Debrid-Link (quota, disponibilité).
- `SsrfGuardHandler` devra accepter les sous-domaines pour les
  hébergeurs (lot 4), ce qui élargit la surface autorisée par rapport à
  une correspondance exacte.

### Couches impactées

- [x] Domain (`FeedImportDecision`, `FeedImportDecisionEvent`, statuts, erreurs)
- [x] Application (`IMinifluxClient`, commandes et requêtes `FeedImports`, puis `IDebridLinkClient`)
- [x] Persistence (tables `FeedImportDecisions` / `FeedImportDecisionEvents`, repository, read service)
- [x] Web (clients HTTP, `SsrfGuardHandler`, job Hangfire, page `/feed-imports`)

## Liens

- Issue : #1057 ; lot 2 : #1058 ; lot 3 : #1060
- Guide d'installation : [`docs/FEED-IMPORT.md`](../FEED-IMPORT.md)
- ADR liées : [0001](0001-deploiement-raspberry-pi-4-self-hosted.md), [0003](0003-postgresql-self-hosted-au-lieu-de-neon.md), [0015](0015-garde-fou-ssrf-appels-sortants.md), [0016](0016-hangfire-jobs-arriere-plan.md)
- API Miniflux : https://miniflux.app/docs/api.html
- API Debrid-Link v2 : https://debrid-link.fr/api_doc/v2/introduction
- Code : `Web/Configuration/FeedImportConfiguration.cs`, `Web/Infrastructure/MinifluxClient.cs`, `Web/Infrastructure/DebridLinkClient.cs`, `Web/Infrastructure/FeedImportFileDownloader.cs`, `Web/Services/FeedImportSyncJob.cs`, `Web/Services/FeedImportDownloadJob.cs`, `Application/FeedImports/`
