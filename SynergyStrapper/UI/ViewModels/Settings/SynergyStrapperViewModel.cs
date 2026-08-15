using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using SynergyStrapper.Integrations;

namespace SynergyStrapper.UI.ViewModels.Settings
{
    public class SynergyStrapperViewModel : NotifyPropertyChangedViewModel
    {
        private bool _isCheckingForUpdates;
        private string _updateStatusText = Strings.Menu_SynergyStrapper_UpdateStatus_Ready;

        public SynergyStrapperViewModel()
        {
            CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
            CleanOldFilesCommand = new AsyncRelayCommand(CleanOldFilesAsync);
        }

        public WebEnvironment[] WebEnvironments => Enum.GetValues<WebEnvironment>();

        public bool UpdateCheckingEnabled
        {
            get => App.Settings.Prop.CheckForUpdates;
            set => App.Settings.Prop.CheckForUpdates = value;
        }

        public bool AnalyticsEnabled
        {
            get => App.Settings.Prop.EnableAnalytics;
            set => App.Settings.Prop.EnableAnalytics = value;
        }

        public WebEnvironment WebEnvironment
        {
            get => App.Settings.Prop.WebEnvironment;
            set => App.Settings.Prop.WebEnvironment = value;
        }

        public Visibility WebEnvironmentVisibility => App.Settings.Prop.DeveloperMode ? Visibility.Visible : Visibility.Collapsed;

        public string InstalledVersionText => string.Format(Strings.Menu_SynergyStrapper_Version, App.Version);

        public string UpdateStatusText
        {
            get => _updateStatusText;
            private set
            {
                if (_updateStatusText == value)
                    return;

                _updateStatusText = value;
                OnPropertyChanged(nameof(UpdateStatusText));
            }
        }

        public bool IsCheckingForUpdates
        {
            get => _isCheckingForUpdates;
            private set
            {
                if (_isCheckingForUpdates == value)
                    return;

                _isCheckingForUpdates = value;
                OnPropertyChanged(nameof(IsCheckingForUpdates));
            }
        }

        public bool ShouldExportConfig { get; set; } = true;

        public bool ShouldExportLogs { get; set; } = true;

        public ICommand ExportDataCommand => new RelayCommand(ExportData);

        public IAsyncRelayCommand CleanOldFilesCommand { get; }

        public IAsyncRelayCommand CheckForUpdatesCommand { get; }

        private async Task CheckForUpdatesAsync()
        {
            if (IsCheckingForUpdates)
                return;

            IsCheckingForUpdates = true;
            UpdateStatusText = Strings.Menu_SynergyStrapper_UpdateStatus_Checking;

            try
            {
                var release = await App.GetLatestRelease();
                if (release is null)
                {
                    UpdateStatusText = Strings.Menu_SynergyStrapper_UpdateStatus_Unavailable;
                    return;
                }

                if (!GitHubUpdateService.TryGetCompatibleAsset(release, out var asset))
                {
                    UpdateStatusText = Strings.Menu_SynergyStrapper_UpdateStatus_Unavailable;
                    return;
                }

                bool updateAvailable = await GitHubUpdateService.IsUpdateAvailableAsync(
                    release,
                    asset,
                    App.Version,
                    App.IsProductionBuild,
                    Paths.Application);

                UpdateStatusText = updateAvailable
                    ? string.Format(
                        Strings.Menu_SynergyStrapper_UpdateStatus_Available,
                        release.TagName.TrimStart('v', 'V'))
                    : Strings.Menu_SynergyStrapper_UpdateStatus_Current;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("SynergyStrapperViewModel::CheckForUpdatesAsync", ex);
                UpdateStatusText = Strings.Menu_SynergyStrapper_UpdateStatus_Error;
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        private async Task CleanOldFilesAsync()
        {
            const string LOG_IDENT = "SynergyStrapperViewModel::CleanOldFilesAsync";

            try
            {
                CleanerResult result = await Task.Run(Cleaner.CleanOldFiles);
                Frontend.ShowMessageBox(
                    string.Format(
                        Strings.Menu_SynergyStrapper_Cleaner_Result,
                        result.DeletedFiles,
                        result.FailedFiles,
                        result.SkippedFiles),
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox(Strings.Menu_SynergyStrapper_Cleaner_Error, MessageBoxImage.Error);
            }
        }

        private void ExportData()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");

            var dialog = new SaveFileDialog
            {
                FileName = $"SynergyStrapper-export-{timestamp}.zip",
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip"
            };

            if (dialog.ShowDialog() != true)
                return;

            using var memStream = new MemoryStream();
            using var zipStream = new ZipOutputStream(memStream);

            if (ShouldExportConfig)
            {
                var files = new List<string>()
                {
                    App.Settings.FileLocation,
                    App.State.FileLocation,
                    App.FastFlags.FileLocation
                };

                AddFilesToZipStream(zipStream, files, "Config/");
            }

            if (ShouldExportLogs && Directory.Exists(Paths.Logs))
            {
                var files = Directory.GetFiles(Paths.Logs)
                    .Where(x => !x.Equals(App.Logger.FileLocation, StringComparison.OrdinalIgnoreCase));

                AddFilesToZipStream(zipStream, files, "Logs/");
            }

            zipStream.CloseEntry();
            zipStream.Finish();
            memStream.Position = 0;

            using var outputStream = File.OpenWrite(dialog.FileName);
            memStream.CopyTo(outputStream);

            Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
        }

        private void AddFilesToZipStream(ZipOutputStream zipStream, IEnumerable<string> files, string directory)
        {
            const string LOG_IDENT = "SynergyStrapperViewModel::AddFilesToZipStream";

            foreach (string file in files)
            {
                if (!File.Exists(file))
                    continue;

                try
                {
                    using FileStream fileStream = File.OpenRead(file);

                    var entry = new ZipEntry(directory + Path.GetFileName(file));
                    entry.DateTime = DateTime.Now;

                    zipStream.PutNextEntry(entry);
                    fileStream.CopyTo(zipStream);
                }
                catch (IOException ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to open '{file}'");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }
        }
    }
}
