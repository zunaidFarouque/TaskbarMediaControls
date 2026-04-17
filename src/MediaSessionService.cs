namespace TaskbarMediaControls;

public sealed class MediaSessionService : IMediaSessionService {
    private readonly IMediaMetadataProvider _provider;

    public event Action<MediaSessionInfo>? MediaInfoChanged;

    public MediaSessionService()
        : this(new GsmtcMediaMetadataProvider()) {
    }

    public MediaSessionService(IMediaMetadataProvider provider) {
        _provider = provider;
        _provider.MediaInfoChanged += OnProviderMediaInfoChanged;
    }

    public async Task InitializeAsync() {
        await _provider.InitializeAsync();
        Publish(await _provider.GetCurrentInfoAsync());
    }

    public async Task<MediaSessionInfo> GetCurrentInfoAsync() {
        return await _provider.GetCurrentInfoAsync();
    }

    public async Task RefreshAsync() {
        var info = await GetCurrentInfoAsync();
        Publish(info);
    }

    private void Publish(MediaSessionInfo info) {
        MediaInfoChanged?.Invoke(info);
    }

    private void OnProviderMediaInfoChanged(MediaSessionInfo info) {
        Publish(info);
    }

    public void Dispose() {
        _provider.MediaInfoChanged -= OnProviderMediaInfoChanged;
        _provider.Dispose();
    }
}
