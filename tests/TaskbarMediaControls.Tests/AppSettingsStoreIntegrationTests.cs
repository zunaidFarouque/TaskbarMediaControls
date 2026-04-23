using System.Text.Json;

namespace TaskbarMediaControls.Tests;

public class AppSettingsStoreIntegrationTests {
    [Fact]
    public void ResolveDefaultSettingsPath_WhenPortablePath_ShouldUseExecutableDirectory() {
        using var temp = new TempDirectory();
        var exeDirectory = temp.Path;

        var resolvedPath = InvokeResolveDefaultSettingsPath(exeDirectory);

        Assert.Equal(Path.Combine(exeDirectory, "settings.json"), resolvedPath);
    }

    [Fact]
    public void ResolveDefaultSettingsPath_WhenScoopPath_ShouldUseRoamingAppData() {
        var scoopPath = Path.Combine("C:\\", "Users", "someone", "scoop", "apps", "TaskbarMediaControls-Plus", "current");
        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskbarMediaControls-plus",
            "settings.json"
        );

        var resolvedPath = InvokeResolveDefaultSettingsPath(scoopPath);

        Assert.Equal(expectedPath, resolvedPath);
    }

    [Fact]
    public void MigrateLegacySettingsIfNeeded_WhenDestinationMissing_ShouldCopyLegacyFile() {
        using var temp = new TempDirectory();
        var legacyPath = Path.Combine(temp.Path, "legacy", "settings.json");
        var targetPath = Path.Combine(temp.Path, "target", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacyPath)!);
        File.WriteAllText(legacyPath, """{"ShowHoverTrackInfo":false}""");

        InvokeMigrateLegacySettingsIfNeeded(targetPath, legacyPath);

        Assert.True(File.Exists(targetPath));
        Assert.Equal(File.ReadAllText(legacyPath), File.ReadAllText(targetPath));
    }

    [Fact]
    public void MigrateLegacySettingsIfNeeded_WhenDestinationExists_ShouldNotOverwrite() {
        using var temp = new TempDirectory();
        var legacyPath = Path.Combine(temp.Path, "legacy", "settings.json");
        var targetPath = Path.Combine(temp.Path, "target", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacyPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(legacyPath, """{"ShowHoverTrackInfo":false}""");
        File.WriteAllText(targetPath, """{"ShowHoverTrackInfo":true}""");

        InvokeMigrateLegacySettingsIfNeeded(targetPath, legacyPath);

        Assert.Equal("""{"ShowHoverTrackInfo":true}""", File.ReadAllText(targetPath));
    }

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
        Assert.Equal(ClickAction.PreviousTrack, settings.PreviousIcon.SingleClick);
        Assert.Equal(ClickAction.PlayPause, settings.PlayPauseIcon.SingleClick);
        Assert.Equal(ClickAction.NextTrack, settings.NextIcon.SingleClick);
        Assert.Equal(ClickAction.DoNothing, settings.PreviousIcon.DoubleClick);
        Assert.Equal(ClickAction.OpenFallbackExecutable, settings.PlayPauseIcon.DoubleClick);
        Assert.Equal(ClickAction.DoNothing, settings.NextIcon.DoubleClick);
        Assert.Equal(
            FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback,
            settings.FallbackActionWhenMediaActive
        );
        Assert.Equal(FallbackPlayerType.Other, settings.FallbackPlayerType);
    }

    [Fact]
    public void SaveAndLoad_ShouldRoundTripValues() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var store = new AppSettingsStore(path);
        var input = new AppSettings {
            FallbackExecutablePath = @"C:\A\B.exe",
            ShowHoverTrackInfo = false,
            ConfigVersion = AppSettingsStore.CurrentConfigVersion
        };
        input.NextIcon.Visible = false;
        input.PreviousIcon.SingleClick = ClickAction.OpenSettings;
        input.PlayPauseIcon.SingleClick = ClickAction.NextTrack;
        input.NextIcon.SingleClick = ClickAction.PreviousTrack;
        input.PlayPauseIcon.DoubleClick = ClickAction.DoNothing;
        input.FallbackActionWhenMediaActive = FallbackActionWhenMediaActive.OpenFallbackExecutable;
        input.FallbackPlayerType = FallbackPlayerType.Foobar;

        store.Save(input);
        var output = store.Load();

        Assert.Equal(@"C:\A\B.exe", output.FallbackExecutablePath);
        Assert.False(output.ShowHoverTrackInfo);
        Assert.False(output.NextIcon.Visible);
        Assert.Equal(ClickAction.OpenSettings, output.PreviousIcon.SingleClick);
        Assert.Equal(ClickAction.NextTrack, output.PlayPauseIcon.SingleClick);
        Assert.Equal(ClickAction.PreviousTrack, output.NextIcon.SingleClick);
        Assert.Equal(ClickAction.DoNothing, output.PlayPauseIcon.DoubleClick);
        Assert.Equal(FallbackActionWhenMediaActive.OpenFallbackExecutable, output.FallbackActionWhenMediaActive);
        Assert.Equal(FallbackPlayerType.Foobar, output.FallbackPlayerType);
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
    public void Load_WhenLegacyDoubleClickConfigured_ShouldMigratePlayPauseToFallbackDefault() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var legacy = new AppSettings {
            ConfigVersion = 2
        };
        legacy.PreviousIcon.DoubleClick = ClickAction.PreviousTrack;
        legacy.PlayPauseIcon.DoubleClick = ClickAction.PlayPause;
        legacy.NextIcon.DoubleClick = ClickAction.NextTrack;
        File.WriteAllText(path, JsonSerializer.Serialize(legacy));

        var store = new AppSettingsStore(path);
        var loaded = store.Load();

        Assert.Equal(ClickAction.DoNothing, loaded.PreviousIcon.DoubleClick);
        Assert.Equal(ClickAction.OpenFallbackExecutable, loaded.PlayPauseIcon.DoubleClick);
        Assert.Equal(ClickAction.DoNothing, loaded.NextIcon.DoubleClick);
        Assert.Equal(AppSettingsStore.CurrentConfigVersion, loaded.ConfigVersion);
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

    [Fact]
    public void Load_WhenLegacyVersionWithExplicitValue_ShouldPreserveFallbackMediaActiveBehavior() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var legacy = new AppSettings {
            ConfigVersion = 4,
            FallbackActionWhenMediaActive = FallbackActionWhenMediaActive.OpenFallbackExecutable
        };
        File.WriteAllText(path, JsonSerializer.Serialize(legacy));

        var store = new AppSettingsStore(path);
        var loaded = store.Load();

        Assert.Equal(FallbackActionWhenMediaActive.OpenFallbackExecutable, loaded.FallbackActionWhenMediaActive);
        Assert.Equal(AppSettingsStore.CurrentConfigVersion, loaded.ConfigVersion);
    }

    [Fact]
    public void Load_WhenFallbackMediaActiveBehaviorIsInvalid_ShouldNormalizeToDefault() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var invalid = new AppSettings {
            ConfigVersion = AppSettingsStore.CurrentConfigVersion,
            FallbackActionWhenMediaActive = (FallbackActionWhenMediaActive)999
        };
        File.WriteAllText(path, JsonSerializer.Serialize(invalid));

        var store = new AppSettingsStore(path);
        var loaded = store.Load();

        Assert.Equal(FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback, loaded.FallbackActionWhenMediaActive);
        Assert.Equal(AppSettingsStore.CurrentConfigVersion, loaded.ConfigVersion);
    }

    [Fact]
    public void Load_WhenFallbackPlayerTypeIsInvalid_ShouldNormalizeToDefault() {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var invalid = new AppSettings {
            ConfigVersion = AppSettingsStore.CurrentConfigVersion,
            FallbackPlayerType = (FallbackPlayerType)999
        };
        File.WriteAllText(path, JsonSerializer.Serialize(invalid));

        var store = new AppSettingsStore(path);
        var loaded = store.Load();

        Assert.Equal(FallbackPlayerType.Other, loaded.FallbackPlayerType);
        Assert.Equal(AppSettingsStore.CurrentConfigVersion, loaded.ConfigVersion);
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

    private static string InvokeResolveDefaultSettingsPath(string executableDirectory) {
        var method = typeof(AppSettingsStore).GetMethod(
            "ResolveDefaultSettingsPath",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public
        );
        Assert.NotNull(method);
        var result = method!.Invoke(null, [executableDirectory]);
        var value = Assert.IsType<string>(result);
        return value;
    }

    private static void InvokeMigrateLegacySettingsIfNeeded(string targetSettingsPath, string legacySettingsPath) {
        var method = typeof(AppSettingsStore).GetMethod(
            "MigrateLegacySettingsIfNeeded",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public
        );
        Assert.NotNull(method);
        method!.Invoke(null, [targetSettingsPath, legacySettingsPath]);
    }
}
