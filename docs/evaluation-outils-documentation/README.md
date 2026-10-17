# Évaluation d'outils de génération de documentation à partir du code

**Date :** 17 octobre 2026
**Dépôt analysé :** MyComicsManager, commit `631720b` (.NET 10, Blazor Server, EF Core, PostgreSQL)
**Contrainte :** fonctionner avec un abonnement Claude Code, sans clé API
**Modèle :** `claude-sonnet-5-5` pour tous les tests (certains outils délèguent à Haiku 5.5)

Six outils ont été testés sur deux besoins :

1. **Spécifications métier** : lister les règles métier des écrans et de la base à partir du code et du SQL.
2. **Spécifications techniques et d'architecture** : C4 et au-delà (wiki technique, schéma de base).

Pour relancer chaque test, voir le [mode opératoire](MODE-OPERATOIRE.md). Les productions brutes sont dans [`resultats/`](resultats/).

---

## Synthèse

| # | Outil | Besoin | Popularité | Durée | Quota (équiv. API) | Verdict |
|---|---|---|---|---|---|---|
| 1 | **tbls** | Schéma de base | 4,4k ⭐ | 0,1 s | 0 $ | ✅ Indispensable, déterministe, intégrable en CI |
| 1b | **tbls + Claude** (commentaires métier) | Schéma de base | – | 1,5 min | 0,48 $ | ✅ Excellent rapport qualité/coût |
| 2 | **CodeWiki** | Wiki technique | 1,8k ⭐ (ACL 2026) | voir § 2 | voir § 2 | voir § 2 |
| 3 | **Plugin `c4-architecture`** (wshobson/agents) | C4 complet | 41k ⭐ (catalogue) | ≈ 19 min | **6,95 $** | ⚠️ Très complet, mais très gourmand |
| 4 | **Skill `c4-codebase-architecture`** (lmammino) | C4 (3 vues) | 21 ⭐ | 2 min | 0,48 $ | ✅ Meilleur rapport qualité/coût pour le C4 |
| 5 | **Kit `code-business-reverse`** | Règles métier | 0 ⭐ | ≈ 45 min | **13,4 $** | ✅ Le plus abouti pour les règles métier |
| 6 | **Plugin DEFRA `claude-legacy-reveng`** | Règles métier | 8 ⭐ | ≈ 17 min | 2,52 $ | ✅ Bon complément côté serveur et base |

Les montants sont des **équivalents API** relevés dans le JSON de fin de `claude -p` (`total_cost_usd`). Avec un
abonnement, ils ne sont pas facturés, mais ils mesurent la part de quota consommée.

### Recommandation

| Besoin | Outil principal | Complément |
|---|---|---|
| Documentation de la base | **tbls** en CI, commentaires métier générés une fois par Claude puis relus | – |
| Architecture C4 (contexte, conteneurs, composants) | **Skill lmammino** : rapide, sobre, et il pose des questions | **Plugin wshobson** si l'on veut aussi le niveau code (une fiche par dossier) |
| Spécification métier (écrans, parcours, règles) | **Kit code-business-reverse** : citations à la ligne, vérification indépendante, export Word | **Agents DEFRA** pour les règles des handlers CQRS et de la base |
| Wiki technique navigable | CodeWiki (voir § 2) | – |

---

## Leçon n° 1 : le quota de l'abonnement est la vraie limite

Les quatre premiers tests ont été lancés **en parallèle**. En 12 minutes, ils ont consommé l'équivalent de 13 $ et
épuisé la limite de session de l'abonnement. Tout s'est arrêté, avec 4 heures d'attente avant la réinitialisation.

| Comportement à la limite | Outil |
|---|---|
| La session s'arrête net, le travail déjà écrit sur disque reste | `claude -p` (plugins, skills, kit) |
| Mise en pause automatique jusqu'à la réinitialisation (≈ 4 h) | CodeWiki |

**Règles pratiques :**

- **Un outil à la fois.** Les sessions partagent la même limite que votre session interactive.
- **Préférer les outils reprenables.** Le kit code-business-reverse a repris l'étape interrompue sans rien refaire
  (fichiers `_progress.md`). Le plugin wshobson a pu reprendre parce que ses fiches étaient déjà écrites, en lui
  précisant dans le prompt de ne pas refaire la phase 1. CodeWiki garde un cache des modules.
- **Restreindre le périmètre** des outils exhaustifs (wshobson, CodeWiki) : un projet ou une fonctionnalité à la fois.

Répartition de la consommation par modèle :

| Outil | Sonnet 5.5 | Haiku 5.5 |
|---|---|---|
| wshobson | 4,12 $ (138k tokens produits) | **2,83 $ (767k tokens produits)** : un sous-agent par répertoire |
| code-business-reverse | 13,4 $ | – |
| DEFRA | 2,52 $ | négligeable |
| lmammino, tbls + Claude | 0,48 $ chacun | négligeable |

Les agents DEFRA épinglent le modèle `claude-sonnet-4-20250514`, mais ce modèle n'apparaît pas dans la
consommation : ils ont tourné sur le modèle de la session.

---

## 1. tbls : documentation du schéma de base

**Résultat :** [`resultats/tbls/`](resultats/tbls/README.md), soit une page par table (colonnes, contraintes, index)
et des diagrammes entité-relation Mermaid. Configuration dans [`scripts/tbls/.tbls.yml`](scripts/tbls/.tbls.yml).

- **Sans LLM :** fidèle par construction, régénéré en 0,1 s. `tbls lint` a signalé 5 tables sans commentaire et
  4 tables sans relation (`Users`, `ImportJobs`, `FeatureToggleOverrides`, `IsbnBedethequeUrls`). Ces tables sont
  liées par un identifiant sans clé étrangère, ce qui est une information utile en soi.
- **Avec Claude (1,5 min) :** la section `comments:` du `.tbls.yml` a été remplie en français à partir des entités
  et des configurations EF. Exemple : *« ISBN du livre. Peut être vide pour une BD numérique dont l'ISBN n'est pas
  encore connu ; obligatoire pour une BD physique. »* Claude n'a rien inventé : il n'a pas commenté les colonnes
  évidentes (`Title`, `CreatedOnUtc`).
- **SQL Server :** tbls se connecte directement à une base existante (`sqlserver://…`). C'est le point d'entrée
  naturel pour un projet Dapper avec procédures stockées.

**Limite :** ne décrit pas les règles portées par le code (validations, workflows).

## 2. CodeWiki : wiki technique

<!-- CODEWIKI -->

## 3. Plugin `c4-architecture` (wshobson/agents)

**Résultat :** [`resultats/c4-wshobson/`](resultats/c4-wshobson/c4-context.md), soit 91 fiches `c4-code-*`, 10 fiches
composants, une vue conteneurs, une vue contexte et une spécification OpenAPI des endpoints
(`apis/mycomicsmanager-web-api.yaml`).

- **Points forts :** couverture exhaustive, de bas en haut (chaque dossier est documenté avant d'être agrégé). La vue
  contexte est rédigée pour un non-développeur, avec des personas (propriétaire de collection, administrateur, tâches
  planifiées, surveillance du dossier d'import, supervision). Les incertitudes sont signalées (rôle `Admin` côté Auth0).
- **Points faibles :** c'est l'outil le plus coûteux (≈ 7 $, dont 767k tokens Haiku). Le niveau code (91 fiches)
  paraphrase en grande partie le code et vieillira vite. Le plugin n'est pas reprenable nativement : il a fallu
  préciser la reprise dans le prompt.
- **À retenir :** à utiliser ponctuellement, sur un périmètre restreint, ou en ne gardant que les niveaux
  composants, conteneurs et contexte.

## 4. Skill `c4-codebase-architecture` (lmammino)

**Résultat :** [`resultats/c4-lmammino/`](resultats/c4-lmammino/README.md), soit 4 pages : contexte, conteneurs,
composants du conteneur Web, questions ouvertes.

- **Points forts :** 2 minutes, 0,48 $. La vue conteneurs est juste et dense : Blazor, Hangfire (1 worker) et
  `FileWatcherService` dans le même processus, Tesseract, PostgreSQL partagé staging/prod. Chaque élément cite ses
  preuves (fichiers, ADR) et le skill distingue explicitement **observé**, **inféré** et **risque**.
- **Questions ouvertes pertinentes :** déploiement sur le Raspberry Pi absent du dépôt, Miniflux interne ou externe,
  pourquoi les clients HTTP sont répartis entre `Application` et `Web/Infrastructure`, une clé publiée dans le README.
- **Points faibles :** pas de niveau code ni de vue de déploiement. Ce sont des « prochaines étapes » qu'il propose
  lui-même.
- **À retenir :** en mode interactif, laissez-le poser ses questions : c'est ce qui le distingue.

## 5. Kit `code-business-reverse` : spécification métier

**Résultat :** [`resultats/regles-metier-code-business-reverse/`](resultats/regles-metier-code-business-reverse/deliverables/Business-Requirements-Specification.md),
qui comprend :

- une spécification consolidée (Markdown et [Word](resultats/regles-metier-code-business-reverse/deliverables/Business-Requirements-Specification.docx)) ;
- 80 règles (66 d'écran `BR-UI-*`, 14 de base `BR-DB-*`), chacune avec énoncé métier, déclencheur, message affiché,
  **source au fichier et à la ligne**, niveau de confiance et statut de revue ;
- 15 parcours utilisateur, une matrice des rôles, 7 fiches d'entités avec cycle de vie, un glossaire ;
- des questions ouvertes et des anomalies suspectées.

**Contrôles effectués :**

- 6 citations vérifiées à la main (`LibraryValidator.cs:11-14`, `Login.razor:17`, `AccountsEndpoints.cs:13-17`,
  `NavBar.razor.cs:76`…) : **toutes exactes**.
- L'étape `/re-verify`, menée en session séparée avec trois vérificateurs, a contrôlé 167 éléments des parcours.
  Résultat : 50 corrects, **117 corrigés ou complétés**, aucun retiré. Le premier jet est donc juste sur le fond,
  mais souvent incomplet ou imprécis. **La vérification n'est pas optionnelle.**

**Anomalies trouvées, à trier :**

- D-UI-001 : le changement d'une fonctionnalité activable n'est protégé par le rôle Administrateur que dans
  l'interface, ni le service ni la commande ne le vérifient.
- D-UI-003 et D-UI-004 : tome, pages et date ne sont contrôlés que par le formulaire. L'import depuis le web
  contourne le validateur.
- D-UI-005 : en fin de lecture, deux appels distincts, et l'échec du second est seulement journalisé.

**Limites :**

- Seuls l'interface et la base ont été extraits. Le document indique lui-même que les règles des traitements
  serveur (handlers CQRS) restent à faire, avec `/re-service`.
- Il faut Chrome ou Edge pour rendre les diagrammes Mermaid dans le Word (absents dans ce test).
- Le kit est conçu pour de gros systèmes (vagues, reprise, travail parallèle) : il est un peu lourd pour une petite
  application, et c'est l'outil le plus coûteux de l'évaluation.
- Projet très récent (0 ⭐), mais tout tient dans des procédures Markdown lisibles et modifiables (`kit/`).

## 6. Plugin DEFRA `claude-legacy-reveng`

**Résultat :** [`resultats/defra/`](resultats/defra/application-analysis.md), soit `application-analysis.md`
(fonctionnalités, workflows, **63 règles BR-001 à BR-063**, modèle de domaine, intégrations) et
`database-analysis.md` (schéma, contraintes, règles portées par la base).

- **Points forts :** couvre précisément ce qui manque au kit précédent, à savoir les règles des handlers. Exemples :
  unicité de l'ISBN (`BOK409`), une seule tâche d'import active par fichier (`IMP409B`), espace disque requis de
  3 × la taille du fichier, page refusée au-delà de 50 Mo, transitions de la tâche d'import. Trois règles contrôlées
  dans le code : exactes.
- **Points faibles :**
  - Les sources sont citées au fichier, parfois avec un joker (`Application/**/*Handler.cs`), jamais à la ligne.
  - Il n'y a pas d'étape de vérification.
  - Les agents sont écrits pour du .NET « legacy » (VB, WebForms) et imposent l'anglais britannique, qu'il faut
    contredire dans le prompt.
  - L'agent base de données ne lit que des fichiers `.sql` : il a fallu générer le script des migrations EF.
- **Non testé :** la chaîne complète (`reveng curate` → `synthesise` → `decompose`), qui part de captures d'écran et
  de transcriptions d'entretiens pour produire un PRD et des fiches fonctionnalités.

---

## Pistes pour aller plus loin

- **Combiner les deux outils de règles métier :** le kit pour les écrans, avec vérification, et l'agent DEFRA pour
  les handlers. Ou bien lancer `/re-service` du kit sur `Application/`, pour garder un format et une traçabilité
  homogènes.
- **Écrire un skill maison** inspiré du kit : citation à la ligne obligatoire, vérificateur séparé, gabarit de
  règle en français. Le rendre conforme aux conventions du dépôt (CQRS, `Result<T>`, codes d'erreur `BOK409`…).
- **Mettre la documentation en CI :** `tbls diff` sur la base ; régénérer les vues C4 et la spécification métier à
  chaque release, et non à chaque commit, pour ménager le quota.
