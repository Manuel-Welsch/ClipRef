# ClipRef

A tiny macOS menu-bar app that saves whatever is on your clipboard — **text or an
image** — to a file and hands you back an `@`-reference you can paste straight into
[Claude Code](https://claude.com/claude-code).

The point: stop pasting long logs or screenshots *inline*. Instead of dumping 2,000
lines into the context window — or pasting an image Claude can look at but can't copy
into your project — you copy it, click once, and paste a one-line file reference.
Claude reads (or copies) the file only when it actually needs to.

```
copy a log / screenshot  ─▶  click the menu-bar icon  ─▶  ⌘V into Claude Code
                                               (pastes @/…/clip-20260602-094134-778.txt
                                                     or @/…/clip-20260602-101647-102.png)
```

---

## Features

- **One click = one file.** Left-click the menu-bar icon and the current clipboard is
  written to a timestamped file in your folder: text as `clip-….txt`, an image as
  `clip-….png`.
- **`@`-path back on the clipboard.** Right after saving, your clipboard holds
  `@/absolute/path/to/clip-….{txt,png}`, ready to paste into Claude Code as a file
  reference. (The content is already safe on disk, so swapping the clipboard costs you
  nothing.) For an image, Claude can then copy it into your project as an asset.
- **Text or image, automatically.** Text wins when present; otherwise an image on the
  clipboard (screenshot, "Copy Image", etc.) is saved as PNG.
- **Configurable folder.** Defaults to `~/Developer/clipboard-logs`; change it from the
  right-click menu (the choice is remembered).
- **Automatic 7-day cleanup.** Files older than the retention window are pruned on launch
  and after each save. Only files ClipRef created (`clip-*.txt` / `clip-*.png`) are ever
  deleted.
- **Launch at login.** Registered via `SMAppService`; toggle it from the menu.
- **No Dock icon.** Runs as a menu-bar agent (`LSUIElement`).
- **Headless mode.** `ClipRef --save-once` does the save from a script/terminal.

## Requirements

- macOS 14 (Sonoma) or later
- Xcode 16+ to build from source (Xcode 26 used during development)

## Install

### Build from source

```sh
git clone https://github.com/Manuel-Welsch/ClipRef.git
cd ClipRef
xcodebuild -project ClipRef.xcodeproj -scheme ClipRef \
  -configuration Release -derivedDataPath build -allowProvisioningUpdates build
ditto build/Build/Products/Release/ClipRef.app /Applications/ClipRef.app
open /Applications/ClipRef.app
```

> **Signing:** the project uses automatic signing with a specific Apple Development
> team. To build under your own account, open the project in Xcode and pick your team
> under *Signing & Capabilities*, or override on the command line with
> `DEVELOPMENT_TEAM=YOURTEAMID`. To build without any team, set
> `CODE_SIGN_IDENTITY="-" CODE_SIGNING_REQUIRED=NO` (an unsigned build cannot register a
> login item via `SMAppService`).

### Or open in Xcode

```sh
open ClipRef.xcodeproj
```

Run with ⌘R. For launch-at-login to point at a stable location, copy the built app to
`/Applications` and launch it from there.

## Usage

- **Left-click** the icon → save the clipboard (text or image). A checkmark + sound
  confirm success; a warning icon means the clipboard had neither text nor an image.
- **Right-click** (or control-click) → menu:
  - **Save Clipboard Now**
  - **Open Folder**
  - **Change Folder…**
  - **Launch at Login** (toggle)
  - **Quit ClipRef**
- Switch to Claude Code and **⌘V** — the pasted `@/…/clip-….{txt,png}` resolves to a
  file reference.

## Configuration

| What | How | Default |
| --- | --- | --- |
| Folder | Right-click → *Change Folder…* | `~/Developer/clipboard-logs` |
| Retention (days) | `defaults write de.manuelwelsch.ClipRef retentionDays 14` | `7` |
| Launch at login | Right-click → *Launch at Login* | on (first run) |

## Headless / CLI mode

```sh
/Applications/ClipRef.app/Contents/MacOS/ClipRef --save-once
# prints the saved file path; exit 0 = saved, 2 = no text/image, 1 = write error
```

This runs the exact same save logic as a left-click — handy for scripting or testing.

## How it works

- **AppKit `NSStatusItem`** with a button that distinguishes left-click (instant save)
  from right-click (menu) — something SwiftUI's `MenuBarExtra` can't cleanly do.
- **`ClipboardLogger`** reads `NSPasteboard.general` (string first, otherwise an image
  re-encoded to PNG), writes a timestamped file, and puts the `@`-path back on the
  pasteboard.
- **`SMAppService.mainApp`** handles launch-at-login (shows up in *System Settings →
  General → Login Items*).
- The Xcode project uses a **file-system synchronized group**, so any `.swift` file
  dropped into `ClipRef/` is automatically part of the target — no `pbxproj` bookkeeping.

### Project layout

```
ClipRef/
  main.swift            App entry point + --save-once headless mode
  AppDelegate.swift     Menu-bar item, click handling, menu, login item
  ClipboardLogger.swift Clipboard → file (text/image), @-path, retention cleanup
ClipRef.xcodeproj/      Xcode project (synchronized group, shared scheme)
```

## Releasing

This repo builds a locally-signed app. To distribute it more widely:

- **Direct download / Homebrew Cask** — sign with a **Developer ID** certificate and
  **notarize** the app, then attach a zipped `.app` (or `.dmg`) to a GitHub Release. A
  Homebrew *Cask* (in your own tap) can then `brew install --cask clipref` and drop it
  into `/Applications`.
- **Mac App Store** — requires the Apple Developer Program, enabling **App Sandbox**
  (which changes file access: writing to an arbitrary user folder needs a security-scoped
  bookmark from the folder picker), an *Apple Distribution* certificate + Mac App Store
  provisioning profile, an App Store Connect record, and passing App Review.

## License

[MIT](LICENSE) © 2026 Manuel Welsch
