using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace SynergyStrapper.UI.ViewModels.Installer
{
    public class InstallViewModel : NotifyPropertyChangedViewModel
    {
        private readonly SynergyStrapper.Installer installer = new();

        private readonly string _originalInstallLocation;

        public EventHandler<bool>? SetCanContinueEvent;

        public string InstallLocation 
        {
            get => installer.InstallLocation;
            set
            {
                if (!String.IsNullOrEmpty(ErrorMessage))
                {
                    SetCanContinueEvent?.Invoke(this, true);

                    installer.InstallLocationError = "";
                    OnPropertyChanged(nameof(ErrorMessage));
                }

            installer.InstallLocation = value;
            OnPropertyChanged(nameof(DataFoundMessageVisibility));
            OnPropertyChanged(nameof(ImportSettingsVisibility));
            OnPropertyChanged(nameof(ImportSourceName));
            }
        }

        public Visibility DataFoundMessageVisibility => installer.ExistingDataPresent ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ImportSettingsVisibility => installer.ImportSourceDetected ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ImportNotFoundVisibility => installer.ImportSettings && !installer.ImportSourceDetected ? Visibility.Visible : Visibility.Collapsed;

        public string ImportSourceName => installer.ImportSourceName;

        public string ErrorMessage => installer.InstallLocationError;

        public bool CreateDesktopShortcuts
        {
            get => installer.CreateDesktopShortcuts;
            set => installer.CreateDesktopShortcuts = value;
        }
        
        public bool CreateStartMenuShortcuts
        {
            get => installer.CreateStartMenuShortcuts;
            set => installer.CreateStartMenuShortcuts = value;
        }

        public bool AnalyticsEnabled
        {
            get => installer.EnableAnalytics;
            set => installer.EnableAnalytics = value;
        }

        public bool ImportSettings
        {
            get => installer.ImportSettings;
            set
            {
                installer.ImportSettings = value;
                OnPropertyChanged(nameof(ImportNotFoundVisibility));
            }
        }

        public ICommand BrowseInstallLocationCommand => new RelayCommand(BrowseInstallLocation);

        public ICommand ResetInstallLocationCommand => new RelayCommand(ResetInstallLocation);

        public ICommand OpenFolderCommand => new RelayCommand(OpenFolder);

        public InstallViewModel()
        {
            _originalInstallLocation = installer.InstallLocation;
        }

        public bool DoInstall()
        {
            if (!installer.CheckInstallLocation())
            {
                SetCanContinueEvent?.Invoke(this, false);

                OnPropertyChanged(nameof(ErrorMessage));
                return false;
            }

            installer.DoInstall();

            return true;
        }

        private void BrowseInstallLocation()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            InstallLocation = dialog.SelectedPath;
            OnPropertyChanged(nameof(InstallLocation));
        }

        private void ResetInstallLocation()
        {
            InstallLocation = _originalInstallLocation;
            OnPropertyChanged(nameof(InstallLocation));
        }

        private void OpenFolder() => Process.Start("explorer.exe", Paths.Base);
    }
}
