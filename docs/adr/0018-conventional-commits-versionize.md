# ADR-0018: Conventional Commits + Versionize pour le versioning automatique

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Le projet a besoin de calculer sa version, taguer les releases et générer un
changelog sans étape manuelle. La convention retenue est
[Conventional Commits](https://www.conventionalcommits.org/fr/v1.0.0/)
appliquée au **titre de la Pull Request** (`.github/semantic.yml`,
`titleOnly: true`, types restreints à `feat`/`fix` uniquement — cf.
`CLAUDE.md`, "PR titles must start with feat or fix"), et
[Versionize](https://github.com/versionize/versionize) (dotnet tool)
calcule le bump SemVer depuis les commits accumulés depuis le dernier tag,
dans le job `publish` de `.github/workflows/dotnet-core-publish.yml`.

Cette ADR documente l'état obtenu après une comparaison avec un projet
soeur, **LoreAI** (`github.com/slucky31/LoreAI`), qui a la même stack
(Versionize + Conventional Commits) mais en place depuis plus longtemps, et
avec sa propre ADR sur le sujet
(`docs/adr/0008-versioning-semver-conventional-commits.md` dans ce repo-là).
Plusieurs écarts identifiés par cette comparaison ont été corrigés dans la
foulée ; ils sont documentés ici avec leur raison, pas seulement le
résultat final.

## Options considérées

- **Option A — Versionize + Conventional Commits sur le titre de PR (retenue)**
  - Pour : aucune dépendance Node (contrairement à semantic-release) ;
    dotnet tool natif à l'écosystème .NET ; le titre de PR (validé par un
    check obligatoire) devient le message du commit de squash sur `main`,
    donc Versionize lit une source fiable et déjà validée.
  - Contre : Versionize est un outil de niche (comparé à semantic-release)
    avec un écosystème de plugins plus restreint.
- **Option B — semantic-release**
  - Pour : outil de référence, très documenté, plugins nombreux.
  - Contre : ajoute une dépendance Node à un repo 100% .NET pour un
    bénéfice nul par rapport à Versionize.
- **Option C — GitVersion**
  - Pour : outil .NET natif également.
  - Contre : pensé pour des stratégies de branches (GitFlow) plutôt que le
    trunk-based squash-merge utilisé ici ; ne gère ni tag ni release
    GitHub nativement, aurait fallu le combiner avec des étapes
    supplémentaires pour un calcul de version moins direct que Versionize
    sur ce cas d'usage (un seul `<Version>` partagé, pas de monorepo).

## Décision

- **Source de vérité unique de version** : `<Version>` dans
  `Directory.Build.props`, partagé par les 4 projets `src/`
  (`Application`, `Domain`, `Persistence`, `Web` ne sont jamais versionnés
  indépendamment). Centralisé depuis une duplication dans les 4 `.csproj`
  (voir "Différences avec LoreAI, corrigées" ci-dessous).
- **Versionize** (`dotnet tool install --global Versionize`, job `publish`)
  calcule le bump depuis les commits Conventional Commits accumulés depuis
  le dernier tag (`versionize --exit-insignificant-commits`), avec
  `.versionize` configurant les sections du changelog (`✨ Features`,
  `🐛 Bug Fixes`, `🚀 Performance`). Contrairement à LoreAI, **un
  `CHANGELOG.md` est committé** (pas de `--skip-changelog`) : choix
  assumé, les deux approches sont valables — LoreAI préfère ne garder les
  notes que dans les GitHub Releases.
- **Squash-merge** : le titre de la PR (validé Conventional Commits)
  devient le message du commit sur `main`, que Versionize interprète.
- **Validation du titre de PR** : l'app GitHub **Semantic PR** (Ezard),
  configurée via `.github/semantic.yml`, avec un check `Semantic PR`
  obligatoire dans la protection de branche.
- **Protection de la branche `main`** : ajoutée dans le cadre de ce travail
  (elle n'existait pas avant). Checks obligatoires : `Semantic PR`,
  `build`, `SonarCloud Code Analysis`. `enforce_admins: false` (les
  administrateurs peuvent contourner les checks requis — nécessaire pour
  le point suivant), `allow_force_pushes: false`, `allow_deletions: false`.
- **Le job `publish` pousse le commit de release avec un PAT dédié**
  (`secrets.RELEASE_TOKEN`), pas le `GITHUB_TOKEN` par défaut : une fois
  `main` protégée, le `GITHUB_TOKEN` (qui authentifie comme
  `github-actions[bot]`) est rejeté au push (`GH006: Protected branch
  update failed`) car les checks requis ne se déclenchent jamais sur un
  push direct hors PR. Un PAT appartenant à un compte admin du repo est
  exempté via `enforce_admins: false` — mais **seul un PAT classique
  fonctionne** : un *fine-grained personal access token* ne peut jamais
  contourner les règles de protection de branche, quel que soit
  `enforce_admins` (limitation documentée de GitHub, pas un bug de
  configuration).

## Différences avec LoreAI, identifiées puis corrigées

Cette comparaison a révélé trois écarts, tous corrigés dans ce repo (pas
seulement documentés) :

1. **`<Version>` dupliqué dans 4 `.csproj`** au lieu d'une source unique
   dans `Directory.Build.props` comme chez LoreAI — risque de
   désynchronisation. Versionize scanne tous les `.csproj`/`.props` et
   exige des versions cohérentes entre eux avant de tous les mettre à
   jour (vérifié dans son code source, `DotnetBumpFile.Discover`) : une
   seule entrée dans `Directory.Build.props` suffit et fonctionne
   normalement.
2. **Signal de release trompeur** : le job `publish` laissait l'étape
   Versionize échouer (`--exit-insignificant-commits` retourne un code
   non-zéro quand il n'y a rien à publier) sans `continue-on-error`, ce
   qui faisait apparaître tout le job — et donc le run — en échec rouge à
   chaque commit `chore`/`docs`/`ci`, alors que `docker-web` était déjà
   correctement sauté (`needs: publish`). LoreAI capture le code de sortie
   proprement (`set +e` + variable `released` en sortie de step) et garde
   le job vert. Repris à l'identique ici.
3. **`docker-web` checkoutait le commit d'origine, pas le commit bumpé** :
   sans `ref:` explicite, `actions/checkout` résout le SHA qui a déclenché
   le workflow — **avant** que `publish` pousse son commit de version.
   L'image Docker publiée aurait donc porté un tag `docker/metadata-action`
   correct mais un `<Version>` embarqué obsolète. Corrigé en pinnant
   `ref: main` sur ce checkout, comme le fait LoreAI sur ses jobs `build`
   et `docker`.

Une quatrième différence a été **découverte en débogant la mise en place
du PAT**, sans équivalent direct chez LoreAI à documenter comme tel : la
précédence des identifiants git. `actions/checkout` persiste le token
qu'on lui donne comme `http.extraheader` pour tout le job ; si on ne
change que le `github_token` de l'étape de push (`ad-m/github-push-action`)
sans aussi le mettre sur le `checkout` initial, l'en-tête persistant du
`GITHUB_TOKEN` par défaut prend le pas sur le token embarqué dans l'URL de
l'étape de push — le push continue de s'authentifier comme
`github-actions[bot]`, jamais exempté, quel que soit le contenu de
`RELEASE_TOKEN`. Diagnostiqué par un `git push --dry-run` local avec un
token admin propre (qui passe sans problème, confirmant que la protection
de branche et le PAT étaient corrects) puis corrigé en alignant sur
LoreAI : `token: ${{ secrets.RELEASE_TOKEN }}` sur le `checkout` du job
`publish`, pas seulement sur l'étape de push.

## Conséquences

### Positives

- Version calculée automatiquement, sans étape manuelle, cohérente entre
  tous les projets `src/` grâce à la source unique dans
  `Directory.Build.props`.
- `main` protégée sans bloquer le pipeline de release : les checks de
  qualité (build, Semantic PR, SonarCloud) sont obligatoires pour toute PR
  humaine, tout en laissant le job automatisé de release passer via le PAT
  admin.
- Le run `publish` reste vert pour les commits qui ne déclenchent pas de
  release (`chore`/`docs`/`ci`), au lieu d'afficher un échec trompeur.
- L'image Docker publiée reflète toujours la version qu'elle porte comme
  tag.

### Négatives

- Le PAT `RELEASE_TOKEN` est un classic token à scope `repo` : plus large
  qu'un fine-grained token, et lié à un compte personnel plutôt qu'à une
  identité de service — à renouveler manuellement s'il expire, et à
  regénérer si le compte change de rôle sur le repo.
- Cette dépendance à un compte admin humain est un point de fragilité :
  si l'accès admin de ce compte change, la release casse silencieusement
  au prochain `feat`/`fix` mergé (le job `publish` échouera de façon
  visible, mais seulement à ce moment-là).
- `CHANGELOG.md` committé (contrairement à LoreAI) ajoute un fichier à
  fusionner/résoudre en cas de conflit rare entre deux releases
  concurrentes — non observé à ce jour, usage mono-mainteneur.

### Couches impactées

- [ ] Domain
- [ ] Application
- [ ] Persistence
- [ ] Web

(Décision d'outillage CI/versioning, ne touche aucune couche applicative.)

## Liens

- `.github/workflows/dotnet-core-publish.yml` (jobs `publish`, `docker-web`)
- `.github/semantic.yml`, `.versionize`, `Directory.Build.props`
- [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md), [ADR-0003](0003-postgresql-self-hosted-au-lieu-de-neon.md) — contexte d'hébergement partagé avec les contraintes de ce pipeline
- LoreAI `docs/adr/0008-versioning-semver-conventional-commits.md` — ADR équivalente sur le projet soeur, base de la comparaison
- PR #1025, #1026 — mise en œuvre des corrections listées ci-dessus
