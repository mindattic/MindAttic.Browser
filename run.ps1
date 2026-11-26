#Requires -Version 5.1
<#
.SYNOPSIS
    MindAttic.Browser's run menu: 1) Redeploy  2) Run Debug  3) Run Release  (the MindAttic standard, MAT-T279).

.DESCRIPTION
    A thin wrapper over the shared deploy-lib.ps1 (Invoke-AppRun, MAT-T301). run.bat beside it (written by
    deploy-lib.ps1's Write-RunBat) opens the same menu on a double-click.
      1) Redeploy     builds the Browser to R:\MindAttic\Apps\MindAttic.Browser\{Debug,Release}
                      Debug   framework-dependent (fast rebuilds, the dev/test copy)
                      Release self-contained single-file win-x64 (no .NET runtime needed)
      2) Run Debug    R:\...\Debug\MindAttic.Browser.exe in this console; extra arguments (a URL or site folder,
                      -capture video) pass through
      3) Run Release  R:\...\Release\MindAttic.Browser.exe, started the way a double-click would start it
    Picks combine: 13 is Deploy & Run. Tools that open a page (MindAttic.Web's capture.ps1, Tickets' `view`,
    MindAttic.Media.Capture) start R:\MindAttic\Apps\MindAttic.Browser\Release\MindAttic.Browser.exe directly.

    The Browser's own differences, in the override block below:
      - PreBuild asks a running copy of that config to close and waits up to 30s, so a capture in progress can
        export; if it is still open, that config's build is skipped and the deployed copy kept.
      - PostBuild runs the exe with --write-settings-manifest so MindAttic Vault shows this build's settings, and
        after the Release build points any Desktop or Start-menu MindAttic.Browser shortcut still aimed at the old
        C:\Apps\MindAttic.Browser install at the Release exe.

    Callers from before the shared menu: -Launch means -Redeploy -RunRelease.

.EXAMPLE
    .\run.ps1                    the menu
    .\run.ps1 -RunDebug https://mindattic.com -capture video
    .\run.ps1 -Redeploy -RunRelease -DryRun
#>
param(
    [switch]$Redeploy,
    [switch]$RunDebug,
    [switch]$RunRelease,
    [switch]$DryRun,
    [switch]$Launch,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$AppArgs = @()
)
$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$Override = @{
    Exe       = 'MindAttic.Browser.exe'
    Plan      = @{ Kind = 'dotnet'; Projects = @("$repoRoot\src\MindAttic.Browser\MindAttic.Browser.csproj") }
    PreBuild  = {
        param($Config, $Out)
        $target = Get-DeployTarget -RepoName 'MindAttic.Browser' -Config $Config
        $running = @(Get-Process 'MindAttic.Browser' -ErrorAction SilentlyContinue | Where-Object {
            try { $_.Path -and $_.Path.StartsWith("$target\", [StringComparison]::OrdinalIgnoreCase) } catch { $false }
        })
        if (-not $running) { return }
        Write-Host "  Asking $($running.Count) running $Config instance(s) to close (up to 30s, so a capture in progress can export)..." -ForegroundColor DarkYellow
        foreach ($p in $running) { try { $p.CloseMainWindow() | Out-Null } catch {} }
        $deadline = (Get-Date).AddSeconds(30)
        while ((Get-Date) -lt $deadline -and ($running | Where-Object { -not $_.HasExited })) { Start-Sleep -Milliseconds 500 }
        if ($running | Where-Object { -not $_.HasExited }) {
            throw "MindAttic.Browser $Config is still open in $target; kept that build rather than close it mid-capture."
        }
    }
    PostBuild = {
        param($Config, $Out)
        $exe = Join-Path $Out 'MindAttic.Browser.exe'
        $manifest = Start-Process -FilePath $exe -ArgumentList '--write-settings-manifest' -WorkingDirectory $Out -Wait -PassThru -WindowStyle Hidden
        if ($manifest.ExitCode -ne 0) { Write-Host "  (--write-settings-manifest exited $($manifest.ExitCode))" -ForegroundColor DarkYellow }
        if ($Config -ne 'Release') { return }

        $shell = New-Object -ComObject WScript.Shell
        foreach ($dir in [Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs')) {
            Get-ChildItem -LiteralPath $dir -Filter 'MindAttic*Browser*.lnk' -File -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
                $lnk = $shell.CreateShortcut($_.FullName)
                if ($lnk.TargetPath -notlike 'C:\Apps\MindAttic.Browser\*') { return }
                $lnk.TargetPath = $exe
                $lnk.Arguments = ''
                $lnk.WorkingDirectory = $Out
                $lnk.IconLocation = "$exe,0"
                $lnk.Save()
                Write-Host "  Shortcut $($_.FullName) -> $exe" -ForegroundColor DarkGray
            }
        }
    }
}

function Find-DeployLib {
    if ($env:MINDATTIC_DEPLOY_LIB -and (Test-Path -LiteralPath $env:MINDATTIC_DEPLOY_LIB)) { return $env:MINDATTIC_DEPLOY_LIB }
    $roots = @()
    $d = $PSScriptRoot
    while ($d) { $roots += $d; $d = Split-Path -Parent $d }
    $roots += 'D:\Projects\MindAttic'
    foreach ($r in $roots) {
        foreach ($rel in 'MindAttic.Agency\shared\deploy-lib.ps1', 'mindattic-agent-standard\shared\deploy-lib.ps1') {
            $p = Join-Path $r $rel
            if (Test-Path -LiteralPath $p) { return $p }
        }
    }
    throw 'deploy-lib.ps1 not found: clone MindAttic.Agency beside this repo, or set MINDATTIC_DEPLOY_LIB to its path.'
}

. (Find-DeployLib)
Invoke-AppRun -Repo $PSScriptRoot -Redeploy:($Redeploy -or $Launch) -RunDebug:$RunDebug -RunRelease:($RunRelease -or $Launch) `
    -DryRun:$DryRun -AppArgs $AppArgs -Override $Override
