using SynergyStrapper.UI.ViewModels.Settings;

using System.Windows.Controls;

namespace SynergyStrapper.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for AppearancePage.xaml
    /// </summary>
    public partial class AppearancePage
    {
        public AppearancePage()
        {
            DataContext = new AppearanceViewModel(this);
            InitializeComponent();
        }

        public void CustomThemeSelection(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not AppearanceViewModel viewModel
                || sender is not ListBox list
                || list.SelectedItem is not string selectedTheme)
            {
                return;
            }

            viewModel.SelectedCustomTheme = selectedTheme;
            viewModel.SelectedCustomThemeName = selectedTheme;

            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomTheme));
            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomThemeName));
        }
    }
}
