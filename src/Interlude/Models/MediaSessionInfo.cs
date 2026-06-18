namespace Interlude.Models;

public sealed class MediaSessionInfo
{
    public string DisplayName { get; set; } = string.Empty;

    public string SourceAppUserModelId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public PlayerPlaybackState PlaybackState { get; set; } = PlayerPlaybackState.Unknown;

    public bool CanPause { get; set; }

    public bool CanPlay { get; set; }

    public bool CanToggle { get; set; }

    public string IconPath { get; set; } = string.Empty;

    public string Summary => string.IsNullOrWhiteSpace(Title)
        ? DisplayName
        : $"{DisplayName} - {Title}";
}
