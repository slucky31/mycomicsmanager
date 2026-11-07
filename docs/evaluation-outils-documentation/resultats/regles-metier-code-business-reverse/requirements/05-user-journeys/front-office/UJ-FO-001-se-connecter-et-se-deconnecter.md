# UJ-FO-001 — Se connecter et se déconnecter

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Visiteur non connecté, puis tout utilisateur connecté.
- **Objectif :** Accéder à ses bibliothèques en s'identifiant, et pouvoir se déconnecter.
- **Entrée :** Toute adresse de l'application (redirection) ou `/login` (`Web/Components/Pages/Login.razor:3`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Toute page protégée | Demande une page sans être connecté | Redirection vers l'écran de connexion (l'adresse demandée n'est pas retenue : après identification, l'utilisateur arrive sur la liste des bibliothèques) | BR-UI-001 | — | `Web/Components/Routes.razor:3-7`, `Web/Components/SharedComponents/RedirectToLogin.razor:4-7` |
| 2 | Connexion | Clique sur « Login » | Envoi vers le service d'identité externe (Auth0) ; la session est persistante (cookie valable 7 jours, prolongé à chaque usage) | BR-UI-003 | `GET /Account/Login` (USR) | `Web/Components/Pages/Login.razor:15-21`, `Web/EndPoints/AccountsEndpoints.cs:13-23`, `Web/Program.cs:191-202` |
| 3 | Liste des bibliothèques | (retour après identification) | La liste de ses bibliothèques s'affiche ; le compte est créé à la première connexion, seulement si le service d'identité fournit une adresse e-mail et qu'aucun compte ne porte déjà cet identifiant d'authentification | BR-UI-003 | Création du compte (USR) | `Web/CustomAuthenticationStateProvider.cs:36-65` |
| 4 | Paramètres | Clique sur « Sign out » | Déconnexion du service d'identité et du cookie de session, puis retour à l'adresse racine (qui renvoie à l'écran de connexion) | BR-UI-064 | `GET /Account/Logout` (USR) | `Web/Components/Pages/UserSettings.razor:68-71`, `Web/EndPoints/AccountsEndpoints.cs:25-35` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Toute page | Connexion exigée sauf connexion et erreur | (redirection) | Oui, pour les deux adresses d'API de livres (`RequireAuthorization`, puis propriétaire du livre contrôlé) ; l'état de santé `/health` et les adresses de connexion/déconnexion restent publics | `Web/EndPoints/BooksEndpoints.cs:17-18`, `Web/Program.cs:349-352` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Voir l'écran de connexion et la page d'erreur | Tous | `Web/Components/Pages/Login.razor:1`, `Web/Components/Pages/Error.razor:1` |
| Toute autre page | Utilisateur connecté | `Web/Components/Pages/_Imports.razor:1` |

## Liés

BR-UI-001, BR-UI-003, BR-UI-064, D-UI-006, Q-UI-003. Composant Utilisateurs et authentification (USR), intégration INT-001 (Auth0).

Vérificateur 2026-10-17 : 4 étapes, 1 validation et 2 permissions relues contre le code. Étape 1 : l'adresse demandée n'est pas conservée (`Web/Components/SharedComponents/RedirectToLogin.razor:6` navigue vers `/login`) et un utilisateur connecté mais sans le rôle exigé arrive aussi sur cet écran (voir roles-and-permissions). Étape 2 : durée de session ajoutée (`Web/Program.cs:197-202`, 7 jours glissants) ; le bouton « Login » est un lien texte, sans titre de niveau 1 sur la page alors que le routeur place le focus sur `h1` (`Web/Components/Routes.razor:8`, `Web/Components/Pages/Login.razor` sans `h1`). Étape 3 : conditions de création du compte précisées (adresse e-mail obligatoire ; sans e-mail, aucun compte n'est créé et les écrans échouent ensuite avec « utilisateur introuvable » ; voir Q-UI-012). Étape 4 : la déconnexion est un simple lien GET (`Web/Components/Pages/UserSettings.razor:68`, `Web/EndPoints/AccountsEndpoints.cs:25`), voir D-UI-010 (proposée). La citation `Web/Components/Pages/Error.razor:1` de la table des permissions est complétée en chemin complet : `Web/Components/Pages/Error.razor:1`.

## Commentaires du produit
