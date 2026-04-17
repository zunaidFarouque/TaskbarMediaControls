using System.Diagnostics;
using System.Collections.Concurrent;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TaskbarMediaControls;

public enum IconType {
    Previous,
    Next,
    Play,
    Pause,
    PreviousPressed,
    NextPressed,
    PlayPressed,
    PausePressed,
}

public static class IconManager {
    private static readonly string PrevIcon = "TaskbarMediaControls.Resources.back.ico";
    private static readonly string PlayIcon = "TaskbarMediaControls.Resources.play.ico";
    private static readonly string PauseIcon = "TaskbarMediaControls.Resources.pause.ico";
    private static readonly string NextIcon = "TaskbarMediaControls.Resources.skip.ico";
    private static readonly string PrevPressedIcon = "TaskbarMediaControls.Resources.back_pressed.ico";
    private static readonly string PlayPressedIcon = "TaskbarMediaControls.Resources.play_pressed.ico";
    private static readonly string PausePressedIcon = "TaskbarMediaControls.Resources.pause_pressed.ico";
    private static readonly string NextPressedIcon = "TaskbarMediaControls.Resources.skip_pressed.ico";


    private static readonly string PrevIcon_Light = "TaskbarMediaControls.Resources.back_light.ico";
    private static readonly string PlayIcon_Light = "TaskbarMediaControls.Resources.play_light.ico";
    private static readonly string PauseIcon_Light = "TaskbarMediaControls.Resources.pause_light.ico";
    private static readonly string NextIcon_Light = "TaskbarMediaControls.Resources.skip_light.ico";
    private static readonly string PrevPressedIcon_Light = "TaskbarMediaControls.Resources.back_pressed_light.ico";
    private static readonly string PlayPressedIcon_Light = "TaskbarMediaControls.Resources.play_pressed_light.ico";
    private static readonly string PausePressedIcon_Light = "TaskbarMediaControls.Resources.pause_pressed_light.ico";
    private static readonly string NextPressedIcon_Light = "TaskbarMediaControls.Resources.skip_pressed_light.ico";
    private static readonly ConcurrentDictionary<(IconType Type, bool IsDarkMode), Icon> IconCache = new();

    private static string GetIconPathForType(IconType type, bool isDarkMode) {
        switch (type) {
            case IconType.Previous:
                return isDarkMode ? PrevIcon : PrevIcon_Light;
            case IconType.Next:
                return isDarkMode ? NextIcon : NextIcon_Light;
            case IconType.Play:
                return isDarkMode ? PlayIcon : PlayIcon_Light;
            case IconType.Pause:
                return isDarkMode ? PauseIcon : PauseIcon_Light;
            case IconType.PreviousPressed:
                return isDarkMode ? PrevPressedIcon : PrevPressedIcon_Light;
            case IconType.NextPressed:
                return isDarkMode ? NextPressedIcon : NextPressedIcon_Light;
            case IconType.PlayPressed:
                return isDarkMode ? PlayPressedIcon : PlayPressedIcon_Light;
            case IconType.PausePressed:
                return isDarkMode ? PausePressedIcon : PausePressedIcon_Light;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static bool IsSystemDarkMode() {
        try {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            Debug.WriteLine($"System dark mode: {val}");
            return val is int i && i == 0;
        }
        catch {
            return true;
        }
    }

    public static Icon LoadIcon(IconType type) {
        var isDarkMode = IsSystemDarkMode();
        var template = IconCache.GetOrAdd((type, isDarkMode), _ => LoadTemplateIcon(type, isDarkMode));
        return (Icon)template.Clone();
    }

    public static void ResetCache() {
        foreach (var entry in IconCache.Values) {
            entry.Dispose();
        }

        IconCache.Clear();
    }

    private static Icon LoadTemplateIcon(IconType type, bool isDarkMode) {
        var assembly = Assembly.GetExecutingAssembly();
        if (IsPressedType(type)) {
            var fallbackType = GetFallbackType(type);
            var baseIcon = LoadTemplateIcon(fallbackType, isDarkMode);
            try {
                return CreatePressedVariant(baseIcon);
            }
            finally {
                baseIcon.Dispose();
            }
        }

        using var stream = assembly.GetManifestResourceStream(GetIconPathForType(type, isDarkMode));
        if (stream == null) {
            var fallbackType = GetFallbackType(type);
            using var fallbackStream = assembly.GetManifestResourceStream(GetIconPathForType(fallbackType, isDarkMode));
            if (fallbackStream == null) {
                throw new Exception($"Resource not found: {type}");
            }

            return new Icon(fallbackStream);
        }

        return new Icon(stream);
    }

    private static bool IsPressedType(IconType type) {
        return type is IconType.PreviousPressed or IconType.NextPressed or IconType.PlayPressed or IconType.PausePressed;
    }

    private static Icon CreatePressedVariant(Icon baseIcon) {
        using var source = baseIcon.ToBitmap();
        var target = new Bitmap(source.Width, source.Height);
        using (var graphics = Graphics.FromImage(target)) {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.Clear(Color.Transparent);

            const float scale = 0.86f;
            var width = source.Width * scale;
            var height = source.Height * scale;
            var x = (source.Width - width) / 2f;
            var y = (source.Height - height) / 2f;
            graphics.DrawImage(source, x, y, width, height);
        }

        var handle = target.GetHicon();
        try {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally {
            DestroyIcon(handle);
            target.Dispose();
        }
    }

    private static IconType GetFallbackType(IconType type) {
        return type switch {
            IconType.PreviousPressed => IconType.Previous,
            IconType.NextPressed => IconType.Next,
            IconType.PlayPressed => IconType.Play,
            IconType.PausePressed => IconType.Pause,
            _ => type
        };
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}