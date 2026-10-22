---
codex: 1
project: MindAttic.Browser
code: M.BRWS
layer: stories
status: living
updated: 2026-10-12
---
<!-- GENERATED from MindAttic.Tickets/data/Tickets.db by `tickets db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the tickets CLI. -->

# MindAttic.Browser — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Priority backlog
_Nothing recorded yet._

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| M.BRWS-T1 | 2026-10-12 | docs | closed | Start the permanent ticket log in USER_STORIES.md | — |
| M.BRWS-T2 | 2026-10-12 | feature | closed | Use assets/browser.ico as the app icon (exe and every window), add the redeploy launch.bat | src/MindAttic.Browser/MindAttic.Browser.csproj |
| M.BRWS-T3 | 2026-10-13 | chore | closed | Move tools\deploy.ps1 to root deploy.ps1: rebuild only on change, -Force/-Launch, generate C:\Apps\MindAttic.Browser\deploy.bat (replaces launch.bat) | deploy.ps1 |
| M.BRWS-T4 | 2026-10-14 | chore | open | NUnit tests for BrowserTarget.Parse/NormalizeUrl/HostName edge cases (src/MindAttic.Browser/BrowserTarget.cs) not yet in BrowserTests.cs: -capture-config consumes the next arg only without '=', --host=value form, a folder with no index.htm/html opens its root, a .txt file is treated as a URL/search, HostName turns spaces/non-ASCII into '-' and an all-symbol name into 'site.local', 127.0.0.1:port gets http://, blank text goes Home - a wrong parse opens the wrong page or virtual host | — |
| M.BRWS-T5 | 2026-10-16 | chore | closed | install.ps1 adds this repo's 15 .prose/commands stubs for the shared global commands, so any LLM client finds them | mindattic-agent-standard/install.ps1 run 2026-10-16 |
| M.BRWS-T6 | 2026-10-16 | chore | open | Bug hunt: run an exhaustive /bughunt over the whole repo and fix every real bug found (each fix under its own fix ticket), then hunt again; close this only when a full pass finds nothing | user request 2026-10-16: approved repo sweep |
| M.BRWS-T7 | 2026-10-16 | chore | open | Run the full unit test suite and fix every failure (each under its own fix ticket) until it passes; put the passing run in --ref. No test project: close this wont-do saying so | user request 2026-10-16: approved repo sweep |
| M.BRWS-T8 | 2026-10-16 | chore | open | Clean up code and docs: remove dead code, stale comments and outdated or duplicated docs, and simplify where behaviour doesn't change (/simplify); tests stay green | user request 2026-10-16: approved repo sweep |
| M.BRWS-T9 | 2026-10-16 | docs | open | Bring README.md up to date with the code as it is now (what it is, setup, usage, commands, current state), following codex-standard/README-STYLE.md; README.htm regenerated if the repo has one | user request 2026-10-16: approved repo sweep |
| M.BRWS-T10 | 2026-10-16 | feature | open | Log to MindAttic.Log (3.0.0): every app/CLI entry point writes to the standalone MindAttic.Log.db Vault assigns this app (VaultPaths.LoggingDbPath, MindAttic.Vault 6.0.0) through Serilog's WriteToMindAtticLog, tagged with its Component; log startup, user actions that change data, failures and unhandled exceptions (UI thread, AppDomain, unobserved tasks); tests point the log at a temp file. Pattern: MindAttic.Workflow WorkflowLog (M.FLOW-T92), MindAttic.Orchestrator OrchestratorLog | M.FLOW-T92 |
| M.BRWS-T11 | 2026-10-16 | docs | closed | Update USER_STORIES.md | — |
| M.BRWS-T12 | 2026-10-18 | feature | closed | Add Ctrl+H browsing history: recorded per visit, viewable in a History window, cleared in widening buckets | — |
| M.BRWS-T13 | 2026-10-18 | feature | closed | Add Ask Claude (AutoWebNav): a chat panel that drives the live browser pane via an agentic Operator tool-use loop | — |
| M.BRWS-T14 | 2026-10-19 | chore | wont-do | Convert tracked source files to LF line endings via .gitattributes, per HOUSE-LAW-13 (low priority) | — |
| M.BRWS-T15 | 2026-10-20 | docs | open | Publish MindAttic.Browser docs to GitHub Pages via the shared Toolkit DocFX pipeline: run adopt-docs-site.ps1 (README.md as home page, docs/*.md as articles, API reference only where the code builds in CI), enable Pages with build_type=workflow, set the repo homepage URL, retire tools/build-readme.ps1 and README.htm, confirm the docs Actions run is green and https://mindattic.github.io/MindAttic.Browser/ returns 200. Depends on MAT-T8. | — |
| M.BRWS-T16 | 2026-10-20 | chore | closed | Move to MindAttic.Components.Wpf 19.0.0 (Mc->Ma rename): bump the PackageReference and rename every Mc-prefixed style/class reference to Ma across AutoWebNavWindow.xaml, CropWindow.xaml, HistoryWindow.xaml, MainWindow.xaml(.cs) and SettingsWindow.xaml (~53 occurrences) | 8dbe96e |
| M.BRWS-T17 | 2026-10-22 | docs | closed | Update USER_STORIES.md | — |
