# ADR-0006: Blazor Server (plutôt que WebAssembly) comme modèle d'hébergement UI

**Statut** : Acceptée
**Date** : 2026-09-19
**Décideurs** : @nicolas-dufaut

## Contexte

Blazor propose plusieurs modèles d'exécution : **Server** (rendu côté
serveur, état maintenu en mémoire serveur, communication via SignalR),
**WebAssembly** (exécution du binaire .NET dans le navigateur), et
**Auto/Hybrid** (bascule entre les deux).

Le projet utilise Blazor **Server** :
`Web/Program.cs` — `AddRazorComponents().AddInteractiveServerComponents()`,
`.AddInteractiveServerRenderMode()`. Aucune trace de composants WASM ou de
mode Auto dans la configuration.

Ce choix est cohérent avec le reste de la stack : hébergement self-hosted
mono-utilisateur ([ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md)),
Auth0 avec cookie d'authentification côté serveur
(`CustomAuthenticationStateProvider`), et un besoin de JS interop limité
(scanner ISBN) plutôt que d'exécution lourde côté client.

Point de vigilance déjà identifié dans les documents internes du projet
(`Analysis.md`) : Blazor Server maintient une connexion SignalR persistante
par utilisateur connecté, ce qui pose une question de scalabilité en cas de
montée en charge (nombre de connexions simultanées, gestion de la
reconnexion réseau).

## Options considérées

- **Option A — Blazor Server (retenue)**
  - Pour : temps de chargement initial rapide (pas de téléchargement du
    runtime .NET/WASM) ; accès direct et sans latence supplémentaire aux
    services serveur (EF Core, Hangfire) depuis les composants ; cohérent
    avec un déploiement self-hosted mono-utilisateur où le nombre de
    connexions simultanées reste faible.
  - Contre : dépendance à une connexion SignalR permanente (sensible à la
    latence réseau et aux coupures) ; ne fonctionne pas hors-ligne ; la
    charge (état + rendu) est portée par le serveur, donc par le
    Raspberry Pi.
- **Option B — Blazor WebAssembly**
  - Pour : s'exécute entièrement côté client, pas de connexion permanente
    requise, peut fonctionner hors-ligne (PWA).
  - Contre : téléchargement initial plus lourd (runtime .NET) ; accès aux
    données doit passer par une API HTTP exposée séparément (plus de
    surface d'authentification/autorisation à sécuriser) ; inutile pour une
    app mono-utilisateur qui n'a pas de contrainte hors-ligne.
- **Option C — Blazor Auto/Hybrid**
  - Pour : bascule automatique Server → WASM après le premier chargement,
    cumule les avantages des deux modes.
  - Contre : complexité de configuration et de test bien plus importante
    (composants doivent fonctionner dans les deux contextes de rendu) pour
    un bénéfice non nécessaire ici (pas de besoin hors-ligne, usage
    mono-utilisateur).

## Décision

On utilise **Blazor Server** avec rendu interactif serveur
(`AddInteractiveServerComponents`) comme unique modèle d'exécution de
l'UI.

## Conséquences

### Positives

- Chargement initial rapide, cohérent avec un usage mono-utilisateur sur
  réseau local/personnel.
- Accès direct aux services serveur (EF Core, Hangfire, Auth0) sans exposer
  d'API HTTP publique supplémentaire pour l'UI.
- Empreinte client minimale (pas de runtime .NET téléchargé dans le
  navigateur).

### Négatives

- Scalabilité limitée par le nombre de connexions SignalR simultanées que
  le Raspberry Pi peut maintenir — non problématique aujourd'hui (usage
  mono-utilisateur) mais à revisiter si l'app devait accueillir plusieurs
  utilisateurs simultanés.
- Aucune tolérance au hors-ligne : une coupure réseau ou serveur interrompt
  l'interactivité de l'UI en cours.
- Toute la charge de rendu est portée par le serveur, à surveiller en même
  temps que la charge du pipeline d'import (Hangfire) qui tourne sur le
  même hôte.

### Couches impactées

- [x] Web (hosting model, `Program.cs`, tous les composants Razor)
- [ ] Domain
- [ ] Application
- [ ] Persistence

## Liens

- `Web/Program.cs` (`AddInteractiveServerComponents`, `AddInteractiveServerRenderMode`)
- `Web/CustomAuthenticationStateProvider.cs`
- [ADR-0001](0001-deploiement-raspberry-pi-4-self-hosted.md) — cible de déploiement Raspberry Pi 4
- `Analysis.md` — remarque sur la scalabilité des connexions SignalR (point de vigilance, pas encore une ADR ouverte séparée)
