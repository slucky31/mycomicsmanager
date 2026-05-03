# MyComicsManager

[![.NET Core Build](https://github.com/slucky31/mycomicsmanager/actions/workflows/dotnet-core-build.yml/badge.svg)](https://github.com/slucky31/mycomicsmanager/actions/workflows/dotnet-core-build.yml)

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-9.0.0-594AE2?logo=blazor)](https://mudblazor.com/)
[![Entity Framework Core](https://img.shields.io/badge/EF_Core-10.0.3-512BD4?logo=dotnet)](https://docs.microsoft.com/ef/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Neon-4169E1?logo=postgresql)](https://neon.tech/)
[![Auth0](https://img.shields.io/badge/Auth0-1.6.1-EB5424?logo=auth0)](https://auth0.com/)
[![Serilog](https://img.shields.io/badge/Serilog-10.0.0-0099A0?logo=serilog)](https://serilog.net/)
[![Docker](https://img.shields.io/badge/Docker-linux%2Farm64-2496ED?logo=docker)](https://www.docker.com/)

[![License](https://img.shields.io/github/license/slucky31/mycomicsmanager)](LICENSE)

---

## 🗂️ Description

**MyComicsManager** est une application web permettant de gérer votre collection de bandes dessinées. Elle offre une interface moderne et réactive pour cataloguer, rechercher et organiser vos comics et BD.

---

## 🏗️ Architecture

Le projet suit une **architecture en couches (Clean Architecture)** avec le pattern **CQRS** :

```
Domain/        → Entités, objets valeurs, erreurs (aucune dépendance externe)
Application/   → Handlers CQRS, interfaces, orchestrateurs (dépend uniquement du Domain)
Persistence/   → Infrastructure EF Core, repositories, migrations PostgreSQL
Web/           → Interface Blazor Server avec MudBlazor, endpoints, authentification Auth0
tests/         → Tests unitaires, d'intégration, d'architecture et de composants
```

Les règles d'architecture sont vérifiées automatiquement via `tests/Architecture.Tests` (NetArchTest).

---

## 🚀 Technologies utilisées

| Technologie | Version | Rôle |
|---|---|---|
| [.NET](https://dotnet.microsoft.com/) | 10.0 | Framework principal |
| [Blazor Server](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) | 10.0 | Interface utilisateur |
| [MudBlazor](https://mudblazor.com/) | 9.0.0 | Composants UI Material Design |
| [Entity Framework Core](https://docs.microsoft.com/ef/core/) | 10.0.3 | ORM / accès aux données |
| [PostgreSQL (Neon)](https://neon.tech/) | — | Base de données |
| [Auth0](https://auth0.com/) | 1.6.1 | Authentification |
| [Serilog](https://serilog.net/) | 10.0.0 | Journalisation structurée |
| [Cloudinary](https://cloudinary.com/) | 1.28.0 | Gestion des images |
| [FluentValidation](https://docs.fluentvalidation.net/) | 12.1.1 | Validation des données |
| [Scrutor](https://github.com/khellang/Scrutor) | 7.0.0 | Auto-enregistrement DI |
| [Docker](https://www.docker.com/) | — | Conteneurisation (linux/arm64) |

---

## 🧪 Tests

| Projet | Type | Outils |
|---|---|---|
| `Application.UnitTests` | Tests unitaires (handlers CQRS) | xUnit 2.9.3, NSubstitute 5.3.0, AwesomeAssertions 9.4.0 |
| `Domain.UnitTests` | Tests unitaires (entités, valeurs) | xUnit, AwesomeAssertions |
| `Persistence.Integration.Tests` | Tests d'intégration EF Core / PostgreSQL | xUnit + base réelle |
| `Architecture.Tests` | Vérification des frontières de couches | NetArchTest 1.3.2 |
| `Web.Tests` | Tests de composants Blazor | bUnit 2.5.3 |

---

## ⚙️ Installation et démarrage

### Prérequis

- [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (optionnel)
- Une base de données PostgreSQL (ex. : [Neon](https://neon.tech/))
- Un tenant [Auth0](https://auth0.com/)

### Démarrage local

```bash
# Restaurer les dépendances
dotnet restore

# Compiler la solution
dotnet build MyComicsManager.slnx

# Lancer l'application (port 8080)
dotnet run --project Web/Web.csproj

# Rechargement à chaud
dotnet watch run --project Web/Web.csproj
```

### Configuration

Copiez `appsettings.json` en `appsettings.Development.json` et renseignez les valeurs :

```json
{
  "ConnectionStrings": {
    "Default": "<votre-chaine-postgresql>"
  },
  "Auth0": {
    "Domain": "<votre-domaine-auth0>",
    "ClientId": "<votre-client-id>"
  }
}
```

> ⚠️ Ne committez jamais vos secrets. Utilisez des variables d'environnement en production.

### Migrations EF Core

```bash
dotnet ef migrations add NomDeLaMigration --project Persistence
dotnet ef database update --project Persistence
```

---

## 🧹 Qualité du code

```bash
# Vérifier la conformité du style
dotnet format --verify-no-changes

# Corriger automatiquement le style
dotnet format
```

Le projet utilise `.editorconfig` (4 espaces pour C#, 2 espaces pour XML, CRLF, UTF-8 BOM) et [SonarAnalyzer.CSharp](https://www.sonarqube.org/) pour l'analyse statique.

---

## 🐳 Docker

```bash
docker build -f Web/Dockerfile -t mycomicsmanager-web .
docker run -p 8080:8080 mycomicsmanager-web
```

L'image est publiée sur le registre GitHub Container Registry (`ghcr.io`) via le workflow CI/CD.

---

## 📝 Licence

Ce projet est distribué sous licence [MIT](LICENSE).

---

## 👤 Auteur

**Nicolas DUFAUT** – [nicolas.dufaut@gmail.com](mailto:nicolas.dufaut@gmail.com)
