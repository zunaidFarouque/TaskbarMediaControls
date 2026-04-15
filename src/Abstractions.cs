namespace TaskbarMediaControls;

public interface IAppSettingsStore {
    AppSettings Load();
    void Save(AppSettings settings);
}

public interface IMediaSessionService : IDisposable {
    event Action<MediaSessionInfo>? MediaInfoChanged;
    Task InitializeAsync();
    Task<MediaSessionInfo> GetCurrentInfoAsync();
    Task RefreshAsync();
}

public interface IClipboardService {
    void SetText(string text);
}

public interface IProcessLauncher {
    void Start(string path);
}

public interface IStartupManager {
    bool StartupEntryExists();
    bool IsStartupEnabled();
    void SetStartup(bool enable);
}
