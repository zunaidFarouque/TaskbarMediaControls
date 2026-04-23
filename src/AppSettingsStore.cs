using System.Text.Json;

namespace TaskbarMediaControls;

public sealed class AppSettingsStore : IAppSettingsStore {
    public const int CurrentConfigVersion = 6;

    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public AppSettingsStore() {
        var exeDirectory = Path.GetDirectoryName(Application.ExecutablePath) ?? AppContext.BaseDirectory;
        _settingsPath = ResolveDefaultSettingsPath(exeDirectory);
        var targetDirectory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory)) {
            Directory.CreateDirectory(targetDirectory);
        }

        var legacySettingsPath = Path.Combine(exeDirectory, "settings.json");
        MigrateLegacySettingsIfNeeded(_settingsPath, legacySettingsPath);
    }

    public AppSettingsStore(string settingsPath) {
        var folder = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(folder)) {
            Directory.CreateDirectory(folder);
        }

        _settingsPath = settingsPath;
    }

    internal static string ResolveDefaultSettingsPath(string executableDirectory) {
        if (IsScoopInstallPath(executableDirectory)) {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appSettingsDirectory = Path.Combine(appDataPath, "TaskbarMediaControls-plus");
            return Path.Combine(appSettingsDirectory, "settings.json");
        }

        return Path.Combine(executableDirectory, "settings.json");
    }

    internal static bool IsScoopInstallPath(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return false;
        }

        var normalizedPath = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var segments = normalizedPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) {
            return false;
        }

        var hasScoopSegment = segments.Any(segment => string.Equals(segment, "scoop", StringComparison.OrdinalIgnoreCase));
        var hasAppsSegment = segments.Any(segment => string.Equals(segment, "apps", StringComparison.OrdinalIgnoreCase));
        return hasScoopSegment && hasAppsSegment;
    }

    internal static void MigrateLegacySettingsIfNeeded(string targetSettingsPath, string legacySettingsPath) {
        if (string.IsNullOrWhiteSpace(targetSettingsPath) || string.IsNullOrWhiteSpace(legacySettingsPath)) {
            return;
        }

        if (string.Equals(
                Path.GetFullPath(targetSettingsPath),
                Path.GetFullPath(legacySettingsPath),
                StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        if (File.Exists(targetSettingsPath) || !File.Exists(legacySettingsPath)) {
            return;
        }

        try {
            var targetDirectory = Path.GetDirectoryName(targetSettingsPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory)) {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(legacySettingsPath, targetSettingsPath);
        }
        catch {
            // Ignore migration failures and continue with defaults/load behavior.
        }
    }

    public AppSettings Load() {
        try {
            if (!File.Exists(_settingsPath)) {
                var defaults = new AppSettings();
                Save(defaults);
                return defaults;
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings == null) {
                var defaults = new AppSettings();
                Save(defaults);
                return defaults;
            }

            var changed = EnsureDefaults(settings);
            if (changed) {
                Save(settings);
            }

            return settings;
        }
        catch {
            var defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(AppSettings settings) {
        EnsureDefaults(settings);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static bool EnsureDefaults(AppSettings settings) {
        var changed = false;

        if (settings.PreviousIcon == null) {
            settings.PreviousIcon = new IconBehaviorSettings {
            Visible = true,
            SingleClick = ClickAction.PreviousTrack,
            DoubleClick = ClickAction.DoNothing
        };
            changed = true;
        }

        if (settings.PlayPauseIcon == null) {
            settings.PlayPauseIcon = new IconBehaviorSettings {
            Visible = true,
            SingleClick = ClickAction.PlayPause,
            DoubleClick = ClickAction.OpenFallbackExecutable
        };
            changed = true;
        }

        if (settings.NextIcon == null) {
            settings.NextIcon = new IconBehaviorSettings {
            Visible = true,
            SingleClick = ClickAction.NextTrack,
            DoubleClick = ClickAction.DoNothing
        };
            changed = true;
        }

        if (settings.FallbackExecutablePath == null) {
            settings.FallbackExecutablePath = string.Empty;
            changed = true;
        }

        if (!Enum.IsDefined(settings.FallbackActionWhenMediaActive)) {
            settings.FallbackActionWhenMediaActive = FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback;
            changed = true;
        }

        if (!Enum.IsDefined(settings.FallbackPlayerType)) {
            settings.FallbackPlayerType = FallbackPlayerType.Other;
            changed = true;
        }

        if (settings.ConfigVersion < 1) {
            settings.ConfigVersion = 1;
            changed = true;
        }

        // Legacy migration: old configs may have lost obvious single-click defaults.
        if (settings.ConfigVersion < 2) {
            if (settings.PreviousIcon.SingleClick == ClickAction.DoNothing) {
                settings.PreviousIcon.SingleClick = ClickAction.PreviousTrack;
                changed = true;
            }

            if (settings.PlayPauseIcon.SingleClick == ClickAction.DoNothing) {
                settings.PlayPauseIcon.SingleClick = ClickAction.PlayPause;
                changed = true;
            }

            if (settings.NextIcon.SingleClick == ClickAction.DoNothing) {
                settings.NextIcon.SingleClick = ClickAction.NextTrack;
                changed = true;
            }
        }

        // Legacy migration: ensure first-time double-click defaults remain "DoNothing".
        if (settings.ConfigVersion < 3) {
            if (settings.PreviousIcon.DoubleClick != ClickAction.DoNothing) {
                settings.PreviousIcon.DoubleClick = ClickAction.DoNothing;
                changed = true;
            }

            if (settings.PlayPauseIcon.DoubleClick != ClickAction.DoNothing) {
                settings.PlayPauseIcon.DoubleClick = ClickAction.DoNothing;
                changed = true;
            }

            if (settings.NextIcon.DoubleClick != ClickAction.DoNothing) {
                settings.NextIcon.DoubleClick = ClickAction.DoNothing;
                changed = true;
            }
        }

        // Legacy migration: promote play/pause double-click fallback launch default.
        if (settings.ConfigVersion < 4) {
            if (settings.PlayPauseIcon.DoubleClick == ClickAction.DoNothing) {
                settings.PlayPauseIcon.DoubleClick = ClickAction.OpenFallbackExecutable;
                changed = true;
            }
        }

        // Legacy migration: initialize the new setting only when missing/invalid.
        if (settings.ConfigVersion < 5) {
            if (!Enum.IsDefined(settings.FallbackActionWhenMediaActive)) {
                settings.FallbackActionWhenMediaActive = FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback;
                changed = true;
            }
        }

        // Legacy migration: initialize fallback player type only when missing/invalid.
        if (settings.ConfigVersion < 6) {
            if (!Enum.IsDefined(settings.FallbackPlayerType)) {
                settings.FallbackPlayerType = FallbackPlayerType.Other;
                changed = true;
            }
        }

        if (settings.ConfigVersion != CurrentConfigVersion) {
            settings.ConfigVersion = CurrentConfigVersion;
            changed = true;
        }

        return changed;
    }
}
