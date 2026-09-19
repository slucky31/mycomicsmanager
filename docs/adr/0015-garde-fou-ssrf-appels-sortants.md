# ADR-0015: Garde-fou SSRF (allow-list) sur tous les appels HTTP sortants

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

L'application effectue des appels HTTP sortants vers plusieurs services
externes pour rechercher des métadonnées de livres (OpenLibrary, Google
Books, SerpApi comme proxy pour Bedetheque) et pour héberger les images de
couverture (Cloudinary). Ces appels sortants sont une surface d'attaque
classique de type **SSRF (Server-Side Request Forgery)** : si une URL
utilisée dans un de ces appels peut être influencée par une entrée
utilisateur, un attaquant pourrait potentiellement forcer le serveur à
requêter une ressource interne (ex. métadonnées cloud, service interne non
exposé publiquement).

Le projet corrige ce risque via `Web/Infrastructure/SsrfGuardHandler.cs`,
un `DelegatingHandler` avec une **allow-list d'hôtes**, appliqué de façon
**uniforme** à tous les `HttpClient` externes configurés dans
`Web/Program.cs` (OpenLibrary, Google Books, Bedetheque, SerpApi,
Cloudinary). Chaque client est en plus configuré avec `AllowAutoRedirect =
false` au niveau du socket handler (empêche une redirection HTTP de
rediriger silencieusement vers un hôte hors de l'allow-list) et des
timeouts (15-30s). Ce correctif a été traité comme un point de sécurité
prioritaire (référencé en interne comme `SEC-M4`, marqué résolu).

## Options considérées

- **Option A — `DelegatingHandler` avec allow-list, appliqué à tous les `HttpClient` externes (retenue)**
  - Pour : point de contrôle unique et systématique — impossible d'oublier
    la protection sur un nouveau client HTTP externe si la convention
    (enregistrer le handler pour chaque `HttpClient`) est respectée ;
    combiné à `AllowAutoRedirect = false`, bloque aussi le contournement
    par redirection.
  - Contre : nécessite une discipline pour brancher le handler à chaque
    nouveau `HttpClient` ajouté (rien ne force structurellement son
    ajout — un oubli reste possible lors de l'intégration d'un futur
    fournisseur externe).
- **Option B — Validation d'URL ad hoc dans chaque service consommateur**
  - Pour : pas de dépendance à un handler commun.
  - Contre : logique de validation dupliquée dans chaque service
    (`OpenLibraryService`, `GoogleBooksService`, `BedethequeService`,
    `CloudinaryService`...), risque élevé d'oubli ou d'incohérence entre
    services.
- **Option C — Pas de protection dédiée, confiance dans le fait que les URLs sont toujours contrôlées par le code**
  - Pour : aucun développement supplémentaire.
  - Contre : fragile dès qu'une URL peut un jour être influencée
    (paramètre de requête, configuration utilisateur) ; ne suit pas les
    recommandations OWASP sur le SSRF pour une application qui fait des
    appels sortants vers des services tiers.

## Décision

On applique un `DelegatingHandler` d'allow-list (`SsrfGuardHandler`) à tous
les `HttpClient` configurés pour des appels sortants vers des services
externes, combiné à `AllowAutoRedirect = false` et des timeouts explicites
sur chaque client.

## Conséquences

### Positives

- Protection systématique contre le SSRF sur l'ensemble des intégrations
  externes actuelles (OpenLibrary, Google Books, Bedetheque via SerpApi,
  Cloudinary).
- Les redirections HTTP ne peuvent pas être utilisées pour contourner
  l'allow-list.
- Les timeouts explicites limitent aussi le risque d'un appel externe qui
  bloquerait une requête utilisateur indéfiniment.

### Négatives

- Toute nouvelle intégration externe doit explicitement enregistrer son
  `HttpClient` avec le même handler — ce n'est pas automatique au niveau du
  framework, donc à vérifier en revue de code à chaque ajout de service
  externe.
- L'allow-list doit être maintenue à jour à chaque changement d'hôte chez
  un fournisseur externe (ex. changement de domaine chez Cloudinary).

### Couches impactées

- [x] Web (`SsrfGuardHandler`, configuration des `HttpClient` dans `Program.cs`)
- [ ] Domain
- [ ] Application
- [ ] Persistence

## Liens

- `Web/Infrastructure/SsrfGuardHandler.cs`
- `Web/Program.cs` (enregistrement des `HttpClient` OpenLibrary, Google Books, Bedetheque, SerpApi, Cloudinary)
- `Application/ComicInfoSearch/*Service.cs` — services consommant ces clients
