# Script pour FORCER la conversion ASCII → UTF-8 sans BOM
# Ce script réécrit vraiment les fichiers pour que Windows les reconnaisse comme UTF-8
# Usage: .\force-utf8-encoding.ps1
# Usage avec test: .\force-utf8-encoding.ps1 -DryRun

param(
    [string]$Path = ".",
    [switch]$DryRun = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FORCER l'encodage UTF-8 sans BOM" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "MODE TEST (DRY RUN) - Aucun fichier ne sera modifié" -ForegroundColor Yellow
    Write-Host ""
}

# Extensions à convertir
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
    '*.config',
    '*.html',
    '*.htm',
    '*.sql'
)

# Fichiers sans extension
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

# Fonction pour forcer UTF-8
function Force-UTF8Encoding {
    param(
        [string]$FilePath,
        [switch]$DryRun
    )
    
    try {
        if ($DryRun) {
            Write-Host " [Serait forcé en UTF-8]" -ForegroundColor Yellow -NoNewline
            return "would-convert"
        }
        
        # Lire le contenu en détectant automatiquement l'encodage
        $content = Get-Content -Path $FilePath -Raw -Encoding Default
        
        # Si le fichier est vide, on le laisse tel quel
        if ([string]::IsNullOrEmpty($content)) {
            Write-Host " [Vide]" -ForegroundColor DarkGray -NoNewline
            return "skipped"
        }
        
        # Créer un encodeur UTF-8 sans BOM
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        
        # Réécrire le fichier en UTF-8 sans BOM
        # L'utilisation de WriteAllText force l'écriture en UTF-8
        [System.IO.File]::WriteAllText($FilePath, $content, $utf8NoBom)
        
        Write-Host " [Forcé UTF-8] ✓" -ForegroundColor Green -NoNewline
        return "converted"
    }
    catch {
        Write-Host " [Erreur: $($_.Exception.Message)]" -ForegroundColor Red -NoNewline
        return "error"
    }
}

# Compteurs
$stats = @{
    Total = 0
    Converted = 0
    Skipped = 0
    Errors = 0
    WouldConvert = 0
}

Write-Host "Forçage de l'encodage UTF-8 en cours..." -ForegroundColor Cyan
Write-Host ""

# Fonction pour traiter les fichiers
function Process-Files {
    param(
        [string]$Pattern,
        [string]$Description
    )
    
    $files = Get-ChildItem -Path $Path -Filter $Pattern -Recurse -File -ErrorAction SilentlyContinue |
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
        Write-Host "$Description ($($files.Count) fichiers) :" -ForegroundColor Yellow
        
        foreach ($file in $files) {
            $script:stats.Total++
            $relativePath = $file.FullName.Replace((Get-Item $Path).FullName, ".")
            
            # Tronquer le chemin s'il est trop long
            if ($relativePath.Length -gt 70) {
                $relativePath = "..." + $relativePath.Substring($relativePath.Length - 67)
            }
            
            Write-Host "  $relativePath" -NoNewline
            
            # Forcer UTF-8
            $result = Force-UTF8Encoding -FilePath $file.FullName -DryRun:$DryRun
            
            switch ($result) {
                "converted"       { $script:stats.Converted++ }
                "skipped"        { $script:stats.Skipped++ }
                "error"          { $script:stats.Errors++ }
                "would-convert"  { $script:stats.WouldConvert++ }
            }
            
            Write-Host ""
        }
        Write-Host ""
    }
}

# Traiter tous les fichiers
foreach ($extension in $extensionsToConvert) {
    Process-Files -Pattern $extension -Description "Fichiers $extension"
}

# Traiter les fichiers sans extension
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
    
    if ($files.Count -gt 0) {
        Write-Host "Fichier $fileName :" -ForegroundColor Yellow
        foreach ($file in $files) {
            $stats.Total++
            $relativePath = $file.FullName.Replace((Get-Item $Path).FullName, ".")
            Write-Host "  $relativePath" -NoNewline
            
            $result = Force-UTF8Encoding -FilePath $file.FullName -DryRun:$DryRun
            
            switch ($result) {
                "converted"       { $stats.Converted++ }
                "skipped"        { $stats.Skipped++ }
                "error"          { $stats.Errors++ }
                "would-convert"  { $stats.WouldConvert++ }
            }
            
            Write-Host ""
        }
        Write-Host ""
    }
}

# Afficher les résultats
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Résumé des opérations" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Fichiers traités       : $($stats.Total)" -ForegroundColor White

if ($DryRun) {
    Write-Host "À forcer en UTF-8      : $($stats.WouldConvert)" -ForegroundColor Yellow
    Write-Host "Fichiers vides         : $($stats.Skipped)" -ForegroundColor DarkGray
}
else {
    Write-Host "Fichiers forcés UTF-8  : $($stats.Converted)" -ForegroundColor Green
    Write-Host "Fichiers vides         : $($stats.Skipped)" -ForegroundColor DarkGray
}

if ($stats.Errors -gt 0) {
    Write-Host "Erreurs                : $($stats.Errors)" -ForegroundColor Red
}
Write-Host ""

if ($DryRun) {
    Write-Host "⚠ MODE TEST - Aucun fichier n'a été modifié" -ForegroundColor Yellow
    Write-Host "  Exécutez sans -DryRun pour forcer tous les fichiers en UTF-8" -ForegroundColor Yellow
}
else {
    Write-Host "✓ Forçage terminé!" -ForegroundColor Green
    Write-Host "  Tous vos fichiers ont été réécrits en UTF-8 sans BOM" -ForegroundColor Green
    Write-Host ""
    Write-Host "ℹ Note technique :" -ForegroundColor Cyan
    Write-Host "  Les fichiers ASCII purs (sans caractères > 127) sont techniquement" -ForegroundColor White
    Write-Host "  identiques en UTF-8. Windows peut toujours les détecter comme ASCII" -ForegroundColor White
    Write-Host "  car les octets sont les mêmes. C'est normal et sans conséquence." -ForegroundColor White
    
    if ($stats.Converted -gt 0) {
        Write-Host ""
        Write-Host "Prochaines étapes recommandées :" -ForegroundColor Yellow
        Write-Host "  1. Vérifier que tout compile : dotnet build" -ForegroundColor White
        Write-Host "  2. Tester votre application" -ForegroundColor White
        Write-Host "  3. Commiter les changements :" -ForegroundColor White
        Write-Host "     git add -A" -ForegroundColor DarkGray
        Write-Host "     git commit -m `"Force all files to UTF-8 encoding`"" -ForegroundColor DarkGray
    }
}

Write-Host ""
