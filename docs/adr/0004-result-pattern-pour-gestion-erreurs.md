# ADR-0004: `Result<T>` / `TError` pour la gestion d'erreurs métier (vs exceptions)

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Les erreurs "attendues" du domaine (validation d'entrée, règle métier
violée, entité introuvable) doivent être traitées différemment d'une
erreur système imprévue (bug, panne réseau, etc.). Utiliser des exceptions
pour les deux cas mélange flux de contrôle métier et gestion d'incidents,
et rend le code appelant (handlers CQRS, composants Blazor) plus difficile
à raisonner : chaque appel devient potentiellement "throw".

Le projet définit son propre type `Result`/`Result<TValue>` dans
`Domain/Primitives/` (`Result.cs`, `ResultBase.cs`, `TError.cs`), avec des
conversions implicites depuis `TError`/`TValue`. Le code source crédite
explicitement `github.com/altmann/FluentResults` comme inspiration, mais le
pattern est **réimplémenté en interne** plutôt que consommé comme
dépendance NuGet.

`TError` est un `sealed record(string Code, string? Description)`, avec des
instances statiques définies par agrégat (`Domain/Books/BooksError.cs`,
`Domain/Libraries/LibrariesError.cs`, `Domain/Users/UsersError.cs`,
`Domain/ImportJobs/ImportJobError.cs`). Les handlers CQRS retournent
`Task<Result<T>>` de bout en bout (`CLAUDE.md` documente déjà cette
convention).

En complément, `Web/GlobalExceptionHandler.cs` (`IExceptionHandler`) associé
à `AddProblemDetails()` (`Web/Program.cs`) capture les exceptions
réellement imprévues et renvoie un `ProblemDetails` générique (500) — un
filet de sécurité pour ce que `Result<T>` ne couvre pas.

## Options considérées

- **Option A — `Result<T>`/`TError` maison (retenue)**
  - Pour : sépare clairement erreurs métier attendues (valeur de retour) et
    erreurs système imprévues (exception + `GlobalExceptionHandler`) ;
    aucune dépendance externe ; contrôle total sur la forme de `TError`
    (code + description) adaptée aux besoins du projet.
  - Contre : réimplémentation d'un pattern déjà disponible en package ;
    chaque nouveau développeur doit apprendre la convention interne plutôt
    qu'une bibliothèque documentée publiquement.
- **Option B — FluentResults (bibliothèque NuGet)**
  - Pour : bibliothèque mature, features supplémentaires (erreurs
    multiples, metadata, successes).
  - Contre : dépendance externe pour un besoin couvert par une
    implémentation minimale ; API plus riche que nécessaire ici.
- **Option C — Exceptions partout (y compris erreurs métier)**
  - Pour : plus simple à écrire au premier abord (`throw` direct).
  - Contre : coût de performance des exceptions sur le chemin nominal
    (erreurs métier fréquentes, ex. validation ISBN) ; oblige tous les
    appelants à `try/catch` pour distinguer erreur métier d'erreur système ;
    empêche de représenter facilement "plusieurs erreurs possibles" dans la
    signature d'une méthode.

## Décision

On utilise le type maison `Result`/`Result<TValue>` (`Domain/Primitives/`)
pour toutes les erreurs métier attendues, retourné explicitement par les
factory methods du domaine (`Book.Create`, `Library.Create`, ...) et par
tous les handlers CQRS. Les exceptions restent réservées aux erreurs
système imprévues, interceptées globalement par `GlobalExceptionHandler`.

## Conséquences

### Positives

- Les erreurs métier possibles sont visibles dans la signature de méthode
  (`Task<Result<Book>>` plutôt que `Task<Book>` + une exception cachée).
- Pas de coût de performance des exceptions sur le chemin d'erreur métier
  (fréquent : validations, règles d'agrégat).
- Séparation nette entre "l'utilisateur a fait une erreur" (Result) et
  "quelque chose d'anormal s'est produit" (exception + ProblemDetails 500).

### Négatives

- Discipline requise : un développeur qui oublie de vérifier
  `result.IsFailure` avant d'utiliser `result.Value` peut introduire un bug
  silencieux (`CLAUDE.md` documente déjà une règle dédiée : "Blazor — Error
  Handling in Load Methods" pour forcer la branche `else if
  (result.IsFailure)`).
- Toute nouvelle règle métier doit choisir consciemment entre "erreur
  attendue → `TError`" et "cas exceptionnel → exception" ; pas toujours
  évident à la frontière (ex. erreur de connexion DB pendant une validation).

### Couches impactées

- [x] Domain (`Result`, `TError`, factory methods)
- [x] Application (handlers CQRS retournant `Result<T>`)
- [x] Web (`GlobalExceptionHandler`, branches `IsFailure`/`IsSuccess` dans les composants)
- [ ] Persistence

## Liens

- `Domain/Primitives/Result.cs`, `ResultBase.cs`, `TError.cs`
- `Domain/Books/BooksError.cs`, `Domain/Libraries/LibrariesError.cs`,
  `Domain/Users/UsersError.cs`, `Domain/ImportJobs/ImportJobError.cs`
- `Web/GlobalExceptionHandler.cs`
- `CLAUDE.md` — sections "Error Handling" et "Blazor — Error Handling in Load Methods"
