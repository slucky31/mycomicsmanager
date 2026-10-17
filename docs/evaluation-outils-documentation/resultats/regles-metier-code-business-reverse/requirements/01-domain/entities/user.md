# ENT-User — Utilisateur

**Statut :** Brouillon · **Composant propriétaire :** Utilisateurs et authentification (`utilisateurs-auth`) · **Stocké dans :** `Users`

## Définition

Un **utilisateur** est une personne authentifiée par le fournisseur d'identité (Auth0). Son enregistrement local est créé automatiquement à la première connexion et sert de propriétaire à ses bibliothèques et à ses décisions d'import par flux.

## Identifiants

| Identifiant | Signification métier | Source |
|---|---|---|
| Identifiant de l'utilisateur (GUID) | Identité technique locale | `Domain/Users/User.cs:16` |
| Identifiant d'authentification | Identifiant fourni par le fournisseur d'identité (revendication `sub`) ; sert à retrouver l'utilisateur à chaque connexion | `Domain/Users/User.cs:10`, `Web/CustomAuthenticationStateProvider.cs:37-38`, `Web/CustomAuthenticationStateProvider.cs:57` |

## Attributs clés (métier uniquement)

| Attribut (nom métier) | Signification | Valeurs / format autorisés | Colonne / champ | Source |
|---|---|---|---|---|
| Courriel | Adresse fournie par le fournisseur d'identité | Obligatoire pour créer l'utilisateur | `Email` | `Domain/Users/User.cs:8`, `Web/CustomAuthenticationStateProvider.cs:58-59` |
| Identifiant d'authentification | Voir Identifiants | Obligatoire | `AuthId` | `Domain/Users/User.cs:10` |

## Cycle de vie

Pas de statut. L'utilisateur est créé à la première connexion s'il n'existe pas et si un courriel est fourni. La modification `User.Update` (`Domain/Users/User.cs:23-27`) n'a aucun appelant repéré (voir Q-DOM-006).

| État de départ | État d'arrivée | Déclencheur / qui | Règles | Source (où l'état change) |
|---|---|---|---|---|
| (inconnu) | Enregistré | Première connexion d'un utilisateur authentifié disposant d'un courriel | — | `Web/CustomAuthenticationStateProvider.cs:55-65` |
| (inconnu) | Enregistré | Commande de création explicite (refus si le courriel ou l'identifiant existe déjà) | — | `Application/Users/Create/CreateUserCommandHandler.cs:23-39` |

## Relations

| Entité liée | Relation (métier) | Cardinalité |
|---|---|---|
| [ENT-Library](library.md) | Possède des bibliothèques (référence logique, sans clé étrangère en base) | 1 utilisateur → 0..n bibliothèques |
| [ENT-FeedImportDecision](feed-import-decision.md) | Possède des décisions d'import par flux (référence logique) | 1 utilisateur → 0..n décisions |

## Contraintes et règles résidant en base

Aucune contrainte d'unicité sur le courriel ni sur l'identifiant d'authentification : voir D-DOM-003.

## Données de référence

Aucune. Le rôle `Admin` vient des revendications du fournisseur d'identité, pas de la base (`Web/Infrastructure/HangfireAuthorizationFilter.cs:10-11`).

## Commentaires du produit
