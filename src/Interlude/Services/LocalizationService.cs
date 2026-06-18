using System.Globalization;
using System.Windows;
using Interlude.Models;

namespace Interlude.Services;

public sealed class LocalizationService
{
    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["Common.Enabled"] = "Enabled",
        ["Common.Disabled"] = "Disabled",
        ["Common.Yes"] = "Yes",
        ["Common.No"] = "No",
        ["Common.None"] = "None",
        ["Common.NotConfigured"] = "Not configured",
        ["Common.Available"] = "Available",
        ["Common.Waiting"] = "Waiting",
        ["Common.Minimize"] = "Minimize",
        ["Common.Close"] = "Close",
        ["App.UnexpectedError"] = "Interlude hit an unexpected error. Details were written to the log directory.",

        ["FirstRun.WindowTitle"] = "Welcome to Interlude",
        ["FirstRun.Title"] = "Set up your default music player",
        ["FirstRun.Description"] = "Interlude pauses your chosen music player when other apps start producing audio, then resumes only music that Interlude paused itself.",
        ["FirstRun.Step1"] = "1. Open the music player you want Interlude to control.",
        ["FirstRun.Step2"] = "2. Play any track so Windows exposes its media session.",
        ["FirstRun.Step3"] = "3. Choose that media session as the default player.",
        ["FirstRun.Later"] = "You can change this later from Settings.",
        ["FirstRun.ChoosePlayer"] = "Choose player",

        ["Main.Subtitle"] = "Windows audio coordination is running in the background.",
        ["Main.Automation"] = "Automation",
        ["Main.DefaultPlayer"] = "Default player",
        ["Main.TargetAvailability"] = "Target availability",
        ["Main.PlaybackState"] = "Playback state",
        ["Main.AutomationState"] = "Automation state",
        ["Main.PausedByInterlude"] = "Paused by Interlude",
        ["Main.DetectionMode"] = "Detection mode",
        ["Main.Enable"] = "Enable",
        ["Main.PauseAutomation"] = "Pause automation",
        ["Main.ChangePlayer"] = "Change player",
        ["Main.Settings"] = "Settings",
        ["Main.RestoreMusicNow"] = "Restore music now",
        ["Main.Logs"] = "Logs",
        ["Main.ToggleStartup"] = "Toggle startup",
        ["Main.ExitInterlude"] = "Exit Interlude",
        ["Main.Status"] = "Status",
        ["Main.RunningStatus"] = "Running status",
        ["Main.AutomationSwitch"] = "Auto control",
        ["Main.CurrentPlayer"] = "Current player",
        ["Main.GeneralSettings"] = "General settings",
        ["Main.PlayerSettings"] = "Player settings",
        ["Main.DetectionSettings"] = "Detection settings",
        ["Main.LogDirectory"] = "Log directory",

        ["Settings.WindowTitle"] = "Interlude settings",
        ["Settings.Save"] = "Save",
        ["Settings.Reset"] = "Reset settings",
        ["Settings.General"] = "General",
        ["Settings.EnableAutomation"] = "Enable automation",
        ["Settings.StartWithWindows"] = "Start with Windows",
        ["Settings.StartMinimizedToTray"] = "Start minimized to tray",
        ["Settings.CloseWindowToTray"] = "Close window to tray",
        ["Settings.RespectManualPlaybackChanges"] = "Respect manual playback changes",
        ["Settings.OpenLogDirectory"] = "Open log directory",
        ["Settings.Language"] = "Language",
        ["Settings.Player"] = "Player",
        ["Settings.CurrentDefaultPlayer"] = "Current default player",
        ["Settings.ChangePlayer"] = "Change player",
        ["Settings.Detection"] = "Detection",
        ["Settings.Mode"] = "Mode",
        ["Settings.ActivityThreshold"] = "Activity threshold",
        ["Settings.PollIntervalMs"] = "Poll interval (ms)",
        ["Settings.StartConfirmMs"] = "Start confirm (ms)",
        ["Settings.SilenceConfirmMs"] = "Silence confirm (ms)",
        ["Settings.ResumeDelayMs"] = "Resume delay (ms)",
        ["Settings.IgnoreSystemSounds"] = "Ignore system sounds",
        ["Settings.Advanced"] = "Advanced",
        ["Settings.ConfigDirectory"] = "Config directory",
        ["Settings.LogDirectory"] = "Log directory",
        ["Settings.DamagedSettings"] = "If settings.json is damaged, Interlude backs it up and creates a default file.",
        ["IgnoreApps.EntryTitle"] = "Ignored apps",
        ["IgnoreApps.Manage"] = "Manage >",
        ["IgnoreApps.Summary"] = "{0} ignored",
        ["IgnoreApps.Description"] = "Ignored apps do not trigger pause.",
        ["IgnoreApps.Title"] = "Ignored apps",
        ["IgnoreApps.Search"] = "Search by app or process name",
        ["IgnoreApps.Refresh"] = "Refresh app list",
        ["IgnoreApps.AddRunning"] = "Add from running apps",
        ["IgnoreApps.AddManual"] = "Choose program file",
        ["IgnoreApps.ExecutableFilter"] = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
        ["IgnoreApps.Status.Ready"] = "Refresh to list running audio apps.",
        ["IgnoreApps.Status.Refreshing"] = "Refreshing applications...",
        ["IgnoreApps.Status.FoundApplications"] = "Found {0} application(s).",
        ["IgnoreApps.Status.RefreshFailed"] = "Refresh failed. Check the log directory for details.",
        ["PlayerSelection.WindowTitle"] = "Choose default player",
        ["PlayerSelection.Prompt"] = "Choose the player Interlude should monitor.",
        ["PlayerSelection.Refresh"] = "Refresh",
        ["PlayerSelection.PlayerList"] = "Players",
        ["PlayerSelection.EmptyTitle"] = "No media players detected yet",
        ["PlayerSelection.EmptyHint"] = "Open a player, play audio once, then click Refresh.",
        ["PlayerSelection.Application"] = "Application",
        ["PlayerSelection.Title"] = "Title",
        ["PlayerSelection.State"] = "State",
        ["PlayerSelection.Cancel"] = "Cancel",
        ["PlayerSelection.SetAsDefault"] = "Set as default",
        ["PlayerSelection.Status.OpenPlayer"] = "Open your music player, play any track, then refresh.",
        ["PlayerSelection.Status.Refreshing"] = "Refreshing sessions...",
        ["PlayerSelection.Status.NoMediaSessions"] = "No media players detected yet. Open a player and play audio once.",
        ["PlayerSelection.Status.FoundMediaSessions"] = "Found {0} media session(s).",
        ["PlayerSelection.Status.RefreshFailed"] = "Refresh failed. Check the log directory for details.",
        ["PlayerSelection.Status.SelectMediaSessionFirst"] = "Select a media session first.",
        ["PlayerSelection.Status.DefaultPlayerSaved"] = "Default player saved.",

        ["Tray.Status"] = "Interlude: {0}",
        ["Tray.DefaultPlayer"] = "Default player: {0}",
        ["Tray.State"] = "State: {0}",
        ["Tray.DisableAutomation"] = "Disable automation",
        ["Tray.EnableAutomation"] = "Enable automation",
        ["Tray.RestoreMusicNow"] = "Restore music now",
        ["Tray.ShowMainWindow"] = "Show main window",
        ["Tray.ChangeDefaultPlayer"] = "Change default player",
        ["Tray.Settings"] = "Settings",
        ["Tray.OpenLogDirectory"] = "Open log directory",
        ["Tray.DisableStartWithWindows"] = "Disable start with Windows",
        ["Tray.EnableStartWithWindows"] = "Enable start with Windows",
        ["Tray.ExitInterlude"] = "Exit Interlude",

        ["PlaybackState.Unknown"] = "Unknown",
        ["PlaybackState.Closed"] = "Closed",
        ["PlaybackState.Playing"] = "Playing",
        ["PlaybackState.Paused"] = "Paused",
        ["PlaybackState.Stopped"] = "Stopped",
        ["AutomationState.WaitingForTarget"] = "Waiting for target",
        ["AutomationState.Idle"] = "Idle",
        ["AutomationState.InterruptionActive"] = "Interruption active",
        ["AutomationState.AutoPaused"] = "Auto-paused",
        ["AutomationState.ResumePending"] = "Resume pending",
        ["AutomationState.ManualOverride"] = "Manual override",
        ["DetectionMode.AudioPeak"] = "Sound activity",
        ["DetectionMode.MediaPlayback"] = "Media playback",
        ["DetectionMode.Hybrid"] = "Hybrid"
    };

    private static readonly IReadOnlyDictionary<string, string> ChineseSimplified = new Dictionary<string, string>
    {
        ["Common.Enabled"] = "已启用",
        ["Common.Disabled"] = "已禁用",
        ["Common.Yes"] = "是",
        ["Common.No"] = "否",
        ["Common.None"] = "无",
        ["Common.NotConfigured"] = "尚未配置",
        ["Common.Available"] = "可用",
        ["Common.Waiting"] = "等待中",
        ["Common.Minimize"] = "最小化",
        ["Common.Close"] = "关闭",
        ["App.UnexpectedError"] = "Interlude 遇到意外错误，详细信息已写入日志目录。",

        ["FirstRun.WindowTitle"] = "欢迎使用 Interlude",
        ["FirstRun.Title"] = "设置默认音乐播放器",
        ["FirstRun.Description"] = "当其他应用开始播放声音时，Interlude 会暂停你选择的音乐播放器，并且只恢复由 Interlude 暂停的音乐。",
        ["FirstRun.Step1"] = "1. 打开你希望 Interlude 控制的音乐播放器。",
        ["FirstRun.Step2"] = "2. 播放任意歌曲，让 Windows 暴露它的媒体会话。",
        ["FirstRun.Step3"] = "3. 选择对应的媒体会话作为默认播放器。",
        ["FirstRun.Later"] = "之后可以在“设置”里修改。",
        ["FirstRun.ChoosePlayer"] = "选择播放器",

        ["Main.Subtitle"] = "Windows 音频协调正在后台运行。",
        ["Main.Automation"] = "自动控制",
        ["Main.DefaultPlayer"] = "默认播放器",
        ["Main.TargetAvailability"] = "目标可用性",
        ["Main.PlaybackState"] = "播放状态",
        ["Main.AutomationState"] = "自动控制状态",
        ["Main.PausedByInterlude"] = "由 Interlude 暂停",
        ["Main.DetectionMode"] = "检测模式",
        ["Main.Enable"] = "启用",
        ["Main.PauseAutomation"] = "暂停自动控制",
        ["Main.ChangePlayer"] = "更换播放器",
        ["Main.Settings"] = "设置",
        ["Main.RestoreMusicNow"] = "立即恢复音乐",
        ["Main.Logs"] = "日志",
        ["Main.ToggleStartup"] = "切换开机启动",
        ["Main.ExitInterlude"] = "退出 Interlude",
        ["Main.Status"] = "状态",
        ["Main.RunningStatus"] = "运行状态",
        ["Main.AutomationSwitch"] = "自动控制",
        ["Main.CurrentPlayer"] = "当前播放器",
        ["Main.GeneralSettings"] = "常规设置",
        ["Main.PlayerSettings"] = "播放器设置",
        ["Main.DetectionSettings"] = "检测设置",
        ["Main.LogDirectory"] = "日志目录",

        ["Settings.WindowTitle"] = "Interlude 设置",
        ["Settings.Save"] = "保存",
        ["Settings.Reset"] = "重置设置",
        ["Settings.General"] = "常规",
        ["Settings.EnableAutomation"] = "启用自动控制",
        ["Settings.StartWithWindows"] = "随 Windows 启动",
        ["Settings.StartMinimizedToTray"] = "启动时最小化到托盘",
        ["Settings.CloseWindowToTray"] = "关闭窗口时最小化到托盘",
        ["Settings.RespectManualPlaybackChanges"] = "尊重手动播放变更",
        ["Settings.OpenLogDirectory"] = "打开日志目录",
        ["Settings.Language"] = "语言",
        ["Settings.Player"] = "播放器",
        ["Settings.CurrentDefaultPlayer"] = "当前默认播放器",
        ["Settings.ChangePlayer"] = "更换播放器",
        ["Settings.Detection"] = "检测",
        ["Settings.Mode"] = "模式",
        ["Settings.ActivityThreshold"] = "声音触发阈值",
        ["Settings.PollIntervalMs"] = "轮询间隔（毫秒）",
        ["Settings.StartConfirmMs"] = "开始确认时间（毫秒）",
        ["Settings.SilenceConfirmMs"] = "静音确认时间（毫秒）",
        ["Settings.ResumeDelayMs"] = "恢复延迟（毫秒）",
        ["Settings.IgnoreSystemSounds"] = "忽略系统声音",
        ["Settings.Advanced"] = "高级",
        ["Settings.ConfigDirectory"] = "配置目录",
        ["Settings.LogDirectory"] = "日志目录",
        ["Settings.DamagedSettings"] = "如果 settings.json 损坏，Interlude 会先备份它，然后创建默认配置文件。",
        ["IgnoreApps.EntryTitle"] = "忽略应用",
        ["IgnoreApps.Manage"] = "管理 >",
        ["IgnoreApps.Summary"] = "已忽略 {0} 个应用",
        ["IgnoreApps.Description"] = "忽略后不会触发暂停。",
        ["IgnoreApps.Title"] = "忽略应用",
        ["IgnoreApps.Search"] = "按应用名称或进程名称搜索",
        ["IgnoreApps.Refresh"] = "刷新应用列表",
        ["IgnoreApps.AddRunning"] = "从正在运行的应用添加",
        ["IgnoreApps.AddManual"] = "手动选择程序文件",
        ["IgnoreApps.ExecutableFilter"] = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
        ["IgnoreApps.Status.Ready"] = "刷新后显示正在运行的音频应用。",
        ["IgnoreApps.Status.Refreshing"] = "正在刷新应用...",
        ["IgnoreApps.Status.FoundApplications"] = "找到 {0} 个应用。",
        ["IgnoreApps.Status.RefreshFailed"] = "刷新失败。请查看日志目录获取详细信息。",
        ["PlayerSelection.WindowTitle"] = "选择默认播放器",
        ["PlayerSelection.Prompt"] = "请选择 Interlude 需要监测的播放器",
        ["PlayerSelection.Refresh"] = "刷新",
        ["PlayerSelection.PlayerList"] = "播放器列表",
        ["PlayerSelection.EmptyTitle"] = "暂未检测到媒体播放器",
        ["PlayerSelection.EmptyHint"] = "请先打开播放器并播放一次音频，然后点击刷新。",
        ["PlayerSelection.Application"] = "应用",
        ["PlayerSelection.Title"] = "标题",
        ["PlayerSelection.State"] = "状态",
        ["PlayerSelection.Cancel"] = "取消",
        ["PlayerSelection.SetAsDefault"] = "设为默认",
        ["PlayerSelection.Status.OpenPlayer"] = "打开音乐播放器，播放任意歌曲，然后刷新。",
        ["PlayerSelection.Status.Refreshing"] = "正在刷新会话...",
        ["PlayerSelection.Status.NoMediaSessions"] = "暂未检测到媒体播放器，请先打开播放器并播放一次音频。",
        ["PlayerSelection.Status.FoundMediaSessions"] = "找到 {0} 个媒体会话。",
        ["PlayerSelection.Status.RefreshFailed"] = "刷新失败。请查看日志目录获取详细信息。",
        ["PlayerSelection.Status.SelectMediaSessionFirst"] = "请先选择一个媒体会话。",
        ["PlayerSelection.Status.DefaultPlayerSaved"] = "默认播放器已保存。",

        ["Tray.Status"] = "Interlude：{0}",
        ["Tray.DefaultPlayer"] = "默认播放器：{0}",
        ["Tray.State"] = "状态：{0}",
        ["Tray.DisableAutomation"] = "禁用自动控制",
        ["Tray.EnableAutomation"] = "启用自动控制",
        ["Tray.RestoreMusicNow"] = "立即恢复音乐",
        ["Tray.ShowMainWindow"] = "显示主窗口",
        ["Tray.ChangeDefaultPlayer"] = "更换默认播放器",
        ["Tray.Settings"] = "设置",
        ["Tray.OpenLogDirectory"] = "打开日志目录",
        ["Tray.DisableStartWithWindows"] = "禁用开机启动",
        ["Tray.EnableStartWithWindows"] = "启用开机启动",
        ["Tray.ExitInterlude"] = "退出 Interlude",

        ["PlaybackState.Unknown"] = "未知",
        ["PlaybackState.Closed"] = "已关闭",
        ["PlaybackState.Playing"] = "播放中",
        ["PlaybackState.Paused"] = "已暂停",
        ["PlaybackState.Stopped"] = "已停止",
        ["AutomationState.WaitingForTarget"] = "等待目标播放器",
        ["AutomationState.Idle"] = "空闲",
        ["AutomationState.InterruptionActive"] = "其他声音正在播放",
        ["AutomationState.AutoPaused"] = "已自动暂停",
        ["AutomationState.ResumePending"] = "等待恢复",
        ["AutomationState.ManualOverride"] = "手动接管",
        ["DetectionMode.AudioPeak"] = "声音活动",
        ["DetectionMode.MediaPlayback"] = "媒体播放",
        ["DetectionMode.Hybrid"] = "混合"
    };

    private readonly SettingsService _settingsService;

    public LocalizationService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public event EventHandler? LanguageChanged;

    public string CurrentLanguage => AppLanguage.Normalize(_settingsService.Current.Language);

    public bool IsChinese => CurrentLanguage == AppLanguage.ChineseSimplified;

    public string T(string key)
    {
        var dictionary = IsChinese ? ChineseSimplified : English;
        return dictionary.TryGetValue(key, out var value)
            ? value
            : English.TryGetValue(key, out var fallback)
                ? fallback
                : key;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, T(key), args);
    }

    public string EnabledDisabled(bool enabled) => T(enabled ? "Common.Enabled" : "Common.Disabled");

    public string YesNo(bool value) => T(value ? "Common.Yes" : "Common.No");

    public string PlaybackState(PlayerPlaybackState state) => T($"PlaybackState.{state}");

    public string AutomationState(AutomationState state) => T($"AutomationState.{state}");

    public string DetectionMode(DetectionMode mode) => T($"DetectionMode.{mode}");

    public void SetLanguage(string languageCode, bool save)
    {
        var normalized = AppLanguage.Normalize(languageCode);
        if (_settingsService.Current.Language == normalized)
        {
            ApplyResources();
            return;
        }

        _settingsService.Current.Language = normalized;
        if (save)
        {
            _settingsService.Save();
        }

        ApplyResources();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyResources()
    {
        if (Application.Current is null)
        {
            return;
        }

        foreach (var key in English.Keys.Union(ChineseSimplified.Keys))
        {
            Application.Current.Resources[key] = T(key);
        }
    }

    public static string TranslateResourceValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var key = value switch
        {
            PlayerPlaybackState state => $"PlaybackState.{state}",
            AutomationState state => $"AutomationState.{state}",
            DetectionMode mode => $"DetectionMode.{mode}",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(key))
        {
            return value.ToString() ?? string.Empty;
        }

        return Application.Current?.Resources[key] as string
            ?? value.ToString()
            ?? string.Empty;
    }
}
