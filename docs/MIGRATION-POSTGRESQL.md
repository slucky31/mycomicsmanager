# Migration Neon → PostgreSQL auto-hébergé

Procédure pour créer les environnements staging/prod sur l'instance PostgreSQL auto-hébergée et basculer la prod depuis Neon.

## 0. Pré-requis

- Accès superuser (`postgres`) à l'instance self-hosted
- La chaîne de connexion Neon actuelle (source du dump)
- PostgreSQL tournant en Docker : l'image officielle embarque déjà `psql`, `pg_dump` et `pg_restore`, pas besoin de les installer sur l'hôte — toutes les commandes ci-dessous passent par `docker exec`

```bash
docker ps   # repère le nom/l'ID du conteneur Postgres, ex: mycomicsmanager-postgres-1
```

## 1. Créer le rôle applicatif + les 2 bases (self-hosted)

Connecte-toi en superuser :

```bash
docker exec -it <container> psql -U postgres
```

puis exécute :

```sql
CREATE ROLE mycomicsmanager WITH LOGIN PASSWORD '<mot-de-passe-fort>';
CREATE DATABASE mycomicsmanager_staging OWNER mycomicsmanager;
CREATE DATABASE mycomicsmanager_prod    OWNER mycomicsmanager;
```

`pg_trgm` (utilisé par une migration existante) est une extension "trusted" depuis PG13 : le propriétaire de la base peut la créer sans être superuser, pas de permission spéciale à donner.

## 2. Dump de la prod Neon

Le conteneur a accès à Internet, donc `pg_dump` s'exécute directement dedans — pas besoin de récupérer le dump sur l'hôte, la restauration (étape 3) se fait sur `localhost` dans ce même conteneur :

```bash
docker exec <container> pg_dump "postgresql://<user>:<pass>@<neon-host>/<db>?sslmode=require" \
  --format=custom --no-owner --no-privileges \
  --file=/tmp/mycomicsmanager_prod.dump
```

- `--format=custom` : permet un restore parallélisable et plus robuste qu'un simple `.sql`.
- `--no-owner --no-privileges` : on jette les rôles/ACL Neon, `mycomicsmanager` deviendra propriétaire de tout ce qu'il restaure lui-même.

## 3. Restore dans la base prod self-hosted

Restaure **en tant que rôle `mycomicsmanager`** (pas superuser), pour qu'il devienne propriétaire des objets. Comme la cible est le conteneur lui-même, `localhost` suffit :

```bash
docker exec <container> pg_restore --no-owner --no-privileges \
  --dbname="postgresql://mycomicsmanager:<mot-de-passe-fort>@localhost:5432/mycomicsmanager_prod" \
  /tmp/mycomicsmanager_prod.dump
```

Le dump contient déjà `CREATE EXTENSION IF NOT EXISTS pg_trgm` et la table `__EFMigrationsHistory` — l'historique de migrations restauré permettra à l'auto-migrate au démarrage de l'app de ne rien rejouer d'inutile.

## 4. Staging

Deux options, à ton choix :

- **Vide** (recommandé, plus simple) : ne rien faire ici. Au premier démarrage de l'app sur `mycomicsmanager_staging`, `Database.Migrate()` crée le schéma à partir de zéro.
- **Copie de la prod** (pour tester avec des données réalistes) : rejoue le même restore que l'étape 3 en ciblant `mycomicsmanager_staging` à la place.

## 5. Basculer la config de l'app

Dans le `docker-compose` de déploiement, pour chaque service :

```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Production   # ou Staging
  ConnectionStrings__DefaultConnection: "Host=<host>;Database=mycomicsmanager_prod;Username=mycomicsmanager;Password=<mot-de-passe-fort>"
```

Redéploie. L'app applique les migrations en attente au démarrage (`Web/Program.cs`) puis démarre normalement.

## 6. Vérifier avant de couper Neon

- `curl http://<host>:8080/health` → doit être vert.
- Compare un count rapide entre Neon et le self-hosted pour être sûr que rien n'a été perdu :

  ```sql
  SELECT count(*) FROM "Books";
  ```

  sur les deux instances, doit matcher.
- Garde Neon quelques jours en lecture seule avant de le résilier, au cas où.

## Fenêtre de coupure

Une seule fenêtre de coupure à prévoir : entre la fin du dump (étape 2) et le redémarrage de l'app sur la nouvelle base (étape 5), tout write fait sur Neon dans l'intervalle sera perdu. Pour une appli perso mono-utilisateur, un dump juste avant de couper l'app suffit largement — pas besoin de réplication logique pour ça.
