using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

using SynergyStrapper.UI.ViewModels.Settings;

namespace SynergyStrapper.UI.Elements.Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        private Models.Persistable.WindowState _state => App.State.Prop.SettingsWindow;
        private readonly Dictionary<TextBlock, Brush?> _originalTextBlockBackgrounds = new();
        private readonly List<TextBlock> _searchTextBlocks = new();
        private DispatcherTimer? _searchDebounceTimer;
        private Page? _lastSearchedPage;
        private bool _navigationStateRestored;

        public MainWindow(bool showAlreadyRunningWarning)
        {
            var viewModel = new MainWindowViewModel();

            viewModel.RequestSaveNoticeEvent += (_, _) => SettingsSavedSnackbar.Show();
            viewModel.RequestCloseWindowEvent += (_, _) => Close();

            DataContext = viewModel;
            InitializeComponent();

            App.Logger.WriteLine("MainWindow", "Initializing settings window");

            if (showAlreadyRunningWarning)
                ShowAlreadyRunningSnackbar();

            LoadState();
        }

        public void LoadState()
        {
            if (_state.Left > SystemParameters.VirtualScreenWidth)
                _state.Left = 0;

            if (_state.Top > SystemParameters.VirtualScreenHeight)
                _state.Top = 0;

            if (_state.Width > 0)
                this.Width = _state.Width;

            if (_state.Height > 0)
                this.Height = _state.Height;

            if (_state.Left > 0 && _state.Top > 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = _state.Left;
                this.Top = _state.Top;
            }
        }

        private void WpfUiWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_navigationStateRestored)
                return;

            _navigationStateRestored = true;
            RootTitleBar.MinimizeToTray = App.Settings.Prop.Features.MinimizeToTray;

            if (String.IsNullOrWhiteSpace(_state.LastPageTag))
                return;

            var item = RootNavigation.Items
                .OfType<Wpf.Ui.Controls.NavigationItem>()
                .FirstOrDefault(x => String.Equals(x.Tag?.ToString(), _state.LastPageTag, StringComparison.OrdinalIgnoreCase));

            if (item?.PageType is not null)
                Navigate(item.PageType);
        }

        private async void ShowAlreadyRunningSnackbar()
        {
            await Task.Delay(500); // wait for everything to finish loading
            AlreadyRunningSnackbar.Show();
        }

        #region Search

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            ClearSearchHighlights();
            _searchTextBlocks.Clear();
            _lastSearchedPage = e.Content as Page;

            if (_lastSearchedPage is null)
                return;

            CacheSearchTextBlocks(_lastSearchedPage);
            PerformSearch(GlobalSearchBox.Text.Trim());
        }

        private void GlobalSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounceTimer ??= CreateSearchDebounceTimer();
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private DispatcherTimer CreateSearchDebounceTimer()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                PerformSearch(GlobalSearchBox.Text.Trim());
            };
            return timer;
        }

        private void PerformSearch(string query)
        {
            ClearSearchHighlights();

            if (_lastSearchedPage is null || String.IsNullOrWhiteSpace(query))
                return;

            var normalizedQuery = query.ToLowerInvariant();
            var matches = new List<TextBlock>();

            foreach (var textBlock in _searchTextBlocks)
            {
                if (String.IsNullOrWhiteSpace(textBlock.Text))
                    continue;

                if (!textBlock.Text.ToLowerInvariant().Contains(normalizedQuery))
                    continue;

                _originalTextBlockBackgrounds[textBlock] = textBlock.Background;
                textBlock.Background = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));
                matches.Add(textBlock);
            }

            ScrollToClosestMatch(matches);
        }

        private void CacheSearchTextBlocks(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBlock textBlock && !String.IsNullOrWhiteSpace(textBlock.Text))
                    _searchTextBlocks.Add(textBlock);

                CacheSearchTextBlocks(child);
            }
        }

        private void ClearSearchHighlights()
        {
            foreach (var pair in _originalTextBlockBackgrounds)
                pair.Key.Background = pair.Value;

            _originalTextBlockBackgrounds.Clear();
        }

        private static void ScrollToClosestMatch(IEnumerable<TextBlock> matches)
        {
            foreach (var textBlock in matches)
            {
                DependencyObject? parent = textBlock;
                ScrollViewer? scrollViewer = null;

                while (parent is not null)
                {
                    if (parent is ScrollViewer candidate)
                    {
                        scrollViewer = candidate;
                        break;
                    }

                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (scrollViewer is null)
                    continue;

                var position = textBlock.TransformToAncestor(scrollViewer).Transform(new Point(0, 0));
                if (position.Y < 0 || position.Y + textBlock.ActualHeight > scrollViewer.ViewportHeight)
                    scrollViewer.ScrollToVerticalOffset(Math.Max(0, scrollViewer.VerticalOffset + position.Y));

                break;
            }
        }

        #endregion Search

        #region INavigationWindow methods

        public Frame GetFrame() => RootFrame;

        public INavigation GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.PageService = pageService;

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        private void WpfUiWindow_Closing(object sender, CancelEventArgs e)
        {
            if (App.FastFlags.Changed || App.PendingSettingTasks.Any())
            {
                var result = Frontend.ShowMessageBox(Strings.Menu_UnsavedChanges, MessageBoxImage.Warning, MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            var selectedItem = RootNavigation.Items
                .OfType<Wpf.Ui.Controls.NavigationItem>()
                .Concat(RootNavigation.Footer.OfType<Wpf.Ui.Controls.NavigationItem>())
                .FirstOrDefault(x => x.IsActive && x.Tag is not null);

            if (selectedItem?.Tag is not null)
                _state.LastPageTag = selectedItem.Tag.ToString();

            _state.Width = this.Width;
            _state.Height = this.Height;
            _state.Top = this.Top;
            _state.Left = this.Left;

            App.State.Save();
        }

        private void WpfUiWindow_Closed(object sender, EventArgs e)
        {
            if (App.LaunchSettings.TestModeFlag.Active)
                LaunchHandler.LaunchRoblox(LaunchMode.Player);
            else
                App.SoftTerminate();
        }
    }
}
