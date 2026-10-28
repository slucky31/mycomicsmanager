# UJ-FO-011 — Importer des fichiers dans une bibliothèque numérique

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté.
- **Objectif :** Verser des archives de bandes dessinées dans une bibliothèque numérique et suivre leur traitement.
- **Entrée :** Entrée « Import » de la navigation, ou « Import » du menu d'une bibliothèque numérique ; route `/import?libraryId=…` (`Web/Components/Pages/Import.razor:3`, `Web/Components/Pages/Libraries/LibraryDetailPage.razor:144`, entrée du menu « More actions » proposé aux bibliothèques numériques seulement, `Web/Components/Pages/Libraries/LibraryDetailPage.razor:38-41`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Import | Ouvre l'écran | Liste des bibliothèques numériques, présélection, imports récents de la bibliothèque ; sans bibliothèque numérique : invitation à en créer une (« No digital library yet. Create one to import comic files. ») ; échec de chargement : « Unable to load the libraries. » | BR-UI-048 | Bibliothèques (LIB), imports : `Web/Services/ImportService.cs` (IMP) | `Web/Components/Pages/Import.razor.cs:35-78`, `Web/Components/Pages/Import.razor:18-28` |
| 2 | Idem | Change de bibliothèque | Imports de la bibliothèque choisie ; suivi remis à zéro | BR-UI-048 | idem | `Web/Components/Pages/Import.razor.cs:117-122` |
| 3 | Idem | « Choose files » (jusqu'à 10 fichiers CBZ, CBR, ZIP, RAR, PDF) | Envoi fichier par fichier ; chaque fichier devient un import ; erreurs listées par fichier ; « n file(s) uploaded successfully » seulement si aucun fichier n'a échoué (n = nombre de fichiers choisis, y compris ceux non traités si la bibliothèque a changé en cours d'envoi) | BR-UI-049, BR-UI-050 | Création d'un import (IMP) : `Web/Services/ImportService.cs:80-120` | `Web/Components/Pages/Import.razor.cs:124-170` |
| 4 | Idem | Attend | Les imports se rafraîchissent toutes les 3 s tant qu'un import n'est pas terminé ; un import terminé propose le livre créé | BR-UI-051 | Liste des imports (IMP) | `Web/Components/Pages/Import.razor.cs:172-226` |
| 5 | Idem | « Clear finished imports » ou supprime un import terminé | Imports terminés ou échoués retirés, sans confirmation ; action groupée : « n job(s) deleted » ou « Unable to delete n job(s) » ; action grisée s'il n'y a aucun import terminé | BR-UI-052 | Suppression d'import (IMP) | `Web/Components/Pages/Import.razor.cs:235-288` |
| 6 | Idem | Déclare un import bloqué en échec (action proposée seulement pour un import ni terminé ni en attente « Pending ») | « Import marked as failed. » ; liste rechargée ; sans confirmation ; refus : « Unable to force the job to fail » ou motif du serveur | BR-UI-053 | Échec forcé (IMP) : `Application/ImportJobs/ForceFail/` | `Web/Components/Pages/Import.razor.cs:290-308`, `Web/Components/SharedComponents/ImportJobCard.razor.cs:18` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Fichiers | Extensions .cbz .cbr .zip .rar .pdf ; 10 fichiers au plus | « <fichier> : Verify the import request parameters. » pour une extension refusée | Extensions : oui (liste configurée, puis liste fixe identique dans la commande de création). Nombre de fichiers : non retrouvé côté serveur (limite du sélecteur seulement) | `Web/Components/Pages/Import.razor:34-39`, `Web/Services/ImportService.cs:79-84`, `Application/ImportJobs/Create/CreateImportJobCommandHandler.cs:15-16,29-34`, `Domain/ImportJobs/ImportJobError.cs:7` |
| Fichier vide, bibliothèque non numérique ou d'un autre utilisateur | Refus de créer l'import | « <fichier> : » suivi du motif du serveur | Oui | `Application/ImportJobs/Create/CreateImportJobCommandHandler.cs:21-27,36-52` |
| Taille | Plafond configuré (1 024 Mo dans le dépôt, 500 Mo si la configuration est absente) non annoncé à l'écran | Aucun motif listé : le dépassement lève une exception de lecture, non interceptée par l'écran (voir D-UI-030) | Oui (limite de lecture du flux) | `Web/Services/ImportService.cs:102-109`, `Application/ImportJobs/ImportSettings.cs:10`, `Web/appsettings.json:72` |
| Envoi | Bouton désactivé pendant l'envoi ou sans bibliothèque | « Uploading… » | — | `Web/Components/Pages/Import.razor:37,44` |
| Un même fichier déjà en cours | Un seul import non terminé par chemin de fichier stocké. Pour un envoi depuis l'écran, le fichier est enregistré sous un nom préfixé d'un identifiant unique : le refus ne se produit donc pas, et le même fichier peut être envoyé plusieurs fois (D-UI-031) | « An active import job already exists for this file. » en cas de doublon de chemin (`Domain/ImportJobs/ImportJobError.cs:15`) | Oui (BR-DB-001) | `Persistence/ApplicationDbContext.cs:87-90`, `Web/Services/ImportService.cs:99-100`, `Application/ImportJobs/Create/CreateImportJobCommandHandler.cs:54-58` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Importer, suivre, effacer, forcer l'échec | Utilisateur connecté (page protégée), dans ses bibliothèques numériques ; effacer et forcer l'échec : seulement ses propres imports (propriétaire de la bibliothèque) | `Web/Components/Pages/_Imports.razor:1`, `Application/ImportJobs/Create/CreateImportJobCommandHandler.cs:36-52`, `Application/ImportJobs/Delete/DeleteImportJobCommandHandler.cs:25-34`, `Application/ImportJobs/ForceFail/ForceFailImportJobCommandHandler.cs:25-29` |

## Liés

BR-UI-048 à BR-UI-053, BR-DB-001, Q-UI-004, Q-UI-009, Q-UI-010, UJ-FO-010.

> Vérificateur 2026-10-17 : (1) étape 3 : le message de succès n'apparaît que si aucun fichier n'a échoué ; une extension refusée donne le motif générique « Verify the import request parameters. » ; (2) étape 6 : l'action n'est offerte que pour un import ni terminé ni « Pending » (BR-UI-053 écrit « Waiting », qui n'existe pas dans l'énumération des statuts : à corriger dans la règle) ; le serveur refuse si la transition de statut est invalide ; (3) taille : l'ancienne mention « motif du refus » était inexacte (exception non interceptée, D-UI-030) ; le plafond par défaut du code est 500 Mo, 1 024 Mo dans la configuration du dépôt ; (4) unicité : sans effet pour un envoi depuis l'écran (chemin stocké unique), elle ne protège que le dépôt par dossier surveillé (D-UI-031) ; (5) 10 fichiers : limite appliquée par le sélecteur seulement ; (6) chemins nus remplacés par les chemins complets ; permissions : références corrigées (l'ancienne source ne concernait que « forcer l'échec »). Les imports sont listés du plus récent au plus ancien. La liste des bibliothèques est limitée aux 200 premières (Q-UI-009).

## Commentaires du produit
