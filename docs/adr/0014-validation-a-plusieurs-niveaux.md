# ADR-0014: Validation à plusieurs niveaux (FluentValidation UI + validation d'entrée handler + invariants domaine)

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Une même donnée (ex. un titre de livre, un ISBN) peut être invalide à
plusieurs endroits du flux : saisie utilisateur incorrecte dans l'UI,
paramètre de commande CQRS malformé, ou violation d'une règle métier au
niveau du domaine. Le projet ne fait pas reposer cette responsabilité sur
une seule couche : la validation est répartie sur **trois niveaux
distincts**, chacun avec un rôle différent.

1. **UI / DTO (FluentValidation)** — `Web/Validators/BookValidator.cs`,
   `LibraryValidator.cs` valident `BookUiDto`/`LibraryUiDto` (forme des
   champs : longueur, plage, format) avant soumission. Ces validateurs
   exposent un délégué `ValidateValue` branché directement sur les champs
   MudBlazor (`Validation` param de `MudTextField`/`MudForm`), pour un
   retour immédiat à l'utilisateur.
2. **Entrée des handlers CQRS** — règle documentée dans `CLAUDE.md`
   ("CQRS Handlers — Input Validation") : chaque `Handle` valide tous ses
   paramètres d'entrée (format, plage, nullité) en tout début de méthode,
   avant tout appel à un repository ou service externe, et retourne une
   erreur domaine (`Result<T>`) en cas d'échec.
3. **Invariants du domaine** — les factory methods (`Book.Create`,
   `Library.Create`, etc.) réappliquent les règles métier fondamentales et
   retournent `Result<T>` (cf. [ADR-0004](0004-result-pattern-pour-gestion-erreurs.md)),
   garantissant qu'aucune entité invalide ne peut exister, même si elle est
   construite depuis un autre chemin que l'UI (ex. script d'import, futur
   endpoint API).

Ces trois niveaux ne sont pas redondants par accident : chacun protège un
point d'entrée différent (UI, cas d'usage, modèle de domaine), et un
appelant qui contourne l'UI (API, script, autre handler) reste protégé par
les deux niveaux suivants.

## Options considérées

- **Option A — Validation à trois niveaux, chacun à son point d'entrée (retenue)**
  - Pour : aucun point d'entrée (UI, handler, construction directe d'entité)
    ne peut produire une donnée invalide ; le retour d'erreur reste rapide
    et localisé à la couche où l'erreur est détectée (pas besoin de
    remonter jusqu'au domaine pour un simple champ vide côté UI).
  - Contre : une même règle (ex. "le titre ne peut pas être vide") peut
    exister formulée à deux ou trois endroits différents, avec un risque de
    divergence si l'une est mise à jour sans les autres.
- **Option B — Validation uniquement dans le domaine (factory methods)**
  - Pour : une seule source de vérité pour chaque règle métier.
  - Contre : l'UI devrait attendre un aller-retour serveur complet pour
    signaler une erreur de saisie simple (mauvaise expérience utilisateur) ;
    perd le retour immédiat par champ que permet FluentValidation dans
    MudBlazor.
- **Option C — Validation uniquement côté UI (FluentValidation)**
  - Pour : retour immédiat à l'utilisateur, simple à mettre en place.
  - Contre : n'importe quel appelant qui ne passe pas par l'UI (script,
    futur endpoint API, test) pourrait créer une entité invalide en base —
    incompatible avec la garantie recherchée par le pattern `Result<T>`
    (cf. [ADR-0004](0004-result-pattern-pour-gestion-erreurs.md)).

## Décision

On maintient une validation à trois niveaux : FluentValidation pour les
DTO/formulaires UI (retour rapide à l'utilisateur), validation d'entrée en
tête de chaque handler CQRS (protège chaque cas d'usage), et invariants
appliqués dans les factory methods du domaine (garantie ultime,
indépendante du point d'entrée).

## Conséquences

### Positives

- Aucune entité invalide ne peut exister en base, quel que soit le chemin
  emprunté pour la créer (UI, script, futur endpoint API).
- L'UI conserve un retour de validation rapide et localisé par champ, sans
  attendre un aller-retour serveur complet pour les erreurs de saisie
  simples.
- Chaque couche protège son propre point d'entrée sans dépendre de la
  bonne exécution des couches au-dessus d'elle.

### Négatives

- Une règle métier modifiée doit potentiellement être répercutée à
  plusieurs endroits (validator FluentValidation, validation d'entrée du
  handler, invariant du domaine) pour rester cohérente — pas de mécanisme
  automatique de synchronisation entre les trois niveaux.
- Un nouveau contributeur peut se demander pourquoi une règle semble
  dupliquée s'il ne connaît pas le rôle distinct de chaque niveau.

### Couches impactées

- [x] Web (validators FluentValidation)
- [x] Application (validation d'entrée en tête des handlers)
- [x] Domain (invariants dans les factory methods)
- [ ] Persistence

## Liens

- `Web/Validators/BookValidator.cs`, `LibraryValidator.cs`
- `CLAUDE.md` — section "CQRS Handlers — Input Validation"
- [ADR-0004](0004-result-pattern-pour-gestion-erreurs.md) — `Result<T>` utilisé par les deux derniers niveaux
