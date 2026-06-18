namespace Interlude.Models;

public sealed class AutomationStepResult
{
    public AutomationState PreviousState { get; init; }

    public AutomationState CurrentState { get; init; }

    public bool PausedByUs { get; init; }

    public AutomationStepAction Action { get; init; }

    public bool StateChanged => PreviousState != CurrentState;
}
