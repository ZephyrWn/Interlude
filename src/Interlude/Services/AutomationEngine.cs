using Interlude.Controllers;
using Interlude.Models;

namespace Interlude.Services;

public sealed class AutomationEngine : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly MediaSessionService _mediaSessionService;
    private readonly AudioActivityService _audioActivityService;
    private readonly PlayerFadeService _playerFadeService;
    private readonly LoggingService _log;
    private readonly AutomationStateMachine _stateMachine = new();
    private readonly SemaphoreSlim _controlLock = new(1, 1);
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _loopTask;
    private DateTimeOffset? _rawActiveSince;
    private DateTimeOffset? _rawSilentSince;
    private bool _confirmedInterruption;
    private bool _disposed;

    public AutomationEngine(
        SettingsService settingsService,
        MediaSessionService mediaSessionService,
        AudioActivityService audioActivityService,
        PlayerFadeService playerFadeService,
        LoggingService log)
    {
        _settingsService = settingsService;
        _mediaSessionService = mediaSessionService;
        _audioActivityService = audioActivityService;
        _playerFadeService = playerFadeService;
        _log = log;
    }

    public event EventHandler<AutomationSnapshot>? SnapshotChanged;

    public AutomationSnapshot CurrentSnapshot { get; private set; } = new();

    public AutomationState State => _stateMachine.State;

    public bool PausedByUs => _stateMachine.PausedByUs;

    public void Start()
    {
        if (_loopTask is { IsCompleted: false })
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunAsync(_cancellationTokenSource.Token));
        _log.Info("Automation engine started.");
    }

    public async Task StopAsync()
    {
        if (_cancellationTokenSource is null)
        {
            return;
        }

        _cancellationTokenSource.Cancel();

        try
        {
            if (_loopTask is not null)
            {
                await _loopTask;
            }
        }
        catch (OperationCanceledException)
        {
        }

        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
        _loopTask = null;
        _log.Info("Automation engine stopped.");
    }

    public async Task<bool> ImmediateRestoreAsync(CancellationToken cancellationToken = default)
    {
        await _controlLock.WaitAsync(cancellationToken);
        try
        {
            if (!_stateMachine.PausedByUs)
            {
                return false;
            }

            var settings = _settingsService.Current;
            var controller = await _mediaSessionService.FindTargetControllerAsync(
                settings.TargetPlayer.SourceAppUserModelId,
                cancellationToken);

            if (controller is null ||
                controller.GetPlaybackState() != PlayerPlaybackState.Paused)
            {
                return false;
            }

            var played = await _playerFadeService.PlayWithFadeAsync(controller, settings, cancellationToken);
            if (!played)
            {
                _log.Warning("Immediate restore was requested, but TryPlayAsync returned false.");
                return false;
            }

            _stateMachine.MarkManualOverride();
            _log.Info("Immediate restore succeeded; manual override state entered.");
            PublishSnapshot(settings, controller, [], targetAvailable: true);
            return true;
        }
        catch (Exception ex)
        {
            _log.Error("Immediate restore failed.", ex);
            return false;
        }
        finally
        {
            _controlLock.Release();
        }
    }

    public void PauseForSleep()
    {
        _stateMachine.ResetForTargetLoss();
        ResetInterruptionConfirmation();
        _log.Info("System suspend detected; automation state was cleared.");
    }

    public void ResumeAfterSleep()
    {
        _mediaSessionService.Reinitialize();
        _stateMachine.ResetForTargetLoss();
        ResetInterruptionConfirmation();
        _log.Info("System resume detected; media and audio state will be refreshed.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _controlLock.Dispose();
        _disposed = true;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunSingleIterationAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Error("Automation loop iteration failed.", ex);
                ResetInterruptionConfirmation();
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task RunSingleIterationAsync(CancellationToken cancellationToken)
    {
        var settings = _settingsService.Current;
        var delay = Math.Clamp(settings.Detection.PollIntervalMs, 25, 1000);

        if (!settings.HasTargetPlayer)
        {
            _stateMachine.ResetForTargetLoss();
            ResetInterruptionConfirmation();
            PublishSnapshot(settings, controller: null, [], targetAvailable: false);
            await Task.Delay(delay, cancellationToken);
            return;
        }

        if (!settings.Enabled)
        {
            PublishSnapshot(settings, controller: null, [], targetAvailable: false);
            await Task.Delay(delay, cancellationToken);
            return;
        }

        var controller = await _mediaSessionService.FindTargetControllerAsync(
            settings.TargetPlayer.SourceAppUserModelId,
            cancellationToken);

        if (controller is null)
        {
            await _controlLock.WaitAsync(cancellationToken);
            try
            {
                var result = await _stateMachine.StepAsync(
                    new AutomationStepInput
                    {
                        TargetAvailable = false,
                        Now = DateTimeOffset.Now
                    },
                    _ => Task.FromResult(false),
                    _ => Task.FromResult(false),
                    cancellationToken);

                LogStepResult(result, []);
            }
            finally
            {
                _controlLock.Release();
            }

            ResetInterruptionConfirmation();
            PublishSnapshot(settings, controller: null, [], targetAvailable: false);
            await Task.Delay(delay, cancellationToken);
            return;
        }

        var interruption = await DetectInterruptionAsync(settings, cancellationToken);
        var confirmedInterruption = UpdateInterruptionConfirmation(
            interruption.RawActive,
            settings.Detection,
            DateTimeOffset.Now);
        var effectiveInterruption = _stateMachine.State == AutomationState.ResumePending
            ? confirmedInterruption || interruption.RawActive
            : confirmedInterruption;

        var playbackState = controller.GetPlaybackState();

        await _controlLock.WaitAsync(cancellationToken);
        try
        {
            var result = await _stateMachine.StepAsync(
                new AutomationStepInput
                {
                    TargetAvailable = true,
                    TargetPlaybackState = playbackState,
                    InterruptionActive = effectiveInterruption,
                    ResumeDelay = TimeSpan.FromMilliseconds(settings.Detection.ResumeDelayMs),
                    RespectManualOverride = settings.RespectManualOverride,
                    Now = DateTimeOffset.Now
                },
                token => SafePauseAsync(controller, token),
                token => SafePlayAsync(controller, token),
                cancellationToken);

            LogStepResult(result, interruption.TriggeringProcesses);
        }
        finally
        {
            _controlLock.Release();
        }

        PublishSnapshot(settings, controller, interruption.TriggeringProcesses, targetAvailable: true);
        await Task.Delay(delay, cancellationToken);
    }

    private async Task<InterruptionSnapshot> DetectInterruptionAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var triggerNames = new List<string>();
        var audioSessions = Array.Empty<AudioSessionInfo>();
        var rawActive = false;

        if (settings.Detection.Mode is
            DetectionMode.AudioPeak or
            DetectionMode.MediaPlayback or
            DetectionMode.Hybrid)
        {
            var audio = await _audioActivityService.GetInterruptionSnapshotAsync(
                settings,
                cancellationToken);
            audioSessions = audio.AudioSessions.ToArray();

            if (settings.Detection.Mode is DetectionMode.AudioPeak or DetectionMode.Hybrid)
            {
                triggerNames.AddRange(audio.TriggeringProcesses);
                rawActive |= audio.RawActive;
            }
            else
            {
                // QQ and WeChat play voice messages and embedded videos through Core Audio,
                // but do not publish a Windows media session. Treat their audible sessions
                // as a compatibility fallback without broadening media mode to every sound.
                var mediaAudioFallbacks = audio.TriggeringProcesses
                    .Where(MediaAudioFallbackMatcher.IsSupportedProcess)
                    .ToArray();
                triggerNames.AddRange(mediaAudioFallbacks.Select(name => $"Media audio: {name}"));
                rawActive |= mediaAudioFallbacks.Length > 0;
            }
        }

        if (settings.Detection.Mode is DetectionMode.MediaPlayback or DetectionMode.Hybrid)
        {
            var otherPlaying = await _mediaSessionService.GetOtherPlayingSessionsAsync(
                settings,
                cancellationToken);
            if (otherPlaying.Count > 0)
            {
                rawActive = true;
                triggerNames.AddRange(otherPlaying.Select(session => $"Media: {session.DisplayName}"));
            }
        }

        return new InterruptionSnapshot
        {
            RawActive = rawActive,
            AudioSessions = audioSessions,
            TriggeringProcesses = triggerNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private bool UpdateInterruptionConfirmation(
        bool rawActive,
        DetectionSettings settings,
        DateTimeOffset now)
    {
        if (rawActive)
        {
            _rawSilentSince = null;
            _rawActiveSince ??= now;

            if (!_confirmedInterruption &&
                now - _rawActiveSince.Value >= TimeSpan.FromMilliseconds(settings.StartConfirmMs))
            {
                _confirmedInterruption = true;
            }

            return _confirmedInterruption;
        }

        _rawActiveSince = null;
        if (!_confirmedInterruption)
        {
            return false;
        }

        _rawSilentSince ??= now;
        if (now - _rawSilentSince.Value >= TimeSpan.FromMilliseconds(settings.SilenceConfirmMs))
        {
            _confirmedInterruption = false;
            _rawSilentSince = null;
        }

        return _confirmedInterruption;
    }

    private void ResetInterruptionConfirmation()
    {
        _rawActiveSince = null;
        _rawSilentSince = null;
        _confirmedInterruption = false;
    }

    private async Task<bool> SafePauseAsync(
        IPlayerController controller,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = _settingsService.Current;
            var paused = await _playerFadeService.PauseWithFadeAsync(controller, settings, cancellationToken);
            _log.Info(paused
                ? $"Paused target player: {controller.DisplayName}."
                : $"Pause returned false for target player: {controller.DisplayName}.");
            return paused;
        }
        catch (Exception ex)
        {
            _log.Error("Pause failed.", ex);
            return false;
        }
    }

    private async Task<bool> SafePlayAsync(
        IPlayerController controller,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = _settingsService.Current;
            var played = await _playerFadeService.PlayWithFadeAsync(controller, settings, cancellationToken);
            _log.Info(played
                ? $"Resumed target player: {controller.DisplayName}."
                : $"Play returned false for target player: {controller.DisplayName}.");
            return played;
        }
        catch (Exception ex)
        {
            _log.Error("Play failed.", ex);
            return false;
        }
    }

    private void LogStepResult(
        AutomationStepResult result,
        IReadOnlyList<string> triggers)
    {
        if (result.StateChanged)
        {
            _log.Info($"Automation state changed: {result.PreviousState} -> {result.CurrentState}.");
        }

        if (result.Action != AutomationStepAction.None)
        {
            var triggerText = triggers.Count == 0 ? "none" : string.Join(", ", triggers);
            _log.Info($"Automation action: {result.Action}. Triggers: {triggerText}.");
        }
    }

    private void PublishSnapshot(
        AppSettings settings,
        IPlayerController? controller,
        IReadOnlyList<string> triggers,
        bool targetAvailable)
    {
        var snapshot = new AutomationSnapshot
        {
            Enabled = settings.Enabled,
            TargetDisplayName = string.IsNullOrWhiteSpace(settings.TargetPlayer.DisplayName)
                ? "Not configured"
                : settings.TargetPlayer.DisplayName,
            PlaybackState = controller?.GetPlaybackState() ?? PlayerPlaybackState.Unknown,
            State = _stateMachine.State,
            PausedByUs = _stateMachine.PausedByUs,
            TriggeringProcesses = triggers.Count == 0 ? "None" : string.Join(", ", triggers),
            DetectionMode = settings.Detection.Mode,
            StartWithWindows = settings.StartWithWindows,
            TargetAvailable = targetAvailable
        };

        CurrentSnapshot = snapshot;
        SnapshotChanged?.Invoke(this, snapshot);
    }
}
