#Requires -Version 7.0
<#
.SYNOPSIS
    Génère un rapport de couverture de tests pour MyComicsManager.

.DESCRIPTION
    Exécute les tests avec collecte de couverture via coverlet, puis génère
    un rapport HTML avec dotnet-reportgenerator-globaltool.

.PARAMETER OpenReport
    Ouvre le rapport HTML dans le navigateur après génération.

.PARAMETER SkipIntegration
    Ignore les projets de tests d'intégration (nécessitent une base de données).

.PARAMETER ConnectionString
    Connection string Neon pour les tests d'intégration.
    Si absent, la variable d'environnement ConnectionStrings__NeonConnectionUnitTests est utilisée.

.EXAMPLE
    ./Generate-CoverageReport.ps1
    ./Generate-CoverageReport.ps1 -OpenReport
    ./Generate-CoverageReport.ps1 -SkipIntegration -OpenReport
    ./Generate-CoverageReport.ps1 -SkipIntegration -ConnectionString "Host=...;Database=...;Username=...;Password=..." -OpenReport
#>

[CmdletBinding()]
param(
    [switch]$OpenReport,
    [switch]$SkipIntegration,
    [string]$ConnectionString = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─── Couleurs & helpers ────────────────────────────────────────────────────────
function Write-Step  { param($msg) Write-Host "`n▶ $msg" -ForegroundColor Cyan }
function Write-Ok    { param($msg) Write-Host "  ✓ $msg" -ForegroundColor Green }
function Write-Warn  { param($msg) Write-Host "  ⚠ $msg" -ForegroundColor Yellow }
function Write-Fail  { param($msg) Write-Host "  ✗ $msg" -ForegroundColor Red }

# ─── Chemins ──────────────────────────────────────────────────────────────────
$RepoRoot    = Split-Path $PSScriptRoot -Parent
$TestsDir    = Join-Path $RepoRoot "tests"
$CoverageDir = Join-Path $RepoRoot "coverage-report"
$ResultsDir  = Join-Path $CoverageDir "raw"

# Projets de tests unitaires
$UnitTestProjects = @(
    "Application.UnitTests"
    "Domain.UnitTests"
    "Architecture.Tests"
    "Web.Tests"
)

# Projets d'intégration (nécessitent ConnectionStrings__NeonConnectionUnitTests)
$IntegrationTestProjects = @(
    "Persistence.Integration.Tests"
)

# ─── Vérifications préalables ─────────────────────────────────────────────────
Write-Step "Vérification des prérequis"

# dotnet
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Fail "dotnet SDK introuvable. Installez le .NET SDK."
    exit 1
}
Write-Ok "dotnet $(dotnet --version)"

# reportgenerator
if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    Write-Warn "reportgenerator introuvable. Installation en cours..."
    dotnet tool install --global dotnet-reportgenerator-globaltool
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "Impossible d'installer reportgenerator."
        exit 1
    }
    Write-Ok "reportgenerator installé."
} else {
    Write-Ok "reportgenerator $(reportgenerator --version 2>&1 | Select-String '\d+\.\d+\.\d+' | ForEach-Object { $_.Matches[0].Value })"
}

# Variable d'env pour les tests d'intégration
if (-not $SkipIntegration) {
    if ($ConnectionString) {
        $env:ConnectionStrings__NeonConnectionUnitTests = $ConnectionString
        Write-Ok "Connection string définie via -ConnectionString."
    } elseif (-not $env:ConnectionStrings__NeonConnectionUnitTests) {
        Write-Warn "Aucune connection string fournie (-ConnectionString) ni variable d'environnement définie."
        Write-Warn "Les tests d'intégration seront ignorés (utilisez -SkipIntegration pour supprimer cet avertissement)."
        $SkipIntegration = $true
    } else {
        Write-Ok "Connection string lue depuis la variable d'environnement."
    }
}

# ─── Build unique ───────────────────────────────────────────────────────
Write-Step "Build de la solution"

$SolutionFile = Get-ChildItem $RepoRoot -Filter "*.sln" | Select-Object -First 1
if (-not $SolutionFile) {
    Write-Fail "Aucun fichier .sln trouvé dans $RepoRoot. Abandon."
    exit 1
}

dotnet build $SolutionFile.FullName --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Fail "La compilation a échoué. Abandon."
    exit 1
}
Write-Ok "Solution compilée : $($SolutionFile.Name)"

# ─── Nettoyage ────────────────────────────────────────────────────────
Write-Step "Nettoyage du répertoire de sortie"

if (Test-Path $CoverageDir) {
    Remove-Item $CoverageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ResultsDir | Out-Null
Write-Ok "Répertoire créé : $CoverageDir"

# ─── Exécution des tests ──────────────────────────────────────────────────────
$ProjectsToRun = $UnitTestProjects
if (-not $SkipIntegration) {
    $ProjectsToRun += $IntegrationTestProjects
}

$CoverageFiles = @()
$FailedProjects = @()

foreach ($ProjectName in $ProjectsToRun) {
    Write-Step "Tests : $ProjectName"

    $ProjectPath = Join-Path $TestsDir $ProjectName
    if (-not (Test-Path $ProjectPath)) {
        # Cherche un sous-dossier contenant le .csproj attendu :
        # 1. Correspondance exacte : sous-dossier contenant "$ProjectName.csproj"
        $ProjectPath = Get-ChildItem $TestsDir -Directory |
            Where-Object { Test-Path (Join-Path $_.FullName "$ProjectName.csproj") } |
            Select-Object -First 1 -ExpandProperty FullName

        # 2. Repli : sous-dossier contenant un .csproj dont le nom commence par le premier segment
        if (-not $ProjectPath) {
            $firstToken = $ProjectName.Split('.')[0]
            $candidates = @(Get-ChildItem $TestsDir -Directory |
                Where-Object {
                    Get-ChildItem $_.FullName -Filter "*.csproj" -File |
                        Where-Object { $_.BaseName.StartsWith($firstToken, [System.StringComparison]::OrdinalIgnoreCase) }
                } |
                Sort-Object FullName)

            if ($candidates.Count -gt 1) {
                Write-Fail "Ambiguïté : $($candidates.Count) dossiers correspondent au préfixe '$firstToken' pour le projet '$ProjectName' : $($candidates.FullName -join ', '). Abandon."
                exit 1
            }
            $ProjectPath = $candidates | Select-Object -First 1 -ExpandProperty FullName
        }
    }

    if (-not $ProjectPath -or -not (Test-Path $ProjectPath)) {
        Write-Fail "Projet introuvable : '$ProjectName' dans '$TestsDir'. Abandon."
        exit 1
    }

    $ResultPath = Join-Path $ResultsDir $ProjectName
    New-Item -ItemType Directory -Path $ResultPath | Out-Null

    $TestArgs = @(
        "test"
        $ProjectPath
        "--configuration", "Release"
        "--no-restore"
        "--no-build"
        "--collect:XPlat Code Coverage"
        "--results-directory", $ResultPath
        "--"
        "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura"
        "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include=[Domain]*,[Application]*,[Persistence]*,[Web]*"
        "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude=[*.Tests]*,[*.UnitTests]*,[*.Integration.Tests]*"
    )

    dotnet @TestArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Fail "$ProjectName : des tests ont échoué (code $LASTEXITCODE)."
        $FailedProjects += $ProjectName
    } else {
        Write-Ok "$ProjectName : OK"
    }

    # Collecte les fichiers de couverture générés
    $Generated = Get-ChildItem $ResultPath -Recurse -Filter "coverage.cobertura.xml"
    if ($Generated) {
        $CoverageFiles += $Generated.FullName
    }
}

# ─── Vérification des fichiers de couverture ─────────────────────────────────
Write-Step "Fichiers de couverture collectés"

if ($CoverageFiles.Count -eq 0) {
    Write-Fail "Aucun fichier coverage.cobertura.xml trouvé. Abandon."
    exit 1
}

foreach ($f in $CoverageFiles) {
    Write-Ok $f
}

# ─── Génération du rapport ────────────────────────────────────────────────────
Write-Step "Génération du rapport HTML"

$ReportPath = Join-Path $CoverageDir "html"
$ReportsArg = $CoverageFiles -join ";"

reportgenerator `
    "-reports:$ReportsArg" `
    "-targetdir:$ReportPath" `
    "-reporttypes:Html;HtmlSummary;Badges;TextSummary" `
    "-assemblyfilters:+Domain;+Application;+Persistence;+Web;-*.Tests;-*.UnitTests" `
    "-classfilters:-*Migrations*" `
    "-filefilters:-*.g.cs;-*RegexGenerator*;-*GlobalUsings*" `
    "-title:MyComicsManager Coverage" `
    "-verbosity:Warning"

if ($LASTEXITCODE -ne 0) {
    Write-Fail "reportgenerator a échoué."
    exit 1
}

# ─── Résumé textuel ───────────────────────────────────────────────────────────
$SummaryFile = Join-Path $ReportPath "Summary.txt"
if (Test-Path $SummaryFile) {
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    Get-Content $SummaryFile | Write-Host
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
}

# ─── Résultat final ───────────────────────────────────────────────────────────
$IndexFile = Join-Path $ReportPath "index.html"
Write-Ok "Rapport généré : $IndexFile"

if ($OpenReport) {
    Write-Step "Ouverture du rapport"
    Start-Process $IndexFile
}

if ($FailedProjects.Count -gt 0) {
    Write-Warn "Projets avec des échecs de tests : $($FailedProjects -join ', ')"
    exit 1
}
