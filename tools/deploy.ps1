#Requires -Version 5.1
<#
.SYNOPSIS
    Rebuilds and publishes a standalone Release copy of MindAttic Browser to
    C:\Apps\MindAttic.Browser\ — the same process Automata, JobHunt and KdpPublish use
    (tools\deploy.ps1), so a launch runs current source, including whatever
    MindAttic.Capture.WebView2/.Wpf version is newest in C:\LocalNuGet, rather than a stale build —
    unless a deployed instance is still open (possibly mid-recording), in which case it launches
    that build as-is rather than killing it.

.DESCRIPTION
    1. Asks any running deployed instance (one whose process path is under C:\Apps\MindAttic.Browser
       — never an instance you launched from source some other way) to close itself and waits up to
       30s, so a capture in progress gets the chance to export before the process goes away. If one
       is still open after that (or the exe is still locked), the rebuild is skipped entirely and
       this just launches the exe already there — a slightly stale build beats killing someone's
       open browser, maybe mid-recording, to chase freshness.
    2. Otherwise: removes bin/obj for the App + Tests projects (a clean rebuild, not an incremental
       one), then dotnet publish (Release, win-x64, framework-dependent single file) ->
       C:\Apps\MindAttic.Browser\.
    3. Writes C:\Apps\MindAttic.Browser\launch.bat, which calls this deploy.ps1 (by its baked-in
       source path) before every launch and forwards its own arguments to the exe — so
       `launch.bat <url|folder> -capture video` behaves exactly like calling the exe directly, just
       on the freshest build that was safe to make. If the source repo has moved, it warns and
       launches what is already deployed, still forwarding arguments.

.PARAMETER Launch
    After publishing, immediately run the deployed exe with any remaining arguments.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\deploy.ps1
    powershell -ExecutionPolicy Bypass -File tools\deploy.ps1 -Launch https://mindattic.com -capture video
#>
param(
    [switch]$Launch,
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$LaunchArgs = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoDir = Split-Path $PSScriptRoot   # tools\ -> MindAttic.Browser\
$proj    = Join-Path $repoDir 'src\MindAttic.Browser\MindAttic.Browser.csproj'
$out     = 'C:\Apps\MindAttic.Browser'
$exeName = 'MindAttic.Browser.exe'

$exe = Join-Path $out $exeName

# ── Ask deployed instances to close, gracefully ─────────────────────────────
# Only processes running from the deployed folder: a copy someone launched straight from a build
# output, or an older location, is none of this script's business and is left alone.
Write-Host ''
Write-Host '  Checking for a running deployed instance...' -ForegroundColor Yellow
$deployed = Get-Process 'MindAttic.Browser' -ErrorAction SilentlyContinue | Where-Object {
    try { $_.Path -and $_.Path.StartsWith("$out\", [StringComparison]::OrdinalIgnoreCase) } catch { $false }
}
$skipRebuild = $false
if ($deployed) {
    Write-Host "    Asking $(@($deployed).Count) instance(s) to close (up to 30s, so a capture in progress can export)..." -ForegroundColor DarkYellow
    foreach ($p in $deployed) { try { $p.CloseMainWindow() | Out-Null } catch {} }
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline -and ($deployed | Where-Object { -not $_.HasExited })) { Start-Sleep -Milliseconds 500 }
    $stillOpen = $deployed | Where-Object { -not $_.HasExited }
    if ($stillOpen) {
        Write-Host "    $(@($stillOpen).Count) instance(s) are still open; skipping the rebuild and launching the current build instead." -ForegroundColor Yellow
        $skipRebuild = $true
    } else {
        Write-Host '    Closed.' -ForegroundColor DarkGray
    }
} else {
    Write-Host '    Nothing running from here.' -ForegroundColor DarkGray
}

if (-not $skipRebuild) {
    # ── Clear build cache ────────────────────────────────────────────────────
    Write-Host ''
    Write-Host '  Clearing build cache (bin/obj)...' -ForegroundColor Cyan
    foreach ($rel in @('src\MindAttic.Browser', 'tests\MindAttic.Browser.Tests')) {
        $projRoot = Join-Path $repoDir $rel
        if (-not (Test-Path $projRoot)) { continue }
        foreach ($d in @((Join-Path $projRoot 'bin'), (Join-Path $projRoot 'obj'))) {
            if (Test-Path $d) {
                try { Remove-Item -Recurse -Force $d -ErrorAction Stop }
                catch { Write-Host "    (could not fully remove $d : $($_.Exception.Message))" -ForegroundColor DarkYellow }
            }
        }
    }

    # ── Publish ───────────────────────────────────────────────────────────────
    Write-Host ''
    Write-Host "  Publishing $proj" -ForegroundColor Cyan
    Write-Host "    -> $exe"

    dotnet publish $proj `
        --configuration Release `
        --runtime win-x64 `
        --self-contained false `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        --output $out | Out-Host

    if ($LASTEXITCODE -ne 0) {
        # A locked file (an instance that closed too slowly, or one this run didn't know about)
        # fails the same way a build error would. Either way: don't block — run what's there.
        Write-Host "Publish failed (exit $LASTEXITCODE); launching the existing build instead." -ForegroundColor Yellow
        $skipRebuild = $true
    } elseif (-not (Test-Path $exe)) {
        Write-Host "Publish completed but $exeName not found at $exe." -ForegroundColor Red
        exit 1
    }
}

if (-not (Test-Path $exe)) {
    Write-Host "No build has ever been published to $exe." -ForegroundColor Red
    exit 1
}

# ── Write self-redeploying launcher ─────────────────────────────────────────
# Always redeploys from source before launching, so a call can never run a stale build. Forwards
# %* so `launch.bat Hyperspace -capture video` reaches the exe exactly as typed.
$deployPs1Path = Join-Path $PSScriptRoot 'deploy.ps1'
$launchBatPath = Join-Path $out 'launch.bat'
@(
    '@echo off',
    'title MindAttic Browser',
    "set `"DEPLOY_PS1=$deployPs1Path`"",
    'if exist "%DEPLOY_PS1%" (',
    '    echo Redeploying latest build from source...',
    '    powershell -NoProfile -ExecutionPolicy Bypass -File "%DEPLOY_PS1%"',
    '    if errorlevel 1 echo Redeploy failed - launching whatever is already in this folder instead.',
    ') else (',
    '    echo Source repo not found at "%DEPLOY_PS1%" - launching existing build without redeploying.',
    ')',
    "start `"`" `"%~dp0$exeName`" %*"
) | Set-Content $launchBatPath -Encoding ascii

# ── Summary ────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '  Published successfully.' -ForegroundColor Green
Write-Host "    Exe    : $exe ($([math]::Round(((Get-Item $exe).Length / 1MB), 1)) MB)" -ForegroundColor Gray
Write-Host "    Launch : $launchBatPath" -ForegroundColor Gray
Write-Host ''

if ($Launch) {
    Write-Host '  Launching...' -ForegroundColor Cyan
    Start-Process $exe -ArgumentList $LaunchArgs
}
