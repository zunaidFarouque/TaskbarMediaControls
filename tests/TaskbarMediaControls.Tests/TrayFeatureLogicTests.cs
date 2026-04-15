namespace TaskbarMediaControls.Tests;

public class TrayFeatureLogicTests {
    [Fact]
    public void DefaultContextMenu_ShouldContainExpectedSectionsInOrder() {
        var labels = TrayFeatureLogic.DefaultContextMenuLabels();

        Assert.Equal("Exit / Close", labels[0]);
        Assert.Equal("Settings", labels[1]);
        Assert.Equal("---", labels[2]);
        Assert.Equal("Media title: N/A", labels[3]);
        Assert.Equal("Media artist: N/A", labels[4]);
        Assert.Equal("Media playing with: N/A", labels[5]);
        Assert.Equal("---", labels[6]);
        Assert.Equal("Previous Track", labels[7]);
        Assert.Equal("Play/Pause", labels[8]);
        Assert.Equal("Next Track", labels[9]);
    }

    [Fact]
    public void DefaultContextMenu_ShouldNotContainLaunchOnStartup() {
        var contains = TrayFeatureLogic.MenuContainsLaunchOnStartup(TrayFeatureLogic.DefaultContextMenuLabels());
        Assert.False(contains);
    }

    [Fact]
    public void BuildMenuState_NoSessionAndNoFallback_ShouldDisableInfoItems() {
        var info = new MediaSessionInfo { HasActiveSession = false };
        var state = TrayFeatureLogic.BuildMenuState(info, canOpenFallbackApp: false);

        Assert.False(state.MediaTitleEnabled);
        Assert.False(state.MediaArtistEnabled);
        Assert.False(state.MediaAppEnabled);
        Assert.Equal("Media title: N/A", state.MediaTitleText);
    }

    [Fact]
    public void BuildMenuState_NoSessionWithFallback_ShouldEnableAppItemOnly() {
        var info = new MediaSessionInfo { HasActiveSession = false };
        var state = TrayFeatureLogic.BuildMenuState(info, canOpenFallbackApp: true);

        Assert.False(state.MediaTitleEnabled);
        Assert.False(state.MediaArtistEnabled);
        Assert.True(state.MediaAppEnabled);
    }

    [Fact]
    public void BuildHoverText_ShouldUseStaticTextWhenHoverDisabled() {
        var info = new MediaSessionInfo {
            HasActiveSession = true,
            Title = "Song",
            Artist = "Artist",
            SourceApp = "Player"
        };

        var text = TrayFeatureLogic.BuildHoverText(false, info, "Play / Pause");
        Assert.Equal("Play / Pause", text);
    }

    [Fact]
    public void TrimTooltip_ShouldRespectLimit() {
        var longText = new string('x', 70);
        var result = TrayFeatureLogic.TrimTooltip(longText);
        Assert.Equal(63, result.Length);
    }

    [Fact]
    public void GetIconVisibilities_ShouldMapPerIconFlags() {
        var settings = new AppSettings();
        settings.PreviousIcon.Visible = false;
        settings.PlayPauseIcon.Visible = true;
        settings.NextIcon.Visible = false;

        var flags = TrayFeatureLogic.GetIconVisibilities(settings);

        Assert.Equal(new[] { false, true, false }, flags);
    }

    [Fact]
    public void FallbackPathValidation_ShouldAllowEmptyOrExistingOnly() {
        Assert.True(TrayFeatureLogic.IsFallbackPathValid(" "));
        Assert.False(TrayFeatureLogic.IsFallbackPathValid(@"Z:\nonexistent\missing.exe"));
    }

    [Fact]
    public void DefaultClickMapping_ShouldMatchExpectedIntent() {
        var settings = new AppSettings();

        Assert.Equal(ClickAction.PreviousTrack, TrayFeatureLogic.GetSingleClickAction(settings, 0));
        Assert.Equal(ClickAction.PlayPause, TrayFeatureLogic.GetSingleClickAction(settings, 1));
        Assert.Equal(ClickAction.NextTrack, TrayFeatureLogic.GetSingleClickAction(settings, 2));
    }
}
