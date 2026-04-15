using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TaskbarMediaControls;

public class TrayAppContext : ApplicationContext {
    private readonly NotifyIcon[] _trayIcons = new NotifyIcon[3];
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

        icon.MouseClick += (_, e) => {
            if (e.Button != MouseButtons.Left) {
                return;
            }

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
            UpdateMediaMenuItems();
            ApplyTooltips();
            return;
        }

        _uiContext.Post(_ => {
            _currentMediaInfo = info;
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

        var exitItem = new ToolStripMenuItem("Exit / Close");
        exitItem.Click += (_, _) => Application.Exit();
        menu.Items.Add(exitItem);

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new ToolStripSeparator());

        _mediaTitleItem = new ToolStripMenuItem("Media title: N/A");
        _mediaTitleItem.Click += (_, _) => CopyIfAvailable(_currentMediaInfo.Title);
        menu.Items.Add(_mediaTitleItem);

        _mediaArtistItem = new ToolStripMenuItem("Media artist: N/A");
        _mediaArtistItem.Click += (_, _) => CopyIfAvailable(_currentMediaInfo.Artist);
        menu.Items.Add(_mediaArtistItem);

        _mediaAppItem = new ToolStripMenuItem("Media playing with: N/A");
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
        if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath)) {
            _processLauncher.Start(sourcePath);
            return;
        }

        if (CanOpenFallbackApp()) {
            _processLauncher.Start(_settings.FallbackExecutablePath);
        }
    }

    private bool CanOpenFallbackApp() {
        return !string.IsNullOrWhiteSpace(_settings.FallbackExecutablePath) &&
               File.Exists(_settings.FallbackExecutablePath);
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
        _trayIcons[0].Icon = IconManager.LoadIcon(IconType.Previous);
        _trayIcons[1].Icon = IconManager.LoadIcon(IconType.Play);
        _trayIcons[2].Icon = IconManager.LoadIcon(IconType.Next);
        ApplyTooltips();
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