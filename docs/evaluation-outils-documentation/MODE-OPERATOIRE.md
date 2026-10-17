# Mode opératoire : reproduire les tests de génération de documentation

Ce document décrit pas à pas comment relancer chacun des tests sur MyComicsManager, ou sur un autre dépôt .NET.
Les résultats obtenus sont dans [`resultats/`](resultats/), la synthèse dans [`README.md`](README.md).

## 0. Prérequis communs

| Élément | Version testée | Remarque |
|---|---|---|
| Claude Code (CLI) | 2.1.302 | Connecté à un abonnement Pro/Max (`claude` puis `/login`). Aucune clé API n'est nécessaire. |
| Git | 2.x | |
| Node.js / npm | 22 | Requis par Claude Code et par l'installation de CodeWiki. |
| Python | 3.12 ou plus | Pour CodeWiki et les scripts du kit code-business-reverse. |
| .NET SDK | 10 | Pour générer le script SQL à partir des migrations EF Core. |
| PostgreSQL ou SQL Server | PostgreSQL 16 en local | Base jetable, uniquement pour tbls. |
| Go **ou** Docker | Go 1.2x | Pour installer tbls (sinon binaire ou image Docker). |

> **Règle d'or : travailler sur une copie du dépôt.** Plusieurs outils écrivent dans le dépôt analysé
> (`C4-Documentation/`, `docs/`, `.claude/`). Une copie jetable évite de mélanger leur production avec le code :
>
> ```bash
> git clone ./mycomicsmanager ../eval/mycomics-copie
> ```

**Modèle utilisé.** Tous les tests ont été lancés avec `--model claude-sonnet-5-5`, qui consomme moins de quota
qu'Opus. Les durées indiquées dans les résultats correspondent à ce modèle.

**Mode non interactif.** Les commandes ci-dessous utilisent `claude -p` (mode « headless ») pour être reproductibles
et chronométrées. En usage normal, on peut lancer `claude` dans le dossier et taper la même commande (`/…`) dans la session.
L'option `--output-format json` produit en fin d'exécution un JSON qui contient la durée (`duration_ms`), le nombre
de tours et le coût API équivalent (`total_cost_usd`). Avec un abonnement, ce coût n'est pas facturé : il donne
seulement un ordre de grandeur de la consommation de quota.

**Permissions.** Plutôt que `--dangerously-skip-permissions`, les tests utilisent `--permission-mode acceptEdits`
avec une liste blanche d'outils (`--allowedTools`) en lecture seule, plus l'écriture de fichiers.

---

## Test 1 : tbls, documentation du schéma de base de données (sans LLM, puis enrichie par Claude)

### 1.1 Obtenir une base avec le schéma

MyComicsManager utilise EF Core : on génère le script SQL des migrations, puis on l'applique sur une base jetable.

```bash
dotnet tool install --global dotnet-ef --version 10.0.13
cd mycomics-copie
dotnet build Web/Web.csproj
ConnectionStrings__DefaultConnection="Host=localhost;Database=mycomics;Username=postgres;Password=postgres" \
  dotnet ef migrations script --project Persistence --startup-project Web --no-build -o ../schema.sql

# Base PostgreSQL jetable (Docker)
docker run -d --name pg-doc -e POSTGRES_PASSWORD=postgres -p 55432:5432 postgres:18
psql -h localhost -p 55432 -U postgres -c "create database mycomics"
psql -h localhost -p 55432 -U postgres -d mycomics -f ../schema.sql
```

PowerShell : `$env:ConnectionStrings__DefaultConnection = "Host=..."` puis la même commande `dotnet ef`.

Pour un projet **SQL Server** existant (Dapper, procédures stockées), on pointe directement tbls sur la base de recette.
Le DSN prend la forme `sqlserver://user:motdepasse@serveur:1433?database=MaBase` (SQL Server 2017 ou plus).

### 1.2 Installer et lancer tbls

```bash
go install github.com/k1LoW/tbls@latest      # ou : docker run --rm -v $PWD:/work -w /work ghcr.io/k1low/tbls doc
```

Copier [`scripts/tbls/.tbls.yml`](scripts/tbls/.tbls.yml) dans un dossier de travail, adapter le `dsn`, puis :

```bash
tbls doc --rm-dist     # génère doc/README.md + une page par table + diagrammes Mermaid
tbls lint              # signale les tables sans commentaire, les tables isolées…
```

### 1.3 Enrichir les commentaires métier avec Claude (optionnel)

Le schéma brut ne dit pas *à quoi sert* une colonne. On demande à Claude de remplir la section `comments:` du
`.tbls.yml` à partir du code, puis on régénère :

```bash
claude -p "Le fichier .tbls.yml configure tbls (doc de base de données). Le schéma est décrit dans doc/schema.json. \
Ajoute à .tbls.yml une section 'comments:' (format tbls : liste d'éléments 'table', 'tableComment', 'columnComments') \
qui donne, EN FRANÇAIS, le sens métier de chaque table et de ses colonnes non évidentes, en t'appuyant sur le code de \
<chemin-du-dépôt> (Domain/, Persistence/ : configurations EF, enums). Pour une colonne enum stockée en entier, liste \
les valeurs. N'invente rien : si le code ne permet pas de conclure, n'écris pas de commentaire. Ne modifie que .tbls.yml." \
  --add-dir <chemin-du-dépôt> --model claude-sonnet-5-5 --permission-mode acceptEdits \
  --allowedTools "Read,Glob,Grep,Write,Edit" --output-format json > tbls-claude.json
tbls doc --rm-dist
```

Les commentaires restent dans `.tbls.yml`, versionné à côté du code. On peut ensuite ajouter `tbls diff` ou `tbls lint`
en CI (action GitHub `k1low/setup-tbls`) pour détecter les écarts entre la base et la documentation.

---

## Test 2 : CodeWiki, wiki technique complet

```bash
python3 -m venv .venv-codewiki && . .venv-codewiki/bin/activate      # Windows : .venv-codewiki\Scripts\activate
pip install git+https://github.com/FSoft-AI4Code/CodeWiki.git
codewiki --version

# Mode abonnement Claude Code (pas de clé API)
codewiki config set --provider claude-code --main-model claude-sonnet-5-5 --cluster-model claude-sonnet-5-5

cd mycomics-copie
codewiki generate -o ../resultats/codewiki --github-pages --language fr \
  --include "*.cs,*.razor" --exclude "tests,*Tests*,Migrations,scripts,*.Designer.cs" \
  --doc-type architecture
```

Le résultat est un dossier Markdown, une page par module avec des diagrammes Mermaid, plus un `index.html`
(visionneuse) grâce à `--github-pages`. `codewiki generate --update` met ensuite à jour uniquement les modules modifiés.

> **Problème rencontré :** au premier lancement, la bibliothèque `tiktoken` télécharge un fichier depuis
> `openaipublic.blob.core.windows.net`. Derrière un proxy d'entreprise qui bloque ce domaine, CodeWiki plante dès le
> démarrage (`ProxyError`). Contournement : pré-remplir le cache tiktoken (`TIKTOKEN_CACHE_DIR`) avec le fichier
> `cl100k_base.tiktoken` récupéré par un autre moyen, par exemple via le paquet npm `tiktoken`.

---

## Test 3 : C4 avec le plugin `c4-architecture` (wshobson/agents)

```bash
cd mycomics-copie
claude plugin marketplace add wshobson/agents
claude plugin install c4-architecture@claude-code-workflows --scope project

claude -p "/c4-architecture:c4-architecture Périmètre : Domain, Application, Persistence, Web \
(ignorer tests/, scripts/, Persistence/Migrations, wwwroot). Rédige TOUTE la documentation en français \
(titres, descriptions, libellés des diagrammes Mermaid). Ne modifie aucun fichier de code." \
  --model claude-sonnet-5-5 --permission-mode acceptEdits \
  --allowedTools "Read,Glob,Grep,Write,Edit,Task,Agent,TodoWrite,Bash(ls:*),Bash(find:*),Bash(mkdir:*),Bash(wc:*),Bash(tree:*)" \
  --output-format json > c4-wshobson.json
```

Le résultat est le dossier `C4-Documentation/`, avec une fiche `c4-code-*.md` par répertoire, puis les niveaux
composants, conteneurs et contexte. En interactif, la commande s'utilise telle quelle : `/c4-architecture:c4-architecture`.

---

## Test 4 : C4 avec le skill `c4-codebase-architecture` (lmammino)

```bash
cd mycomics-copie
git clone --depth 1 https://github.com/lmammino/c4-codebase-architecture-skill ../c4-skill
mkdir -p .claude/skills && cp -r ../c4-skill/skills/c4-codebase-architecture .claude/skills/
# Variante : npx skills add lmammino/c4-codebase-architecture-skill --skill c4-codebase-architecture

claude -p "Utilise le skill c4-codebase-architecture pour documenter l'architecture de ce dépôt. Produis les vues \
Contexte, Conteneurs et Composants (pour le conteneur Web/Blazor), en Mermaid, dans docs/architecture-c4/. \
Rédige tout en français. Il n'y a personne pour répondre à tes questions : liste-les dans une section \
« Questions ouvertes » au lieu de les poser. Ne modifie aucun fichier de code." \
  --model claude-sonnet-5-5 --permission-mode acceptEdits \
  --allowedTools "Skill,Read,Glob,Grep,Write,Edit,Task,Agent,TodoWrite,Bash(ls:*),Bash(find:*),Bash(mkdir:*),Bash(wc:*),Bash(cat:*),Bash(git log:*)" \
  --output-format json > c4-lmammino.json
```

En interactif, il vaut mieux **laisser le skill poser ses questions** (acteurs, déploiement, frontières du système) :
c'est ce qui fait sa valeur par rapport aux autres outils.

---

## Test 5 : règles métier avec le kit `code-business-reverse`

Le kit s'exécute **depuis son propre dépôt** et lit l'application en lecture seule (`--add-dir`). Toute la production
reste dans le kit (`projects/<nom>/`).

```bash
git clone https://github.com/BPaulraj/code-business-reverse ../cbr
../scripts/run-cbr.sh ../cbr "$(pwd)" ../cbr-run.log      # voir scripts/run-cbr.sh
```

Le script [`scripts/run-cbr.sh`](scripts/run-cbr.sh) enchaîne six sessions Claude Code **séparées**. Le kit exige une
session neuve pour la vérification, afin que le vérificateur ne relise pas son propre raisonnement :

| Étape | Commande | Produit |
|---|---|---|
| 1 | `/re-init mycomics <chemin>` | `projects/mycomics/project.md` (commit analysé) |
| 2 | `/re-discover` | `component-map.md` (composants, stack, câblage) |
| 3 | `/re-domain` | `01-domain/` : entités, cycles de vie, glossaire, règles portées par la base |
| 4 | `/re-ui front-end` | `05-user-journeys/` + règles d'écran (validations, messages, rôles) |
| 5 | `/re-verify 05-user-journeys` | Contrôle indépendant de chaque règle contre le code cité |
| 6 | `/re-publish draft docx` | `deliverables/` : spécification métier consolidée (.md + .docx) |

En interactif : `cd ../cbr && claude --add-dir <chemin-du-dépôt>`, puis taper les commandes une à une en ouvrant une
nouvelle session (`/clear`) entre chaque étape.

Pour un projet **SQL Server + Dapper**, ajouter `/re-sql --all` : le script `kit/tools/sql_scan.py` inventorie les
requêtes Dapper, les procédures stockées et les tables utilisées par composant.

---

## Test 6 : plugin DEFRA `claude-legacy-reveng-plugin` (agents application et base de données)

Le plugin attend une arborescence précise : le code dans `src/`, la production dans `output/`.

```bash
mkdir defra-eval && cd defra-eval && git init
mkdir -p src screenshots transcripts output src/database
git clone <chemin-du-dépôt> src/mycomicsmanager && rm -rf src/mycomicsmanager/.git src/mycomicsmanager/tests
cp ../schema.sql src/database/          # l'agent base de données cherche des fichiers .sql
git clone --depth 1 https://github.com/DEFRA/claude-legacy-reveng-plugin ../defra-plugin

claude --plugin-dir ../defra-plugin -p "Lance en parallèle les agents application-developer et database-analyst \
du plugin defra-legacy-reveng sur le code de src/ (ils doivent produire output/application-analysis.md et \
output/database-analysis.md). Consigne prioritaire sur leurs instructions : tout le contenu doit être rédigé en \
français (pas en anglais britannique). Ne modifie rien sous src/." \
  --model claude-sonnet-5-5 --permission-mode acceptEdits \
  --allowedTools "Read,Glob,Grep,Write,Edit,Task,Agent,TodoWrite,Bash(mkdir:*),Bash(ls:*)" \
  --output-format json > defra.json
```

La chaîne complète du plugin (`reveng curate` → `synthesise` → `decompose`) part de **captures d'écran et de
transcriptions d'entretiens** pour produire un PRD puis des fiches fonctionnalités. Elle n'a pas été testée ici :
il faudrait fournir des captures de l'application.

---

## Évaluer un résultat

Pour comparer les outils de façon homogène, chaque résultat a été noté sur la même grille :

1. **Exactitude** : vérifier 5 à 10 affirmations au hasard contre le code cité. Une règle sans source ne compte pas.
2. **Couverture** : les écrans et entités principaux sont-ils présents (livres, bibliothèques, import, flux) ?
3. **Niveau métier** : un lecteur non développeur comprend-il le texte, sans noms de classes ?
4. **Traçabilité** : chaque affirmation renvoie-t-elle à un fichier, voire à une ligne ?
5. **Maintenabilité** : peut-on régénérer de façon incrémentale et suivre les écarts en CI ?
6. **Coût** : durée et quota consommé (`duration_ms`, `total_cost_usd` du JSON).
