using Interlude.Models;

namespace Interlude.Services;

public sealed class AutomationStateMachine
{
    public AutomationState State { get; private set; } = AutomationState.WaitingForTarget;

    public bool PausedByUs { get; private set; }

    public DateTimeOffset? ResumeDueAt { get; private set; }

    public async Task<AutomationStepResult> StepAsync(
        AutomationStepInput input,
        Func<CancellationToken, Task<bool>> pauseAsync,
        Func<CancellationToken, Task<bool>> playAsync,
        CancellationToken cancellationToken = default)
    {
        var previous = State;
        var action = AutomationStepAction.None;

        if (!input.TargetAvailable)
        {
            if (State != AutomationState.WaitingForTarget || PausedByUs)
            {
                action = AutomationStepAction.TargetLost;
            }

            State = AutomationState.WaitingForTarget;
            PausedByUs = false;
            ResumeDueAt = null;
            return CreateResult(previous, action);
        }

        switch (State)
        {
            case AutomationState.WaitingForTarget:
                State = AutomationState.Idle;
                break;

            case AutomationState.Idle:
                if (input.InterruptionActive)
                {
                    if (input.TargetIsPlaying)
                    {
                        var paused = await pauseAsync(cancellationToken);
                        if (paused)
                        {
                            PausedByUs = true;
                            State = AutomationState.AutoPaused;
                            action = AutomationStepAction.PauseSucceeded;
                        }
                        else
                        {
                            PausedByUs = false;
                            State = AutomationState.InterruptionActive;
                            action = AutomationStepAction.PauseFailed;
                        }
                    }
                    else
                    {
                        PausedByUs = false;
                        State = AutomationState.InterruptionActive;
                    }
                }
                break;

            case AutomationState.InterruptionActive:
                if (!input.InterruptionActive)
                {
                    State = AutomationState.Idle;
                }
                break;

            case AutomationState.AutoPaused:
                if (input.RespectManualOverride && input.TargetIsPlaying)
                {
                    PausedByUs = false;
                    ResumeDueAt = null;
                    State = AutomationState.ManualOverride;
                    action = AutomationStepAction.ManualOverrideDetected;
                    break;
                }

                if (!input.InterruptionActive)
                {
                    ResumeDueAt = input.Now.Add(input.ResumeDelay);
                    State = AutomationState.ResumePending;
                    action = AutomationStepAction.ResumeScheduled;
                }
                break;

            case AutomationState.ResumePending:
                if (input.InterruptionActive)
                {
                    ResumeDueAt = null;
                    State = AutomationState.AutoPaused;
                    action = AutomationStepAction.ResumeCancelled;
                }
                else if (ResumeDueAt is not null && input.Now >= ResumeDueAt)
                {
                    if (PausedByUs && input.TargetIsPaused)
                    {
                        var played = await playAsync(cancellationToken);
                        if (played)
                        {
                            PausedByUs = false;
                            ResumeDueAt = null;
                            State = AutomationState.Idle;
                            action = AutomationStepAction.PlaySucceeded;
                        }
                        else
                        {
                            PausedByUs = false;
                            ResumeDueAt = null;
                            State = AutomationState.Idle;
                            action = AutomationStepAction.PlayFailed;
                        }
                    }
                    else
                    {
                        PausedByUs = false;
                        ResumeDueAt = null;
                        State = AutomationState.Idle;
                    }
                }
                break;

            case AutomationState.ManualOverride:
                if (!input.InterruptionActive)
                {
                    State = AutomationState.Idle;
                }
                break;
        }

        return CreateResult(previous, action);
    }

    public void MarkManualOverride()
    {
        PausedByUs = false;
        ResumeDueAt = null;
        State = AutomationState.ManualOverride;
    }

    public void ResetForTargetLoss()
    {
        PausedByUs = false;
        ResumeDueAt = null;
        State = AutomationState.WaitingForTarget;
    }

    private AutomationStepResult CreateResult(
        AutomationState previousState,
        AutomationStepAction action)
    {
        return new AutomationStepResult
        {
            PreviousState = previousState,
            CurrentState = State,
            PausedByUs = PausedByUs,
            Action = action
        };
    }
}
