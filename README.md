# Interlude

Interlude is a lightweight Windows background utility that automatically coordinates music playback with other audio activity on your PC. It can pause a selected music player when another app starts producing audio, then resume only the music that Interlude paused itself.

## Features

- Choose the media player Interlude should monitor through Windows media sessions.
- Automatically pause and resume music based on sound activity, media playback, or hybrid detection.
- Configure detection thresholds, polling interval, start confirmation, silence confirmation, and resume delay.
- Ignore selected apps so their audio does not trigger an automatic pause.
- Run from the system tray, minimize to tray, and close to tray.
- Toggle automatic startup with Windows.
- Respect manual playback changes so user control is not overwritten unexpectedly.
- Restore music manually when needed.
- Open the log directory from the app or tray menu.
- Use English or Simplified Chinese UI.

## Screenshots

No public screenshots are included in this repository yet.

## Download

Normal users do not need to download or build the source code. Download the Windows release from:

https://github.com/ZephyrWn/Interlude/releases

For v0.8.0, download `Interlude-v0.8.0-win-x64.exe`. If a ZIP file is provided, extract the full ZIP before running Interlude.

## Usage

1. Download the Release file.
2. If you downloaded a ZIP file, extract it first.
3. Run `Interlude.exe`.
4. Choose your language and default music player during setup.
5. Configure the detection mode, ignored apps, startup behavior, and tray behavior as needed.
6. Let Interlude run in the background or minimize it to the system tray.

## System Requirements

- Windows 10 or Windows 11
- x64 system

The v0.8.0 Windows x64 release is published as a self-contained build, so normal users do not need to install the .NET Runtime separately.

## Build From Source

Building from source is intended for developers who want to inspect the code, modify features, or contribute to the project. Normal users should download the Release build instead.

Requirements:

- .NET SDK 8.0
- Windows with desktop application build support

Build:

```powershell
dotnet build Interlude.sln -c Release
```

Publish a self-contained Windows x64 build:

```powershell
dotnet publish src/Interlude/Interlude.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o artifacts/publish/win-x64
```

The repository also includes `scripts/Build-Release.ps1`, which publishes and validates a single-file Windows x64 executable.

## License

Interlude is released under the MIT License. See `LICENSE` for details.

## Project

https://github.com/ZephyrWn/Interlude
