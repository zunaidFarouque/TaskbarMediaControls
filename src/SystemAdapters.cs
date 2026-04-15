using System.Diagnostics;
using Microsoft.Win32;

namespace TaskbarMediaControls;

public sealed class ClipboardService : IClipboardService {
    public void SetText(string text) {
        Clipboard.SetText(text);
    }
}

public sealed class ProcessLauncher : IProcessLauncher {
    public void Start(string path) {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}

public sealed class StartupManager : IStartupManager {
    private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryApprovedKey =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string AppName = "TaskbarMediaControls";

    public bool StartupEntryExists() {
        try {
            using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
            return runKey?.GetValue(AppName) != null;
        }
        catch {
            return false;
        }
    }

    public bool IsStartupEnabled() {
        try {
            using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
            if (runKey == null) {
                return false;
            }

            var value = runKey.GetValue(AppName)?.ToString();
            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }

            value = value.Trim('"');
            string exePath = Path.GetFullPath(Application.ExecutablePath);
            return string.Equals(value, exePath, StringComparison.OrdinalIgnoreCase);
        }
        catch {
            return false;
        }
    }

    public void SetStartup(bool enable) {
        string exePath = Path.GetFullPath(Application.ExecutablePath);
        string value = $"\"{exePath}\"";

        using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true)
                           ?? Registry.CurrentUser.CreateSubKey(RegistryRunKey);
        if (enable) {
            runKey.SetValue(AppName, value, RegistryValueKind.String);
        }
        else {
            runKey.DeleteValue(AppName, false);
        }

        using var approvedKey = Registry.CurrentUser.OpenSubKey(RegistryApprovedKey, true)
                                ?? Registry.CurrentUser.CreateSubKey(RegistryApprovedKey);
        if (enable) {
            byte[] enabledValue = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            approvedKey.SetValue(AppName, enabledValue, RegistryValueKind.Binary);
        }
        else {
            approvedKey.DeleteValue(AppName, false);
        }
    }
}
