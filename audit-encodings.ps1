# Script pour analyser les encodages de tous les fichiers
# Usage: .\audit-encodings.ps1
# Usage avec détails: .\audit-encodings.ps1 -Detailed
# Usage pour un type spécifique: .\audit-encodings.ps1 -ShowOnly ASCII

param(
    [string]$Path = ".",
    [switch]$Detailed = $false,
    [string]$ShowOnly = "",
    [switch]$ExportCsv = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Audit des encodages de fichiers" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Extensions à analyser
$extensionsToAnalyze = @(
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
    '*.sql',
    '*.resx',
    '*.xaml'
)

# Fichiers sans extension
$filesWithoutExtension = @(
    'LICENSE',
    'Dockerfile',
    '.gitattributes'
)

# Dossiers à exclure
$excludeDirs = @(
    'bin',
    'obj',
    '.git',
    '.vs',
    'node_modules',
    'packages',
    'TestResults',
    'BenchmarkDotNet.Artifacts'
)

Write-Host "Chemin analysé: $((Get-Item $Path).FullName)" -ForegroundColor Yellow
Write-Host "Dossiers exclus: $($excludeDirs -join ', ')" -ForegroundColor Yellow
Write-Host ""

# Fonction pour détecter l'encodage d'un fichier
function Get-FileEncoding {
    param([string]$FilePath)
    
    try {
        $bytes = [System.IO.File]::ReadAllBytes($FilePath)
        
        if ($bytes.Length -eq 0) {
            return "Empty"
        }
        
        # Vérifier UTF-8 avec BOM
        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
            return "UTF8-BOM"
        }
        
        # Vérifier UTF-16 LE BOM
        if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
            return "UTF16-LE"
        }
        
        # Vérifier UTF-16 BE BOM
        if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {
            return "UTF16-BE"
        }
        
        # Vérifier UTF-32 LE BOM
        if ($bytes.Length -ge 4 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE -and $bytes[2] -eq 0x00 -and $bytes[3] -eq 0x00) {
            return "UTF32-LE"
        }
        
        # Vérifier UTF-32 BE BOM
        if ($bytes.Length -ge 4 -and $bytes[0] -eq 0x00 -and $bytes[1] -eq 0x00 -and $bytes[2] -eq 0xFE -and $bytes[3] -eq 0xFF) {
            return "UTF32-BE"
        }
        
        # Vérifier si c'est du UTF-8 valide (sans BOM)
        try {
            $text = [System.Text.Encoding]::UTF8.GetString($bytes)
            $bytesBack = [System.Text.Encoding]::UTF8.GetBytes($text)
            
            # Si on peut faire l'aller-retour sans perte
            if ($bytes.Length -eq $bytesBack.Length) {
                $match = $true
                for ($i = 0; $i -lt $bytes.Length; $i++) {
                    if ($bytes[$i] -ne $bytesBack[$i]) {
                        $match = $false
                        break
                    }
                }
                if ($match) {
                    # Vérifier s'il y a des caractères étendus (> 127)
                    $hasExtendedChars = $false
                    foreach ($byte in $bytes) {
                        if ($byte -gt 127) {
                            $hasExtendedChars = $true
                            break
                        }
                    }
                    
                    if ($hasExtendedChars) {
                        return "UTF8"
                    }
                    else {
                        # Tous les caractères sont < 128, c'est de l'ASCII pur
                        return "ASCII"
                    }
                }
            }
        }
        catch {
            # Pas du UTF-8 valide
        }
        
        # Probablement ANSI/Windows-1252 ou autre encodage
        return "ANSI/Other"
    }
    catch {
        return "Error"
    }
}

# Structures pour stocker les résultats
[System.Collections.ArrayList]$allFiles = @()
$encodingStats = @{}
$encodingByExtension = @{}

Write-Host "Scan des fichiers en cours..." -ForegroundColor Cyan
Write-Host ""

# Fonction pour traiter les fichiers
function Scan-Files {
    param(
        [string]$Pattern
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
    
    foreach ($file in $files) {
        $encoding = Get-FileEncoding -FilePath $file.FullName
        $extension = if ($file.Extension) { $file.Extension } else { "(sans extension)" }
        
        # Compter les encodages globalement
        if (-not $encodingStats.ContainsKey($encoding)) {
            $encodingStats[$encoding] = 0
        }
        $encodingStats[$encoding]++
        
        # Compter par extension
        if (-not $encodingByExtension.ContainsKey($extension)) {
            $encodingByExtension[$extension] = @{}
        }
        if (-not $encodingByExtension[$extension].ContainsKey($encoding)) {
            $encodingByExtension[$extension][$encoding] = 0
        }
        $encodingByExtension[$extension][$encoding]++
        
        # Stocker le fichier
        $relativePath = $file.FullName.Replace((Get-Item $Path).FullName, ".")
        [void]$allFiles.Add([PSCustomObject]@{
            Path = $relativePath
            Extension = $extension
            Encoding = $encoding
            Size = $file.Length
        })
    }
}

# Scanner tous les fichiers
foreach ($extension in $extensionsToAnalyze) {
    Scan-Files -Pattern $extension
}

# Scanner les fichiers sans extension
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
        $encoding = Get-FileEncoding -FilePath $file.FullName
        $extension = "(sans extension)"
        
        if (-not $encodingStats.ContainsKey($encoding)) {
            $encodingStats[$encoding] = 0
        }
        $encodingStats[$encoding]++
        
        if (-not $encodingByExtension.ContainsKey($extension)) {
            $encodingByExtension[$extension] = @{}
        }
        if (-not $encodingByExtension[$extension].ContainsKey($encoding)) {
            $encodingByExtension[$extension][$encoding] = 0
        }
        $encodingByExtension[$extension][$encoding]++
        
        $relativePath = $file.FullName.Replace((Get-Item $Path).FullName, ".")
        [void]$allFiles.Add([PSCustomObject]@{
            Path = $relativePath
            Extension = $extension
            Encoding = $encoding
            Size = $file.Length
        })
    }
}

# Afficher le résumé global
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RÉSUMÉ GLOBAL DES ENCODAGES" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$totalFiles = $allFiles.Count
Write-Host "Total de fichiers analysés: $totalFiles" -ForegroundColor White
Write-Host ""

foreach ($enc in $encodingStats.Keys | Sort-Object) {
    $count = $encodingStats[$enc]
    $percentage = [math]::Round(($count / $totalFiles) * 100, 1)
    
    $color = switch ($enc) {
        "UTF8"         { "Green" }
        "ASCII"        { "Yellow" }
        "UTF8-BOM"     { "Magenta" }
        "ANSI/Other"   { "Red" }
        "UTF16-LE"     { "Red" }
        "UTF16-BE"     { "Red" }
        "Empty"        { "DarkGray" }
        "Error"        { "Red" }
        default        { "White" }
    }
    
    $bar = ""
    $barLength = [math]::Floor($percentage / 2)
    if ($barLength -gt 0) {
        $bar = "█" * $barLength
    }
    
    Write-Host ("{0,-15} : {1,4} fichiers ({2,5}%) " -f $enc, $count, $percentage) -NoNewline -ForegroundColor $color
    Write-Host $bar -ForegroundColor $color
}

Write-Host ""

# Afficher par extension
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ENCODAGES PAR TYPE DE FICHIER" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

foreach ($ext in $encodingByExtension.Keys | Sort-Object) {
    $encodings = $encodingByExtension[$ext]
    $totalForExt = ($encodings.Values | Measure-Object -Sum).Sum
    
    Write-Host "$ext ($totalForExt fichiers) :" -ForegroundColor Yellow
    
    foreach ($enc in $encodings.Keys | Sort-Object) {
        $count = $encodings[$enc]
        $color = switch ($enc) {
            "UTF8"         { "Green" }
            "ASCII"        { "Yellow" }
            "UTF8-BOM"     { "Magenta" }
            "ANSI/Other"   { "Red" }
            default        { "White" }
        }
        Write-Host "  - $enc : $count" -ForegroundColor $color
    }
    Write-Host ""
}

# Liste détaillée si demandée
if ($Detailed -or $ShowOnly) {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  LISTE DÉTAILLÉE DES FICHIERS" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    $filesToShow = if ($ShowOnly) {
        $allFiles | Where-Object { $_.Encoding -eq $ShowOnly } | Sort-Object Path
    } else {
        $allFiles | Sort-Object Encoding, Extension, Path
    }
    
    if ($filesToShow.Count -eq 0) {
        Write-Host "Aucun fichier trouvé avec l'encodage '$ShowOnly'" -ForegroundColor Yellow
    }
    else {
        $currentEncoding = ""
        foreach ($file in $filesToShow) {
            if ($file.Encoding -ne $currentEncoding) {
                $currentEncoding = $file.Encoding
                Write-Host ""
                Write-Host "--- $currentEncoding ---" -ForegroundColor Cyan
            }
            
            $color = switch ($file.Encoding) {
                "UTF8"         { "Green" }
                "ASCII"        { "Yellow" }
                "UTF8-BOM"     { "Magenta" }
                "ANSI/Other"   { "Red" }
                default        { "White" }
            }
            
            $sizeKB = [math]::Round($file.Size / 1KB, 2)
            Write-Host "  $($file.Path) " -NoNewline -ForegroundColor $color
            Write-Host "($sizeKB KB)" -ForegroundColor DarkGray
        }
    }
    Write-Host ""
}

# Recommandations
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RECOMMANDATIONS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$needsAction = $false

if ($encodingStats.ContainsKey("ASCII") -and $encodingStats["ASCII"] -gt 0) {
    Write-Host "⚠ $($encodingStats['ASCII']) fichiers ASCII détectés" -ForegroundColor Yellow
    Write-Host "  → Utilisez convert-ascii-to-utf8.ps1 pour les convertir en UTF-8 explicite" -ForegroundColor White
    $needsAction = $true
}

if ($encodingStats.ContainsKey("UTF8-BOM") -and $encodingStats["UTF8-BOM"] -gt 0) {
    Write-Host "⚠ $($encodingStats['UTF8-BOM']) fichiers UTF-8 avec BOM détectés" -ForegroundColor Magenta
    Write-Host "  → Utilisez convert-from-ansi-to-utf8.ps1 pour retirer le BOM" -ForegroundColor White
    $needsAction = $true
}

if ($encodingStats.ContainsKey("ANSI/Other") -and $encodingStats["ANSI/Other"] -gt 0) {
    Write-Host "⚠ $($encodingStats['ANSI/Other']) fichiers ANSI/Other détectés" -ForegroundColor Red
    Write-Host "  → Utilisez convert-from-ansi-to-utf8.ps1 pour les convertir" -ForegroundColor White
    $needsAction = $true
}

if ($encodingStats.ContainsKey("UTF16-LE") -or $encodingStats.ContainsKey("UTF16-BE")) {
    $utf16Count = 0
    if ($encodingStats.ContainsKey("UTF16-LE")) { $utf16Count += $encodingStats["UTF16-LE"] }
    if ($encodingStats.ContainsKey("UTF16-BE")) { $utf16Count += $encodingStats["UTF16-BE"] }
    Write-Host "⚠ $utf16Count fichiers UTF-16 détectés" -ForegroundColor Red
    Write-Host "  → Conversion manuelle recommandée" -ForegroundColor White
    $needsAction = $true
}

if (-not $needsAction) {
    Write-Host "✓ Tous les fichiers sont correctement encodés en UTF-8 sans BOM!" -ForegroundColor Green
}

Write-Host ""

# Export CSV si demandé
if ($ExportCsv) {
    $csvPath = Join-Path $Path "encoding-audit-report.csv"
    $allFiles | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
    Write-Host "✓ Rapport exporté vers: $csvPath" -ForegroundColor Green
    Write-Host ""
}
else {
    Write-Host "💡 Utilisez -ExportCsv pour exporter les résultats en CSV" -ForegroundColor DarkGray
    Write-Host "💡 Utilisez -Detailed pour voir la liste complète des fichiers" -ForegroundColor DarkGray
    Write-Host "💡 Utilisez -ShowOnly ASCII pour voir uniquement les fichiers ASCII" -ForegroundColor DarkGray
    Write-Host ""
}
