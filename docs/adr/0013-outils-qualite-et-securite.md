# ADR-0013: Outils de qualité/sécurité — SonarCloud + CodeQL + Renovate (sans DeepSource)

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Le projet avait jusqu'à récemment **trois outils d'analyse statique**
actifs en parallèle, avec un objectif largement chevauchant (qualité du
code C#, détection de bugs/anti-patterns) :

- **SonarCloud** (`.github/workflows/sonarcloud.yml`) — qualité de code +
  couverture de tests (rapports Cobertura), intégré à la CI sur chaque push
  `main` et chaque PR. `SonarAnalyzer.CSharp` est aussi référencé comme
  package analyzer local (`Directory.Packages.props`), donc les mêmes
  règles s'appliquent déjà en local à la compilation.
- **CodeQL** (`.github/workflows/codeql-analysis.yml`) — scan de sécurité
  GitHub natif, sur push `main`/`develop` et en cron hebdomadaire.
- **DeepSource** (`.deepsource.toml`) — analyseur de qualité de code
  générique, configuré a minima (`analyzer = "csharp"`, aucune règle
  personnalisée) : chevauche largement SonarCloud sans apporter de
  vérification distincte utilisée dans ce projet.

**Renovate** (`.github/renovate.json`) répond à un besoin différent (mise à
jour automatisée des dépendances) et n'est pas concerné par cette
redondance.

## Options considérées

- **Option A — Conserver SonarCloud + CodeQL + Renovate, retirer DeepSource (retenue)**
  - Pour : SonarCloud couvre déjà la qualité de code (et tourne aussi en
    local via `SonarAnalyzer.CSharp`) ; CodeQL apporte une couverture de
    sécurité complémentaire spécifique (recherche de vulnérabilités,
    intégrée nativement à GitHub, gratuite) ; DeepSource n'ajoutait aucune
    règle ou capacité non déjà couverte par les deux autres.
  - Contre : perte de la détection éventuelle de patterns spécifiques à
    DeepSource — non identifiée comme utile dans ce projet (config
    minimale, jamais personnalisée).
- **Option B — Garder les trois outils**
  - Pour : défense en profondeur théorique (plusieurs moteurs de règles).
  - Contre : trois rapports à surveiller pour un bénéfice marginal ; bruit
    supplémentaire en CI/PR (commentaires, checks) sans valeur ajoutée
    distincte constatée.
- **Option C — Ne garder qu'un seul outil (ex. SonarCloud seul)**
  - Pour : minimise le nombre d'outils à maintenir.
  - Contre : CodeQL apporte une détection de vulnérabilités de sécurité
    (injections, désérialisation, etc.) que SonarCloud ne couvre pas de la
    même façon ; le retirer réduirait la couverture de sécurité réelle du
    projet, pas seulement le bruit.

## Décision

On conserve **SonarCloud** (qualité de code + couverture) et **CodeQL**
(sécurité), et on **supprime DeepSource** (`.deepsource.toml`) car il ne
couvrait rien qui ne soit déjà couvert par les deux autres. Renovate reste
en place pour les mises à jour de dépendances, sujet distinct.

## Conséquences

### Positives

- Un rapport de qualité de code en moins à surveiller, sans perte de
  couverture réelle.
- Réduction du bruit en CI/PR (un check en moins par commit).

### Négatives

- Si un besoin spécifique à DeepSource apparaît plus tard (règle
  particulière non couverte par SonarCloud), il faudra le réintroduire
  consciemment plutôt que de découvrir qu'il a disparu sans explication —
  cette ADR sert justement de trace pour ce cas.

### Couches impactées

- [ ] Domain
- [ ] Application
- [ ] Persistence
- [ ] Web

(Décision d'outillage CI, ne touche aucune couche applicative.)

## Liens

- `.github/workflows/sonarcloud.yml`, `.github/workflows/codeql-analysis.yml`
- `.github/renovate.json`
- `Directory.Packages.props` — `SonarAnalyzer.CSharp` (analyse locale)
- `.deepsource.toml` — supprimé par cette décision
