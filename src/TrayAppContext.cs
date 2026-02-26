using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace TaskbarMediaControls {
    public class TrayAppContext : ApplicationContext {
        private readonly NotifyIcon[] _trayIcons = new NotifyIcon[3];

        private readonly int[] _mediaKeys = { 0xB1, 0xB3, 0xB0 };
        private readonly string[] _tooltips = { "Previous Track", "Play / Pause", "Next Track" };


        private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private const string RegistryApprovedKey =
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

        private const string AppName = "TaskbarMediaControls";

        private bool _isPlaying;
        private bool _launchOnStartup;

        private readonly ContextMenuStrip _trayMenu;

        private NotifyIcon CreateNotifyIcon(IconType iconType, string tooltip, int index) {
            var icon = new NotifyIcon {
                Icon = IconManager.LoadIcon(iconType),
                Text = tooltip,
                Visible = true,
                ContextMenuStrip = _trayMenu
            };

            icon.MouseUp += (_, e) => {
                if (e.Button == MouseButtons.Left) {
                    SendMediaKey(_mediaKeys[index]);

                    if (index == 1)
                        TogglePlayPause();
                }
            };

            return icon;
        }

        public TrayAppContext() {
            _launchOnStartup = !StartupEntryExists() || IsStartupEnabled();
            

            _trayMenu = BuildContextMenu();

            _trayIcons[0] = CreateNotifyIcon(IconType.Previous, _tooltips[0], 0);
            _trayIcons[1] = CreateNotifyIcon(IconType.Play, _tooltips[1], 1);
            _trayIcons[2] = CreateNotifyIcon(IconType.Next, _tooltips[2], 2);

            Application.ApplicationExit += OnApplicationExit;

            if (_launchOnStartup) SetStartup(true);
            
            SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;
        }

        private void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
            if (e.Category == UserPreferenceCategory.General)
                RefreshIcons();
        }

        private void TogglePlayPause() {
            _isPlaying = !_isPlaying;

            _trayIcons[1].Icon?.Dispose();
            _trayIcons[1].Icon = IconManager.LoadIcon(_isPlaying ? IconType.Pause : IconType.Play);
            _trayIcons[1].Text = _isPlaying ? "Pause" : "Play";
        }

        private ContextMenuStrip BuildContextMenu() {
            var menu = new ContextMenuStrip();

            var startupItem = new ToolStripMenuItem("Launch on Startup") {
                Checked = _launchOnStartup,
                CheckOnClick = true
            };
            startupItem.CheckedChanged += (_, _) => {
                _launchOnStartup = startupItem.Checked;
                SetStartup(_launchOnStartup);
            };
            menu.Items.Add(startupItem);

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => Application.Exit();
            menu.Items.Add(exitItem);

            return menu;
        }

        private void OnApplicationExit(object? sender, EventArgs e) {
            foreach (var icon in _trayIcons) {
                icon.Visible = false;
                icon.Dispose();
            }
            
            SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged;
        }

        private void SetStartup(bool enable) {
            string exePath = Path.GetFullPath(Application.ExecutablePath);
            string value = $"\"{exePath}\"";

            try {
                // 1) Run key
                using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true)
                                   ?? Registry.CurrentUser.CreateSubKey(RegistryRunKey);
                if (enable)
                    runKey.SetValue(AppName, value, RegistryValueKind.String);
                else
                    runKey.DeleteValue(AppName, false);

                // 2) Force StartupApproved to enabled
                using var approvedKey = Registry.CurrentUser.OpenSubKey(RegistryApprovedKey, true)
                                        ?? Registry.CurrentUser.CreateSubKey(RegistryApprovedKey);
                if (enable) {
                    byte[] enabledValue = new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    approvedKey.SetValue(AppName, enabledValue, RegistryValueKind.Binary);
                }
                else {
                    approvedKey.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"Failed to set startup: {ex.Message}");
            }
        }

        private bool IsStartupEnabled() {
            try {
                using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
                if (runKey == null) return false;

                var value = runKey.GetValue(AppName)?.ToString();
                if (string.IsNullOrWhiteSpace(value)) return false;

                value = value.Trim('"');
                string exePath = Path.GetFullPath(Application.ExecutablePath);

                return string.Equals(value, exePath, StringComparison.OrdinalIgnoreCase);
            }
            catch {
                return false;
            }
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private void SendMediaKey(int key) {
            keybd_event((byte)key, 0, 0, 0);
            keybd_event((byte)key, 0, 2, 0);
        }

        private void RefreshIcons() {
            _trayIcons[0].Icon = IconManager.LoadIcon(IconType.Previous);
            _trayIcons[1].Icon = IconManager.LoadIcon(_isPlaying ? IconType.Pause : IconType.Play);
            _trayIcons[2].Icon = IconManager.LoadIcon(IconType.Next);
        }
        private bool StartupEntryExists() {
            try {
                using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
                return runKey?.GetValue(AppName) != null;
            }
            catch { return false; }
        }
    }
}