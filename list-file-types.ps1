# Script pour lister tous les types de fichiers dans une solution
# Usage: .\list-file-types.ps1
# Usage avec chemin: .\list-file-types.ps1 -Path "C:\MonProjet"

param(
    [string]$Path = ".",
    [switch]$IncludeHidden = $false,
    [switch]$ExcludeCommon = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Analyse des types de fichiers" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Dossiers à exclure par défaut
$excludeDirs = @(
    'bin',
    'obj',
    '.git',
    '.vs',
    'node_modules',
    'packages',
    '.idea',
    'TestResults',
    'BenchmarkDotNet.Artifacts'
)

Write-Host "Chemin analysé: $((Get-Item $Path).FullName)" -ForegroundColor Yellow
Write-Host "Dossiers exclus: $($excludeDirs -join ', ')" -ForegroundColor Yellow
Write-Host ""

# Récupérer tous les fichiers
$allFiles = Get-ChildItem -Path $Path -File -Recurse -Force:$IncludeHidden -ErrorAction SilentlyContinue |
    Where-Object {
        $file = $_
        # Exclure les dossiers spécifiques
        $shouldExclude = $false
        foreach ($dir in $excludeDirs) {
            if ($file.FullName -like "*\$dir\*") {
                $shouldExclude = $true
                break
            }
        }
        -not $shouldExclude
    }

# Grouper par extension
$filesByExtension = $allFiles | Group-Object Extension | Sort-Object Count -Descending

Write-Host "Nombre total de fichiers: $($allFiles.Count)" -ForegroundColor Green
Write-Host "Nombre de types différents: $($filesByExtension.Count)" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Afficher les résultats
$results = @()

foreach ($group in $filesByExtension) {
    $extension = if ($group.Name) { $group.Name } else { "(sans extension)" }
    $count = $group.Count
    
    # Calculer la taille totale
    $totalSize = ($group.Group | Measure-Object -Property Length -Sum).Sum
    $sizeInMB = [math]::Round($totalSize / 1MB, 2)
    
    # Obtenir quelques exemples de fichiers
    $examples = ($group.Group | Select-Object -First 3 | ForEach-Object { $_.Name }) -join ", "
    if ($group.Count -gt 3) {
        $examples += "..."
    }
    
    $results += [PSCustomObject]@{
        Extension = $extension
        Nombre = $count
        'Taille (MB)' = $sizeInMB
        Exemples = $examples
    }
}

# Afficher sous forme de tableau
$results | Format-Table -AutoSize -Wrap

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Statistiques par catégorie" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Catégoriser les fichiers
$categories = @{
    'Code Source' = @('.cs', '.razor', '.cshtml', '.vb', '.fs')
    'Projets/Solutions' = @('.csproj', '.sln', '.vbproj', '.fsproj', '.shproj')
    'Configuration' = @('.json', '.xml', '.config', '.yml', '.yaml', '.env', '.editorconfig', '.gitignore', '.gitattributes')
    'Web/Frontend' = @('.js', '.ts', '.jsx', '.tsx', '.css', '.scss', '.sass', '.html', '.htm')
    'Documentation' = @('.md', '.txt', '.pdf', '.docx')
    'Données' = @('.sql', '.csv', '.xlsx', '.db', '.sqlite')
    'Images' = @('.png', '.jpg', '.jpeg', '.gif', '.svg', '.ico', '.bmp')
    'Ressources' = @('.resx', '.resources', '.resw')
    'Tests' = @('.dll', '.pdb')
}

foreach ($category in $categories.Keys) {
    $categoryFiles = $allFiles | Where-Object { $categories[$category] -contains $_.Extension }
    $count = $categoryFiles.Count
    
    if ($count -gt 0) {
        $totalSize = ($categoryFiles | Measure-Object -Property Length -Sum).Sum
        $sizeInMB = [math]::Round($totalSize / 1MB, 2)
        
        Write-Host "$category : " -NoNewline -ForegroundColor Yellow
        Write-Host "$count fichiers ($sizeInMB MB)" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Top 5 des fichiers les plus volumineux" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$allFiles | 
    Sort-Object Length -Descending | 
    Select-Object -First 5 |
    ForEach-Object {
        $sizeInMB = [math]::Round($_.Length / 1MB, 2)
        $relativePath = $_.FullName.Replace((Get-Item $Path).FullName, ".")
        Write-Host "$sizeInMB MB - " -NoNewline -ForegroundColor Cyan
        Write-Host "$relativePath" -ForegroundColor White
    }

Write-Host ""

# Option pour exporter en CSV
Write-Host "Voulez-vous exporter les résultats en CSV? (O/N)" -ForegroundColor Yellow
$export = Read-Host

if ($export -eq 'O' -or $export -eq 'o') {
    $csvPath = Join-Path $Path "file-types-report.csv"
    $results | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
    Write-Host "✓ Exporté vers: $csvPath" -ForegroundColor Green
}

Write-Host ""
Write-Host "Analyse terminée!" -ForegroundColor Green
