# UJ-FO-015 — Administrer l'application

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Administrateur (rôle `Admin`) ; la partie « compte, santé, version » est ouverte à tout utilisateur connecté.
- **Objectif :** Activer ou couper des fonctionnalités sans redéploiement, régler le niveau de détail des journaux, accéder au tableau de bord des tâches de fond, vérifier la santé du système.
- **Entrée :** Entrée « Settings » de la navigation, route `/settings` (`Web/Components/Pages/UserSettings.razor:1`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Paramètres | Ouvre l'écran | Compte (nom, courriel, photo), santé du système, version, « Sign out » ; section « Administration » pour les administrateurs seulement | BR-UI-002, BR-UI-064 | Contrôles de santé (OPS) | `Web/Components/Pages/UserSettings.razor:29-90` |
| 2 | Idem | « System health » | Détail des contrôles ; « Health status unavailable » si aucun contrôle n'est disponible ; échec de lecture : « Failed to load system health » | BR-UI-064 | idem | `Web/Components/Pages/UserSettings.razor:30-62`, `Web/Components/Pages/UserSettings.razor.cs:55-71` |
| 3 | Idem (administrateur) | Interrupteur d'une fonctionnalité parmi les 7 de la carte : import par flux, lecture d'ISBN par OCR, recherche Bedetheque, BnF, Google Books, OpenLibrary, Bubble | Changement immédiat et durable ; « <fonction> turned on/off. » ; l'activation est refusée si la configuration est incomplète (import par flux, OCR, Bubble seulement ; la désactivation n'est jamais refusée) ; l'interrupteur revient à son état réel en cas d'échec ; pour l'import par flux, la tâche récurrente est ajoutée ou retirée aussitôt | BR-UI-065 | Surcharge de fonctionnalité (SET) : `Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs` | `Web/Components/Pages/Admin/FeatureTogglesCard.razor.cs:21-49`, `Web/Models/FeatureToggleViewModel.cs:24-34`, `Application/Settings/FeatureToggleDefaults.cs:17-58`, `Web/Services/FeatureToggleService.cs:34-37` |
| 4 | Idem (administrateur) | « Log levels » | Page de réglage des journaux (`/admin/log-levels`, rôle `Admin` exigé par la page elle-même) | BR-UI-002 | — | `Web/Components/Pages/UserSettings.razor:76-81`, `Web/Components/Pages/Admin/LogLevels.razor:1-2` |
| 5 | Niveaux de journalisation | Filtre les composants ; choisit un niveau par composant (Debug, Info, Warning, Error, None), « inherit » (retour au niveau du parent, sauf pour la racine), ou « Reset » | Effet immédiat jusqu'au redémarrage, en mémoire du serveur ; « Log levels reset to the configured values » (retour aux niveaux de la configuration) ; aucun message pour un réglage isolé | BR-UI-066 | Interrupteurs de niveaux (OPS) | `Web/Components/Pages/Admin/LogLevels.razor.cs:12-19,34-47`, `Web/Components/Pages/Admin/LogLevels.razor:14-16,49-59`, `Web/Infrastructure/LogLevelSwitches.cs:117-144` |
| 6 | Paramètres (administrateur) | « Hangfire » | Ouvre le tableau de bord des tâches de fond dans un nouvel onglet ; le serveur n'y laisse entrer qu'un utilisateur connecté ayant le rôle `Admin` | BR-UI-002 | `/hangfire` (OPS) | `Web/Components/Pages/UserSettings.razor:83-87`, `Web/Infrastructure/HangfireAuthorizationFilter.cs:7-11`, `Web/Program.cs:336-339` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Fonctionnalité | Activation refusée si la configuration requise manque (import par flux : courriel de l'utilisateur, adresse, clé et catégorie Miniflux ; OCR : chemin de Tesseract ; Bubble : clé d'API) ; valeur identique à la configuration = retrait de la surcharge ; aucun contrôle pour Bedetheque, BnF, Google Books, OpenLibrary (Q-UI-031) | Message de la règle (nom de la clé manquante), à défaut « Unable to change the feature. » | Oui (commande) ; rôle non contrôlé (D-UI-001) | `Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:25-48` |
| Fonctionnalité | Un seul changement à la fois (interrupteurs grisés) | — | — | `Web/Components/Pages/Admin/FeatureTogglesCard.razor.cs:21-29` |
| Journalisation | Pas de confirmation (effet limité au redémarrage) ; « Reset » remet tous les composants à la configuration | « Log levels reset to the configured values » | — | `Web/Components/Pages/Admin/LogLevels.razor.cs:34-39` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Voir compte, santé, version, se déconnecter | Utilisateur connecté (page protégée) | `Web/Components/Pages/_Imports.razor:1`, `Web/Components/Pages/UserSettings.razor:29-72` |
| Fonctionnalités (carte masquée aux autres rôles, mais commande serveur sans contrôle de rôle : D-UI-001), niveaux de journalisation (page `[Authorize(Roles = "Admin")]`), tableau de bord Hangfire (filtre serveur : connecté et rôle `Admin`) | `Admin` | `Web/Components/Pages/UserSettings.razor:74`, `Web/Components/Pages/Admin/LogLevels.razor:2`, `Web/Infrastructure/HangfireAuthorizationFilter.cs:10-11`, `Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:17-59` |

## Liés

BR-UI-002, BR-UI-064 à BR-UI-066, D-UI-001, Q-UI-002, Q-UI-003, BR-DB-005.

> Vérificateur 2026-10-17 : (1) rôle `Admin` : les trois protections (affichage de la section, page des journaux, tableau de bord Hangfire) testent bien le rôle `Admin` du jeton de l'utilisateur, mais aucun code du dépôt ne lui attribue ce rôle (portée de connexion « openid profile email » seulement, aucune correspondance de revendication) : l'existence effective de cette revendication dépend de la configuration du fournisseur d'identité, hors dépôt (Q-UI-030) ; la mention « vérifié par le serveur » pour Hangfire est exacte pour le contrôle lui-même ; (2) étape 3 : la carte des fonctionnalités compte 7 interrupteurs (liste ajoutée) et le refus pour configuration incomplète ne concerne que 3 d'entre eux (Q-UI-031) ; (3) étape 5 : niveaux proposés Debug, Info, Warning, Error, None (pas Verbose ni Fatal), « inherit » absent pour la racine ; (4) étape 2 : source corrigée (30-62 et non 25-60). Le tableau de bord Hangfire n'est pas en lecture seule (pas d'option correspondante dans sa configuration : D-UI-035).

## Commentaires du produit
