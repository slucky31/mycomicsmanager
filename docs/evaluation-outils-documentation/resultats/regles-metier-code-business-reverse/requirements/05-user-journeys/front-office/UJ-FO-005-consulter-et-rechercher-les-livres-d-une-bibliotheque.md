# UJ-FO-005 — Consulter, rechercher et trier les livres d'une bibliothèque

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté (propriétaire de la bibliothèque).
- **Objectif :** Retrouver un livre, choisir l'ordre et la présentation de la liste.
- **Entrée :** Carte d'une bibliothèque, route `/libraries/{id}` (`Web/Components/Pages/Libraries/LibraryDetailPage.razor:3`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Livres d'une bibliothèque | Ouvre la bibliothèque | Chargement de la bibliothèque puis de la première page de 24 livres, dans le tri par défaut de la bibliothèque ; recherche et mode d'affichage de la dernière visite de la session retrouvés. Adresse qui n'est pas un identifiant valide : retour immédiat à la liste sans message ; bibliothèque introuvable (ou toute erreur de lecture) : message temporaire « Library not found » puis retour à la liste. Bibliothèque vide : « No books in this library yet » avec « Import » (numérique) ou « Add your first book » (physique) | BR-UI-015 | Lecture bibliothèque (LIB), livres paginés : `Application/Books/List/GetPagedBooksQueryHandler.cs` | `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:183-224`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor:60-76` |
| 2 | Idem | Fait défiler | Pages suivantes chargées à la demande | BR-UI-015 | idem | `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:264-312` |
| 3 | Idem | Saisit dans « Search by title or series » | Résultats 300 ms après la dernière frappe ; recherche sans tenir compte de la casse, dans le titre ou la série (texte contenu) ; « No results for “…” » avec « Clear search » si vide ; « N books match “…” » (ou « 1 book matches ») si des résultats existent | BR-UI-014 | idem : `Persistence/Queries/BookReadService.cs:26-32` | `Web/Components/Pages/Libraries/LibraryDetailPage.razor:28,49-52,77-84`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:316-331`, `Web/Models/LibraryDisplay.cs:25-26` |
| 4 | Idem | Ouvre « Sort and view » et choisit un tri | Trois tris : « Newest first », « Oldest first », « Series & volume » (série, puis tome, puis ordre d'ajout). Le tri choisi est enregistré comme tri par défaut de la bibliothèque, puis la liste est rechargée ; choisir le tri déjà actif ne fait rien | BR-UI-012 | Mise à jour de la bibliothèque (LIB) | `Web/Components/Pages/Libraries/LibraryDetailPage.razor:106-114`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:360-410`, `Persistence/Queries/BookReadService.cs:34-42` |
| 5 | Idem | Choisit « Covers » ou « List » | Présentation changée sans rechargement ; mémorisée pour la bibliothèque à la sortie de la page, pour la durée de la session (service à durée de vie « par connexion »), contrairement au tri qui est enregistré sur la bibliothèque | BR-UI-013 | — | `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:178,195-201,412-421`, `Web/Program.cs:259` |
| 6 | Idem | Appui long / clic droit / « ⋮ » sur un livre | Menu d'actions du livre : Edit, Import info from the web, Move to another library, Delete ; pour un livre numérique en plus Read, Find ISBN et Download CBZ (UJ-FO-014, UJ-FO-007, UJ-FO-008) | BR-UI-016 | — | `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:433-457`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor:149-182` |
| 7 | Idem | Clique sur « New book » (physique) ou « More actions » (numérique) | Menu d'ajout « Add a book » (Scan the barcode, Type the ISBN, Enter details manually ; UJ-FO-006) ou menu « More actions » (Import, Find missing ISBNs ; UJ-FO-010, UJ-FO-011) | BR-UI-016 | — | `Web/Components/Pages/Libraries/LibraryDetailPage.razor:38-46,123-147` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Chargement des livres | Échec signalé, livres déjà affichés conservés (si le premier chargement échoue, l'écran affiche ensuite « No books in this library yet », voir D-UI-011 proposée) | « Failed to load books » | n/a | `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:257-261,293-297`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor:60-76` |
| Tri | Échec signalé ; le tri affiché ne change pas | « Failed to save sort order » | n/a | `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:400-404` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Consulter les livres d'une bibliothèque | Propriétaire (le serveur filtre par utilisateur) | `Web/Services/BooksService.cs:164-175`, `Application/Books/List/GetPagedBooksQueryHandler.cs:23-27`, `Persistence/Queries/BookReadService.cs:22-24` |

## Liés

BR-UI-012 à BR-UI-016, Q-UI-006, UJ-FO-006, UJ-FO-014, D-UI-011, D-UI-012.

Vérificateur 2026-10-17 : 7 étapes, 2 validations et 1 permission relues. Corrigé : (a) étape 1 : le parcours décrivait « Library not found » pour une adresse invalide ; une adresse qui n'est pas un identifiant renvoie à la liste sans aucun message (`Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:189-193`), le message n'apparaît que pour une lecture en échec (206) ; (b) permission : la citation `Web/Services/BooksService.cs:47-58` désignait la création d'un livre, pas la liste ; remplacée par la lecture paginée et les deux contrôles serveur (bibliothèque appartenant à l'utilisateur, livres filtrés par propriétaire) ; (c) étapes 3, 4, 6 et 7 : recherche insensible à la casse sur le titre et la série, libellés exacts des tris et des menus ajoutés ; (d) étape 5 : la mémorisation du mode d'affichage n'est écrite qu'à la sortie de la page et vit le temps de la session (Q-UI-006 : option par défaut conservée). Accessibilité contrôlée : champ de recherche avec `aria-label`, boutons d'icônes tous étiquetés (« Back to libraries », « Search », « Sort and view », « More actions », « New book »), résultats annoncés par `role="status"`. Cas limite non documenté : une recherche faite uniquement d'espaces est ignorée par le serveur mais l'écran affiche tout de même la ligne de résultats « N books match “ ” » (`Web/Components/Pages/Libraries/LibraryDetailPage.razor:49`, `Persistence/Queries/BookReadService.cs:26`), voir D-UI-012 (proposée).

## Commentaires du produit
