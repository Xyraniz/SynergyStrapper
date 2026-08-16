namespace SynergyStrapper.Models.Persistable;

public sealed class FeatureSettings
{
    // 4.1 Multi-instance policy
    public bool AllowMultipleInstances { get; set; }
    public bool KeepMultiInstanceWatcherAlive { get; set; } = true;

    // 4.3 Stable deployment path
    public bool UseStaticDirectory { get; set; }
    public string StaticDirectory { get; set; } = string.Empty;

    // 4.4 Maintenance cleaner
    public CleanerSettings Cleaner { get; set; } = new();

    // 4.5 Memory trimmer
    public bool EnableMemoryTrimmer { get; set; }
    public int MemoryTrimThresholdMb { get; set; } = 2048;
    public int MemoryTrimIntervalSeconds { get; set; } = 60;

    // 4.6 Crash handler
    public bool DisableCrashHandler { get; set; }

    // 4.7 Tray and lifecycle
    public bool MinimizeToTray { get; set; }
    public bool KeepRunningInTray { get; set; } = true;
    public bool CloseSettingsOnLaunch { get; set; }

    // 4.8 Roblox app storage
    public AppStorageSettings AppStorage { get; set; } = new();

    // 4.9 Custom death sound
    public string CustomDeathSoundPath { get; set; } = string.Empty;

    // 4.10–4.12 managed visual customization
    public Dictionary<string, string> CursorSlots { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string PlayerIconPath { get; set; } = string.Empty;
    public string StudioIconPath { get; set; } = string.Empty;

    // 4.13–4.14 FastFlag diagnostics and typed values
    public bool EnableFastFlagAvailabilityCheck { get; set; } = true;
    public string FastFlagAllowlistUrl { get; set; } = string.Empty;
    public string FastFlagAllowlistRevision { get; set; } = string.Empty;
    public bool EnableChannelPinGuard { get; set; } = true;

    // 4.16 Studio-first mod packs
    public string ActiveStudioModPack { get; set; } = string.Empty;
    public bool ConfirmStudioDeletes { get; set; } = true;

    // 4.17–4.18 performance
    public string PerformanceProfile { get; set; } = "Balanced";
    public int FrameRateLimit { get; set; }

    // 4.19 informational overlay
    public bool EnableGameOverlay { get; set; }
    public bool OverlayShowPing { get; set; } = true;
    public bool OverlayShowRegion { get; set; } = true;
    public bool OverlayShowClock { get; set; } = true;
    public bool OverlayDimmerEnabled { get; set; }

    // 4.22 history refinements
    public bool ShowHistoryRegion { get; set; } = true;
    public bool ShowHistoryTotalTime { get; set; } = true;
}

public sealed class CleanerSettings
{
    public bool SynergyLogs { get; set; } = true;
    public bool SynergyDownloads { get; set; } = true;
    public bool RobloxLogs { get; set; }
    public bool RobloxCache { get; set; }
    public int MaxAgeDays { get; set; } = 30;
    public int MaxFilesPerDirectory { get; set; } = 200;
}

public sealed class AppStorageSettings
{
    public bool Enabled { get; set; }
    public bool DarkTheme { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool LaunchAtStartup { get; set; }
    public bool HideVersionDetails { get; set; }
    public bool HideProductionDetails { get; set; }
}
