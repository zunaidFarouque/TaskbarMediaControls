using Microsoft.Win32;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TaskbarMediaControls
{
    public class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon[] trayIcons = new NotifyIcon[3];

        private readonly int[] mediaKeys = { 0xB1, 0xB3, 0xB0 };
        private readonly string[] tooltips = { "Previous Track", "Play / Pause", "Next Track" };

        private readonly string prevIcon = "TaskbarMediaControls.Resources.prev.ico";
        private readonly string playIcon = "TaskbarMediaControls.Resources.play.ico";
        private readonly string pauseIcon = "TaskbarMediaControls.Resources.pause.ico";
        private readonly string nextIcon = "TaskbarMediaControls.Resources.next.ico";

        private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "TaskbarMediaControls";

        private bool isPlaying = false;
        private bool launchOnStartup = true;

        private readonly ContextMenuStrip trayMenu;

        public TrayAppContext()
        {
            // Load startup state
            launchOnStartup = IsStartupEnabled();

            // Build shared right-click menu
            trayMenu = BuildContextMenu();

            // Create icons in order: Previous → Play → Next
            trayIcons[0] = CreateNotifyIcon(prevIcon, tooltips[0], 0);
            trayIcons[1] = CreateNotifyIcon(playIcon, tooltips[1], 1);
            trayIcons[2] = CreateNotifyIcon(nextIcon, tooltips[2], 2);

            Application.ApplicationExit += OnApplicationExit;

            // Register startup if enabled
            if (launchOnStartup) SetStartup(true);
        }

        private NotifyIcon CreateNotifyIcon(string iconPath, string tooltip, int index)
        {
            var icon = new NotifyIcon
            {
                Icon = LoadIcon(iconPath),
                Text = tooltip,
                Visible = true,
                ContextMenuStrip = trayMenu
            };

            // Use MouseUp to separate left vs right click
            icon.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    SendMediaKey(mediaKeys[index]);

                    // Toggle play/pause only for middle icon
                    if (index == 1)
                        TogglePlayPause();
                }
            };

            return icon;
        }

        private void TogglePlayPause()
        {
            isPlaying = !isPlaying;

            trayIcons[1].Icon?.Dispose();
            trayIcons[1].Icon = LoadIcon(isPlaying ? pauseIcon : playIcon);
            trayIcons[1].Text = isPlaying ? "Pause" : "Play";
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            var startupItem = new ToolStripMenuItem("Launch on Startup")
            {
                Checked = launchOnStartup,
                CheckOnClick = true
            };
            startupItem.CheckedChanged += (s, e) =>
            {
                launchOnStartup = startupItem.Checked;
                SetStartup(launchOnStartup);
            };
            menu.Items.Add(startupItem);

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();
            menu.Items.Add(exitItem);

            return menu;
        }

        private void OnApplicationExit(object? sender, EventArgs e)
        {
            foreach (var icon in trayIcons)
            {
                icon.Visible = false;
                icon.Dispose();
            }
        }
        private void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryRunKey);
                if (key != null)
                {
                    string exePath = $"\"{Assembly.GetExecutingAssembly().Location}\"";
                    if (enable)
                        key.SetValue(AppName, exePath);
                    else
                        key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update startup setting: {ex.Message}");
            }
        }
        private bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
                if (key != null)
                {
                    var value = key.GetValue(AppName)?.ToString();
                    if (string.IsNullOrWhiteSpace(value))
                        return false;

                    string exePath = Assembly.GetExecutingAssembly().Location;

                    // Remove quotes for comparison
                    value = value.Trim('"');
                    exePath = exePath.Trim('"');

                    return string.Equals(value, exePath, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            return false;
        }
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private void SendMediaKey(int key)
        {
            keybd_event((byte)key, 0, 0, 0);
            keybd_event((byte)key, 0, 2, 0);
        }

        private Icon LoadIcon(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
                throw new Exception($"Resource not found: {resourcePath}");
            return new Icon(stream);
        }
    }
}