namespace TaskbarMediaControls;

public sealed record MenuState(
    string MediaTitleText,
    string MediaArtistText,
    string MediaAppText,
    bool MediaTitleEnabled,
    bool MediaArtistEnabled,
    bool MediaAppEnabled
);

public static class TrayFeatureLogic {
    public static IReadOnlyList<string> DefaultContextMenuLabels() {
        return [
            "Exit / Close",
            "Settings",
            "---",
            "Media title: N/A",
            "Media artist: N/A",
            "Media playing with: N/A",
            "---",
            "Previous Track",
            "Play/Pause",
            "Next Track"
        ];
    }

    public static MenuState BuildMenuState(MediaSessionInfo info, bool canOpenFallbackApp) {
        var hasSession = info.HasActiveSession;
        return new MenuState(
            $"Media title: {info.Title}",
            $"Media artist: {info.Artist}",
            $"Media playing with: {info.SourceApp}",
            hasSession,
            hasSession,
            hasSession || canOpenFallbackApp
        );
    }

    public static string BuildHoverText(bool showHoverTrackInfo, MediaSessionInfo info, string actionText) {
        if (!showHoverTrackInfo || !info.HasActiveSession) {
            return actionText;
        }

        return $"{actionText} | {info.Title} - {info.Artist} ({info.SourceApp})";
    }

    public static string TrimTooltip(string value) {
        return value.Length <= 63 ? value : value[..63];
    }

    public static bool IsFallbackPathValid(string path) {
        var trimmed = path.Trim();
        return trimmed.Length == 0 || File.Exists(trimmed);
    }

    public static bool MenuContainsLaunchOnStartup(IEnumerable<string> menuItems) {
        return menuItems.Any(item => item.Contains("Launch on Startup", StringComparison.OrdinalIgnoreCase));
    }

    public static ClickAction GetSingleClickAction(AppSettings settings, int index) {
        return index switch {
            0 => settings.PreviousIcon.SingleClick,
            1 => settings.PlayPauseIcon.SingleClick,
            _ => settings.NextIcon.SingleClick
        };
    }

    public static ClickAction GetDoubleClickAction(AppSettings settings, int index) {
        return index switch {
            0 => settings.PreviousIcon.DoubleClick,
            1 => settings.PlayPauseIcon.DoubleClick,
            _ => settings.NextIcon.DoubleClick
        };
    }

    public static bool[] GetIconVisibilities(AppSettings settings) {
        return [settings.PreviousIcon.Visible, settings.PlayPauseIcon.Visible, settings.NextIcon.Visible];
    }
}
