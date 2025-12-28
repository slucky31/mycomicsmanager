# Script simple : Conversion Windows-1252 → UTF-8 sans BOM
# Usage: .\convert-windows1252-to-utf8.ps1

param(
    [string]$Path = ".",
    [switch]$DryRun = $false
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Conversion Windows-1252 → UTF-8 sans BOM" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "MODE TEST - Aucun fichier ne sera modifié" -ForegroundColor Yellow
    Write-Host ""
}

# Extensions à convertir
$extensions = @('*.cs', '*.razor', '*.cshtml', '*.json', '*.xml', '*.yml', '*.yaml', '*.md', '*.txt', '*.sql')

# Dossiers à ignorer
$excludeDirs = @('bin', 'obj', '.git', '.vs', 'node_modules', 'packages')

# Compteurs
$converted = 0
$skipped = 0
$errors = 0

Write-Host "Recherche des fichiers..." -ForegroundColor Cyan
Write-Host ""

foreach ($ext in $extensions) {
    $files = Get-ChildItem -Path $Path -Filter $ext -Recurse -File -ErrorAction SilentlyContinue |
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
        $relativePath = $file.FullName.Replace((Get-Item $Path).FullName, ".")
        
        try {
            # Lire avec l'encodage Windows-1252
            $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::GetEncoding(1252))
            
            if ($DryRun) {
                Write-Host "  [TEST] $relativePath" -ForegroundColor Yellow
                $converted++
            }
            else {
                # Écrire en UTF-8 sans BOM
                $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
                [System.IO.File]::WriteAllText($file.FullName, $content, $utf8NoBom)
                
                Write-Host "  ✓ $relativePath" -ForegroundColor Green
                $converted++
            }
        }
        catch {
            Write-Host "  ✗ $relativePath - Erreur: $($_.Exception.Message)" -ForegroundColor Red
            $errors++
        }
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Résumé" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "Fichiers à convertir : $converted" -ForegroundColor Yellow
}
else {
    Write-Host "Fichiers convertis   : $converted" -ForegroundColor Green
}

if ($errors -gt 0) {
    Write-Host "Erreurs              : $errors" -ForegroundColor Red
}

Write-Host ""

if ($DryRun) {
    Write-Host "Pour lancer la conversion réelle, exécutez :" -ForegroundColor Yellow
    Write-Host "  .\convert-windows1252-to-utf8.ps1" -ForegroundColor White
}
else {
    Write-Host "✓ Conversion terminée !" -ForegroundColor Green
    Write-Host ""
    Write-Host "Prochaines étapes :" -ForegroundColor Cyan
    Write-Host "  1. Vérifier la compilation : dotnet build" -ForegroundColor White
    Write-Host "  2. Vérifier les caractères accentués" -ForegroundColor White
    Write-Host "  3. Commiter : git add -A && git commit -m 'Convert to UTF-8'" -ForegroundColor White
}

Write-Host ""
