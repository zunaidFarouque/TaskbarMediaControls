using System.Text.Json;

namespace TaskbarMediaControls.Tests;

public class AppSettingsStoreIntegrationTests {
    [Fact]
    public void Load_WhenFileMissing_ShouldCreateDefaults() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var store = new AppSettingsStore(path);

        var settings = store.Load();

        Assert.True(File.Exists(path));
        Assert.True(settings.PreviousIcon.Visible);
        Assert.True(settings.ShowHoverTrackInfo);
        Assert.Equal(AppSettingsStore.CurrentConfigVersion, settings.ConfigVersion);
    }

    [Fact]
    public void SaveAndLoad_ShouldRoundTripValues() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var store = new AppSettingsStore(path);
        var input = new AppSettings {
            FallbackExecutablePath = @"C:\A\B.exe",
            ShowHoverTrackInfo = false
        };
        input.NextIcon.Visible = false;
        input.PlayPauseIcon.DoubleClick = ClickAction.OpenSettings;

        store.Save(input);
        var output = store.Load();

        Assert.Equal(@"C:\A\B.exe", output.FallbackExecutablePath);
        Assert.False(output.ShowHoverTrackInfo);
        Assert.False(output.NextIcon.Visible);
        Assert.Equal(ClickAction.OpenSettings, output.PlayPauseIcon.DoubleClick);
    }

    [Fact]
    public void Load_WhenCorruptedJson_ShouldFallbackToDefaults() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(path, "{ this is invalid json");
        var store = new AppSettingsStore(path);

        var output = store.Load();

        Assert.True(output.PreviousIcon.Visible);
        Assert.Equal(string.Empty, output.FallbackExecutablePath);
    }

    [Fact]
    public void Load_WhenPartialJson_ShouldPopulateDefaults() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var partial = new { ShowHoverTrackInfo = false };
        File.WriteAllText(path, JsonSerializer.Serialize(partial));
        var store = new AppSettingsStore(path);

        var output = store.Load();

        Assert.False(output.ShowHoverTrackInfo);
        Assert.NotNull(output.PreviousIcon);
        Assert.NotNull(output.PlayPauseIcon);
        Assert.NotNull(output.NextIcon);
        Assert.Equal(AppSettingsStore.CurrentConfigVersion, output.ConfigVersion);
    }

    [Fact]
    public void Load_WhenLegacyDoNothingSingles_ShouldMigrateToFeatureDefaults() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var legacy = new AppSettings {
            ConfigVersion = 1
        };
        legacy.PreviousIcon.SingleClick = ClickAction.DoNothing;
        legacy.PlayPauseIcon.SingleClick = ClickAction.DoNothing;
        legacy.NextIcon.SingleClick = ClickAction.DoNothing;

        File.WriteAllText(path, JsonSerializer.Serialize(legacy));
        var store = new AppSettingsStore(path);

        var output = store.Load();
        var persisted = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));

        Assert.Equal(ClickAction.PreviousTrack, output.PreviousIcon.SingleClick);
        Assert.Equal(ClickAction.PlayPause, output.PlayPauseIcon.SingleClick);
        Assert.Equal(ClickAction.NextTrack, output.NextIcon.SingleClick);
        Assert.Equal(AppSettingsStore.CurrentConfigVersion, output.ConfigVersion);
        Assert.NotNull(persisted);
        Assert.Equal(AppSettingsStore.CurrentConfigVersion, persisted!.ConfigVersion);
    }

    [Fact]
    public void Load_WhenVersionMissing_ShouldUpgradeAndPersist() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var versionless = """
                          {
                            "ShowHoverTrackInfo": true
                          }
                          """;
        File.WriteAllText(path, versionless);
        var store = new AppSettingsStore(path);

        var loaded = store.Load();
        var persisted = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));

        Assert.Equal(AppSettingsStore.CurrentConfigVersion, loaded.ConfigVersion);
        Assert.NotNull(persisted);
        Assert.Equal(AppSettingsStore.CurrentConfigVersion, persisted!.ConfigVersion);
    }

    private sealed class TempDirectory : IDisposable {
        public TempDirectory() {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() {
            try {
                Directory.Delete(Path, true);
            }
            catch {
                // Ignore cleanup failures.
            }
        }
    }
}
