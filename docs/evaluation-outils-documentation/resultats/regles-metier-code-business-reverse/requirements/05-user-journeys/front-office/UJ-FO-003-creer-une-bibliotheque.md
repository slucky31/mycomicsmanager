# UJ-FO-003 — Créer une bibliothèque

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté.
- **Objectif :** Créer une bibliothèque de livres papier ou de fichiers numériques, avec un nom, une couleur et une icône.
- **Entrée :** Bouton « New library » de la liste (`Web/Components/Pages/Libraries/LibrariesList.razor:20-21`), route `/libraries/add`.

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Création d'une bibliothèque | Ouvre le formulaire | Aperçu de la carte, type « Physical books » présélectionné, couleur et icône par défaut | BR-UI-006, BR-UI-007 | — | `Web/Components/Pages/Libraries/LibraryForm.razor:5-8,28-37`, `Web/Validators/LibraryUiDto.cs:14-20` |
| 2 | Idem | Saisit le nom, choisit type, couleur, icône | L'aperçu suit la saisie | BR-UI-005, BR-UI-007 | — | `Web/Components/Pages/Libraries/LibraryForm.razor.cs:44-45,51-73` |
| 3 | Idem | Clique sur « Create library » | Contrôle de la saisie ; si erreurs, résumé (« 1 field needs your attention. » / « N fields need your attention. ») et un message par champ (le premier de chaque champ) | BR-UI-005, BR-UI-006, BR-UI-008, BR-UI-009 | — | `Web/Components/Pages/Libraries/AddLibrary.razor.cs:35-47`, `Web/Models/FormErrors.cs:18`, `Web/Components/Pages/Libraries/LibraryForm.razor:16-19` |
| 4 | Idem | (envoi accepté) | Création ; message « <nom> was created » ; retour à la liste | BR-UI-010 | Création de bibliothèque (LIB) : `Application/Libraries/Create/CreateLibraryCommandHandler.cs` | `Web/Components/Pages/Libraries/AddLibrary.razor.cs:52-58` |
| 5 | Idem | (envoi refusé) | « Another library already has this name » (nom déjà pris par une autre bibliothèque du même utilisateur, sans tenir compte de la casse ni des espaces en début et fin) ; tout autre refus (dossier non créé, erreur inattendue) donne « Unable to create the library » ; l'utilisateur reste sur le formulaire | BR-UI-010 | idem | `Web/Components/Pages/Libraries/AddLibrary.razor.cs:60-67`, `Web/Models/LibraryDisplay.cs:29-30`, `Persistence/Queries/LibraryReadService.cs:80-98` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Nom | Obligatoire (un nom fait uniquement d'espaces est refusé), 200 caractères au plus | « Name is required. » / « Name must not exceed 200 characters. » | Oui : non vide et 200 caractères au plus (`Domain/Libraries/Library.cs:43`) ; longueur aussi en base (BR-DB-011) | `Web/Validators/LibraryValidator.cs:11-14` |
| Couleur | Obligatoire, `#RRGGBB` | « Color is required. » / « Invalid color format. » | Partiel : le serveur exige seulement une couleur non vide de 7 caractères au plus (`Domain/Libraries/Library.cs:48`) ; le format `#RRGGBB` n'est contrôlé que par l'écran (la couleur ne peut être choisie que dans une palette fixe, `Web/Components/Pages/Libraries/LibraryForm.razor:43-54`) | `Web/Validators/LibraryValidator.cs:16-19` |
| Icône | Obligatoire, 100 caractères au plus | « Icon is required. » / « Icon name must not exceed 100 characters. » | Oui : non vide et 100 caractères au plus (`Domain/Libraries/Library.cs:53`) ; longueur aussi en base (BR-DB-011) | `Web/Validators/LibraryValidator.cs:21-24` |
| Type | Valeur valide | « Please select a valid library type. » | Oui : un type inconnu est refusé (`Domain/Libraries/Library.cs:38-41`) ; le choix se fait par deux cartes (aucun cas d'écran où le type est vide) | `Web/Validators/LibraryValidator.cs:26-27` |
| Création | Pas de confirmation (action non destructive) ; bouton désactivé pendant l'enregistrement | — | Pas d'idempotence serveur (D-DOM-001) | `Web/Components/Pages/Libraries/AddLibrary.razor:10` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Créer une bibliothèque | Utilisateur connecté | `Web/Components/Pages/_Imports.razor:1` |

## Liés

BR-UI-005 à BR-UI-010, D-DOM-001, UJ-FO-004, D-UI-009.

Vérificateur 2026-10-17 : 5 étapes, 5 validations et 1 permission relues contre le code. Corrigé : (a) les trois mentions « Non retrouvé » côté serveur étaient inexactes ou incomplètes : le type est bien contrôlé (`Domain/Libraries/Library.cs:38`), la couleur et l'icône sont contrôlées sur la présence et la longueur (`Domain/Libraries/Library.cs:48,53`), seul le format `#RRGGBB` reste propre à l'écran ; (b) étape 5 : règle de comparaison du nom précisée (casse ignorée, espaces aux extrémités ignorés, par utilisateur) et deuxième message possible détaillé ; (c) citations d'étapes 1 et 2 recalées sur les lignes réelles. Étape 4, non signalé : pour une bibliothèque numérique, la création crée aussi le dossier de stockage et le dossier d'import avant l'enregistrement (`Application/Libraries/Create/CreateLibraryCommandHandler.cs:31-47`) ; le dossier porte le nom de la bibliothèque sans accents en majuscules, commun à tous les utilisateurs, voir D-UI-009 (proposée). Accessibilité contrôlée : résumé d'erreurs avec `role="alert"`, champ nom avec `aria-invalid` et `aria-describedby`, couleurs avec `aria-label` et `aria-pressed`, type dans un groupe `fieldset`/`legend` (`Web/Components/Pages/Libraries/LibraryForm.razor:16-37,40-47`). Le bouton « Create library » est présent deux fois (en-tête et pied de page, `Web/Components/Pages/Libraries/AddLibrary.razor:10,17`), tous deux désactivés pendant l'enregistrement.

## Commentaires du produit
