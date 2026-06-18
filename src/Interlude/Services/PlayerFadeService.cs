using Interlude.Controllers;
using Interlude.Models;

namespace Interlude.Services;

public sealed class PlayerFadeService : IDisposable
{
    private readonly AudioSessionVolumeService _volumeService;
    private readonly LoggingService _log;
    private readonly object _sync = new();
    private CancellationTokenSource? _fadeCancellation;
    private float? _savedVolume;

    public PlayerFadeService(AudioSessionVolumeService volumeService, LoggingService log)
    {
        _volumeService = volumeService;
        _log = log;
    }

    public async Task<bool> PauseWithFadeAsync(
        IPlayerController controller,
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        var linked = CreateFadeCancellation(cancellationToken);
        try
        {
            var currentVolume = await _volumeService.GetTargetVolumeAsync(settings, linked.Token);
            if (currentVolume is null)
            {
                return await controller.PauseAsync(cancellationToken);
            }

            if (_savedVolume is null || currentVolume.Value > 0.01f)
            {
                _savedVolume = currentVolume.Value;
            }

            await _volumeService.FadeTargetVolumeAsync(
                settings,
                currentVolume.Value,
                0,
                AudioSessionVolumeService.DefaultFadeDuration,
                linked.Token);

            return await controller.PauseAsync(cancellationToken);
        }
        finally
        {
            ClearFadeCancellation(linked);
        }
    }

    public async Task<bool> PlayWithFadeAsync(
        IPlayerController controller,
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        var linked = CreateFadeCancellation(cancellationToken);
        try
        {
            var targetVolume = _savedVolume ?? await _volumeService.GetTargetVolumeAsync(settings, linked.Token);
            if (targetVolume is null)
            {
                return await controller.PlayAsync(cancellationToken);
            }

            await _volumeService.SetTargetVolumeAsync(settings, 0, linked.Token);
            var played = await controller.PlayAsync(cancellationToken);
            if (!played)
            {
                await RestoreVolumeBestEffortAsync(settings, targetVolume.Value);
                return false;
            }

            await _volumeService.FadeTargetVolumeAsync(
                settings,
                0,
                targetVolume.Value,
                AudioSessionVolumeService.DefaultFadeDuration,
                linked.Token);
            _savedVolume = null;
            return true;
        }
        finally
        {
            ClearFadeCancellation(linked);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _fadeCancellation?.Cancel();
            _fadeCancellation?.Dispose();
            _fadeCancellation = null;
        }
    }

    private CancellationTokenSource CreateFadeCancellation(CancellationToken outerToken)
    {
        lock (_sync)
        {
            _fadeCancellation?.Cancel();
            _fadeCancellation?.Dispose();
            _fadeCancellation = CancellationTokenSource.CreateLinkedTokenSource(outerToken);
            return _fadeCancellation;
        }
    }

    private void ClearFadeCancellation(CancellationTokenSource source)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_fadeCancellation, source))
            {
                _fadeCancellation = null;
            }
        }

        source.Dispose();
    }

    private async Task RestoreVolumeBestEffortAsync(AppSettings settings, float volume)
    {
        try
        {
            await _volumeService.SetTargetVolumeAsync(settings, volume);
        }
        catch (Exception ex)
        {
            _log.Error("Failed to restore target volume after play failure.", ex);
        }
    }
}
