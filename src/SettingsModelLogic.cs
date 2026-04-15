namespace TaskbarMediaControls;

public static class SettingsModelLogic {
    public static AppSettings Clone(AppSettings source) {
        return new AppSettings {
            PreviousIcon = new IconBehaviorSettings {
                Visible = source.PreviousIcon.Visible,
                SingleClick = source.PreviousIcon.SingleClick,
                DoubleClick = source.PreviousIcon.DoubleClick
            },
            PlayPauseIcon = new IconBehaviorSettings {
                Visible = source.PlayPauseIcon.Visible,
                SingleClick = source.PlayPauseIcon.SingleClick,
                DoubleClick = source.PlayPauseIcon.DoubleClick
            },
            NextIcon = new IconBehaviorSettings {
                Visible = source.NextIcon.Visible,
                SingleClick = source.NextIcon.SingleClick,
                DoubleClick = source.NextIcon.DoubleClick
            },
            ShowHoverTrackInfo = source.ShowHoverTrackInfo,
            FallbackExecutablePath = source.FallbackExecutablePath
        };
    }
}
