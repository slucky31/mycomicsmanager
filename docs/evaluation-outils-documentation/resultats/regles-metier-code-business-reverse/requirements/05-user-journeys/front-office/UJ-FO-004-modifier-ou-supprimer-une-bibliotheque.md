# UJ-FO-004 — Modifier ou supprimer une bibliothèque

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté (propriétaire).
- **Objectif :** Changer le nom, la couleur ou l'icône d'une bibliothèque, ou la supprimer avec ses livres.
- **Entrée :** Menu « ⋮ » d'une carte de la liste (`Web/Components/Pages/Libraries/LibrariesList.razor:66-72`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Liste des bibliothèques | Ouvre le menu d'une bibliothèque | Propose « Edit name, icon and color », « Import books » (numérique seulement), « Delete library » | BR-UI-016 | — | `Web/Components/Pages/Libraries/LibrariesList.razor:66-72` |
| 2 | Modification d'une bibliothèque | Choisit « Edit… » | Formulaire prérempli ; le type n'est pas modifiable ; aperçu avec nombre de livres et couvertures | BR-UI-007 | Lecture d'une bibliothèque et résumés (LIB) | `Web/Components/Pages/Libraries/EditLibrary.razor.cs:55-79` |
| 3 | Idem | Clique sur « Save changes » | Contrôle ; message « <nom> was saved » ; retour à la bibliothèque. Refus : « Another library already has this name » (nom pris) ou « Unable to save the changes » (autre cause, dont un dossier numérique impossible à déplacer) ; l'utilisateur reste sur le formulaire. Pour une bibliothèque numérique, le renommage déplace aussi le dossier de stockage et le dossier d'import | BR-UI-005, BR-UI-006, BR-UI-008, BR-UI-009, BR-UI-010 | Mise à jour (LIB) : `Application/Libraries/Update/UpdateLibraryCommandHandler.cs` | `Web/Components/Pages/Libraries/EditLibrary.razor.cs:97-134`, `Application/Libraries/Update/UpdateLibraryCommandHandler.cs:62-96` |
| 4 | Idem | Bibliothèque introuvable ou non chargée (adresse invalide, bibliothèque d'un autre utilisateur, erreur) | Message temporaire « Unable to load the library », et page « Library not found — This library does not exist or could not be loaded. » avec le bouton « Back to libraries » | — | — | `Web/Components/Pages/Libraries/EditLibrary.razor:13-19`, `Web/Components/Pages/Libraries/EditLibrary.razor.cs:51-71` |
| 5 | Liste des bibliothèques | Choisit « Delete library » | Boîte de confirmation « Delete library » (bouton d'action rouge « Delete library », bouton « Cancel » ; Échap ou un clic à côté annule ; le focus démarre sur le premier bouton) rappelant la suppression des livres (avec leur nombre) et l'irréversibilité | BR-UI-011 | — | `Web/Components/Pages/Libraries/LibrariesList.razor.cs:56-67`, `Web/Extensions/DialogServiceExtensions.cs:8-26`, `Web/Components/Pages/Dialogs/ConfirmationDialog.razor:2,12-15`, `Web/Extensions/AppDialogs.cs:8-12` |
| 6 | Idem | Confirme | Suppression, message « The library was correctly deleted », rechargement | BR-UI-011 | Suppression de bibliothèque (LIB) : `Application/Libraries/Delete/DeleteLibraryCommandHandler.cs` | `Web/Components/Pages/Libraries/LibrariesList.razor.cs:69-79` ; pour une bibliothèque numérique, les dossiers de stockage et d'import sont supprimés avant l'enregistrement : `Application/Libraries/Delete/DeleteLibraryCommandHandler.cs:25-42` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Nom, couleur, icône | Mêmes règles qu'à la création | Voir UJ-FO-003 | Voir UJ-FO-003 | `Web/Validators/LibraryValidator.cs:11-24` |
| Suppression | Confirmation obligatoire ; pas de saisie du nom | « The library “…” will be deleted with its … This cannot be undone. » | Suppression en cascade des livres (BR-DB-007) | `Web/Components/Pages/Libraries/LibrariesList.razor.cs:60-63` |
| Suppression | Échec signalé | « Failed to delete library » | — | `Web/Components/Pages/Libraries/LibrariesList.razor.cs:70-75` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Modifier ou supprimer une bibliothèque | Utilisateur connecté ; le serveur vérifie le propriétaire (« introuvable » sinon) | `Application/Libraries/Update/UpdateLibraryCommandHandler.cs:20-23`, `Application/Libraries/Delete/DeleteLibraryCommandHandler.cs:20-23` |

## Liés

BR-UI-005 à BR-UI-011, BR-DB-007, D-DOM-006, D-DOM-007, Q-DOM-007, Q-UI-007, D-UI-009.

Vérificateur 2026-10-17 : 6 étapes, 3 validations et 1 permission relues. Corrigé : étape 4, le parcours annonçait « Library not found avec retour à la liste » : c'est une page d'état avec un bouton « Back to libraries » (pas de retour automatique) précédée d'un message temporaire « Unable to load the library » ; étape 3, messages de refus manquants ajoutés (« Unable to save the changes », nom déjà pris) et effet de bord du renommage d'une bibliothèque numérique (déplacement de dossiers, avec retour arrière du premier déplacement si le second échoue, `Application/Libraries/Update/UpdateLibraryCommandHandler.cs:81-92`) ; étape 6, effet de bord de la suppression numérique documenté (D-DOM-007) ; la ligne de permission citait la seule ligne 22 : la vérification complète est 20-23 et existe aussi pour la suppression. Confirmé : le message d'échec de suppression « Failed to delete library » est le même quelle qu'en soit la cause (dossier non supprimé, introuvable), `Web/Components/Pages/Libraries/LibrariesList.razor.cs:70-75`. Accessibilité : la boîte est un `alertdialog` titré et décrit (`Web/Components/Pages/Dialogs/ConfirmationDialog.razor:2`), le menu d'actions de carte est étiqueté (« More actions for <nom> »). Rien de nouveau n'est exigé pour modifier le type : il n'est pas transmis (`Web/Components/Pages/Libraries/EditLibrary.razor.cs:114`), conforme à BR-UI-007.

## Commentaires du produit
