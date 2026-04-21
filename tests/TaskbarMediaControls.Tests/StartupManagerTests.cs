namespace TaskbarMediaControls.Tests;

public class StartupManagerTests {
    [Fact]
    public void SetStartup_Enable_ShouldCreateShortcutAndReportEnabled() {
        using var temp = new TempDirectory();
        var currentExe = Path.Combine(temp.Path, "TaskbarMediaControlsPlus.exe");
        File.WriteAllText(currentExe, string.Empty);
        var manager = new StartupManager(temp.Path, currentExe);

        manager.SetStartup(true);

        var shortcutPath = Path.Combine(temp.Path, "TaskbarMediaControls-plus.lnk");
        Assert.True(File.Exists(shortcutPath));
        Assert.True(manager.StartupEntryExists());
        Assert.True(manager.IsStartupEnabled());
    }

    [Fact]
    public void SetStartup_Disable_ShouldDeleteShortcutAndReportDisabled() {
        using var temp = new TempDirectory();
        var currentExe = Path.Combine(temp.Path, "TaskbarMediaControlsPlus.exe");
        var manager = new StartupManager(temp.Path, currentExe);
        manager.SetStartup(true);

        manager.SetStartup(false);

        var shortcutPath = Path.Combine(temp.Path, "TaskbarMediaControls-plus.lnk");
        Assert.False(File.Exists(shortcutPath));
        Assert.False(manager.StartupEntryExists());
        Assert.False(manager.IsStartupEnabled());
    }

    [Fact]
    public void IsStartupEnabled_WhenExecutableMoved_ShouldReturnFalseUntilReenabled() {
        using var temp = new TempDirectory();
        var originalExe = Path.Combine(temp.Path, "TaskbarMediaControlsPlus-original.exe");
        var movedExe = Path.Combine(temp.Path, "TaskbarMediaControlsPlus-moved.exe");

        var originalManager = new StartupManager(temp.Path, originalExe);
        originalManager.SetStartup(true);

        var movedManager = new StartupManager(temp.Path, movedExe);
        Assert.False(movedManager.IsStartupEnabled());

        movedManager.SetStartup(true);
        Assert.True(movedManager.IsStartupEnabled());
    }

    [Fact]
    public void IsStartupEnabled_WithInvalidShortcutTarget_ShouldReturnFalse() {
        using var temp = new TempDirectory();
        var currentExe = Path.Combine(temp.Path, "TaskbarMediaControlsPlus.exe");
        var manager = new StartupManager(temp.Path, currentExe);
        var shortcutPath = Path.Combine(temp.Path, "TaskbarMediaControls-plus.lnk");

        File.WriteAllText(shortcutPath, "not a real shortcut");

        Assert.True(manager.StartupEntryExists());
        Assert.False(manager.IsStartupEnabled());
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
