using SynergyStrapper.UI.ViewModels.Settings;

using System;
using System.IO;
using System.Windows;

namespace SynergyStrapper.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ModsPage.xaml
    /// </summary>
    public partial class ModsPage
    {
        public ModsPage()
        {
            DataContext = new ModsViewModel();
            InitializeComponent();
        }

        private void CursorSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataContext is not ModsViewModel viewModel)
                return;

            if (viewModel.CursorTypeTask.NewState == Enums.CursorType.Custom && !File.Exists(Paths.CustomCursor))
            {
                Frontend.ShowMessageBox("Choose a custom PNG before selecting the custom cursor preset.", MessageBoxImage.Warning);
                viewModel.CursorTypeTask.NewState = Enums.CursorType.Default;
                CursorTypeComboBox.SelectedItem = Enums.CursorType.Default;
            }

            Dispatcher.BeginInvoke(
                new Action(viewModel.RefreshCursorPreview),
                System.Windows.Threading.DispatcherPriority.DataBind
            );
        }
    }
}
