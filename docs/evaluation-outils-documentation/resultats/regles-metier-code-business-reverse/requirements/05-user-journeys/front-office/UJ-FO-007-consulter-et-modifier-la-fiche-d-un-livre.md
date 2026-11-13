# UJ-FO-007 — Consulter et modifier la fiche d'un livre

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté (propriétaire du livre).
- **Objectif :** Voir la fiche d'un livre, corriger ses informations, gérer son historique de lectures.
- **Entrée :** Un livre de la bibliothèque, route `/books/{id}` (`Web/Components/Pages/Books/BookDetail.razor:1`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Fiche d'un livre | Ouvre la fiche | Couverture, titre, série et tome, auteurs, format, dernière note, « Read » (livre numérique : « Read », ou « Continue reading » avec « Page n of N » et barre de progression dès qu'une page a été lue), détails, historique des lectures ; livre inexistant, appartenant à un autre utilisateur ou non chargeable : page « Book not found » (en cas d'échec de chargement, « Unable to load the book » s'affiche aussi) | BR-UI-041 (livre numérique) | Lecture d'un livre (BK) : `Application/Books/GetById/GetBookQueryHandler.cs` | `Web/Components/Pages/Books/BookDetail.razor.cs:50-76`, `Web/Components/Pages/Books/BookDetail.razor:31-39` |
| 2 | Idem | Clique sur « Add a reading » (bouton principal d'un livre papier ; pour un livre numérique, bouton de l'historique, le bouton principal étant « Read ») | Choix d'une note de 1 à 5 étoiles pour une lecture « today » ; « Save » grisé tant qu'aucune étoile ; échec : « Unexpected error while adding reading date » ; succès : fiche rechargée | BR-UI-029 | Ajout de lecture (BK) : `Application/Books/AddReadingDate/AddReadingDateCommandHandler.cs` | `Web/Components/Pages/Books/BookDetail.razor:200-218` |
| 3 | Idem | Clique sur « Edit » | Ouvre la modification | — | — | `Web/Components/Pages/Books/BookDetail.razor:21-22` |
| 4 | Modification d'un livre | Modifie les champs, clique sur « Save changes » | Validation ; « <titre> was saved » ; retour à la fiche ; ISBN déjà utilisé par un autre livre (de n'importe quel utilisateur, D-UI-020) : « Another book already has this ISBN » ; autre échec : « Unable to save the changes » ; livre introuvable à l'ouverture : « Book not found » | BR-UI-008, BR-UI-009, BR-UI-022 à BR-UI-026, BR-UI-028 | Mise à jour (BK) : `Application/Books/Update/UpdateBookCommandHandler.cs` | `Web/Components/Pages/Books/EditBook.razor.cs:105-159` |
| 5 | Idem | Supprime une lecture | Confirmation « Delete this reading? » puis suppression, relecture de la liste ; une seule suppression à la fois ; échec : « Unable to delete the reading » | BR-UI-030 | Suppression de lecture (BK) : `Application/Books/DeleteReadingDate/DeleteReadingDateCommandHandler.cs` | `Web/Components/Pages/Books/EditBook.razor.cs:162-197` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Champs du livre | Mêmes règles qu'à l'ajout, sans la bibliothèque ni la note | Voir UJ-FO-006 | Voir UJ-FO-006 | `Web/Validators/BookValidator.cs:9-48` |
| ISBN d'un livre numérique | Peut rester vide ; s'il est renseigné il doit être valide | « Enter a valid ISBN: 10 or 13 digits » | Oui | `Web/Validators/BookValidator.cs:15-20` |
| Chargement de la fiche | Échec signalé | « Unable to load the book » | n/a | `Web/Components/Pages/Books/BookDetail.razor.cs:67-68` |
| Note d'une lecture existante | Non modifiable depuis l'écran (étoiles en lecture seule, suppression seule ; « New readings are added from the book’s page. ») | — | — | `Web/Components/Pages/Books/EditBook.razor:47-73` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Voir, modifier un livre, gérer ses lectures | Propriétaire du livre (le serveur répond « introuvable » sinon) | `Application/Books/GetById/GetBookQueryHandler.cs:25-35`, `Application/Books/Update/UpdateBookCommandHandler.cs:35-45`, `Application/Books/AddReadingDate/AddReadingDateCommandHandler.cs:29-39`, `Application/Books/DeleteReadingDate/DeleteReadingDateCommandHandler.cs:19-29` |

## Liés

BR-UI-022 à BR-UI-030, Q-DOM-005, UJ-FO-008, UJ-FO-014.

Vérificateur 2026-10-17 : étape 1 — le chemin `Web/Components/Pages/Books/BookDetail.razor` nu est remplacé par son chemin complet ; la fiche manquante et la fiche d'un autre utilisateur sont indistinguables (le serveur répond « introuvable »). Étape 2 — le bouton « Add a reading » est le bouton principal seulement pour un livre papier ; sa date est « today » (date du jour en temps universel côté serveur). Étape 4 — « Another book already has this ISBN » : le doublon est cherché sur tous les livres de la base (D-UI-020) ; pour un livre papier, un ISBN vide est aussi refusé par le serveur (erreur générique « Unable to save the changes ») si le contrôle d'écran était contourné. Étape 5 — suppression d'une lecture : le serveur ne signale aucune erreur si l'identifiant de lecture n'existe pas dans le livre. Validations — la citation d'une ligne qui ne concernait pas les lectures est remplacée par la liste des lectures de l'écran de modification. Permissions — la lecture et la suppression d'une lecture sont aussi limitées au propriétaire (même contrôle « introuvable »).

## Commentaires du produit
