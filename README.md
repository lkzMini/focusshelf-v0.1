# FocusShelf

FocusShelf is a small local-first Windows app for gently holding today's focus.

It is intentionally simple: one current focus, one next step, a quick note, a small focus timer, and a lightweight “done today” list.

FocusShelf is currently an early MVP. It is usable for basic testing, but still experimental.

## Current status

FocusShelf is an early public MVP focused on:

- local-first usage
- simple daily focus
- small task/notes flow
- readable light/dark theme behavior
- local persistence
- minimal UI

It is not intended to be a full productivity suite, project manager, calendar, or task automation system.

## Features

- Current focus field
- Next step field
- Quick note field
- Simple focus timer
- Daily reminder/message area
- “What felt done” list
- Local JSON persistence
- Safer local save handling
- Improved light/dark text input contrast
- Basic resize protection
- No account required
- No cloud sync

## Download

Go to the latest GitHub Release and download the Windows `.zip` build.

Example:

```txt
FocusShelf-v0.1.0-win-x64.zip
```

Then:

1. Extract the `.zip`
2. Run:

```txt
FocusShelf.App.exe
```

This is an unsigned early build, so Windows SmartScreen may show a warning.

If that happens, choose:

```txt
More info → Run anyway
```

## Runtime requirements

FocusShelf is distributed as a framework-dependent ZIP build.

If the app does not launch, you may need:

- .NET Desktop Runtime
- Windows App SDK Runtime

The app is currently tested as a Windows-focused WinUI/Windows App SDK desktop app.

## Build from source

Requirements:

- Windows
- .NET 8 SDK or newer compatible SDK
- Visual Studio 2022 or compatible Windows/.NET development tools

Clone the repo:

```powershell
git clone https://github.com/lkzMini/FocusShelf.git
cd FocusShelf
```

Build the app:

```powershell
dotnet build .\src\FocusShelf.App\FocusShelf.App.csproj -c Release
```

## Creating a release ZIP

Use the included release script:

```powershell
.\scripts\build-release.ps1 -Platform x64 -Version v0.1.0
```

The script is expected to:

1. Build the app in Release mode
2. Create a clean package folder
3. Generate a ZIP artifact
4. Extract the ZIP into a smoke-test folder
5. Launch the app from the extracted folder
6. Fail if the app exits immediately or logs an Application Error

Expected artifact:

```txt
artifacts\release\FocusShelf-v0.1.0-win-x64.zip
```

## Manual QA checklist

Before publishing a release, test:

1. Open the app from the extracted ZIP
2. Type into the main focus fields
3. Confirm text remains readable
4. Start, pause, and reset the timer
5. Add an item to the done list
6. Close and reopen the app
7. Confirm local state persists
8. Resize the window
9. Confirm the UI does not visually break
10. Test in light and dark Windows themes if possible

## Known limitations

- Early MVP
- Windows-focused
- Unsigned executable
- No installer yet
- No automated tests yet
- No sync
- No accounts
- No notifications
- No advanced planning/project features
- ZIP release may require the correct .NET/Windows App SDK runtimes on the target machine

## Privacy

FocusShelf is designed as a local-first app.

Current MVP behavior stores data locally only. It does not require an account, cloud sync, or an online service.

Local app state is stored under the user profile, for example:

```txt
%LOCALAPPDATA%\FocusShelf\
```

## Packaging note

FocusShelf is currently released as a simple framework-dependent ZIP.

A self-signed MSIX flow was intentionally rejected for the public MVP because it would require users to manually trust a certificate or run installation steps with elevated privileges. That is not acceptable UX for a small public utility.

A future release may use a proper installer or MSIX package if a trusted signing path is available.

## Roadmap ideas

Possible future improvements:

- Better installer
- More polished first-run experience
- Optional import/export
- Better keyboard shortcuts
- More theme polish
- Automated tests
- More robust release pipeline
- Optional notification/reminder support

## License

This project is released under the MIT License.
