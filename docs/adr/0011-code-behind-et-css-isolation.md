# ADR-0011: Convention code-behind (`.razor.cs`) + CSS isolation (`.razor.css`)

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Un composant Blazor peut mélanger balisage, logique C# (`@code { ... }`) et
styles dans un seul fichier `.razor`, ou séparer ces préoccupations :
logique dans un fichier `.razor.cs` (partial class), styles dans un
fichier `.razor.css` (CSS scoping automatique de Blazor).

Le projet suit la seconde approche. Sur 35 fichiers `.razor`, 26 ont un
`.razor.cs` associé et 24 un `.razor.css`. Les fichiers sans code-behind
sont presque tous des fichiers d'infrastructure sans logique propre
(`App.razor`, `_Imports.razor`, `Routes.razor`, `LoginLayout.razor`,
`MainLayout.razor`, `ScannerLayout.razor`, `RedirectToLogin.razor`), à une
exception près : `Web/Components/Pages/Statistics.razor` est une page avec
de la logique mais sans code-behind — un écart connu à la convention,
plutôt qu'une remise en cause de la règle elle-même.

Un skill dédié (`blazor-refactor`) est disponible dans l'environnement de
développement, décrit comme "Audits et refactore les composants Blazor pour
imposer les conventions code-behind + CSS isolation" — signe que cette
convention est activement maintenue/rattrapée plutôt qu'acquise une fois
pour toutes.

## Options considérées

- **Option A — Code-behind + CSS isolation (retenue)**
  - Pour : sépare balisage, logique et style, ce qui facilite la lecture
    d'un composant complexe ; la logique en `.razor.cs` bénéficie
    pleinement des outils C# (refactoring, navigation, tests unitaires plus
    simples à cibler) ; le CSS scoping isole les styles par composant,
    évite les fuites de style entre pages.
  - Contre : plus de fichiers à gérer par composant (jusqu'à 3 au lieu
    d'1) ; nécessite une discipline constante pour ne pas glisser de
    logique dans le `.razor` "juste pour un cas simple".
- **Option B — Tout dans le `.razor` (`@code` inline + styles inline ou globaux)**
  - Pour : un seul fichier par composant, plus rapide à créer pour un
    composant trivial.
  - Contre : les composants avec une logique non triviale deviennent
    rapidement difficiles à lire (balisage et C# entremêlés) ; pas de
    scoping CSS automatique sans `.razor.css`, risque de collisions de
    style à l'échelle de l'application.

## Décision

Tout composant Blazor avec de la logique propre doit avoir son code dans un
fichier `.razor.cs` (partial class) et ses styles spécifiques dans un
fichier `.razor.css`. Les fichiers `.razor` sans logique (layouts,
`_Imports.razor`, routage) restent exemptés.

## Conséquences

### Positives

- Lisibilité : le balisage Razor reste concentré sur la structure visuelle,
  la logique est isolée et testable comme du C# classique.
- Le CSS scoping par composant évite les effets de bord visuels entre
  pages.
- Convention vérifiable et corrigeable de façon outillée (skill
  `blazor-refactor`), donc rattrapable si un écart apparaît.

### Négatives

- Écart connu non corrigé à ce jour : `Statistics.razor` contient de la
  logique sans fichier `.razor.cs` dédié.
- Trois fichiers par composant (au lieu d'un) à créer et nommer de façon
  cohérente pour chaque nouveau composant.

### Couches impactées

- [x] Web (tous les composants Razor)
- [ ] Domain
- [ ] Application
- [ ] Persistence

## Liens

- `Web/Components/Pages/Statistics.razor` — écart connu à corriger
- Skill `blazor-refactor` (audit/correction automatisée de la convention)
