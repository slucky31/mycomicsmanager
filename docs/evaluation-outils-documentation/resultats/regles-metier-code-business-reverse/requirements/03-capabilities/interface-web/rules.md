# Interface web (Blazor) — Règles métier

**Composant :** `Web/Components/` · **Préfixe :** `UI` · **Capacité :** [capability.md](capability.md)

Règles visibles à l'écran : validations de saisie, activation / désactivation des boutons, confirmations, messages, permissions d'affichage. Quand une règle existe aussi côté serveur, elle est référencée (BR-DB-xxx) ou signalée comme « non retrouvée côté serveur » avec une anomalie. Les numéros d'entrée (EP) renvoient à [_progress.md](_progress.md). Les textes d'écran de l'application sont en anglais ; ils sont cités tels quels entre guillemets.

## Résumé

| ID | Titre | Confiance | Statut |
|---|---|---|---|
| BR-UI-001 | Toute page exige une connexion, sauf la connexion et la page d'erreur | Élevée | Brouillon |
| BR-UI-002 | L'administration est réservée au rôle Administrateur dans l'interface | Élevée | Brouillon |
| BR-UI-003 | Après connexion, l'utilisateur arrive sur la liste des bibliothèques | Élevée | Brouillon |
| BR-UI-004 | Une pastille signale les arbitrages d'import par flux en attente | Moyenne | Brouillon |
| BR-UI-005 | Le nom d'une bibliothèque est obligatoire et limité à 200 caractères | Élevée | Brouillon |
| BR-UI-006 | La couleur et l'icône d'une bibliothèque sont obligatoires | Élevée | Brouillon |
| BR-UI-007 | Le type d'une bibliothèque se choisit à la création et ne change plus | Élevée | Brouillon |
| BR-UI-008 | Les erreurs d'un formulaire apparaissent à l'envoi puis suivent la saisie | Élevée | Brouillon |
| BR-UI-009 | Un formulaire ne peut pas être envoyé deux fois pendant l'enregistrement | Élevée | Brouillon |
| BR-UI-010 | Une bibliothèque portant déjà ce nom est refusée | Moyenne | Brouillon |
| BR-UI-011 | La suppression d'une bibliothèque demande une confirmation | Élevée | Brouillon |
| BR-UI-012 | Le tri choisi devient le tri par défaut de la bibliothèque | Élevée | Brouillon |
| BR-UI-013 | Le mode d'affichage et la recherche sont mémorisés par bibliothèque | Moyenne | Brouillon |
| BR-UI-014 | La recherche porte sur le titre ou la série, après une courte pause | Élevée | Brouillon |
| BR-UI-015 | Les livres s'affichent par pages de 24, à la demande | Élevée | Brouillon |
| BR-UI-016 | Les actions d'une bibliothèque dépendent de son type | Élevée | Brouillon |
| BR-UI-017 | Un livre papier ne s'ajoute que dans une bibliothèque physique | Élevée | Brouillon |
| BR-UI-018 | Un ISBN scanné invalide est refusé | Élevée | Brouillon |
| BR-UI-019 | L'ISBN saisi à la main est contrôlé avant la recherche | Élevée | Brouillon |
| BR-UI-020 | Un livre trouvé s'ajoute en un geste, avec une bibliothèque obligatoire | Élevée | Brouillon |
| BR-UI-021 | Livre introuvable : le formulaire est proposé, ISBN prérempli | Élevée | Brouillon |
| BR-UI-022 | Le titre et la série d'un livre sont obligatoires | Élevée | Brouillon |
| BR-UI-023 | L'ISBN d'un livre physique est obligatoire et doit être valide | Élevée | Brouillon |
| BR-UI-024 | Le tome et le nombre de pages doivent être supérieurs à zéro | Moyenne | Brouillon |
| BR-UI-025 | La date de parution ne peut pas être dans le futur | Moyenne | Brouillon |
| BR-UI-026 | Longueurs maximales des champs d'un livre | Élevée | Brouillon |
| BR-UI-027 | La note à l'ajout est facultative, de 1 à 5 | Élevée | Brouillon |
| BR-UI-028 | Un ISBN déjà présent est refusé à l'ajout et à la modification | Élevée | Brouillon |
| BR-UI-029 | Une lecture ajoutée depuis la fiche exige une note de 1 à 5 | Élevée | Brouillon |
| BR-UI-030 | Une lecture se supprime depuis la modification du livre, après confirmation | Élevée | Brouillon |
| BR-UI-031 | La suppression d'un livre demande une confirmation | Élevée | Brouillon |
| BR-UI-032 | Un livre ne se déplace que vers une autre bibliothèque du même type | Élevée | Brouillon |
| BR-UI-033 | Le téléchargement d'un livre est réservé à son propriétaire et aux livres numériques | Élevée | Brouillon |
| BR-UI-034 | Le lancement de la recherche des ISBN manquants est signalé et non répétable pendant son démarrage | Élevée | Brouillon |
| BR-UI-035 | L'issue de la recherche d'ISBN dans les pages oriente l'utilisateur | Élevée | Brouillon |
| BR-UI-036 | Les catalogues en ligne sont interrogés en parallèle et indépendamment | Élevée | Brouillon |
| BR-UI-037 | L'utilisateur choisit la source de chaque champ ; le reste est conservé | Élevée | Brouillon |
| BR-UI-038 | La couverture externe est copiée chez l'hébergeur d'images avant l'enregistrement | Élevée | Brouillon |
| BR-UI-039 | Un ISBN lu dans les pages est enregistré sur le livre | Élevée | Brouillon |
| BR-UI-040 | Une recherche en ligne n'est pas relancée pour le même livre et le même ISBN | Moyenne | Brouillon |
| BR-UI-041 | Le lecteur reprend à la dernière page lue | Élevée | Brouillon |
| BR-UI-042 | La progression de lecture est enregistrée après une courte pause et à la sortie | Élevée | Brouillon |
| BR-UI-043 | En fin de livre, l'utilisateur peut le marquer comme lu avec une note | Élevée | Brouillon |
| BR-UI-044 | Les réglages d'affichage du lecteur sont mémorisés par livre | Élevée | Brouillon |
| BR-UI-045 | Le sens de lecture inverse les commandes de page | Élevée | Brouillon |
| BR-UI-046 | Le mode recherche d'ISBN ne sauvegarde pas la progression | Élevée | Brouillon |
| BR-UI-047 | La lecture du texte des pages propose les ISBN trouvés | Élevée | Brouillon |
| BR-UI-048 | L'import de fichiers ne propose que les bibliothèques numériques | Élevée | Brouillon |
| BR-UI-049 | Formats et nombre de fichiers acceptés à l'import | Élevée | Brouillon |
| BR-UI-050 | Taille maximale d'un fichier importé | Moyenne | Brouillon |
| BR-UI-051 | Le suivi des imports se rafraîchit tant qu'un import n'est pas terminé | Élevée | Brouillon |
| BR-UI-052 | Les imports terminés ou échoués peuvent être effacés de la liste | Élevée | Brouillon |
| BR-UI-053 | Un import bloqué peut être déclaré en échec | Moyenne | Brouillon |
| BR-UI-054 | Les décisions d'import par flux se filtrent par statut et se recherchent | Élevée | Brouillon |
| BR-UI-055 | La synchronisation manuelle n'existe que si l'import par flux est activé | Élevée | Brouillon |
| BR-UI-056 | Un doublon probable demande une décision de l'utilisateur | Élevée | Brouillon |
| BR-UI-057 | Des liens ambigus demandent un choix de l'utilisateur | Élevée | Brouillon |
| BR-UI-058 | Les actions manuelles sur une décision dépendent de son état | Moyenne | Brouillon |
| BR-UI-059 | La correction de série et de volume exige une série | Élevée | Brouillon |
| BR-UI-060 | Une seule action à la fois sur les décisions d'import | Élevée | Brouillon |
| BR-UI-061 | La suppression de décisions demande une confirmation et épargne celles en cours | Élevée | Brouillon |
| BR-UI-062 | La purge des décisions téléchargées demande une confirmation | Élevée | Brouillon |
| BR-UI-063 | Les statistiques portent sur toutes les bibliothèques ou sur une seule | Élevée | Brouillon |
| BR-UI-064 | La page des paramètres montre le compte, la santé du système et la version | Moyenne | Brouillon |
| BR-UI-065 | Un administrateur active ou désactive les fonctionnalités sans redéploiement | Élevée | Brouillon |
| BR-UI-066 | Un administrateur règle les niveaux de journalisation jusqu'au redémarrage | Élevée | Brouillon |

---

### BR-UI-001 — Toute page exige une connexion, sauf la connexion et la page d'erreur

- **Énoncé :** Un visiteur non connecté qui demande une page de l'application est renvoyé vers l'écran de connexion ; seuls l'écran de connexion et la page d'erreur sont accessibles sans être connecté.
- **Justification (déduite) :** Les bibliothèques et les livres sont personnels.
- **Déclencheur :** Navigation vers une page.
- **Conditions / exceptions :** Les pages « Connexion » et « Erreur » sont explicitement anonymes.
- **Résultat en cas de violation :** Redirection vers l'écran de connexion.
- **Utilisé dans :** UJ-FO-001, EP #1, EP #2
- **Source :**
  - `Web/Components/Pages/_Imports.razor:1` — autorisation exigée par défaut sur toutes les pages
  - `Web/Components/Pages/Login.razor:1` — connexion anonyme
  - `Web/Components/Pages/Error.razor:1` — page d'erreur anonyme
  - `Web/Components/Routes.razor:3-7` — page non autorisée remplacée par la redirection vers la connexion
- **Confiance :** Élevée
- **Note technique :** Le même principe s'applique aux deux adresses d'API (`Web/EndPoints/BooksEndpoints.cs:17-18`, autorisation exigée).
- **Liés :** Q-UI-003
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-002 — L'administration est réservée au rôle Administrateur dans l'interface

- **Énoncé :** La section « Administration » des paramètres (niveaux de journalisation, tableau de bord des tâches de fond, fonctionnalités activables) n'est affichée qu'aux administrateurs, et la page des niveaux de journalisation refuse les autres utilisateurs.
- **Justification (déduite) :** Ces réglages touchent le fonctionnement de toute l'application.
- **Déclencheur :** Affichage des paramètres ; accès à `/admin/log-levels`.
- **Conditions / exceptions :** Rôle `Admin`. Le tableau de bord des tâches de fond est protégé de la même façon côté serveur.
- **Résultat en cas de violation :** Section absente ; page des niveaux de journalisation refusée (redirection vers la connexion).
- **Utilisé dans :** UJ-FO-015, EP #17, EP #18
- **Source :**
  - `Web/Components/Pages/UserSettings.razor:74-90` — section visible pour le rôle `Admin` seulement
  - `Web/Components/Pages/Admin/LogLevels.razor:2` — page réservée au rôle `Admin`
  - `Web/Infrastructure/HangfireAuthorizationFilter.cs:10-11` — tableau de bord protégé côté serveur
- **Confiance :** Élevée
- **Note technique :** La façon dont le rôle `Admin` arrive dans l'identité (réclamation Auth0) n'est pas dans le dépôt. Le changement des fonctionnalités n'a pas de contrôle de rôle propre côté serveur : voir D-UI-001.
- **Liés :** D-UI-001, Q-UI-003
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-003 — Après connexion, l'utilisateur arrive sur la liste des bibliothèques

- **Énoncé :** Le bouton « Login » mène à la connexion externe, puis l'utilisateur est renvoyé sur la liste de ses bibliothèques.
- **Déclencheur :** Clic sur « Login ».
- **Conditions / exceptions :** Aucune.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-001, EP #1
- **Source :** `Web/Components/Pages/Login.razor:17` — adresse de connexion avec retour sur `/libraries/list`
- **Confiance :** Élevée
- **Note technique :** Le point d'entrée `/Account/Login` accepte une adresse de retour libre fournie dans l'URL (`Web/EndPoints/AccountsEndpoints.cs:13-17`) ; voir USR.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-004 — Une pastille signale les arbitrages d'import par flux en attente

- **Énoncé :** Dans la navigation, l'entrée « Feeds » affiche le nombre de décisions d'import qui attendent un arbitrage de l'utilisateur, dès qu'il est supérieur à zéro.
- **Déclencheur :** Affichage de la navigation ; changement signalé par la page des flux.
- **Conditions / exceptions :** L'entrée « Feeds » est affichée même si l'import par flux est désactivé (voir Q-UI-005).
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-012, EP #19
- **Source :**
  - `Web/Components/Layout/NavBar.razor:38-48` — entrée « Feeds » et pastille
  - `Web/Components/Layout/NavBar.razor.cs:76` — comptage des décisions en attente d'arbitrage
- **Confiance :** Moyenne. Le calcul exact du compte est dans le service d'import par flux (non relu ici).
- **Note technique :** Mise à jour par l'événement `DecisionsChanged` (`Web/Components/Layout/NavBar.razor.cs:24`).
- **Liés :** Q-UI-005
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-005 — Le nom d'une bibliothèque est obligatoire et limité à 200 caractères

- **Énoncé :** Une bibliothèque ne peut être créée ou renommée sans nom, et le nom ne dépasse pas 200 caractères.
- **Déclencheur :** Envoi du formulaire de création ou de modification.
- **Conditions / exceptions :** Champ marqué « obligatoire » dans le formulaire.
- **Résultat en cas de violation :** Messages « Name is required. » / « Name must not exceed 200 characters. » ; l'enregistrement n'est pas lancé.
- **Utilisé dans :** UJ-FO-003, UJ-FO-004, EP #4, EP #5
- **Source :**
  - `Web/Validators/LibraryValidator.cs:11-14` — règle et messages
  - `Domain/Libraries/LibraryConstants.cs:5` — longueur maximale
  - `Web/Components/Pages/Libraries/LibraryForm.razor:22` — champ obligatoire
- **Confiance :** Élevée
- **Note technique :** Doublon côté base : BR-DB-011 (longueur). Le côté serveur refuse une requête invalide avec une erreur de validation (`Application/Libraries/Update/UpdateLibraryCommandHandler.cs:15`).
- **Liés :** BR-DB-011
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-006 — La couleur et l'icône d'une bibliothèque sont obligatoires

- **Énoncé :** Une bibliothèque a toujours une couleur au format `#RRGGBB` et une icône (100 caractères au plus) ; sans choix, la couleur proposée est un indigo et l'icône est « collection de livres ».
- **Déclencheur :** Choix dans le formulaire ; envoi.
- **Conditions / exceptions :** Les couleurs sont choisies dans une palette fixe ; les icônes dans une liste.
- **Résultat en cas de violation :** « Color is required. », « Invalid color format. », « Icon is required. », « Icon name must not exceed 100 characters. »
- **Utilisé dans :** UJ-FO-003, UJ-FO-004, EP #4, EP #5
- **Source :**
  - `Web/Validators/LibraryValidator.cs:16-24` — règles
  - `Web/Validators/LibraryUiDto.cs:13-17` — valeurs par défaut (`#5C6BC0`, `CollectionsBookmark`)
  - `Domain/Libraries/LibraryConstants.cs:6-7` — longueurs
- **Confiance :** Élevée
- **Note technique :** Les valeurs par défaut sont ensuite toujours valides : l'utilisateur ne peut pas laisser ces champs vides depuis l'écran.
- **Liés :** BR-DB-011
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-007 — Le type d'une bibliothèque se choisit à la création et ne change plus

- **Énoncé :** À la création, l'utilisateur choisit si la bibliothèque contient des livres papier (« Physical books », par défaut) ou des fichiers numériques (« Digital books ») ; ce type ne peut plus être modifié ensuite.
- **Rationale (déduite) :** Un livre papier ne vit pas dans une bibliothèque de fichiers, et inversement.
- **Déclencheur :** Formulaire de création ; formulaire de modification.
- **Conditions / exceptions :** À la modification, le choix du type n'est pas proposé et la demande de mise à jour ne le transporte pas.
- **Résultat en cas de violation :** n/a (le choix n'existe pas)
- **Utilisé dans :** UJ-FO-003, UJ-FO-004, EP #4, EP #5
- **Source :**
  - `Web/Components/Pages/Libraries/LibraryForm.razor:9-11,28-37` — type choisi à la création seulement, mentions « cannot be changed »
  - `Web/Components/Pages/Libraries/EditLibrary.razor.cs:114` — la mise à jour envoie nom, couleur et icône seulement
  - `Web/Validators/LibraryValidator.cs:26-27` — type valide exigé
- **Confiance :** Élevée
- **Note technique :** Aucun contrôle équivalent n'a été cherché côté serveur ; la demande de mise à jour ne contient simplement pas le type.
- **Liés :** BR-UI-016
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-008 — Les erreurs d'un formulaire apparaissent à l'envoi puis suivent la saisie

- **Énoncé :** Un formulaire (bibliothèque, ajout ou modification de livre) n'affiche aucune erreur avant le premier envoi ; après un envoi refusé, chaque erreur se met à jour à chaque modification d'un champ.
- **Déclencheur :** Envoi, puis saisie.
- **Conditions / exceptions :** Un livre trouvé en un geste qui échoue à la validation bascule vers le formulaire détaillé pour montrer l'erreur.
- **Résultat en cas de violation :** Résumé d'erreur et message sous le champ concerné.
- **Utilisé dans :** UJ-FO-003, UJ-FO-004, UJ-FO-006, UJ-FO-007, EP #4, #5, #9, #11
- **Source :**
  - `Web/Components/Pages/Libraries/AddLibrary.razor.cs:26-33` — nouvelle validation seulement après un premier envoi
  - `Web/Components/Pages/Libraries/EditLibrary.razor.cs:88-95` — idem
  - `Web/Components/Pages/Books/AddBookForm.razor.cs:151-158,167-174` — idem, avec bascule vers le formulaire
  - `Web/Components/Pages/Books/EditBook.razor.cs:96-103` — idem
- **Confiance :** Élevée
- **Note technique :** Pur comportement d'affichage ; conservé car la restitution des erreurs est visible de l'utilisateur.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-009 — Un formulaire ne peut pas être envoyé deux fois pendant l'enregistrement

- **Énoncé :** Pendant l'enregistrement d'une bibliothèque, d'un livre ou de ses modifications, les boutons d'envoi sont désactivés et un second envoi est ignoré.
- **Déclencheur :** Clic ou touche d'envoi.
- **Conditions / exceptions :** Garde propre à l'écran ; l'envoi est seulement ignoré pour une même page ouverte.
- **Résultat en cas de violation :** Second envoi ignoré sans message.
- **Utilisé dans :** UJ-FO-003, UJ-FO-004, UJ-FO-006, UJ-FO-007
- **Source :**
  - `Web/Components/Pages/Libraries/AddLibrary.razor.cs:37-40` et `AddLibrary.razor:10,17` — garde et bouton désactivé
  - `Web/Components/Pages/Libraries/EditLibrary.razor.cs:99-102` et `EditLibrary.razor:24,31` — idem
  - `Web/Components/Pages/Books/AddBookForm.razor.cs:162-165` et `AddBookForm.razor:61,71,84` — idem
  - `Web/Components/Pages/Books/EditBook.razor.cs:107-110` et `EditBook.razor:27,78` — idem
- **Confiance :** Élevée
- **Note technique :** Pas de clé d'idempotence côté serveur : un appel répété hors de l'écran (nouvel onglet, réessai réseau) n'est pas bloqué par cette garde. Pour les livres, l'unicité de l'ISBN n'est contrôlée que par l'application (D-DOM-002) ; pour les bibliothèques, l'unicité du nom aussi (D-DOM-001).
- **Liés :** D-DOM-001, D-DOM-002
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-010 — Une bibliothèque portant déjà ce nom est refusée

- **Énoncé :** Créer ou renommer une bibliothèque avec le nom d'une autre bibliothèque est refusé avec le message « Another library already has this name ».
- **Déclencheur :** Enregistrement d'une bibliothèque.
- **Conditions / exceptions :** Comparaison faite par le serveur ; l'écran ne fait que présenter le message.
- **Résultat en cas de violation :** Message d'erreur ; l'utilisateur reste sur le formulaire.
- **Utilisé dans :** UJ-FO-003, UJ-FO-004
- **Source :**
  - `Web/Models/LibraryDisplay.cs:30` — message pour l'erreur « doublon »
  - `Web/Components/Pages/Libraries/AddLibrary.razor.cs:61` — message présenté à la création
  - `Application/Libraries/Create/CreateLibraryCommandHandler.cs:19` — refus côté serveur
- **Confiance :** Moyenne. La règle de comparaison (casse, accents) n'est pas relue ici.
- **Note technique :** Non garanti en base : D-DOM-001.
- **Liés :** D-DOM-001
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-011 — La suppression d'une bibliothèque demande une confirmation

- **Énoncé :** Avant de supprimer une bibliothèque, l'utilisateur doit confirmer par un message qui rappelle que la bibliothèque est supprimée avec ses livres et que l'opération est irréversible ; le bouton de confirmation est rouge et il existe un bouton « Cancel ».
- **Déclencheur :** Action « Delete library » du menu de la bibliothèque.
- **Conditions / exceptions :** Aucune exception ; pas de saisie du nom à retaper.
- **Résultat en cas de violation :** Sans confirmation, rien n'est supprimé.
- **Utilisé dans :** UJ-FO-004, EP #3
- **Source :**
  - `Web/Components/Pages/Libraries/LibrariesList.razor.cs:60-67` — message « The library “…” will be deleted with its … This cannot be undone. »
  - `Web/Extensions/DialogServiceExtensions.cs:13` — couleur d'erreur par défaut
  - `Web/Components/Pages/Dialogs/ConfirmationDialog.razor:12-13` — bouton « Cancel » et bouton d'action
  - `Web/Components/Pages/Libraries/LibrariesList.razor:72` — action marquée « Danger »
- **Confiance :** Élevée
- **Note technique :** Pas de verrou contre un double clic pendant la suppression elle-même (la boîte de dialogue se ferme avant l'appel). Effets côté serveur : BR-DB-007, D-DOM-006, D-DOM-007.
- **Liés :** BR-DB-007, D-DOM-007, Q-UI-007
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-012 — Le tri choisi devient le tri par défaut de la bibliothèque

- **Énoncé :** Quand l'utilisateur choisit un tri (plus récents d'abord, plus anciens d'abord, série puis tome) dans une bibliothèque, ce choix est enregistré sur la bibliothèque et réutilisé à sa prochaine ouverture ; le tri initial d'une bibliothèque est « plus récents d'abord ».
- **Déclencheur :** Choix dans « Sort and view ».
- **Conditions / exceptions :** Un choix identique au tri actuel ne fait rien. Un nouveau choix annule le précédent s'il est encore en cours.
- **Résultat en cas de violation :** Échec : message « Failed to save sort order » et le tri affiché ne change pas.
- **Utilisé dans :** UJ-FO-005, EP #6
- **Source :**
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor:108-114` — trois choix de tri
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:360-410` — enregistrement puis rechargement
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:212` — tri initial = tri par défaut de la bibliothèque
  - `Web/Validators/LibraryUiDto.cs:25` — valeur par défaut « plus récents d'abord »
- **Confiance :** Élevée
- **Note technique :** Enregistré via la mise à jour de la bibliothèque, sans changer son nom.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-013 — Le mode d'affichage et la recherche sont mémorisés par bibliothèque

- **Énoncé :** Quand l'utilisateur quitte une bibliothèque puis y revient, le mode d'affichage (couvertures ou liste) et la recherche en cours sont retrouvés ; le mode initial est « couvertures ».
- **Déclencheur :** Sortie puis retour sur la page d'une bibliothèque.
- **Conditions / exceptions :** La durée de conservation n'est pas établie.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-005, EP #6
- **Source :**
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:178` — état enregistré à la sortie
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:195-201` — état restauré
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:98` — mode initial couvertures
- **Confiance :** Moyenne. Durée de vie du service de mémorisation non vérifiée.
- **Note technique :** Service `LibraryStateService`.
- **Liés :** Q-UI-006
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-014 — La recherche porte sur le titre ou la série, après une courte pause

- **Énoncé :** La recherche d'une bibliothèque cherche dans le titre et la série des livres et s'applique 300 ms après la dernière frappe ; sans résultat, l'écran affiche « No results for “…” » avec une action pour effacer la recherche.
- **Déclencheur :** Saisie dans le champ de recherche.
- **Conditions / exceptions :** Une frappe plus récente annule la recherche précédente.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-005, EP #6
- **Source :**
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor:28,77-84` — champ « Search by title or series » et message sans résultat
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:316-331` — pause de 300 ms
- **Confiance :** Élevée
- **Note technique :** La portée réelle de la recherche (colonnes) est dans la requête de livres (module Livres).
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-015 — Les livres s'affichent par pages de 24, à la demande

- **Énoncé :** Une bibliothèque affiche ses livres 24 par 24 ; la page suivante se charge quand l'utilisateur atteint le bas de la liste. Un échec de chargement affiche « Failed to load books » sans effacer les livres déjà affichés.
- **Déclencheur :** Ouverture de la bibliothèque ; défilement.
- **Conditions / exceptions :** Un nouveau tri ou une nouvelle recherche rend caduque toute page demandée avant.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-005, EP #6
- **Source :**
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:30` — taille de page 24
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:229-312` — chargement de la première page et des suivantes, résultats périmés ignorés
- **Confiance :** Élevée
- **Note technique :** Le tri stable (clé de départage) est côté serveur.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-016 — Les actions d'une bibliothèque dépendent de son type

- **Énoncé :** Dans une bibliothèque numérique, la barre propose « More actions » (Import, recherche des ISBN manquants) et chaque livre propose Lire, Chercher l'ISBN et Télécharger ; dans une bibliothèque physique, la barre propose « New book » (ajout par scan, ISBN saisi ou saisie manuelle) et ces actions sont absentes. Une bibliothèque vide propose « Import » (numérique) ou « Add your first book » (physique).
- **Déclencheur :** Affichage de la bibliothèque.
- **Conditions / exceptions :** Actions communes : modifier, importer l'information depuis le web, déplacer, supprimer.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-005, UJ-FO-006, UJ-FO-014, EP #6
- **Source :**
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor:38-46,62-75,166-179` — actions selon le type
  - `Web/Components/Pages/Libraries/LibrariesList.razor:67-70` — « Import books » seulement pour une bibliothèque numérique
- **Confiance :** Élevée
- **Note technique :** Test `IsDigital` sur le type de la bibliothèque (`LibraryDetailPage.razor.cs:101`).
- **Liés :** BR-UI-007
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-017 — Un livre papier ne s'ajoute que dans une bibliothèque physique

- **Énoncé :** Le formulaire d'ajout ne propose que les bibliothèques physiques ; la bibliothèque d'où l'on vient est présélectionnée, sinon la bibliothèque physique unique s'il n'y en a qu'une, sinon l'utilisateur doit choisir.
- **Déclencheur :** Arrivée sur le formulaire d'ajout.
- **Conditions / exceptions :** Seules les 100 premières bibliothèques triées par nom sont chargées (voir Q-UI-009). Échec de chargement : « Unable to load your libraries ».
- **Résultat en cas de violation :** Message « Choose a library » à l'envoi si aucune n'est choisie.
- **Utilisé dans :** UJ-FO-006, EP #9
- **Source :**
  - `Web/Components/Pages/Books/AddBookForm.razor.cs:84-98` — filtre sur le type physique et présélection
  - `Web/Validators/AddBookValidator.cs:10-11` — bibliothèque obligatoire
- **Confiance :** Élevée
- **Note technique :** Le serveur vérifie aussi que la bibliothèque existe et peut recevoir le livre (`Application/Books/Create/CreateBookCommandHandler.cs:44-74`).
- **Liés :** Q-UI-009
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-018 — Un ISBN scanné invalide est refusé

- **Énoncé :** Quand le lecteur de code-barres détecte un ISBN dont la clé de contrôle est fausse, l'écran affiche « Invalid ISBN scanned: … » et reprend le scan ; un ISBN valide mène à la recherche du livre.
- **Déclencheur :** Détection d'un code-barres.
- **Conditions / exceptions :** Si la caméra est inutilisable, un message unique est affiché avec une possibilité de réessayer.
- **Résultat en cas de violation :** Message d'erreur, scan repris.
- **Utilisé dans :** UJ-FO-006, EP #7
- **Source :**
  - `Web/Components/Pages/Books/AddBookScan.razor.cs:132-140` — contrôle et message
  - `Web/Components/Pages/Books/AddBookScan.razor.cs:61-66,144-151` — caméra indisponible
  - `Application/Helpers/IsbnHelper.cs:5-28` — ISBN valide = 10 caractères (clé ISBN-10, X permis) ou 13 chiffres (clé ISBN-13)
- **Confiance :** Élevée
- **Note technique :** Contrôle également fait dans le script du navigateur (`ValidateIsbn`, `AddBookScan.razor.cs:153-157`).
- **Liés :** BR-UI-019
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-019 — L'ISBN saisi à la main est contrôlé avant la recherche

- **Énoncé :** L'ISBN tapé à la main est nettoyé des tirets et espaces ; s'il est vide l'écran affiche « Enter the ISBN », s'il n'est pas valide « Enter a valid ISBN: 10 or 13 digits », sinon la recherche du livre est lancée.
- **Déclencheur :** Validation de la saisie.
- **Conditions / exceptions :** L'aide affichée est « 10 or 13 digits. Dashes and spaces are ignored. ».
- **Résultat en cas de violation :** Message sous le champ, pas de recherche.
- **Utilisé dans :** UJ-FO-006, EP #8
- **Source :**
  - `Web/Models/IsbnEntry.cs:8-22` — messages et contrôle
  - `Web/Components/Pages/Books/AddBookManual.razor.cs:49-57` — lancement de la recherche
- **Confiance :** Élevée
- **Note technique :** La validité suit `Application/Helpers/IsbnHelper.cs`.
- **Liés :** BR-UI-018, BR-UI-023
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-020 — Un livre trouvé s'ajoute en un geste, avec une bibliothèque obligatoire

- **Énoncé :** Quand les catalogues connaissent l'ISBN, l'écran présente la fiche trouvée (couverture, titre, série, auteurs, parution, source) et le bouton « Add to <bibliothèque> » ajoute le livre ; une note facultative (« Already read it? ») ajoute en plus une date de lecture ; « Edit details before adding » ouvre le formulaire complet.
- **Déclencheur :** Recherche réussie.
- **Conditions / exceptions :** Si l'ajout échoue à la validation, l'écran bascule vers le formulaire complet.
- **Résultat en cas de violation :** Message « Choose a library » ou erreurs du formulaire.
- **Utilisé dans :** UJ-FO-006, EP #9
- **Source :**
  - `Web/Components/Pages/Books/AddBookForm.razor:16-65` — fiche trouvée, bibliothèque, note, boutons
  - `Web/Components/Pages/Books/AddBookForm.razor.cs:100-123` — préremplissage depuis le catalogue
  - `Web/Components/Pages/Books/AddBookForm.razor.cs:187-206` — ajout et message « … was added to … »
- **Confiance :** Élevée
- **Note technique :** Pas d'étape de confirmation en plus : l'ajout est réversible (suppression possible).
- **Liés :** BR-UI-027
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-021 — Livre introuvable : le formulaire est proposé, ISBN prérempli

- **Énoncé :** Quand aucun catalogue ne connaît l'ISBN, ou quand la recherche échoue, le formulaire d'ajout s'ouvre avec l'ISBN prérempli ("Not found online: fill in the details") ; en cas d'échec de la recherche, un avertissement « The catalogues could not be searched: fill in the details » est ajouté.
- **Déclencheur :** Recherche sans résultat ou en erreur.
- **Conditions / exceptions :** Aucune.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-006, EP #9
- **Source :**
  - `Web/Components/Pages/Books/AddBookForm.razor.cs:56-61,125-132` — repli et avertissement
- **Confiance :** Élevée
- **Note technique :** Les catalogues interrogés sont décrits dans le composant Recherche de métadonnées (SRC).
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-022 — Le titre et la série d'un livre sont obligatoires

- **Énoncé :** Un livre ne peut être ajouté ou modifié sans titre ni série.
- **Déclencheur :** Envoi du formulaire.
- **Conditions / exceptions :** Aucune.
- **Résultat en cas de violation :** « Enter a title », « Enter a series ».
- **Utilisé dans :** UJ-FO-006, UJ-FO-007, EP #9, EP #11
- **Source :**
  - `Web/Validators/BookValidator.cs:11-12,22-23` — règles
  - `Application/Books/Update/UpdateBookCommandHandler.cs:20-23` — même exigence côté serveur
  - `Application/Books/Create/CreateBookCommandHandler.cs:19-22` — idem à la création
- **Confiance :** Élevée
- **Note technique :** Doublon écran / serveur sans écart apparent.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-023 — L'ISBN d'un livre physique est obligatoire et doit être valide

- **Énoncé :** Pour un livre papier, l'ISBN est obligatoire et doit être un ISBN valide de 10 ou 13 chiffres (20 caractères au plus) ; pour un livre numérique l'ISBN peut rester vide, mais s'il est renseigné il doit être valide.
- **Déclencheur :** Envoi du formulaire d'ajout ou de modification.
- **Conditions / exceptions :** Le champ n'est marqué « obligatoire » que pour un livre papier.
- **Résultat en cas de violation :** « Enter the ISBN », « Enter a valid ISBN: 10 or 13 digits », « ISBN must not exceed 20 characters. »
- **Utilisé dans :** UJ-FO-006, UJ-FO-007, EP #9, EP #11
- **Source :**
  - `Web/Validators/BookValidator.cs:15-20` — règles
  - `Web/Validators/BookUiDto.cs:42-44` — indicateur « livre physique »
  - `Web/Components/Pages/Books/BookFields.razor:47-48` — champ marqué obligatoire seulement pour un livre physique
  - `Application/Books/Update/UpdateBookCommandHandler.cs:25-33` — validité contrôlée côté serveur si renseigné
  - `Application/Books/Create/CreateBookCommandHandler.cs:25-28` — idem à la création
- **Confiance :** Élevée
- **Note technique :** L'obligation de l'ISBN pour un livre physique est portée par le domaine (`Domain/Books/PhysicalBook.cs:15,40`).
- **Liés :** BR-UI-019
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-024 — Le tome et le nombre de pages doivent être supérieurs à zéro

- **Énoncé :** Le numéro de tome doit être supérieur à 0 (1 par défaut) ; le nombre de pages, s'il est renseigné, doit être supérieur à 0.
- **Déclencheur :** Envoi du formulaire.
- **Conditions / exceptions :** Les champs numériques du formulaire ont aussi un minimum de 1.
- **Résultat en cas de violation :** « Volume number must be greater than 0. », « Number of pages must be greater than 0. »
- **Utilisé dans :** UJ-FO-006, UJ-FO-007, EP #9, EP #11
- **Source :**
  - `Web/Validators/BookValidator.cs:26-27,42-44` — règles
  - `Web/Components/Pages/Books/BookFields.razor:12,26` — minimum de saisie
- **Confiance :** Moyenne. Aucun contrôle équivalent n'a été retrouvé côté serveur.
- **Note technique :** Voir D-UI-003 : l'import d'informations depuis le web enregistre un tome sans passer par ce contrôle, et traite le tome 0 comme « inconnu » (`Web/Components/Pages/Books/FindBookDetails.razor.cs:221-223`).
- **Liés :** D-UI-003, D-UI-004, Q-UI-008
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-025 — La date de parution ne peut pas être dans le futur

- **Énoncé :** La date de parution d'un livre, si elle est renseignée, ne peut pas être postérieure à aujourd'hui.
- **Déclencheur :** Envoi du formulaire.
- **Conditions / exceptions :** Jour courant en temps universel.
- **Résultat en cas de violation :** « Publish date must not be in the future. »
- **Utilisé dans :** UJ-FO-006, UJ-FO-007, EP #9, EP #11
- **Source :** `Web/Validators/BookValidator.cs:46-47`
- **Confiance :** Moyenne. Aucun contrôle équivalent retrouvé côté serveur.
- **Note technique :** Voir D-UI-003.
- **Liés :** D-UI-003
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-026 — Longueurs maximales des champs d'un livre

- **Énoncé :** Le formulaire limite le titre à 200 caractères, la série à 200, l'ISBN à 20, les auteurs à 200, les éditeurs à 200 et l'adresse de la couverture à 500.
- **Déclencheur :** Envoi du formulaire.
- **Conditions / exceptions :** Aucune.
- **Résultat en cas de violation :** « Title must not exceed 200 characters. » et messages analogues.
- **Utilisé dans :** UJ-FO-006, UJ-FO-007, EP #9, EP #11
- **Source :**
  - `Web/Validators/BookValidator.cs:13,17,24,30,37,40` — règles
  - `Domain/Books/BookConstants.cs:5-10` — valeurs
- **Confiance :** Élevée
- **Note technique :** Même limites en base (BR-DB-010). L'import d'informations depuis le web ne passe pas par le formulaire : D-UI-004.
- **Liés :** BR-DB-010, D-UI-004, Q-DOM-008
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-027 — La note à l'ajout est facultative, de 1 à 5

- **Énoncé :** À l'ajout d'un livre, l'utilisateur peut le noter de 1 à 5 étoiles pour y associer aussitôt une date de lecture ; sans note, aucune lecture n'est créée.
- **Déclencheur :** Choix d'une note avant l'ajout.
- **Conditions / exceptions :** Valeur 0 = « pas de note » ; hors de 0 à 5, le serveur refuse.
- **Résultat en cas de violation :** Message d'erreur du formulaire ou du serveur.
- **Utilisé dans :** UJ-FO-006, EP #9
- **Source :**
  - `Web/Validators/BookValidator.cs:32-34` — valeurs 0 à 5 acceptées
  - `Web/Components/Pages/Books/AddBookForm.razor:54-58` — « Rate it to add a reading date (optional). »
  - `Application/Books/Create/CreateBookCommandHandler.cs:31-33,78-81` — note refusée hors de 0 à 5 ; lecture ajoutée si note > 0
- **Confiance :** Élevée
- **Note technique :** Le message d'erreur du formulaire dit « between 1 and 5 stars » alors que 0 est accepté : D-UI-002.
- **Liés :** D-UI-002, Q-DOM-005
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-028 — Un ISBN déjà présent est refusé à l'ajout et à la modification

- **Énoncé :** Ajouter un livre dont l'ISBN existe déjà affiche « This book is already in your libraries » ; modifier un livre pour lui donner l'ISBN d'un autre affiche « Another book already has this ISBN ».
- **Déclencheur :** Enregistrement.
- **Conditions / exceptions :** Contrôle fait par le serveur.
- **Résultat en cas de violation :** Message, l'utilisateur reste sur l'écran.
- **Utilisé dans :** UJ-FO-006, UJ-FO-007, EP #9, EP #11
- **Source :**
  - `Web/Components/Pages/Books/AddBookForm.razor.cs:211` — message à l'ajout
  - `Web/Components/Pages/Books/EditBook.razor.cs:158` — message à la modification
  - `Application/Books/Create/CreateBookCommandHandler.cs:38-42` — refus à la création
  - `Application/Books/Update/UpdateBookCommandHandler.cs:52` — refus à la modification
- **Confiance :** Élevée
- **Note technique :** Étendue exacte (par utilisateur ou globale) à vérifier dans le module Livres ; non garanti en base (D-DOM-002).
- **Liés :** D-DOM-002
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-029 — Une lecture ajoutée depuis la fiche exige une note de 1 à 5

- **Énoncé :** Sur la fiche d'un livre, « Add a reading » enregistre une lecture à la date du jour ; le bouton « Save » reste désactivé tant qu'aucune étoile n'est choisie.
- **Déclencheur :** Clic sur « Add a reading » puis « Save ».
- **Conditions / exceptions :** Pour un livre numérique, le bouton principal est « Lire » ; « Add a reading » reste disponible dans l'historique.
- **Résultat en cas de violation :** Bouton désactivé ; le serveur refuse aussi une note hors de 1 à 5.
- **Utilisé dans :** UJ-FO-007, EP #10
- **Source :**
  - `Web/Components/Pages/Books/BookDetail.razor:200-218` — formulaire « Add a reading · today »
  - `Web/Components/Pages/Books/BookDetail.razor.cs:103-134` — garde (note 0 ou envoi déjà en cours)
  - `Application/Books/AddReadingDate/AddReadingDateCommandHandler.cs:24-27` — note de 1 à 5 exigée
- **Confiance :** Élevée
- **Note technique :** Le même enregistrement existe dans le lecteur (BR-UI-043).
- **Liés :** BR-UI-043
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-030 — Une lecture se supprime depuis la modification du livre, après confirmation

- **Énoncé :** Dans la modification d'un livre, chaque lecture peut être supprimée après une confirmation « Delete this reading? » ; une seule suppression de lecture à la fois est possible.
- **Déclencheur :** Action de suppression d'une lecture.
- **Conditions / exceptions :** La note d'une lecture existante n'est pas modifiable depuis l'écran.
- **Résultat en cas de violation :** Échec : « Unable to delete the reading ».
- **Utilisé dans :** UJ-FO-007, EP #11
- **Source :** `Web/Components/Pages/Books/EditBook.razor.cs:162-197`
- **Confiance :** Élevée
- **Note technique :** La modification d'une lecture n'est appelée que par un script d'import (Q-DOM-005).
- **Liés :** Q-DOM-005
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-031 — La suppression d'un livre demande une confirmation

- **Énoncé :** Avant de supprimer un livre, depuis sa fiche ou depuis la bibliothèque, l'utilisateur confirme un message qui rappelle que le livre est supprimé avec ses lectures et que l'opération est irréversible.
- **Déclencheur :** Action « Delete ».
- **Conditions / exceptions :** Aucune.
- **Résultat en cas de violation :** Sans confirmation, rien n'est supprimé ; échec : « Failed to delete book ».
- **Utilisé dans :** UJ-FO-014, EP #6, EP #10
- **Source :**
  - `Web/Components/Pages/Books/BookDetail.razor.cs:221-243` — confirmation depuis la fiche
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:580-605` — confirmation depuis la bibliothèque
- **Confiance :** Élevée
- **Note technique :** Suppression en cascade des lectures : BR-DB-008. Pas de garde contre un double envoi pendant la suppression.
- **Liés :** BR-DB-008
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-032 — Un livre ne se déplace que vers une autre bibliothèque du même type

- **Énoncé :** Le déplacement propose seulement les autres bibliothèques du même type (physique ou numérique), indique que le fichier est déplacé avec le livre, marque une bibliothèque « Suggested », et désactive « Move » tant qu'aucune destination n'est choisie ; sans destination possible, l'écran annonce « No other library of the same type ». Un déplacement à la fois.
- **Déclencheur :** Action « Move to another library ».
- **Conditions / exceptions :** La boîte de dialogue de choix tient lieu de confirmation.
- **Résultat en cas de violation :** Échec : message de l'erreur ou « Failed to move book ».
- **Utilisé dans :** UJ-FO-014, EP #6, EP #10
- **Source :**
  - `Web/Services/BookMoveWorkflow.cs:35-55` — destinations, message sans destination, boîte de dialogue
  - `Web/Components/Pages/Dialogs/MoveBookDialog.razor:11,26,34,51` — texte « Its file is moved too », bibliothèque actuelle non choisissable, bouton désactivé
  - `Web/Components/Pages/Books/BookDetail.razor.cs:252-271` — un seul déplacement à la fois
- **Confiance :** Élevée
- **Note technique :** Qui calcule les destinations (même type) : requête de destinations du module Livres, non relue ici.
- **Liés :** Q-UI-007
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-033 — Le téléchargement d'un livre est réservé à son propriétaire et aux livres numériques

- **Énoncé :** « Download CBZ » (ou « CBZ » sur la fiche) télécharge le fichier du livre numérique ; l'adresse de téléchargement refuse un utilisateur non connecté, un livre qui n'est pas à lui, un livre papier, et un fichier hors du dossier de stockage.
- **Déclencheur :** Action de téléchargement.
- **Conditions / exceptions :** Livres numériques seulement.
- **Résultat en cas de violation :** Non autorisé, introuvable ou « Only digital books can be downloaded. »
- **Utilisé dans :** UJ-FO-014, EP #6, EP #10
- **Source :**
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:537-541` — ouverture de l'adresse dans un nouvel onglet
  - `Web/Components/Pages/Books/BookDetail.razor:113-117` — lien « CBZ » pour un livre numérique
  - `Web/EndPoints/BooksEndpoints.cs:17,29-52` — contrôles côté serveur
- **Confiance :** Élevée
- **Note technique :** Règle serveur, référencée plutôt que re-décrite : à porter au composant Livres.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-034 — Le lancement de la recherche des ISBN manquants est signalé et non répétable pendant son démarrage

- **Énoncé :** « Find missing ISBNs » lance en arrière-plan la lecture des pages des livres numériques sans ISBN ; l'écran annonce combien de livres sont traités, ou « Every book of this library has an ISBN or was already scanned » s'il n'y en a aucun, et l'action est grisée pendant le démarrage.
- **Déclencheur :** Action du menu « More actions » d'une bibliothèque numérique.
- **Conditions / exceptions :** Les livres déjà analysés sont ignorés.
- **Résultat en cas de violation :** « Unable to start the search of the missing ISBNs ».
- **Utilisé dans :** UJ-FO-010, EP #6
- **Source :**
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor:145-146` — action désactivée pendant le démarrage
  - `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:500-535` — messages
- **Confiance :** Élevée
- **Note technique :** Sélection des livres et traitement : module Import de fichiers / Livres (scan d'ISBN).
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-035 — L'issue de la recherche d'ISBN dans les pages oriente l'utilisateur

- **Énoncé :** Après « Scan » sur la fiche d'un livre numérique : un ISBN unique trouvé est enregistré et mène à l'import d'informations (« ISBN found in the pages ») ; plusieurs ISBN proposent un choix (« Several ISBNs found in the pages: pick the book's own ») ; aucun ISBN donne « No ISBN found in the pages » ; une lecture impossible donne « The pages could not be read ».
- **Déclencheur :** Action de scan de la fiche.
- **Conditions / exceptions :** Une analyse à la fois (bouton grisé).
- **Résultat en cas de violation :** Voir issues.
- **Utilisé dans :** UJ-FO-010, EP #10
- **Source :** `Web/Components/Pages/Books/BookDetail.razor.cs:139-194`
- **Confiance :** Élevée
- **Note technique :** Lecture des pages côté serveur (OCR) ; voir module Import de fichiers.
- **Liés :** BR-UI-047
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-036 — Les catalogues en ligne sont interrogés en parallèle et indépendamment

- **Énoncé :** « Import info from the web » interroge en même temps les catalogues activés (BnF, Bédéthèque, Bubble, OpenLibrary, Google Books) pour l'ISBN du livre ; chacun s'affiche dès sa réponse, et l'échec de l'un n'empêche ni les autres ni l'enregistrement avec les valeurs actuelles (« Could not fetch data from …. You can still save with current values. »).
- **Déclencheur :** Ouverture de la page d'import d'informations.
- **Conditions / exceptions :** Chaque source peut être désactivée dans « Settings → Features ». Sans ISBN, aucune recherche.
- **Résultat en cas de violation :** Avertissement par source.
- **Utilisé dans :** UJ-FO-008, EP #12
- **Source :**
  - `Web/Components/Pages/Books/FindBookDetails.razor.cs:116-136` — recherches simultanées
  - `Web/Components/Pages/Books/FindBookDetails.razor.cs:190-201` — sources actives
  - `Web/Components/Pages/Books/FindBookDetails.razor.cs:237-248` — message d'échec par source
- **Confiance :** Élevée
- **Note technique :** Intégrations INT-003 à INT-008.
- **Liés :** BR-UI-065
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-037 — L'utilisateur choisit la source de chaque champ ; le reste est conservé

- **Énoncé :** Pour chaque champ (titre, série, tome, auteurs, éditeur, date de parution, nombre de pages, couverture) l'utilisateur choisit la valeur actuelle ou celle d'une source ; un champ non choisi garde sa valeur actuelle, et le bouton d'application reste désactivé tant qu'aucun changement n'est choisi.
- **Déclencheur :** Choix d'une source ; clic sur le bouton d'application.
- **Conditions / exceptions :** Une valeur de source illisible (tome, date, pages) est ignorée et la valeur actuelle est gardée.
- **Résultat en cas de violation :** Échec : « Failed to update book: … ».
- **Utilisé dans :** UJ-FO-008, EP #12
- **Source :**
  - `Web/Components/Pages/Books/FindBookDetails.razor.cs:225-235` — choix par champ et décompte des changements
  - `Web/Components/Pages/Books/FindBookDetails.razor.cs:341-417` — valeurs appliquées et enregistrement
  - `Web/Components/Pages/Books/FindBookDetails.razor:136-143` — boutons « Cancel » et d'application
- **Confiance :** Élevée
- **Note technique :** Aucune confirmation avant d'écraser les valeurs : l'écran récapitule les changements dans le libellé du bouton. Voir D-UI-004.
- **Liés :** D-UI-004
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-038 — La couverture externe est copiée chez l'hébergeur d'images avant l'enregistrement

- **Énoncé :** Si la couverture choisie est une adresse web qui ne pointe pas déjà vers l'hébergeur d'images, elle y est copiée avant l'enregistrement ; si la copie échoue, l'adresse d'origine est enregistrée et un avertissement est affiché (« Cover could not be uploaded to Cloudinary. The original URL will be saved. »).
- **Déclencheur :** Application des choix.
- **Conditions / exceptions :** Adresses http ou https absolues seulement.
- **Résultat en cas de violation :** Avertissement, enregistrement poursuivi.
- **Utilisé dans :** UJ-FO-008, EP #12
- **Source :** `Web/Components/Pages/Books/FindBookDetails.razor.cs:357-371`
- **Confiance :** Élevée
- **Note technique :** Intégration INT-002 ; voir aussi le composant Stockage de fichiers et services image.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-039 — Un ISBN lu dans les pages est enregistré sur le livre

- **Énoncé :** Quand la page d'import d'informations est ouverte avec un ISBN lu dans les pages du livre et que cet ISBN est valide, il remplace l'ISBN actuel pour la recherche et il est enregistré sur le livre avec l'application des choix ; il compte comme un changement.
- **Déclencheur :** Ouverture depuis le lecteur, la fiche ou après un scan.
- **Conditions / exceptions :** Un ISBN invalide dans l'adresse est ignoré.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-008, UJ-FO-010, EP #12
- **Source :** `Web/Components/Pages/Books/FindBookDetails.razor.cs:106-114,232,396`
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-040 — Une recherche en ligne n'est pas relancée pour le même livre et le même ISBN

- **Énoncé :** La page d'import d'informations ne relance pas les recherches si le livre et l'ISBN n'ont pas changé, car chaque recherche consomme un quota de services payants (SerpApi, Google).
- **Déclencheur :** Réaffichage de la page.
- **Conditions / exceptions :** Un autre livre ou un autre ISBN relance les recherches.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-008, EP #12
- **Source :** `Web/Components/Pages/Books/FindBookDetails.razor.cs:58-71`
- **Confiance :** Moyenne. Motif déduit du commentaire du code.
- **Note technique :** Un résultat arrivé pour un autre livre est ignoré (`FindBookDetails.razor.cs:179-183`).
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-041 — Le lecteur reprend à la dernière page lue

- **Énoncé :** Ouvrir un livre numérique dans le lecteur ramène à la dernière page enregistrée, avec les réglages d'affichage de ce livre ; un livre introuvable ou illisible affiche « This book cannot be read » ou un message d'erreur.
- **Déclencheur :** Ouverture du lecteur.
- **Conditions / exceptions :** En mode recherche d'ISBN, le lecteur commence à la première page.
- **Résultat en cas de violation :** Message d'erreur.
- **Utilisé dans :** UJ-FO-009, EP #13
- **Source :**
  - `Web/Components/Pages/Books/BookReader.razor.cs:137-173` — chargement et position de départ
  - `Web/Components/Pages/Books/BookReader.razor:14-23` — message « This book cannot be read »
  - `Web/EndPoints/BooksEndpoints.cs:18,65-80` — pages servies à leur propriétaire seulement
- **Confiance :** Élevée
- **Note technique :** Le libellé du bouton de la fiche (Lire / Reprendre) vient de la fiche (`BookDetail.razor:57-73`).
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-042 — La progression de lecture est enregistrée après une courte pause et à la sortie

- **Énoncé :** La page atteinte est enregistrée quand l'utilisateur reste une seconde sur une page, à la fermeture du lecteur et à toute sortie de la page, sans avertir en cas d'échec.
- **Déclencheur :** Changement de page.
- **Conditions / exceptions :** Rien n'est enregistré si la page n'a pas changé.
- **Résultat en cas de violation :** Échec silencieux (journalisé).
- **Utilisé dans :** UJ-FO-009, EP #13
- **Source :** `Web/Components/Pages/Books/BookReader.razor.cs:18,582-631,664-687`
- **Confiance :** Élevée
- **Note technique :** Un échec n'est pas montré à l'utilisateur : il peut perdre sa position sans le savoir.
- **Liés :** D-UI-005
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-043 — En fin de livre, l'utilisateur peut le marquer comme lu avec une note

- **Énoncé :** En atteignant la fin d'un livre, le lecteur propose une note de 1 à 5 (3 par défaut) ; « Mark as read » ajoute une lecture à la date du jour, remet la progression à la première page et retourne à la fiche ; « Keep reading » ferme la proposition.
- **Déclencheur :** Atteinte de la dernière page.
- **Conditions / exceptions :** Non proposé en mode recherche d'ISBN. Le bouton est désactivé pendant l'enregistrement.
- **Résultat en cas de violation :** Échec : « Unexpected error while adding reading date ».
- **Utilisé dans :** UJ-FO-009, EP #13
- **Source :**
  - `Web/Components/Pages/Books/BookReader.razor:223-239` — fenêtre de fin de livre
  - `Web/Components/Pages/Books/BookReader.razor.cs:62,299-304,633-662` — note par défaut, mode ISBN exclu, enregistrement
- **Confiance :** Élevée
- **Note technique :** Les deux appels (ajout de lecture, remise à zéro) ne sont pas dans une même transaction : D-UI-005.
- **Liés :** BR-UI-029, D-UI-005
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-044 — Les réglages d'affichage du lecteur sont mémorisés par livre

- **Énoncé :** La mise en page (pleine largeur, page seule, deux pages), le défilement (continu ou page par page) et le sens de lecture choisis sont mémorisés pour ce livre et retrouvés à sa prochaine ouverture ; si la mémorisation échoue, un avertissement est affiché.
- **Déclencheur :** Choix dans le panneau « Display ».
- **Conditions / exceptions :** Aide au premier usage affichée une fois (gestes, rotation du téléphone en portrait).
- **Résultat en cas de violation :** « Unable to save the display settings ».
- **Utilisé dans :** UJ-FO-009, EP #13
- **Source :**
  - `Web/Components/Pages/Books/BookReader.razor.cs:373-400` — enregistrement
  - `Web/Components/Pages/Books/BookReader.razor:182` — mention « Saved for this book. »
- **Confiance :** Élevée
- **Note technique :** Les rappels d'aide et de rotation sont gardés côté navigateur (`bookReader.js`), non relus.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-045 — Le sens de lecture inverse les commandes de page

- **Énoncé :** En sens droite-à-gauche (mangas), la flèche gauche et la zone gauche de l'écran vont à la page suivante, la flèche droite à la précédente, et la barre de progression se remplit depuis la droite.
- **Déclencheur :** Navigation dans le lecteur.
- **Conditions / exceptions :** Sens choisi par livre.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-009, EP #13
- **Source :**
  - `Web/Components/Pages/Books/BookReader.razor.cs:281-284` — inversion
  - `Web/Components/Pages/Books/BookReader.razor:117-128,192-200` — libellés et zones inversés
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-046 — Le mode recherche d'ISBN ne sauvegarde pas la progression

- **Énoncé :** Quand le lecteur est ouvert pour chercher l'ISBN, il ne retient ni la page ni la fin de lecture : il affiche « ISBN search · progress not saved » et ne propose pas de marquer le livre comme lu.
- **Déclencheur :** Ouverture avec le mode « isbn ».
- **Conditions / exceptions :** Mode demandé depuis la bibliothèque, la fiche ou le bouton « Look in the reader ».
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-010, EP #13
- **Source :**
  - `Web/Components/Pages/Books/BookReader.razor.cs:15,89,299-304,582-587,610`
  - `Web/Components/Pages/Books/BookReader.razor:59-62` — pastille d'information
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** BR-UI-047
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-047 — La lecture du texte des pages propose les ISBN trouvés

- **Énoncé :** Dans le lecteur, « Find the ISBN on this page » lit le texte de la page affichée, et le mode recherche d'ISBN lit une à une les premières et dernières pages jusqu'à trouver un ISBN ; les ISBN trouvés sont proposés à l'utilisateur, qui en choisit un pour aller chercher les informations du livre (la progression est alors enregistrée avant de quitter).
- **Déclencheur :** Action du lecteur.
- **Conditions / exceptions :** Délai de 2 minutes par page (le premier usage télécharge le moteur de lecture). Aucun ISBN : « No ISBN found on this page » ou « No ISBN found in the first and last pages ».
- **Résultat en cas de violation :** « Unable to read the text of this page ».
- **Utilisé dans :** UJ-FO-010, EP #13
- **Source :** `Web/Components/Pages/Books/BookReader.razor.cs:20-21,460-578`
- **Confiance :** Élevée
- **Note technique :** Lecture dans le navigateur (`isbnOcr.js`), distincte de l'analyse faite sur le serveur (BR-UI-035).
- **Liés :** BR-UI-035
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-048 — L'import de fichiers ne propose que les bibliothèques numériques

- **Énoncé :** L'écran d'import ne liste que les bibliothèques numériques ; la bibliothèque indiquée dans l'adresse est présélectionnée, sinon la première ; sans bibliothèque numérique, l'écran invite à en créer une (« No digital library yet. Create one to import comic files. »).
- **Déclencheur :** Ouverture de l'écran d'import.
- **Conditions / exceptions :** 200 bibliothèques au plus chargées.
- **Résultat en cas de violation :** « Unable to load the libraries. »
- **Utilisé dans :** UJ-FO-011, EP #14
- **Source :**
  - `Web/Components/Pages/Import.razor.cs:35-78` — filtre et présélection
  - `Web/Components/Pages/Import.razor:22-28` — état vide
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** Q-UI-009
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-049 — Formats et nombre de fichiers acceptés à l'import

- **Énoncé :** On peut choisir jusqu'à 10 fichiers à la fois, aux formats CBZ, CBR, ZIP, RAR ou PDF ; le choix est impossible tant qu'aucune bibliothèque n'est sélectionnée ou pendant un envoi. Chaque fichier devient un import ; les échecs sont listés par fichier (« <fichier> : <raison> »).
- **Déclencheur :** Choix de fichiers.
- **Conditions / exceptions :** Le contrôle des extensions est refait côté serveur.
- **Résultat en cas de violation :** Message par fichier ; « n file(s) uploaded successfully » si aucun échec.
- **Utilisé dans :** UJ-FO-011, EP #14
- **Source :**
  - `Web/Components/Pages/Import.razor:34-39,59` — formats, 10 fichiers, désactivation
  - `Web/Components/Pages/Import.razor.cs:124-170` — envoi fichier par fichier
  - `Web/Services/ImportService.cs:80-81` — contrôle des extensions côté serveur
  - `Web/appsettings.json:71` — extensions prises en charge
- **Confiance :** Élevée
- **Note technique :** Le message de succès s'affiche même si la bibliothèque a changé en cours d'envoi (les derniers fichiers sont alors abandonnés sans message).
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-050 — Taille maximale d'un fichier importé

- **Énoncé :** Un fichier importé ne peut pas dépasser la taille maximale configurée (1 024 Mo dans la configuration du dépôt).
- **Déclencheur :** Envoi d'un fichier.
- **Conditions / exceptions :** Valeur de production inconnue.
- **Résultat en cas de violation :** Fichier refusé, motif affiché sous l'écran.
- **Utilisé dans :** UJ-FO-011, EP #14
- **Source :**
  - `Web/Services/ImportService.cs:103-107` — plafond appliqué à la lecture du fichier
  - `Web/appsettings.json:72` — `MaxFileSizeMb` = 1024
- **Confiance :** Moyenne. Valeur de production non connue.
- **Note technique :** L'écran n'annonce pas ce plafond (seuls formats et nombre de fichiers sont indiqués).
- **Liés :** Q-UI-010
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-051 — Le suivi des imports se rafraîchit tant qu'un import n'est pas terminé

- **Énoncé :** La liste des imports de la bibliothèque (du plus récent au plus ancien) se rafraîchit toutes les 3 secondes tant qu'au moins un import n'est ni terminé ni échoué, puis s'arrête.
- **Déclencheur :** Ouverture de l'écran, envoi de fichiers.
- **Conditions / exceptions :** Terminé = Completed ou Failed ; un import terminé propose un lien vers le livre créé.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-011, EP #14
- **Source :**
  - `Web/Components/Pages/Import.razor.cs:172-226` — interrogation toutes les 3 s
  - `Web/Models/ImportJobViewModel.cs:42,73` — lien vers le livre, import terminé
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-052 — Les imports terminés ou échoués peuvent être effacés de la liste

- **Énoncé :** Un import terminé ou échoué peut être effacé de la liste, un par un ou tous ensemble (« Clear finished imports »), sans confirmation ; les livres déjà créés ne sont pas concernés.
- **Déclencheur :** Action d'effacement.
- **Conditions / exceptions :** Les imports en cours ne peuvent pas être effacés.
- **Résultat en cas de violation :** « Unable to delete n job(s) ».
- **Utilisé dans :** UJ-FO-011, EP #14
- **Source :**
  - `Web/Components/Pages/Import.razor:75-81` — action groupée, désactivée sans import terminé
  - `Web/Components/Pages/Import.razor.cs:235-288` — suppression
  - `Web/Components/SharedComponents/ImportJobCard.razor:44-48` — suppression d'un import terminé
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** Q-UI-004
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-053 — Un import bloqué peut être déclaré en échec

- **Énoncé :** Pour un import ni terminé ni en attente, l'écran propose de le déclarer en échec (« Import marked as failed. »), ce qui permet ensuite de l'effacer et, selon la règle d'unicité des imports, de réimporter le même fichier.
- **Déclencheur :** Action « force fail » sur la carte d'un import.
- **Conditions / exceptions :** Proposé pour tout import dont le statut n'est ni terminé ni « Waiting » ; aucune confirmation.
- **Résultat en cas de violation :** « Unable to force the job to fail ».
- **Utilisé dans :** UJ-FO-011, EP #14
- **Source :**
  - `Web/Components/SharedComponents/ImportJobCard.razor.cs:18` — condition de l'action
  - `Web/Components/Pages/Import.razor.cs:290-308` — appel et message
- **Confiance :** Moyenne. Effet exact sur le traitement en cours à vérifier dans le composant Import de fichiers.
- **Note technique :** —
- **Liés :** BR-DB-001, Q-UI-004
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-054 — Les décisions d'import par flux se filtrent par statut et se recherchent

- **Énoncé :** L'écran « Feeds » liste les décisions par pages de 20 (« Show more »), avec des boutons de filtre par statut affichant leur compteur et une recherche d'articles qui s'applique 400 ms après la frappe.
- **Déclencheur :** Ouverture, filtre, recherche.
- **Conditions / exceptions :** Une réponse arrivée après un changement de filtre ou de recherche est ignorée.
- **Résultat en cas de violation :** « Unable to load the import decisions. »
- **Utilisé dans :** UJ-FO-012, EP #15
- **Source :**
  - `Web/Components/Pages/FeedImportsPage.razor.cs:13,59-97,133-148` — pages de 20, filtre, recherche
  - `Web/Components/Pages/FeedImportsPage.razor:217-228,244-252` — recherche à 400 ms et filtres
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-055 — La synchronisation manuelle n'existe que si l'import par flux est activé

- **Énoncé :** Le bouton « Sync now » n'est affiché que si la fonctionnalité d'import par flux est activée ; sinon l'écran indique « Miniflux sync is turned off (Settings, Features) ». La demande déclenche une synchronisation en arrière-plan (« Sync started. Refresh the list in a few moments. ») et le bouton est grisé pendant la demande.
- **Déclencheur :** Clic sur « Sync now ».
- **Conditions / exceptions :** La fonctionnalité est désactivée par défaut dans la configuration du dépôt.
- **Résultat en cas de violation :** Demande refusée côté serveur (« désactivé ») si la fonctionnalité est coupée entre-temps.
- **Utilisé dans :** UJ-FO-012, EP #15
- **Source :**
  - `Web/Components/Pages/FeedImportsPage.razor:230-237,254-257` — bouton et note
  - `Web/Services/FeedImportService.cs:29,68-77` — refus si la fonction est coupée
  - `Web/Components/Pages/FeedImportsPage.razor.cs:170-192` — demande
- **Confiance :** Élevée
- **Note technique :** Intégration Miniflux (INT-009) ; la valeur de production de l'activation est inconnue (cf. glossaire « Fonctionnalité activable »).
- **Liés :** BR-UI-065
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-056 — Un doublon probable demande une décision de l'utilisateur

- **Énoncé :** Quand un livre de même série et même tome existe déjà, l'écran signale un « doublon probable », donne un lien vers le livre existant et demande de choisir « Not a duplicate » ou « Confirm duplicate ».
- **Déclencheur :** Décision à arbitrer pour cause de doublon.
- **Conditions / exceptions :** Visible sur la carte et dans le détail.
- **Résultat en cas de violation :** « Unable to update the decision. »
- **Utilisé dans :** UJ-FO-012, EP #15
- **Source :** `Web/Components/Pages/FeedImportsPage.razor:38-42,194-210,326-329`
- **Confiance :** Élevée
- **Note technique :** Les conséquences de chaque choix sont dans le composant Import par flux (FDI).
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-057 — Des liens ambigus demandent un choix de l'utilisateur

- **Énoncé :** Quand plusieurs liens de téléchargement sont possibles, l'utilisateur garde le bon fichier (« Keep this link ») ou indique que tous les liens désignent le même fichier (« All these links are the same file », proposé s'il y a plusieurs fichiers).
- **Déclencheur :** Décision à arbitrer pour cause de liens ambigus.
- **Conditions / exceptions :** Les liens sont ouverts dans un nouvel onglet sans transmission de la page d'origine.
- **Résultat en cas de violation :** « Unable to update the decision. »
- **Utilisé dans :** UJ-FO-012, EP #15
- **Source :** `Web/Components/Pages/FeedImportsPage.razor:43-58,155-162,330-337`
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-058 — Les actions manuelles sur une décision dépendent de son état

- **Énoncé :** Selon l'état d'une décision, l'écran propose : forcer le téléchargement, réessayer, corriger la série et le volume, ou ignorer.
- **Déclencheur :** Affichage d'une décision.
- **Conditions / exceptions :** Les conditions exactes de chaque action sont calculées par le modèle de vue.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-012, EP #15
- **Source :** `Web/Components/Pages/FeedImportsPage.razor:60-96,310-319`
- **Confiance :** Moyenne. Conditions non relues dans le modèle de vue.
- **Note technique :** Modèle de vue des décisions d'import par flux (`Web/Models/FeedImportDecisionViewModel.cs`) ; à détailler avec le composant FDI.
- **Liés :** Q-UI-011
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-059 — La correction de série et de volume exige une série

- **Énoncé :** Corriger l'analyse du titre d'une décision exige une série (200 caractères au plus), accepte un titre (300 au plus) et un volume non négatif ; « Save » reste grisé tant que la série est vide. Le contrôle de doublon est relancé après la correction.
- **Déclencheur :** Action « Fix series / volume ».
- **Conditions / exceptions :** Une seule correction en cours à la fois.
- **Résultat en cas de violation :** Bouton grisé ; ou « Unable to update the decision. »
- **Utilisé dans :** UJ-FO-012, EP #15
- **Source :** `Web/Components/Pages/FeedImportsPage.razor:97-118` ; `Web/Components/Pages/FeedImportsPage.razor.cs:204-227`
- **Confiance :** Élevée
- **Note technique :** Les limites 200 et 300 correspondent à `Domain/FeedImports/FeedImportConstants.cs:7-8`.
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-060 — Une seule action à la fois sur les décisions d'import

- **Énoncé :** Tant qu'une action est en cours sur une décision, toute autre action sur les décisions est ignorée et les boutons sont grisés.
- **Déclencheur :** Clic sur une action.
- **Conditions / exceptions :** Après réussite : « Decision updated. », rechargement et mise à jour de la pastille de navigation.
- **Résultat en cas de violation :** Action ignorée sans message.
- **Utilisé dans :** UJ-FO-012, EP #15
- **Source :** `Web/Components/Pages/FeedImportsPage.razor.cs:229-257`
- **Confiance :** Élevée
- **Note technique :** Verrou limité à la page ouverte ; pas de protection côté serveur observée ici.
- **Liés :** BR-DB-004
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-061 — La suppression de décisions demande une confirmation et épargne celles en cours

- **Énoncé :** Supprimer une ou plusieurs décisions demande une confirmation qui précise que l'historique est supprimé et que les livres déjà importés sont conservés ; les décisions en cours de téléchargement ne peuvent pas être supprimées et sont conservées (« being downloaded will be kept »).
- **Déclencheur :** Action « Delete » ou « Select several ».
- **Conditions / exceptions :** Le compte rendu distingue les décisions supprimées et non supprimées.
- **Résultat en cas de violation :** « Unable to delete the decisions. »
- **Utilisé dans :** UJ-FO-012, EP #15
- **Source :** `Web/Components/Pages/FeedImportsPage.razor.cs:259-329` ; `Web/Components/Pages/FeedImportsPage.razor:289`
- **Confiance :** Élevée
- **Note technique :** Suppression en cascade de l'historique : BR-DB-009.
- **Liés :** BR-DB-009
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-062 — La purge des décisions téléchargées demande une confirmation

- **Énoncé :** « Delete downloaded items » supprime, après confirmation, toutes les décisions téléchargées avec succès et leur historique ; les décisions dont l'import est en cours ou échoué sont conservées.
- **Déclencheur :** Menu de la page des flux.
- **Conditions / exceptions :** Aucune.
- **Résultat en cas de violation :** « Unable to delete the downloaded decisions. »
- **Utilisé dans :** UJ-FO-012, EP #15
- **Source :** `Web/Components/Pages/FeedImportsPage.razor.cs:331-375`
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** BR-DB-009
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-063 — Les statistiques portent sur toutes les bibliothèques ou sur une seule

- **Énoncé :** L'écran des statistiques montre, pour toutes les bibliothèques par défaut ou pour celle choisie, le nombre de livres, la part lue, des indicateurs et un graphique des lectures par mois ; sans livre, il affiche « No books yet. Add books to a library to see your statistics. ».
- **Déclencheur :** Ouverture ; changement de bibliothèque.
- **Conditions / exceptions :** Un changement de bibliothèque annule le chargement précédent. 200 bibliothèques au plus listées.
- **Résultat en cas de violation :** « Failed to load statistics ».
- **Utilisé dans :** UJ-FO-013, EP #16
- **Source :**
  - `Web/Components/Pages/Statistics.razor.cs:50-90` — chargement et annulation
  - `Web/Components/Pages/Statistics.razor:22-33,47-50` — synthèse, vide, graphique
- **Confiance :** Élevée
- **Note technique :** Définition des indicateurs : composant Statistiques (STA).
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-064 — La page des paramètres montre le compte, la santé du système et la version

- **Énoncé :** Les paramètres affichent le nom, le courriel et la photo du compte (photo acceptée seulement si son adresse est en https), un résumé de la santé du système dépliable, la version de l'application et « Sign out ».
- **Déclencheur :** Ouverture des paramètres.
- **Conditions / exceptions :** Visible de tout utilisateur connecté.
- **Résultat en cas de violation :** « Failed to load system health ».
- **Utilisé dans :** UJ-FO-001, UJ-FO-015, EP #17
- **Source :**
  - `Web/Components/Pages/UserSettings.razor.cs:34-41` — photo en https seulement
  - `Web/Components/Pages/UserSettings.razor:29-72` — santé, version, déconnexion
- **Confiance :** Moyenne. L'état de santé est visible de tout utilisateur connecté, ce qui peut être voulu ou non (Q-UI-003).
- **Note technique :** —
- **Liés :** Q-UI-003
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-065 — Un administrateur active ou désactive les fonctionnalités sans redéploiement

- **Énoncé :** Un administrateur active ou désactive une fonctionnalité (par exemple import par flux, catalogues en ligne) ; le changement s'applique tout de suite et survit à un redémarrage ; remettre la valeur de la configuration supprime la surcharge, et activer une fonctionnalité dont la configuration est incomplète est refusé.
- **Déclencheur :** Interrupteur de la carte « Features ».
- **Conditions / exceptions :** Rôle `Admin`. Un seul changement à la fois ; en cas d'échec, l'interrupteur revient à son état réel.
- **Résultat en cas de violation :** Message d'erreur de la règle serveur ; « Unable to change the feature. »
- **Utilisé dans :** UJ-FO-015, EP #17
- **Source :**
  - `Web/Components/Pages/Admin/FeatureTogglesCard.razor:1-5,22` — texte d'aide et interrupteur
  - `Web/Components/Pages/Admin/FeatureTogglesCard.razor.cs:21-49` — enregistrement et relecture
  - `Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:25-28,30-48` — refus si configuration manquante, retrait de la surcharge
- **Confiance :** Élevée
- **Note technique :** Pour l'import par flux, la tâche récurrente est ajoutée ou retirée immédiatement (`Web/Services/FeatureToggleService.cs:34-37`). Pas de contrôle de rôle côté serveur : D-UI-001.
- **Liés :** BR-DB-005, D-UI-001
- **Statut :** Brouillon
- **Commentaires du produit :**

### BR-UI-066 — Un administrateur règle les niveaux de journalisation jusqu'au redémarrage

- **Énoncé :** Un administrateur règle le niveau de détail des journaux par composant (Debug, Info, Warning, Error, None) ou le laisse hérité du parent ; le changement est immédiat et dure jusqu'au prochain redémarrage ; « Reset » remet tous les niveaux à leur valeur configurée.
- **Déclencheur :** Page « Log levels ».
- **Conditions / exceptions :** Rôle `Admin`.
- **Résultat en cas de violation :** n/a
- **Utilisé dans :** UJ-FO-015, EP #18
- **Source :**
  - `Web/Components/Pages/Admin/LogLevels.razor:2,14-17,25-27`
  - `Web/Components/Pages/Admin/LogLevels.razor.cs:34-47`
- **Confiance :** Élevée
- **Note technique :** —
- **Liés :** —
- **Statut :** Brouillon
- **Commentaires du produit :**
