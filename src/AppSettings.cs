namespace TaskbarMediaControls;

public enum TrayIconSlot {
    Previous,
    PlayPause,
    Next
}

public enum ClickAction {
    DoNothing,
    PlayPause,
    NextTrack,
    PreviousTrack,
    OpenSettings
}

public sealed class IconBehaviorSettings {
    public bool Visible { get; set; } = true;
    public ClickAction SingleClick { get; set; }
    public ClickAction DoubleClick { get; set; }
}

public sealed class AppSettings {
    public int ConfigVersion { get; set; }

    public IconBehaviorSettings PreviousIcon { get; set; } = new() {
        Visible = true,
        SingleClick = ClickAction.PreviousTrack,
        DoubleClick = ClickAction.DoNothing
    };

    public IconBehaviorSettings PlayPauseIcon { get; set; } = new() {
        Visible = true,
        SingleClick = ClickAction.PlayPause,
        DoubleClick = ClickAction.DoNothing
    };

    public IconBehaviorSettings NextIcon { get; set; } = new() {
        Visible = true,
        SingleClick = ClickAction.NextTrack,
        DoubleClick = ClickAction.DoNothing
    };

    public bool ShowHoverTrackInfo { get; set; } = true;
    public string FallbackExecutablePath { get; set; } = string.Empty;
}
