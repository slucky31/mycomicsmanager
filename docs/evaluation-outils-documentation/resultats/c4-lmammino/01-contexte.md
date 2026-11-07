# Vue 1 — Diagramme de contexte

**Portée** : le système MyComicsManager vu de l'extérieur : qui l'utilise, de quels systèmes il dépend.

```mermaid
flowchart TB
    user["👤 Lecteur / collectionneur<br/>[Personne]<br/>Catalogue, lit et organise ses BD"]
    admin["👤 Administrateur<br/>[Personne]<br/>Feature toggles, niveaux de logs, dashboard Hangfire"]

    mcm["MyComicsManager<br/>[Système logiciel]<br/>Gère une collection de BD numériques et physiques :<br/>catalogue, import, lecture, statistiques"]

    auth0["Auth0<br/>[Système externe]<br/>Authentification (OpenID Connect)"]
    isbn["Sources de métadonnées ISBN<br/>[Systèmes externes]<br/>BnF, Google Books, OpenLibrary,<br/>Bubble (Algolia), Bedetheque via SerpApi"]
    cloud["Cloudinary<br/>[Système externe]<br/>Hébergement des images de couverture"]
    mini["Miniflux<br/>[Système externe auto-hébergé]<br/>Agrégateur de flux RSS"]
    debrid["Debrid-Link<br/>[Système externe]<br/>Génération de liens de téléchargement"]
    sites["Sites d'articles et hébergeurs de fichiers<br/>[Systèmes externes]<br/>planete-bd.org, zone-ebook.com, 1fichier, rapidgator…"]
    files["Dossier d'import local<br/>[Système de fichiers]<br/>Fichiers déposés par l'utilisateur"]

    user -->|"Consulte, lit, importe (HTTPS)"| mcm
    admin -->|"Administre (HTTPS)"| mcm
    user -.->|"Dépose des CBZ/PDF"| files
    mcm -->|"Authentifie les utilisateurs (OIDC)"| auth0
    mcm -->|"Recherche les métadonnées par ISBN (HTTPS)"| isbn
    mcm -->|"Envoie les couvertures (HTTPS)"| cloud
    mcm -->|"Lit les entrées de flux (API REST)"| mini
    mcm -->|"Débride les liens (API REST)"| debrid
    mcm -->|"Lit les articles, télécharge les fichiers (HTTPS)"| sites
    mcm -->|"Surveille et ingère"| files
```

## Éléments

| Élément | Preuves (observé) |
|---|---|
| Utilisateur / Admin | Pages Blazor (`Web/Components/Pages`), `Admin/FeatureTogglesCard`, `LogLevels`, `HangfireAuthorizationFilter` (rôle `Admin`, ADR-0016) |
| Auth0 | `Web/Configuration/Auth0Configuration.cs`, ADR-0008 |
| Sources ISBN | `Application/ComicInfoSearch/*Service.cs`, `appsettings.json` (BnF, GoogleBooks, OpenLibrary, Bubble, SerpApi) |
| Cloudinary | `Persistence/Services/CloudinaryService.cs`, `Web/Infrastructure/CloudinaryHealthCheck.cs` |
| Miniflux / Debrid-Link / sites | `Web/Infrastructure/MinifluxClient.cs`, `DebridLinkClient.cs`, `ArticlePageFetcher.cs`, `docs/FEED-IMPORT.md`, ADR-0019 |

Inféré : « lecteur » et « administrateur » sont probablement deux casquettes de la même personne (voir questions ouvertes).
