namespace Interlude.Models;

public sealed class InterruptionSnapshot
{
    public bool RawActive { get; init; }

    public IReadOnlyList<AudioSessionInfo> AudioSessions { get; init; } = [];

    public IReadOnlyList<string> TriggeringProcesses { get; init; } = [];
}
