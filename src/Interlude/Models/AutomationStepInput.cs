namespace Interlude.Models;

public sealed class AutomationStepInput
{
    public bool TargetAvailable { get; init; }

    public PlayerPlaybackState TargetPlaybackState { get; init; }

    public bool InterruptionActive { get; init; }

    public TimeSpan ResumeDelay { get; init; }

    public bool RespectManualOverride { get; init; } = true;

    public DateTimeOffset Now { get; init; } = DateTimeOffset.Now;

    public bool TargetIsPlaying => TargetPlaybackState == PlayerPlaybackState.Playing;

    public bool TargetIsPaused => TargetPlaybackState == PlayerPlaybackState.Paused;
}
