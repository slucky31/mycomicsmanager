# Rôles et permissions — Front-office

Matrice rôle × action de l'interface web. L'application ne connaît qu'un rôle explicite (`Admin`) ; tout autre utilisateur connecté est un « Utilisateur ». Les données sont toujours cantonnées au propriétaire : les services passent l'identifiant de l'utilisateur courant aux requêtes et commandes (`Web/Services/BooksService.cs:34-40`). Rôle `Admin` : voir Q-UI-003 (attribution non visible dans le dépôt).

| Action | Visiteur | Utilisateur | Administrateur | Source |
|---|---|---|---|---|
| Voir l'écran de connexion et la page d'erreur | Oui | Oui | Oui | `Web/Components/Pages/Login.razor:1`, `Web/Components/Pages/Error.razor:1` |
| Voir toute autre page | Non (redirigé) | Oui | Oui | `Web/Components/Pages/_Imports.razor:1`, `Web/Components/Routes.razor:3-7` |
| Voir l'état de santé technique (`/health`) | Oui (aucune authentification exigée) | Oui | Oui | `Web/Program.cs:349-352` |
| Créer, modifier, supprimer ses bibliothèques | Non | Oui | Oui | `Web/Components/Pages/Libraries/AddLibrary.razor:3`, `Web/Components/Pages/Libraries/EditLibrary.razor:3`, `Web/Components/Pages/Libraries/LibrariesList.razor.cs:56-79` (UJ-FO-003, UJ-FO-004) ; propriétaire contrôlé côté serveur : `Application/Libraries/Update/UpdateLibraryCommandHandler.cs:20-23`, `Application/Libraries/Delete/DeleteLibraryCommandHandler.cs:20-23` |
| Ajouter, modifier, supprimer, déplacer ses livres | Non | Oui | Oui | UJ-FO-006, UJ-FO-007, UJ-FO-014 |
| Lire, télécharger ses livres numériques | Non | Oui | Oui | `Web/EndPoints/BooksEndpoints.cs:17-18,29-36,74-81` |
| Importer des fichiers, suivre et effacer ses imports | Non | Oui | Oui | UJ-FO-011 |
| Traiter les décisions d'import par flux | Non | Oui | Oui | `Web/Services/FeedImportService.cs:38-44,85-92` |
| Lancer la synchronisation du flux | Non | Oui, si la fonctionnalité est activée | Oui, si activée | `Web/Services/FeedImportService.cs:29,68-77` |
| Voir ses statistiques | Non | Oui | Oui | UJ-FO-013 |
| Voir compte, santé du système, version | Non | Oui | Oui | `Web/Components/Pages/UserSettings.razor:29-72` |
| Activer / couper une fonctionnalité | Non | Non (non affiché ; non contrôlé côté serveur, D-UI-001) | Oui | `Web/Components/Pages/UserSettings.razor:74-90`, `Web/Services/FeatureToggleService.cs:23-40` |
| Régler les niveaux de journalisation | Non | Non (page refusée) | Oui | `Web/Components/Pages/Admin/LogLevels.razor:2` |
| Ouvrir le tableau de bord Hangfire | Non | Non (lien absent ; adresse directe refusée par le filtre) | Oui | `Web/Infrastructure/HangfireAuthorizationFilter.cs:10-11`, `Web/Program.cs:336-339`, `Web/Components/Pages/UserSettings.razor:83` |

Vérificateur 2026-10-17 : (1) ligne ajoutée « état de santé technique » : l'adresse `/health` est publiée sans `RequireAuthorization` et renvoie le détail des contrôles (base de données, hébergeur d'images, Debrid-Link, Miniflux) à un visiteur non connecté (`Web/Program.cs:222-229,349-352`) : la matrice annonçait à tort que seules la connexion et l'erreur sont publiques ; voir D-UI-008 (proposée). (2) Ligne « niveaux de journalisation » : pour un utilisateur connecté sans rôle `Admin`, « page refusée » signifie en réalité une redirection vers l'écran de connexion (la zone « non autorisé » du routeur est la même pour un utilisateur connecté, `Web/Components/Routes.razor:3-7`, `Web/Components/SharedComponents/RedirectToLogin.razor:6`) ; il n'y a pas de message de refus. (3) Citations remplacées : dossier `Libraries/` remplacé par fichiers précis ; `Web/Services/FeedImportService.cs:38-41` (simple lecture de l'utilisateur courant) étendu à 38-44 et 85-92 (utilisateur transmis aux requêtes et commandes) ; `Web/Services/FeedImportService.cs:68-72` étendu à 29,68-77 (le drapeau et l'enfilement du travail). (4) Contrôlé et conforme : rôle `Admin` utilisé seulement aux trois endroits listés (`Web/Components/Pages/UserSettings.razor:74`, `Web/Components/Pages/Admin/LogLevels.razor:2`, `Web/Infrastructure/HangfireAuthorizationFilter.cs:11`) ; la commande de changement de fonctionnalité ne vérifie aucun rôle (`Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:17-59`), donc la mention « non contrôlé côté serveur » (D-UI-001) est exacte. Les lignes sans source de code (UJ-FO-006, -007, -011, -013, -014) renvoient à des parcours hors de ce lot et n'ont pas été vérifiées ici. Accessibilité : le lien Hangfire a un `aria-label` « Hangfire (opens in a new tab) » (`Web/Components/Pages/UserSettings.razor:83`).

## Pistes de maker-checker, audit et rapports

Aucun circuit d'approbation, aucune correction manuelle soumise à validation et aucun rapport administrateur n'existent à l'écran. Les changements de fonctionnalités et de niveaux de journalisation sont seulement tracés dans les journaux applicatifs (`Web/Services/FeatureToggleService.cs:31`, `Web/Components/Pages/Admin/LogLevels.razor.cs:37,44`) ; il n'y a pas d'historique consultable. Vérificateur 2026-10-17 : citation `Web/Components/Pages/Admin/LogLevels.razor.cs:36` décalée d'une ligne, corrigée en 37 (et 44 pour la remise à zéro) ; ces journaux ne contiennent pas l'identité de l'administrateur qui a agi (aucun nom d'utilisateur dans les messages).

## Commentaires du produit
