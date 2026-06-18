using Interlude.Models;
using Windows.Media.Control;

namespace Interlude.Controllers;

public sealed class GsmtcPlayerController : IPlayerController
{
    private readonly GlobalSystemMediaTransportControlsSession _session;

    public GsmtcPlayerController(
        GlobalSystemMediaTransportControlsSession session,
        string displayName)
    {
        _session = session;
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? session.SourceAppUserModelId
            : displayName;
    }

    public string DisplayName { get; }

    public string SourceAppUserModelId => _session.SourceAppUserModelId;

    public bool CanPause => _session.GetPlaybackInfo()?.Controls?.IsPauseEnabled == true;

    public bool CanPlay => _session.GetPlaybackInfo()?.Controls?.IsPlayEnabled == true;

    public PlayerPlaybackState GetPlaybackState()
    {
        var status = _session.GetPlaybackInfo()?.PlaybackStatus;
        return status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => PlayerPlaybackState.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => PlayerPlaybackState.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => PlayerPlaybackState.Stopped,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => PlayerPlaybackState.Closed,
            _ => PlayerPlaybackState.Unknown
        };
    }

    public async Task<bool> PauseAsync(CancellationToken cancellationToken = default)
    {
        if (!CanPause)
        {
            return false;
        }

        return await _session.TryPauseAsync().AsTask(cancellationToken);
    }

    public async Task<bool> PlayAsync(CancellationToken cancellationToken = default)
    {
        if (!CanPlay)
        {
            return false;
        }

        return await _session.TryPlayAsync().AsTask(cancellationToken);
    }
}
