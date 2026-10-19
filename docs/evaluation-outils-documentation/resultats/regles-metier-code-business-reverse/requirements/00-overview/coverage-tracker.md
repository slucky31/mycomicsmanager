# Suivi de couverture

`/re-coverage` actualise ce fichier. Chaque commande met à jour sa propre ligne en terminant.

**Cycle de statut d'un composant :** Non démarré → En cours → Extrait → Vérifié → En revue → Confirmé

## Composants

| Composant | Dossier | Points d'entrée (trouvés / faits) | Règles | Élevée / Moyenne / Faible | Q. ouvertes | Anomalies | Statut | Dernière mise à jour |
|---|---|---|---|---|---|---|---|---|
| Base de données et modèle de domaine | `database` | 12 tables / 12 | 14 | 10 / 4 / 0 | 9 | 7 | Extrait | 2026-10-17 |
| Livres | `livres` | 14 handlers + 2 endpoints + 5 pages / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Bibliothèques | `bibliotheques` | 6 handlers + 4 pages / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Import de fichiers | `import-fichiers` | 6 handlers + 1 service hébergé / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Import par flux | `import-flux` | 9 handlers + 3 jobs / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Recherche de métadonnées | `recherche-metadonnees` | 5 services / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Statistiques | `statistiques` | 1 handler + 1 page / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Utilisateurs et authentification | `utilisateurs-auth` | 1 handler + 2 endpoints / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Paramètres et administration | `parametres-admin` | 1 handler + 3 pages / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Stockage de fichiers et services image | `stockage-images` | ~12 services / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Exploitation | `exploitation` | 2 endpoints techniques / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Scripts utilitaires | `scripts` | 3 programmes / 0 | 0 | 0 / 0 / 0 | 0 | 0 | Non démarré | 2026-10-17 |
| Interface web | `interface-web` | 19 écrans / 19 | 66 | 56 / 10 / 0 | 11 | 6 | Extrait | 2026-10-17 |

## Services web

Sans objet : pas de couche de services web distincte (2 endpoints API rattachés à **Livres**, 2 endpoints de connexion rattachés à **Utilisateurs et authentification**).

| Domaine métier | Endpoints (trouvés / faits) | Transit | Orchestration | Logique métier | Requête données | Inutilisés | Règles | Statut |
|---|---|---|---|---|---|---|---|---|

## Traitements batch

Sans objet : pas d'application batch ; les tâches de fond sont dans les composants **Import de fichiers** et **Import par flux**.

| Ordre | ID | Traitement | Sorties (tables / fichiers) | Règles | Élevée / Moyenne / Faible | Q. ouvertes | Anomalies | Statut | Dernière mise à jour |
|---|---|---|---|---|---|---|---|---|---|

## Processus

| ID | Processus | Composants | Étapes | Écarts | Statut | Dernière mise à jour |
|---|---|---|---|---|---|---|

## Avancement de la revue (règles)

| Brouillon | En revue | Confirmé | Modifié | Rejeté | Obsolète |
|---|---|---|---|---|---|
| 0 | 0 | 0 | 0 | 0 | 0 |
