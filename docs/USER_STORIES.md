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
| M.BRWS-T4 | 2026-10-14 | chore | wont-do | NUnit tests for BrowserTarget.Parse/NormalizeUrl/HostName edge cases (src/MindAttic.Browser/BrowserTarget.cs) not yet in BrowserTests.cs: -capture-config consumes the next arg only without '=', --host=value form, a folder with no index.htm/html opens its root, a .txt file is treated as a URL/search, HostName turns spaces/non-ASCII into '-' and an all-symbol name into 'site.local', 127.0.0.1:port gets http://, blank text goes Home - a wrong parse opens the wrong page or virtual host | portfolio reset: audit nice-to-have; no reported misparse |
| M.BRWS-T5 | 2026-10-16 | chore | closed | install.ps1 adds this repo's 15 .prose/commands stubs for the shared global commands, so any LLM client finds them | mindattic-agent-standard/install.ps1 run 2026-10-16 |
| M.BRWS-T6 | 2026-10-16 | chore | closed | Bug hunt: run an exhaustive /bughunt over the whole repo and fix every real bug found (each fix under its own fix ticket), then hunt again; close this only when a full pass finds nothing | 144684b2151e620bea8eb082c8a2a76de979d9f7 |
| M.BRWS-T7 | 2026-10-16 | chore | wont-do | Run the full unit test suite and fix every failure (each under its own fix ticket) until it passes; put the passing run in --ref. No test project: close this wont-do saying so | portfolio reset: repo-sweep test loop; tests run only on explicit owner request |
| M.BRWS-T8 | 2026-10-16 | chore | wont-do | Clean up code and docs: remove dead code, stale comments and outdated or duplicated docs, and simplify where behaviour doesn't change (/simplify); tests stay green | portfolio reset: generic cleanup, audit nice-to-have |
| M.BRWS-T9 | 2026-10-16 | docs | open | README.md presents MindAttic.Browser 1.0 as released: status badge from work-in-progress to live, screenshots (browser.png, crop-lightbox.png) retaken from the current Release build, and a short section on the AutoWebNav operator panel; follows codex-standard/README-STYLE.md | user request 2026-10-16: approved repo sweep |
| M.BRWS-T10 | 2026-10-16 | feature | open | Log to MindAttic.Log (3.0.0): every app/CLI entry point writes to the standalone MindAttic.Log.db Vault assigns this app (VaultPaths.LoggingDbPath, MindAttic.Vault 6.0.0) through Serilog's WriteToMindAtticLog, tagged with its Component; log startup, user actions that change data, failures and unhandled exceptions (UI thread, AppDomain, unobserved tasks); tests point the log at a temp file. Pattern: MindAttic.Workflow WorkflowLog (M.FLOW-T92), MindAttic.Orchestrator OrchestratorLog | M.FLOW-T92 |
| M.BRWS-T11 | 2026-10-16 | docs | closed | Update USER_STORIES.md | — |
| M.BRWS-T12 | 2026-10-18 | feature | closed | Add Ctrl+H browsing history: recorded per visit, viewable in a History window, cleared in widening buckets | — |
| M.BRWS-T13 | 2026-10-18 | feature | closed | Add Ask Claude (AutoWebNav): a chat panel that drives the live browser pane via an agentic Operator tool-use loop | — |
| M.BRWS-T14 | 2026-10-19 | chore | wont-do | Convert tracked source files to LF line endings via .gitattributes, per HOUSE-LAW-13 (low priority) | — |
| M.BRWS-T15 | 2026-10-20 | docs | closed | Publish MindAttic.Browser docs to GitHub Pages via the shared Agency DocFX pipeline: run adopt-docs-site.ps1 (README.md as home page, docs/*.md as articles, API reference only where the code builds in CI), enable Pages with build_type=workflow, set the repo homepage URL, retire tools/build-readme.ps1 and README.htm, confirm the docs Actions run is green and https://mindattic.github.io/MindAttic.Browser/ returns 200. Depends on MAT-T8. | — |
| M.BRWS-T16 | 2026-10-20 | chore | closed | Move to MindAttic.Components.Wpf 19.0.0 (Mc->Ma rename): bump the PackageReference and rename every Mc-prefixed style/class reference to Ma across AutoWebNavWindow.xaml, CropWindow.xaml, HistoryWindow.xaml, MainWindow.xaml(.cs) and SettingsWindow.xaml (~53 occurrences) | 8dbe96e |
| M.BRWS-T17 | 2026-10-22 | docs | closed | Update USER_STORIES.md | — |
| M.BRWS-T18 | 2026-10-24 | docs | closed | Update USER_STORIES.md | — |
| M.BRWS-T19 | 2026-10-30 | feature | wont-do | Extract MindAttic.Browser's settings into a Vault Dashboard settings-tab package | superseded by Epic E redesign: target MindAttic.Vault.Desktop, not Vault Dashboard |
| M.BRWS-T20 | 2026-10-30 | chore | wont-do | Remove MindAttic.Browser's own settings page and wire its cog to open Vault Dashboard instead | superseded by Epic E redesign: target MindAttic.Vault.Desktop, not Vault Dashboard |
| M.BRWS-T21 | 2026-10-30 | feature | wont-do | Extract MindAttic.Browser's settings into a native WPF settings-tab package for Desktop | withdrawn: plan rejected by owner 2026-10-31, see M.VALT-T26 |
| M.BRWS-T22 | 2026-10-30 | chore | wont-do | Remove MindAttic.Browser's own settings page/window, wire its cog to MindAttic.Vault.Desktop | withdrawn: plan rejected by owner 2026-10-31, see M.VALT-T26 |
| M.BRWS-T23 | 2026-10-31 | feature | closed | LAMP: move MindAttic.Browser's app-level settings and keys to MindAttic.Vault.Desktop (manifest, LoadOrSeed, shared named keys, cog opens Vault) | 9c9823e LampSettingsTests (38/38) |
| M.BRWS-T24 | 2026-10-31 | fix | closed | A Vault edit to the Browser's toolbar buttons (pinnedActions) doesn't apply to the open window: RefreshSettingsInto swaps the PinnedActions list, but the cog menu's IsPinned delegate still reads the old list, so the toolbar and the menu's pin checks stay stale (found verifying M.VALT-T73) | b857d34 LampSettingsTests.Setting_PinnedActions_VaultEditReachesThePredicateTheOpenWindowBoundAtStartup |
| M.BRWS-T25 | 2026-10-31 | fix | closed | run.ps1 doesn't run the published exe with --write-settings-manifest (LAMP playbook step 7), so after a redeploy MindAttic Vault keeps showing the old settings manifest until the app next runs (found verifying M.VALT-T73) | e3a54cb LampSettingsTests.RunPs1_WritesTheSettingsManifestAfterPublishing |
| M.BRWS-T26 | 2026-10-31 | chore | closed | Vault tab named after the repo: SettingsApp DisplayName 'MindAttic Browser' -> 'MindAttic.Browser' | M.VALT-T75 bd3b631 |
| M.BRWS-T27 | 2026-10-31 | docs | closed | Update USER_STORIES.md | — |
| M.BRWS-T28 | 2026-11-02 | docs | closed | Adopt MindAttic.Agency docs-site pipeline, publish README to GitHub Pages (HOUSE-LAW-14) | — |
| M.BRWS-T29 | 2026-11-02 | fix | closed | Bughunt sweep, two passes: find and fix real bugs across tracked source (user request 2026-11-02) | — |
| M.BRWS-T30 | 2026-11-02 | fix | closed | Fix the open items the 2026-11-02 bughunt left for decision (per-monitor crop lightbox, serialize browser saves vs Vault edits) | — |
| M.BRWS-T31 | 2026-11-03 | fix | closed | Adopt MindAttic.Vault 8.0.0 TryLoad/LoadStrict in the settings refresh/save path (replaces the M.BRWS-T30 last-good-state fallback) so a failed read is detected explicitly | — |
| M.BRWS-T32 | 2026-11-10 | chore | closed | Crash recovery: commit the command-catalog sync (.agent/commands/*.md) and regenerated docs/USER_STORIES.md left uncommitted by the session crash | — |
| M.BRWS-T33 | 2026-11-10 | chore | closed | Adopt the shared docs-site pipeline: turn on GitHub Pages | — |
| M.BRWS-T34 | 2026-11-17 | docs | closed | Update USER_STORIES.md | — |
| M.BRWS-T35 | 2026-11-21 | fix | closed | NewWindowRequested navigates every popup in the same tab, so OAuth popups (Google/Discord sign-in on midjourney.com) lose window.opener and their handler page hangs blank; let real popups open as popup windows | — |
| M.BRWS-T36 | 2026-11-22 | docs | closed | 2026-11-22 morning | — |
| M.BRWS-T37 | 2026-11-24 | fix | wont-do | Hunt the whole repo for real bugs with no time limit and fix every confirmed one | portfolio reset: open-ended bughunt with no concrete defect |
| M.BRWS-T38 | 2026-11-24 | chore | closed | MindAttic.Browser moves to the MindAttic.Media.Capture.Wpf and .WebView2 packages | children done |
| M.BRWS-T39 | 2026-11-24 | chore | closed | MindAttic.Browser references MindAttic.Media.Capture.Wpf and .WebView2 1.0.0 (from MindAttic.Capture.Wpf 1.0.0 and .WebView2 2.0.0), usings, NuGet.config comment, README and run.ps1 package-name check updated; builds | c32d5c8 |
| M.BRWS-T40 | 2026-11-24 | fix | wont-do | Fails on main: MindAttic.Browser.Tests.CaptureFilesTests.Screenshot_names_never_overwrite — Assert.That(first, Is.EqualTo(@"R:\Cap\youtube.com\youtube.com_2026-10-11T14.30.05.123.png")) Expected string length 58 but was 55. Strings differ at index 35. Expected: "R:\\Cap\\youtube.com\\youtube.com_2026-10-11T14.30.05.123.png" But was: "R:\\Cap\\youtube.com\\youtube.com_2026.10.11T14.30.05... (landed on main since the last green run: tickets M.BRWS-T39 in c32d5c8 Build Browser on MindAttic.Media.Capture packages (M.BRWS-T39)) | superseded by M.BRWS-T41: bisected to c32d5c8 |
| M.BRWS-T41 | 2026-11-24 | fix | closed | Fails on main: MindAttic.Browser.Tests.CaptureFilesTests.Screenshot_names_never_overwrite — Assert.That(first, Is.EqualTo(@"R:\Cap\youtube.com\youtube.com_2026-10-11T14.30.05.123.png")) Expected string length 58 but was 55. Strings differ at index 35. Expected: "R:\\Cap\\youtube.com\\youtube.com_2026-10-11T14.30.05.123.png" But was: "R:\\Cap\\youtube.com\\youtube.com_2026.10.11T14.30.05... (bisected to c32d5c8 "Build Browser on MindAttic.Media.Capture packages (M.BRWS-T39)" on main, reopening M.BRWS-T39's work: fix what that commit changed so this passes again; bisected from the last green 42b3674 in 0 steps) | 6cc802d3393001e304d6682c4bd7de6dd304a844 |
| M.BRWS-T42 | 2026-11-24 | feature | closed | BrowserSettings.DefaultCaptureFolder becomes R:\MindAttic\Media\Capture (saved old default upgrades); README | 5bee60953c0ad60ea324626d2ef70e882fd53677 |
| M.BRWS-T43 | 2026-11-26 | fix | closed | WebNavOperatorService: an assistant turn with no parts (empty reply) is stored in history, and the API then rejects every later request with empty content until the conversation is reset | 4fbe85b |
| M.BRWS-T44 | 2026-11-26 | fix | closed | set_field blurs the field it just filled, so press_enter (which targets the focused element) hits the page body | d7bf90934cb13291f6fe91cba67ab9c8b514b56c |
| M.BRWS-T45 | 2026-11-26 | fix | closed | check_checkbox over-matches: labelOf appends the parent's whole text, so sibling checkboxes sharing a parent all match one phrase | aef04b0f20e1e1942050b0368395141d1b3fd65e |
| M.BRWS-T46 | 2026-11-26 | fix | closed | Crop box move/resize drifts from the cursor after hitting the image edge: _last tracks the unclamped cursor | 144684b |
| M.BRWS-T47 | 2026-11-27 | feature | open | Failures and unhandled exceptions are recorded where they can be read (portfolio reset, M.EXPT-T48) | — |
| M.BRWS-T48 | 2026-11-27 | feature | open | The Browser writes startup, failures and unhandled exceptions to its Vault-assigned MindAttic.Log.db | — |
| M.BRWS-T49 | 2026-11-27 | feature | open | Present the Browser as a released 1.0 app (portfolio reset, M.EXPT-T48) | — |
| M.BRWS-T50 | 2026-11-27 | feature | open | README shows the released state with current screenshots | — |
