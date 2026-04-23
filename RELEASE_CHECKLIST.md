# TaskbarMediaControls-plus Release Checklist

Use this checklist for every portable + Scoop release.

## 1) Prepare version

- Pick release version `X.Y.Z`.
- Update Scoop manifest template values:
  - `version`
  - release `url`
  - `hash` (after ZIP is built)

## 2) Build and package portable ZIP

From repo root:

```powershell
$version = "X.Y.Z"
dotnet test "./TaskbarMediaControls.sln" -c Release
dotnet publish "./TaskbarMediaControls.csproj" -c Release
$publishDir = "./bin/Release/net8.0-windows10.0.19041.0/publish"
$zipPath = "./bin/Release/TaskbarMediaControls-Plus-v$version-portable.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$publishDir/*" -DestinationPath $zipPath
Get-FileHash $zipPath -Algorithm SHA256
```

## 3) Publish GitHub release

- Create tag/release `vX.Y.Z`.
- Upload ZIP: `TaskbarMediaControls-Plus-vX.Y.Z-portable.zip`.
- Copy SHA256 from previous step.

## 4) Update Scoop bucket manifest

In your Scoop bucket repo, update `TaskbarMediaControls-Plus.json`:

- `version`: `X.Y.Z`
- `architecture.64bit.url`: GitHub release ZIP URL
- `architecture.64bit.hash`: SHA256 from `Get-FileHash`

Commit and push bucket changes.

## 5) Verify Scoop install/update

```powershell
scoop update
scoop update TaskbarMediaControls-Plus
```

For first-time install (if bucket not added yet):

```powershell
scoop bucket add <bucket-name> <bucket-url>
scoop install TaskbarMediaControls-Plus
```

## 6) Smoke check

- Launch app from Scoop install.
- Confirm tray icons appear.
- Confirm Scoop run creates/updates `%APPDATA%\TaskbarMediaControls-plus\settings.json`.
- If upgrading from older versions, confirm legacy app-folder `settings.json` is migrated on first run.
- Confirm portable ZIP run still creates/updates `settings.json` beside the executable.
