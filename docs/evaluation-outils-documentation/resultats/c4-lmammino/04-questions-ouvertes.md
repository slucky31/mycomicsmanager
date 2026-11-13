# Questions ouvertes

Personne n'étant disponible pour répondre, voici les points que le code ne tranche pas.

## Périmètre et acteurs
1. Y a-t-il réellement plusieurs utilisateurs, ou un seul (ADR-0001 : « mono-utilisateur ») ? L'administrateur est-il une personne distincte ? Le contexte montre deux profils par prudence.
2. D'autres personnes (famille, autres appareils) accèdent-elles à l'application, et par quel chemin réseau (reverse proxy, VPN, exposition publique) ? Aucun proxy n'apparaît dans le dépôt.

## Topologie d'exécution
3. Comment le conteneur est-il déployé sur le Raspberry Pi (docker compose, Watchtower, autre) ? Aucun descripteur de déploiement n'est dans le dépôt, seulement l'image publiée sur `ghcr.io`.
4. Miniflux est-il sur le même hôte/réseau Docker (`http://miniflux:8080`) et fait-il partie du système ou d'une plateforme voisine ?
5. Staging et production partagent une instance PostgreSQL : les dossiers de fichiers et d'import sont-ils aussi partagés ou séparés ?

## Responsabilités internes
6. Répartition entre `FileWatcherService` (polling) et Hangfire : l'ADR-0016 la signale comme question ouverte. Faut-il unifier ?
7. Pourquoi les services de recherche ISBN (HTTP) sont-ils dans `Application` et les clients Miniflux/Debrid-Link dans `Web/Infrastructure`, plutôt que dans une couche d'infrastructure unique ? Choix voulu ou dette ?
8. Faut-il une vue de composants dédiée au module `FeedImports` (analyse par site, regroupement de liens, arbitrage) ?

## Sécurité et conformité
9. Le README publie une clé de recherche Bubble, présentée comme publique en lecture seule : choix assumé ?
10. Les hôtes autorisés (`AllowedSourceHosts`, `AllowedDownloadHosts`) désignent des sites de téléchargement : le cadre d'usage doit-il être précisé dans l'ADR-0019 ?

## Prochaines étapes suggérées
- Ajouter une vue de déploiement (hôte, volumes, réseau) une fois la question 3 éclaircie.
- Ajouter une vue dynamique du pipeline d'import (dépôt du fichier → job → CBZ → bibliothèque).
- Relier ces pages aux ADR existants (`docs/adr/`).
