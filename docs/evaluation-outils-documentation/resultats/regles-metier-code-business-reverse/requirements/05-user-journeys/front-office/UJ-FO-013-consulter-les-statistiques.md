# UJ-FO-013 — Consulter les statistiques

**Statut :** Brouillon · **Application :** Front-office · **Dernière extraction :** 2026-10-17

## Acteur et objectif

- **Rôle(s) :** Utilisateur connecté.
- **Objectif :** Voir le volume de sa collection, la part lue et le rythme de lecture, pour l'ensemble de ses bibliothèques ou pour une seule.
- **Entrée :** Entrée « Statistics » de la navigation, route `/statistics` (`Web/Components/Pages/Statistics.razor:1`).

## Étapes

| # | Écran | Action de l'utilisateur | Réponse du système | Validations / règles | Appel arrière | Source |
|---|---|---|---|---|---|---|
| 1 | Statistiques | Ouvre l'écran | Statistiques de toutes les bibliothèques : nombre de livres, lus / non lus, pourcentage lu (arrondi à l'entier inférieur, pour qu'un seul livre non lu n'affiche jamais 100 %), indicateurs (séries, lectures, note moyenne, pages ; nombre de bibliothèques seulement pour « toutes » ; stockage numérique seulement s'il est supérieur à 0), lectures par mois sur les 12 derniers mois dont le mois en cours (mois vides à 0) | BR-UI-063 | Statistiques (STA) : `Application/Statistics/Get/GetStatisticsQueryHandler.cs:35-59` | `Web/Components/Pages/Statistics.razor.cs:25-48`, `Web/Components/Pages/Statistics.razor:22-50`, `Web/Models/StatisticsViewModel.cs:19-58` |
| 2 | Idem | Choisit une bibliothèque | Statistiques de cette bibliothèque (le serveur refuse une bibliothèque qui n'est pas à l'utilisateur) ; un changement annule le chargement précédent | BR-UI-063 | idem | `Web/Components/Pages/Statistics.razor.cs:31-35,50-90`, `Application/Statistics/Get/GetStatisticsQueryHandler.cs:26-33` |
| 3 | Idem | (aucun livre) | « 0 books » et « No books yet. Add books to a library to see your statistics. » ; les indicateurs et le graphique restent affichés | BR-UI-063 | — | `Web/Components/Pages/Statistics.razor:29-33,36-81` |

## Validations montrées à l'utilisateur

| Champ / action | Règle | Message affiché | Aussi contrôlée côté serveur ? | Source |
|---|---|---|---|---|
| Chargement des bibliothèques | Échec signalé (200 bibliothèques au plus) ; les statistiques « toutes bibliothèques » sont quand même chargées | « Failed to load libraries » | n/a | `Web/Components/Pages/Statistics.razor.cs:11,38-48` |
| Chargement des statistiques | Échec signalé ; les chiffres précédemment affichés restent à l'écran alors que le sélecteur indique la nouvelle bibliothèque (D-UI-033) | « Failed to load statistics » | n/a | `Web/Components/Pages/Statistics.razor.cs:69-78` |

## Permissions

| Action | Rôles autorisés | Source |
|---|---|---|
| Consulter ses statistiques | Utilisateur connecté ; les chiffres ne portent que sur ses propres bibliothèques | `Web/Components/Pages/_Imports.razor:1`, `Application/Statistics/Get/GetStatisticsQueryHandler.cs:21-33` |

## Liés

BR-UI-063, composant Statistiques (STA).

> Vérificateur 2026-10-17 : étape 3 corrigée (l'écran affiche « 0 books » et conserve indicateurs et graphique) ; périmètre du graphique précisé (12 mois glissants en heure UTC, mois en cours inclus) ; règles d'affichage des indicateurs et arrondi du pourcentage ajoutés ; chemins nus `Web/Components/Pages/Statistics.razor` remplacés par le chemin complet. Point à valider : l'affichage conservé des chiffres périmés après un échec de chargement (D-UI-033).

## Commentaires du produit
