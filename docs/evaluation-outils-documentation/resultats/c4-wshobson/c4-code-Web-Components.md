# C4 Code Level : Web/Components (racine)

## 1. Vue d'ensemble

- **Nom** : Web.Components (racine des composants Razor)
- **Description** : coquille HTML de l'application, routeur Blazor avec contrôle d'accès, imports Razor communs. Les pages et les composants partagés se trouvent dans les sous-dossiers, hors de ce périmètre.
- **Emplacement relatif** : `Web/Components/`
- **Langage** : Razor (Blazor Server, rendu interactif), HTML, C#
- **Objectif** : fournir le document HTML racine, la table de routes, et les espaces de noms importés par tous les composants.

## 2. Éléments de code

### 2.1 `App.razor` (`Web/Components/App.razor`, lignes 1 à 29 et au-delà)

- **Description** : document HTML racine rendu par `MapRazorComponents<App>()` (`Program.cs`, ligne 341).
  - `<head>` : préconnexion et polices Google (Roboto, Bangers), feuille MudBlazor (`_content/MudBlazor/MudBlazor.min.css`), feuille de styles de l'application `mcm.Web.styles.css` versionnée avec `AppVersion.Versioned` (ligne 15), icônes et manifeste dans `/favicon/`.
  - `<body>` : fond `#f5f5f5`, `Routes` (routeur), scripts `_framework/blazor.web.js` et `_content/MudBlazor/MudBlazor.min.js`, puis un script inline.
- **Dépendances** : `Web.Infrastructure` (`AppVersion`), `Routes.razor`, MudBlazor, Blazor.

### 2.2 `Routes.razor` (`Web/Components/Routes.razor`, lignes 1 à 10)

```razor
<Router AppAssembly="@typeof(Program).Assembly">
    <Found Context="routeData"> ... </Found>
</Router>
```

- **Description** : routeur Blazor sur l'assemblage `Web`.
  - Routes trouvées : `AuthorizeRouteView` avec `DefaultLayout = MainLayout` ; sans session autorisée, affiche `RedirectToLogin` (ligne 5).
  - `FocusOnNavigate` place le focus sur le `h1` après navigation (accessibilité).
- **Dépendances** : `Web.Components.Layout.MainLayout`, `RedirectToLogin` (dans les composants partagés, hors périmètre).

### 2.3 `_Imports.razor` (`Web/Components/_Imports.razor`, lignes 1 à 19)

- **Description** : `@using` communs à tous les composants : `Ardalis.GuardClauses`, `Microsoft.AspNetCore.Authorization`, `Components.Authorization`, `Components.Forms`, `Components.Routing`, `Components.Web`, `Components.Web.Virtualization`, `Microsoft.JSInterop`, `MudBlazor`, `Web`, `Web.Components`, `Web.Components.Layout`, `Web.Components.Pages.Books`, `Web.Components.Pages.Libraries.Views`, `Web.Components.SharedComponents`, `Web.Models`, et `static RenderMode`.
- **Dépendances** : bibliothèques listées ci-dessus, `Web.Models` (hors périmètre).

## 3. Dépendances

### 3.1 Internes

- `Web/Components/Layout` : `MainLayout` (route par défaut), `NavBar`, `LoginLayout`, `ScannerLayout`.
- `Web/Infrastructure` : `AppVersion`.
- `Web/Program.cs` : `MapRazorComponents<App>()` et `typeof(Program).Assembly`.
- Sous-dossiers `Pages` et `SharedComponents` : cibles des `@using`, hors périmètre.

### 3.2 Externes

- Blazor Server (`Microsoft.AspNetCore.Components.*`), MudBlazor, Google Fonts (`fonts.googleapis.com`, `fonts.gstatic.com`).

## 4. Relations

```mermaid
---
title: Document racine et routage
---
flowchart TB
    APP["App.razor<br/>document HTML"] --> RT["Routes.razor<br/>Router"]
    RT --> AR["AuthorizeRouteView"]
    AR -->|autorisé| ML["MainLayout"]
    AR -->|non autorisé| RL["RedirectToLogin"]
    RT --> FOC["FocusOnNavigate<br/>sur h1"]
    ML --> PAGES["Pages du dossier Pages<br/>(hors périmètre)"]
    IMP["_Imports.razor"] -.->|@using communs| APP
    IMP -.->|@using communs| PAGES
```

## 5. Remarques

- Les styles `mcm.Web.styles.css` (isolation CSS Blazor) sont générés à partir des fichiers `.razor.css`.
- Les polices sont chargées depuis Google Fonts (`App.razor`, lignes 10 à 13) ; `Content-Security-Policy` n'est pas configurée dans ce périmètre.
