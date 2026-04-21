# TaskbarMediaControls-plus

<img width="255" height="89" alt="showoff" src="https://github.com/user-attachments/assets/bf906225-287c-4b8a-9ffe-52dba4a35fb5" />

`TaskbarMediaControls-plus` is an independent fork of TaskbarMediaControls for Windows 11.
It keeps the lightweight tray-control workflow while improving fallback player launch behavior
and documenting the project for maintainable, release-friendly development.

## Upstream credit

This project is based on the original TaskbarMediaControls by `2latemc`.

- Original repository: [https://github.com/2latemc/TaskbarMediaControls](https://github.com/2latemc/TaskbarMediaControls)
- This fork: independent maintenance and release cadence under `TaskbarMediaControls-plus`

This repository keeps Apache 2.0 licensing and attribution requirements from upstream.

## What changed in plus (and why)

The most important plus changes are focused on fallback app launching, especially `foobar2000`
when it runs in tray/minimized states where the process window is difficult to restore.

- **Stronger foobar restore path:** tries `/show`, verifies restore attempts, then falls back to
  multiple `/command` variants if needed.
- **Window restore reliability:** adds top-level window enumeration for processes that do not expose
  a direct `MainWindowHandle` while still owning visible windows.
- **Safer launch decisions:** resolves foobar handling from executable path (not only exact configured fallback path),
  reducing duplicate launches for already-running instances.
- **Clearer error messaging:** explicitly reports when `/show` and `/command` fallback strategies both fail.
- **Expanded tests:** adds/updates tests around launch player resolution, duplicate-avoidance behavior,
  and foobar launch outcomes.

Detailed technical change notes are in [PLUS_CHANGES.md](./PLUS_CHANGES.md).

## Features

- Tray media controls: Previous, Play/Pause, Next
- Single-click and double-click action customization per icon
- Optional launch on Windows startup (Startup-folder shortcut)
- Fallback media application launch flow with player-type handling
- Hover metadata display (optional)

## Get it

### 1) Portable ZIP (main)

Portable is the main distribution model for `TaskbarMediaControls-plus`.
Installation is not required for core functionality.

- Download: `TaskbarMediaControls-Plus-v<version>-portable.zip`
- Extract to any folder.
- Run `TaskbarMediaControlsPlus.exe`.
- Settings are saved in `settings.json` in the same folder as the executable.

If you move the portable folder, settings move with it.

This portable build is framework-dependent, so users need the .NET runtime available.

### 2) Scoop (also available)

Available through Scoop as `TaskbarMediaControls-Plus`:

```powershell
scoop bucket add <bucket-name> <bucket-url>
scoop install TaskbarMediaControls-Plus
```

Update with:

```powershell
scoop update
scoop update TaskbarMediaControls-Plus
```

### 3) Optional installer

An installer build is also available for users who prefer setup/uninstall and normal Start menu integration.
Portable remains the recommended/default option.

## Startup behavior (portable)

Use `Settings -> Launch on Startup` in the tray app to manage startup.
When enabled, the app creates a shortcut in the Windows Startup folder:
`%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\TaskbarMediaControls-plus.lnk`.

If you move the portable folder/executable, re-enable the setting once so the
startup shortcut target is updated to the new location.

## Manual shortcut helper (optional)

`scripts/shortcut-manager.bat` is still available as a manual helper to toggle shortcuts interactively.

- `1` toggles Desktop shortcut
- `2` toggles Start Menu shortcut
- `3` toggles Startup shortcut
- Press `Enter` to apply selected toggle(s)

You can run the BAT repeatedly; each selected target is toggled on/off depending on current state.

## Build and run (local)

```powershell
dotnet restore "./TaskbarMediaControls.sln"
dotnet build "./TaskbarMediaControls.sln"
dotnet run --project "./TaskbarMediaControls.csproj"
```

More setup and testing guidance is in [DEVELOPMENT_GUIDE.md](./DEVELOPMENT_GUIDE.md).

Scoop manifest template for maintainers: [scoop/TaskbarMediaControls-Plus.json](./scoop/TaskbarMediaControls-Plus.json)
Release process checklist: [RELEASE_CHECKLIST.md](./RELEASE_CHECKLIST.md)

## Compatibility and limitations

- Target platform: Windows 10/11 (`net8.0-windows10.0.19041.0`)
- Tray behavior can vary by media app session exposure
- VLC support is still limited

## Why a plus fork

`TaskbarMediaControls-plus` exists to:

- ship behavior updates faster for edge cases observed in real-world usage,
- keep fork-specific release notes and support expectations clear,
- remain fully transparent about upstream origin and derivative changes.
