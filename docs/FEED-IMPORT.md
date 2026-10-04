# Import automatique depuis Miniflux — mise en place

Ce guide décrit comment brancher MCM sur le Miniflux du Raspberry Pi pour
importer automatiquement les BD marquées d'une étoile. La décision
d'architecture est documentée dans [ADR-0019](adr/0019-integration-miniflux-debrid-link.md).

> État actuel : la chaîne complète est en place — synchronisation Miniflux,
> analyse des articles, arbitrage, téléchargement via Debrid-Link avec dépôt
> dans la bibliothèque « À trier », et actions manuelles sur les décisions.

## Fonctionnement

1. Dans Miniflux, mettre une étoile sur un article de la catégorie « BD ».
2. Toutes les `FeedImport:SyncIntervalMinutes` minutes (ou via le bouton
   « Synchroniser maintenant » de `/feed-imports`), le job Hangfire
   `feed-import-sync` récupère les articles ★ de cette catégorie.
3. Chaque nouvel article est enregistré comme décision `Pending`, **puis**
   son étoile est retirée dans Miniflux. Si le retrait échoue, il est
   réessayé à la synchronisation suivante, sans créer de doublon.
4. Dans la foulée, chaque décision `Pending` est analysée :
   - la page de l'article est récupérée en HTTPS (domaines de `FeedImport:AllowedSourceHosts` uniquement) ;
   - un extracteur dédié (planete-bd.org, zone-ebook.com) ne lit que le bloc de l'article — ni barre
     latérale, ni publicités, ni liens « Tous mes fichiers » — et relève l'ISBN s'il est indiqué ; pour un
     autre site, ou si la mise en page change, un extracteur générique lit toute la page ;
   - les liens vers les hébergeurs de `FeedImport:AllowedDownloadHosts` sont extraits puis regroupés :
     les liens vers le même fichier (même nom, ou hébergeurs différents) sont des **miroirs** ;
     des noms de fichiers différents sont des **livres différents**, chacun avec sa propre décision ;
   - chaque livre est comparé aux bibliothèques de l'utilisateur : doublon certain → `SkippedDuplicate`,
     doublon probable ou regroupement ambigu → `AwaitingArbitration`, sinon → `LinksExtracted`.
5. Le suivi et l'arbitrage se font dans la page **Feeds** (`/feed-imports`) de MCM : déplier une
   décision « À arbitrer » pour choisir le bon lien, fusionner les liens en miroirs, ou confirmer /
   infirmer le doublon.
6. À la fin de chaque synchronisation, si `DebridLink:ApiKey` est renseignée, chaque décision
   `LinksExtracted` (y compris celles débloquées par un arbitrage) part dans un job Hangfire
   `FeedImportDownloadJob` :
   - la bibliothèque digitale `FeedImport:TargetLibraryName` (« À trier ») est créée au besoin ;
   - les miroirs sont essayés dans l'ordre de `DebridLink:HosterPriority`, en sautant les hébergeurs
     que Debrid-Link ne prend pas en charge ; chaque échec de miroir est tracé dans l'historique ;
   - le lien direct renvoyé par Debrid-Link n'est suivi que si son domaine est dans
     `FeedImport:AllowedDownloadHosts` ou `DebridLink:DownloadHosts` ; nom, extension
     (`Import:SupportedExtensions`) et taille (`Import:MaxFileSizeMb`) sont vérifiés avant et pendant
     le téléchargement ;
   - le fichier est téléchargé dans `Import:TempDirectory`, l'`ImportJob` est créé, puis le fichier
     est déposé dans le dossier d'import de la bibliothèque (copie en `.part` puis renommage) →
     `Downloaded` ;
   - `Downloaded` est l'état final côté Feeds : la suite (extraction, conversion, métadonnées) se
     suit dans la page **Import**, que le bouton « Suivre l'import » ouvre sur la bibliothèque « À trier ».

### Actions manuelles

Dans le détail d'une décision, `/feed-imports` propose les actions possibles selon son statut (chaque
action est tracée dans l'historique, décidée par « Utilisateur ») :

| Action | Disponible pour | Effet |
| ------ | --------------- | ----- |
| **Forcer le téléchargement** | Doublon, doublon probable | Passe outre le doublon et lance le téléchargement |
| **Relancer** | Échoué, Ignoré, téléchargement interrompu depuis plus d'une heure | Refait le contrôle des doublons sur les liens enregistrés puis télécharge ; sans lien, l'article est réanalysé |
| **Corriger série / tome** | Un seul fichier : liens extraits, doublon (probable), échoué, ignoré | Remplace la lecture du titre puis refait le contrôle des doublons |
| **Ignorer** | En attente, liens extraits, à arbitrer, doublon, échoué | Plus rien n'est fait pour cette décision |

Quand une action ou un arbitrage aboutit à « Liens extraits », le téléchargement démarre aussitôt (si
`DebridLink:ApiKey` est renseignée), sans attendre la synchronisation suivante.

Le menu **Feeds** affiche un badge avec le nombre de décisions « À arbitrer ».

### Règles de doublon

| Résultat | Condition |
| -------- | --------- |
| Doublon certain | ISBN identique, ou même série (casse, accents, ponctuation et article initial ignorés) et même tome |
| Doublon probable | Série très proche (similarité ≥ 0,85) et même tome, ou même série mais tome non trouvé dans le titre |
| Pas de doublon | Sinon |

## Prérequis

- Miniflux en service : conteneur `miniflux` sur le réseau Docker externe
  `pi-postgres`, interface web sur l'IP Tailscale, port `5100`.
- Un utilisateur MCM existant (celui qui se connecte via Auth0) : les
  décisions lui seront rattachées.

## 1. Réseau Docker : MCM doit joindre `http://miniflux:8080`

MCM appelle Miniflux par le réseau Docker interne, sans passer par
Tailscale. Le nom `miniflux` n'est résolu que si les deux conteneurs sont
sur le même réseau Docker.

Repérer le conteneur MCM et vérifier ses réseaux :

```bash
docker ps --format '{{.Names}}\t{{.Image}}' | grep -i comics
docker inspect -f '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}} {{end}}' <conteneur-mcm>
```

Si `pi-postgres` n'apparaît pas, l'ajouter dans le `docker-compose` de
déploiement (un `docker network connect` ponctuel serait perdu à la
prochaine recréation du conteneur) :

```yaml
services:
  mycomicsmanager:
    # ...
    networks:
      - default
      - pi-postgres

networks:
  pi-postgres:
    external: true
```

Vérifier que Miniflux répond depuis ce réseau (l'image MCM n'embarque pas
`curl`, on passe par un conteneur jetable) :

```bash
docker run --rm --network pi-postgres curlimages/curl -s http://miniflux:8080/healthcheck
# → OK
```

## 2. Miniflux : catégorie et clé d'API

Dans l'interface Miniflux (`http://<ip-tailscale>:5100`) :

1. **Catégories** → créer la catégorie `BD` et y ranger les flux
   `https://planete-bd.org/rss.xml` et `https://zone-ebook.com/rss.xml`.
2. **Paramètres → Clés d'API** (*API Keys*) → créer une clé, par exemple `mcm`.

Tester la clé depuis le réseau Docker :

```bash
docker run --rm --network pi-postgres curlimages/curl -s \
  -H "X-Auth-Token: <cle-api>" http://miniflux:8080/v1/categories
# → la liste doit contenir {"id":..., "title":"BD", ...}
```

## 3. Configuration de MCM

Les valeurs par défaut sont dans `Web/appsettings.json`. En déploiement,
les surcharger par variables d'environnement dans le `docker-compose` :

```yaml
environment:
  FeedImport__Enabled: "true"
  FeedImport__UserEmail: "<email-de-l-utilisateur-mcm>"
  FeedImport__SyncIntervalMinutes: "30"   # 1–59, ou multiple de 60 jusqu'à 1440
  Miniflux__BaseUrl: "http://miniflux:8080"
  Miniflux__ApiKey: "<cle-api>"           # secret : jamais dans appsettings.*.json
  Miniflux__CategoryName: "BD"
  DebridLink__ApiKey: "<cle-api-debrid-link>"  # secret : jamais dans appsettings.*.json
```

| Clé | Défaut | Rôle |
| --- | ------ | ---- |
| `FeedImport:Enabled` | `false` | Active la synchronisation ; à `false`, le job récurrent est retiré de Hangfire |
| `FeedImport:UserEmail` | — | Email de l'utilisateur MCM propriétaire des décisions (obligatoire si activé) |
| `FeedImport:SyncIntervalMinutes` | `30` | Intervalle du job `feed-import-sync` |
| `FeedImport:TargetLibraryName` | `À trier` | Bibliothèque digitale de dépôt, créée au premier téléchargement |
| `FeedImport:AllowedSourceHosts` | `planete-bd.org`, `zone-ebook.com` | Domaines des pages d'articles autorisés, sous-domaines compris |
| `FeedImport:AllowedDownloadHosts` | `dailyuploads.net`, `katfile.biz`, `trbt.cc`, `turbobit.net`, `rapidgator.net`, `1fichier.com` | Domaines des hébergeurs dont les liens sont retenus, sous-domaines compris (relevés sur les deux sites en octobre 2026). `fileq.net` et `frdl.io`, aussi présents sur les sites, sont volontairement exclus : Debrid-Link ne les prend pas en charge (`GET /downloader/domains`) |
| `Miniflux:BaseUrl` | `http://miniflux:8080` | URL de Miniflux ; seul cet hôte est autorisé par le garde-fou SSRF |
| `Miniflux:ApiKey` | — | Clé d'API Miniflux (obligatoire si activé) — `Miniflux__ApiKey` |
| `Miniflux:CategoryName` | `BD` | Catégorie Miniflux surveillée |
| `DebridLink:ApiKey` | — | Clé d'API privée Debrid-Link (compte → API) — `DebridLink__ApiKey`. Vide : rien n'est téléchargé |
| `DebridLink:BaseUrl` | `https://debrid-link.com/api/v2/` | API Debrid-Link ; seul cet hôte est autorisé, en HTTPS |
| `DebridLink:HosterPriority` | `1fichier.com`, `rapidgator.net`, `turbobit.net`, `trbt.cc`, `katfile.biz`, `dailyuploads.net` | Ordre d'essai des miroirs ; les autres viennent ensuite, dans l'ordre de la page |
| `DebridLink:DownloadHosts` | `debrid.link`, `debrid-link.com`, `debrid-link.fr` | Domaines des serveurs de fichiers Debrid-Link (liens directs), sous-domaines compris |

Pour une liste, une variable par élément : `FeedImport__AllowedDownloadHosts__0`,
`FeedImport__AllowedDownloadHosts__1`, etc.

Si `FeedImport:Enabled` vaut `true` et qu'une valeur obligatoire manque,
**l'application refuse de démarrer** et le log indique la clé en cause.

> **Staging et prod partagent le même Miniflux.** N'activer
> `FeedImport:Enabled` que sur **un seul** environnement (la prod) : sinon
> le premier qui synchronise retire l'étoile et l'article n'apparaît que
> dans sa base.

## 4. Vérifier

1. Redéployer MCM.
2. Dashboard Hangfire (`/hangfire`, rôle Admin) → **Recurring jobs** : le job
   `feed-import-sync` est présent avec la bonne périodicité.
3. Mettre une étoile sur un article de la catégorie « BD », puis
   **Feeds → Synchroniser maintenant**.
4. Après quelques secondes, rafraîchir la liste : l'article apparaît en
   « En attente » et son étoile a disparu dans Miniflux.

En cas de problème, les logs Serilog contiennent un résumé à chaque
synchronisation (`Feed import sync done: ...`) ou l'erreur rencontrée
(`Feed import sync failed`, `Miniflux returned 401`, utilisateur introuvable...).

## Dépannage

| Symptôme | Cause probable |
| -------- | -------------- |
| `Miniflux request failed` / nom `miniflux` non résolu | MCM n'est pas sur le réseau `pi-postgres` (étape 1) |
| `Miniflux returned 401` | Clé d'API absente ou invalide |
| `Miniflux category not found` | La catégorie `Miniflux:CategoryName` n'existe pas (la comparaison ignore la casse) |
| `user configured in FeedImport:UserEmail not found` | L'email ne correspond à aucun utilisateur MCM (se connecter une fois à MCM d'abord) |
| `SSRF guard blocked outgoing request` | `Miniflux:BaseUrl` pointe vers un autre hôte que celui appelé |
| Décision « Échoué » à l'étape *Source* | Le domaine de l'article n'est pas dans `FeedImport:AllowedSourceHosts` |
| Décision « Échoué » à l'étape *Extraction des liens* | Aucun lien vers un hébergeur de `FeedImport:AllowedDownloadHosts` sur la page (par exemple un article qui ne propose que fileq.net ou frdl.io) |
| L'article reste ★ dans Miniflux | Échec du retrait d'étoile : il est réessayé à chaque synchronisation (voir les logs `failed to unstar`) |
| Les décisions restent « Liens extraits » | `DebridLink:ApiKey` absente (log `Feed import downloads skipped`) |
| Décision « Échoué » à l'étape *Téléchargement* : clé absente ou invalide | Clé Debrid-Link expirée ou révoquée : en générer une nouvelle |
| Décision « Échoué » : *Domaine de téléchargement non autorisé (xxx)* | Debrid-Link sert le fichier depuis un domaine inconnu : l'ajouter à `DebridLink:DownloadHosts` |
| Décision « Échoué » : *Quota Debrid-Link atteint* | Limite journalière du compte : les autres miroirs ne sont pas essayés pour ne pas consommer le quota |
| Décision « Échoué » à l'étape *Bibliothèque* | Une bibliothèque **physique** porte déjà le nom `FeedImport:TargetLibraryName` |
| Décision « Échoué » à l'étape *Import* | Le fichier téléchargé n'a pas pu être remis au pipeline d'import (création de la tâche ou dépôt dans le dossier) |
| Fichier téléchargé mais livre absent | L'import lui-même a échoué : voir la page **Import** de la bibliothèque « À trier » |
| Décision bloquée en « Téléchargement... » | Application arrêtée pendant le téléchargement : le bouton « Relancer » apparaît une heure après le début du téléchargement |
