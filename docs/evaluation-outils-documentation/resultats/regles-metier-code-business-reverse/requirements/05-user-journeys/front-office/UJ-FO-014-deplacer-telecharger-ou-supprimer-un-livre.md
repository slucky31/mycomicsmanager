# UJ-FO-014 — Déplacer, télécharger ou supprimer un livre

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté (propriétaire du livre).
- **Objectif :** Changer un livre de bibliothèque, récupérer son fichier, ou le retirer définitivement.
- **Entrée :** Menu d'actions d'un livre (bibliothèque) ou menu « ⋮ » / boutons de la fiche (`Web/Components/Pages/Libraries/LibraryDetailPage.razor:149-182`, `Web/Components/Pages/Books/BookDetail.razor:111-119,230-250`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Bibliothèque ou fiche | « Move to another library » | Boîte de choix des autres bibliothèques du même type, dont la bibliothèque actuelle (non choisissable), « Suggested » étant la destination contenant le plus de livres de la même série (séries comparées sans accents, casse ni article initial ; à égalité, ordre alphabétique) ; « Move » grisé tant que rien n'est choisi ; sans destination : message « No other library of the same type » (et non un texte dans la boîte) ; échec de chargement : « Failed to load libraries » | BR-UI-032 | Destinations et déplacement (BK) : `Application/Books/MoveTargets/`, `Application/Books/Move/MoveBookCommandHandler.cs`, `Application/Books/MoveTargets/GetBookMoveTargetsQueryHandler.cs:38-56` | `Web/Services/BookMoveWorkflow.cs:14-56` |
| 2 | Idem | Choisit une destination et valide | Livre (et, pour un livre numérique, son fichier) déplacé ; « Book moved » ; le livre quitte la liste ou la fiche est rechargée ; refus : motif du serveur, à défaut « Failed to move book » ; si l'enregistrement échoue le fichier est remis dans sa bibliothèque d'origine | BR-UI-032 | idem | `Web/Services/BookMoveWorkflow.cs:22-31`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:543-568`, `Application/Books/Move/MoveBookCommandHandler.cs:72-97` |
| 3 | Idem | « Download CBZ » (menu de la bibliothèque numérique, ouvert dans un nouvel onglet) / « CBZ » (fiche) | Téléchargement du fichier (livres numériques seulement) ; les refus du serveur (non connecté, livre introuvable ou d'un autre utilisateur, livre papier « Only digital books can be downloaded. », fichier hors du dossier de stockage, fichier absent « File not found on server. ») s'affichent comme une page d'erreur brute, sans message de l'application | BR-UI-033 | `GET /api/books/{id}/download` (BK) | `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:537-541`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor:166-176`, `Web/Components/Pages/Books/BookDetail.razor:113-117`, `Web/EndPoints/BooksEndpoints.cs:21-63` |
| 4 | Idem | « Delete » (menu ⋮ de la fiche, menu du livre dans la bibliothèque) | Boîte de confirmation (bouton « Delete book » rouge, « Cancel ») : le livre est supprimé avec ses lectures, irréversible ; sur la fiche le message cite le titre | BR-UI-031 | — | `Web/Components/Pages/Books/BookDetail.razor.cs:225-228`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:583`, `Web/Extensions/DialogServiceExtensions.cs:8-13` |
| 5 | Idem | Confirme | Suppression du livre (et, après l'enregistrement, de son fichier pour un livre numérique) ; retour à la bibliothèque (fiche) ou rechargement de la liste ; échec : « Failed to delete book » | BR-UI-031 | Suppression de livre (BK) : `Application/Books/Delete/DeleteBookCommandHandler.cs:26-42` | `Web/Components/Pages/Books/BookDetail.razor.cs:230-249`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:584-604` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Déplacement | Même type de bibliothèque, bibliothèque actuelle non choisissable ; la destination doit appartenir à l'utilisateur | « No other library of the same type » / motif du serveur (à défaut « Failed to move book ») | Oui (type de bibliothèque, propriétaire du livre et de la destination) | `Web/Services/BookMoveWorkflow.cs:25,45-50`, `Application/Books/Move/MoveBookCommandHandler.cs:29-45,53-57` |
| Déplacement | Un seul à la fois ; la boîte de choix tient lieu de confirmation | — | — | `Web/Components/Pages/Books/BookDetail.razor.cs:254-259` |
| Suppression | Confirmation obligatoire ; pas de garde contre un double envoi pendant la suppression | « This book will be deleted with its readings. This cannot be undone. » (bibliothèque) ; « “<titre>” will be deleted with its readings. This cannot be undone. » (fiche) | Cascade des lectures (BR-DB-008) | `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs:580-605` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Déplacer, télécharger, supprimer | Propriétaire du livre (le serveur filtre par utilisateur ; livre d'un autre utilisateur = « introuvable ») | `Web/EndPoints/BooksEndpoints.cs:35-41`, `Application/Books/Move/MoveBookCommandHandler.cs:29-40`, `Application/Books/Delete/DeleteBookCommandHandler.cs:26-30` |

## Liés

BR-UI-031 à BR-UI-033, BR-DB-008, Q-UI-007.

> Vérificateur 2026-10-17 : (1) étape 1 : critère de la destination « Suggested » ajouté (plus de livres de la même série) ; (2) étape 3 : les refus du téléchargement ne passent par aucun message de l'application (ouverture directe de l'adresse) ; le « Download CBZ » n'apparaît dans le menu que pour une bibliothèque numérique ; (3) étape 4 : le message de confirmation de la fiche cite le titre, celui de la bibliothèque non ; (4) étape 5 : la suppression d'un livre numérique supprime aussi son fichier, mais seulement après l'enregistrement en base, et le résultat de la suppression du fichier n'est pas contrôlé (D-UI-034) ; (5) déplacement : le message d'échec affiché est celui du serveur, « Failed to move book » n'est qu'un repli ; (6) chemins nus `Web/Components/Pages/Libraries/LibraryDetailPage.razor.cs` remplacés par le chemin complet. Aucune garde contre un double envoi à la suppression sur la fiche non plus (second envoi : « Failed to delete book »).

## Commentaires du produit
