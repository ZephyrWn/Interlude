namespace Interlude.Models;

public enum AutomationStepAction
{
    None,
    PauseSucceeded,
    PauseFailed,
    ResumeScheduled,
    ResumeCancelled,
    PlaySucceeded,
    PlayFailed,
    ManualOverrideDetected,
    TargetLost
}
