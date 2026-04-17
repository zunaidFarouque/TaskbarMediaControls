using System.Diagnostics;
using Windows.Media.Control;

namespace TaskbarMediaControls;

public sealed class GsmtcMediaMetadataProvider : IMediaMetadataProvider {
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly SemaphoreSlim _refreshSignal = new(1, 1);
    private int _refreshQueued;
    private bool _isDisposed;
    private string? _cachedProcessPathSourceId;
    private string? _cachedProcessPath;

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

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) {
        AttachSession(sender.GetCurrentSession());
        QueueRefreshPublish();
    }

    private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) {
        AttachSession(sender.GetCurrentSession());
        QueueRefreshPublish();
    }

    private void AttachSession(GlobalSystemMediaTransportControlsSession? session) {
        if (_currentSession != null) {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }

        _currentSession = session;
        _cachedProcessPathSourceId = null;
        _cachedProcessPath = null;

        if (_currentSession != null) {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
        }
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) {
        QueueRefreshPublish();
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) {
        QueueRefreshPublish();
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

    private void QueueRefreshPublish() {
        if (_isDisposed) {
            return;
        }

        Interlocked.Exchange(ref _refreshQueued, 1);
        _ = DrainRefreshQueueAsync();
    }

    private async Task DrainRefreshQueueAsync() {
        if (_isDisposed) {
            return;
        }

        if (!await _refreshSignal.WaitAsync(0)) {
            return;
        }

        try {
            while (!_isDisposed && Interlocked.Exchange(ref _refreshQueued, 0) == 1) {
                Publish(await GetCurrentInfoAsync());
            }
        }
        finally {
            _refreshSignal.Release();
        }
    }

    private string? ResolveProcessPath(string? sourceAppUserModelId) {
        if (string.IsNullOrWhiteSpace(sourceAppUserModelId)) {
            return null;
        }

        if (string.Equals(_cachedProcessPathSourceId, sourceAppUserModelId, StringComparison.OrdinalIgnoreCase)) {
            return _cachedProcessPath;
        }

        foreach (var process in Process.GetProcesses()) {
            try {
                if (string.IsNullOrWhiteSpace(process.ProcessName)) {
                    continue;
                }

                if (sourceAppUserModelId.Contains(process.ProcessName, StringComparison.OrdinalIgnoreCase)) {
                    _cachedProcessPathSourceId = sourceAppUserModelId;
                    _cachedProcessPath = process.MainModule?.FileName;
                    return _cachedProcessPath;
                }
            }
            catch {
                // Ignore process access failures.
            }
            finally {
                process.Dispose();
            }
        }

        _cachedProcessPathSourceId = sourceAppUserModelId;
        _cachedProcessPath = null;
        return null;
    }

    public void Dispose() {
        _isDisposed = true;
        if (_manager != null) {
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
            _manager.SessionsChanged -= OnSessionsChanged;
        }

        if (_currentSession != null) {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }

        _refreshSignal.Dispose();
    }
}
