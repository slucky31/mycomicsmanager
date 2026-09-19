# ADR-0003: PostgreSQL self-hosted (instance unique staging/prod) au lieu de Neon

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

La base de données de production tournait initialement sur **Neon**
(PostgreSQL serverless managé). Le projet a depuis basculé vers une instance
PostgreSQL **auto-hébergée** (conteneur Docker, `pg_main`), colocalisée avec
l'application sur le même hôte — cf. [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md).

Cette décision découle directement du choix d'hébergement : une fois
l'application déployée sur un Raspberry Pi self-hosted plutôt que sur un
service cloud, faire dépendre la production d'un service managé externe
(Neon) n'apportait plus grand-chose et ajoutait une dépendance réseau
externe, une limite de plan gratuit potentielle, et un point de facturation
séparé pour une application mono-utilisateur.

`docs/MIGRATION-POSTGRESQL.md` documente la procédure complète (dump/restore
via `pg_dump`/`pg_restore`, création du rôle applicatif, bascule de la
chaîne de connexion). Cette ADR documente la **décision** ; le runbook
détaillé reste dans ce fichier séparé.

Un point notable de la décision : **une seule instance PostgreSQL
auto-hébergée héberge à la fois `mycomicsmanager_staging` et
`mycomicsmanager_prod`** (deux bases distinctes, même serveur), plutôt que
deux instances séparées. Justification explicite dans le runbook : "appli
perso mono-utilisateur ... pas besoin de réplication logique pour ça."

**Neon reste utilisé pour la CI** : les tests d'intégration
(`Persistence.Integration.Tests`) continuent de cibler une base Neon dédiée
via la variable `ConnectionStrings__NeonConnectionUnitTests`
(`.github/workflows/*.yml`). Ce n'est pas un oubli de migration mais un
choix délibéré — voir Décision ci-dessous.

## Options considérées

- **Option A — PostgreSQL self-hosted, instance unique staging+prod (retenue)**
  - Pour : cohérent avec l'hébergement Raspberry Pi déjà choisi ; coût nul ;
    contrôle total sur la version PostgreSQL et les extensions (`pg_trgm`) ;
    pas de limite de plan gratuit à surveiller.
  - Contre : pas d'isolation forte entre staging et prod (même serveur, même
    ressources CPU/RAM) ; sauvegardes et mises à jour à gérer soi-même ; pas
    de haute disponibilité ni de réplication automatique.
- **Option B — Garder Neon (ou un autre Postgres managé)**
  - Pour : sauvegardes automatiques, scalabilité, pas de maintenance serveur.
  - Contre : dépendance à un service externe et à sa facturation pour une
    app qui tourne déjà sur du matériel self-hosted ; latence réseau
    supplémentaire entre le Raspberry Pi et le service managé ; redondant
    avec l'objectif de coût nul de l'hébergement self-hosted.
- **Option C — Deux instances self-hosted séparées (staging / prod)**
  - Pour : isolation complète entre environnements.
  - Contre : ressources limitées du Raspberry Pi 4 ; complexité de gestion
    (deux conteneurs, deux jeux de sauvegardes) jugée disproportionnée pour
    un usage mono-utilisateur.

## Décision

On héberge PostgreSQL **soi-même**, en conteneur Docker sur le même hôte que
l'application, avec **une seule instance serveur** portant deux bases
distinctes (`mycomicsmanager_staging`, `mycomicsmanager_prod`) et un rôle
applicatif dédié (`mycomicsmanager`) propriétaire des deux.

Les **tests d'intégration en CI continuent d'utiliser Neon** plutôt que
l'instance self-hosted : le runner GitHub Actions n'a pas d'accès réseau au
Raspberry Pi, et une base Neon éphémère (plan gratuit, pas de données
persistantes à protéger) est plus simple à provisionner pour ce contexte
que d'exposer l'instance self-hosted à Internet ou de maintenir une
troisième instance PostgreSQL en conteneur dans le workflow CI. Le
self-hosted ne concerne donc que staging/prod ; Neon reste l'outil pour les
tests.

## Conséquences

### Positives

- Coût d'infrastructure nul, cohérent avec [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md).
- Contrôle total sur la version PostgreSQL et les extensions activées.
- Simplicité opérationnelle : un seul serveur à surveiller et sauvegarder.

### Négatives

- Pas de sauvegarde automatique managée : les sauvegardes (dump régulier)
  sont à la charge du mainteneur — non outillé à ce jour au-delà de la
  procédure manuelle du runbook.
- Staging et prod partagent les ressources CPU/RAM/IO du même serveur : un
  test de charge sur staging peut dégrader la prod.
- Fenêtre de coupure assumée lors de toute migration de données similaire
  (dump → restore → bascule) : perte des écritures faites dans l'intervalle.
  Jugé acceptable pour un usage mono-utilisateur (cf. runbook, section
  "Fenêtre de coupure").
- Deux fournisseurs PostgreSQL différents coexistent désormais dans le
  projet (Neon pour la CI, self-hosted pour staging/prod). C'est assumé
  (contextes différents, contraintes différentes) mais cela veut dire que
  le comportement observé en CI (version PostgreSQL, extensions
  disponibles, latence) peut légèrement différer de celui de la prod — à
  garder en tête en cas de bug qui ne reproduit qu'en prod.

### Couches impactées

- [x] Persistence (connexion, migrations, extensions)
- [ ] Domain
- [ ] Application
- [x] Web (configuration de la chaîne de connexion par environnement)

## Liens

- `docs/MIGRATION-POSTGRESQL.md` — runbook détaillé de la migration
- [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md) — cible de déploiement Raspberry Pi 4
- `CLAUDE.md` — mention de `ConnectionStrings__NeonConnectionUnitTests`
- `Persistence/ApplicationDbContext.cs`
