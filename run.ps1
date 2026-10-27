#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes a standalone Release copy of MindAttic Browser to C:\Apps\MindAttic.Browser\ when the
    source (or the newest MindAttic.Capture package in C:\LocalNuGet) has changed, and optionally
    launches it. The one script for deploying and launching the Browser.

.DESCRIPTION
    Never prompts: agents and /deploy run it non-interactively. The interactive choice lives in
    the generated launcher C:\Apps\MindAttic.Browser\run.bat (the desktop shortcut target),
    which, when started with no arguments, opens a menu first:
      1) Deploy & Run - runs this script with -Force (always republishes, still subject to the
                        graceful-close rule below), then starts the exe.
      2) Run          - starts the already-deployed exe without calling this script at all
                        (falls through to Deploy & Run if nothing is deployed yet).
    Started with arguments (as MindAttic.Web's capture.ps1 does) it never prompts: it runs this
    script without -Force (republish only if changed), then starts the exe with those arguments,
    so `run.bat <url|folder> -capture video` behaves exactly like calling the exe directly. If
    the source repo has moved, or the redeploy fails, it launches what is already deployed,
    still forwarding arguments.

    Every run (published or not) rewrites that launcher when its text has changed, so an
    existing install picks up launcher changes without a republish.

    Without -Force this is a fast no-op unless the deployed exe is missing, a source
    file under this repo is newer than the deployed exe, or a MindAttic.Capture*.nupkg in
    C:\LocalNuGet is newer than it (the project floats to the newest Capture package, which an
    mtime check on source alone would miss).

    When it does publish:
    1. Asks any running deployed instance (one whose process path is under C:\Apps\MindAttic.Browser
       - never an instance you launched from source some other way) to close itself and waits up to
       30s, so a capture in progress gets the chance to export before the process goes away. If one
       is still open after that, the rebuild is skipped entirely - a slightly stale build beats
       killing someone's open browser, maybe mid-recording, to chase freshness.
    2. With -Force, removes bin/obj for the App + Tests projects (a clean rebuild).
    3. dotnet publish (Release, win-x64, framework-dependent single file) -> C:\Apps\MindAttic.Browser\.

.PARAMETER Force
    Republish with a clean bin/obj rebuild even if nothing looks changed.

.PARAMETER LauncherOnly
    Only (re)write the generated launcher and exit: no build, nothing closed.

.PARAMETER Launch
    After deploying, start the deployed exe with any remaining arguments.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File run.ps1
    powershell -ExecutionPolicy Bypass -File run.ps1 -Force
    powershell -ExecutionPolicy Bypass -File run.ps1 -Launch https://mindattic.com -capture video
    powershell -ExecutionPolicy Bypass -File run.ps1 -LauncherOnly
#>
param(
    [switch]$Force,
    [switch]$LauncherOnly,
    [switch]$Launch,
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$LaunchArgs = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoDir   = $PSScriptRoot
$proj      = Join-Path $repoDir 'src\MindAttic.Browser\MindAttic.Browser.csproj'
$out       = 'C:\Apps\MindAttic.Browser'
$exeName   = 'MindAttic.Browser.exe'
$exe       = Join-Path $out $exeName
$runBat    = Join-Path $out 'run.bat'
$localNuGet = 'C:\LocalNuGet'

$sourceExtensions = @('.cs', '.csproj', '.xaml', '.razor', '.cshtml', '.css', '.js', '.json',
                      '.props', '.targets', '.resx')
$skipDirs = @('bin', 'obj', '.git', '.vs', 'node_modules')

# Newest-first walk that never descends into build output or VCS folders.
function Find-NewerSource([string]$dir, [datetime]$since) {
    foreach ($item in Get-ChildItem -LiteralPath $dir -Force -ErrorAction SilentlyContinue) {
        if ($item.PSIsContainer) {
            if ($skipDirs -contains $item.Name) { continue }
            if (Find-NewerSource $item.FullName $since) { return $true }
        } elseif (($sourceExtensions -contains $item.Extension.ToLowerInvariant()) -and
                  $item.LastWriteTimeUtc -gt $since) {
            Write-Host "  Changed: $($item.FullName)" -ForegroundColor DarkGray
            return $true
        }
    }
    return $false
}

function Test-NeedsPublish {
    if (-not (Test-Path $exe)) { return $true }
    $exeTime = (Get-Item $exe).LastWriteTimeUtc
    if (Find-NewerSource $repoDir $exeTime) { return $true }
    $newerPkg = Get-ChildItem -Path $localNuGet -Filter 'MindAttic.Capture*.nupkg' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -gt $exeTime } | Select-Object -First 1
    if ($newerPkg) {
        Write-Host "  Newer package: $($newerPkg.FullName)" -ForegroundColor DarkGray
        return $true
    }
    return $false
}

# Writes the double-click launcher (only when its text changed, so a launcher that is running
# this script is not rewritten under itself) and removes the stale launchers it replaced. The
# menu lives there, not in this script, so the script never blocks.
function Write-Launcher {
    $runPs1Path = Join-Path $repoDir 'run.ps1'
    $lines = @(
        '@echo off',
        'title MindAttic Browser',
        "rem Generated by $runPs1Path - edits here are overwritten.",
        "set `"RUN_PS1=$runPs1Path`"",
        'rem Started with arguments (e.g. MindAttic.Web capture.ps1): no menu, never prompt.',
        'set "ARG1=%~1"',
        'if defined ARG1 goto auto',
        'echo.',
        'echo   MindAttic Browser',
        'echo     1^) Deploy ^& Run',
        'echo     2^) Run',
        'echo.',
        'choice /c 12 /n /m "  Choose 1 or 2: "',
        'if errorlevel 2 goto run',
        ':deploy',
        'if exist "%RUN_PS1%" (',
        '    powershell -NoProfile -ExecutionPolicy Bypass -File "%RUN_PS1%" -Force',
        '    if errorlevel 1 echo Redeploy failed - launching whatever is already in this folder instead.',
        ') else (',
        '    echo Source repo not found at "%RUN_PS1%" - launching existing build without redeploying.',
        ')',
        'goto launch',
        ':auto',
        'if exist "%RUN_PS1%" (',
        '    powershell -NoProfile -ExecutionPolicy Bypass -File "%RUN_PS1%"',
        '    if errorlevel 1 echo Redeploy failed - launching whatever is already in this folder instead.',
        ') else (',
        '    echo Source repo not found at "%RUN_PS1%" - launching existing build without redeploying.',
        ')',
        'goto launch',
        ':run',
        "if exist `"%~dp0$exeName`" goto launch",
        'echo Nothing is deployed yet - deploying first.',
        'goto deploy',
        ':launch',
        "if not exist `"%~dp0$exeName`" (",
        "    echo Nothing deployed at `"%~dp0$exeName`".",
        '    if not defined ARG1 pause',
        '    exit /b 1',
        ')',
        "start `"`" `"%~dp0$exeName`" %*"
    )
    # CRLF throughout: cmd mis-finds labels (goto) in LF-only batch files.
    $text = ($lines -join "`r`n") + "`r`n"
    $current = if (Test-Path $runBat) { [IO.File]::ReadAllText($runBat) } else { $null }
    if ($current -ne $text) { [IO.File]::WriteAllText($runBat, $text, [Text.Encoding]::ASCII) }
    # run.bat replaces the old generated launch.bat, then deploy.bat (renamed to run.bat).
    foreach ($old in 'launch.bat', 'deploy.bat') {
        $oldBat = Join-Path $out $old
        if (Test-Path $oldBat) { Remove-Item $oldBat -Force -ErrorAction SilentlyContinue }
    }
}

New-Item -ItemType Directory -Force -Path $out | Out-Null

if ($LauncherOnly) {
    Write-Launcher
    Write-Host "  Wrote $runBat" -ForegroundColor Gray
    exit 0
}

if (-not $Force -and -not (Test-NeedsPublish)) {
    Write-Host '  Up to date - nothing to publish.' -ForegroundColor DarkGray
} else {
    # ── Ask deployed instances to close, gracefully ─────────────────────────
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
            Write-Host "    $(@($stillOpen).Count) instance(s) are still open; skipping the rebuild and keeping the current build." -ForegroundColor Yellow
            $skipRebuild = $true
        } else {
            Write-Host '    Closed.' -ForegroundColor DarkGray
        }
    } else {
        Write-Host '    Nothing running from here.' -ForegroundColor DarkGray
    }

    if (-not $skipRebuild) {
        if ($Force) {
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
        }

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
            # fails the same way a build error would. Either way: don't block - run what's there.
            Write-Host "Publish failed (exit $LASTEXITCODE); keeping the existing build." -ForegroundColor Yellow
        } elseif (-not (Test-Path $exe)) {
            Write-Host "Publish completed but $exeName not found at $exe." -ForegroundColor Red
            exit 1
        } else {
            # An incremental publish can copy an unchanged bundle with its old timestamp; stamp it
            # so the next run's change check compares against this deploy.
            (Get-Item $exe).LastWriteTime = Get-Date
            Write-Host ''
            Write-Host '  Published successfully.' -ForegroundColor Green
            Write-Host "    Exe    : $exe ($([math]::Round(((Get-Item $exe).Length / 1MB), 1)) MB)" -ForegroundColor Gray
        }
    }

    if (-not (Test-Path $exe)) {
        Write-Host "No build has ever been published to $exe." -ForegroundColor Red
        exit 1
    }
    Write-Host "    Launch : $runBat" -ForegroundColor Gray
    Write-Host ''
}

Write-Launcher

if ($Launch) {
    Write-Host '  Launching...' -ForegroundColor Cyan
    if ($LaunchArgs.Count -gt 0) { Start-Process $exe -ArgumentList $LaunchArgs }
    else { Start-Process $exe }
}
exit 0
