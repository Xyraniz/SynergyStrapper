using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.Input;
using SynergyStrapper.Integrations;
using SynergyStrapper.Models.Persistable;

namespace SynergyStrapper.UI.ViewModels.Settings;

public sealed class FeaturesViewModel : NotifyPropertyChangedViewModel
{
    private string _statusText = "Ready.";
    private string _staticDirectory = string.Empty;
    private string _selectedPerformanceProfile;
    private int _fpsLimit;
    private string _studioPackName = "Default";

    public FeaturesViewModel()
    {
        _staticDirectory = Settings.StaticDirectory;
        _selectedPerformanceProfile = Settings.PerformanceProfile;
        _fpsLimit = Settings.FrameRateLimit;
    }

    public FeatureSettings Settings => App.Settings.Prop.Features;
    public IReadOnlyList<string> PerformanceProfiles => PerformanceProfileManager.Profiles.Keys.OrderBy(x => x).ToList();
    public IReadOnlyList<int> FpsOptions { get; } = new[] { 0, 30, 60, 120, 144, 240, 360 };
    public IReadOnlyList<string> CursorSlots => FeatureManager.CursorSlotFiles.Keys.ToList();

    public bool AllowMultipleInstances { get => Settings.AllowMultipleInstances; set { Settings.AllowMultipleInstances = value; OnPropertyChanged(nameof(AllowMultipleInstances)); } }
    public bool KeepMultiInstanceWatcherAlive { get => Settings.KeepMultiInstanceWatcherAlive; set { Settings.KeepMultiInstanceWatcherAlive = value; OnPropertyChanged(nameof(KeepMultiInstanceWatcherAlive)); } }
    public bool UseStaticDirectory { get => Settings.UseStaticDirectory; set { Settings.UseStaticDirectory = value; OnPropertyChanged(nameof(UseStaticDirectory)); } }
    public string StaticDirectory { get => _staticDirectory; set { _staticDirectory = value; Settings.StaticDirectory = value; OnPropertyChanged(nameof(StaticDirectory)); } }
    public int CleanerMaxAgeDays { get => Settings.Cleaner.MaxAgeDays; set { Settings.Cleaner.MaxAgeDays = Math.Clamp(value, 1, 3650); OnPropertyChanged(nameof(CleanerMaxAgeDays)); } }
    public int CleanerMaxFiles { get => Settings.Cleaner.MaxFilesPerDirectory; set { Settings.Cleaner.MaxFilesPerDirectory = Math.Clamp(value, 1, 5000); OnPropertyChanged(nameof(CleanerMaxFiles)); } }
    public bool CleanSynergyLogs { get => Settings.Cleaner.SynergyLogs; set => Settings.Cleaner.SynergyLogs = value; }
    public bool CleanSynergyDownloads { get => Settings.Cleaner.SynergyDownloads; set => Settings.Cleaner.SynergyDownloads = value; }
    public bool CleanRobloxLogs { get => Settings.Cleaner.RobloxLogs; set => Settings.Cleaner.RobloxLogs = value; }
    public bool CleanRobloxCache { get => Settings.Cleaner.RobloxCache; set => Settings.Cleaner.RobloxCache = value; }
    public bool EnableMemoryTrimmer { get => Settings.EnableMemoryTrimmer; set { Settings.EnableMemoryTrimmer = value; OnPropertyChanged(nameof(EnableMemoryTrimmer)); } }
    public int MemoryTrimThresholdMb { get => Settings.MemoryTrimThresholdMb; set { Settings.MemoryTrimThresholdMb = Math.Clamp(value, 256, 32768); OnPropertyChanged(nameof(MemoryTrimThresholdMb)); } }
    public int MemoryTrimIntervalSeconds { get => Settings.MemoryTrimIntervalSeconds; set { Settings.MemoryTrimIntervalSeconds = Math.Clamp(value, 10, 3600); OnPropertyChanged(nameof(MemoryTrimIntervalSeconds)); } }
    public bool DisableCrashHandler { get => Settings.DisableCrashHandler; set { Settings.DisableCrashHandler = value; OnPropertyChanged(nameof(DisableCrashHandler)); } }
    public bool MinimizeToTray { get => Settings.MinimizeToTray; set { Settings.MinimizeToTray = value; OnPropertyChanged(nameof(MinimizeToTray)); } }
    public bool KeepRunningInTray { get => Settings.KeepRunningInTray; set { Settings.KeepRunningInTray = value; OnPropertyChanged(nameof(KeepRunningInTray)); } }
    public bool CloseSettingsOnLaunch { get => Settings.CloseSettingsOnLaunch; set { Settings.CloseSettingsOnLaunch = value; OnPropertyChanged(nameof(CloseSettingsOnLaunch)); } }
    public bool AppStorageEnabled { get => Settings.AppStorage.Enabled; set => Settings.AppStorage.Enabled = value; }
    public bool AppStorageDarkTheme { get => Settings.AppStorage.DarkTheme; set => Settings.AppStorage.DarkTheme = value; }
    public bool AppStorageMinimizeToTray { get => Settings.AppStorage.MinimizeToTray; set => Settings.AppStorage.MinimizeToTray = value; }
    public bool AppStorageLaunchAtStartup { get => Settings.AppStorage.LaunchAtStartup; set => Settings.AppStorage.LaunchAtStartup = value; }
    public bool AppStorageHideVersion { get => Settings.AppStorage.HideVersionDetails; set => Settings.AppStorage.HideVersionDetails = value; }
    public bool AppStorageHideProduction { get => Settings.AppStorage.HideProductionDetails; set => Settings.AppStorage.HideProductionDetails = value; }
    public bool ConfirmStudioDeletes { get => Settings.ConfirmStudioDeletes; set => Settings.ConfirmStudioDeletes = value; }
    public bool EnableGameOverlay { get => Settings.EnableGameOverlay; set => Settings.EnableGameOverlay = value; }
    public bool OverlayShowPing { get => Settings.OverlayShowPing; set => Settings.OverlayShowPing = value; }
    public bool OverlayShowRegion { get => Settings.OverlayShowRegion; set => Settings.OverlayShowRegion = value; }
    public bool OverlayShowClock { get => Settings.OverlayShowClock; set => Settings.OverlayShowClock = value; }
    public bool OverlayDimmerEnabled { get => Settings.OverlayDimmerEnabled; set => Settings.OverlayDimmerEnabled = value; }
    public bool ShowHistoryRegion { get => Settings.ShowHistoryRegion; set => Settings.ShowHistoryRegion = value; }
    public bool ShowHistoryTotalTime { get => Settings.ShowHistoryTotalTime; set => Settings.ShowHistoryTotalTime = value; }
    public bool EnableFastFlagAvailabilityCheck { get => Settings.EnableFastFlagAvailabilityCheck; set => Settings.EnableFastFlagAvailabilityCheck = value; }
    public bool EnableChannelPinGuard { get => Settings.EnableChannelPinGuard; set => Settings.EnableChannelPinGuard = value; }
    public string FastFlagAllowlistUrl { get => Settings.FastFlagAllowlistUrl; set => Settings.FastFlagAllowlistUrl = value; }
    public string SelectedPerformanceProfile { get => _selectedPerformanceProfile; set { _selectedPerformanceProfile = value; Settings.PerformanceProfile = value; OnPropertyChanged(nameof(SelectedPerformanceProfile)); } }
    public int FpsLimit { get => _fpsLimit; set { _fpsLimit = value; Settings.FrameRateLimit = value; OnPropertyChanged(nameof(FpsLimit)); } }
    public string StudioPackName { get => _studioPackName; set { _studioPackName = value; OnPropertyChanged(nameof(StudioPackName)); } }
    public string StatusText { get => _statusText; private set { _statusText = value; OnPropertyChanged(nameof(StatusText)); } }

    public ICommand BrowseStaticDirectoryCommand => new RelayCommand(BrowseStaticDirectory);
    public ICommand PreviewCleanerCommand => new RelayCommand(() => StatusText = FormatCleanerResult(Cleaner.Preview(), true));
    public ICommand CleanNowCommand => new RelayCommand(() => StatusText = FormatCleanerResult(Cleaner.CleanOldFiles(), false));
    public ICommand ChooseDeathSoundCommand => new RelayCommand(ChooseDeathSound);
    public ICommand RemoveDeathSoundCommand => new RelayCommand(() => StatusText = FeatureManager.RemoveDeathSound() ? "Custom death sound removed." : "Unable to remove custom death sound.");
    public ICommand ChooseCursorCommand => new RelayCommand<string>(ChooseCursor);
    public ICommand RemoveCursorCommand => new RelayCommand<string>(slot => StatusText = FeatureManager.RemoveCursor(slot ?? string.Empty) ? $"{slot} cursor removed." : "Unable to remove cursor.");
    public ICommand ImportCursorSetCommand => new RelayCommand(ImportCursorSet);
    public ICommand ExportCursorSetCommand => new RelayCommand(ExportCursorSet);
    public ICommand ChooseIconCommand => new RelayCommand<string>(ChooseIcon);
    public ICommand ApplyAppStorageCommand => new RelayCommand(() => StatusText = FeatureManager.TryApplyAppStorage(out string message) ? message : $"appStorage.json was not changed: {message}");
    public IAsyncRelayCommand RefreshFastFlagAvailabilityCommand => new AsyncRelayCommand(RefreshFastFlagAvailabilityAsync);
    public ICommand ApplyPerformanceCommand => new RelayCommand(() => StatusText = PerformanceProfileManager.Apply(SelectedPerformanceProfile, FpsLimit) ? $"Applied {SelectedPerformanceProfile} profile with FPS limit {FpsLimit}." : "Performance profile could not be applied.");
    public ICommand RollbackPerformanceCommand => new RelayCommand(() => StatusText = PerformanceProfileManager.RollbackLast() ? "Last performance change rolled back." : "No performance backup was available.");
    public ICommand OpenStudioPackFolderCommand => new RelayCommand(OpenStudioPackFolder);
    public ICommand ApplyStudioPackCommand => new RelayCommand(ApplyStudioPack);

    private void BrowseStaticDirectory()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Choose a safe Roblox static installation directory." };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            StaticDirectory = dialog.SelectedPath;
    }

    private void ChooseDeathSound()
    {
        var dialog = new OpenFileDialog { Filter = "Audio files|*.ogg;*.wav;*.mp3|All files|*.*" };
        if (dialog.ShowDialog() == true)
            StatusText = FeatureManager.TrySetDeathSound(dialog.FileName, out string error) ? "Custom death sound staged." : error;
    }

    private void ChooseCursor(string? slot)
    {
        if (string.IsNullOrWhiteSpace(slot))
            return;
        var dialog = new OpenFileDialog { Filter = "PNG files|*.png" };
        if (dialog.ShowDialog() == true)
            StatusText = FeatureManager.TrySetCursor(slot, dialog.FileName, out string error) ? $"{slot} cursor staged." : error;
    }

    private void ImportCursorSet()
    {
        var dialog = new OpenFileDialog { Filter = "Cursor sets|*.zip" };
        if (dialog.ShowDialog() == true)
            StatusText = FeatureManager.TryImportCursorSet(dialog.FileName, out string message) ? message : $"Cursor set rejected: {message}";
    }

    private void ExportCursorSet()
    {
        var dialog = new SaveFileDialog { Filter = "Cursor sets|*.zip", FileName = "SynergyCursorSet.zip" };
        if (dialog.ShowDialog() == true)
            StatusText = FeatureManager.TryExportCursorSet(dialog.FileName, out string message) ? message : message;
    }

    private void ChooseIcon(string? product)
    {
        if (string.IsNullOrWhiteSpace(product))
            return;
        var dialog = new OpenFileDialog { Filter = "Icon files|*.ico" };
        if (dialog.ShowDialog() == true)
            StatusText = FeatureManager.TrySetIcon(product, dialog.FileName, out string error) ? $"{product} icon staged." : error;
    }

    private async Task RefreshFastFlagAvailabilityAsync()
    {
        StatusText = await FastFlagAvailabilityService.RefreshAsync() ? "FastFlag allowlist refreshed." : "Could not refresh allowlist; cached data was retained.";
        IReadOnlyList<FastFlagAvailabilityEntry> statuses = FastFlagAvailabilityService.CheckCurrentFlags();
        StatusText += $" {statuses.Count(x => x.Status == FastFlagAvailability.Unavailable)} unavailable flags detected.";
    }

    private void OpenStudioPackFolder()
    {
        StudioModPackManager.EnsurePack(StudioPackName);
        Process.Start(new ProcessStartInfo("explorer.exe", StudioModPackManager.GetPackDirectory(StudioPackName)) { UseShellExecute = true });
    }

    private void ApplyStudioPack()
    {
        try
        {
            if (Settings.ConfirmStudioDeletes && StudioModPackManager.ContainsDeleteEntries(StudioPackName))
            {
                MessageBoxResult confirmation = Frontend.ShowMessageBox(
                    "This pack contains DELETE entries. Existing Studio files will be backed up before removal. Continue?",
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo);
                if (confirmation != MessageBoxResult.Yes)
                {
                    StatusText = "Studio pack application cancelled.";
                    return;
                }
            }

            StudioModPackResult result = StudioModPackManager.Apply(StudioPackName);
            StatusText = $"Studio pack applied: {result.AppliedFiles} copied, {result.DeletedFiles} deleted, {result.SkippedFiles} skipped. Backup: {result.BackupPath}";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    private static string FormatCleanerResult(CleanerResult result, bool preview)
        => $"{(preview ? "Preview" : "Cleanup")}: scanned {result.ScannedFiles}, candidates {result.CandidateFiles}, deleted {result.DeletedFiles}, failed {result.FailedFiles}, skipped {result.SkippedFiles}.";
}
