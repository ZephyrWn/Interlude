namespace Interlude.Models;

public sealed class AutomationSnapshot
{
    public bool Enabled { get; init; }

    public string TargetDisplayName { get; init; } = string.Empty;

    public PlayerPlaybackState PlaybackState { get; init; } = PlayerPlaybackState.Unknown;

    public AutomationState State { get; init; } = AutomationState.WaitingForTarget;

    public bool PausedByUs { get; init; }

    public string TriggeringProcesses { get; init; } = string.Empty;

    public DetectionMode DetectionMode { get; init; } = DetectionMode.AudioPeak;

    public bool StartWithWindows { get; init; }

    public bool TargetAvailable { get; init; }
}
