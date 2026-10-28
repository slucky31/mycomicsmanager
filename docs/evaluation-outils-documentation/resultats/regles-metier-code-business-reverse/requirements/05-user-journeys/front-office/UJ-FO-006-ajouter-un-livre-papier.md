# UJ-FO-006 — Ajouter un livre papier

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté.
- **Objectif :** Ajouter à une bibliothèque physique un livre en scannant son code-barres, en tapant son ISBN, ou en saisissant ses informations à la main.
- **Entrée :** « New book » d'une bibliothèque physique (`Web/Components/Pages/Libraries/LibraryDetailPage.razor:44-45,123-140`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Livres d'une bibliothèque | Clique sur « New book » | Menu : « Scan the barcode », « Type the ISBN », « Enter details manually » | BR-UI-016 | — | `Web/Components/Pages/Libraries/LibraryDetailPage.razor:123-140` |
| 2a | Scan | Présente le code-barres | Le script de la caméra extrait un ISBN (13 chiffres ou 10 caractères) et le fait contrôler (clé) ; un code invalide est ignoré sans message et le scan continue ; un ISBN valide mène à la recherche (étape 3). Le message « Invalid ISBN scanned: … » ne s'affiche que si le second contrôle côté écran échoue (en pratique jamais atteint) ; la caméra est alors arrêtée et non relancée. Caméra inutilisable : écran « Camera not available » avec « Type the ISBN » et « Try again » | BR-UI-018 | — (script de la caméra) | `Web/Components/Pages/Books/AddBookScan.razor.cs:116-151`, `Web/wwwroot/js/isbnScanner.js:15-37,129-139`, `Web/Components/Pages/Books/AddBookScan.razor:12-23` |
| 2b | Saisie de l'ISBN | Tape l'ISBN | Contrôle : « Enter the ISBN » / « Enter a valid ISBN: 10 or 13 digits » | BR-UI-019 | — | `Web/Components/Pages/Books/AddBookManual.razor.cs:49-57`, `Web/Models/IsbnEntry.cs:8-22` |
| 3 | Ajout d'un livre | (arrivée avec un ISBN) | « Looking for this ISBN in the catalogues… » ; fiche trouvée (« Book found »), ou formulaire à compléter, ISBN prérempli (« Not found online: fill in the details »), avec l'avertissement « The catalogues could not be searched: fill in the details » si la recherche a échoué ; échec de chargement des bibliothèques : « Unable to load your libraries » | BR-UI-017, BR-UI-020, BR-UI-021 | Recherche par ISBN (SRC) : `Application/ComicInfoSearch/ComicSearchService.cs` | `Web/Components/Pages/Books/AddBookForm.razor.cs:63-132` |
| 4a | Ajout (fiche trouvée) | Choisit la bibliothèque, note éventuellement, clique sur « Add to <bibliothèque> » | Validation (si elle échoue, bascule vers le formulaire) puis création ; « <titre> was added to <bibliothèque> » ; retour à la bibliothèque ; autre échec : « Unable to add the book ». Bibliothèque présélectionnée si connue ou unique, sinon à choisir ; sans bibliothèque choisie le bouton s'intitule « Add book » | BR-UI-017, BR-UI-020, BR-UI-027 | Création de livre (BK) : `Application/Books/Create/CreateBookCommandHandler.cs` | `Web/Components/Pages/Books/AddBookForm.razor.cs:160-212` |
| 4b | Ajout (formulaire) | Complète les champs, clique sur « Add book » | Mêmes contrôles, avec erreurs sous les champs | BR-UI-008, BR-UI-022 à BR-UI-026 | idem | `Web/Components/Pages/Books/AddBookForm.razor:67-88` |
| 5 | Idem | (livre déjà connu) | « This book is already in your libraries » | BR-UI-028 | idem | `Web/Components/Pages/Books/AddBookForm.razor.cs:211` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Titre / Série | Obligatoires, 200 caractères | « Enter a title » / « Enter a series » ; « Title must not exceed 200 characters. » / « Serie must not exceed 200 characters. » | Oui pour l'obligation (non vide) ; longueur : en base seulement | `Web/Validators/BookValidator.cs:11-13,22-24`, `Application/Books/Create/CreateBookCommandHandler.cs:18-22` |
| ISBN | Obligatoire pour un livre papier, ISBN-10 ou ISBN-13 valide (clé de contrôle ; tirets et espaces ignorés), 20 caractères | « Enter the ISBN » / « Enter a valid ISBN: 10 or 13 digits » / « ISBN must not exceed 20 characters. » | Oui (non vide, validité) ; longueur : en base | `Web/Validators/BookValidator.cs:15-20`, `Application/Books/Create/CreateBookCommandHandler.cs:25-28` |
| Tome | Supérieur à 0 | « Volume number must be greater than 0. » | Non retrouvé (D-UI-003) | `Web/Validators/BookValidator.cs:26-27` |
| Pages | Supérieur à 0 si renseigné | « Number of pages must be greater than 0. » | Non retrouvé (D-UI-003) | `Web/Validators/BookValidator.cs:42-44` |
| Date de parution | Pas dans le futur | « Publish date must not be in the future. » | Non retrouvé (D-UI-003) | `Web/Validators/BookValidator.cs:46-47` |
| Auteurs / éditeurs / lien de couverture | 200 / 200 / 500 caractères | « … must not exceed … characters. » | Oui, en base (BR-DB-010) | `Web/Validators/BookValidator.cs:29-40` |
| Note | 0 à 5 (0 = pas de note) | « The rating must be between 1 and 5 stars. » (D-UI-002) | Oui (0 à 5) | `Web/Validators/BookValidator.cs:32-34`, `Application/Books/Create/CreateBookCommandHandler.cs:30-34` |
| Bibliothèque | Obligatoire | « Choose a library » | Oui (bibliothèque existante, appartenant à l'utilisateur, compatible avec le type du livre) | `Web/Validators/AddBookValidator.cs:10-11`, `Application/Books/Create/CreateBookCommandHandler.cs:45-75` |
| ISBN déjà connu | Refusé si un livre porte déjà cet ISBN — la recherche porte sur tous les livres de la base, pas seulement ceux de l'utilisateur (D-UI-020) | « This book is already in your libraries » | Oui (serveur seulement) | `Application/Books/Create/CreateBookCommandHandler.cs:36-43`, `Persistence/Repositories/BookRepository.cs:87-98` |
| Ajout | Pas d'étape de relecture en plus de la fiche ; bouton désactivé pendant l'enregistrement ; pas d'idempotence serveur | — | D-DOM-002 | `Web/Components/Pages/Books/AddBookForm.razor.cs:162-165` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Ajouter un livre | Utilisateur connecté, dans ses propres bibliothèques physiques | `Web/Components/Pages/Books/AddBookForm.razor.cs:94`, `Application/Books/Create/CreateBookCommandHandler.cs:45-75` |

## Liés

BR-UI-017 à BR-UI-028, D-UI-002, D-UI-003, D-DOM-002, Q-UI-009, UJ-FO-008.

Vérificateur 2026-10-17 : étape 1 — l'état vide d'une bibliothèque physique propose aussi « Add your first book » (`Web/Components/Pages/Libraries/LibraryDetailPage.razor:73`). Étape 2a — corrigée : le script de la caméra (`Web/wwwroot/js/isbnScanner.js:135-138`) appelle déjà le contrôle de validité avant de remonter l'ISBN, donc un code invalide est ignoré en silence et le message « Invalid ISBN scanned » n'est pas atteint en pratique ; BR-UI-018 (« reprend le scan ») est inexact : après ce message la caméra est arrêtée et jamais relancée (D-UI-021). Étape 2b — le contrôle accepte aussi un ISBN-10 terminé par X ; l'ISBN saisi est nettoyé puis mis en majuscules. Étape 3 — messages d'échec ajoutés. Étape 4a/4b — comportement de bascule vers le formulaire et libellé « Add book » ajoutés. Étape 5 — le message « This book is already in your libraries » n'apparaît que pour l'erreur de doublon, tout autre échec affiche « Unable to add the book » ; le doublon est cherché sur tous les livres de la base (D-UI-020). Validations — longueurs titre/série/ISBN : le serveur ne les contrôle pas (base seulement) ; tome, pages, date de parution : absence de contrôle serveur confirmée (le domaine ne les teste pas) ; la date de parution est comparée au jour courant en temps universel. Permissions — plage de lignes du serveur corrigée (45-75).

## Commentaires du produit
