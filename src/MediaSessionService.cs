namespace TaskbarMediaControls;

public sealed class MediaSessionService : IMediaSessionService {
    public event Action<MediaSessionInfo>? MediaInfoChanged;

    public async Task InitializeAsync() {
        await Task.CompletedTask;
        Publish(new MediaSessionInfo { HasActiveSession = false });
    }

    public async Task<MediaSessionInfo> GetCurrentInfoAsync() {
        await Task.CompletedTask;
        return new MediaSessionInfo { HasActiveSession = false };
    }

    public async Task RefreshAsync() {
        var info = await GetCurrentInfoAsync();
        Publish(info);
    }

    private void Publish(MediaSessionInfo info) {
        MediaInfoChanged?.Invoke(info);
    }

    public void Dispose() {
        // No unmanaged resources yet.
    }
}
