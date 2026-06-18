namespace Interlude.Models;

public enum AutomationState
{
    WaitingForTarget,
    Idle,
    InterruptionActive,
    AutoPaused,
    ResumePending,
    ManualOverride
}
