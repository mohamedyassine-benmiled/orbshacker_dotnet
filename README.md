<div align="center">

# orbshacker — .NET edition

**A Windows process-name research tool for Discord's detectable-game database.**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?style=for-the-badge&logo=windows)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-GPLv3-blue?style=for-the-badge)](LICENSE)

</div>

> **Educational purposes only.** Users are responsible for complying with Discord's, Steam's, and all other applicable terms. Use at your own risk.

## Features

- Loads the current detectable-game list from Discord's API, with the original GitHub archive as a fallback.
- Searches names and aliases, chooses a suitable Win32 executable, and supports custom executable names.
- Copies the published single-file .NET application under the requested process name and launches an independent graphical countdown timer.
- Bakes timer duration, automatic deletion, and Steam-manifest cleanup settings into each copied executable.
- Supports multiple simultaneous processes and optional cleanup of processes, files, empty directories, and Steam manifests.
- Searches Steam, reads Steam metadata and the active account from the Windows registry, and generates the partial-download `appmanifest_<appid>.acf` required by Steam quest mode.
- Checks GitHub Releases and performs in-place updates for the published Windows executable.
- Preserves the original manual fallbacks when Steam or SteamCMD metadata cannot be detected, and reads legacy `settings.py` values during migration.

## Requirements

- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build from source (published releases are self-contained)
- Internet access for Discord/Steam database features

## Build and run

```powershell
dotnet restore Orbshacker.sln
dotnet test Orbshacker.sln
dotnet run --project src/Orbshacker/Orbshacker.csproj
```

Normal `dotnet build` and `dotnet run` outputs are framework-dependent and require the .NET 8 Windows Desktop Runtime. The application copies their `.dll`, `.deps.json`, and `.runtimeconfig.json` files beside each generated game executable. Release publishing instead produces one self-contained executable, so generated game copies need no supporting files or installed runtime.

Create the same self-contained, single-file executable used by releases:

```powershell
dotnet publish src/Orbshacker/Orbshacker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
.\publish\orbshacker.exe
```

## Configuration

On first launch, `settings.json` is created beside `orbshacker.exe`:

```json
{
  "CHOSEN_FOLDER": "C:/Users/you/Desktop",
  "AUTO_DELETE": false,
  "TIMER_MINUTES": 15,
  "FAKE_EXE_DIR": "Win64",
  "STEAM_MANIFEST_PATH": null
}
```

Each copied executable contains its own baked snapshot, so it continues to use the selected settings without depending on the original application directory.
If `settings.json` is absent, the application can also import the original `CHOSEN_FOLDER`, `AUTO_DELETE`, `TIMER_MINUTES`, and `FAKE_EXE_DIR` assignments from a legacy `settings.py` file.

## Project structure

```text
src/Orbshacker/             Application, services, timer UI, and Windows integration
tests/Orbshacker.Tests/     xUnit tests for portable behavior
.github/workflows/          Windows build, test, publish, and GitHub Release pipeline
Orbshacker.sln              Visual Studio/.NET solution
```

## License

GPL v3. See [LICENSE](LICENSE).
