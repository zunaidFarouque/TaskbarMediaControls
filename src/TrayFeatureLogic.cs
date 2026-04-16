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
            "Settings",
            "Close Media Controls",
            "---",
            "Title: N/A",
            "Artist: N/A",
            "Playing with: N/A",
            "---",
            "Previous Track",
            "Play/Pause",
            "Next Track"
        ];
    }

    public static MenuState BuildMenuState(MediaSessionInfo info, bool canOpenFallbackApp) {
        var hasSession = info.HasActiveSession;
        return new MenuState(
            $"Title: {info.Title}",
            $"Artist: {info.Artist}",
            $"Playing with: {info.SourceApp}",
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

    public static bool ShouldOpenCurrentMediaAppBeforeFallback(AppSettings settings, bool hasActiveSession) {
        if (!hasActiveSession) {
            return false;
        }

        return settings.FallbackActionWhenMediaActive == FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback;
    }

    public static FallbackOpenAction ResolveFallbackOpenAction(
        AppSettings settings,
        bool hasActiveSession,
        bool hasValidSourcePath,
        bool hasValidFallbackPath
    ) {
        if (hasActiveSession &&
            settings.FallbackActionWhenMediaActive == FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback) {
            if (hasValidSourcePath) {
                return FallbackOpenAction.OpenMediaSource;
            }

            return hasValidFallbackPath ? FallbackOpenAction.OpenFallback : FallbackOpenAction.None;
        }

        return hasValidFallbackPath ? FallbackOpenAction.OpenFallback : FallbackOpenAction.None;
    }

    public static FallbackPlayerType ResolveLaunchPlayerType(
        AppSettings settings,
        string? launchPath,
        bool matchesConfiguredFallbackPath
    ) {
        if (matchesConfiguredFallbackPath) {
            return settings.FallbackPlayerType;
        }

        if (IsFoobarExecutablePath(launchPath)) {
            return FallbackPlayerType.Foobar;
        }

        return FallbackPlayerType.Other;
    }

    public static bool ShouldAvoidDuplicateWhenRunningWithoutWindow(
        AppSettings settings,
        string? launchPath,
        bool matchesConfiguredFallbackPath
    ) {
        if (matchesConfiguredFallbackPath) {
            return true;
        }

        return IsFoobarExecutablePath(launchPath);
    }

    public static bool IsFoobarExecutablePath(string? path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return false;
        }

        try {
            return string.Equals(Path.GetFileName(path), "foobar2000.exe", StringComparison.OrdinalIgnoreCase);
        }
        catch {
            return false;
        }
    }

    public static string BuildProcessLaunchErrorMessage(ProcessLaunchResult result, string operationDescription) {
        return result.Outcome switch {
            ProcessLaunchOutcome.RunningWithoutWindow =>
                "The fallback app is running in the background but has no restorable window.",
            ProcessLaunchOutcome.FoobarRestoreFailed =>
                "Could not restore Foobar2000 from tray/minimized state after trying /show and the /command fallback.",
            ProcessLaunchOutcome.InvalidPath =>
                $"{operationDescription} failed because the executable path is invalid.",
            ProcessLaunchOutcome.Failed when !string.IsNullOrWhiteSpace(result.ErrorMessage) =>
                $"{operationDescription} failed: {result.ErrorMessage}",
            ProcessLaunchOutcome.Failed =>
                $"{operationDescription} failed due to an unexpected error.",
            _ => $"{operationDescription} could not be completed."
        };
    }
}
