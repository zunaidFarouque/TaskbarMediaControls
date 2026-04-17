namespace TaskbarMediaControls.Tests;

public class TrayFeatureLogicTests {
    [Fact]
    public void DefaultContextMenu_ShouldContainExpectedSectionsInOrder() {
        var labels = TrayFeatureLogic.DefaultContextMenuLabels();

        Assert.Equal("Settings", labels[0]);
        Assert.Equal("Close Media Controls", labels[1]);
        Assert.Equal("---", labels[2]);
        Assert.Equal("Title: N/A", labels[3]);
        Assert.Equal("Artist: N/A", labels[4]);
        Assert.Equal("Playing with: N/A", labels[5]);
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
        Assert.Equal("Title: N/A", state.MediaTitleText);
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
    public void BuildMenuState_WithMetadata_ShouldRenderValuesAndEnableItems() {
        var info = new MediaSessionInfo {
            HasActiveSession = true,
            Title = "Song A",
            Artist = "Artist B",
            SourceApp = "Player C"
        };
        var state = TrayFeatureLogic.BuildMenuState(info, canOpenFallbackApp: false);

        Assert.Equal("Title: Song A", state.MediaTitleText);
        Assert.Equal("Artist: Artist B", state.MediaArtistText);
        Assert.Equal("Playing with: Player C", state.MediaAppText);
        Assert.True(state.MediaTitleEnabled);
        Assert.True(state.MediaArtistEnabled);
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

    [Theory]
    [InlineData(MediaPlaybackState.Playing, IconType.Pause)]
    [InlineData(MediaPlaybackState.Paused, IconType.Play)]
    [InlineData(MediaPlaybackState.Stopped, IconType.Play)]
    [InlineData(MediaPlaybackState.Unknown, IconType.Play)]
    public void ResolvePlayPauseIconType_ShouldMapPlaybackState(MediaPlaybackState playbackState, IconType expectedIconType) {
        var info = new MediaSessionInfo {
            HasActiveSession = true,
            PlaybackState = playbackState
        };

        var iconType = TrayFeatureLogic.ResolvePlayPauseIconType(info);

        Assert.Equal(expectedIconType, iconType);
    }

    [Fact]
    public void ResolvePlayPauseIconType_ShouldUsePlayIconWhenNoSession() {
        var info = new MediaSessionInfo {
            HasActiveSession = false,
            PlaybackState = MediaPlaybackState.Playing
        };

        var iconType = TrayFeatureLogic.ResolvePlayPauseIconType(info);

        Assert.Equal(IconType.Play, iconType);
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

    [Fact]
    public void ShouldOpenCurrentMediaAppBeforeFallback_ShouldBeTrueForDefaultWithActiveSession() {
        var settings = new AppSettings();

        Assert.True(TrayFeatureLogic.ShouldOpenCurrentMediaAppBeforeFallback(settings, hasActiveSession: true));
    }

    [Fact]
    public void ShouldOpenCurrentMediaAppBeforeFallback_ShouldBeFalseWhenSettingPrefersFallback() {
        var settings = new AppSettings {
            FallbackActionWhenMediaActive = FallbackActionWhenMediaActive.OpenFallbackExecutable
        };

        Assert.False(TrayFeatureLogic.ShouldOpenCurrentMediaAppBeforeFallback(settings, hasActiveSession: true));
    }

    [Fact]
    public void ShouldOpenCurrentMediaAppBeforeFallback_ShouldBeFalseWhenNoActiveSession() {
        var settings = new AppSettings();

        Assert.False(TrayFeatureLogic.ShouldOpenCurrentMediaAppBeforeFallback(settings, hasActiveSession: false));
    }

    [Theory]
    [InlineData(true, FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback, true, true, FallbackOpenAction.OpenMediaSource)]
    [InlineData(true, FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback, true, false, FallbackOpenAction.OpenMediaSource)]
    [InlineData(true, FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback, false, true, FallbackOpenAction.OpenFallback)]
    [InlineData(true, FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback, false, false, FallbackOpenAction.None)]
    [InlineData(true, FallbackActionWhenMediaActive.OpenFallbackExecutable, true, true, FallbackOpenAction.OpenFallback)]
    [InlineData(true, FallbackActionWhenMediaActive.OpenFallbackExecutable, true, false, FallbackOpenAction.None)]
    [InlineData(false, FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback, true, true, FallbackOpenAction.OpenFallback)]
    [InlineData(false, FallbackActionWhenMediaActive.OpenFallbackExecutable, false, true, FallbackOpenAction.OpenFallback)]
    [InlineData(false, FallbackActionWhenMediaActive.OpenFallbackExecutable, false, false, FallbackOpenAction.None)]
    public void ResolveFallbackOpenAction_ShouldFollowRuntimeMatrix(
        bool hasActiveSession,
        FallbackActionWhenMediaActive configuredBehavior,
        bool hasValidSourcePath,
        bool hasValidFallbackPath,
        FallbackOpenAction expected
    ) {
        var settings = new AppSettings {
            FallbackActionWhenMediaActive = configuredBehavior
        };

        var action = TrayFeatureLogic.ResolveFallbackOpenAction(
            settings,
            hasActiveSession,
            hasValidSourcePath,
            hasValidFallbackPath
        );

        Assert.Equal(expected, action);
    }

    [Fact]
    public void ResolveLaunchPlayerType_ShouldTreatFoobarExeAsFoobar() {
        var settings = new AppSettings {
            FallbackPlayerType = FallbackPlayerType.Foobar
        };

        var playerType = TrayFeatureLogic.ResolveLaunchPlayerType(
            settings,
            @"D:\OtherPath\foobar2000.exe",
            matchesConfiguredFallbackPath: false
        );

        Assert.Equal(FallbackPlayerType.Foobar, playerType);
    }

    [Fact]
    public void ResolveLaunchPlayerType_ShouldTreatFoobarExeAsFoobar_EvenWhenFallbackTypeIsOther() {
        var settings = new AppSettings {
            FallbackPlayerType = FallbackPlayerType.Other
        };

        var playerType = TrayFeatureLogic.ResolveLaunchPlayerType(
            settings,
            @"C:\Players\foobar2000.exe",
            matchesConfiguredFallbackPath: false
        );

        Assert.Equal(FallbackPlayerType.Foobar, playerType);
    }

    [Fact]
    public void ShouldAvoidDuplicateWhenRunningWithoutWindow_ShouldBeTrueForFoobarExe() {
        var settings = new AppSettings {
            FallbackPlayerType = FallbackPlayerType.Foobar
        };

        var avoidDuplicate = TrayFeatureLogic.ShouldAvoidDuplicateWhenRunningWithoutWindow(
            settings,
            @"D:\OtherPath\foobar2000.exe",
            matchesConfiguredFallbackPath: false
        );

        Assert.True(avoidDuplicate);
    }

    [Fact]
    public void ShouldAvoidDuplicateWhenRunningWithoutWindow_ShouldBeTrueForFoobarExe_EvenWhenFallbackTypeIsOther() {
        var settings = new AppSettings {
            FallbackPlayerType = FallbackPlayerType.Other
        };

        var avoidDuplicate = TrayFeatureLogic.ShouldAvoidDuplicateWhenRunningWithoutWindow(
            settings,
            @"C:\Players\foobar2000.exe",
            matchesConfiguredFallbackPath: false
        );

        Assert.True(avoidDuplicate);
    }

    [Theory]
    [InlineData(ProcessLaunchOutcome.RunningWithoutWindow, null, "open fallback", "running in the background")]
    [InlineData(ProcessLaunchOutcome.FoobarRestoreFailed, null, "open fallback", "foobar2000")]
    [InlineData(ProcessLaunchOutcome.InvalidPath, null, "open fallback", "path is invalid")]
    [InlineData(ProcessLaunchOutcome.Failed, "Access denied", "open fallback", "Access denied")]
    [InlineData(ProcessLaunchOutcome.Failed, null, "open fallback", "unexpected error")]
    public void BuildProcessLaunchErrorMessage_ShouldDescribeLaunchFailures(
        ProcessLaunchOutcome outcome,
        string? detail,
        string operationDescription,
        string expectedSnippet
    ) {
        var result = new ProcessLaunchResult(outcome, detail);
        var message = TrayFeatureLogic.BuildProcessLaunchErrorMessage(result, operationDescription);

        Assert.Contains(expectedSnippet, message, StringComparison.OrdinalIgnoreCase);
    }
}
