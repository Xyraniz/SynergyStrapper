using System.Windows.Controls;

using SynergyStrapper.UI.ViewModels.Settings;

namespace SynergyStrapper.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for IntegrationsPage.xaml
    /// </summary>
    public partial class IntegrationsPage
    {
        public IntegrationsPage()
        {
            DataContext = new IntegrationsViewModel();
            InitializeComponent();
        }

        public void CustomIntegrationSelection(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not IntegrationsViewModel viewModel
                || sender is not ListBox list
                || list.SelectedItem is not CustomIntegration selectedIntegration)
            {
                return;
            }

            viewModel.SelectedCustomIntegration = selectedIntegration;
            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomIntegration));
        }
    }
}
