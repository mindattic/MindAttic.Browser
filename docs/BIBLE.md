---
layer: bible
status: living
---
# MindAttic.Browser — Project Bible

> Single source of truth for what MindAttic.Browser IS, is NOT, and the rules that keep it coherent.
> README says how to build and run it; this says how to think about it. The finish line lives in
> `docs/RELEASE.md`.

## 1. The one sentence {#M.BRWS-§1}
MindAttic.Browser is a Windows browser for everyday browsing that turns anything on screen into a
screenshot or a video on the clipboard in one click, and serves a local site folder as if it were
deployed.

## 2. The product promise {#M.BRWS-§2}
- **One click to keep it.** Ctrl+Shift+S shoots the page, opens the crop lightbox, saves the image and
  puts it on the clipboard. Ctrl+Shift+R records; the finished video's file lands on the clipboard.
- **Files explain themselves.** Every capture is named after the site and a timestamp, under the
  capture folder (default `R:\MindAttic\Media\Capture`).
- **Local sites behave like live ones.** A folder or file opens at `https://<site>.local/` with no web
  server.
- **Settings live in Vault.** The cog opens MindAttic.Vault; saved edits apply to the open window.
- **A capture in progress is never killed by a redeploy.** `run.ps1` asks a running copy to close and
  keeps the old build if it does not.

## 3. What it is NOT {#M.BRWS-§3}
- **NOT a Chrome replacement.** No extensions, no sync, no account.
- **NOT a recorder of its own.** Recording and capture naming come from the MindAttic.Media.Capture
  packages.
- **NOT a web server.** Local sites are served through WebView2 virtual host mapping.

## 4. Architecture canon {#M.BRWS-§4}
```
 MainWindow (WPF) ── WebView2 ── page
     │  toolbar: back/forward/reload/home, star, screenshot, record, folder, settings
     ├─ CropWindow + CropMath ──> CaptureFiles ──> ClipboardService
     ├─ Media.Capture.Wpf / .WebView2 ──> video, -capture flags
     ├─ BrowserTarget.Parse ──> URL | search | https://<site>.local/
     ├─ HistoryWindow, favorites
     └─ AutoWebNavWindow ──> Operator/WebNavOperatorService (LLM tool calling over the page)
```
### 4.1 Projects
- `src/MindAttic.Browser` — the WPF app, `net10.0-windows10.0.19041.0`, version 1.0.0.
- `tests/MindAttic.Browser.Tests` — NUnit.
- `run.ps1` / `run.bat` — the shared run menu; Release deploys self-contained to
  `R:\MindAttic\Apps\MindAttic.Browser\Release`.

### 4.2 Domain model (the nouns)
`BrowserTarget`, `BrowserSettings` (home page, search engine, capture folder, clipboard and crop
options, pinned toolbar actions), capture file, favorite, history entry.

### 4.3 Key services (the verbs)
`BrowserTarget.Parse/NormalizeUrl/HostName`, `CaptureFiles`, `ClipboardService`, `CropMath`,
`WebNavOperatorService`, `WebNavToolRegistry`.

## 5. The Laws {#M.BRWS-§5}
Inherits `MindAttic.HouseRules.md` by reference.
1. **Capture is one action.** {#M.BRWS-LAW-1} Shoot, save and copy happen together.
2. **Other tools start the Release exe.** {#M.BRWS-LAW-2} MindAttic.Web `capture.ps1`, `tickets view`
   and Media.Capture launch `R:\MindAttic\Apps\MindAttic.Browser\Release\MindAttic.Browser.exe`.
3. **Settings are Vault's.** {#M.BRWS-LAW-3} No settings dialog of its own; browsing data
   (favorites, history) stays local.

## 6. Verified state {#M.BRWS-§6}
- Built on the MindAttic.Media.Capture packages: c32d5c8.
- OAuth popups as real windows: cda6f05. Crop box tracks the cursor at the image edge: 144684b.
- Shared run menu and Release deploy: 23e80b0.

## 7. Active frontier {#M.BRWS-§7}
See `docs/RELEASE.md`: crash logging to MindAttic.Log and the README presenting 1.0 as released.

## 8. Quality bar {#M.BRWS-§8}
A change is done when the solution builds, the Release deploy runs it, and a screenshot and a short
recording still land on the clipboard.

## 9. Glossary {#M.BRWS-§9}
- **Lightbox** — the crop overlay opened after a screenshot.
- **Virtual host** — the `https://<site>.local/` mapping for a local folder.
- **Operator** — the AutoWebNav panel that drives the page with LLM tool calls.
