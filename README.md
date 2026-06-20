# Interlude

**Language / 语言：** [中文](#中文说明) | [English](#english)

## 中文说明

Interlude 是一个轻量级 Windows 后台工具，用来协调音乐播放器和其他应用的声音。当其他应用开始播放声音时，它可以自动暂停你选择的音乐播放器，并且只恢复由 Interlude 自己暂停过的音乐。

### 功能

- 通过 Windows 媒体会话选择 Interlude 需要监测的音乐播放器。
- 根据声音活动、媒体播放状态或混合模式自动暂停和恢复音乐。
- 可配置声音触发阈值、轮询间隔、开始确认时间、静音确认时间和恢复延迟。
- 支持忽略指定应用，避免这些应用的声音触发自动暂停。
- 支持系统托盘运行、最小化到托盘、关闭到托盘。
- 支持随 Windows 开机启动。
- 支持尊重用户手动播放变更，避免自动控制覆盖用户操作。
- 支持需要时手动恢复音乐。
- 可从主界面或托盘菜单打开日志目录。
- 支持英文和简体中文界面。

### 三种检测模式

| 模式 | 通俗说明 | 适合场景 |
| --- | --- | --- |
| 声音活动模式 | 直接听系统里有没有其他应用发出声音。只要声音达到设定的触发阈值，就会暂停音乐。 | 适合游戏、语音通话等不一定会显示媒体播放状态的应用。 |
| 媒体模式 **（默认模式）** | 查看其他应用是否正在播放视频或音频；开始播放时暂停音乐，停止后再恢复。 | 适合浏览器视频、影视播放器等常见媒体应用，判断更稳定，也不容易被短促的提示音打断。 |
| 混合模式 | 同时使用前两种判断方式；检测到声音活动或媒体播放中的任意一种，就会暂停音乐。 | 覆盖范围最广，适合希望尽量不错过任何播放声音场景的用户。 |

### 截图

| 截图 | 说明 |
| --- | --- |
| ![Interlude 中文状态页](docs/images/interlude-status-zh.png) | 状态页集中展示当前播放器、播放状态、目标可用性和检测模式，方便快速确认自动控制是否正在工作。 |

### 下载

普通用户不需要下载源码，也不需要自行构建。请从 GitHub Releases 页面下载 Windows 发布版本：

https://github.com/ZephyrWn/Interlude/releases

v0.8.0 推荐下载 `Interlude-v0.8.0-win-x64.zip`，完整解压后运行其中的 `Interlude.exe`。如果只想直接运行单个文件，也可以下载 `Interlude-v0.8.0-win-x64.exe`。

### 使用方法

1. 下载推荐的 `Interlude-v0.8.0-win-x64.zip`。
2. 将 ZIP 文件完整解压到一个文件夹。
3. 运行解压后的 `Interlude.exe`。
4. 在首次启动时选择语言和默认音乐播放器。
5. 根据需要配置检测模式、忽略应用、开机启动和托盘行为。
6. 让 Interlude 在后台运行，或最小化到系统托盘。

### 系统要求

- Windows 10 或 Windows 11
- x64 系统

v0.8.0 的 Windows x64 发布版本是自包含版本，普通用户不需要额外安装 .NET Runtime。

### 从源码构建

从源码构建仅适用于希望查看代码、修改功能或参与开发的开发者。普通用户请直接下载 Release 版本使用。

要求：

- .NET SDK 8.0
- 支持桌面应用构建的 Windows 环境

构建：

```powershell
dotnet build Interlude.sln -c Release
```

发布 Windows x64 自包含版本：

```powershell
dotnet publish src/Interlude/Interlude.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o artifacts/publish/win-x64
```

仓库中也包含 `scripts/Build-Release.ps1`，用于发布并验证 Windows x64 单文件可执行程序。

### 许可证

Interlude 使用 MIT License。详情见 `LICENSE`。

### 项目地址

https://github.com/ZephyrWn/Interlude

## English

Interlude is a lightweight Windows background utility that automatically coordinates music playback with other audio activity on your PC. It can pause a selected music player when another app starts producing audio, then resume only the music that Interlude paused itself.

### Features

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

### Detection Modes

| Mode | Plain-language description | Best for |
| --- | --- | --- |
| Sound activity | Listens for actual sound from other apps. Music pauses when that sound reaches the configured threshold. | Games, voice calls, and apps that may not report a media playback state. |
| Media mode **(Default)** | Checks whether another app is playing audio or video. Music pauses when playback starts and resumes after it stops. | Browser videos and media players. It is stable and less likely to react to brief notification sounds. |
| Hybrid | Uses both checks. Music pauses when either sound activity or media playback is detected. | The broadest coverage when you do not want to miss an app that starts making sound. |

### Screenshots

| Screenshot | Description |
| --- | --- |
| ![Interlude English status page](docs/images/interlude-status-en.png) | The status page shows the current player, playback state, target availability, and detection mode at a glance. |

### Download

Normal users do not need to download or build the source code. Download the Windows release from:

https://github.com/ZephyrWn/Interlude/releases

For v0.8.0, the recommended download is `Interlude-v0.8.0-win-x64.zip`. Extract the full ZIP, then run `Interlude.exe`. If you prefer a single direct executable, `Interlude-v0.8.0-win-x64.exe` is also available.

### Usage

1. Download the recommended `Interlude-v0.8.0-win-x64.zip`.
2. Extract the full ZIP into a folder.
3. Run the extracted `Interlude.exe`.
4. Choose your language and default music player during setup.
5. Configure the detection mode, ignored apps, startup behavior, and tray behavior as needed.
6. Let Interlude run in the background or minimize it to the system tray.

### System Requirements

- Windows 10 or Windows 11
- x64 system

The v0.8.0 Windows x64 release is published as a self-contained build, so normal users do not need to install the .NET Runtime separately.

### Build From Source

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

### License

Interlude is released under the MIT License. See `LICENSE` for details.

### Project

https://github.com/ZephyrWn/Interlude
