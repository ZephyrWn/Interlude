using Interlude.Models;

namespace Interlude.Controllers;

public interface IPlayerController
{
    string DisplayName { get; }

    string SourceAppUserModelId { get; }

    bool CanPause { get; }

    bool CanPlay { get; }

    PlayerPlaybackState GetPlaybackState();

    Task<bool> PauseAsync(CancellationToken cancellationToken = default);

    Task<bool> PlayAsync(CancellationToken cancellationToken = default);
}
