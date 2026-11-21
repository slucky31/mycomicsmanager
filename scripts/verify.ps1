#Requires -Version 7
<#
.SYNOPSIS
    Runs the checks of the CI locally and exits with code 2 at the first failure.

.DESCRIPTION
    -Fast      Build with warnings as errors, unit tests with coverage, coverage of the changed lines.
    (default)  -Fast, plus the integration tests (when ConnectionStrings__NeonConnectionUnitTests is set),
               the duplication check (jscpd) and the secret scan of the branch commits (Betterleaks).
    -Mutation  Everything, plus Stryker on the code changed since origin/main.
    -FromHook  Called by the Claude Code Stop hook: reads the hook JSON on stdin and exits 0 at once when
               the hook already blocked this turn (stop_hook_active) or when no C# file changed.

    The failed command and the end of its output go to stderr: the Stop hook hands them back to the agent.

.EXAMPLE
    dotnet tool run pwsh scripts/verify.ps1 -Fast
#>
[CmdletBinding()]
param(
    [switch] $Fast,
    [switch] $Mutation,
    [switch] $FromHook
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$unitTestProjects = @(
    'tests/Domain.UnitTests/Domain.UnitTests.csproj',
    'tests/Application.UnitTests/Application.UnitTests.csproj',
    'tests/Web.Tests/Web.Tests.csproj',
    'tests/Architecture.Tests/Architecture.Tests.csproj'
)
$integrationTestProjects = @('tests/Persistence.Integration.Tests/Persistence.Tests.csproj')
$codeFilePattern = '\.(cs|razor|csproj|props)$'
$resultsDirectory = Join-Path $root 'test-results'

function Get-BaseRef {
    git rev-parse --verify --quiet origin/main *> $null
    if ($LASTEXITCODE -eq 0) { return 'origin/main' }
    return $null
}

function Get-ChangedFiles {
    $files = @(git status --porcelain | ForEach-Object { $_.Substring(3) })
    $base = Get-BaseRef
    if ($base) {
        $files += @(git diff --name-only "$base...HEAD")
    }
    return $files | Where-Object { $_ } | Sort-Object -Unique
}

function Stop-Verification([string] $Command, [string[]] $Output) {
    $errors = @($Output | Where-Object { $_ -match '(?i)\b(error|failed|failure)\b' } | Select-Object -Unique -First 30)
    $summary = if ($errors.Count -gt 0) { $errors } else { @($Output | Select-Object -Last 30) }
    [Console]::Error.WriteLine("verify.ps1: '$Command' failed.")
    $summary | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 2
}

function Invoke-Step([string] $Name, [string] $Command, [string[]] $Arguments) {
    Write-Host "==> $Name" -ForegroundColor Cyan
    # Native commands write progress on stderr: it must not stop the script.
    $ErrorActionPreference = 'Continue'
    $output = & $Command @Arguments 2>&1 | ForEach-Object {
        $line = "$_"
        if (-not $FromHook) { Write-Host $line }
        $line
    }
    if ($LASTEXITCODE -ne 0) {
        Stop-Verification "$Command $($Arguments -join ' ')" $output
    }
}

function Test-Tool([string] $Name) {
    return [bool] (Get-Command $Name -ErrorAction SilentlyContinue)
}

if ($FromHook) {
    $hookInput = [Console]::In.ReadToEnd()
    if ($hookInput) {
        $hook = $hookInput | ConvertFrom-Json
        # Already blocked once in this turn: let the agent stop instead of looping on a fault it cannot fix.
        if ($hook.PSObject.Properties['stop_hook_active'] -and $hook.stop_hook_active) { exit 0 }
    }
    if (-not (Get-ChangedFiles | Where-Object { $_ -match $codeFilePattern })) { exit 0 }
}

Invoke-Step 'Build (warnings as errors)' 'dotnet' @('build', 'MyComicsManager.slnx', '-c', 'Release', '-warnaserror', '-nologo')

if (Test-Path $resultsDirectory) { Remove-Item $resultsDirectory -Recurse -Force }
$testProjects = $unitTestProjects
if (-not $Fast) {
    if ($env:ConnectionStrings__NeonConnectionUnitTests) {
        $testProjects += $integrationTestProjects
    }
    else {
        Write-Host 'Integration tests skipped: ConnectionStrings__NeonConnectionUnitTests is not set.' -ForegroundColor Yellow
    }
}
foreach ($project in $testProjects) {
    Invoke-Step "Tests: $project" 'dotnet' @('test', '--project', $project, '-c', 'Release', '--no-build',
        '--coverage', '--coverage-output-format', 'cobertura', '--results-directory', $resultsDirectory)
}

$base = Get-BaseRef
if (-not $base) {
    Write-Host 'Coverage of the changed lines skipped: no origin/main to compare with.' -ForegroundColor Yellow
}
elseif (-not (Test-Tool 'pipx')) {
    Write-Host 'Coverage of the changed lines skipped: pipx is not installed.' -ForegroundColor Yellow
}
else {
    $reports = @(Get-ChildItem $resultsDirectory -Recurse -Filter '*.cobertura.xml' | ForEach-Object FullName)
    Invoke-Step 'Coverage of the changed lines' 'pipx' (@('run', '--spec', 'diff-cover==11.0.0', 'diff-cover') + $reports +
        @("--compare-branch=$base", '--fail-under=80', '--exclude', '*.razor', '*.razor.cs'))
}

if (-not $Fast) {
    if (Test-Tool 'npx') {
        Invoke-Step 'Duplication (jscpd)' 'npx' @('--yes', 'jscpd@5.4.2', '.')
    }
    else {
        Write-Host 'Duplication check skipped: npx is not installed.' -ForegroundColor Yellow
    }

    if (-not (Test-Tool 'betterleaks')) {
        Write-Host 'Secret scan skipped: betterleaks is not installed (https://github.com/betterleaks/betterleaks).' -ForegroundColor Yellow
    }
    elseif ($base) {
        Invoke-Step 'Secret scan (Betterleaks)' 'betterleaks' @('git', '.', "--log-opts=$base..HEAD", '--redact=100', '--no-banner', '--no-color')
    }
}

if ($Mutation) {
    if (-not $base) {
        Stop-Verification 'dotnet stryker --since' @('Mutation testing needs origin/main to compare with.')
    }
    Invoke-Step 'Restore the tools' 'dotnet' @('tool', 'restore')
    $config = Join-Path $root 'stryker-config.json'
    $mutated = @(
        @{ Project = 'Domain'; Folder = 'tests/Domain.UnitTests'; Tests = @() },
        @{ Project = 'Application'; Folder = 'tests/Application.UnitTests'; Tests = @() },
        # The Persistence services are tested in Application.UnitTests.
        @{ Project = 'Persistence'; Folder = 'tests/Persistence.Integration.Tests'; Tests = @('Persistence.Tests.csproj', '../Application.UnitTests/Application.UnitTests.csproj') }
    )
    foreach ($target in $mutated) {
        if ($target.Project -eq 'Persistence' -and -not $env:ConnectionStrings__NeonConnectionUnitTests) {
            Write-Host 'Stryker on Persistence skipped: ConnectionStrings__NeonConnectionUnitTests is not set.' -ForegroundColor Yellow
            continue
        }
        # Stryker exits silently when started from the repository root: it runs from the test folder.
        Push-Location $target.Folder
        try {
            $arguments = @('stryker', '--config-file', $config, '--project', "$($target.Project).csproj", "--since:$base")
            foreach ($test in $target.Tests) { $arguments += @('--test-project', $test) }
            Invoke-Step "Mutation testing: $($target.Project)" 'dotnet' $arguments
        }
        finally {
            Pop-Location
        }
    }
}

Write-Host 'verify.ps1: every check passed.' -ForegroundColor Green
exit 0
