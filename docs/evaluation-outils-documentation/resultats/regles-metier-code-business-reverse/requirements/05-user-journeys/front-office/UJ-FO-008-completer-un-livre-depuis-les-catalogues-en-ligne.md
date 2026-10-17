# UJ-FO-008 — Compléter un livre depuis les catalogues en ligne

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté (propriétaire du livre).
- **Objectif :** Remplacer ou compléter les informations d'un livre (titre, série, tome, auteurs, éditeur, parution, pages, couverture) avec celles des catalogues en ligne.
- **Entrée :** « Import info from the web » (menu du livre) ou « Import info » (fiche) ; route `/books/{id}/find-details` (`Web/Components/Pages/Books/FindBookDetails.razor:1`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Compléter un livre | Ouvre la page | Chargement du livre (échec : « Book not found ») ; si le livre a un ISBN (ou si un ISBN valide lu dans les pages est passé dans l'adresse, qui remplace alors l'ISBN actuel), interrogation simultanée des catalogues activés avec progression « Searching the sources… (n/total) » ; sans ISBN : « This book has no ISBN, so the web sources cannot be searched. » ; chaque source s'affiche à sa réponse ; seuls les champs dont les valeurs diffèrent sont proposés | BR-UI-036, BR-UI-039, BR-UI-040 | Recherches par ISBN (SRC) : BnF, Bédéthèque, Bubble, OpenLibrary, Google Books | `Web/Components/Pages/Books/FindBookDetails.razor.cs:62-136` |
| 2 | Idem | (source en échec) | « Could not fetch data from … . You can still save with current values. » | BR-UI-036 | — | `Web/Components/Pages/Books/FindBookDetails.razor.cs:243-247` |
| 3 | Idem | (aucune source ne connaît le livre) | « No source found this book. » | BR-UI-036 | — | `Web/Components/Pages/Books/FindBookDetails.razor.cs:207-208`, `Web/Components/Pages/Books/FindBookDetails.razor:51-54` |
| 4 | Idem | Choisit, champ par champ, la valeur actuelle ou celle d'une source | Le libellé du bouton d'application compte les changements (« Apply changes », « Apply 1 change », « Apply n changes ») ; le bouton est grisé tant qu'il n'y a aucun changement ; l'ISBN lu dans les pages compte pour un changement | BR-UI-037, BR-UI-039 | — | `Web/Components/Pages/Books/FindBookDetails.razor.cs:225-235`, `Web/Components/Pages/Books/FindBookDetails.razor:136-144`, `Web/Models/BookDetailsComparison.cs:160-165` |
| 5 | Idem | Applique | Couverture choisie, si adresse web (http ou https) ne pointant pas déjà vers l'hébergeur d'images, copiée chez lui (échec : « Cover could not be uploaded to Cloudinary. The original URL will be saved. », l'enregistrement continue) ; une valeur de tome, de date ou de pages illisible garde la valeur actuelle ; mise à jour du livre (avec l'ISBN lu dans les pages le cas échéant) ; retour à la fiche ; échec : « Failed to update book: <description de l'erreur> » | BR-UI-038, D-UI-004 | Copie de couverture (STO) ; mise à jour (BK) | `Web/Components/Pages/Books/FindBookDetails.razor.cs:341-417` |
| 6 | Idem | Clique sur « Cancel » | Retour à la fiche sans rien enregistrer | — | — | `Web/Components/Pages/Books/FindBookDetails.razor.cs:419` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Valeurs appliquées | Aucune validation d'écran (le validateur du formulaire n'est pas utilisé) | « Failed to update book: <raison du serveur> » | Partiellement (ISBN valide, titre et série présents, ISBN pas déjà porté par un autre livre ; longueurs en base ; tome, pages, date de parution non contrôlés) | `Web/Components/Pages/Books/FindBookDetails.razor.cs:392-416` |
| Application | Bouton grisé pendant l'enregistrement ; pas de confirmation avant écrasement (le détail des changements est visible avant) | — | — | `Web/Components/Pages/Books/FindBookDetails.razor.cs:341-349`, `Web/Components/Pages/Books/FindBookDetails.razor:136-143` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Compléter un livre | Propriétaire du livre | `Application/Books/Update/UpdateBookCommandHandler.cs:41-45` |
| Utiliser une source | Source activée dans « Settings → Features » | `Web/Components/Pages/Books/FindBookDetails.razor.cs:190-201` |

## Liés

BR-UI-036 à BR-UI-040, D-UI-004, Q-UI-008, INT-002 à INT-008, UJ-FO-010.

Vérificateur 2026-10-17 : le chemin nu `Web/Components/Pages/Books/FindBookDetails.razor` est remplacé par son chemin complet (étape 3, validations). Étape 1 — ajout de l'ISBN passé dans l'adresse, de la progression et du message sans ISBN (`Web/Components/Pages/Books/FindBookDetails.razor:42-49`). Étape 2 — seules les erreurs réseau, de délai ou d'annulation d'une source produisent cet avertissement (le nom de la source y est « the BnF catalogue », « OpenLibrary », « Google Books », « Bedetheque », « Bubble ») ; toute autre exception n'est pas interceptée (Q-UI-020). Étape 3 — libellé exact « No source found this book. ». Étape 5 — la copie de couverture est précisée (hébergeur d'images = Cloudinary ; adresse déjà chez lui laissée telle quelle ; échec = avertissement et enregistrement avec l'adresse d'origine, BR-UI-038) ; la mise à jour est faite sans jeton d'annulation et sans traitement d'une exception inattendue (le bouton reste alors grisé). Aucune validation d'écran confirmée : un titre/série vide issu d'un catalogue est retombé sur la valeur actuelle (`?? valeur actuelle`), les dates futures et tomes ≤ 0 ne sont pas contrôlés (D-UI-003/004). Permissions — plage 41-45 de la mise à jour serveur confirmée (propriétaire, sinon « introuvable »).

## Commentaires du produit
