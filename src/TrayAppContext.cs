using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TaskbarMediaControls;

public class TrayAppContext : ApplicationContext {
    private readonly NotifyIcon[] _trayIcons = new NotifyIcon[3];
    private readonly System.Windows.Forms.Timer[] _iconFeedbackTimers = new System.Windows.Forms.Timer[3];
    private readonly IAppSettingsStore _settingsStore;
    private readonly IMediaSessionService _mediaSessionService;
    private readonly IClipboardService _clipboardService;
    private readonly IProcessLauncher _processLauncher;
    private readonly IStartupManager _startupManager;
    private readonly SynchronizationContext? _uiContext;
    private readonly System.Windows.Forms.Timer[] _singleClickTimers = new System.Windows.Forms.Timer[3];

    private const int MediaKeyPrevious = 0xB1;
    private const int MediaKeyPlayPause = 0xB3;
    private const int MediaKeyNext = 0xB0;

    private readonly ContextMenuStrip _trayMenu;

    private bool _launchOnStartup;
    private AppSettings _settings;
    private MediaSessionInfo _currentMediaInfo = new() { HasActiveSession = false };

    private ToolStripMenuItem? _mediaTitleItem;
    private ToolStripMenuItem? _mediaArtistItem;
    private ToolStripMenuItem? _mediaAppItem;

    public TrayAppContext()
        : this(new AppSettingsStore(), new MediaSessionService(), new ClipboardService(), new ProcessLauncher(), new StartupManager()) {
    }

    internal TrayAppContext(
        IAppSettingsStore settingsStore,
        IMediaSessionService mediaSessionService,
        IClipboardService clipboardService,
        IProcessLauncher processLauncher,
        IStartupManager startupManager
    ) {
        _settingsStore = settingsStore;
        _mediaSessionService = mediaSessionService;
        _clipboardService = clipboardService;
        _processLauncher = processLauncher;
        _startupManager = startupManager;
        _uiContext = SynchronizationContext.Current;
        _settings = _settingsStore.Load();
        _launchOnStartup = _startupManager.IsStartupEnabled();

        _trayMenu = BuildContextMenu();

        _trayIcons[0] = CreateNotifyIcon(IconType.Previous, "Previous Track", 0);
        _trayIcons[1] = CreateNotifyIcon(IconType.Play, "Play / Pause", 1);
        _trayIcons[2] = CreateNotifyIcon(IconType.Next, "Next Track", 2);

        ApplyIconVisibility();
        ApplyTooltips();

        Application.ApplicationExit += OnApplicationExit;
        if (_launchOnStartup) {
            TrySetStartup(true);
        }

        SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;

        _mediaSessionService.MediaInfoChanged += OnMediaInfoChanged;
        _ = InitializeMediaAsync();
    }

    private NotifyIcon CreateNotifyIcon(IconType iconType, string tooltip, int index) {
        var icon = new NotifyIcon {
            Icon = IconManager.LoadIcon(iconType),
            Text = tooltip,
            Visible = false,
            ContextMenuStrip = _trayMenu
        };

        _singleClickTimers[index] = new System.Windows.Forms.Timer { Interval = 250 };
        _singleClickTimers[index].Tick += (_, _) => {
            _singleClickTimers[index].Stop();
            ExecuteClickAction(GetSingleClickAction(index));
        };

        _iconFeedbackTimers[index] = new System.Windows.Forms.Timer { Interval = 110 };
        _iconFeedbackTimers[index].Tick += (_, _) => {
            _iconFeedbackTimers[index].Stop();
            RestoreIconFromFeedback(index);
        };

        icon.MouseClick += (_, e) => {
            if (e.Button != MouseButtons.Left) {
                return;
            }

            ShowPressedFeedback(index);
            _singleClickTimers[index].Stop();
            _singleClickTimers[index].Start();
        };

        icon.MouseDoubleClick += (_, e) => {
            if (e.Button != MouseButtons.Left) {
                return;
            }

            _singleClickTimers[index].Stop();
            ExecuteClickAction(GetDoubleClickAction(index));
        };

        return icon;
    }

    private async Task InitializeMediaAsync() {
        await _mediaSessionService.InitializeAsync();
        await _mediaSessionService.RefreshAsync();
    }

    private void OnMediaInfoChanged(MediaSessionInfo info) {
        if (_uiContext == null) {
            _currentMediaInfo = info;
            UpdatePlayPauseIconForCurrentState();
            UpdateMediaMenuItems();
            ApplyTooltips();
            return;
        }

        _uiContext.Post(_ => {
            _currentMediaInfo = info;
            UpdatePlayPauseIconForCurrentState();
            UpdateMediaMenuItems();
            ApplyTooltips();
        }, null);
    }

    private void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
        if (e.Category == UserPreferenceCategory.General) {
            RefreshIcons();
        }
    }

    private ContextMenuStrip BuildContextMenu() {
        var menu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        var closeItem = new ToolStripMenuItem("Close Media Controls");
        closeItem.Click += (_, _) => Application.Exit();
        menu.Items.Add(closeItem);

        menu.Items.Add(new ToolStripSeparator());

        _mediaTitleItem = new ToolStripMenuItem("Title: N/A");
        _mediaTitleItem.Click += (_, _) => CopyIfAvailable(_currentMediaInfo.Title);
        menu.Items.Add(_mediaTitleItem);

        _mediaArtistItem = new ToolStripMenuItem("Artist: N/A");
        _mediaArtistItem.Click += (_, _) => CopyIfAvailable(_currentMediaInfo.Artist);
        menu.Items.Add(_mediaArtistItem);

        _mediaAppItem = new ToolStripMenuItem("Playing with: N/A");
        _mediaAppItem.Click += (_, _) => OpenCurrentMediaAppOrFallback();
        menu.Items.Add(_mediaAppItem);

        menu.Items.Add(new ToolStripSeparator());

        var prevItem = new ToolStripMenuItem("Previous Track");
        prevItem.Click += (_, _) => SendMediaKey(MediaKeyPrevious);
        menu.Items.Add(prevItem);

        var playPauseItem = new ToolStripMenuItem("Play/Pause");
        playPauseItem.Click += (_, _) => SendMediaKey(MediaKeyPlayPause);
        menu.Items.Add(playPauseItem);

        var nextItem = new ToolStripMenuItem("Next Track");
        nextItem.Click += (_, _) => SendMediaKey(MediaKeyNext);
        menu.Items.Add(nextItem);

        UpdateMediaMenuItems();
        return menu;
    }

    private void OnApplicationExit(object? sender, EventArgs e) {
        foreach (var icon in _trayIcons) {
            icon.Visible = false;
            icon.Dispose();
        }

        foreach (var timer in _singleClickTimers) {
            timer?.Stop();
            timer?.Dispose();
        }

        foreach (var timer in _iconFeedbackTimers) {
            timer?.Stop();
            timer?.Dispose();
        }

        SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged;
        _mediaSessionService.MediaInfoChanged -= OnMediaInfoChanged;
        _mediaSessionService.Dispose();
    }

    private void OpenSettings() {
        using var form = new SettingsForm(_settings, _launchOnStartup);
        if (form.ShowDialog() == DialogResult.OK) {
            _settings = form.UpdatedSettings;
            _settingsStore.Save(_settings);
            _launchOnStartup = form.LaunchOnStartupEnabled;
            TrySetStartup(_launchOnStartup);
            ApplyIconVisibility();
            ApplyTooltips();
            UpdateMediaMenuItems();
        }
    }

    private void ExecuteClickAction(ClickAction action) {
        switch (action) {
            case ClickAction.DoNothing:
                return;
            case ClickAction.PlayPause:
                SendMediaKey(MediaKeyPlayPause);
                return;
            case ClickAction.NextTrack:
                SendMediaKey(MediaKeyNext);
                return;
            case ClickAction.PreviousTrack:
                SendMediaKey(MediaKeyPrevious);
                return;
            case ClickAction.OpenSettings:
                OpenSettings();
                return;
            case ClickAction.OpenFallbackExecutable:
                OpenFallbackExecutable();
                return;
            default:
                return;
        }
    }

    private ClickAction GetSingleClickAction(int index) {
        return TrayFeatureLogic.GetSingleClickAction(_settings, index);
    }

    private ClickAction GetDoubleClickAction(int index) {
        return TrayFeatureLogic.GetDoubleClickAction(_settings, index);
    }

    private void ApplyIconVisibility() {
        var visibilities = TrayFeatureLogic.GetIconVisibilities(_settings);
        _trayIcons[0].Visible = visibilities[0];
        _trayIcons[1].Visible = visibilities[1];
        _trayIcons[2].Visible = visibilities[2];
    }

    private void UpdateMediaMenuItems() {
        if (_mediaTitleItem == null || _mediaArtistItem == null || _mediaAppItem == null) {
            return;
        }

        _mediaTitleItem.Text = $"Media title: {_currentMediaInfo.Title}";
        _mediaArtistItem.Text = $"Media artist: {_currentMediaInfo.Artist}";
        _mediaAppItem.Text = $"Media playing with: {_currentMediaInfo.SourceApp}";

        var state = TrayFeatureLogic.BuildMenuState(_currentMediaInfo, CanOpenFallbackApp());
        _mediaTitleItem.Text = state.MediaTitleText;
        _mediaArtistItem.Text = state.MediaArtistText;
        _mediaAppItem.Text = state.MediaAppText;
        _mediaTitleItem.Enabled = state.MediaTitleEnabled;
        _mediaArtistItem.Enabled = state.MediaArtistEnabled;
        _mediaAppItem.Enabled = state.MediaAppEnabled;
    }

    private void CopyIfAvailable(string value) {
        if (!_currentMediaInfo.HasActiveSession || string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        try {
            _clipboardService.SetText(value);
        }
        catch {
            // Ignore clipboard errors.
        }
    }

    private void OpenCurrentMediaAppOrFallback() {
        var sourcePath = _currentMediaInfo.SourceProcessPath;
        if (IsValidExecutablePath(sourcePath)) {
            var matchesFallbackPath = IsFallbackExecutablePath(sourcePath);
            var avoidDuplicate = TrayFeatureLogic.ShouldAvoidDuplicateWhenRunningWithoutWindow(
                _settings,
                sourcePath,
                matchesFallbackPath
            );
            var resolvedPlayerType = TrayFeatureLogic.ResolveLaunchPlayerType(_settings, sourcePath, matchesFallbackPath);
            TryLaunchPath(
                sourcePath,
                avoidDuplicateWhenRunningWithoutWindow: avoidDuplicate,
                playerType: resolvedPlayerType,
                operationDescription: "open current media application"
            );
            return;
        }

        TryLaunchPath(
            _settings.FallbackExecutablePath,
            avoidDuplicateWhenRunningWithoutWindow: true,
            playerType: _settings.FallbackPlayerType,
            operationDescription: "open fallback application"
        );
    }

    private bool CanOpenFallbackApp() {
        return !string.IsNullOrWhiteSpace(_settings.FallbackExecutablePath) &&
               File.Exists(_settings.FallbackExecutablePath);
    }

    private void OpenFallbackExecutable() {
        var sourcePath = _currentMediaInfo.SourceProcessPath;
        var hasValidSourcePath = IsValidExecutablePath(sourcePath);
        var hasValidFallbackPath = CanOpenFallbackApp();
        var action = TrayFeatureLogic.ResolveFallbackOpenAction(
            _settings,
            _currentMediaInfo.HasActiveSession,
            hasValidSourcePath,
            hasValidFallbackPath
        );

        switch (action) {
            case FallbackOpenAction.OpenMediaSource:
                var matchesFallbackPath = IsFallbackExecutablePath(sourcePath);
                var avoidDuplicate = TrayFeatureLogic.ShouldAvoidDuplicateWhenRunningWithoutWindow(
                    _settings,
                    sourcePath,
                    matchesFallbackPath
                );
                var resolvedPlayerType = TrayFeatureLogic.ResolveLaunchPlayerType(_settings, sourcePath, matchesFallbackPath);
                TryLaunchPath(
                    sourcePath,
                    avoidDuplicateWhenRunningWithoutWindow: avoidDuplicate,
                    playerType: resolvedPlayerType,
                    operationDescription: "open current media application"
                );
                return;
            case FallbackOpenAction.OpenFallback:
                TryLaunchPath(
                    _settings.FallbackExecutablePath,
                    avoidDuplicateWhenRunningWithoutWindow: true,
                    playerType: _settings.FallbackPlayerType,
                    operationDescription: "open fallback application"
                );
                return;
            default:
                ShowTrayError("No executable available",
                    "No valid media player executable was found for this action.");
                return;
        }
    }

    private bool IsValidExecutablePath(string? path) {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    private bool IsFallbackExecutablePath(string? path) {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(_settings.FallbackExecutablePath)) {
            return false;
        }

        try {
            var fullPath = Path.GetFullPath(path);
            var fallbackPath = Path.GetFullPath(_settings.FallbackExecutablePath);
            return string.Equals(fullPath, fallbackPath, StringComparison.OrdinalIgnoreCase);
        }
        catch {
            return false;
        }
    }

    private void TryLaunchPath(
        string? path,
        bool avoidDuplicateWhenRunningWithoutWindow,
        FallbackPlayerType playerType,
        string operationDescription
    ) {
        if (string.IsNullOrWhiteSpace(path)) {
            ShowTrayError("Invalid executable", $"{operationDescription} failed because the path is empty.");
            return;
        }

        var result = _processLauncher.Start(path, avoidDuplicateWhenRunningWithoutWindow, playerType);
        if (result.IsSuccess) {
            return;
        }

        ShowTrayError("Media action failed",
            TrayFeatureLogic.BuildProcessLaunchErrorMessage(result, operationDescription));
    }

    private void ShowTrayError(string title, string message) {
        var icon = _trayIcons.FirstOrDefault(candidate => candidate.Visible) ?? _trayIcons[1];
        icon.BalloonTipTitle = title;
        icon.BalloonTipText = message;
        icon.BalloonTipIcon = ToolTipIcon.Warning;
        icon.ShowBalloonTip(5000);
    }

    private void TrySetStartup(bool enable) {
        try {
            _startupManager.SetStartup(enable);
        }
        catch (Exception ex) {
            MessageBox.Show($"Failed to set startup: {ex.Message}");
        }
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    private void SendMediaKey(int key) {
        keybd_event((byte)key, 0, 0, 0);
        keybd_event((byte)key, 0, 2, 0);
        _ = _mediaSessionService.RefreshAsync();
    }

    private void RefreshIcons() {
        foreach (var timer in _iconFeedbackTimers) {
            timer?.Stop();
        }

        for (var index = 0; index < _trayIcons.Length; index++) {
            ApplyIconByIndex(index, pressed: false);
        }

        ApplyTooltips();
    }

    private void UpdatePlayPauseIconForCurrentState() {
        ApplyIconByIndex(1, pressed: false);
    }

    private void ShowPressedFeedback(int index) {
        if (index < 0 || index >= _trayIcons.Length) {
            return;
        }

        ApplyIconByIndex(index, pressed: true);
        _iconFeedbackTimers[index].Stop();
        _iconFeedbackTimers[index].Start();
    }

    private void RestoreIconFromFeedback(int index) {
        if (index < 0 || index >= _trayIcons.Length) {
            return;
        }

        ApplyIconByIndex(index, pressed: false);
    }

    private void ApplyIconByIndex(int index, bool pressed) {
        if (index < 0 || index >= _trayIcons.Length) {
            return;
        }

        var notifyIcon = _trayIcons[index];
        if (notifyIcon == null) {
            return;
        }

        var iconType = ResolveIconTypeForIndex(index, pressed);
        try {
            notifyIcon.Icon = IconManager.LoadIcon(iconType);
        }
        catch (ObjectDisposedException) {
            // Can happen during shutdown while events/timers are still draining.
        }
    }

    private IconType ResolveIconTypeForIndex(int index, bool pressed) {
        return index switch {
            0 => pressed ? IconType.PreviousPressed : IconType.Previous,
            1 => pressed
                ? (TrayFeatureLogic.ResolvePlayPauseIconType(_currentMediaInfo) == IconType.Pause
                    ? IconType.PausePressed
                    : IconType.PlayPressed)
                : TrayFeatureLogic.ResolvePlayPauseIconType(_currentMediaInfo),
            2 => pressed ? IconType.NextPressed : IconType.Next,
            _ => IconType.Play
        };
    }

    private void ApplyTooltips() {
        var previous = TrayFeatureLogic.BuildHoverText(_settings.ShowHoverTrackInfo, _currentMediaInfo, "Previous Track");
        var playPause = TrayFeatureLogic.BuildHoverText(_settings.ShowHoverTrackInfo, _currentMediaInfo, "Play / Pause");
        var next = TrayFeatureLogic.BuildHoverText(_settings.ShowHoverTrackInfo, _currentMediaInfo, "Next Track");

        _trayIcons[0].Text = TrayFeatureLogic.TrimTooltip(previous);
        _trayIcons[1].Text = TrayFeatureLogic.TrimTooltip(playPause);
        _trayIcons[2].Text = TrayFeatureLogic.TrimTooltip(next);
    }
}