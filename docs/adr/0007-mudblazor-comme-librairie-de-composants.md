# ADR-0007: MudBlazor comme librairie de composants UI

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Une application Blazor a besoin d'une librairie de composants UI (boutons,
formulaires, dialogues, notifications, data grid...) plutôt que de tout
construire en HTML/CSS brut.

Le projet utilise **MudBlazor**, enregistré dans `Web/Program.cs`
(`AddMudServices`, avec une configuration Snackbar personnalisée). C# est
utilisé de bout en bout (pas de composants React/Vue mêlés), et
FluentValidation est intégré aux formulaires MudBlazor via un délégué de
validation par champ (`Web/Validators/BookValidator.cs`,
`ValidateValue`) exposé au paramètre `Validation` de `MudTextField`/`MudForm`.

Aucun fichier de thème MudBlazor personnalisé n'a été trouvé dans le
projet : l'application utilise le thème par défaut de la librairie. Le
`TODO.md` du projet mentionne une "refonte graphique" avec plusieurs pistes
encore en discussion (cartes au survol, vue magazine, liste compacte,
grille façon masonry) — cette question de direction visuelle reste ouverte
et est suivie séparément dans le backlog d'ADR ("Refonte graphique de la
liste de livres").

## Options considérées

- **Option A — MudBlazor (retenue)**
  - Pour : librairie mature spécifiquement conçue pour Blazor (pas un
    portage JS) ; large catalogue de composants (formulaires, data grid,
    dialogues, snackbar) couvrant les besoins actuels (listes de livres,
    formulaires de création/édition, scanner ISBN) ; licence permissive
    (MIT), gratuite ; bonne intégration avec FluentValidation via des
    délégués simples.
  - Contre : identité visuelle par défaut peu différenciée tant qu'aucun
    thème custom n'est défini ; certains composants complexes (data grid
    avancé) ont des limites par rapport à des offres commerciales.
- **Option B — Composants Bootstrap + Razor "à la main"**
  - Pour : contrôle total du HTML/CSS, pas de dépendance à une librairie
    tierce Blazor.
  - Contre : beaucoup plus de code à écrire et maintenir pour des
    composants déjà résolus par MudBlazor (validation de formulaire,
    dialogues, snackbar, data binding two-way) ; pas de bind natif Blazor.
- **Option C — Librairie commerciale (Radzen, Syncfusion, Telerik...)**
  - Pour : composants avancés (grids, charts) souvent plus riches.
  - Contre : coût de licence pour un projet personnel ; complexité de
    configuration disproportionnée par rapport aux besoins actuels de
    l'application.

## Décision

On utilise **MudBlazor** comme unique librairie de composants pour
l'ensemble de l'UI Blazor Server, avec le thème par défaut de la librairie
(pas de thème custom à ce jour).

## Conséquences

### Positives

- Développement rapide de formulaires, listes et dialogues sans code
  HTML/CSS bas niveau à maintenir.
- Intégration native avec FluentValidation via des délégués de validation
  par champ (`BookValidator.cs`, `LibraryValidator.cs`).
- Composants accessibles par défaut dans une certaine mesure (MudBlazor
  suit les pratiques ARIA de base) — les règles internes du projet exigent
  malgré tout un `aria-label` explicite sur tout `MudIconButton`/`MudMenu`
  icon-only (`CLAUDE.md`, section "Accessibility").

### Négatives

- L'absence de thème personnalisé signifie que l'identité visuelle de
  l'application est actuellement celle par défaut de MudBlazor — la
  direction graphique reste une décision ouverte (cf. backlog "Refonte
  graphique de la liste de livres").
- Changer de librairie de composants plus tard serait coûteux (composants
  MudBlazor utilisés dans la quasi-totalité des pages).

### Couches impactées

- [x] Web (tous les composants Razor, `Program.cs` — `AddMudServices`)
- [ ] Domain
- [ ] Application
- [ ] Persistence

## Liens

- `Web/Program.cs` (`AddMudServices`, configuration Snackbar)
- `Web/Validators/BookValidator.cs`, `LibraryValidator.cs` (intégration FluentValidation)
- `CLAUDE.md` — section "Accessibility (Blazor / MudBlazor)"
- Backlog (ouverte) : "Refonte graphique de la liste de livres" (`TODO.md`)
