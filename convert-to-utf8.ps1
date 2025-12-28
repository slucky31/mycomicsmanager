# Script pour convertir les fichiers en UTF-8 sans BOM
# Usage: .\convert-to-utf8.ps1
# Usage avec chemin: .\convert-to-utf8.ps1 -Path "C:\MonProjet"

param(
    [string]$Path = ".",
    [switch]$DryRun = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Conversion UTF-8 sans BOM" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "MODE TEST (DRY RUN) - Aucun fichier ne sera modifié" -ForegroundColor Yellow
    Write-Host ""
}

# Extensions à convertir (basées sur votre analyse)
$extensionsToConvert = @(
    '*.cs',
    '*.razor',
    '*.csproj',
    '*.yml',
    '*.yaml',
    '*.json',
    '*.md',
    '*.js',
    '*.props',
    '*.css',
    '*.txt',
    '*.editorconfig',
    '*.versionize',
    '*.dockerignore',
    '*.ps1',
    '*.sln',
    '*.toml',
    '*.gitignore',
    '*.cshtml',
    '*.xml',
    '*.config'
)

# Fichiers sans extension à inclure
$filesWithoutExtension = @(
    'LICENSE',
    'Dockerfile'
)

# Dossiers à exclure
$excludeDirs = @(
    'bin',
    'obj',
    '.git',
    '.vs',
    'node_modules',
    'packages',
    'TestResults'
)

Write-Host "Extensions à traiter: " -ForegroundColor Yellow
$extensionsToConvert | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
Write-Host ""

# Fonction pour vérifier si un fichier a le BOM UTF-8
function Test-UTF8BOM {
    param([string]$FilePath)
    
    try {
        $bytes = [System.IO.File]::ReadAllBytes($FilePath)
        if ($bytes.Length -ge 3) {
            return ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
        }
    }
    catch {
        return $false
    }
    return $false
}

# Fonction pour convertir un fichier
function Convert-FileToUTF8NoBOM {
    param(
        [string]$FilePath,
        [switch]$DryRun
    )
    
    try {
        # Vérifier si le fichier a déjà le BOM
        $hasBOM = Test-UTF8BOM -FilePath $FilePath
        
        if (-not $hasBOM) {
            Write-Host "  ⊘ Déjà UTF-8 sans BOM" -ForegroundColor DarkGray -NoNewline
            return $false
        }
        
        if ($DryRun) {
            Write-Host "  ✓ Serait converti (BOM détecté)" -ForegroundColor Yellow -NoNewline
            return $true
        }
        
        # Lire le contenu
        $content = [System.IO.File]::ReadAllText($FilePath)
        
        # Créer un encodeur UTF-8 sans BOM
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        
        # Écrire avec le nouvel encodage
        [System.IO.File]::WriteAllText($FilePath, $content, $utf8NoBom)
        
        Write-Host "  ✓ Converti" -ForegroundColor Green -NoNewline
        return $true
    }
    catch {
        Write-Host "  ✗ Erreur: $($_.Exception.Message)" -ForegroundColor Red -NoNewline
        return $false
    }
}

# Compteurs
$totalFiles = 0
$convertedFiles = 0
$skippedFiles = 0
$errorFiles = 0

Write-Host "Début de la conversion..." -ForegroundColor Cyan
Write-Host ""

# Traiter les fichiers par extension
foreach ($extension in $extensionsToConvert) {
    $files = Get-ChildItem -Path $Path -Filter $extension -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $file = $_
            $shouldExclude = $false
            foreach ($dir in $excludeDirs) {
                if ($file.FullName -like "*\$dir\*") {
                    $shouldExclude = $true
                    break
                }
            }
            -not $shouldExclude
        }
    
    if ($files.Count -gt 0) {
        Write-Host "Traitement des fichiers $extension :" -ForegroundColor Yellow
        
        foreach ($file in $files) {
            $totalFiles++
            $relativePath = $file.FullName.Replace((Get-Item $Path).FullName, ".") 
            Write-Host "  $relativePath" -NoNewline
            
            $result = Convert-FileToUTF8NoBOM -FilePath $file.FullName -DryRun:$DryRun
            
            if ($result -eq $true) {
                $convertedFiles++
            }
            elseif ($result -eq $false) {
                $skippedFiles++
            }
            else {
                $errorFiles++
            }
            
            Write-Host "" # Nouvelle ligne
        }
        Write-Host ""
    }
}

# Traiter les fichiers sans extension
Write-Host "Traitement des fichiers sans extension :" -ForegroundColor Yellow
foreach ($fileName in $filesWithoutExtension) {
    $files = Get-ChildItem -Path $Path -Filter $fileName -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $file = $_
            $shouldExclude = $false
            foreach ($dir in $excludeDirs) {
                if ($file.FullName -like "*\$dir\*") {
                    $shouldExclude = $true
                    break
                }
            }
            -not $shouldExclude
        }
    
    foreach ($file in $files) {
        $totalFiles++
        $relativePath = $file.FullName.Replace((Get-Item $Path).FullName, ".")
        Write-Host "  $relativePath" -NoNewline
        
        $result = Convert-FileToUTF8NoBOM -FilePath $file.FullName -DryRun:$DryRun
        
        if ($result -eq $true) {
            $convertedFiles++
        }
        elseif ($result -eq $false) {
            $skippedFiles++
        }
        else {
            $errorFiles++
        }
        
        Write-Host ""
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Résumé" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Fichiers analysés      : $totalFiles" -ForegroundColor White
Write-Host "Fichiers convertis     : $convertedFiles" -ForegroundColor Green
Write-Host "Déjà UTF-8 sans BOM    : $skippedFiles" -ForegroundColor DarkGray
if ($errorFiles -gt 0) {
    Write-Host "Erreurs                : $errorFiles" -ForegroundColor Red
}
Write-Host ""

if ($DryRun) {
    Write-Host "⚠ MODE TEST - Aucun fichier n'a été modifié" -ForegroundColor Yellow
    Write-Host "  Exécutez sans -DryRun pour appliquer les modifications" -ForegroundColor Yellow
}
else {
    Write-Host "✓ Conversion terminée!" -ForegroundColor Green
    
    if ($convertedFiles -gt 0) {
        Write-Host ""
        Write-Host "Prochaines étapes recommandées :" -ForegroundColor Yellow
        Write-Host "  1. Vérifier que tout compile : dotnet build" -ForegroundColor White
        Write-Host "  2. Tester votre application" -ForegroundColor White
        Write-Host "  3. Commiter les changements :" -ForegroundColor White
        Write-Host "     git add -A" -ForegroundColor DarkGray
        Write-Host "     git commit -m `"Convert all files to UTF-8 without BOM`"" -ForegroundColor DarkGray
    }
}

Write-Host ""
