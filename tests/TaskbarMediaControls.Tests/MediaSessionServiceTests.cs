namespace TaskbarMediaControls.Tests;

public class MediaSessionServiceTests {
    [Fact]
    public async Task InitializeAsync_ShouldPublishProviderCurrentInfo() {
        var provider = new FakeProvider {
            CurrentInfo = new MediaSessionInfo {
                HasActiveSession = true,
                Title = "Track",
                Artist = "Artist",
                SourceApp = "Player",
                PlaybackState = MediaPlaybackState.Playing
            }
        };
        var service = new MediaSessionService(provider);
        MediaSessionInfo? published = null;
        service.MediaInfoChanged += info => published = info;

        await service.InitializeAsync();

        Assert.NotNull(published);
        Assert.True(published!.HasActiveSession);
        Assert.Equal("Track", published.Title);
        Assert.Equal(MediaPlaybackState.Playing, published.PlaybackState);
    }

    [Fact]
    public async Task RefreshAsync_ShouldPublishCurrentInfo() {
        var provider = new FakeProvider {
            CurrentInfo = new MediaSessionInfo {
                HasActiveSession = true,
                Title = "Refreshed",
                Artist = "A",
                SourceApp = "B",
                PlaybackState = MediaPlaybackState.Paused
            }
        };
        var service = new MediaSessionService(provider);
        var published = new List<MediaSessionInfo>();
        service.MediaInfoChanged += info => published.Add(info);

        await service.RefreshAsync();

        Assert.NotEmpty(published);
        Assert.True(published[^1].HasActiveSession);
        Assert.Equal("Refreshed", published[^1].Title);
        Assert.Equal(MediaPlaybackState.Paused, published[^1].PlaybackState);
    }

    [Fact]
    public async Task GetCurrentInfoAsync_ShouldReturnProviderInfo() {
        var provider = new FakeProvider {
            CurrentInfo = new MediaSessionInfo {
                HasActiveSession = true,
                Title = "Song",
                Artist = "Singer",
                SourceApp = "App"
            }
        };
        var service = new MediaSessionService(provider);
        var info = await service.GetCurrentInfoAsync();

        Assert.True(info.HasActiveSession);
        Assert.Equal("Song", info.Title);
        Assert.Equal("Singer", info.Artist);
    }

    [Fact]
    public async Task ProviderEvents_ShouldBeForwardedToServiceSubscribers() {
        var provider = new FakeProvider();
        var service = new MediaSessionService(provider);
        MediaSessionInfo? published = null;
        service.MediaInfoChanged += info => published = info;

        await provider.RaiseAsync(new MediaSessionInfo {
            HasActiveSession = true,
            Title = "Live",
            Artist = "Now",
            SourceApp = "Player"
        });

        Assert.NotNull(published);
        Assert.Equal("Live", published!.Title);
    }

    private sealed class FakeProvider : IMediaMetadataProvider {
        public event Action<MediaSessionInfo>? MediaInfoChanged;
        public MediaSessionInfo CurrentInfo { get; set; } = new() { HasActiveSession = false };

        public Task InitializeAsync() => Task.CompletedTask;
        public Task<MediaSessionInfo> GetCurrentInfoAsync() => Task.FromResult(CurrentInfo);

        public Task RaiseAsync(MediaSessionInfo info) {
            MediaInfoChanged?.Invoke(info);
            return Task.CompletedTask;
        }

        public void Dispose() {
            // No-op
        }
    }
}
