# UJ-FO-012 — Traiter les entrées du flux (import par flux)

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté.
- **Objectif :** Suivre les articles repérés dans le lecteur de flux, trancher les cas ambigus (doublons, liens multiples), relancer, corriger ou nettoyer.
- **Entrée :** Entrée « Feeds » de la navigation (avec pastille), route `/feed-imports` (`Web/Components/Pages/FeedImportsPage.razor:1`, `Web/Components/Layout/NavBar.razor:38-48`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Décisions d'import | Ouvre l'écran | Liste par pages de 20, filtres par statut avec compteurs (tous les statuts sauf « Imported », jamais attribué), panneau de détail (côté, ou sous la carte sur téléphone) ; liste vide : « No import decisions. » ; échecs : « Unable to load the import decisions. » / « Unable to count the import decisions. » | BR-UI-054 | Décisions et compteurs (FDI) : `Web/Services/FeedImportService.cs:31-66` | `Web/Components/Pages/FeedImportsPage.razor.cs:49-131` |
| 2 | Idem | Filtre, cherche (recherche appliquée 400 ms après la frappe, espaces retirés, terme vide = pas de recherche) | Première page rechargée ; réponses périmées ignorées | BR-UI-054 | idem | `Web/Components/Pages/FeedImportsPage.razor.cs:133-148` |
| 3 | Idem | « Sync now » | « Sync started. Refresh the list in a few moments. » (synchronisation lancée en arrière-plan, liste rechargée aussitôt) ; bouton absent et note « Miniflux sync is turned off (Settings, Features). » si la fonctionnalité est coupée ; bouton grisé pendant la demande | BR-UI-055 | Déclenchement (FDI) : `Web/Services/FeedImportService.cs:68-77` | `Web/Components/Pages/FeedImportsPage.razor.cs:170-192` |
| 4 | Idem | Doublon probable : « Not a duplicate » ou « Confirm duplicate » | « Decision updated. », décision enregistrée, liste et pastille actualisées ; après un arbitrage réussi, un téléchargement est demandé (sans effet si la décision n'est pas à l'état « liens extraits ») seulement si une clé Debrid-Link est configurée (sinon rien n'est lancé, sans message à l'écran ; D-UI-032) | BR-UI-056, BR-UI-060 | Arbitrage (FDI) : `Application/FeedImports/Arbitrate/ResolveFeedImportArbitrationCommandHandler.cs:30-35` | `Web/Components/Pages/FeedImportsPage.razor:194-210`, `Web/Components/Pages/FeedImportsPage.razor.cs:196-198`, `Web/Services/FeedImportService.cs:91-97,209-218` |
| 5 | Idem | Liens ambigus : « Keep this link » (sur chaque fichier) ou « All these links are the same file » (si plusieurs fichiers) | « Decision updated. » ; le contrôle de doublon est relancé sur le fichier retenu et peut aboutir à un nouvel arbitrage | BR-UI-057, BR-UI-060 | idem | `Web/Components/Pages/FeedImportsPage.razor:43-58,155-162`, `Application/FeedImports/Arbitrate/ResolveFeedImportArbitrationCommandHandler.cs:45-75` |
| 6 | Idem | « Force download » (un seul fichier, doublon probable ou ignoré comme doublon), « Retry » (décision échouée, ignorée ou téléchargement interrompu depuis plus d'une heure), « Ignore » (en attente, liens extraits, arbitrage, doublon ignoré, échec) | Action appliquée, « Decision updated. » ; le téléchargement ou une nouvelle synchronisation est lancé selon le statut obtenu | BR-UI-058, BR-UI-060 | Actions manuelles (FDI) : `Application/FeedImports/Manage/` | `Web/Components/Pages/FeedImportsPage.razor:60-96`, `Web/Components/Pages/FeedImportsPage.razor.cs:200-202`, `Domain/FeedImports/FeedImportDecision.cs:309-341`, `Web/Services/FeedImportService.cs:189-207` |
| 7 | Idem | « Fix series / volume » (un seul fichier ; liens extraits, arbitrage, doublon ignoré, échec ou ignorée) | Formulaire (série obligatoire) ; contrôle de doublon relancé | BR-UI-059 | Correction (FDI) : `Application/FeedImports/Manage/CorrectFeedImportDecisionCommandHandler.cs` | `Web/Components/Pages/FeedImportsPage.razor:97-118`, `Domain/FeedImports/FeedImportDecision.cs:315-330` |
| 8 | Idem | Supprime une ou plusieurs décisions (menu de la carte, panneau, ou « Select several ») | Confirmation ; les décisions dont le téléchargement est en cours (moins d'une heure) ou dont l'import tourne sont conservées | BR-UI-061 | Suppression (FDI) : `Application/FeedImports/Delete/DeleteFeedImportDecisionCommandHandler.cs` | `Web/Components/Pages/FeedImportsPage.razor.cs:259-329`, `Domain/FeedImports/FeedImportDecision.cs:338-341` |
| 9 | Idem | « Delete downloaded items » | Confirmation ; sont supprimées les décisions téléchargées dont l'import est terminé avec succès ou dont la tâche d'import a été retirée de l'écran Import ; celles dont l'import tourne ou a échoué sont conservées | BR-UI-062 | Suppression des téléchargées (FDI) : `Application/FeedImports/Delete/DeleteDownloadedFeedImportDecisionsCommandHandler.cs:57-60` | `Web/Components/Pages/FeedImportsPage.razor.cs:331-375` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Série (correction) | Obligatoire (bouton grisé si vide ou espaces), 200 caractères | — | Oui : refus si vide, plus de 200 caractères (série) ou 300 (titre), ou volume négatif ; espaces retirés | `Web/Components/Pages/FeedImportsPage.razor:102,111`, `Application/FeedImports/Manage/CorrectFeedImportDecisionCommandHandler.cs:18-25,33`, `Domain/FeedImports/FeedImportConstants.cs:7-8` |
| Volume / titre (correction) | Volume ≥ 0 (la borne n'est pas bloquante à la frappe) ; titre 300 caractères | — | Oui (idem) | `Web/Components/Pages/FeedImportsPage.razor:104-107`, `Application/FeedImports/Manage/CorrectFeedImportDecisionCommandHandler.cs:21-22` |
| Actions | Une seule à la fois (boutons grisés) | « Decision updated. » / raison du refus, ou « Unable to update the decision. » | Concurrence détectée en base (BR-DB-004) | `Web/Components/Pages/FeedImportsPage.razor.cs:229-257` |
| Suppression | Confirmation avec rappel de ce qui est conservé ; la suppression de la décision ne touche pas le livre déjà importé | « … decision(s) deleted. » ou « … decision(s) deleted, … not deleted (download or import in progress). » ; si aucune n'est supprimée, motif du serveur ; sélection entièrement en téléchargement : « The selected decisions are being downloaded. » | Oui (téléchargement de moins d'une heure ou import en cours) | `Web/Components/Pages/FeedImportsPage.razor.cs:264-320`, `Application/FeedImports/Delete/DeleteFeedImportDecisionCommandHandler.cs:33-36,44-53` |
| Suppression des téléchargées | Confirmation ; les décisions dont l'import tourne ou a échoué sont conservées | « … decision(s) deleted. » ou « … decision(s) deleted, … kept (import running or failed). » | Oui | `Web/Components/Pages/FeedImportsPage.razor.cs:338-366` |
| Téléchargements forcés / relances | Pas de confirmation ; action réversible par une autre action | — | — | `Web/Components/Pages/FeedImportsPage.razor:60-96` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Voir et traiter les décisions | Utilisateur connecté (page protégée), décisions du compte seulement (le serveur compare le propriétaire à chaque action) | `Web/Components/Pages/_Imports.razor:1`, `Web/Services/FeedImportService.cs:38-41`, `Application/FeedImports/Delete/DeleteFeedImportDecisionCommandHandler.cs:27-31`, `Application/FeedImports/Manage/CorrectFeedImportDecisionCommandHandler.cs:27-31` |
| Synchroniser | Utilisateur connecté, si la fonctionnalité est activée ; la synchronisation est globale (non limitée au compte) | `Web/Services/FeedImportService.cs:68-77` |

## Liés

BR-UI-004, BR-UI-054 à BR-UI-062, BR-DB-003, BR-DB-004, BR-DB-009, Q-UI-005, Q-UI-011, INT-009, INT-010.

> Vérificateur 2026-10-17 : (1) étape 9 : l'ancien texte « téléchargées avec succès » était incomplet : sont aussi supprimées les décisions dont la tâche d'import a été retirée de l'écran Import ; (2) étape 8 : « en téléchargement » recouvre un téléchargement de moins d'une heure (au-delà il est jugé interrompu) et, côté serveur seulement, un import encore en cours ; l'écran ne grise que le premier cas ; (3) étapes 4 à 7 : les conditions d'affichage des actions ont été relevées dans le domaine (voir aussi Q-UI-011, désormais renseignée côté code) ; (4) la limite de 200/300 caractères et le volume ≥ 0 sont bien contrôlés par le serveur, contrairement à ce que laissait entendre la mention « limite définie dans le domaine » ; (5) l'arbitrage lance le téléchargement même si la clé Debrid-Link est absente de la configuration : rien n'est lancé et rien n'est signalé (D-UI-032) ; (6) chemins nus `.razor.cs` remplacés. Statut « Imported » : jamais attribué, donc pas de filtre correspondant.

## Commentaires du produit
