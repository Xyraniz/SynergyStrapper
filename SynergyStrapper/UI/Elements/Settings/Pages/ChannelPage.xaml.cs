using System.Windows;
using Microsoft.Win32;
using SynergyStrapper.AppData;
using SynergyStrapper.RobloxInterfaces;

namespace SynergyStrapper.UI.Elements.Settings.Pages
{
    public partial class ChannelPage
    {
        public ChannelPage()
        {
            InitializeComponent();
            LoadValues();
        }

        private void LoadValues()
        {
            ChannelTextBox.Text = App.Settings.Prop.RobloxChannel;
            ChannelHashTextBox.Text = App.Settings.Prop.RobloxChannelHash;
            UpdateRobloxToggle.IsChecked = App.Settings.Prop.UpdateRoblox;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string channel = ChannelTextBox.Text.Trim().ToLowerInvariant();
            if (String.IsNullOrWhiteSpace(channel) || !Regex.IsMatch(channel, "^[a-z0-9_-]+$"))
            {
                Frontend.ShowMessageBox("Channel names may only contain letters, numbers, underscores and hyphens.", MessageBoxImage.Warning);
                return;
            }

            if (!String.IsNullOrWhiteSpace(ChannelHashTextBox.Text) && !Regex.IsMatch(ChannelHashTextBox.Text.Trim(), "^[a-zA-Z0-9-]+$"))
            {
                Frontend.ShowMessageBox("The pinned version GUID contains invalid characters.", MessageBoxImage.Warning);
                return;
            }

            App.Settings.Prop.RobloxChannel = channel;
            App.Settings.Prop.RobloxChannelHash = ChannelHashTextBox.Text.Trim();
            App.Settings.Prop.UpdateRoblox = UpdateRobloxToggle.IsChecked ?? true;
            App.Settings.Save();

            SetRegistryChannel(new RobloxPlayerData().RegistryName, channel);
            SetRegistryChannel(new RobloxStudioData().RegistryName, channel);

            StatusBar.Title = "Channel saved";
            StatusBar.Message = channel == Deployment.DefaultChannel
                ? "Player and Studio will use the production channel."
                : $"Player and Studio will use '{channel}'. Restart Roblox to apply the deployment choice.";
            StatusBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Success;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ChannelTextBox.Text = Deployment.DefaultChannel;
            ChannelHashTextBox.Text = "";
            UpdateRobloxToggle.IsChecked = true;
            SaveButton_Click(sender, e);
        }

        private static void SetRegistryChannel(string registryName, string channel)
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey($"SOFTWARE\\ROBLOX Corporation\\Environments\\{registryName}\\Channel");
            key.SetValueSafe("www.roblox.com", channel == Deployment.DefaultChannel ? "" : channel);
        }
    }
}
