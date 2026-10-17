# Base de données et modèle de domaine — Règles métier

**Composant :** `Domain/`, `Persistence/` · **Préfixe :** `DB` · **Capacité :** [capability.md](capability.md)

## Résumé

| ID | Titre | Confiance | Statut |
|---|---|---|---|
| BR-DB-001 | Un seul import non terminé par fichier source | Élevée | Brouillon |
| BR-DB-002 | Modifications concurrentes d'une tâche d'import détectées | Moyenne | Brouillon |
| BR-DB-003 | Une seule décision par livre d'un article et par utilisateur | Élevée | Brouillon |
| BR-DB-004 | Modifications concurrentes d'une décision d'import détectées | Moyenne | Brouillon |
| BR-DB-005 | Une seule surcharge par fonctionnalité | Élevée | Brouillon |
| BR-DB-006 | Une seule URL Bédéthèque mémorisée par ISBN | Élevée | Brouillon |
| BR-DB-007 | La suppression d'une bibliothèque supprime ses livres | Élevée | Brouillon |
| BR-DB-008 | La suppression d'un livre supprime ses dates de lecture | Élevée | Brouillon |
| BR-DB-009 | La suppression d'une décision d'import supprime son historique | Élevée | Brouillon |
| BR-DB-010 | Longueurs maximales de la fiche d'un livre | Élevée | Brouillon |
| BR-DB-011 | Longueurs maximales d'une bibliothèque et d'une tâche d'import | Élevée | Brouillon |
| BR-DB-012 | Les dates de création et de modification sont horodatées à l'enregistrement | Élevée | Brouillon |
| BR-DB-013 | Un livre est physique ou numérique, jamais les deux | Moyenne | Brouillon |
| BR-DB-014 | Les clés de protection des sessions survivent à un redéploiement | Moyenne | Brouillon |

Aucune procédure stockée, fonction, vue ni déclencheur n'existe dans le dépôt : toute la logique résidant en base se limite aux contraintes ci-dessous (`Persistence/Migrations/`, voir [capability.md](capability.md)). Numéros de point d'entrée (EP) : voir [capability.md](capability.md).

---

### BR-DB-001 — Un seul import non terminé par fichier source

- **Énoncé :** Tant qu'une tâche d'import n'est ni terminée ni échouée pour un fichier donné, aucune autre tâche ne peut être créée pour le même chemin de fichier ; une fois la tâche terminée ou échouée, le même fichier peut être importé à nouveau.
- **Justification (déduite) :** Empêche d'importer deux fois la même archive, par exemple lorsque la surveillance de dossier détecte le fichier plusieurs fois.
- **Déclencheur :** Création d'une tâche d'import.
- **Conditions / exceptions :** S'applique aux statuts autres que Terminée et Échouée.
- **Résultat en cas de violation :** Enregistrement refusé par la base ; l'erreur technique « DB_UPDATE_ERROR » est renvoyée.
- **Utilisé dans :** EP #8, P-002
- **Source :**
  - `Persistence/ApplicationDbContext.cs:87-90` — index unique filtré sur le chemin du fichier d'origine
  - `Persistence/Migrations/ApplicationDbContextModelSnapshot.cs:349-351` — même index dans le modèle migré
  - `Persistence/UnitOfWork.cs:18-21` — erreur renvoyée en cas de refus
- **Confiance :** Élevée
- **Note technique :** Le filtre repose sur les valeurs numériques 6 (Terminée) et 7 (Échouée) ; à mettre à jour si l'énumération est réordonnée (commentaire du code, `Persistence/ApplicationDbContext.cs:87`). Voir D-DOM-004.
- **Liés :** ENT-ImportJob, D-DOM-004
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-002 — Modifications concurrentes d'une tâche d'import détectées

- **Énoncé :** Si deux traitements modifient en même temps la même tâche d'import, le second enregistrement est refusé au lieu d'écraser silencieusement le premier.
- **Justification (déduite) :** Éviter qu'une relance de la file de tâches et une action manuelle (« marquer comme échouée ») se contredisent.
- **Déclencheur :** Enregistrement d'une tâche d'import déjà modifiée par ailleurs.
- **Conditions / exceptions :** Appliqué par le jeton de version de ligne du système de base de données.
- **Résultat en cas de violation :** Enregistrement refusé ; erreur « DB_UPDATE_ERROR ».
- **Utilisé dans :** EP #8, P-002
- **Source :**
  - `Persistence/ApplicationDbContext.cs:91-95` — colonne de version de ligne utilisée comme jeton de concurrence
  - `Persistence/UnitOfWork.cs:18-21` — toute erreur de mise à jour est convertie en erreur technique
- **Confiance :** Moyenne. Le comportement de refus est celui du composant d'accès aux données ; aucun test ne le vérifie ici.
- **Note technique :** Colonne système `xmin` de PostgreSQL.
- **Liés :** ENT-ImportJob
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-003 — Une seule décision par livre d'un article et par utilisateur

- **Énoncé :** Pour un utilisateur, un livre d'un article Miniflux (repéré par le numéro d'entrée et le rang dans l'article) ne peut avoir qu'une seule décision d'import.
- **Justification (déduite) :** Empêche de traiter deux fois le même article lors de synchronisations répétées.
- **Déclencheur :** Synchronisation des articles favoris ; création des décisions sœurs lors de l'analyse.
- **Conditions / exceptions :** La synchronisation vérifie d'abord l'existence de la décision de rang 0 (`Persistence/Repositories/FeedImportDecisionRepository.cs:18`).
- **Résultat en cas de violation :** Enregistrement refusé par la base.
- **Utilisé dans :** EP #9, P-003
- **Source :**
  - `Persistence/ApplicationDbContext.cs:125` — index unique utilisateur + entrée + rang
  - `Application/FeedImports/Sync/SyncFeedImportsCommandHandler.cs:81-95` — « déjà connue » avant création
- **Confiance :** Élevée
- **Note technique :** Le commentaire de la synchronisation parle d'une unicité sur l'entrée seule (`Application/FeedImports/Sync/SyncFeedImportsCommandHandler.cs:82`) ; l'index réel inclut le rang.
- **Liés :** ENT-FeedImportDecision
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-004 — Modifications concurrentes d'une décision d'import détectées

- **Énoncé :** Si deux traitements modifient en même temps la même décision d'import par flux, le second enregistrement est refusé.
- **Justification (déduite) :** Éviter qu'une action de l'utilisateur et un traitement de fond écrasent mutuellement leurs changements de statut.
- **Déclencheur :** Enregistrement d'une décision déjà modifiée par ailleurs.
- **Conditions / exceptions :** Même mécanisme que BR-DB-002.
- **Résultat en cas de violation :** Enregistrement refusé ; erreur « DB_UPDATE_ERROR ».
- **Utilisé dans :** EP #9, P-003
- **Source :**
  - `Persistence/ApplicationDbContext.cs:131-135` — jeton de concurrence
  - `Persistence/UnitOfWork.cs:18-21`
- **Confiance :** Moyenne
- **Note technique :** Colonne système `xmin`.
- **Liés :** BR-DB-002, ENT-FeedImportDecision
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-005 — Une seule surcharge par fonctionnalité

- **Énoncé :** Une fonctionnalité activable a au plus une valeur choisie par l'administrateur.
- **Justification (déduite) :** La valeur choisie remplace sans ambiguïté celle de la configuration.
- **Déclencheur :** Enregistrement d'un choix dans la page Paramètres.
- **Conditions / exceptions :** Choisir la valeur de la configuration supprime la surcharge au lieu de la stocker.
- **Résultat en cas de violation :** Enregistrement refusé par la base.
- **Utilisé dans :** EP #11, P-007
- **Source :**
  - `Persistence/ApplicationDbContext.cs:108` — index unique sur la fonctionnalité
  - `Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:30-49` — ajout, mise à jour ou suppression
- **Confiance :** Élevée
- **Note technique :** La fonctionnalité est enregistrée sous forme de texte (`Persistence/ApplicationDbContext.cs:107`).
- **Liés :** ENT-FeatureToggleOverride
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-006 — Une seule URL Bédéthèque mémorisée par ISBN

- **Énoncé :** Pour un ISBN donné, une seule adresse de la fiche Bédéthèque est mémorisée.
- **Justification (déduite) :** Éviter de rechercher plusieurs fois la même fiche chez le tiers (cache).
- **Déclencheur :** Mémorisation du résultat d'une recherche Bédéthèque.
- **Conditions / exceptions :** ISBN 20 caractères max, adresse 500 caractères max.
- **Résultat en cas de violation :** Enregistrement refusé par la base.
- **Utilisé dans :** EP #7
- **Source :**
  - `Persistence/ApplicationDbContext.cs:101-104` — table, longueurs et index unique
  - `Persistence/Repositories/IsbnBedethequeCacheRepository.cs:20` — création de l'entrée
- **Confiance :** Élevée
- **Note technique :** Table de cache technique ; voir [capability.md](capability.md).
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-007 — La suppression d'une bibliothèque supprime ses livres

- **Énoncé :** Quand une bibliothèque est supprimée, tous les livres qu'elle contient sont supprimés avec elle.
- **Justification (déduite) :** Un livre ne peut pas exister sans bibliothèque.
- **Déclencheur :** Suppression d'une bibliothèque par son propriétaire.
- **Conditions / exceptions :** Pour une bibliothèque numérique, les dossiers sont supprimés avant l'enregistrement en base.
- **Résultat en cas de violation :** Sans objet.
- **Utilisé dans :** EP #1, EP #3
- **Source :**
  - `Persistence/ApplicationDbContext.cs:66-70` — suppression en cascade livre → bibliothèque
  - `Application/Libraries/Delete/DeleteLibraryCommandHandler.cs:25-37` — dossiers supprimés puis bibliothèque retirée
- **Confiance :** Élevée
- **Note technique :** Les dates de lecture des livres suivent aussi (BR-DB-008). Les tâches d'import de la bibliothèque ne sont pas supprimées : voir D-DOM-006. L'ordre dossiers puis base peut laisser des fichiers supprimés si l'enregistrement échoue : voir D-DOM-007.
- **Liés :** BR-DB-008, D-DOM-006, D-DOM-007
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-008 — La suppression d'un livre supprime ses dates de lecture

- **Énoncé :** Quand un livre est supprimé, toutes ses lectures datées et notées sont supprimées.
- **Justification (déduite) :** Une lecture n'a de sens que rattachée à son livre.
- **Déclencheur :** Suppression d'un livre ou d'une bibliothèque.
- **Conditions / exceptions :** Aucune.
- **Résultat en cas de violation :** Sans objet.
- **Utilisé dans :** EP #3, EP #6
- **Source :**
  - `Persistence/ApplicationDbContext.cs:60-64`
- **Confiance :** Élevée
- **Note technique :** Cascade de clé étrangère.
- **Liés :** BR-DB-007, ENT-ReadingDate
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-009 — La suppression d'une décision d'import supprime son historique

- **Énoncé :** Quand une décision d'import par flux est supprimée, tout son historique de changements de statut est supprimé.
- **Justification (déduite) :** L'historique n'a de sens que rattaché à sa décision.
- **Déclencheur :** Suppression d'une décision par l'utilisateur.
- **Conditions / exceptions :** La suppression est refusée pendant un téléchargement en cours (`Domain/FeedImports/FeedImportDecision.cs:338`).
- **Résultat en cas de violation :** Sans objet.
- **Utilisé dans :** EP #9, EP #10
- **Source :**
  - `Persistence/ApplicationDbContext.cs:127-130`
- **Confiance :** Élevée
- **Note technique :** Cascade de clé étrangère.
- **Liés :** ENT-FeedImportDecision
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-010 — Longueurs maximales de la fiche d'un livre

- **Énoncé :** Dans la fiche d'un livre, la série et le titre comptent au plus 200 caractères, l'ISBN 20, le lien d'image 500, les auteurs 200, les éditeurs 200 et le chemin du fichier numérique 500.
- **Justification (déduite) :** Limites de stockage reprises dans les formulaires.
- **Déclencheur :** Création ou modification d'un livre.
- **Conditions / exceptions :** Le formulaire vérifie ces limites ; ni les gestionnaires ni le domaine ne les vérifient (voir Q-DOM-008).
- **Résultat en cas de violation :** Formulaire : message « ne doit pas dépasser N caractères » ; ailleurs, l'enregistrement est refusé par la base.
- **Utilisé dans :** EP #3, EP #5
- **Source :**
  - `Domain/Books/BookConstants.cs:5-11` — valeurs
  - `Persistence/ApplicationDbContext.cs:54-59`, `Persistence/ApplicationDbContext.cs:75` — limites de colonnes
  - `Web/Validators/BookValidator.cs:13-40` — contrôle dans le formulaire
- **Confiance :** Élevée
- **Note technique :** Règle présente à deux endroits (formulaire et base) ; les valeurs viennent des mêmes constantes.
- **Liés :** Q-DOM-008
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-011 — Longueurs maximales d'une bibliothèque et d'une tâche d'import

- **Énoncé :** Le nom d'une bibliothèque compte au plus 200 caractères, sa couleur 7 et son icône 100 ; pour une tâche d'import, le nom de fichier compte au plus 260 caractères, le chemin 500, le message d'erreur 2000 et l'étape en erreur 100.
- **Justification (déduite) :** Limites de stockage.
- **Déclencheur :** Création ou modification d'une bibliothèque ; création ou échec d'une tâche d'import.
- **Conditions / exceptions :** Pour la bibliothèque, le domaine refuse aussi les valeurs trop longues ou vides. Pour la tâche d'import, seules les valeurs vides sont refusées par le domaine ; les messages d'erreur ne sont pas tronqués par le domaine (`Domain/ImportJobs/ImportJob.cs:114-116`).
- **Résultat en cas de violation :** Bibliothèque : demande refusée (« mauvaise requête ») ; tâche d'import : enregistrement refusé par la base.
- **Utilisé dans :** EP #1, EP #8
- **Source :**
  - `Domain/Libraries/LibraryConstants.cs:5-7`, `Domain/Libraries/Library.cs:43-56` — contrôle du domaine
  - `Persistence/ApplicationDbContext.cs:47-49`, `Persistence/ApplicationDbContext.cs:81-84` — limites de colonnes
  - `Domain/ImportJobs/ImportJobConstants.cs:5-8` — valeurs
- **Confiance :** Élevée
- **Note technique :** Deux décisions regroupées par entité de stockage ; à scinder si le produit les traite séparément.
- **Liés :** Q-DOM-008
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-012 — Les dates de création et de modification sont horodatées à l'enregistrement

- **Énoncé :** À chaque enregistrement, un nouvel élément reçoit sa date de création et un élément modifié reçoit sa date de modification, en heure UTC.
- **Justification (déduite) :** Traçabilité minimale des données.
- **Déclencheur :** Enregistrement de toute entité suivie (livres, bibliothèques, utilisateurs, lectures, décisions, tâches, surcharges, cache).
- **Conditions / exceptions :** La date de création est fixée seulement à l'ajout ; la tâche d'import et la décision portent en plus leurs propres dates (`CreatedAt`).
- **Résultat en cas de violation :** Sans objet.
- **Utilisé dans :** EP #1 à #11
- **Source :**
  - `Persistence/UnitOfWork.cs:24-40`
  - `Domain/Primitives/Entity.cs:10-11`
- **Confiance :** Élevée
- **Note technique :** Mécanisme dans la couche d'accès aux données, pas dans la base.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-013 — Un livre est physique ou numérique, jamais les deux

- **Énoncé :** Chaque livre est soit un livre physique soit un livre numérique ; il possède l'un ou l'autre ensemble de données propres (le fichier et ses réglages de lecture pour le numérique).
- **Justification (déduite) :** Les deux types de bibliothèques ne contiennent chacune que leur type de livre.
- **Déclencheur :** Création d'un livre.
- **Conditions / exceptions :** La compatibilité avec le type de la bibliothèque est contrôlée par l'application (`Domain/Libraries/Library.cs:117-130`).
- **Résultat en cas de violation :** Création refusée (« type de livre incompatible »).
- **Utilisé dans :** EP #3, EP #4, EP #5
- **Source :**
  - `Persistence/ApplicationDbContext.cs:72-74` — une table par sous-type
  - `Persistence/Migrations/ApplicationDbContextModelSnapshot.cs:543-555` — liaison un-à-un par l'identifiant
- **Confiance :** Moyenne. Rien n'empêche en base qu'un livre ait les deux lignes ; l'exclusivité vient du code applicatif.
- **Note technique :** Héritage « une table par type » (ADR 0005, `docs/adr/0005-tpt-pour-hierarchie-book.md`).
- **Liés :** ENT-Book
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-DB-014 — Les clés de protection des sessions survivent à un redéploiement

- **Énoncé :** Les clés qui protègent les cookies d'authentification sont conservées en base, afin que les utilisateurs restent connectés après un redéploiement.
- **Justification (déduite) :** Commentaire du code : éviter de déconnecter tout le monde à chaque déploiement.
- **Déclencheur :** Démarrage de l'application ; émission de cookies d'authentification.
- **Conditions / exceptions :** Les clés sont stockées par le composant standard de protection des données de la plateforme ; leur chiffrement au repos n'est pas configuré dans la base (non vérifié, voir Q-DOM-009).
- **Résultat en cas de violation :** Sans objet.
- **Utilisé dans :** EP #12
- **Source :**
  - `Persistence/ApplicationDbContext.cs:37-38` — table des clés et commentaire
  - `Persistence/Migrations/20261010125019_AddDataProtectionKeys.cs` — création de la table
- **Confiance :** Moyenne
- **Note technique :** `IDataProtectionKeyContext`.
- **Liés :** Q-DOM-009
- **Statut :** Brouillon
- **Commentaires du produit :**
