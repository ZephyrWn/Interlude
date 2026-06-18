namespace Interlude.Models;

public sealed class AudioSessionInfo
{
    public string ProcessName { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public int ProcessId { get; set; }

    public double PeakValue { get; set; }

    public bool IsAudible { get; set; }

    public bool IsTargetPlayer { get; set; }

    public bool IsExcluded { get; set; }

    public bool IsSystemSound { get; set; }

    public string State { get; set; } = string.Empty;

    public string DisplayName => ProcessId > 0
        ? $"{ProcessName} ({ProcessId})"
        : ProcessName;
}
