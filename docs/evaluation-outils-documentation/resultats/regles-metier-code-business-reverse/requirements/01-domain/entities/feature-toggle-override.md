# ENT-FeatureToggleOverride — Surcharge de fonctionnalité

**Statut :** Brouillon · **Composant propriétaire :** Paramètres et administration (`parametres-admin`) · **Stocké dans :** `FeatureToggleOverrides`

## Définition

Une **surcharge de fonctionnalité** est la valeur (activée ou non) choisie par un administrateur dans la page Paramètres pour une fonctionnalité activable. Elle remplace la valeur de la configuration (fichiers de paramètres, variables d'environnement) tant qu'elle existe ; la supprimer ramène la fonctionnalité à la configuration.

## Identifiants

| Identifiant | Signification métier | Source |
|---|---|---|
| Fonctionnalité | Une seule surcharge par fonctionnalité | `Persistence/ApplicationDbContext.cs:108` |

## Attributs clés (métier uniquement)

| Attribut (nom métier) | Signification | Valeurs / format autorisés | Colonne / champ | Source |
|---|---|---|---|---|
| Fonctionnalité | Fonction concernée | Une des sept valeurs de la liste ; enregistrée sous forme de texte (64 caractères max) | `Toggle` | `Domain/Settings/FeatureToggleOverride.cs:9`, `Persistence/ApplicationDbContext.cs:107` |
| Activée | Valeur choisie | Vrai / faux | `Enabled` | `Domain/Settings/FeatureToggleOverride.cs:11` |

## Cycle de vie

| État de départ | État d'arrivée | Déclencheur / qui | Règles | Source (où l'état change) |
|---|---|---|---|---|
| Suit la configuration | Surchargée | L'administrateur choisit une valeur différente de la configuration | BR-DB-005 | `Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:42-45` |
| Surchargée | Surchargée (valeur inversée) | L'administrateur choisit l'autre valeur, toujours différente de la configuration | BR-DB-005 | `Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:46-49` |
| Surchargée | Suit la configuration | L'administrateur choisit la valeur de la configuration : la surcharge est supprimée | BR-DB-005 | `Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:38-41` |

L'activation d'une fonctionnalité est refusée si sa configuration obligatoire manque (`Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:25-28`) ; une fonctionnalité inconnue est refusée (`Application/Settings/SetFeatureToggle/SetFeatureToggleCommandHandler.cs:20-23`).

## Relations

Aucune.

## Contraintes et règles résidant en base

- BR-DB-005 : une seule surcharge par fonctionnalité.

## Données de référence

| Fonctionnalité | Configuration d'origine | Rôle | Source |
|---|---|---|---|
| Import par flux | `FeedImport:Enabled` | Synchronisation Miniflux et téléchargements automatiques | `Domain/Settings/FeatureToggle.cs:7` |
| Lecture d'ISBN par OCR | `IsbnOcr:Enabled` | ISBN lu sur les pages des livres importés | `Domain/Settings/FeatureToggle.cs:8` |
| Bédéthèque | `Bedetheque:Enabled` | Recherche Bédéthèque (via SerpApi) | `Domain/Settings/FeatureToggle.cs:9` |
| BnF | `BnfCatalogue:Enabled` | Recherche dans le catalogue de la BnF | `Domain/Settings/FeatureToggle.cs:10` |
| Google Books | `GoogleBooks:Enabled` | Recherche Google Books | `Domain/Settings/FeatureToggle.cs:11` |
| OpenLibrary | `OpenLibrary:Enabled` | Recherche OpenLibrary | `Domain/Settings/FeatureToggle.cs:12` |
| Bubble | `Bubble:Enabled` | Recherche Bubble (clé d'API requise) | `Domain/Settings/FeatureToggle.cs:13` |

Les valeurs sont enregistrées par nom : ne jamais renommer une valeur (`Domain/Settings/FeatureToggle.cs:4`).

## Commentaires du produit
