namespace TaskbarMediaControls;

public enum MediaPlaybackState {
    Unknown,
    Playing,
    Paused,
    Stopped
}

public sealed class MediaSessionInfo {
    public string Title { get; init; } = "N/A";
    public string Artist { get; init; } = "N/A";
    public string SourceApp { get; init; } = "N/A";
    public string? SourceProcessPath { get; init; }
    public bool HasActiveSession { get; init; }
    public MediaPlaybackState PlaybackState { get; init; } = MediaPlaybackState.Unknown;
}
