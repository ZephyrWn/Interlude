using Interlude.Controllers;
using Interlude.Models;
using Windows.Media.Control;

namespace Interlude.Services;

public sealed class MediaSessionService
{
    private readonly IgnoredApplicationMatcher _ignoredApplicationMatcher;
    private readonly LoggingService _log;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private GlobalSystemMediaTransportControlsSessionManager? _manager;

    public MediaSessionService(IgnoredApplicationMatcher ignoredApplicationMatcher, LoggingService log)
    {
        _ignoredApplicationMatcher = ignoredApplicationMatcher;
        _log = log;
    }

    public event EventHandler? SessionsChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is not null)
        {
            return;
        }

        await _initializeLock.WaitAsync(cancellationToken);
        try
        {
            if (_manager is not null)
            {
                return;
            }

            _manager = await GlobalSystemMediaTransportControlsSessionManager
                .RequestAsync()
                .AsTask(cancellationToken);
            _manager.SessionsChanged += (_, _) => SessionsChanged?.Invoke(this, EventArgs.Empty);
            _log.Info("GSMTC session manager initialized.");
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    public void Reinitialize()
    {
        _manager = null;
    }

    public async Task<IReadOnlyList<MediaSessionInfo>> GetSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var result = new List<MediaSessionInfo>();

        foreach (var session in _manager!.GetSessions())
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(await CreateInfoAsync(session, cancellationToken));
        }

        return result;
    }

    public async Task<IPlayerController?> FindTargetControllerAsync(
        string sourceAppUserModelId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceAppUserModelId))
        {
            return null;
        }

        await InitializeAsync(cancellationToken);

        foreach (var session in _manager!.GetSessions())
        {
            if (string.Equals(
                    session.SourceAppUserModelId,
                    sourceAppUserModelId,
                    StringComparison.OrdinalIgnoreCase))
            {
                var info = await CreateInfoAsync(session, cancellationToken);
                return new GsmtcPlayerController(session, info.DisplayName);
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<MediaSessionInfo>> GetOtherPlayingSessionsAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        var sessions = await GetSessionsAsync(cancellationToken);
        return sessions
            .Where(session =>
                !string.Equals(
                    session.SourceAppUserModelId,
                    settings.TargetPlayer.SourceAppUserModelId,
                    StringComparison.OrdinalIgnoreCase) &&
                !_ignoredApplicationMatcher.IsIgnoredMediaSession(session, settings) &&
                session.PlaybackState == PlayerPlaybackState.Playing)
            .ToList();
    }

    private static async Task<MediaSessionInfo> CreateInfoAsync(
        GlobalSystemMediaTransportControlsSession session,
        CancellationToken cancellationToken)
    {
        var info = session.GetPlaybackInfo();
        string title = string.Empty;
        string artist = string.Empty;

        try
        {
            var properties = await session.TryGetMediaPropertiesAsync().AsTask(cancellationToken);
            title = properties.Title ?? string.Empty;
            artist = properties.Artist ?? string.Empty;
        }
        catch
        {
            // Media properties are optional and sometimes unavailable for protected sessions.
        }

        var display = PlayerIdentityResolver.ResolveDisplayName(session.SourceAppUserModelId);

        return new MediaSessionInfo
        {
            DisplayName = display,
            SourceAppUserModelId = session.SourceAppUserModelId,
            Title = title,
            Artist = artist,
            PlaybackState = MapPlaybackState(info?.PlaybackStatus),
            CanPause = info?.Controls?.IsPauseEnabled == true,
            CanPlay = info?.Controls?.IsPlayEnabled == true,
            CanToggle = info?.Controls?.IsPlayPauseToggleEnabled == true
        };
    }

    private static PlayerPlaybackState MapPlaybackState(
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? status)
    {
        return status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => PlayerPlaybackState.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => PlayerPlaybackState.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => PlayerPlaybackState.Stopped,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => PlayerPlaybackState.Closed,
            _ => PlayerPlaybackState.Unknown
        };
    }
}
