# ADR-0008: Auth0 pour l'authentification

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

L'application a besoin d'authentifier ses utilisateurs (même pour un usage
mono-utilisateur, cf. [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md))
sans gérer soi-même le stockage de mots de passe, la récupération de compte,
ou la conformité aux bonnes pratiques de sécurité (hachage, MFA, etc.).

Le projet utilise **Auth0** via le package `Auth0.AspNetCore.Authentication` :
`AddAuth0WebAppAuthentication` dans `Web/Program.cs`, avec les scopes
`openid profile email`, une configuration (`Auth0Configuration`) validée au
démarrage (`ValidateOnStart()`), et un cookie d'authentification avec
expiration glissante de 3 jours.

Comme l'application tourne en Blazor Server (cf. [ADR-0006](0006-blazor-server-plutot-que-webassembly.md)),
le principal ASP.NET Core issu du cookie Auth0 doit être pont vers le
`AuthenticationStateProvider` de Blazor : c'est le rôle de
`Web/CustomAuthenticationStateProvider.cs`.

L'identité Auth0 (claim `sub`) est ensuite résolue vers l'identifiant
interne `User.Id` (Guid) de l'application via `ICurrentUserService` /
`CurrentUserService` (`Web/Services/CurrentUserService.cs`). Cette
résolution est actuellement en transition (fallback par email pour les
utilisateurs pas encore migrés vers un lookup par `sub`) — ce point précis
est une décision **encore ouverte**, suivie séparément dans le backlog
("Migration identité Auth0"). Cette ADR ne couvre que le choix d'Auth0
comme fournisseur d'identité, pas la stratégie de résolution d'identité
interne.

## Options considérées

- **Option A — Auth0 (retenue)**
  - Pour : service géré, pas de stockage de mots de passe côté application ;
    gestion de compte (reset, MFA, providers sociaux) déléguée ; SDK ASP.NET
    Core officiel bien intégré à `Microsoft.AspNetCore.Authentication`.
  - Contre : dépendance à un service externe (disponibilité, latence) ;
    plan gratuit avec limites (nombre d'utilisateurs actifs) à surveiller
    si l'usage devait grandir au-delà du mono-utilisateur.
- **Option B — ASP.NET Core Identity (self-hosted)**
  - Pour : aucune dépendance externe, cohérent avec la philosophie
    self-hosted du reste de l'infra ([ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md), [ADR-0003](0003-postgresql-self-hosted-au-lieu-de-neon.md)).
  - Contre : oblige à gérer soi-même le stockage sécurisé des mots de passe,
    la réinitialisation, et toute évolution future (MFA, providers
    sociaux) — charge de maintenance disproportionnée pour un projet perso.
- **Option C — Autre IdP managé (Okta, Azure AD B2C) ou self-hosted (Keycloak)**
  - Pour : fonctionnalités similaires à Auth0.
  - Contre : Keycloak ajouterait un service de plus à héberger et
    maintenir sur le Raspberry Pi ; Okta/Azure AD B2C n'apportent pas
    d'avantage identifié par rapport à Auth0 pour ce projet.

## Décision

On utilise **Auth0** comme fournisseur d'identité, avec authentification par
cookie côté serveur et un pont (`CustomAuthenticationStateProvider`) vers
l'`AuthenticationStateProvider` de Blazor Server.

## Conséquences

### Positives

- Aucune gestion de mot de passe côté application : réduit la surface
  d'attaque et la charge de maintenance liée à la sécurité de
  l'authentification.
- Fonctionnalités de gestion de compte (reset de mot de passe, etc.)
  disponibles sans développement supplémentaire.

### Négatives

- Dépendance à la disponibilité d'Auth0 : une panne du service empêche
  toute connexion, même si l'application elle-même tourne normalement.
- Plan gratuit Auth0 à surveiller si le nombre d'utilisateurs devait
  augmenter (actuellement non contraignant, usage mono-utilisateur).
- La résolution de l'identité interne à partir du principal Auth0 reste en
  transition (fallback email → `sub`), ce qui introduit une duplication de
  logique constatée entre `CurrentUserService.cs` et
  `Web/EndPoints/BooksEndpoints.cs` — suivi comme décision ouverte séparée.

### Couches impactées

- [x] Web (`Program.cs`, `CustomAuthenticationStateProvider`, `CurrentUserService`)
- [x] Application (`ICurrentUserService` — abstraction consommée par les handlers)
- [ ] Domain
- [ ] Persistence

## Liens

- `Web/Program.cs` (`AddAuth0WebAppAuthentication`)
- `Web/CustomAuthenticationStateProvider.cs`
- `Application/Interfaces/ICurrentUserService.cs`, `Web/Services/CurrentUserService.cs`
- Backlog (ouverte) : "Migration identité Auth0 : passage complet du fallback email → `sub`/`AuthId`"
