# Vérification de l'exactitude : 120 affirmations contrôlées contre le code

**Méthode.** Pour chaque outil, 20 affirmations ont été tirées au hasard (graine fixe `20261017`, donc tirage
reproductible) dans sa production :

- **Règles** (kit code-business-reverse, DEFRA) : 20 règles parmi toutes celles produites.
- **Commentaires tbls** : 20 commentaires de table ou de colonne parmi ceux rédigés par Claude.
- **Documents d'architecture** (lmammino, wshobson, CodeWiki) : 20 lignes hors diagrammes et hors titres, qui
  contiennent au moins une référence au code (texte entre accents graves).

Chaque affirmation a été vérifiée à la main dans le code, au commit analysé (`631720b`). Les échantillons sont dans
[`echantillons/`](echantillons/).

**Barème :**

- ✅ **Exacte** : le fond est juste et, quand une ligne est citée, elle est juste.
- 🟡 **Imprécise** : juste sur le fond, mais incomplète, mal formulée ou avec une citation décalée.
- ❌ **Fausse** : contredite par le code, ou inventée.

## Résultats

| Outil | ✅ Exactes | 🟡 Imprécises | ❌ Fausses | Traçabilité | Volume produit | Coût |
|---|---|---|---|---|---|---|
| **tbls + Claude** | **20** | 0 | 0 | Commentaire relu dans `.tbls.yml` | 79 commentaires | 0,48 $ |
| **Agent DEFRA** | **20** | 0 | 0 | Fichier seulement, parfois un joker | 63 règles + analyse de la base | 2,52 $ |
| **Kit code-business-reverse** | 19 | 1 | 0 | **Fichier + ligne**, confiance, statut | 80 règles, 15 parcours, spec Word | 13,4 $ |
| **Skill C4 lmammino** | 19 | 1 | 0 | Fichier et ADR | 4 pages, ≈ 40 affirmations référencées | 0,48 $ |
| **Plugin C4 wshobson** | 19 | 1 | 0 | Fichier + ligne (niveau code) | 101 fiches, ≈ 3 800 lignes référencées | 6,95 $ |
| **CodeWiki** | 19 | 1 | 0 | Fichier, nom de classe | 40 pages, ≈ 950 lignes référencées | 10,6 $ |

Le volume ne mesure pas la valeur : les 3 800 lignes de wshobson paraphrasent en grande partie le code (signatures,
paramètres), alors que les 40 affirmations de lmammino sont des constats d'architecture.

### Détail des imprécisions

| Outil | Affirmation | Écart constaté |
|---|---|---|
| Kit code-business-reverse | BR-UI-012 : le tri initial d'une bibliothèque est « plus récents d'abord » | Exact, mais la citation `LibraryUiDto.cs:25` pointe 3 lignes trop bas : la valeur par défaut est à la ligne 22 (ligne 25 = `RelativePath`). |
| Skill lmammino | `Web → Application → Domain` ; `Persistence → Application, Domain` | Incomplet : `Web` référence aussi `Persistence`, en tant que racine de composition (`Web.csproj`). |
| Plugin wshobson | Bubble : « Une date seule est convertie au 1er du mois » | Mal formulé : c'est un **mois** seul qui est converti au 1er du mois (`BubbleService.ParseReleaseDate`). |
| CodeWiki | « `FeedImportDecisionEvent.Create` n'est appelable que depuis l'agrégat » | Surinterprété : la méthode est `internal`, donc appelable depuis tout le projet `Domain`, pas seulement depuis l'agrégat. |

### Exemples d'affirmations vérifiées exactes

- **tbls + Claude :** les 9 valeurs de `FeedImportDecisions.Status` (dont « 6 = Importé (non utilisé) », conforme au
  commentaire de l'enum) et le stockage en entier (pas de `HasConversion`).
- **DEFRA :** espace disque requis de 3 × la taille du fichier, page refusée au-delà de 50 Mo, nom du CBZ
  `{ISBN}.cbz` seulement si l'ISBN est **valide**, cache HTTP privé de 24 h avec ETag, téléchargement considéré
  interrompu après 1 h, liste des erreurs qui arrêtent l'essai des miroirs.
- **Kit :** messages exacts des validateurs (« Volume number must be greater than 0. », « Enter a valid ISBN: 10
  or 13 digits »), jeton de concurrence `xmin` sur les tâches d'import, index unique filtré
  `"Status" NOT IN (6, 7)`, rafraîchissement toutes les 3 secondes tant qu'un import n'est pas terminé.
- **wshobson :** signatures et numéros de ligne (`UnitOfWork.SaveChangesAsync` lignes 10-22, `BooksError.BadRequest`
  lignes 20-23), absence de vérification de `ReadingDateId` dans le handler (constat juste et utile).
- **CodeWiki :** l'upload de la couverture est *best-effort*, et un job bloqué dans un état intermédiaire est passé
  en échec à la reprise (`ProcessImportJobCommandHandler`).
- **lmammino :** `WorkerCount = 1`, image `linux/arm64`, deux `IHostedService`, ordre des catalogues
  BnF → Google → OpenLibrary → Bubble → Bédéthèque, chaque source désactivable.

## Conclusions

1. **Aucune invention.** Sur 120 affirmations, aucune n'est fausse. Avec Sonnet 5.5 et un code propre (Clean
   Architecture, noms explicites), tous les outils sont fiables sur le fond. L'exactitude **ne départage pas** les
   outils.
2. **Ce qui les départage : la traçabilité et le coût.**
   - Les citations à la ligne (kit, wshobson) permettent de vérifier en quelques secondes, mais un décalage reste
     possible (1 cas sur 20 pour le kit).
   - Les citations au fichier (DEFRA, lmammino, CodeWiki) obligent à chercher, mais le contenu est tout aussi juste.
3. **Les imprécisions sont des généralisations** (« seulement depuis l'agrégat », « une date seule »), pas des
   erreurs de lecture. C'est le défaut typique d'un LLM qui résume : à surveiller lors d'une relecture.
4. **Ce que cette vérification ne mesure pas : la complétude.** Un outil peut être exact à 100 % et oublier des
   règles importantes. L'étape `/re-verify` du kit l'a montré : sur les parcours utilisateur, elle a corrigé ou
   complété 117 éléments sur 167, alors que les règles tirées ici sont exactes à 100 % sur le fond.

**Limites :**

- Avec 20 affirmations par outil, la marge d'erreur est large. Un score de 19/20 est compatible avec une
  exactitude réelle d'environ 75 à 99 %.
- Le tirage favorise les phrases qui citent du code (plus faciles à vérifier). Les phrases générales d'introduction
  sont sous-représentées.
- Un seul dépôt, petit et bien structuré. Sur du code legacy (procédures stockées, noms obscurs), les écarts entre
  outils seront probablement plus marqués.
