using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SynergyStrapper.UI.Elements.Settings.Pages
{
    public partial class GlobalSettingsPage
    {
        private readonly GlobalSettingsManager _manager = new();

        public GlobalSettingsPage()
        {
            InitializeComponent();
            LoadValues();
        }

        private void LoadValues()
        {
            if (!_manager.Load())
            {
                StatusBar.Title = "Roblox settings not found";
                StatusBar.Message = $"Start Roblox once, then reload this page. Expected file: {_manager.FileLocation}";
                StatusBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Warning;
                return;
            }

            FramerateTextBox.Text = _manager.Get("Rendering.FramerateCap") ?? "";
            SensitivityTextBox.Text = _manager.Get("User.MouseSensitivity") ?? "";
            SelectByTag(QualityComboBox, _manager.Get("Rendering.SavedQualityLevel"));
            SelectByTag(FontSizeComboBox, _manager.Get("UI.FontSize"));
            ReducedMotionToggle.IsChecked = String.Equals(_manager.Get("UI.ReducedMotion"), "true", StringComparison.OrdinalIgnoreCase);
            VREnabledToggle.IsChecked = String.Equals(_manager.Get("User.VREnabled"), "true", StringComparison.OrdinalIgnoreCase);

            StatusBar.Title = "Settings loaded";
            StatusBar.Message = "Values are read from Roblox's local GlobalBasicSettings_13.xml. Save to apply changes.";
            StatusBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Informational;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Int32.TryParse(FramerateTextBox.Text.Trim(), out int framerate) || framerate < 0 || framerate > 1000)
            {
                Frontend.ShowMessageBox("Framerate cap must be a whole number between 0 and 1000.", MessageBoxImage.Warning);
                return;
            }

            if (!Double.TryParse(SensitivityTextBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double sensitivity) || sensitivity < 0 || sensitivity > 100)
            {
                Frontend.ShowMessageBox("Mouse sensitivity must be a number between 0 and 100.", MessageBoxImage.Warning);
                return;
            }

            _manager.Set("Rendering.FramerateCap", framerate.ToString(CultureInfo.InvariantCulture));
            _manager.Set("User.MouseSensitivity", sensitivity.ToString(CultureInfo.InvariantCulture));
            _manager.Set("Rendering.SavedQualityLevel", GetSelectedTag(QualityComboBox) ?? "0");
            _manager.Set("UI.FontSize", GetSelectedTag(FontSizeComboBox) ?? "1");
            _manager.Set("UI.ReducedMotion", (ReducedMotionToggle.IsChecked ?? false).ToString().ToLowerInvariant());
            _manager.Set("User.VREnabled", (VREnabledToggle.IsChecked ?? false).ToString().ToLowerInvariant());

            if (!_manager.Save())
            {
                Frontend.ShowMessageBox("Roblox could not save its global settings. Close Roblox and try again.", MessageBoxImage.Error);
                return;
            }

            StatusBar.Title = "Settings saved";
            StatusBar.Message = "Global Roblox settings were saved successfully.";
            StatusBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Success;
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadValues();
        }

        private static void SelectByTag(ComboBox comboBox, string? tag)
        {
            if (tag is null)
                return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (String.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private static string? GetSelectedTag(ComboBox comboBox)
            => (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
    }
}
