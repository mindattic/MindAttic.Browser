---
layer: release
status: living
---
# MindAttic.Browser — Release charter

> The one finish line this repo is working toward. Every open ticket in this repo sits under one of the epics
> below, and every epic is part of the milestone. Work that does not fit is out of scope until this milestone is
> reached (owner, 2026-11-27, MAT-T319).

## 1. What this is
A WPF + WebView2 Windows browser for everyday browsing that puts a screenshot or a video on the clipboard in one
click and serves local site folders at `https://<site>.local/`.

## 2. The milestone: MindAttic.Browser 1.0 {#milestone}
- **Milestone:** MS-70, "MindAttic.Browser 1.0: everyday capture browser"
- **Published means:** internal tool at `R:\MindAttic\Apps\MindAttic.Browser\Release\MindAttic.Browser.exe`,
  deployed by `run.ps1`.
- **The natural stopping point:** browsing, capture, recording, local sites and Vault settings all work today.
  What is missing for a 1.0 is a record of its failures (it logs nowhere) and a README that presents it as
  released instead of work in progress.

## 3. Done when (acceptance checks)
1. build — `MindAttic.Browser.slnx` builds clean on main.
2. exists — `R:\MindAttic\Apps\MindAttic.Browser\Release\MindAttic.Browser.exe`.
3. signoff — the owner browses, takes a cropped screenshot and records a clip in the Release build.

## 4. Epics, in priority order
| Priority | Epic (ticket) | What it delivers | Features |
|---|---|---|---|
| 900 | M.BRWS-T47 Crash diagnostics | Failures recorded in MindAttic.Log | M.BRWS-T48 Log to MindAttic.Log (990: M.BRWS-T10) |
| 800 | M.BRWS-T49 1.0 release | README presents the released app | M.BRWS-T50 README released (890: M.BRWS-T9) |

## 5. Out of scope (not before this milestone)
- Extensions, sync, accounts, a tab model beyond today's.
- New AutoWebNav operator capabilities.
- BrowserTarget parse edge-case audit tests, generic cleanups, bughunts and repo-sweep test runs.

## 6. Current verified state
- Built on MindAttic.Media.Capture packages: c32d5c8. OAuth popups: cda6f05. Crop tracking: 144684b.
- Shared run menu and Release deploy to `R:\MindAttic\Apps\MindAttic.Browser`: 23e80b0.

## 7. Decision record
- 2026-11-27 — charter drafted from README, source layout, git log and tickets; Legion vote: **Approve**, 5 of 9
  voters answered, plurality quorum reached, consensus 1.0, no dissent.
- Triage: 6 open → done 0, duplicate 0, wont-do 4 (T4, T7, T8, T37), kept 2 (T10, T9 reworded), new tasks 0.
