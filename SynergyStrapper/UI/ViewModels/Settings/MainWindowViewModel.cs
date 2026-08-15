using System.Windows;
using System.Windows.Input;
using SynergyStrapper.UI.Elements.About;
using CommunityToolkit.Mvvm.Input;

namespace SynergyStrapper.UI.ViewModels.Settings
{
    public class MainWindowViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand OpenAboutCommand => new RelayCommand(OpenAbout);
        
        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);
        
        public ICommand CloseWindowCommand => new RelayCommand(CloseWindow);

        public EventHandler? RequestSaveNoticeEvent;
        
        public EventHandler? RequestCloseWindowEvent;

        public bool TestModeEnabled
        {
            get => App.LaunchSettings.TestModeFlag.Active;
            set
            {
                if (value)
                {
                    var result = Frontend.ShowMessageBox(Strings.Menu_TestMode_Prompt, MessageBoxImage.Information, MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                App.LaunchSettings.TestModeFlag.Active = value;
            }
        }

        private void OpenAbout() => new MainWindow().ShowDialog();

        private void CloseWindow() => RequestCloseWindowEvent?.Invoke(this, EventArgs.Empty);

        private void SaveSettings()
        {
            const string LOG_IDENT = "MainWindowViewModel::SaveSettings";

            App.Settings.Save();
            App.State.Save();
            App.FastFlags.Save();

            foreach (var pair in App.PendingSettingTasks.ToList())
            {
                var task = pair.Value;

                if (task.Changed)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Executing pending task '{task}'");
                    task.Execute();
                }
            }

            // A failed task keeps Changed=true (for example, when a remote
            // emoji font cannot be downloaded). Do not discard that state or
            // the user will be told that settings were saved when they were not.
            var failedTasks = App.PendingSettingTasks.Values
                .Where(task => task.Changed)
                .ToList();

            App.PendingSettingTasks.Clear();
            foreach (var task in failedTasks)
                App.PendingSettingTasks[task.Name] = task;

            RequestSaveNoticeEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
