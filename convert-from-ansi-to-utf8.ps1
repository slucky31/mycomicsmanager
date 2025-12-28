# Script pour convertir les fichiers ANSI/Windows-1252 en UTF-8 sans BOM
# Usage: .\convert-from-ansi-to-utf8.ps1
# Usage avec test: .\convert-from-ansi-to-utf8.ps1 -DryRun

param(
    [string]$Path = ".",
    [switch]$DryRun = $false,
    [switch]$ShowEncoding = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Conversion ANSI → UTF-8 sans BOM" -ForegroundColor Cyan
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
        
        # Vérifier si c'est du UTF-8 valide (sans BOM)
        try {
            $text = [System.Text.Encoding]::UTF8.GetString($bytes)
            $bytesBack = [System.Text.Encoding]::UTF8.GetBytes($text)
            
            # Si on peut faire l'aller-retour sans perte, c'est probablement UTF-8
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
                        return "ASCII"
                    }
                }
            }
        }
        catch {
            # Pas du UTF-8 valide
        }
        
        # Probablement ANSI/Windows-1252
        return "ANSI"
    }
    catch {
        return "Unknown"
    }
}

# Fonction pour convertir un fichier
function Convert-FileToUTF8NoBOM {
    param(
        [string]$FilePath,
        [string]$SourceEncoding,
        [switch]$DryRun
    )
    
    try {
        if ($SourceEncoding -eq "UTF8") {
            Write-Host " [Déjà UTF-8]" -ForegroundColor DarkGray -NoNewline
            return "skipped"
        }
        
        if ($SourceEncoding -eq "ASCII") {
            Write-Host " [ASCII - OK]" -ForegroundColor DarkGray -NoNewline
            return "skipped"
        }
        
        if ($SourceEncoding -eq "Empty") {
            Write-Host " [Vide]" -ForegroundColor DarkGray -NoNewline
            return "skipped"
        }
        
        if ($DryRun) {
            Write-Host " [$SourceEncoding → UTF-8]" -ForegroundColor Yellow -NoNewline
            return "would-convert"
        }
        
        # Déterminer l'encodage source
        $sourceEnc = switch ($SourceEncoding) {
            "ANSI"      { [System.Text.Encoding]::GetEncoding(1252) }  # Windows-1252
            "UTF8-BOM"  { [System.Text.Encoding]::UTF8 }
            "UTF16-LE"  { [System.Text.Encoding]::Unicode }
            "UTF16-BE"  { [System.Text.Encoding]::BigEndianUnicode }
            default     { [System.Text.Encoding]::GetEncoding(1252) }
        }
        
        # Lire avec l'encodage source
        $content = [System.IO.File]::ReadAllText($FilePath, $sourceEnc)
        
        # Écrire en UTF-8 sans BOM
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($FilePath, $content, $utf8NoBom)
        
        Write-Host " [$SourceEncoding → UTF-8] ✓" -ForegroundColor Green -NoNewline
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

$encodingStats = @{}

Write-Host "Analyse et conversion des fichiers..." -ForegroundColor Cyan
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
            if ($relativePath.Length -gt 80) {
                $relativePath = "..." + $relativePath.Substring($relativePath.Length - 77)
            }
            
            Write-Host "  $relativePath" -NoNewline
            
            # Détecter l'encodage
            $encoding = Get-FileEncoding -FilePath $file.FullName
            
            # Compter les encodages
            if (-not $encodingStats.ContainsKey($encoding)) {
                $encodingStats[$encoding] = 0
            }
            $encodingStats[$encoding]++
            
            if ($ShowEncoding) {
                Write-Host " [$encoding]" -ForegroundColor Cyan -NoNewline
            }
            
            # Convertir
            $result = Convert-FileToUTF8NoBOM -FilePath $file.FullName -SourceEncoding $encoding -DryRun:$DryRun
            
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
            
            $encoding = Get-FileEncoding -FilePath $file.FullName
            if (-not $encodingStats.ContainsKey($encoding)) {
                $encodingStats[$encoding] = 0
            }
            $encodingStats[$encoding]++
            
            if ($ShowEncoding) {
                Write-Host " [$encoding]" -ForegroundColor Cyan -NoNewline
            }
            
            $result = Convert-FileToUTF8NoBOM -FilePath $file.FullName -SourceEncoding $encoding -DryRun:$DryRun
            
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
Write-Host "  Résumé des encodages détectés" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

foreach ($enc in $encodingStats.Keys | Sort-Object) {
    $count = $encodingStats[$enc]
    $color = switch ($enc) {
        "UTF8"      { "Green" }
        "ASCII"     { "Green" }
        "ANSI"      { "Yellow" }
        "UTF8-BOM"  { "Yellow" }
        default     { "Red" }
    }
    Write-Host "$enc : $count fichiers" -ForegroundColor $color
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Résumé des opérations" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Fichiers analysés      : $($stats.Total)" -ForegroundColor White

if ($DryRun) {
    Write-Host "À convertir            : $($stats.WouldConvert)" -ForegroundColor Yellow
    Write-Host "Déjà corrects          : $($stats.Skipped)" -ForegroundColor Green
}
else {
    Write-Host "Fichiers convertis     : $($stats.Converted)" -ForegroundColor Green
    Write-Host "Déjà corrects          : $($stats.Skipped)" -ForegroundColor DarkGray
}

if ($stats.Errors -gt 0) {
    Write-Host "Erreurs                : $($stats.Errors)" -ForegroundColor Red
}
Write-Host ""

if ($DryRun) {
    Write-Host "⚠ MODE TEST - Aucun fichier n'a été modifié" -ForegroundColor Yellow
    Write-Host "  Exécutez sans -DryRun pour appliquer les modifications" -ForegroundColor Yellow
    Write-Host "  Ajoutez -ShowEncoding pour voir l'encodage de chaque fichier" -ForegroundColor Yellow
}
else {
    Write-Host "✓ Conversion terminée!" -ForegroundColor Green
    
    if ($stats.Converted -gt 0) {
        Write-Host ""
        Write-Host "Prochaines étapes recommandées :" -ForegroundColor Yellow
        Write-Host "  1. Vérifier que tout compile : dotnet build" -ForegroundColor White
        Write-Host "  2. Vérifier les caractères accentués dans vos fichiers" -ForegroundColor White
        Write-Host "  3. Tester votre application" -ForegroundColor White
        Write-Host "  4. Commiter les changements :" -ForegroundColor White
        Write-Host "     git add -A" -ForegroundColor DarkGray
        Write-Host "     git commit -m `"Convert all files from ANSI to UTF-8`"" -ForegroundColor DarkGray
    }
}

Write-Host ""
