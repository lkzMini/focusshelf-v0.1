# FocusShelf

FocusShelf is a small local-first Windows desktop app for gently holding today's focus.

It is not a task-management suite, not a planner, and not a productivity dashboard. The goal is simpler: one current focus, one next step, a quiet note area, a lightweight timer, and a small sense of progress.

## Early MVP status

FocusShelf is an **early MVP**. It is intended to feel calm and useful, but it is still small in scope and conservative in features.

This repository is being prepared for public GitHub use as an early project, not as a finished commercial release.

## Features

- One **Current focus** item
- One **Next step**
- A plain-text **Quick note**
- A simple **focus timer**
- A rotating **daily reminder**
- A tiny **What felt done** list
- Local JSON persistence under the current user's profile
- Theme-aware light/dark styling with a calm visual tone

## What FocusShelf is not

- No accounts
- No sync
- No cloud storage
- No analytics or telemetry
- No notifications
- No calendar
- No heavy settings system
- No AI assistance

## Privacy / local data

FocusShelf stores its state **locally only**.

Current save location:

- `%LOCALAPPDATA%\FocusShelf\state.json`

The app does not send data anywhere and does not require a login.

## Known limitations

- This is still an early MVP with a deliberately small feature set.
- The timer is intentionally simple and does not include advanced session history or notifications.
- Public release packaging is a **Release ZIP**, not MSIX.
- The ZIP does not require admin rights, certificate trust, or MSIX sideloading.
- The ZIP uses the same framework-dependent unpackaged Windows App SDK deployment style as the working Visual Studio/Debug output.

## Tech stack

- C#
- WinUI 3
- Windows App SDK
- Lightweight MVVM (no extra MVVM framework)
- Local JSON persistence
- Framework-dependent unpackaged ZIP release

## Project structure

```text
FocusShelf.sln
src/
  FocusShelf.App/
tests/
docs/
scripts/
```

## Build and run for development

### Requirements

- Windows 10/11
- .NET 8 SDK
- Visual Studio with WinUI / Windows App SDK support, or equivalent MSBuild environment

### Open in Visual Studio

```powershell
devenv .\FocusShelf.sln
```

Use the `FocusShelf.App (Unpackaged)` profile for local development.

### CLI build

```powershell
dotnet build .\src\FocusShelf.App\FocusShelf.App.csproj `
  -c Debug `
  -p:Platform=x64 `
  -p:WindowsPackageType=None `
  -p:WindowsAppSDKSelfContained=false
```

### CLI run

```powershell
dotnet run --project .\src\FocusShelf.App\FocusShelf.App.csproj `
  -c Debug `
  -p:Platform=x64 `
  -p:WindowsPackageType=None `
  -p:WindowsAppSDKSelfContained=false
```

## Release packaging

FocusShelf public releases use a **Release ZIP** generated from a clean Release build output folder.

The public ZIP path intentionally avoids:

- Debug-output hacks
- self-signed MSIX certificates
- certificate installation
- MSIX trust prompts
- admin-only install steps
- manually zipping random `bin\Debug` files

### Why not self-signed MSIX?

Self-signed MSIX technically works as a packaging model, but the user experience is wrong for a small public MVP: users must trust a certificate before installing the package, and that is a security/UX smell. Come on, if the first interaction with a tiny focus app is “please trust my cert”, we already lost.

MSIX remains a possible future option only if the app gets a proper trusted signing story.

### Why not `dotnet publish` self-contained ZIP?

The previous self-contained publish ZIP was created successfully, but the clean-folder smoke test failed immediately with:

- faulting module: `Microsoft.UI.Xaml.dll`
- exception code: `0xc000027b`

The working Debug output is different: it is framework-dependent and uses the Windows App SDK bootstrapper from the `runtimes\win-x64\native` folder instead of carrying private `Microsoft.ui.xaml.dll` / `Microsoft.WindowsAppRuntime.dll` files next to the app executable.

The release ZIP script therefore builds **Release** using the working framework-dependent unpackaged model, copies the clean output into an artifact folder, excludes dev-only noise, zips it, extracts it, and smoke-tests the extracted app.

### Create the release ZIP

```powershell
.\scripts\build-release.ps1 -Platform x64 -Version v0.1.0
```

Expected artifact:

```text
artifacts\release\FocusShelf-v0.1.0-win-x64.zip
```

The script also creates:

```text
artifacts\package\win-x64
artifacts\smoke-test\win-x64
```

### Clean-folder smoke test only

If the ZIP already exists and you want to manually repeat the clean-folder test:

```powershell
Remove-Item .\artifacts\smoke-test\win-x64 -Recurse -Force -ErrorAction SilentlyContinue
Expand-Archive .\artifacts\release\FocusShelf-v0.1.0-win-x64.zip -DestinationPath .\artifacts\smoke-test\win-x64 -Force
Start-Process .\artifacts\smoke-test\win-x64\FocusShelf.App.exe -WorkingDirectory .\artifacts\smoke-test\win-x64
```

The release script performs the stricter automated version: it fails if the app exits immediately or if Windows logs an Application Error for FocusShelf / Microsoft UI XAML during the smoke-test window.

### Install/use from GitHub Releases

1. Download `FocusShelf-v0.1.0-win-x64.zip`.
2. Extract it to a normal user-writable folder.
3. Run `FocusShelf.App.exe`.

No admin. No certificate. No MSIX trust. No installer.

### Optional/future MSIX note

The repository may keep MSIX metadata/assets for future packaging experiments, but MSIX is **not** the default public release path until there is a trusted signing story.

## Manual QA checklist

- ZIP is produced from Release, not Debug
- ZIP extracts into a clean folder
- Extracted `FocusShelf.App.exe` launches
- No Application Error is logged during launch smoke test
- App opens with no existing save file
- App opens with a malformed/corrupt save file without crashing
- Current focus, next step, and note save locally
- Done list add/remove/clear works
- Timer start/pause/reset works
- Text input remains readable in both light and dark system themes
- Window resize does not collapse the layout into unusable shapes
- The app still feels small, calm, and local-first
