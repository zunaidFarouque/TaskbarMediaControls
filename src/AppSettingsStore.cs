using System.Text.Json;

namespace TaskbarMediaControls;

public sealed class AppSettingsStore : IAppSettingsStore {
    public const int CurrentConfigVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public AppSettingsStore() {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "TaskbarMediaControls");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public AppSettingsStore(string settingsPath) {
        var folder = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(folder)) {
            Directory.CreateDirectory(folder);
        }

        _settingsPath = settingsPath;
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
            DoubleClick = ClickAction.DoNothing
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

        if (settings.ConfigVersion < 1) {
            settings.ConfigVersion = 1;
            changed = true;
        }

        // Legacy migration: old configs may have lost single-click defaults.
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

        if (settings.ConfigVersion != CurrentConfigVersion) {
            settings.ConfigVersion = CurrentConfigVersion;
            changed = true;
        }

        return changed;
    }
}
