namespace TaskbarMediaControls.Tests;

public class MediaSessionServiceTests {
    [Fact]
    public async Task InitializeAsync_ShouldPublishInactiveSessionEvent() {
        var service = new MediaSessionService();
        MediaSessionInfo? published = null;
        service.MediaInfoChanged += info => published = info;

        await service.InitializeAsync();

        Assert.NotNull(published);
        Assert.False(published!.HasActiveSession);
    }

    [Fact]
    public async Task RefreshAsync_ShouldPublishCurrentInfo() {
        var service = new MediaSessionService();
        var published = new List<MediaSessionInfo>();
        service.MediaInfoChanged += info => published.Add(info);

        await service.RefreshAsync();

        Assert.NotEmpty(published);
        Assert.False(published[^1].HasActiveSession);
    }

    [Fact]
    public async Task GetCurrentInfoAsync_ShouldReturnInactivePlaceholder() {
        var service = new MediaSessionService();
        var info = await service.GetCurrentInfoAsync();

        Assert.False(info.HasActiveSession);
        Assert.Equal("N/A", info.Title);
    }
}
