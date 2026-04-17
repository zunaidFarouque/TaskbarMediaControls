using System.Diagnostics;
using Windows.Media.Control;

namespace TaskbarMediaControls;

public sealed class GsmtcMediaMetadataProvider : IMediaMetadataProvider {
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    public event Action<MediaSessionInfo>? MediaInfoChanged;

    public async Task InitializeAsync() {
        try {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.CurrentSessionChanged += OnCurrentSessionChanged;
            _manager.SessionsChanged += OnSessionsChanged;
            AttachSession(_manager.GetCurrentSession());
        }
        catch {
            Publish(new MediaSessionInfo { HasActiveSession = false });
        }
    }

    public async Task<MediaSessionInfo> GetCurrentInfoAsync() {
        try {
            if (_manager == null) {
                return new MediaSessionInfo { HasActiveSession = false };
            }

            _currentSession ??= _manager.GetCurrentSession();
            if (_currentSession == null) {
                return new MediaSessionInfo { HasActiveSession = false };
            }

            var props = await _currentSession.TryGetMediaPropertiesAsync();
            var title = string.IsNullOrWhiteSpace(props?.Title) ? "N/A" : props!.Title;
            var artist = string.IsNullOrWhiteSpace(props?.Artist) ? "N/A" : props!.Artist;

            var sourceId = _currentSession.SourceAppUserModelId;
            var sourceApp = BuildSourceAppLabel(sourceId);
            var processPath = ResolveProcessPath(sourceId);
            var playbackState = MapPlaybackState(_currentSession.GetPlaybackInfo()?.PlaybackStatus);

            return new MediaSessionInfo {
                HasActiveSession = true,
                Title = title,
                Artist = artist,
                SourceApp = sourceApp,
                SourceProcessPath = processPath,
                PlaybackState = playbackState
            };
        }
        catch {
            return new MediaSessionInfo { HasActiveSession = false };
        }
    }

    private async void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) {
        AttachSession(sender.GetCurrentSession());
        Publish(await GetCurrentInfoAsync());
    }

    private async void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) {
        AttachSession(sender.GetCurrentSession());
        Publish(await GetCurrentInfoAsync());
    }

    private void AttachSession(GlobalSystemMediaTransportControlsSession? session) {
        if (_currentSession != null) {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }

        _currentSession = session;

        if (_currentSession != null) {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
        }
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) {
        Publish(await GetCurrentInfoAsync());
    }

    private async void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) {
        Publish(await GetCurrentInfoAsync());
    }

    private void Publish(MediaSessionInfo info) {
        MediaInfoChanged?.Invoke(info);
    }

    private static MediaPlaybackState MapPlaybackState(GlobalSystemMediaTransportControlsSessionPlaybackStatus? status) {
        return status switch {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlaybackState.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlaybackState.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaPlaybackState.Stopped,
            _ => MediaPlaybackState.Unknown
        };
    }

    private static string BuildSourceAppLabel(string? sourceAppUserModelId) {
        if (string.IsNullOrWhiteSpace(sourceAppUserModelId)) {
            return "N/A";
        }

        var parts = sourceAppUserModelId.Split('!');
        if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) {
            return parts[0];
        }

        return sourceAppUserModelId;
    }

    private static string? ResolveProcessPath(string? sourceAppUserModelId) {
        if (string.IsNullOrWhiteSpace(sourceAppUserModelId)) {
            return null;
        }

        foreach (var process in Process.GetProcesses()) {
            try {
                if (string.IsNullOrWhiteSpace(process.ProcessName)) {
                    continue;
                }

                if (sourceAppUserModelId.Contains(process.ProcessName, StringComparison.OrdinalIgnoreCase)) {
                    return process.MainModule?.FileName;
                }
            }
            catch {
                // Ignore process access failures.
            }
            finally {
                process.Dispose();
            }
        }

        return null;
    }

    public void Dispose() {
        if (_manager != null) {
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
            _manager.SessionsChanged -= OnSessionsChanged;
        }

        if (_currentSession != null) {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }
    }
}
