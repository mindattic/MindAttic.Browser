---
codex: 1
project: MindAttic.Browser
code: M.BRWS
layer: stories
status: living
updated: 2026-10-12
---
<!-- GENERATED from MindAttic.Workflow/data/workflow.db by `workflow db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the workflow CLI. -->

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
