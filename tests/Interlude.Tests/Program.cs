using Interlude.Models;
using Interlude.Services;

namespace Interlude.Tests;

internal static class Program
{
    private static async Task<int> Main()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            ("playing music is auto-paused when another app starts audio", PlayingMusicIsPaused),
            ("already-paused music is not auto-played after interruption", AlreadyPausedMusicIsNotAutoPlayed),
            ("auto-paused music resumes after silence delay", AutoPausedMusicResumes),
            ("resume delay is cancelled when audio returns", ResumeIsCancelledWhenAudioReturns),
            ("manual playback during auto-pause enters manual override", ManualPlaybackEntersManualOverride),
            ("manual override does not auto-pause while interruption continues", ManualOverrideDoesNotPauseAgain),
            ("target exit clears pausedByUs", TargetExitClearsPausedByUs),
            ("target restart does not auto-play", TargetRestartDoesNotAutoPlay),
            ("pause failure does not set pausedByUs", PauseFailureDoesNotSetPausedByUs),
            ("play failure clears automation ownership safely", PlayFailureClearsOwnership),
            ("default detection mode is audio peak", DefaultDetectionModeIsAudioPeak),
            ("default start confirmation is immediate", DefaultStartConfirmationIsImmediate),
            ("version one media playback default migrates to audio peak", VersionOneMediaPlaybackDefaultMigratesToAudioPeak),
            ("version two start confirmation default migrates to immediate", VersionTwoStartConfirmationDefaultMigratesToImmediate),
            ("version three hybrid default migrates to audio peak", VersionThreeHybridDefaultMigratesToAudioPeak)
        };

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL {test.Name}");
                Console.WriteLine(ex.Message);
            }
        }

        Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed.");
        return failures == 0 ? 0 : 1;
    }

    private static async Task PlayingMusicIsPaused()
    {
        var machine = await CreateIdleMachine();
        var pauseCount = 0;

        var result = await machine.StepAsync(
            Input(PlayerPlaybackState.Playing, interruption: true),
            _ =>
            {
                pauseCount++;
                return Task.FromResult(true);
            },
            _ => Task.FromResult(false));

        AssertEqual(AutomationState.AutoPaused, machine.State);
        AssertTrue(machine.PausedByUs);
        AssertEqual(1, pauseCount);
        AssertEqual(AutomationStepAction.PauseSucceeded, result.Action);
    }

    private static async Task AlreadyPausedMusicIsNotAutoPlayed()
    {
        var machine = await CreateIdleMachine();
        var playCount = 0;

        await machine.StepAsync(
            Input(PlayerPlaybackState.Paused, interruption: true),
            _ => Task.FromResult(true),
            _ =>
            {
                playCount++;
                return Task.FromResult(true);
            });

        AssertEqual(AutomationState.InterruptionActive, machine.State);
        AssertFalse(machine.PausedByUs);

        await machine.StepAsync(
            Input(PlayerPlaybackState.Paused, interruption: false),
            _ => Task.FromResult(true),
            _ =>
            {
                playCount++;
                return Task.FromResult(true);
            });

        AssertEqual(AutomationState.Idle, machine.State);
        AssertEqual(0, playCount);
    }

    private static async Task AutoPausedMusicResumes()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var machine = await CreateAutoPausedMachine(now);
        var playCount = 0;

        await machine.StepAsync(
            Input(PlayerPlaybackState.Paused, interruption: false, now: now),
            _ => Task.FromResult(false),
            _ => Task.FromResult(false));

        AssertEqual(AutomationState.ResumePending, machine.State);

        var result = await machine.StepAsync(
            Input(PlayerPlaybackState.Paused, interruption: false, now: now.AddMilliseconds(100)),
            _ => Task.FromResult(false),
            _ =>
            {
                playCount++;
                return Task.FromResult(true);
            });

        AssertEqual(AutomationState.Idle, machine.State);
        AssertFalse(machine.PausedByUs);
        AssertEqual(1, playCount);
        AssertEqual(AutomationStepAction.PlaySucceeded, result.Action);
    }

    private static async Task ResumeIsCancelledWhenAudioReturns()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var machine = await CreateAutoPausedMachine(now);
        var playCount = 0;

        await machine.StepAsync(
            Input(PlayerPlaybackState.Paused, interruption: false, now: now),
            _ => Task.FromResult(false),
            _ => Task.FromResult(false));

        var result = await machine.StepAsync(
            Input(PlayerPlaybackState.Paused, interruption: true, now: now.AddMilliseconds(50)),
            _ => Task.FromResult(false),
            _ =>
            {
                playCount++;
                return Task.FromResult(true);
            });

        AssertEqual(AutomationState.AutoPaused, machine.State);
        AssertTrue(machine.PausedByUs);
        AssertEqual(0, playCount);
        AssertEqual(AutomationStepAction.ResumeCancelled, result.Action);
    }

    private static async Task ManualPlaybackEntersManualOverride()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var machine = await CreateAutoPausedMachine(now);

        var result = await machine.StepAsync(
            Input(PlayerPlaybackState.Playing, interruption: true, now: now.AddMilliseconds(10)),
            _ => Task.FromResult(false),
            _ => Task.FromResult(false));

        AssertEqual(AutomationState.ManualOverride, machine.State);
        AssertFalse(machine.PausedByUs);
        AssertEqual(AutomationStepAction.ManualOverrideDetected, result.Action);
    }

    private static async Task ManualOverrideDoesNotPauseAgain()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var machine = await CreateAutoPausedMachine(now);
        var pauseCount = 1;

        await machine.StepAsync(
            Input(PlayerPlaybackState.Playing, interruption: true, now: now.AddMilliseconds(10)),
            _ =>
            {
                pauseCount++;
                return Task.FromResult(true);
            },
            _ => Task.FromResult(false));

        await machine.StepAsync(
            Input(PlayerPlaybackState.Playing, interruption: true, now: now.AddMilliseconds(20)),
            _ =>
            {
                pauseCount++;
                return Task.FromResult(true);
            },
            _ => Task.FromResult(false));

        AssertEqual(AutomationState.ManualOverride, machine.State);
        AssertEqual(1, pauseCount);

        await machine.StepAsync(
            Input(PlayerPlaybackState.Playing, interruption: false, now: now.AddMilliseconds(200)),
            _ => Task.FromResult(true),
            _ => Task.FromResult(false));

        AssertEqual(AutomationState.Idle, machine.State);
    }

    private static async Task TargetExitClearsPausedByUs()
    {
        var machine = await CreateAutoPausedMachine(DateTimeOffset.Now);

        var result = await machine.StepAsync(
            new AutomationStepInput { TargetAvailable = false, Now = DateTimeOffset.Now },
            _ => Task.FromResult(false),
            _ => Task.FromResult(false));

        AssertEqual(AutomationState.WaitingForTarget, machine.State);
        AssertFalse(machine.PausedByUs);
        AssertEqual(AutomationStepAction.TargetLost, result.Action);
    }

    private static async Task TargetRestartDoesNotAutoPlay()
    {
        var machine = await CreateAutoPausedMachine(DateTimeOffset.Now);
        var playCount = 0;

        await machine.StepAsync(
            new AutomationStepInput { TargetAvailable = false, Now = DateTimeOffset.Now },
            _ => Task.FromResult(false),
            _ => Task.FromResult(false));

        await machine.StepAsync(
            Input(PlayerPlaybackState.Paused, interruption: false),
            _ => Task.FromResult(false),
            _ =>
            {
                playCount++;
                return Task.FromResult(true);
            });

        AssertEqual(AutomationState.Idle, machine.State);
        AssertEqual(0, playCount);
    }

    private static async Task PauseFailureDoesNotSetPausedByUs()
    {
        var machine = await CreateIdleMachine();

        var result = await machine.StepAsync(
            Input(PlayerPlaybackState.Playing, interruption: true),
            _ => Task.FromResult(false),
            _ => Task.FromResult(false));

        AssertEqual(AutomationState.InterruptionActive, machine.State);
        AssertFalse(machine.PausedByUs);
        AssertEqual(AutomationStepAction.PauseFailed, result.Action);
    }

    private static async Task PlayFailureClearsOwnership()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var machine = await CreateAutoPausedMachine(now);

        await machine.StepAsync(
            Input(PlayerPlaybackState.Paused, interruption: false, now: now),
            _ => Task.FromResult(false),
            _ => Task.FromResult(false));

        var result = await machine.StepAsync(
            Input(PlayerPlaybackState.Paused, interruption: false, now: now.AddMilliseconds(100)),
            _ => Task.FromResult(false),
            _ => Task.FromResult(false));

        AssertEqual(AutomationState.Idle, machine.State);
        AssertFalse(machine.PausedByUs);
        AssertEqual(AutomationStepAction.PlayFailed, result.Action);
    }

    private static Task DefaultDetectionModeIsAudioPeak()
    {
        var settings = AppSettings.CreateDefault();
        settings.Normalize();

        AssertEqual(4, settings.SchemaVersion);
        AssertEqual(DetectionMode.AudioPeak, settings.Detection.Mode);
        return Task.CompletedTask;
    }

    private static Task DefaultStartConfirmationIsImmediate()
    {
        var settings = AppSettings.CreateDefault();
        settings.Normalize();

        AssertEqual(0, settings.Detection.StartConfirmMs);
        return Task.CompletedTask;
    }

    private static Task VersionOneMediaPlaybackDefaultMigratesToAudioPeak()
    {
        var settings = AppSettings.CreateDefault();
        settings.SchemaVersion = 1;
        settings.Detection.Mode = DetectionMode.MediaPlayback;
        settings.Normalize();

        AssertEqual(4, settings.SchemaVersion);
        AssertEqual(DetectionMode.AudioPeak, settings.Detection.Mode);
        return Task.CompletedTask;
    }

    private static Task VersionTwoStartConfirmationDefaultMigratesToImmediate()
    {
        var settings = AppSettings.CreateDefault();
        settings.SchemaVersion = 2;
        settings.Detection.StartConfirmMs = 100;
        settings.Normalize();

        AssertEqual(4, settings.SchemaVersion);
        AssertEqual(0, settings.Detection.StartConfirmMs);
        return Task.CompletedTask;
    }

    private static Task VersionThreeHybridDefaultMigratesToAudioPeak()
    {
        var settings = AppSettings.CreateDefault();
        settings.SchemaVersion = 3;
        settings.Detection.Mode = DetectionMode.Hybrid;
        settings.Normalize();

        AssertEqual(4, settings.SchemaVersion);
        AssertEqual(DetectionMode.AudioPeak, settings.Detection.Mode);
        return Task.CompletedTask;
    }

    private static async Task<AutomationStateMachine> CreateIdleMachine()
    {
        var machine = new AutomationStateMachine();
        await machine.StepAsync(
            Input(PlayerPlaybackState.Playing, interruption: false),
            _ => Task.FromResult(false),
            _ => Task.FromResult(false));
        AssertEqual(AutomationState.Idle, machine.State);
        return machine;
    }

    private static async Task<AutomationStateMachine> CreateAutoPausedMachine(DateTimeOffset now)
    {
        var machine = await CreateIdleMachine();
        await machine.StepAsync(
            Input(PlayerPlaybackState.Playing, interruption: true, now: now),
            _ => Task.FromResult(true),
            _ => Task.FromResult(false));
        AssertEqual(AutomationState.AutoPaused, machine.State);
        AssertTrue(machine.PausedByUs);
        return machine;
    }

    private static AutomationStepInput Input(
        PlayerPlaybackState playbackState,
        bool interruption,
        DateTimeOffset? now = null)
    {
        return new AutomationStepInput
        {
            TargetAvailable = true,
            TargetPlaybackState = playbackState,
            InterruptionActive = interruption,
            ResumeDelay = TimeSpan.FromMilliseconds(100),
            RespectManualOverride = true,
            Now = now ?? DateTimeOffset.Now
        };
    }

    private static void AssertTrue(bool value)
    {
        if (!value)
        {
            throw new InvalidOperationException("Expected true.");
        }
    }

    private static void AssertFalse(bool value)
    {
        if (value)
        {
            throw new InvalidOperationException("Expected false.");
        }
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
        }
    }
}
