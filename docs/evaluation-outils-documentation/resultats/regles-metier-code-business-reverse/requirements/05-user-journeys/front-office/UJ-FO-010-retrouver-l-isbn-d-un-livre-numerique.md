# UJ-FO-010 — Retrouver l'ISBN d'un livre numérique

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté (propriétaire du livre).
- **Objectif :** Donner un ISBN à un livre numérique importé sans ISBN, afin d'aller chercher ses informations (UJ-FO-008).
- **Entrée :** « Find missing ISBNs » d'une bibliothèque numérique, ou « Find ISBN » / « Scan » / « Look in the reader » d'un livre (`Web/Components/Pages/Libraries/LibraryDetailPage.razor:145-146,174`, `Web/Components/Pages/Books/BookDetail.razor:115,163-175`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Livres d'une bibliothèque | « More actions » puis « Find missing ISBNs » | Lecture des pages lancée en arrière-plan pour les livres numériques de la bibliothèque dont l'ISBN est vide et dont les pages n'ont jamais été analysées ; message « Reading the pages of n book(s) in the background. A book showing several ISBNs lets you pick its own. », ou « Every book of this library has an ISBN or was already scanned » s'il n'y en a aucun ; le résultat de l'analyse n'est pas annoncé ensuite | BR-UI-034 | Démarrage du scan de bibliothèque (BK) : `Application/Books/IsbnScan/StartLibraryIsbnScanCommandHandler.cs` | `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:500-535`, `Application/Books/IsbnScan/StartLibraryIsbnScanCommandHandler.cs:21-38`, `Persistence/Repositories/BookRepository.cs:69-76` |
| 2 | Fiche d'un livre | Bouton de scan de l'encadré ISBN (visible tant que le livre n'a pas d'ISBN et qu'aucun choix n'est proposé) | Lecture immédiate des pages sur le serveur ; issues : un seul ISBN, inconnu des autres livres : enregistré sur le livre, « ISBN found in the pages » et ouverture de l'import d'informations ; un seul ISBN déjà porté par un autre livre, ou plusieurs : « Several ISBNs found in the pages: pick the book's own » (choix) ; aucun : « No ISBN found in the pages » ; lecture impossible : « The pages could not be read » ; erreur : « Unable to scan the pages of this book » |  BR-UI-035 | Scan d'un livre (BK) : `Application/Books/IsbnScan/ScanBookIsbnCommandHandler.cs` | `Web/Components/Pages/Books/BookDetail.razor.cs:139-194`, `Application/Books/IsbnScan/BookIsbnScan.cs:21-50`, `Application/Books/IsbnScan/ScanBookIsbnCommandHandler.cs:24-34` |
| 3 | Fiche d'un livre | Choisit un ISBN proposé | Ouvre l'import d'informations avec cet ISBN | BR-UI-039 | — | `Web/Components/Pages/Books/BookDetail.razor.cs:193-194` |
| 4 | Lecteur (mode ISBN) | « Find ISBN » / « Look in the reader » | Lecteur sans enregistrement de progression, pastille « ISBN search · progress not saved » ; raccourcis vers les premières et dernières pages | BR-UI-046 | — | `Web/Components/Pages/Books/BookReader.razor.cs:152,15`, `Web/Components/Pages/Books/BookReader.razor:59-62,96-114` |
| 5 | Idem | « Scan the first and last pages » ou « Find the ISBN on this page » | « Find the ISBN on this page » (menu « More actions », disponible aussi hors mode ISBN) lit la page affichée ; « Scan the first and last pages » (mode ISBN seulement) lit une à une les premières et dernières pages jusqu'à la première qui montre un ISBN (progression « Page i / n »), se positionne sur cette page et propose les ISBN trouvés ; sinon « No ISBN found on this page » / « No ISBN found in the first and last pages » ; lecture dans le navigateur | BR-UI-047 | — (lecture dans le navigateur) | `Web/Components/Pages/Books/BookReader.razor.cs:460-554` |
| 6 | Idem | Choisit un ISBN | Ouverture de l'import d'informations avec cet ISBN (l'ISBN n'est enregistré qu'à l'application des choix) ; la progression n'est PAS enregistrée en mode ISBN malgré l'appel d'enregistrement qui précède (D-UI-023) | BR-UI-039, BR-UI-047 | — | `Web/Components/Pages/Books/BookReader.razor.cs:574-578` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Scan de bibliothèque | Action grisée pendant le démarrage | « Unable to start the search of the missing ISBNs » | — | `Web/Components/Pages/Libraries/LibraryDetailPage.razor:145-146` |
| Scan d'un livre | Bouton grisé pendant l'analyse | « Unable to scan the pages of this book » / « The pages could not be read » | Oui (livre numérique du propriétaire, ISBN encore vide sinon refus) | `Web/Components/Pages/Books/BookDetail.razor.cs:152,186`, `Web/Components/Pages/Books/BookDetail.razor:163-171`, `Application/Books/IsbnScan/ScanBookIsbnCommandHandler.cs:24-34` |
| Lecture d'une page | Délai de 2 minutes par page (le premier usage télécharge le moteur de lecture) | « Unable to read the text of this page » (une page) ; « Unable to read the text of the pages » (lecture des premières et dernières pages) | — | `Web/Components/Pages/Books/BookReader.razor.cs:21,484,538,551` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Rechercher l'ISBN | Propriétaire du livre ; bibliothèques numériques seulement (actions absentes d'une bibliothèque physique ; le serveur refuse une bibliothèque physique et répond « introuvable » pour la bibliothèque ou le livre d'un autre utilisateur) | `Web/Components/Pages/Libraries/LibraryDetailPage.razor:38-41,172-176`, `Application/Books/IsbnScan/StartLibraryIsbnScanCommandHandler.cs:21-30`, `Application/Books/Read/OwnedDigitalBook.cs:17-33` |

## Liés

BR-UI-034, BR-UI-035, BR-UI-039, BR-UI-046, BR-UI-047, UJ-FO-008.

Vérificateur 2026-10-17 : étape 1 — critère de sélection précisé (livres numériques de la bibliothèque, ISBN vide, jamais analysés) ; le message de succès est cité en entier. Étape 2 — corrigée : le serveur enregistre lui-même l'ISBN quand un seul ISBN est trouvé et qu'aucun autre livre (de n'importe quel utilisateur, D-UI-020) ne le porte ; sinon (un seul ISBN déjà utilisé, ou plusieurs) les ISBN sont conservés comme propositions et le message « Several ISBNs found… » s'affiche même pour une seule proposition ; une analyse non terminée donne « The pages could not be read ». BR-UI-035 (« un ISBN unique trouvé est enregistré ») omet ce cas. Étape 5 — la lecture de la page courante existe aussi hors mode ISBN ; message d'échec différent pour la lecture des premières/dernières pages. Étape 6 — corrigée : en mode ISBN l'appel d'enregistrement de la progression ne fait rien, donc BR-UI-047 (« la progression est alors enregistrée avant de quitter ») est inexact (D-UI-023). Permissions — contrôles serveur ajoutés.

## Commentaires du produit
