# UJ-FO-009 — Lire un livre numérique

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté (propriétaire du livre).
- **Objectif :** Lire un livre numérique page par page, reprendre là où il s'était arrêté, et le marquer comme lu à la fin.
- **Entrée :** Bouton « Read » de la fiche ou action « Read » du menu du livre, route `/books/{id}/read` (`Web/Components/Pages/Books/BookReader.razor:1`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Lecteur | Ouvre le livre | Reprise à la dernière page avec les réglages mémorisés ; livre illisible (introuvable, d'un autre utilisateur, papier, archive sans page ou identifiant invalide) : écran « This book cannot be read » avec « Back to book », précédé d'un message d'erreur du serveur ou « Unable to open this book » ; la dernière page est ramenée dans les limites du livre si l'archive a raccourci | BR-UI-041, BR-UI-044 | Informations de lecture (BK) : `Application/Books/Read/GetBookReaderInfoQueryHandler.cs` ; pages : `GET /api/books/{id}/pages/{n}` | `Web/Components/Pages/Books/BookReader.razor.cs:137-173`, `Web/Components/Pages/Books/BookReader.razor:14-24`, `Web/EndPoints/BooksEndpoints.cs:18`, `Application/Books/Read/GetBookReaderInfoQueryHandler.cs:21-41` |
| 2 | Idem | Tourne les pages (touches, gestes, curseur, défilement) | Page affichée, compteur et barre de progression mis à jour ; sens droite-à-gauche inverse les commandes (flèches, zones de l'écran, curseur) ; en bas de la dernière page, ou en poussant « page suivante » sur la dernière, la fenêtre de fin s'ouvre (étape 5) | BR-UI-045 | — | `Web/Components/Pages/Books/BookReader.razor.cs:245-344`, `Web/Components/Pages/Books/BookReader.razor:116-129` |
| 3 | Idem | S'arrête sur une page | Progression (première page de la ligne affichée) enregistrée après une seconde sur la page, à la fermeture et à la sortie ; rien si la page n'a pas changé ; un échec n'est que journalisé | BR-UI-042 | Mise à jour de la progression (BK) : `Application/Books/ReadingProgress/UpdateReadingProgressCommandHandler.cs` | `Web/Components/Pages/Books/BookReader.razor.cs:582-631` |
| 4 | Idem | Ouvre « Display » | Choix de mise en page, de défilement et de sens de lecture, mémorisés pour ce livre | BR-UI-044 | Réglages du lecteur (BK) : `Application/Books/ReaderDisplay/UpdateReaderSettingsCommandHandler.cs` | `Web/Components/Pages/Books/BookReader.razor.cs:373-400` |
| 5 | Idem | Atteint la dernière page | Fenêtre « You finished this book » / « Rate it to add a reading date. » : note en étoiles (3 par défaut), boutons « Keep reading » et « Mark as read » ; jamais proposée en mode recherche d'ISBN | BR-UI-043 | — | `Web/Components/Pages/Books/BookReader.razor:223-239` |
| 6 | Idem | Clique sur « Mark as read » | Ajout d'une lecture à la date du jour, progression remise à la première page, retour à la fiche, « Reading date added » ; ou « Keep reading » pour continuer | BR-UI-043, D-UI-005 | Ajout de lecture (BK) | `Web/Components/Pages/Books/BookReader.razor.cs:633-662` |
| 7 | Idem | Ferme le lecteur (flèche, Échap) | Progression enregistrée, retour à la fiche | BR-UI-042 | — | `Web/Components/Pages/Books/BookReader.razor.cs:664-668` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Note de fin de livre | Étoiles de 1 à 5 (3 par défaut) ; l'écran ne grise pas « Mark as read » si la note vaut 0 (contrairement à la fiche) | Aucun message propre ; une note hors de 1 à 5 donne « Unexpected error while adding reading date » | Oui (1 à 5) | `Application/Books/AddReadingDate/AddReadingDateCommandHandler.cs:24-27`, `Web/Components/Pages/Books/BookReader.razor:229-235` |
| « Mark as read » | Bouton grisé pendant l'enregistrement | « Unexpected error while adding reading date » en cas d'échec | — | `Web/Components/Pages/Books/BookReader.razor:233` |
| Réglages d'affichage | Échec signalé (avertissement) sans bloquer la lecture | « Unable to save the display settings » | Oui (réglages valides, propriétaire) | `Web/Components/Pages/Books/BookReader.razor.cs:391`, `Application/Books/ReaderDisplay/UpdateReaderSettingsCommandHandler.cs:17-27` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Lire un livre | Propriétaire du livre numérique (pages servies après vérification de l'utilisateur ; livre d'un autre utilisateur = « introuvable » ; livre papier refusé) | `Web/EndPoints/BooksEndpoints.cs:65-93`, `Application/Books/Read/OwnedDigitalBook.cs:17-33` |
| Enregistrer la progression, les réglages d'affichage | Propriétaire du livre numérique (même contrôle) | `Application/Books/ReadingProgress/UpdateReadingProgressCommandHandler.cs:22-26`, `Application/Books/ReaderDisplay/UpdateReaderSettingsCommandHandler.cs:23-27` |

## Liés

BR-UI-041 à BR-UI-046, D-UI-005, UJ-FO-010.

Vérificateur 2026-10-17 : le chemin nu `Web/Components/Pages/Books/BookReader.razor` est remplacé par son chemin complet (entrée, étapes 1 et 5, validations). Étape 1 — le libellé de la fiche est « Read » ou « Continue reading » (avec « Page n of N » et barre de progression) selon la dernière page lue (`Web/Models/BookDetailViewModel.cs:107-123`) ; en mode recherche d'ISBN la lecture commence à la première page. Étape 3 — la progression n'est contrôlée côté serveur que pour être positive ou nulle (aucune borne haute : elle est ramenée dans les limites à la réouverture) ; l'enregistrement a lieu une seconde après le dernier changement de page. Étape 5 — « Mark as read » n'est pas grisé quand la note est 0, alors que la fiche grise « Save » (UJ-FO-007 étape 2) : D-UI-022 (la note 0 n'est atteignable que si le composant d'étoiles permet de désélectionner la note, non vérifié). Étape 6 — l'ordre est : ajout de la lecture, remise à zéro de la progression (échec silencieux), message « Reading date added » puis retour à la fiche (D-UI-005 confirmée). Étape 7 — « Échap » ferme le lecteur comme la flèche. Réglages — l'échec est signalé en avertissement (pas en erreur).

## Commentaires du produit
