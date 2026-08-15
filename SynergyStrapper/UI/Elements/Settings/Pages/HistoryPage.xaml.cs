using System.Windows;
using System.Windows.Input;

namespace SynergyStrapper.UI.Elements.Settings.Pages
{
    public partial class HistoryPage
    {
        private readonly List<PlayHistoryEntry> _entries = new();

        public HistoryPage()
        {
            InitializeComponent();
            HistoryGrid.ItemsSource = _entries;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _entries.Clear();
            _entries.AddRange(PlayHistoryManager.Load());
            HistoryGrid.Items.Refresh();
            StatusTextBlock.Text = $"{_entries.Count} saved entr{(_entries.Count == 1 ? "y" : "ies")}.";
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if (HistoryGrid.SelectedItem is PlayHistoryEntry entry)
                Launch(entry);
        }

        private void HistoryGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HistoryGrid.SelectedItem is PlayHistoryEntry entry)
                Launch(entry);
        }

        private static void Launch(PlayHistoryEntry entry)
        {
            try
            {
                Process.Start(new ProcessStartInfo(entry.LaunchUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Roblox could not be launched.\n\n{ex.Message}", MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frontend.ShowMessageBox("Delete all locally saved game history?", MessageBoxImage.Question, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            PlayHistoryManager.Clear();
            _entries.Clear();
            HistoryGrid.Items.Refresh();
            StatusTextBlock.Text = "History cleared.";
        }
    }
}
